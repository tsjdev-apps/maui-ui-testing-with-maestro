# Maestro UI tests

[Maestro](https://docs.maestro.dev/maestro-cli) drives an installed application
through its native UI and accessibility hierarchy. These black-box tests exercise
the .NET MAUI app without referencing its view models, adding application packages,
or creating another .NET test project.

The five YAML flows are shared by Android and iOS. Optional runners provide
PowerShell on Windows/Android and native Bash on macOS/iOS; the flows also run
directly with Maestro. This workspace contains no CI/CD configuration.
The repository's [Android PR workflow](../../.github/workflows/android-ci.yml)
runs Core unit tests and builds the Android app only; it does not execute these
Maestro flows or start an emulator.
The separate [manual Android UI workflow](../../.github/workflows/android-ui-tests.yml)
does run all five flows on a GitHub-hosted Android emulator.

## Manual GitHub Actions run

The Android Maestro workflow uses only `workflow_dispatch`: no PR, push, or
scheduled triggers. After the workflow is available on the default branch
(`main`), select **Actions > Android Maestro UI tests > Run workflow**, choose
the branch, and start the run.

It installs checksum-verified Maestro 2.11.0 and builds a fresh, signed
`android-x64` Debug APK with `EmbedAssembliesIntoApk=true`. It evaluates `APP_ID`
from the project and installs the APK on an API-35 Google APIs x86_64 emulator
(Pixel 5 profile). All five existing flows run unchanged; no local interactive
runner is invoked and no extra test project or app package is added.

`maestro-ui-results` contains the explicit JUnit report, Maestro artifacts,
emulator logcat, and run metadata. It is uploaded even when UI tests fail, and
retained for seven days. Tests retain their failure result if optional logcat
collection fails; that diagnostic failure is reported as a warning.

The emulator action handles shutdown. The CI-only KVM permission step affects
the temporary Ubuntu runner, not local machines. The job has a 45-minute timeout,
the emulator/test step has a 20-minute timeout, and boot is limited to 300 seconds.
No Maestro Cloud or iOS tests run. API-35 and native Linux/GitHub-hosted execution
still need their first real manual run; the local Windows verification below
used API 33 and is not a substitute.

## Prerequisites and native installation

### Windows / Android

- The .NET SDK pinned in [global.json](../../global.json), the MAUI Android
  workload, and its compatible Android SDK/JDK toolchain. See the
  [application setup](../../README.md#prerequisites).
- Android SDK Platform-Tools (ADB), Android Emulator, and an existing x86_64 or
  arm64-v8a Android Virtual Device (AVD).
- Java 17 or newer for Maestro, with `JAVA_HOME` pointing to the JDK directory
  containing `bin\java.exe`.
- Native Maestro CLI in your user `PATH`; PowerShell 7 is recommended.
- `ANDROID_HOME` (or `ANDROID_SDK_ROOT`) pointing to the Android SDK directory.
  The runner resolves ADB and the emulator there; they need not be in `PATH`.

Follow the [official installation guide](https://docs.maestro.dev/maestro-cli/how-to-install-maestro-cli).
For Windows, download `maestro.zip` from the
[official releases](https://github.com/mobile-dev-inc/maestro/releases), extract
it into a stable directory, and add its `bin` directory to your user `PATH`.
Open a new terminal after configuring the environment. Windows uses the native
CLI directly, **without WSL** or an ADB bridge.

Check your installation:

```powershell
dotnet --version
& "$env:JAVA_HOME\bin\java.exe" --version
maestro --version
& "$env:ANDROID_HOME\platform-tools\adb.exe" version
& "$env:ANDROID_HOME\platform-tools\adb.exe" devices -l
```

Configure prerequisites yourself. The runner does not install tools, change
`PATH`, configure Java or the SDK, alter firewall settings, or require elevation.

### macOS / iOS

- The repository-pinned .NET SDK and MAUI/iOS workload.
- Xcode compatible with that workload, with its command-line tools selected and
  an iOS Simulator runtime installed. Do not upgrade Xcode independently of the
  workload's supported version.
- Java 17+ and a valid `JAVA_HOME`.
- Bash 3.2+ and the native macOS `osascript`/`plutil` utilities. The Bash runner
  does not require PowerShell, Python, `jq`, or GNU `timeout`.
- Native Maestro CLI, installed following the same official guide. Its macOS
  Homebrew option is:

```bash
brew tap mobile-dev-inc/tap
brew trust --formula mobile-dev-inc/tap/maestro
brew install mobile-dev-inc/tap/maestro
maestro --version
```

Local simulator testing does not need a physical device, a provisioning profile,
or paid Apple Developer Program membership. iOS simulators run on macOS, not
Windows.

## Workspace and scenarios

| File | Purpose |
| --- | --- |
| [config.yaml](config.yaml) | Discovers only `flows/*` for suite execution. |
| [smoke.yaml](flows/smoke.yaml) | Checks `MAUI Tasks`, the list, all three seeds, and the enabled Add button. Tagged `smoke`. |
| [add-task.yaml](flows/add-task.yaml) | Saves `Write Maestro UI tests` and verifies return navigation and the new row. |
| [validate-task.yaml](flows/validate-task.yaml) | Checks empty and whitespace-only titles against `Enter a task title.`, then saves valid input. |
| [complete-task.yaml](flows/complete-task.yaml) | Completes `Explore Maestro`, asserts `Completed`, toggles back, and checks that row's `To do` status. |
| [delete-task.yaml](flows/delete-task.yaml) | Deletes `Write UI tests` immediately and verifies the other seeds remain. |
| [run-android.ps1](scripts/run-android.ps1) | Checks prerequisites, starts or reuses an emulator, builds/installs the app, invokes Maestro, and stops only an emulator it started. |
| [run-ios.sh](scripts/run-ios.sh) | Selects an available iOS Simulator, boots it if needed, builds/installs the app, invokes the same flows, and shuts down only a simulator it booted. |

### State isolation

Each flow uses `appId: ${APP_ID}` and starts with
`launchApp: { clearState: true }`. Maestro stops the existing app by default
before launching. The process restart recreates the in-memory singleton task
service with `Learn .NET MAUI`, `Explore Maestro`, and `Write UI tests`.
Clearing app data also avoids persisted state if the app later gains storage.
This is destructive to the selected sample app's data, not to the emulator.

Flows do not depend on execution order, another flow creating a task, or random
model IDs. Navigation alone does not reset the singleton. There are no arbitrary
sleeps; assertions wait for visible UI and `scrollUntilVisible` finds added rows.

## Run Android tests with PowerShell

The runner reuses an eligible running emulator. If none is running, it starts an
existing AVD: one available AVD is selected automatically; several produce a
numbered selection prompt. You can also start an emulator manually through Android
Device Manager / Android Studio. List your existing AVDs with:

```powershell
& "$env:ANDROID_HOME\emulator\emulator.exe" -list-avds
```

The runner never creates an AVD. If the list is empty, create one yourself through
the Android tools. Use `-AvdName` with an exact name from the list to avoid the
interactive prompt:

```powershell
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1 -AvdName "pixel_5_-_api_33"
```

Replace that example name with one present on your machine. A running instance of
the named AVD is reused; otherwise that AVD is started, even if other AVDs are
running. `-AvdName` and `-DeviceId` cannot be combined.

From the repository root:

```powershell
# Start/select an emulator, build, install, and run all five flows.
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1

# Build, install, and run one flow.
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1 -Flow "add-task.yaml"

# Skip BOTH build and installation; the app must already be installed.
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1 -SkipBuild

# Select a particular online emulator; wait for boot if necessary.
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1 -DeviceId "emulator-5554"

# Combine options.
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1 `
    -DeviceId "emulator-5554" -Flow "smoke.yaml" -SkipBuild
```

The runner resolves paths from its own location and evaluates the project from
the repository root, so it also works when invoked by absolute path from another
working directory. `-Flow` accepts a file name inside [flows](flows/), not an
arbitrary path.

Only online emulator serials are eligible; offline, unauthorized, and physical
devices are excluded. Exactly one fully booted emulator is selected automatically;
multiple booted emulators require `-DeviceId`. If none is fully booted but one
online emulator is booting, the runner waits for it rather than starting another.
Several booting emulators require an explicit selection.

The runner waits up to 180 seconds for `sys.boot_completed=1`; it does not rely
on a fixed boot delay. A newly started emulator uses a free console/ADB port pair
and its process ownership is checked before testing. Startup logs are stored in
ignored [TestResults](TestResults/).

After success or failure, an emulator started by the runner is shut down. A
pre-existing emulator is never shut down by the runner. Cleanup failures are
reported and return nonzero; an earlier build/install/Maestro error code is
preserved. `-SkipBuild` still skips both build and installation, so it requires
the app to already be installed in the selected AVD.

The runner evaluates the actual Android target framework and `ApplicationId`
with MSBuild. It builds Android Debug for the selected emulator's CPU and creates
a unique build directory inside [TestResults](TestResults/). Exactly one newly
generated `*-Signed.apk` is required; an old APK is never used as a fallback.
Assemblies are embedded in the APK because `adb install` alone does not perform
MAUI Debug Fast Deployment. Installation uses `adb -s ... install -r`.

Every native exit code is checked. Failed builds/installations prevent Maestro
execution, and Maestro's nonzero exit code is returned unchanged. Missing or
invalid prerequisites and selection errors also return nonzero.

## Run Android tests directly

The current Android application ID and iOS bundle ID are both
`de.tsjdevapps.maestromauidemo`. The flows do not hard-code this ID.

To build and install without the helper, from the repository root with the
prerequisites configured and `emulator-5554` already booted:

```powershell
dotnet build src\MaestroMauiDemo\MaestroMauiDemo.csproj `
    -f net10.0-android -c Debug `
    -p:AndroidPackageFormats=apk -p:EmbedAssembliesIntoApk=true

& "$env:ANDROID_HOME\platform-tools\adb.exe" -s emulator-5554 install -r `
    src\MaestroMauiDemo\bin\Debug\net10.0-android\de.tsjdevapps.maestromauidemo-Signed.apk
```

After successful installation, run the suite, one flow, or the smoke tag:

```powershell
maestro --device=emulator-5554 --platform=android test `
    --env APP_ID=de.tsjdevapps.maestromauidemo `
    --test-output-dir tests\MaestroMauiDemo.UITests\TestResults `
    tests\MaestroMauiDemo.UITests

maestro --device=emulator-5554 --platform=android test `
    --env APP_ID=de.tsjdevapps.maestromauidemo `
    --test-output-dir tests\MaestroMauiDemo.UITests\TestResults `
    tests\MaestroMauiDemo.UITests\flows\add-task.yaml

maestro --device=emulator-5554 --platform=android test `
    --env APP_ID=de.tsjdevapps.maestromauidemo --include-tags smoke `
    --test-output-dir tests\MaestroMauiDemo.UITests\TestResults `
    tests\MaestroMauiDemo.UITests
```

Replace the example emulator serial if yours differs. The SDK's ADB command may
also be used as `adb` when Platform-Tools is already in your `PATH`.

## Run the same flows on an iOS Simulator

### Bash runner on macOS

With the macOS prerequisites configured, from the repository root:

```bash
# Select a simulator, build/install the app, and run all five flows.
bash tests/MaestroMauiDemo.UITests/scripts/run-ios.sh

# Select a simulator and run just the smoke flow.
bash tests/MaestroMauiDemo.UITests/scripts/run-ios.sh --flow smoke.yaml

# Skip BOTH build and installation; the app must already be installed.
bash tests/MaestroMauiDemo.UITests/scripts/run-ios.sh --skip-build

bash tests/MaestroMauiDemo.UITests/scripts/run-ios.sh --help
```

Several available simulators produce a numbered selection showing device name,
iOS runtime, state, and UDID. One available simulator is selected automatically.
Unavailable devices, non-iOS runtimes, and devices currently shutting down are
excluded. To avoid the prompt, pass the exact simulator UDID:

```bash
xcrun simctl list devices available
read -r SIMULATOR_UDID
bash tests/MaestroMauiDemo.UITests/scripts/run-ios.sh \
  --device-id "$SIMULATOR_UDID" --flow smoke.yaml --skip-build
```

The script resolves paths from its own location, evaluates the actual iOS target
framework and bundle ID using MSBuild, and uses `iossimulator-arm64` on Apple
Silicon or `iossimulator-x64` on Intel. A Rosetta shell on Apple Silicon still
selects the ARM64 simulator build.

It opens Simulator.app and waits for `simctl bootstatus` to finish, with a
180-second deadline. A failed or timed-out boot prevents build/install/tests.
Builds target only iOS Simulator Debug in a unique ignored output directory.
Exactly one fresh `.app` is required, and its `CFBundleIdentifier` must match
the evaluated project ID before installation.

Already booted/booting simulators are reused and never shut down by the runner.
Only a simulator booted by this script is shut down on success, failure, or a
handled interrupt. Simulator.app itself is not quit globally. No simulator is
created; no tools, signing profiles, global environment variables, or Xcode
configuration are installed or changed.

Every native failure is reported. Earlier build/install/Maestro exit codes are
preserved if cleanup also fails. Boot logs and fresh build outputs remain under
ignored [TestResults](TestResults/); temporary selection files are removed.
Invoking the file with `bash` works without changing its executable file mode.
Shell scripts use LF line endings, enforced by the repository attributes so a
Windows checkout does not introduce macOS-incompatible CRLF.

**The iOS runner is prepared on Windows, not verified on a Mac.** Syntax and
local fixture checks cannot establish that native Xcode builds, code signing,
Simulator.app behavior, or iOS accessibility selectors work. The first Mac run
must verify those surfaces with the compatible Xcode/workload combination.

### Manual macOS workflow

The same flows still work without either helper. List available simulators, then
enter the UDID of the simulator you want to use:

```bash
xcrun simctl list devices available
read -r SIMULATOR_UDID
```

If that simulator is shut down, boot it; skip `boot` if it is already booted:

```bash
xcrun simctl boot "$SIMULATOR_UDID"
open -a Simulator
xcrun simctl bootstatus "$SIMULATOR_UDID" -b
```

Choose the runtime matching the Mac's architecture and build:

```bash
case "$(uname -m)" in
  arm64) RID=iossimulator-arm64 ;;
  x86_64) RID=iossimulator-x64 ;;
  *) echo "Unsupported Mac architecture" >&2; exit 1 ;;
esac

dotnet build src/MaestroMauiDemo/MaestroMauiDemo.csproj \
  -f net10.0-ios -c Debug -p:RuntimeIdentifier="$RID"
```

After a successful build, evaluate the bundle ID, install the simulator app, and
run the shared workspace:

```bash
APP_ID="$(dotnet msbuild src/MaestroMauiDemo/MaestroMauiDemo.csproj \
  -nologo -verbosity:quiet -p:TargetFramework=net10.0-ios \
  -p:Configuration=Debug -p:RuntimeIdentifier="$RID" -getProperty:ApplicationId)"

xcrun simctl install "$SIMULATOR_UDID" \
  "src/MaestroMauiDemo/bin/Debug/net10.0-ios/$RID/MaestroMauiDemo.app"

maestro --device="$SIMULATOR_UDID" --platform=ios test \
  --env "APP_ID=$APP_ID" \
  --test-output-dir tests/MaestroMauiDemo.UITests/TestResults \
  tests/MaestroMauiDemo.UITests
```

For one flow, replace the last argument with
`tests/MaestroMauiDemo.UITests/flows/smoke.yaml`. These are simulator builds, not
device/IPAs. **iOS execution has not been verified on this Windows host.**

## Selectors and platform differences

The app already provides stable `AutomationId` values and task-specific semantic
descriptions. No production changes or test-only hooks were needed.

Android hierarchy inspection with native Maestro verified the IDs, including
`TaskList`, `TaskTitleEntry`, and repeated row controls. Completion and deletion
combine a control ID with its task-specific accessibility text:
`Completion for Explore Maestro` and `Delete task: Write UI tests`.
Title assertions combine `TaskTitleLabel` with deterministic text. The final
to-do assertion is scoped to the layout containing the intended task title.

Maestro selectors are regex-based; literal dots in the seeded title and
validation message are escaped. See the official
[selector guide](https://docs.maestro.dev/maestro-flows/flow-control-and-logic/how-to-use-selectors).
No indexes or screen coordinates are used.

MAUI uses native Android resource IDs and iOS accessibility identifiers. iOS
can expose a different accessibility tree; inspect it on a simulator before
assuming all parent-child relationships match Android. The completion test
checks visible status rather than requiring identical native checkbox state
properties. `hideKeyboard` is used only after focusing and entering text:
Maestro handles Android and iOS differently. On iOS, enable the software keyboard
if a connected hardware keyboard prevents it from appearing, and verify keyboard
dismissal and the scoped status selector when first running these shared flows.

## Diagnostics and troubleshooting

With the app open on the target device:

```powershell
maestro --device=emulator-5554 --platform=android hierarchy
maestro check-syntax tests\MaestroMauiDemo.UITests\flows\smoke.yaml
```

On macOS use `maestro --device="$SIMULATOR_UDID" --platform=ios hierarchy`.

| Problem | Check |
| --- | --- |
| Maestro not found | Use the native installation; its `bin` must be in the terminal's user `PATH`. |
| Invalid Java configuration | Check `JAVA_HOME\bin\java.exe --version`; `JAVA_HOME` must not point to a removed JDK or its `bin` subdirectory. Maestro needs Java 17+. |
| Android SDK / ADB missing | Check the SDK environment variable and installed Platform-Tools; the runner does not search unrelated SDK installations. |
| No AVD / invalid selection | Create an AVD through Android tools if none exists. Select its displayed number or pass its exact name with `-AvdName`. |
| Emulator not ready | Inspect the emulator window and startup logs in `TestResults`; boot is limited to 180 seconds. Use `-DeviceId` for a particular online emulator. |
| Startup/shutdown failed | Inspect the reported logs and error code. Only the runner-owned process tree is stopped; existing emulators are left alone. |
| iOS runner rejects the host | Run it on macOS with native Bash, not on Windows or through WSL. |
| iOS tooling missing | Check the selected full Xcode installation, compatible MAUI/iOS workload, Java 17+ `JAVA_HOME`, and native Maestro. Configure these manually. |
| iOS simulator absent / boot timed out | Install a compatible runtime/create a simulator in Xcode. Inspect the reported boot log; unavailable/non-iOS devices are excluded. |
| iOS app absent with `--skip-build` | Run the Bash helper without `--skip-build` first for that simulator. |
| Build failed | Check the pinned .NET SDK, installed Android workload, and its supported Java/Android SDK versions in the actual build output. |
| App absent or wrong ID | Install it first, avoid `-SkipBuild` on a fresh emulator, and pass the evaluated ID instead of `com.tsjdevapps.maestromauidemo`. |
| App exits immediately after manual ADB install | Embed Debug assemblies with `-p:EmbedAssembliesIntoApk=true`; a Fast Deployment APK alone is insufficient. |
| Element not found | Inspect the foreground app's hierarchy, compare IDs/labels, confirm seeded state, and account for iOS accessibility/keyboard differences. |

The CLI's normal output is the primary feedback. Local logs and screenshots go
under ignored [TestResults](TestResults/); do not add them to source control.
Optional local JUnit output:

```powershell
New-Item -ItemType Directory -Force tests\MaestroMauiDemo.UITests\TestResults | Out-Null
maestro --device=emulator-5554 --platform=android test `
    --env APP_ID=de.tsjdevapps.maestromauidemo `
    --test-output-dir tests\MaestroMauiDemo.UITests\TestResults `
    --format JUNIT --output tests\MaestroMauiDemo.UITests\TestResults\report.xml `
    tests\MaestroMauiDemo.UITests
```

Use `--format HTML` with an `.html` output path for an HTML report. Report paths
must be supplied explicitly: `--test-output-dir` alone does not redirect reports.
See [reports and artifacts](https://docs.maestro.dev/maestro-flows/workspace-management/test-reports-and-artifacts).

## Verified locally

Native Maestro 2.11.0 on Windows, an Android API-33 x86_64 emulator, and .NET SDK
10.0.403:

- All five flows passed as a suite and independently in a different order.
- Default runner execution built a fresh signed APK, installed it, and passed
  the suite from a working directory outside the repository.
- `-SkipBuild` passed the suite without building or installing.
- Interactive automatic AVD startup built/installed the app and passed all five
  flows, then stopped its owned emulator. Direct `-AvdName` startup with
  `-SkipBuild -Flow smoke.yaml` also passed.
- A named running emulator was reused and remained running after the test.
- PowerShell parsing and native `maestro check-syntax` passed for all flows.
- Original native-command fixtures verified prerequisite errors, device selection,
  build/install failures, missing or ambiguous APKs, and exact exit propagation.
  Another 30 isolated cases verified AVD selection, process/console ownership,
  startup errors, the measured 180-second boot deadline, cleanup of owned child
  processes, and preservation of earlier error codes when shutdown also fails.
  The temporary fixtures are not part of this workspace.
- Direct CLI smoke-tag selection produced a local JUnit report with one passing
  test and zero failures.
- The manual workflow passed `actionlint`, syntax checks of all six shell blocks,
  and nine isolated APK-selection/build/UI/diagnostic failure cases. A fresh
  native API-33 run produced a JUnit report with five passing cases and no
  failures, errors, or skipped cases; the local runner shut down its emulator.
  These checks do not exercise GitHub-hosted KVM or the Linux toolchain.
- All 69 existing xUnit cases still passed; their project and source are unchanged.

One Android tooling warning (`XA0033`) occurred when the installed JDK reported
the five-component version `17.0.21.0.2`; packaging, installation, and all flows
still succeeded. It was not suppressed. Native iOS execution remains unverified.
