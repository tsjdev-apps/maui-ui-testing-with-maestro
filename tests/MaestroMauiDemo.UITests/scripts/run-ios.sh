#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'HELP'
Usage: bash run-ios.sh [--device-id UDID] [--flow FILE.yaml] [--skip-build]

Select an available iOS Simulator, build/install the app, and run Maestro.
  --device-id UDID  Select a simulator directly instead of prompting.
  --flow FILE.yaml  Run one file from the flows directory.
  --skip-build      Skip BOTH build and installation; the app must be installed.
  --help            Show this help.

Requires macOS, compatible Xcode/.NET MAUI tooling, Java 17+, and native Maestro.
Only a simulator booted by this script is shut down afterward.
HELP
}

fail() {
    printf 'ERROR: %s\n' "$1" >&2
    exit "${2:-1}"
}

run_checked() {
    local message=$1
    shift
    if "$@"; then
        return 0
    else
        local code=$?
        fail "$message (exit code $code)." "$code"
    fi
}

capture_checked() {
    local message=$1 output
    shift
    if output=$("$@" 2>&1); then
        printf '%s\n' "$output"
    else
        local code=$?
        printf '%s\n' "$output" >&2
        fail "$message (exit code $code)." "$code"
    fi
}

device_id=''
flow=''
skip_build=false
while [[ $# -gt 0 ]]; do
    case "$1" in
        --device-id|--flow)
            [[ $# -ge 2 && -n "$2" && "$2" != --* ]] || fail "Missing value for $1."
            if [[ "$1" == --device-id ]]; then device_id=$2; else flow=$2; fi
            shift 2
            ;;
        --skip-build) skip_build=true; shift ;;
        --help) usage; exit 0 ;;
        *) usage >&2; fail "Unknown argument: $1" ;;
    esac
done

[[ "$(uname -s)" == Darwin ]] || fail 'Run this script on macOS; iOS Simulators and Xcode are not available on Windows or Linux.'
script_dir=$(cd -P -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
workspace=$(cd "$script_dir/.." && pwd)
repository_root=$(cd "$script_dir/../../.." && pwd)
project="$repository_root/src/MaestroMauiDemo/MaestroMauiDemo.csproj"
cd "$repository_root"

printf 'Maestro .NET MAUI iOS UI Tests\nChecking prerequisites...\n'
for tool in dotnet maestro xcrun osascript plutil open; do
    command -v "$tool" >/dev/null 2>&1 || fail "Required command '$tool' was not found. Configure the macOS prerequisites first."
done
[[ -n "${JAVA_HOME:-}" && -x "$JAVA_HOME/bin/java" ]] || fail 'Set JAVA_HOME to a Java 17+ JDK containing an executable bin/java.'
java_version=$(capture_checked 'Java could not start; use Java 17 or newer' "$JAVA_HOME/bin/java" --version)
if [[ "$java_version" =~ ^(openjdk|java)[[:space:]]+([0-9]+) ]]; then
    [[ "${BASH_REMATCH[2]}" -ge 17 ]] || fail "Java 17 or newer is required: $java_version"
else
    fail "Could not recognize the Java version: $java_version"
fi
printf '%s\n' "$java_version"
run_checked 'The repository-pinned .NET SDK is unavailable' dotnet --version
run_checked 'Native Maestro could not start' maestro --version
capture_checked 'Select a full, compatible Xcode installation; simctl is unavailable' xcrun --find simctl >/dev/null
run_checked 'Xcode could not start; check the selected developer directory' xcrun xcodebuild -version

flow_target=$workspace
if [[ -n "$flow" ]]; then
    [[ "$flow" == *.yaml && "$flow" != */* && "$flow" != *\\* ]] || fail '--flow must be a YAML file name from the flows directory.'
    flow_target="$workspace/flows/$flow"
    [[ -f "$flow_target" ]] || fail "The requested flow does not exist: $flow"
fi

machine=$(capture_checked 'Could not determine the Mac architecture' uname -m)
case "$machine" in
    arm64) runtime=iossimulator-arm64 ;;
    x86_64)
        translated=$(capture_checked 'Could not check the Rosetta translation state' sysctl -in sysctl.proc_translated)
        case "$translated" in
            1) runtime=iossimulator-arm64 ;;
            0|'') runtime=iossimulator-x64 ;;
            *) fail "Unexpected Rosetta translation state: $translated" ;;
        esac
        ;;
    *) fail "Unsupported Mac architecture: $machine" ;;
esac

frameworks=$(capture_checked 'Could not evaluate the MAUI target frameworks' dotnet msbuild "$project" -nologo -verbosity:quiet -getProperty:TargetFrameworks)
framework=''
framework_count=0
while IFS= read -r candidate; do
    if [[ "$candidate" =~ ^net[0-9]+\.[0-9]+-ios([0-9].*)?$ ]]; then
        framework=$candidate
        framework_count=$((framework_count + 1))
    fi
done <<< "${frameworks//;/$'\n'}"
[[ "$framework_count" -eq 1 ]] || fail "Expected exactly one iOS target framework, found $framework_count."
app_id=$(capture_checked 'Could not evaluate the iOS ApplicationId' dotnet msbuild "$project" -nologo -verbosity:quiet \
    "-p:TargetFramework=$framework" -p:Configuration=Debug "-p:RuntimeIdentifier=$runtime" -getProperty:ApplicationId)
[[ -n "$app_id" ]] || fail 'The MAUI project does not define an iOS ApplicationId.'
printf 'iOS target: %s; runtime: %s; bundle ID: %s\n' "$framework" "$runtime" "$app_id"

results_dir="$workspace/TestResults"
run_checked 'Could not create the local results directory' mkdir -p "$results_dir"
simulators_json=''
simulators_rows=''
app_candidates=''
boot_pid=''
booted_by_script=false
tests_passed=false

stop_boot_monitor() {
    [[ -n "$boot_pid" ]] || return 0
    if kill -0 "$boot_pid" 2>/dev/null; then
        if ! kill -TERM "$boot_pid"; then
            printf 'ERROR: Could not stop boot monitor PID %s.\n' "$boot_pid" >&2
            return 1
        fi
        local deadline=$((SECONDS + 5))
        while kill -0 "$boot_pid" 2>/dev/null && [[ "$SECONDS" -lt "$deadline" ]]; do sleep 1; done
        if kill -0 "$boot_pid" 2>/dev/null; then
            if ! kill -KILL "$boot_pid"; then
                printf 'ERROR: Could not terminate boot monitor PID %s.\n' "$boot_pid" >&2
                return 1
            fi
        fi
    fi
    if wait "$boot_pid"; then
        :
    else
        local code=$?
        case "$code" in
            130|137|143) : ;;
            *) printf 'Boot monitor exited with code %s during cleanup.\n' "$code" >&2 ;;
        esac
    fi
    boot_pid=''
}

cleanup() {
    local code=$? cleanup_code=0 file
    trap - EXIT INT TERM
    if ! stop_boot_monitor; then cleanup_code=1; fi
    if [[ "$booted_by_script" == true ]]; then
        printf 'Shutting down simulator booted by this script: %s\n' "$device_id"
        if xcrun simctl shutdown "$device_id"; then
            :
        else
            cleanup_code=$?
            printf 'ERROR: Owned simulator shutdown failed (exit code %s).\n' "$cleanup_code" >&2
        fi
    fi
    for file in "$simulators_json" "$simulators_rows" "$app_candidates"; do
        if [[ -n "$file" ]]; then
            if rm -f "$file"; then
                :
            else
                cleanup_code=$?
                printf 'ERROR: Could not remove temporary file: %s\n' "$file" >&2
            fi
        fi
    done
    if [[ "$code" -eq 0 ]]; then code=$cleanup_code; fi
    if [[ "$code" -eq 0 && "$tests_passed" == true ]]; then printf 'Maestro iOS UI tests passed.\n'; fi
    exit "$code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

simulators_json=$(capture_checked 'Could not create the simulator metadata file' mktemp "$results_dir/ios-simulators.XXXXXX")
simulators_rows=$(capture_checked 'Could not create the simulator selection file' mktemp "$results_dir/ios-selection.XXXXXX")
run_checked 'Could not list available simulators' xcrun simctl list devices available --json >"$simulators_json"
run_checked 'Could not parse simulator JSON with native macOS JavaScript' osascript -l JavaScript - "$simulators_json" >"$simulators_rows" <<'JXA'
function run(argv) {
    ObjC.import("Foundation");
    var text = ObjC.unwrap($.NSString.stringWithContentsOfFileEncodingError(argv[0], $.NSUTF8StringEncoding, Ref()));
    if (typeof text !== "string") throw new Error("Could not read simulator metadata as UTF-8.");
    var data = JSON.parse(text);
    if (!data || !data.devices || typeof data.devices !== "object" || Array.isArray(data.devices)) {
        throw new Error("Expected a simctl devices dictionary.");
    }
    var rows = [];
    var seen = {};
    Object.keys(data.devices).sort().forEach(function (runtime) {
        var version = /^com\.apple\.CoreSimulator\.SimRuntime\.iOS-([0-9]+(?:-[0-9]+)*)$/.exec(runtime);
        if (!version) return;
        if (!Array.isArray(data.devices[runtime])) throw new Error("Expected a device array for " + runtime);
        data.devices[runtime].forEach(function (device) {
            if (device.isAvailable !== true) return;
            if (typeof device.udid !== "string" || typeof device.name !== "string" || typeof device.state !== "string") {
                throw new Error("Incomplete simulator metadata.");
            }
            if (!/^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(device.udid)) {
                throw new Error("Invalid simulator UDID.");
            }
            if (["Booted", "Booting", "Shutdown"].indexOf(device.state) === -1) return;
            if (seen[device.udid]) throw new Error("Duplicate simulator UDID.");
            seen[device.udid] = true;
            var name = device.name.replace(/[\t\r\n]/g, " ");
            rows.push([device.udid, device.state, name + " | iOS " + version[1].replace(/-/g, ".")]);
        });
    });
    rows.sort(function (left, right) {
        return left[2].localeCompare(right[2]) || left[0].localeCompare(right[0]);
    });
    return rows.map(function (row) { return row.join("\t"); }).join("\n");
}
JXA

simulator_ids=('')
simulator_states=('')
simulator_labels=('')
simulator_count=0
while IFS=$'\t' read -r id state label; do
    [[ -n "$id" ]] || continue
    simulator_ids[$simulator_count]=$id
    simulator_states[$simulator_count]=$state
    simulator_labels[$simulator_count]=$label
    simulator_count=$((simulator_count + 1))
done <"$simulators_rows"
[[ "$simulator_count" -gt 0 ]] || fail 'No available iOS Simulator is ready for selection. Install a compatible runtime/create a simulator in Xcode, or wait for a shutting-down simulator. This script does not create simulators.'

selected=-1
if [[ -n "$device_id" ]]; then
    for ((index = 0; index < simulator_count; index++)); do
        if [[ "${simulator_ids[$index]}" == "$device_id" ]]; then selected=$index; break; fi
    done
    [[ "$selected" -ge 0 ]] || fail "Simulator '$device_id' is not in the available iOS list. Use its exact UDID."
elif [[ "$simulator_count" -eq 1 ]]; then
    selected=0
else
    printf 'Available iOS Simulators:\n'
    for ((index = 0; index < simulator_count; index++)); do
        printf '  %s. %s (%s) [%s]\n' "$((index + 1))" "${simulator_labels[$index]}" "${simulator_states[$index]}" "${simulator_ids[$index]}"
    done
    printf 'Select a simulator [1-%s] (use --device-id to skip this prompt): ' "$simulator_count"
    IFS= read -r selection || fail 'No simulator selection was received. Pass --device-id for unattended execution.'
    [[ "$selection" =~ ^[1-9][0-9]{0,5}$ ]] || fail 'Invalid simulator selection; enter a listed number.'
    [[ "$selection" -le "$simulator_count" ]] || fail 'Invalid simulator selection; enter a listed number.'
    selected=$((selection - 1))
fi
device_id=${simulator_ids[$selected]}
printf 'iOS Simulator: %s [%s]\n' "${simulator_labels[$selected]}" "$device_id"
if [[ "${simulator_states[$selected]}" == Shutdown ]]; then
    run_checked 'Simulator startup failed' xcrun simctl boot "$device_id"
    booted_by_script=true
fi
run_checked 'Could not open Simulator.app' open -a Simulator --args -CurrentDeviceUDID "$device_id"

boot_log=$(capture_checked 'Could not create the boot log' mktemp "$results_dir/ios-boot.XXXXXX")
printf 'Waiting for simulator boot (up to 180 seconds). Boot log: %s\n' "$boot_log"
xcrun simctl bootstatus "$device_id" >"$boot_log" 2>&1 &
boot_pid=$!
boot_started=$SECONDS
while kill -0 "$boot_pid" 2>/dev/null; do
    if [[ $((SECONDS - boot_started)) -ge 180 ]]; then
        stop_boot_monitor || fail 'The simulator boot deadline expired and its monitor could not be stopped.'
        cat "$boot_log" >&2
        fail "Simulator '$device_id' did not finish booting within 180 seconds."
    fi
    sleep 1
done
if wait "$boot_pid"; then
    boot_pid=''
else
    code=$?
    boot_pid=''
    cat "$boot_log" >&2
    fail "Simulator boot monitoring failed (exit code $code)." "$code"
fi

if [[ "$skip_build" == false ]]; then
    build_dir=$(capture_checked 'Could not create a fresh iOS build directory' mktemp -d "$results_dir/ios-build.XXXXXX")
    printf 'Building iOS Simulator Debug app (%s)...\n' "$runtime"
    run_checked 'iOS build failed; check compatible Xcode and the installed MAUI/iOS workload' dotnet build "$project" \
        -f "$framework" -c Debug "-p:MauiAppTargetFramework=$framework" \
        "-p:RuntimeIdentifier=$runtime" "-p:OutputPath=$build_dir" --verbosity minimal
    app_candidates=$(capture_checked 'Could not create the app discovery file' mktemp "$results_dir/ios-apps.XXXXXX")
    run_checked 'Could not discover the fresh app bundle' find "$build_dir" -type d -name '*.app' -prune -print0 >"$app_candidates"
    app_bundle=''
    app_count=0
    while IFS= read -r -d '' candidate; do
        app_bundle=$candidate
        app_count=$((app_count + 1))
    done <"$app_candidates"
    [[ "$app_count" -eq 1 ]] || fail "Expected one fresh .app bundle in '$build_dir', found $app_count. Nothing will be installed."
    bundle_id=$(capture_checked 'Could not read the built bundle identifier' plutil -extract CFBundleIdentifier raw -o - "$app_bundle/Info.plist")
    [[ "$bundle_id" == "$app_id" ]] || fail "Built bundle ID '$bundle_id' does not match evaluated ApplicationId '$app_id'."
    printf 'Installing simulator app: %s\n' "$app_bundle"
    run_checked 'Simulator app installation failed' xcrun simctl install "$device_id" "$app_bundle"
else
    printf 'Skipping both build and installation; using the already installed app.\n'
fi
installed_app=$(capture_checked "Application '$app_id' is not installed; run without --skip-build first" \
    xcrun simctl get_app_container "$device_id" "$app_id" app)
[[ -n "$installed_app" ]] || fail "Could not resolve the installed app container for '$app_id'."
printf 'Running Maestro iOS UI tests: %s\n' "$flow_target"
run_checked 'Maestro iOS UI tests failed' maestro "--device=$device_id" --platform=ios test --env "APP_ID=$app_id" \
    --test-output-dir "$results_dir" "$flow_target"
tests_passed=true
