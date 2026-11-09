# .NET MAUI UI Testing with Maestro

**MAUI Tasks** is a small, in-memory task management application built with .NET MAUI.
It is the starting point for a blog series about automating .NET MAUI applications
with Maestro on Android and iOS.

The repository contains the application, plain .NET unit tests, local Maestro
UI flows shared by Android and iOS, and platform-specific GitHub Actions workflows.

## Features

- Start with three example tasks.
- Add a task with a required title. Whitespace is trimmed, and blank titles show a
  clear inline validation message.
- Mark tasks as completed or return them to the to-do state.
- Delete tasks immediately.
- Cancel adding a task using the Cancel button or Shell back navigation.
- Use the application in light or dark mode with accessible labels and stable
  automation identifiers.

Tasks are stored in memory and reset when the application restarts. There is no
backend, database, authentication, telemetry, or localization.

## Technology stack

- .NET 10 and C# 14
- .NET MAUI 10.0.120 with XAML and compiled bindings
- CommunityToolkit.Mvvm 8.4.2 for observable properties and asynchronous commands
- Shell navigation and the standard .NET MAUI dependency injection container
- NuGet Central Package Management
- xUnit for framework-independent unit tests
- Maestro CLI for native black-box UI testing

The SDK is pinned to **10.0.403** in [global.json](global.json). Compatible patches
within the same SDK feature band are allowed; preview SDKs are not selected.
Package versions live in [Directory.Packages.props](Directory.Packages.props).

## Supported platforms

| Platform | Target framework | Minimum OS |
| --- | --- | --- |
| Android | `net10.0-android` | Android 5.0 / API 21 |
| iOS | `net10.0-ios` | iOS 15 |
| Windows | `net10.0-windows10.0.19041.0` | Windows 10 version 1809 |
| Mac Catalyst (secondary target) | `net10.0-maccatalyst` | macOS 14 / Mac Catalyst 17 |

The Windows target is included only on Windows hosts. Apple targets are omitted on
Linux, following the standard MAUI template's host conditions.

The application ID is `de.tsjdevapps.maestromauidemo` across platforms. Windows
development uses an unpackaged application; no signing certificate is needed.

## Prerequisites

These prerequisites apply to app development. Unit tests only need the .NET 10
SDK; see [Unit tests](#unit-tests) for the SDK-only command.

- .NET SDK **10.0.403** or a compatible later 10.0.4xx patch.
- The .NET MAUI workloads for the platforms you intend to build.
- A .NET 10-compatible Visual Studio installation with the .NET MAUI workload, or
  Visual Studio Code with the .NET MAUI extension and the platform tools.
- For Android: the Android SDK, a supported JDK, and an emulator or connected
  device. The .NET MAUI/Android tooling can install the required SDK and JDK.
- For Windows: Windows 10/11 and the Windows SDK.
- For iOS and Mac Catalyst: a Mac with the Xcode version required by the installed
  .NET Apple workload. iOS development from Visual Studio on Windows requires a
  paired Mac. An iOS device build also requires appropriate Apple signing.

Run these commands from the repository root:

```powershell
dotnet --version
dotnet workload restore
dotnet restore
dotnet sln dotnet-maui-maestro-demo.slnx list
```

`dotnet workload restore` installs the workloads needed by the solution on the
current host and may require elevated permissions. It does not install Xcode or
set up an Android emulator.

If you only have Windows development tooling installed, you can limit package
restore for the application to that platform without modifying the project:

```powershell
dotnet restore src\MaestroMauiDemo\MaestroMauiDemo.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0
```

## Build

Choose a framework explicitly rather than building every platform on an
incompatible host.
The MAUI application targets only platforms with a UI. Its shared Core library
targets plain `net10.0` and builds automatically through the project reference.

### Windows

```powershell
dotnet build src\MaestroMauiDemo\MaestroMauiDemo.csproj -f net10.0-windows10.0.19041.0 -c Debug --no-restore
```

### Android

```powershell
dotnet build src\MaestroMauiDemo\MaestroMauiDemo.csproj -f net10.0-android -c Debug --no-restore
```

### iOS (on a Mac)

```bash
dotnet build src/MaestroMauiDemo/MaestroMauiDemo.csproj -f net10.0-ios -c Debug \
  -p:MauiAppTargetFramework=net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64
```

Use `iossimulator-x64` instead on an Intel Mac. `MauiAppTargetFramework` limits
the app's implicit restore to iOS without changing the Core project's target;
otherwise restore also requires the Android and Mac Catalyst workloads. The
command restores packages for the selected simulator runtime.

### Mac Catalyst (on a Mac)

```bash
dotnet build src/MaestroMauiDemo/MaestroMauiDemo.csproj -f net10.0-maccatalyst -c Debug
```

Apple application builds and simulator runs require macOS and Xcode; a standalone
Windows host cannot fully validate those targets.

## Run

Open [dotnet-maui-maestro-demo.slnx](dotnet-maui-maestro-demo.slnx) in Visual Studio,
select `MaestroMauiDemo` as the startup project, and choose Windows Machine, an
Android device/emulator, or an iOS simulator on a configured Mac.

Alternatively, use the CLI:

```powershell
# Windows
dotnet build src\MaestroMauiDemo\MaestroMauiDemo.csproj -t:Run -f net10.0-windows10.0.19041.0 -c Debug

# Android: start an emulator or connect a device first
dotnet build src\MaestroMauiDemo\MaestroMauiDemo.csproj -t:Run -f net10.0-android -c Debug
```

On an Apple Silicon Mac with an iOS simulator available:

```bash
dotnet build src/MaestroMauiDemo/MaestroMauiDemo.csproj -t:Run -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64
```

## Repository structure

```text
.
|-- .editorconfig
|-- .github/
|   `-- workflows/
|       |-- android-ci.yml
|       `-- android-ui-tests.yml
|-- .gitignore
|-- Directory.Build.props
|-- Directory.Packages.props
|-- NuGet.config
|-- global.json
|-- dotnet-maui-maestro-demo.slnx
|-- LICENSE
|-- README.md
|-- src/
|   |-- MaestroMauiDemo/
|   |   |-- MaestroMauiDemo.csproj
|   |   |-- App.xaml / App.xaml.cs
|   |   |-- AppShell.xaml / AppShell.xaml.cs
|   |   |-- MauiProgram.cs
|   |   |-- Services/
|   |   |-- Views/
|   |   |-- Resources/
|   |   |-- Platforms/
|   |   `-- Properties/
|   `-- MaestroMauiDemo.Core/
|       |-- MaestroMauiDemo.Core.csproj
|       |-- Models/
|       |-- Services/
|       `-- ViewModels/
`-- tests/
    |-- MaestroMauiDemo.UnitTests/
    |   |-- MaestroMauiDemo.UnitTests.csproj
    |   |-- Services/
    |   |-- ViewModels/
    |   `-- TestDoubles/
    `-- MaestroMauiDemo.UITests/
        |-- config.yaml
        |-- README.md
        |-- flows/
        `-- scripts/
            |-- run-android.ps1
            `-- run-ios.sh
```

- [MaestroMauiDemo.Core](src/MaestroMauiDemo.Core/) is a `net10.0` class library
  containing the models, task service, navigation contract and routes, and view
  models. It uses CommunityToolkit.Mvvm, but has no MAUI or platform dependencies.
  The app and unit-test project each reference Core; Core references neither.
- [TodoService](src/MaestroMauiDemo.Core/Services/TodoService.cs) owns the seeded task
  collection and validates additions. Its asynchronous API currently returns
  completed tasks without artificial delays.
- [TodoItem](src/MaestroMauiDemo.Core/Models/TodoItem.cs) has an immutable ID and title,
  with observable completion state managed by the service.
- [ViewModels](src/MaestroMauiDemo.Core/ViewModels/) coordinate the service, validation,
  and navigation. The task list reloads when it appears, including after a save.
  Navigation is injected through
  [INavigationService](src/MaestroMauiDemo.Core/Services/INavigationService.cs);
  [ShellNavigationService](src/MaestroMauiDemo/Services/ShellNavigationService.cs)
  forwards production requests to Shell using the existing routes.
- [Views](src/MaestroMauiDemo/Views/) use bindings and standard MAUI controls.
  Task-list code-behind only bridges page appearance and checkbox events to
  view-model commands; a checkbox's binding refresh is not treated as user input.
  Row commands use a compiled `x:Reference` binding to the page's typed, read-only
  `ViewModel`, initialized before XAML. This avoids a transient null ancestor
  binding context when Shell navigation refreshes or recycles task rows.
- [MauiProgram](src/MaestroMauiDemo/MauiProgram.cs) registers the task service and
  task-list screen as singletons. Each add-task screen receives a fresh page and
  view model through Shell's dependency injection support.
- [Resources](src/MaestroMauiDemo/Resources/) contain semantic light/dark colors,
  styles, the standard MAUI icon and splash screen, Open Sans fonts, and the
  empty-state illustration. `Resources/Raw` is reserved by the standard MAUI asset
  glob but contains no unused placeholder assets.

Keep one class, record, struct, enum, or interface per C# file. Use English naming
and visible text. Editor conventions are defined in [.editorconfig](.editorconfig).

## Accessibility and automation

Interactive controls have stable `AutomationId` values separate from their
user-facing labels and semantic descriptions:

| Control | Automation ID |
| --- | --- |
| Add task | `AddTaskButton` |
| Task title input | `TaskTitleEntry` |
| Save | `SaveTaskButton` |
| Cancel | `CancelTaskButton` |
| Completion checkbox | `CompleteTaskCheckBox` |
| Delete action | `DeleteTaskButton` |
| Task title | `TaskTitleLabel` |
| Task list | `TaskList` |
| Empty state | `EmptyStateLabel` |
| Validation message | `ValidationMessageLabel` |

Task rows share the same control IDs; Maestro flows combine the completion and
delete IDs with task-specific accessibility descriptions. Accessibility descriptions
include the task title, while automation IDs do not depend on visible text.
Buttons and checkboxes have a minimum 48-unit touch target, task titles wrap, and
content width is bounded on larger screens.

No Maestro-specific application code or package dependencies are present.
The external CLI uses the existing native accessibility surface.

## Unit tests

[MaestroMauiDemo.UnitTests](tests/MaestroMauiDemo.UnitTests/) targets only `net10.0`
and references [MaestroMauiDemo.Core](src/MaestroMauiDemo.Core/) directly, not the
MAUI app. The test dependency graph contains no pages, platform startup code,
Shell implementation, or MAUI packages. The app no longer needs a special
library target or conditional source exclusions for testing.

Tests use xUnit with `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`.
Versions are managed centrally. The test project opts into VSTest rather than
xUnit's default Microsoft.Testing.Platform integration, preserving the standard
`dotnet test` workflow. Handwritten service and navigation doubles keep tests
isolated; no mocking framework or MAUI application host is initialized.

Coverage includes seeded tasks, addition and trimming, invalid titles, unique
IDs, completion and change notifications, deletion and unknown IDs; list loading
and refresh; save/cancel validation and navigation; and command/busy-state
recovery after pending operations and failures.

From the root with the application toolchain installed:

```powershell
dotnet test
```

For a machine with only the .NET SDK, run the unit-test project directly:

```powershell
dotnet test tests\MaestroMauiDemo.UnitTests\MaestroMauiDemo.UnitTests.csproj
```

This restores and runs the tests without MAUI workloads, emulators, simulators,
dispatchers, native UI controls, or special framework properties. The root
solution command still restores and builds the application's platform targets,
so it uses the app prerequisites.

## Pull-request checks for Android

[Android CI](.github/workflows/android-ci.yml) runs on GitHub-hosted Ubuntu 24.04
for pull requests targeting `main`. Creating, reopening, or updating a PR starts
the check; newer commits cancel an older run for the same PR.

The `Android build and unit tests` job:

1. Installs the .NET SDK pinned in [global.json](global.json).
2. Restores and runs the Core unit-test project in Release. These are ordinary
   .NET tests on the runner, not tests executing on an Android device.
3. Uploads the VSTest TRX report as `unit-test-results`, also when tests fail.
   The artifact is retained for seven days.
4. Sets up Temurin JDK 21 and the MAUI Android workload, installs the Android SDK
   components required by the project, and builds Android Debug.

Failed tests or a failed build fail the PR check. The job has a 30-minute timeout,
read-only repository permissions, and official actions referenced by major-version
tags. The SDK is installed in the runner's temporary directory so workload
installation does not need administrator privileges.

This workflow does not deploy the app, start an emulator, or run Maestro/iOS/UI
tests.

## Pull-request checks for iOS

[iOS CI](.github/workflows/ios-ci.yml) runs for pull requests targeting `main`
on the Apple Silicon `xcode-27` GitHub-hosted image. Like the Android check, it
runs the Core unit tests in Release, uploads their TRX report for seven days,
and cancels older runs for the same PR. Its artifact is named
`ios-unit-test-results` to avoid a collision with the Android report.

The job selects Xcode 27.1, installs the MAUI iOS workload from the explicitly
selected `10.0.403` workload set, and builds the app for `iossimulator-arm64`
in Debug. It does not boot a simulator, run Maestro, sign a device build, or
deploy an app. A simulator build does not require an Apple signing certificate
or provisioning profile.

The `10.0.403` workload set uses Microsoft.iOS 27.1 and requires Xcode 27.1.
The SDK version in `global.json` alone does not pin the workload set; the
workflow passes it explicitly to `dotnet workload install` and logs the
installed workloads. The `xcode-27` image is used because the standard
`macos-26` image does not currently provide that Xcode version. If the SDK
workload set or runner image changes, update the selected Xcode version
accordingly. A successful native GitHub-hosted iOS build is still needed.
macOS runner usage can consume more GitHub Actions minutes or incur different
charges than Linux; check the current billing terms for your repository.

## Maestro UI tests

[MaestroMauiDemo.UITests](tests/MaestroMauiDemo.UITests/README.md) contains five
local Maestro CLI flows for startup, adding, title validation, completion, and
deletion. The YAML workspace is shared by Android and iOS; it is not a .NET
project and adds no application or unit-test dependencies.

### Manual GitHub Actions run (Android)

[Android Maestro UI tests](.github/workflows/android-ui-tests.yml) is a separate
`workflow_dispatch`-only workflow. It never runs automatically on PRs, pushes, or
a schedule; the Android PR unit-test/build check remains unchanged.

Once the workflow is present on the default branch (`main`), open **Actions**,
select **Android Maestro UI tests**, choose **Run workflow**, select the branch
to test, and start the run.

It builds a fresh signed `android-x64` Debug APK with embedded assemblies, starts
an API-35 Google APIs x86_64 emulator on Ubuntu 24.04, and runs all five existing
flows with the project-evaluated app ID. Both platform workflows use the same
Maestro CLI release and ZIP SHA-256 pin from
[maestro-cli-release.txt](tests/MaestroMauiDemo.UITests/maestro-cli-release.txt),
currently 2.12.1. Actions use major-version tags.

Failed tests fail the run. JUnit results, Maestro artifacts, emulator logcat, and
run metadata are uploaded as `maestro-ui-results`, including after test failures,
and retained for seven days. The emulator action shuts down its emulator.
The job timeout is 45 minutes; the emulator/test step is limited to 20 minutes.
KVM permissions are configured only on the temporary GitHub-hosted runner.
No Maestro Cloud, unit tests, or iOS tests are executed by this workflow.

The maintainer has reported a successful hosted Android pipeline run. The local
Windows verification used API 33 and is separate from the hosted API-35 run.

### Manual GitHub Actions run (iOS)

[iOS Maestro UI tests](.github/workflows/ios-ui-tests.yml) is also
`workflow_dispatch`-only. Once the workflow is on the default branch, select
**Actions > iOS Maestro UI tests > Run workflow** and choose the branch.

It uses the `xcode-27` Apple Silicon runner, Xcode 27.1, Java 21, the MAUI iOS
workload, and the same checksum-verified Maestro 2.12.1 release as Android.
The job builds a fresh `iossimulator-arm64` Debug `.app`, checks its bundle ID,
creates an iPhone 17 with the installed iOS 27.0 simulator runtime, installs the
app, and runs all five shared flows.

The job uploads `ios-maestro-ui-results` for seven days, including JUnit and
Maestro artifacts when available, simulator diagnostics, and run metadata.
Diagnostics are attempted after test failures, and cleanup attempts to shut down
and delete only the simulator the job created. Simulator creation/boot and tests have separate
timeouts. No Apple signing credentials, physical devices, or Maestro Cloud are
used. The first hosted iOS run is still required to validate simulator boot,
packaging, accessibility selectors, keyboard behavior, and the flows.

### Local runners

With native Maestro and the Android prerequisites configured, use the Windows
PowerShell helper:

```powershell
.\tests\MaestroMauiDemo.UITests\scripts\run-android.ps1
```

This starts an existing AVD if needed, builds Android Debug, installs a fresh
signed APK, and runs all five flows. Several available AVDs produce an interactive
selection; `-AvdName` selects one directly. The helper waits for boot and shuts
down only an emulator it started itself, leaving pre-existing emulators running.
`-Flow smoke.yaml` selects one flow; `-SkipBuild` skips both build and installation.

On macOS, with compatible Xcode/.NET MAUI tooling, Java 17+ with a valid
`JAVA_HOME`, and native Maestro configured, use the native Bash helper:

```bash
bash tests/MaestroMauiDemo.UITests/scripts/run-ios.sh
```

`--device-id` selects a simulator directly, `--flow smoke.yaml` selects one flow,
and `--skip-build` skips both build and installation; `--help` lists the options.
Several available iOS Simulators produce a numbered selection; one is selected
automatically. The helper chooses the Apple Silicon or Intel simulator build,
including ARM64 when the shell runs through Rosetta, and waits up to 180 seconds
for boot before building/installing a fresh iOS Simulator Debug app and running
the shared flows.

It reuses already booted/booting simulators and shuts down only a simulator it
booted itself, including on failure or a handled interrupt. Earlier native error
codes are preserved if cleanup also fails. Boot logs and fresh build outputs
remain in ignored `TestResults`; no simulator is created or global tool
configuration changed. The iOS runner is prepared but has not been executed on
a Mac.

The CLI can also run directly against an already installed app:

```powershell
maestro --device=emulator-5554 test --env APP_ID=de.tsjdevapps.maestromauidemo `
    --test-output-dir tests\MaestroMauiDemo.UITests\TestResults `
    tests\MaestroMauiDemo.UITests
```

See the [UI-test guide](tests/MaestroMauiDemo.UITests/README.md) for native
installation, device selection, manual Android and macOS/iOS Simulator commands,
state isolation, selectors, and troubleshooting. No WSL or CI/CD is required.

## Verification

Validated on Windows with .NET SDK 10.0.403:

- NuGet restore completed for the SLNX solution containing the application,
  Core library, and unit test project.
- Windows and Android Debug builds succeeded with zero warnings and zero errors,
  including a build with warnings treated as errors.
- iOS and Mac Catalyst managed compilation also succeeded with zero warnings and
  zero errors. This is not a substitute for native Apple builds on a Mac.
- `dotnet test` passed all 69 unit test cases, including a direct test-project run
  with MAUI workload resolution disabled and no framework overrides.
- The Android PR workflow passed `actionlint` and Bash command syntax checks.
  Its Release unit-test command passed all 69 tests and generated a validated
  TRX report. Its Android Debug build command succeeded locally with the existing
  Microsoft JDK 17 and the known `XA0033` version-format warning; this does not
  validate the GitHub-hosted Ubuntu/Temurin JDK 21 environment.
- Live Windows checks verified startup, the three seeded tasks, a fresh add-task
  form after canceling, blank-title validation, trimming and saving, completion
  toggling, deletion, and the accessible empty state and recovery from it.

- An earlier local run with Maestro 2.11.0 passed all five Android flows on an
  API-33 x86_64 emulator, both as a suite and independently in a different order.
- The manual Android Maestro workflow passed `actionlint`, syntax checks for all
  six shell blocks, and nine isolated fresh-APK/error-handling cases. A new native
  Windows run built/installed an embedded-assembly x64 APK and produced five
  passing JUnit cases with zero failures; its owned emulator was shut down.
  This does not validate the GitHub-hosted KVM/Linux integration.
- The PowerShell runner passed parsing, `-SkipBuild` execution, and default
  build/install/suite execution from outside the repository. Its explicit Java
  build configuration produced one unsuppressed tooling warning (`XA0033`) for
  the installed JDK's five-component version; the APK and tests succeeded.
- Interactive automatic AVD startup passed all five flows and shut down only the
  owned emulator. Direct `-AvdName` startup and reuse of a named running emulator
  also passed; the reused emulator was left running. Thirty isolated fixture
  cases checked startup/selection failures, the boot deadline, and cleanup with
  preserved native error codes.
- The iOS Bash runner passed native Git Bash syntax/help/Windows-host checks and
  39 isolated CLI fixture cases covering selection, simulator reuse and owned
  shutdown, architecture selection, build/install checks, and native failures.
  The 180-second boot deadline was exercised with a real wait and monitor
  cleanup; the complete simulated timeout run took 183 seconds.
- The runner's actual inline simulator JSON parser passed 16 local checks with
  Foundation stubs. Shell scripts use LF line endings enforced by repository
  attributes. These checks do not execute the native macOS Objective-C bridge,
  Xcode, or an iOS Simulator.
- Both iOS workflows passed `actionlint`, except for the newer `xcode-27`
  hosted label that the linter version does not yet recognize. Their shell
  blocks passed syntax checks, and 23 mocked cases exercised build, simulator,
  Maestro, diagnostics, and cleanup paths. The PR workflow's unit-test command
  also passed all 69 tests and wrote a TRX report on Windows.

Native Apple packaging/signing and Apple simulator/device behavior have not been
validated in this environment. Shared iOS flows are provided but have not been
executed on a simulator. Local runners do not provision AVDs. Both PR workflows
cover unit tests and compilation only; the separate platform-specific Maestro
workflows run only when started manually.

## License

The sample is licensed under [MIT](LICENSE). The template's bundled Open Sans
1.10 fonts retain their [Apache 2.0 license](src/MaestroMauiDemo/Resources/Fonts/LICENSE.txt),
which is also included as an application asset.
