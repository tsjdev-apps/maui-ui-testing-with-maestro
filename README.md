# .NET MAUI UI Testing with Maestro

**MAUI Tasks** is a small, in-memory task management application built with .NET MAUI.
It is the starting point for a blog series about automating .NET MAUI applications
with Maestro on Android and iOS.

The repository contains the application and plain .NET unit tests. There are no
Maestro flows, UI tests, integration tests, or CI/CD workflows yet.

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
dotnet build src/MaestroMauiDemo/MaestroMauiDemo.csproj -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64
```

Use `iossimulator-x64` instead on an Intel Mac. The command restores packages for
the selected simulator runtime.

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
    `-- MaestroMauiDemo.UnitTests/
        |-- MaestroMauiDemo.UnitTests.csproj
        |-- Services/
        |-- ViewModels/
        `-- TestDoubles/
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

## Accessibility and future automation

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

Task rows share the same control IDs; a future automation flow should scope a
completion or delete action to the intended task row. Accessibility descriptions
include the task title, while automation IDs do not depend on visible text.
Buttons and checkboxes have a minimum 48-unit touch target, task titles wrap, and
content width is bounded on larger screens.

No Maestro-specific code or dependencies are present. Maestro examples for
Android and iOS will be added in later steps.

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
- Live Windows checks verified startup, the three seeded tasks, a fresh add-task
  form after canceling, blank-title validation, trimming and saving, completion
  toggling, deletion, and the accessible empty state and recovery from it.

Android device/emulator behavior, native Apple packaging/signing, and Apple
simulator/device behavior have not been validated in this environment.
No UI/integration tests, emulator setup, or workflow files are included.

## License

The sample is licensed under [MIT](LICENSE). The template's bundled Open Sans
1.10 fonts retain their [Apache 2.0 license](src/MaestroMauiDemo/Resources/Fonts/LICENSE.txt),
which is also included as an application asset.
