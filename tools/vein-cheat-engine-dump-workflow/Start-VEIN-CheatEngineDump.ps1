param(
    [string]$CheatEnginePath = $env:CHEAT_ENGINE_PATH,
    [ValidateSet('Idle', 'BelowNormal', 'Normal')]
    [string]$Priority = 'BelowNormal',
    [ValidateSet(1, 2)]
    [int]$CpuThreads = 1,
    [switch]$Headless
)

$ErrorActionPreference = 'Stop'

function Resolve-CheatEnginePath {
    param([string]$PathFromUser)

    if ($PathFromUser -and (Test-Path -LiteralPath $PathFromUser -PathType Leaf)) {
        return (Resolve-Path -LiteralPath $PathFromUser).Path
    }

    $candidates = @(
        'C:\Program Files\Cheat Engine\cheatengine-x86_64-SSE4-AVX2.exe',
        'C:\Program Files\Cheat Engine\cheatengine-x86_64.exe',
        'C:\Program Files\Cheat Engine 7.5\cheatengine-x86_64-SSE4-AVX2.exe',
        'C:\Program Files\Cheat Engine 7.5\cheatengine-x86_64.exe',
        'C:\Program Files (x86)\Cheat Engine 7.5\cheatengine-x86_64-SSE4-AVX2.exe',
        'C:\Program Files (x86)\Cheat Engine 7.5\cheatengine-x86_64.exe'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw 'Set CHEAT_ENGINE_PATH or install Cheat Engine in the default folder.'
}

function ConvertTo-AffinityMask {
    param([int]$ThreadCount)
    $mask = 0
    for ($i = 0; $i -lt $ThreadCount; $i++) {
        $mask = $mask -bor (1 -shl $i)
    }
    [IntPtr]$mask
}

function Initialize-WindowHider {
    if ('Win32.VeinWindowHider' -as [type]) {
        return
    }

    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace Win32
{
    public static class VeinWindowHider
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void HideProcessWindows(int processId)
        {
            EnumWindows((hWnd, lParam) =>
            {
                int ownerProcessId;
                GetWindowThreadProcessId(hWnd, out ownerProcessId);
                if (ownerProcessId == processId)
                {
                    ShowWindow(hWnd, 0);
                }

                return true;
            }, IntPtr.Zero);
        }
    }
}
'@
}

function Initialize-HiddenProcessLauncher {
    if ('Win32.VeinHiddenProcessLauncher' -as [type]) {
        return
    }

    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Win32
{
    public static class VeinHiddenProcessLauncher
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcessW(
            string lpApplicationName,
            StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public static int StartHidden(string fileName, string arguments, string workingDirectory)
        {
            STARTUPINFO startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(typeof(STARTUPINFO));
            startupInfo.dwFlags = 0x00000001;
            startupInfo.wShowWindow = 0;

            PROCESS_INFORMATION processInfo;
            string commandLine = "\"" + fileName + "\"";
            if (!String.IsNullOrWhiteSpace(arguments))
            {
                commandLine += " " + arguments;
            }

            bool created = CreateProcessW(
                fileName,
                new StringBuilder(commandLine),
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0x08000000,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out processInfo);

            if (!created)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            CloseHandle(processInfo.hThread);
            CloseHandle(processInfo.hProcess);
            return processInfo.dwProcessId;
        }
    }
}
'@
}

function Hide-ProcessWindows {
    param([System.Diagnostics.Process]$Process)

    if (-not $Headless -or -not $Process -or $Process.HasExited) {
        return
    }

    Initialize-WindowHider
    [Win32.VeinWindowHider]::HideProcessWindows($Process.Id)
}

function Start-CheatEngineProcess {
    param(
        [string]$FilePath,
        [string]$Arguments,
        [string]$WorkingDirectory
    )

    if ($Headless) {
        Initialize-HiddenProcessLauncher
        $processId = [Win32.VeinHiddenProcessLauncher]::StartHidden($FilePath, $Arguments, $WorkingDirectory)
        return [System.Diagnostics.Process]::GetProcessById($processId)
    }

    return Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -WindowStyle Minimized -WorkingDirectory $WorkingDirectory
}

$root = Split-Path -Parent $PSCommandPath
$masterScript = Join-Path $root 'VEIN_MasterDump_Throttled.lua'
$autorunTargetDir = 'C:\Program Files\Cheat Engine\autorun\custom'
$autorunTarget = Join-Path $autorunTargetDir 'VEIN_RunMasterDump_OneShot.lua'
$desktop = [Environment]::GetFolderPath('Desktop')
$controlDir = Join-Path $desktop 'VEIN_Dump_Control'
$statusPath = Join-Path $controlDir 'status.txt'
$downloadUrl = 'https://www.cheatengine.org/downloads.php'

if (-not (Test-Path -LiteralPath $masterScript)) {
    throw "Missing Cheat Engine Lua dump script: $masterScript"
}

New-Item -ItemType Directory -Force -Path $controlDir | Out-Null
Remove-Item -LiteralPath (Join-Path $controlDir 'pause.flag') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $controlDir 'stop.flag') -Force -ErrorAction SilentlyContinue
Set-Content -LiteralPath $statusPath -Value ("starting " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8

try {
    $resolvedPath = Resolve-CheatEnginePath -PathFromUser $CheatEnginePath
} catch {
    Set-Content -LiteralPath $statusPath -Value ("Cheat Engine missing - opening official download page " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8
    Start-Process -FilePath $downloadUrl
    exit 1
}

New-Item -ItemType Directory -Force -Path $autorunTargetDir | Out-Null
$outputDir = Join-Path $desktop ("VEIN_MasterDump_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$env:VEIN_CE_OUTPUT_DIR = $outputDir

$autorunContent = @"
local script_path = [[$masterScript]]
local autorun_path = [[$autorunTarget]]
local log_path = (os.getenv("USERPROFILE") or "C:\\Users\\Public") .. [[\Desktop\VEIN_CE_OneShot_Log.txt]]

local function append_log(message)
    local handle = io.open(log_path, "ab")
    if handle then
        handle:write(os.date("[%Y-%m-%d %H:%M:%S] ") .. tostring(message) .. "\r\n")
        handle:close()
    end
end

append_log("VEIN one-shot autorun loaded.")

local timer = createTimer(nil, false)
timer.Interval = 2000
timer.OnTimer = function(sender)
    sender.destroy()
    os.remove(autorun_path)
    append_log("Removed one-shot autorun hook.")

    local ok, err = xpcall(function()
        dofile(script_path)
    end, debug.traceback)

    if ok then
        append_log("VEIN master dump completed.")
    else
        append_log("VEIN master dump failed: " .. tostring(err))
    end

    pcall(closeCE)
end
timer.Enabled = true
"@
Set-Content -LiteralPath $autorunTarget -Value $autorunContent -Encoding UTF8

$affinity = ConvertTo-AffinityMask -ThreadCount $CpuThreads
$windowStyle = if ($Headless) { 'Hidden' } else { 'Minimized' }
$arguments = if ($Headless) { '-nosplash' } else { '' }
if ($Headless) {
    Initialize-WindowHider
}

$process = Start-CheatEngineProcess -FilePath $resolvedPath -Arguments $arguments -WorkingDirectory (Split-Path -Parent $resolvedPath)
for ($i = 0; $i -lt 240; $i++) {
    Start-Sleep -Milliseconds 25
    try {
        $process.Refresh()
        Hide-ProcessWindows -Process $process
        if ($i -eq 8) {
            $process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::$Priority
            $process.ProcessorAffinity = $affinity
        }
    } catch {
        # CE may still be starting or already exiting after the one-shot dump.
    }

    if ($process.HasExited) {
        break
    }
}

Set-Content -LiteralPath $statusPath -Value ("running pid=$($process.Id) " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8
