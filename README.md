# .NET MAUI UI Testing with Maestro

**MAUI Tasks** is a small, in-memory task management application built with .NET MAUI.
It is the starting point for a blog series about automating .NET MAUI applications
with Maestro on Android and iOS.

**This initial version contains only the application.** There are no Maestro flows,
test projects, test infrastructure, or CI/CD workflows yet.

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
restore to that platform without modifying the project:

```powershell
dotnet restore dotnet-maui-maestro-demo.slnx -p:TargetFrameworks=net10.0-windows10.0.19041.0
```

## Build

Choose a framework explicitly rather than building every platform on an
incompatible host.

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
`-- src/
    `-- MaestroMauiDemo/
        |-- MaestroMauiDemo.csproj
        |-- App.xaml / App.xaml.cs
        |-- AppShell.xaml / AppShell.xaml.cs
        |-- MauiProgram.cs
        |-- Models/
        |-- Services/
        |-- ViewModels/
        |-- Views/
        |-- Resources/
        |-- Platforms/
        `-- Properties/
```

- [TodoService](src/MaestroMauiDemo/Services/TodoService.cs) owns the seeded task
  collection and validates additions. Its asynchronous API currently returns
  completed tasks without artificial delays.
- [TodoItem](src/MaestroMauiDemo/Models/TodoItem.cs) has an immutable ID and title,
  with observable completion state managed by the service.
- [ViewModels](src/MaestroMauiDemo/ViewModels/) coordinate the service, validation,
  and navigation. The task list reloads when it appears, including after a save.
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

## Verification of this initial version

Validated on Windows with .NET SDK 10.0.403:

- NuGet restore completed for the solution, which references only the application.
- Windows and Android Debug builds succeeded with zero warnings and zero errors,
  including a build with warnings treated as errors.
- iOS and Mac Catalyst managed compilation also succeeded with zero warnings and
  zero errors. This is not a substitute for native Apple builds on a Mac.
- Live Windows checks verified startup, the three seeded tasks, a fresh add-task
  form after canceling, blank-title validation, trimming and saving, completion
  toggling, deletion, and the accessible empty state and recovery from it.

Android device/emulator behavior, native Apple packaging/signing, and Apple
simulator/device behavior have not been validated in this environment.
These checks did not add any test projects, scripts, or workflows to the repository.

## License

The sample is licensed under [MIT](LICENSE). The template's bundled Open Sans
1.10 fonts retain their [Apache 2.0 license](src/MaestroMauiDemo/Resources/Fonts/LICENSE.txt),
which is also included as an application asset.
