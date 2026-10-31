[CmdletBinding()]
param(
    [string]$DeviceId,
    [string]$Flow,
    [switch]$SkipBuild,
    [string]$AvdName
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$exitCode = 1
$startedEmulatorProcess = $null
$startedEmulatorDeviceId = $null
$ownedConsoleConfirmed = $false

function Get-RequiredApplication {
    param([string]$Name, [string]$FailureMessage)

    $application = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $application) {
        throw $FailureMessage
    }

    return $application.Source
}

function Invoke-CheckedCommand {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [string]$FailureMessage,
        [switch]$CaptureOutput
    )

    if ($CaptureOutput) {
        $output = @(& $Command @Arguments)
    }
    else {
        & $Command @Arguments | Out-Host
    }

    if ($LASTEXITCODE -ne 0) {
        $script:exitCode = $LASTEXITCODE
        if ($CaptureOutput) {
            $output | Out-Host
        }
        throw "$FailureMessage (exit code $script:exitCode)."
    }

    if ($CaptureOutput) {
        return $output
    }
}

function Wait-EmulatorBoot {
    param([string]$Adb, [string]$Serial, [Diagnostics.Process]$Process)

    Write-Host "Waiting for emulator '$Serial' to finish booting (up to 180 seconds)..."
    $deadline = [DateTime]::UtcNow.AddSeconds(180)
    do {
        if ($null -ne $Process) {
            $Process.Refresh()
            if ($Process.HasExited) {
                if ($Process.ExitCode -ne 0) {
                    $script:exitCode = $Process.ExitCode
                }
                throw "The emulator launcher exited with code $($Process.ExitCode) before boot completed. Check the emulator logs."
            }
        }
        $devices = (Invoke-CheckedCommand $Adb @('devices', '-l') 'ADB device discovery failed while waiting for boot' -CaptureOutput) -join "`n"
        if ($devices -match "(?m)^$([regex]::Escape($Serial))\s+device(?:\s|$)") {
            $bootState = (Invoke-CheckedCommand $Adb @(
                '-s', $Serial, 'shell', 'getprop', 'sys.boot_completed'
            ) 'Could not query emulator boot state' -CaptureOutput) -join ''
            if ($bootState.Trim() -eq '1') {
                return
            }
        }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Emulator '$Serial' did not finish booting within 180 seconds. Check its window and the emulator logs."
}

function Stop-EmulatorProcessTree {
    param([Diagnostics.Process]$Process)

    $Process.Refresh()
    if ($Process.HasExited) {
        return
    }
    foreach ($child in Get-CimInstance Win32_Process -Filter "ParentProcessId=$($Process.Id)") {
        $childProcess = Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
        if ($null -eq $childProcess) {
            Write-Verbose "Emulator child process $($child.ProcessId) has already exited."
            continue
        }
        if ($childProcess.StartTime -lt $Process.StartTime) {
            throw "Cannot confirm ownership of emulator child process $($child.ProcessId)."
        }
        Stop-EmulatorProcessTree $childProcess
    }
    $Process.Refresh()
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -ErrorAction Stop
    }
}

$workspace = Split-Path $PSScriptRoot -Parent
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$project = Join-Path $repositoryRoot 'src\MaestroMauiDemo\MaestroMauiDemo.csproj'
Push-Location $repositoryRoot
try {
    Write-Host 'Maestro .NET MAUI Android UI Tests'
    if (-not [string]::IsNullOrWhiteSpace($DeviceId) -and -not [string]::IsNullOrWhiteSpace($AvdName)) {
        throw '-DeviceId and -AvdName cannot be combined. Use -DeviceId for an existing emulator or -AvdName to select an AVD.'
    }
    Write-Host 'Checking prerequisites...'
    $dotnet = Get-RequiredApplication 'dotnet' 'The .NET SDK was not found. Install the SDK specified in global.json and the MAUI Android workload.'
    $maestro = Get-RequiredApplication 'maestro' 'The native Maestro CLI was not found in PATH. Install Maestro for Windows and add its bin directory to your user PATH.'

    if ([string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
        throw 'JAVA_HOME is missing. Set it to your Java 17+ JDK directory before running this script.'
    }
    $java = Join-Path $env:JAVA_HOME 'bin\java.exe'
    if (-not (Test-Path -LiteralPath $java -PathType Leaf)) {
        throw "JAVA_HOME points to an invalid JDK: '$env:JAVA_HOME'. Its bin\java.exe does not exist."
    }
    $javaVersion = (Invoke-CheckedCommand $java @('--version') 'Java could not start. Use Java 17 or newer' -CaptureOutput) -join "`n"
    if ($javaVersion -notmatch '(?:openjdk|java)\s+(\d+)' -or [int]$Matches[1] -lt 17) {
        throw "Java 17 or newer is required. JAVA_HOME reports: $javaVersion"
    }
    Write-Host ($javaVersion -split "`n")[0]

    $sdk = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_ -PathType Container) } |
        Select-Object -First 1
    if ($null -eq $sdk) {
        throw 'The Android SDK was not found. Set ANDROID_HOME (or ANDROID_SDK_ROOT) to the installed SDK directory.'
    }
    $adb = Join-Path $sdk 'platform-tools\adb.exe'
    if (-not (Test-Path -LiteralPath $adb -PathType Leaf)) {
        throw "ADB was not found at '$adb'. Install Android SDK Platform-Tools."
    }
    $emulatorExe = Join-Path $sdk 'emulator\emulator.exe'
    if (-not (Test-Path -LiteralPath $emulatorExe -PathType Leaf)) {
        throw "The Android Emulator was not found in '$sdk'. Install it through the Android SDK tools."
    }

    $sdkVersion = Invoke-CheckedCommand $dotnet @('--version') 'The repository-pinned .NET SDK is unavailable' -CaptureOutput
    Write-Host ".NET SDK: $sdkVersion"
    $maestroVersion = Invoke-CheckedCommand $maestro @('--version') 'The native Maestro CLI could not start; check Java and the Maestro installation' -CaptureOutput
    Write-Host "Maestro CLI: $maestroVersion"
    Invoke-CheckedCommand $adb @('version') 'ADB could not start'

    $flowTarget = $workspace
    if (-not [string]::IsNullOrWhiteSpace($Flow)) {
        if ([IO.Path]::GetFileName($Flow) -ne $Flow -or [IO.Path]::GetExtension($Flow) -ne '.yaml') {
            throw '-Flow must be a YAML file name from the flows directory, such as smoke.yaml.'
        }
        $flowTarget = Join-Path (Join-Path $workspace 'flows') $Flow
        if (-not (Test-Path -LiteralPath $flowTarget -PathType Leaf)) {
            throw "The requested flow does not exist: '$Flow'."
        }
    }

    $deviceList = (Invoke-CheckedCommand $adb @('devices', '-l') 'ADB device discovery failed' -CaptureOutput) -join "`n"
    $onlineEmulators = @([regex]::Matches($deviceList, '(?m)^(emulator-\d+)\s+device(?:\s|$)') |
        ForEach-Object { $_.Groups[1].Value })
    $bootedEmulators = @(foreach ($emulator in $onlineEmulators) {
        $bootState = (Invoke-CheckedCommand $adb @('-s', $emulator, 'shell', 'getprop', 'sys.boot_completed') 'Could not query emulator boot state' -CaptureOutput) -join ''
        if ($bootState.Trim() -eq '1') {
            $emulator
        }
    })
    if (-not [string]::IsNullOrWhiteSpace($AvdName)) {
        $matchingEmulators = @(foreach ($serial in $onlineEmulators) {
            $nameOutput = @(Invoke-CheckedCommand $adb @(
                '-s', $serial, 'emu', 'avd', 'name'
            ) 'Could not identify the running AVD' -CaptureOutput)
            if ($nameOutput.Count -gt 0 -and $nameOutput[0].Trim() -ceq $AvdName) {
                $serial
            }
        })
        if ($matchingEmulators.Count -gt 1) {
            throw "Several instances of AVD '$AvdName' are running: $($matchingEmulators -join ', '). Select one with -DeviceId instead."
        }
        if ($matchingEmulators.Count -eq 1) {
            $DeviceId = $matchingEmulators[0]
        }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($DeviceId)) {
        if ($DeviceId -notin $onlineEmulators) {
            throw "Emulator '$DeviceId' is not online. Check adb devices -l. Physical, offline, and unauthorized devices are not supported."
        }
    }
    elseif ($bootedEmulators.Count -eq 1) {
        $DeviceId = $bootedEmulators[0]
    }
    elseif ($bootedEmulators.Count -gt 1) {
        throw "Multiple booted emulators found: $($bootedEmulators -join ', '). Select one with -DeviceId."
    }
    elseif ($onlineEmulators.Count -eq 1) {
        $DeviceId = $onlineEmulators[0]
    }
    elseif ($onlineEmulators.Count -gt 1) {
        throw "Multiple booting emulators found: $($onlineEmulators -join ', '). Select one with -DeviceId."
    }

    if ([string]::IsNullOrWhiteSpace($DeviceId)) {
        $avds = @(Invoke-CheckedCommand $emulatorExe @('-list-avds') 'Could not list existing AVDs' -CaptureOutput |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($avds.Count -eq 0) {
            throw 'No Android Virtual Device exists. Create an AVD in Android Device Manager or Android Studio, then retry. This script does not create AVDs.'
        }
        if (-not [string]::IsNullOrWhiteSpace($AvdName)) {
            if ($AvdName -cnotin $avds) {
                throw "AVD '$AvdName' does not exist. Available AVDs: $($avds -join ', ')."
            }
        }
        elseif ($avds.Count -eq 1) {
            $AvdName = $avds[0]
        }
        else {
            Write-Host 'Available Android Virtual Devices:'
            for ($index = 0; $index -lt $avds.Count; $index++) {
                Write-Host "  $($index + 1). $($avds[$index])"
            }
            $selection = Read-Host "Select an AVD [1-$($avds.Count)] (use -AvdName to skip this prompt)"
            $selectedIndex = 0
            if (-not [int]::TryParse($selection, [ref]$selectedIndex) -or $selectedIndex -lt 1 -or $selectedIndex -gt $avds.Count) {
                throw 'Invalid AVD selection. Enter a listed number or pass -AvdName.'
            }
            $AvdName = $avds[$selectedIndex - 1]
        }

        $listeningPorts = @([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() |
            Select-Object -ExpandProperty Port)
        $availablePorts = @(5554..5584 | Where-Object {
            $_ % 2 -eq 0 -and $_ -notin $listeningPorts -and ($_ + 1) -notin $listeningPorts -and
            $deviceList -notmatch "(?m)^emulator-$_\s"
        })
        if ($availablePorts.Count -eq 0) {
            throw 'No free emulator console/ADB port pair is available between 5554 and 5585. Close an unused emulator and retry.'
        }
        $port = $availablePorts[0]
        $DeviceId = "emulator-$port"
        $emulatorLogs = Join-Path $workspace ("TestResults\emulator\" + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $emulatorLogs -Force | Out-Null
        Write-Host "Starting AVD '$AvdName' on '$DeviceId'..."
        Write-Host "Emulator logs: $emulatorLogs"
        $startedEmulatorProcess = Start-Process -FilePath $emulatorExe -ArgumentList @(
            '-avd', ('"{0}"' -f $AvdName.Replace('"', '\"')), '-port', "$port"
        ) -RedirectStandardOutput (Join-Path $emulatorLogs 'stdout.log') `
            -RedirectStandardError (Join-Path $emulatorLogs 'stderr.log') -PassThru
        $startedEmulatorDeviceId = $DeviceId
    }

    Wait-EmulatorBoot $adb $DeviceId $startedEmulatorProcess
    if ($null -ne $startedEmulatorProcess) {
        $startedEmulatorProcess.Refresh()
        if ($startedEmulatorProcess.HasExited) {
            throw 'The emulator launcher exited before its ownership could be verified.'
        }
        $ownedProcessIds = @($startedEmulatorProcess.Id) + @(Get-CimInstance Win32_Process `
            -Filter "ParentProcessId=$($startedEmulatorProcess.Id)" | Select-Object -ExpandProperty ProcessId)
        $consoleOwners = @(Get-NetTCPConnection -State Listen -ErrorAction Stop |
            Where-Object LocalPort -eq $port | Select-Object -ExpandProperty OwningProcess)
        $ownedConsoleConfirmed = @($consoleOwners | Where-Object { $_ -in $ownedProcessIds }).Count -gt 0
        if (-not $ownedConsoleConfirmed) {
            throw "The console port for '$DeviceId' does not belong to the emulator started by this script. No tests will run against it."
        }
    }
    Write-Host "Android emulator: $DeviceId"

    $frameworks = Invoke-CheckedCommand $dotnet @(
        'msbuild', $project, '-nologo', '-verbosity:quiet', '-getProperty:TargetFrameworks'
    ) 'Could not evaluate the MAUI project target frameworks' -CaptureOutput
    $androidFrameworks = @(($frameworks -join '').Trim().Split(';') | Where-Object { $_ -match '^net\d+\.\d+-android(?:\d.*)?$' })
    if ($androidFrameworks.Count -ne 1) {
        throw "Expected exactly one Android target framework in the MAUI project, but found $($androidFrameworks.Count)."
    }
    $framework = $androidFrameworks[0]
    $appId = ((Invoke-CheckedCommand $dotnet @(
        'msbuild', $project, '-nologo', '-verbosity:quiet', "-p:TargetFramework=$framework",
        '-p:Configuration=Debug', '-getProperty:ApplicationId'
    ) 'Could not evaluate the Android ApplicationId' -CaptureOutput) -join '').Trim()
    if ([string]::IsNullOrWhiteSpace($appId)) {
        throw 'The MAUI project does not define an Android ApplicationId.'
    }
    Write-Host "Android target: $framework; application ID: $appId"

    if (-not $SkipBuild) {
        $abi = ((Invoke-CheckedCommand $adb @(
            '-s', $DeviceId, 'shell', 'getprop', 'ro.product.cpu.abi'
        ) 'Could not determine the emulator CPU architecture' -CaptureOutput) -join '').Trim()
        $runtime = switch ($abi) {
            'x86_64' { 'android-x64' }
            'arm64-v8a' { 'android-arm64' }
            default { throw "Unsupported emulator CPU architecture '$abi'. Use an x86_64 or arm64-v8a AVD." }
        }
        $buildDirectory = Join-Path $workspace ("TestResults\build\" + [Guid]::NewGuid().ToString('N'))
        Write-Host "Building Android Debug APK ($runtime)..."
        Invoke-CheckedCommand $dotnet @(
            'build', $project, '-f', $framework, '-c', 'Debug', "-p:RuntimeIdentifier=$runtime",
            '-p:AndroidPackageFormats=apk', '-p:EmbedAssembliesIntoApk=true',
            "-p:AndroidSdkDirectory=$sdk", "-p:JavaSdkDirectory=$env:JAVA_HOME",
            "-p:OutputPath=$buildDirectory", '--verbosity', 'minimal'
        ) 'Android build failed; check the installed SDK, workload, and build output'

        $apks = @(Get-ChildItem -LiteralPath $buildDirectory -Filter '*-Signed.apk' -File -Recurse)
        if ($apks.Count -ne 1) {
            throw "Expected one fresh signed APK in '$buildDirectory', but found $($apks.Count). No APK will be installed."
        }
        Write-Host "Installing APK: $($apks[0].FullName)"
        Invoke-CheckedCommand $adb @('-s', $DeviceId, 'install', '-r', $apks[0].FullName) 'APK installation failed'
    }
    else {
        Write-Host 'Skipping both build and installation; using the already installed app.'
    }

    $packages = Invoke-CheckedCommand $adb @('-s', $DeviceId, 'shell', 'pm', 'list', 'packages', $appId) 'Could not check installed applications' -CaptureOutput
    if ("package:$appId" -notin $packages) {
        throw "Application '$appId' is not installed on '$DeviceId'. Run this script without -SkipBuild first."
    }

    $resultsDirectory = Join-Path $workspace 'TestResults'
    Write-Host "Running Maestro UI tests: $flowTarget"
    Invoke-CheckedCommand $maestro @(
        "--device=$DeviceId", '--platform=android', 'test', '--env', "APP_ID=$appId",
        '--test-output-dir', $resultsDirectory, $flowTarget
    ) 'Maestro UI tests failed'
    $exitCode = 0
}
catch {
    [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
}
finally {
    if ($null -ne $startedEmulatorProcess) {
        $testExitCode = $exitCode
        try {
            $startedEmulatorProcess.Refresh()
            if (-not $startedEmulatorProcess.HasExited) {
                Write-Host "Stopping emulator started by this script: $startedEmulatorDeviceId"
                if ($ownedConsoleConfirmed) {
                    Invoke-CheckedCommand $adb @('-s', $startedEmulatorDeviceId, 'emu', 'kill') 'Owned emulator shutdown failed'
                }
                else {
                    Stop-EmulatorProcessTree $startedEmulatorProcess
                }
                if (-not $startedEmulatorProcess.WaitForExit(15000)) {
                    throw 'The owned emulator did not stop within 15 seconds.'
                }
            }
        }
        catch {
            [Console]::Error.WriteLine("ERROR: Emulator cleanup failed: $($_.Exception.Message)")
            if ($testExitCode -ne 0) {
                $exitCode = $testExitCode
            }
            elseif ($exitCode -eq 0) {
                $exitCode = 1
            }
            try {
                Stop-EmulatorProcessTree $startedEmulatorProcess
            }
            catch {
                [Console]::Error.WriteLine("ERROR: Could not stop the owned emulator process tree: $($_.Exception.Message)")
            }
        }
    }
    Pop-Location
}

if ($exitCode -eq 0) {
    Write-Host 'Maestro UI tests passed.'
}
exit $exitCode
