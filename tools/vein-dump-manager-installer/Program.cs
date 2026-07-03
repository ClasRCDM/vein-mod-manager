using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace VeinDumpManager.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.Exit(InstallerSelfTest.Run());
        }

        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--install-smoke-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.Exit(InstallerSelfTest.RunInstallSmokeTest());
        }

        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--render-screenshot", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.Exit(InstallerSelfTest.RenderScreenshot());
        }

        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private const string AppName = "Vein Dump Manager";

    private static readonly Color Back = Color.FromArgb(4, 9, 16);
    private static readonly Color Panel = Color.FromArgb(8, 16, 30);
    private static readonly Color Border = Color.FromArgb(32, 68, 112);
    private static readonly Color TextMain = Color.White;
    private static readonly Color TextMuted = Color.FromArgb(188, 215, 250);
    private static readonly Color Red = Color.FromArgb(196, 17, 48);
    private static readonly Color Green = Color.FromArgb(31, 218, 125);
    private static readonly Color Blue = Color.FromArgb(55, 145, 255);
    private static readonly Color Amber = Color.FromArgb(246, 176, 45);
    private static readonly string[] SpinnerFrames = ["\u280b", "\u2819", "\u2839", "\u2838", "\u283c", "\u2834", "\u2826", "\u2827", "\u2807", "\u280f"];

    private const int WmNclButtonDown = 0x00A1;
    private const int HtCaption = 0x02;

    private readonly FlowLayoutPanel _checks = new SmoothFlowLayoutPanel();
    private readonly Label _status = new();
    private readonly System.Windows.Forms.Timer _scanTimer = new();
    private readonly Button _installButton;
    private readonly Button _launchButton;
    private readonly RadioButton _recommendedInstall;
    private readonly RadioButton _customInstall;
    private readonly CheckBox _installUe4ssRuntime;
    private readonly CheckBox _setupCheatEngine;
    private readonly CheckBox _desktopShortcut;
    private readonly CheckBox _startMenuShortcut;
    private readonly string _payloadDir = Path.Combine(AppContext.BaseDirectory, "payload");
    private readonly string _ue4ssRuntimeDir = Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime");
    private readonly string _installDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        AppName);

    private List<DependencyCheck> _currentChecks = [];
    private bool _isScanning;
    private int _spinnerIndex;
    private int _scanStep;
    private int _scanTicks;
    private readonly List<DependencyRow> _scanRows = [];

    public InstallerForm()
    {
        Text = "Vein Dump Manager Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 740);
        ClientSize = new Size(960, 760);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Back;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 9F);
        Icon = TryLoadIcon();

        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = Color.FromArgb(18, 21, 30)
        };
        titleBar.MouseDown += (_, _) => BeginTitleDrag();
        Controls.Add(titleBar);
        var titleText = new Label
        {
            Text = "Vein Dump Manager Installer",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9F),
            Location = new Point(42, 12),
            AutoSize = true
        };
        titleText.MouseDown += (_, _) => BeginTitleDrag();
        titleBar.Controls.Add(titleText);

        var titleIcon = new PictureBox
        {
            Image = TryLoadLogo(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(12, 8),
            Size = new Size(22, 22)
        };
        titleIcon.MouseDown += (_, _) => BeginTitleDrag();
        titleBar.Controls.Add(titleIcon);
        titleBar.Controls.Add(MakeTitleButton("_", (_, _) => WindowState = FormWindowState.Minimized, isClose: false));
        titleBar.Controls.Add(MakeTitleButton("□", (_, _) => ToggleMaximize(), isClose: false));
        titleBar.Controls.Add(MakeTitleButton("X", (_, _) => Close(), isClose: true));

        var hero = new RoundedPanel
        {
            Location = new Point(28, 70),
            Size = new Size(190, 650),
            FillColor = Panel,
            BorderColor = Border
        };
        Controls.Add(hero);
        hero.Controls.Add(new PictureBox
        {
            Image = TryLoadLogo(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(24, 28),
            Size = new Size(142, 142)
        });
        hero.Controls.Add(MakeLabel("VEIN DUMP", 28, 184, 140, 24, 13F, FontStyle.Bold, TextMain));
        hero.Controls.Add(MakeLabel("MANAGER", 28, 210, 140, 24, 13F, FontStyle.Bold, TextMain));
        hero.Controls.Add(MakeSeparator(22, 260, 146));
        hero.Controls.Add(MakeLabel("Installs the app, verifies required runtime pieces, and helps users fix missing dependencies before first launch.", 22, 286, 145, 100, 9F, FontStyle.Regular, TextMuted));

        Controls.Add(MakeLabel("Install Vein Dump Manager", 248, 76, 560, 42, 24F, FontStyle.Bold, TextMain));
        Controls.Add(MakeLabel("Checks dependencies, installs the bundled app, and creates shortcuts.", 250, 120, 640, 28, 10.2F, FontStyle.Regular, TextMuted));

        var card = new RoundedPanel
        {
            Location = new Point(248, 166),
            Size = new Size(680, 438),
            FillColor = Panel,
            BorderColor = Border,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(card);
        card.Controls.Add(MakeLabel("Dependency Check", 22, 20, 220, 28, 14F, FontStyle.Bold, TextMain));
        card.Controls.Add(MakeLabel("The installer checks what the end user actually needs. It does not install developer SDKs.", 22, 52, 610, 22, 9.5F, FontStyle.Regular, TextMuted));

        _checks.Location = new Point(22, 88);
        _checks.Size = new Size(636, 330);
        _checks.FlowDirection = FlowDirection.TopDown;
        _checks.WrapContents = false;
        _checks.AutoScroll = false;
        _checks.BackColor = Color.Transparent;
        card.Controls.Add(_checks);

        var options = new RoundedPanel
        {
            Location = new Point(248, 608),
            Size = new Size(680, 100),
            FillColor = Panel,
            BorderColor = Border,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        Controls.Add(options);

        options.Controls.Add(MakeLabel("Install options", 22, 14, 110, 18, 8.8F, FontStyle.Bold, TextMuted));
        _recommendedInstall = new ThemedRadioButton
        {
            Text = "Recommended",
            Checked = true,
            ForeColor = TextMain,
            BackColor = Panel,
            Location = new Point(142, 13),
            Size = new Size(170, 22),
            TabStop = false,
            AutoSize = false
        };
        _recommendedInstall.CheckedChanged += (_, _) => UpdateInstallMode();
        options.Controls.Add(_recommendedInstall);

        _customInstall = new ThemedRadioButton
        {
            Text = "Custom",
            ForeColor = TextMain,
            BackColor = Panel,
            Location = new Point(340, 13),
            Size = new Size(120, 22),
            TabStop = false,
            AutoSize = false
        };
        _customInstall.CheckedChanged += (_, _) => UpdateInstallMode();
        options.Controls.Add(_customInstall);

        _installUe4ssRuntime = new ThemedCheckBox
        {
            Text = "UE4SS-Vein support",
            Checked = true,
            ForeColor = TextMain,
            BackColor = Panel,
            Location = new Point(22, 44),
            Size = new Size(230, 22),
            TabStop = false,
            AutoSize = false
        };
        options.Controls.Add(_installUe4ssRuntime);

        _setupCheatEngine = new ThemedCheckBox
        {
            Text = "Cheat Engine setup",
            Checked = false,
            ForeColor = TextMain,
            BackColor = Panel,
            Location = new Point(292, 44),
            Size = new Size(230, 22),
            TabStop = false,
            AutoSize = false
        };
        options.Controls.Add(_setupCheatEngine);

        _desktopShortcut = new ThemedCheckBox
        {
            Text = "Desktop shortcut",
            Checked = true,
            ForeColor = TextMain,
            BackColor = Panel,
            Location = new Point(22, 68),
            Size = new Size(230, 22),
            TabStop = false,
            AutoSize = false
        };
        options.Controls.Add(_desktopShortcut);

        _startMenuShortcut = new ThemedCheckBox
        {
            Text = "Start Menu shortcut",
            Checked = true,
            ForeColor = TextMain,
            BackColor = Panel,
            Location = new Point(292, 68),
            Size = new Size(230, 22),
            TabStop = false,
            AutoSize = false
        };
        options.Controls.Add(_startMenuShortcut);

        _status.Text = string.Empty;
        _status.ForeColor = TextMuted;
        _status.Location = new Point(28, 620);
        _status.Size = new Size(520, 24);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

        _installButton = MakeButton("Install", Red, 668, 606, 120, 34);
        _installButton.Click += (_, _) => InstallApp();
        Controls.Add(_installButton);

        _launchButton = MakeButton("Launch", Color.FromArgb(22, 44, 74), 800, 606, 120, 34);
        _launchButton.Enabled = false;
        _launchButton.Visible = false;
        _launchButton.Click += (_, _) => LaunchInstalledApp();
        Controls.Add(_launchButton);
        UpdateInstallMode();

        _scanTimer.Interval = 260;
        _scanTimer.Tick += (_, _) =>
        {
            if (!_isScanning) return;
            _spinnerIndex = (_spinnerIndex + 1) % SpinnerFrames.Length;
            _scanTicks++;
            if (_scanTicks % 3 == 0 && _scanStep < DependencyScanner.ExpectedDependencyNames.Length - 1)
            {
                _scanStep++;
            }

            UpdateScanningRows();
        };

        Resize += (_, _) => LayoutButtons();
        Shown += (_, _) => BeginDependencyScan(showPopups: true);
        LayoutButtons();
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);

    private void BeginTitleDrag()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
        }

        ReleaseCapture();
        SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void LayoutButtons()
    {
        const int contentLeft = 248;
        var contentRight = ClientSize.Width - 32;
        var centeredX = contentLeft + ((contentRight - contentLeft) - _installButton.Width) / 2;
        _installButton.Location = new Point(centeredX, ClientSize.Height - 44);
        _launchButton.Location = new Point(centeredX, ClientSize.Height - 44);
        _status.Location = new Point(28, ClientSize.Height - 38);
        _status.Width = Math.Max(180, centeredX - _status.Left - 18);
    }

    private void UpdateInstallMode()
    {
        var recommended = _recommendedInstall.Checked;
        if (recommended)
        {
            _installUe4ssRuntime.Checked = true;
            _setupCheatEngine.Checked = false;
            _desktopShortcut.Checked = true;
            _startMenuShortcut.Checked = true;
        }

        _installUe4ssRuntime.Enabled = !recommended;
        _setupCheatEngine.Enabled = !recommended;
        _desktopShortcut.Enabled = !recommended;
        _startMenuShortcut.Enabled = !recommended;
    }

    private async void BeginDependencyScan(bool showPopups)
    {
        if (_isScanning)
        {
            return;
        }

        _isScanning = true;
        _scanStep = 0;
        _scanTicks = 0;
        _spinnerIndex = 0;
        _installButton.Enabled = false;
        _installButton.Visible = false;
        _launchButton.Visible = false;
        _status.Text = string.Empty;
        ShowScanningRows();
        _scanTimer.Start();

        await Task.Delay(3_800);
        var checks = await Task.Run(() => DependencyScanner.Scan(_payloadDir).ToList());

        _scanTimer.Stop();
        _isScanning = false;
        ApplyChecks(checks, showPopups);
    }

    private void ShowScanningRows()
    {
        var symbol = SpinnerFrames[_spinnerIndex];
        _checks.SuspendLayout();
        if (_scanRows.Count != DependencyScanner.ExpectedDependencyNames.Length || _checks.Controls.Count != DependencyScanner.ExpectedDependencyNames.Length)
        {
            _checks.Controls.Clear();
            _scanRows.Clear();
            for (var index = 0; index < DependencyScanner.ExpectedDependencyNames.Length; index++)
            {
                var row = new DependencyRow();
                _scanRows.Add(row);
                _checks.Controls.Add(row);
            }
        }

        UpdateScanningRows(symbol);
        _checks.ResumeLayout();
    }

    private void UpdateScanningRows()
    {
        if (_scanRows.Count == 0)
        {
            ShowScanningRows();
            return;
        }

        UpdateScanningRows(SpinnerFrames[_spinnerIndex]);
    }

    private void UpdateScanningRows(string symbol)
    {
        for (var index = 0; index < DependencyScanner.ExpectedDependencyNames.Length; index++)
        {
            var name = DependencyScanner.ExpectedDependencyNames[index];
            var isActive = index == _scanStep;
            var isPending = index > _scanStep;
            var message = isActive
                ? "Checking this item..."
                : isPending
                    ? "Queued for verification."
                    : "Checked. Waiting for final result...";
            var check = new DependencyCheck(name, !isPending && !isActive, DependencySeverity.Included, message, "Please wait while the installer verifies this item.", null);
            _scanRows[index].UpdateState(check, () => { }, isChecking: isActive, activitySymbol: symbol, isPending: isPending, showActions: false);
        }
    }

    private void RefreshChecks(bool showPopups)
    {
        ApplyChecks(DependencyScanner.Scan(_payloadDir).ToList(), showPopups);
    }

    private void ApplyChecks(List<DependencyCheck> checks, bool showPopups)
    {
        _currentChecks = checks;
        _checks.Controls.Clear();
        _scanRows.Clear();
        foreach (var check in _currentChecks)
        {
            _checks.Controls.Add(new DependencyRow(check, () => HandleDependencyAction(check)));
        }

        var blocking = _currentChecks.Where(check => check.Severity == DependencySeverity.Required && !check.IsSatisfied).ToArray();
        var warnings = _currentChecks.Where(check => check.Severity == DependencySeverity.Feature && !check.IsSatisfied).ToArray();
        var appInstalled = File.Exists(InstalledExePath);
        _installButton.Text = appInstalled ? "Reinstall" : "Install";
        _installButton.Visible = true;
        _installButton.Enabled = blocking.Length == 0;
        _launchButton.Enabled = appInstalled;
        _launchButton.Visible = false;

        _status.Text = string.Empty;

        if (showPopups)
        {
            foreach (var check in blocking.Concat(warnings).Take(3))
            {
                using var dialog = new DependencyDialog(check, TryLoadLogo());
                dialog.StartPosition = FormStartPosition.CenterParent;
                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleDependencyAction(check);
                }
            }
        }
    }

    private void HandleDependencyAction(DependencyCheck check)
    {
        try
        {
            if (check.ActionUri == DependencyScanner.Ue4ssInstallAction)
            {
                InstallBundledUe4ssRuntime(showSuccess: true);
                RefreshChecks(showPopups: false);
                return;
            }

            if (check.ActionUri is null)
            {
                MessageBox.Show(this, check.ActionHelp, "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = check.ActionUri,
                UseShellExecute = true
            });

            _status.Text = $"Opened installer/help for {check.Name}. Click Verify after it finishes.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open dependency installer/help: {ex.Message}", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InstallApp()
    {
        RefreshChecks(showPopups: false);
        var blocking = _currentChecks.Where(check => check.Severity == DependencySeverity.Required && !check.IsSatisfied).ToArray();
        if (blocking.Length > 0)
        {
            using var dialog = new DependencyDialog(blocking[0], TryLoadLogo());
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                HandleDependencyAction(blocking[0]);
            }

            return;
        }

        try
        {
            _installButton.Enabled = false;
            _status.Text = "Installing app files...";
            Application.DoEvents();

            CopyDirectory(_payloadDir, _installDir);
            WriteInstallMarker();

            var veinRoot = DependencyScanner.FindVeinRoot();
            if (_installUe4ssRuntime.Checked && veinRoot is not null && DependencyScanner.GetMissingUe4ssRuntimeFiles(veinRoot).Any())
            {
                InstallBundledUe4ssRuntime(showSuccess: false);
            }

            if (_desktopShortcut.Checked)
            {
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppName}.lnk"),
                    InstalledExePath);
            }

            if (_startMenuShortcut.Checked)
            {
                var startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", AppName);
                Directory.CreateDirectory(startMenuDir);
                CreateShortcut(Path.Combine(startMenuDir, $"{AppName}.lnk"), InstalledExePath);
            }

            if (_setupCheatEngine.Checked)
            {
                SetupOptionalCheatEngineSupport();
            }

            _status.Text = $"Installed to {_installDir}";
            _launchButton.Enabled = true;
            _launchButton.Visible = true;
            LaunchInstalledApp();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Install failed: {ex.Message}", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Install failed.";
        }
        finally
        {
            _installButton.Enabled = true;
            RefreshChecks(showPopups: false);
        }
    }

    private void SetupOptionalCheatEngineSupport()
    {
        var cheatEngine = DependencyScanner.FindCheatEngine();
        if (cheatEngine is not null)
        {
            File.WriteAllText(
                Path.Combine(_installDir, "cheat-engine-support.txt"),
                $"Cheat Engine detected: {cheatEngine}{Environment.NewLine}Detected at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}",
                Encoding.UTF8);
            return;
        }

        _status.Text = "Installing optional Cheat Engine support...";
        Application.DoEvents();

        var bundledInstaller = DependencyScanner.FindBundledCheatEngineInstaller(AppContext.BaseDirectory);
        if (bundledInstaller is not null)
        {
            RunInstallerProcess(bundledInstaller, "/S", "Cheat Engine");
            cheatEngine = DependencyScanner.FindCheatEngine();
            if (cheatEngine is not null)
            {
                File.WriteAllText(
                    Path.Combine(_installDir, "cheat-engine-support.txt"),
                    $"Cheat Engine installed from bundled installer: {cheatEngine}{Environment.NewLine}Installed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}",
                    Encoding.UTF8);
                return;
            }
        }

        var winget = TryRunWingetInstall("CheatEngine.CheatEngine", "Cheat Engine");
        cheatEngine = DependencyScanner.FindCheatEngine();
        if (winget && cheatEngine is not null)
        {
            File.WriteAllText(
                Path.Combine(_installDir, "cheat-engine-support.txt"),
                $"Cheat Engine installed through winget: {cheatEngine}{Environment.NewLine}Installed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}",
                Encoding.UTF8);
            return;
        }

        var downloadedInstaller = DependencyScanner.DownloadOfficialCheatEngineInstaller();
        if (downloadedInstaller is not null)
        {
            RunInstallerProcess(downloadedInstaller, "/S", "Cheat Engine");
            cheatEngine = DependencyScanner.FindCheatEngine();
            if (cheatEngine is not null)
            {
                File.WriteAllText(
                    Path.Combine(_installDir, "cheat-engine-support.txt"),
                    $"Cheat Engine installed from official download: {cheatEngine}{Environment.NewLine}Installed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}",
                    Encoding.UTF8);
                return;
            }
        }

        File.WriteAllText(
            Path.Combine(_installDir, "cheat-engine-support.txt"),
            $"Cheat Engine was selected, but no bundled installer, winget package, or official downloadable installer completed successfully. Download page opened: {DependencyScanner.CheatEngineDownloadUrl}{Environment.NewLine}Checked at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}",
            Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = DependencyScanner.CheatEngineDownloadUrl,
            UseShellExecute = true
        });
        throw new InvalidOperationException("Cheat Engine was selected, but the installer could not install it automatically. The official download page was opened.");
    }

    private static void RunInstallerProcess(string fileName, string arguments, string name)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        if (process is null)
        {
            throw new InvalidOperationException($"Could not start the {name} installer.");
        }

        if (!process.WaitForExit(600_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"{name} installer did not finish within 10 minutes.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{name} installer exited with code {process.ExitCode}.");
        }
    }

    private static bool TryRunWingetInstall(string packageId, string name)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "winget",
                ArgumentList =
                {
                    "install",
                    "--id",
                    packageId,
                    "--exact",
                    "--silent",
                    "--accept-package-agreements",
                    "--accept-source-agreements"
                },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(600_000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void InstallBundledUe4ssRuntime(bool showSuccess)
    {
        var veinRoot = DependencyScanner.FindVeinRoot();
        if (veinRoot is null)
        {
            MessageBox.Show(this, "VEIN was not found. Install VEIN through Steam first, then click Verify.", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var bundledWin64 = Path.Combine(_ue4ssRuntimeDir, "Win64");
        var bundledUe4ss = Path.Combine(_ue4ssRuntimeDir, "ue4ss");
        if (!File.Exists(Path.Combine(bundledWin64, "dwmapi.dll")) || !File.Exists(Path.Combine(bundledUe4ss, "UE4SS.dll")))
        {
            MessageBox.Show(this, $"Bundled UE4SS-Vein runtime files are missing from the installer package: {_ue4ssRuntimeDir}", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var win64 = Path.Combine(veinRoot, "Binaries", "Win64");
        var targetUe4ss = Path.Combine(win64, "ue4ss");
        var backupRoot = Path.Combine(targetUe4ss, "Backups", $"installer-{DateTime.Now:yyyyMMdd-HHmmss}");

        try
        {
            _status.Text = "Installing UE4SS-Vein runtime files into VEIN...";
            Application.DoEvents();

            Directory.CreateDirectory(win64);
            Directory.CreateDirectory(targetUe4ss);
            CopyRuntimeFile(Path.Combine(bundledWin64, "dwmapi.dll"), Path.Combine(win64, "dwmapi.dll"), backupRoot, Path.Combine("Win64", "dwmapi.dll"));
            CopyRuntimeDirectory(bundledUe4ss, targetUe4ss, backupRoot);
            EnsureModsTxtEntry(Path.Combine(targetUe4ss, "Mods", "mods.txt"), "Keybinds", "1");

            _status.Text = $"Installed UE4SS-Vein runtime to {win64}";
            if (showSuccess)
            {
                MessageBox.Show(this, $"UE4SS-Vein runtime files were installed into:{Environment.NewLine}{win64}{Environment.NewLine}{Environment.NewLine}Existing overwritten files were backed up under:{Environment.NewLine}{backupRoot}", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(this, $"Windows blocked writing to the VEIN install folder. Run this installer as administrator, then try again.{Environment.NewLine}{Environment.NewLine}{ex.Message}", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "UE4SS install needs administrator permission.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not install UE4SS-Vein runtime files: {ex.Message}", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "UE4SS runtime install failed.";
        }
    }

    private static void CopyRuntimeDirectory(string source, string destination, string backupRoot)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Equals(Path.Combine("Mods", "mods.txt"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CopyRuntimeFile(file, Path.Combine(destination, relative), backupRoot, Path.Combine("ue4ss", relative));
        }
    }

    private static void CopyRuntimeFile(string source, string destination, string backupRoot, string backupRelativePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            var backupPath = Path.Combine(backupRoot, backupRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            File.Copy(destination, backupPath, overwrite: true);
        }

        File.Copy(source, destination, overwrite: true);
    }

    private static void EnsureModsTxtEntry(string modsTxt, string modName, string value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(modsTxt)!);
        var lines = File.Exists(modsTxt) ? File.ReadAllLines(modsTxt).ToList() : [];
        var replacement = $"{modName} : {value}";
        var replaced = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith($"{modName} :", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = replacement;
                replaced = true;
                break;
            }
        }

        if (!replaced)
        {
            lines.Add(replacement);
        }

        File.WriteAllLines(modsTxt, lines, Encoding.UTF8);
    }

    private string InstalledExePath => Path.Combine(_installDir, "Vein Dump Manager.exe");

    private void LaunchInstalledApp()
    {
        if (!File.Exists(InstalledExePath))
        {
            MessageBox.Show(this, "The installed app was not found. Run Install first.", "Vein Dump Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = InstalledExePath,
            WorkingDirectory = _installDir,
            UseShellExecute = true
        });
    }

    private void WriteInstallMarker()
    {
        File.WriteAllText(Path.Combine(_installDir, "install-info.txt"),
            $"Vein Dump Manager installed {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}Source payload: {_payloadDir}{Environment.NewLine}",
            Encoding.UTF8);
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Installer payload was not found: {source}");
        }

        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.IconLocation = targetPath;
            shortcut.Description = "Launch Vein Dump Manager";
            shortcut.Save();
        }
        catch
        {
            // Shortcut failure should not make the install fail.
        }
    }

    private static Label MakeLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color)
    {
        return new Label
        {
            Text = text,
            Location = new Point(left, top),
            Size = new Size(width, height),
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = Color.Transparent
        };
    }

    private static Control MakeSeparator(int left, int top, int width)
    {
        return new Panel
        {
            Location = new Point(left, top),
            Size = new Size(width, 1),
            BackColor = Border
        };
    }

    private static Button MakeButton(string text, Color back, int left, int top, int width, int height)
    {
        var button = new ThemedButton
        {
            Text = text,
            Location = new Point(left, top),
            Size = new Size(width, height),
            BackColor = back,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TabStop = false,
            BorderSize = back == Red ? 0 : 1,
            BorderColor = back == Red ? Red : Border,
            HoverBackColor = back == Red ? Color.FromArgb(215, 25, 58) : Color.FromArgb(28, 58, 96),
            PressedBackColor = back == Red ? Color.FromArgb(160, 12, 38) : Color.FromArgb(18, 38, 68)
        };
        return button;
    }

    private static Button MakeTitleButton(string text, EventHandler click, bool isClose)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Right,
            Width = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(18, 21, 30),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TabStop = false,
            Margin = Padding.Empty
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = isClose ? Red : Color.FromArgb(28, 48, 78);
        button.FlatAppearance.MouseDownBackColor = isClose ? Color.FromArgb(150, 12, 38) : Color.FromArgb(38, 70, 112);
        button.Click += click;
        return button;
    }

    private static Image? TryLoadLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "installer-logo.png");
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var source = Image.FromStream(stream);
        var maxSide = Math.Max(source.Width, source.Height);
        if (maxSide <= 512)
        {
            return new Bitmap(source);
        }

        var scale = 512d / maxSide;
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        return new Bitmap(source, new Size(width, height));
    }

    private static Icon? TryLoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.ico");
        return File.Exists(path) ? new Icon(path) : null;
    }

    internal static Color StatusColor(DependencyCheck check)
    {
        if (check.IsSatisfied) return Green;
        return check.Severity == DependencySeverity.Required ? Red : Amber;
    }
}

internal static class InstallerSelfTest
{
    public static int Run()
    {
        var payload = Path.Combine(AppContext.BaseDirectory, "payload");
        var checks = DependencyScanner.Scan(payload).ToArray();
        Console.WriteLine($"payload={payload}");
        foreach (var check in checks)
        {
            Console.WriteLine($"{check.Name}: {(check.IsSatisfied ? "OK" : "MISSING")} [{check.Severity}] {check.Message}");
        }

        var appExe = Path.Combine(payload, "Vein Dump Manager.exe");
        var requiredPayloadFiles = new[]
        {
            appExe,
            Path.Combine(payload, "Assets", "vein-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "installer-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "Win64", "dwmapi.dll"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "ue4ss", "UE4SS.dll"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "ue4ss", "UE4SS-settings.ini"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "ue4ss", "MemberVariableLayout.ini"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "ue4ss", "VTableLayout.ini"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "ue4ss", "Mods", "Keybinds", "Scripts", "main.lua"),
            Path.Combine(payload, "tools", "vein-cheat-engine-dump-workflow", "Start-VEIN-CheatEngineDump.ps1"),
            Path.Combine(payload, "tools", "vein-cheat-engine-dump-workflow", "VEIN_MasterDump_Throttled.lua")
        };

        foreach (var file in requiredPayloadFiles)
        {
            Console.WriteLine($"payload-file: {(File.Exists(file) ? "OK" : "MISSING")} {file}");
        }

        var bundledUe4ss = Path.Combine(AppContext.BaseDirectory, "Assets", "UE4SS-Vein-runtime", "ue4ss");
        var signatures = Path.Combine(bundledUe4ss, "UE4SS_Signatures");
        var hasSignatures = Directory.Exists(signatures) && Directory.EnumerateFiles(signatures, "*", SearchOption.AllDirectories).Any();
        var hasUsmap = Directory.Exists(bundledUe4ss) && Directory.EnumerateFiles(bundledUe4ss, "Vein-*.usmap", SearchOption.TopDirectoryOnly).Any();
        Console.WriteLine($"payload-dir: {(hasSignatures ? "OK" : "MISSING")} {signatures}");
        Console.WriteLine($"payload-usmap: {(hasUsmap ? "OK" : "MISSING")} {Path.Combine(bundledUe4ss, "Vein-*.usmap")}");

        var blocking = checks.Where(check => check.Severity == DependencySeverity.Required && !check.IsSatisfied).ToArray();
        var missingPayload = requiredPayloadFiles.Where(file => !File.Exists(file)).ToArray();
        return blocking.Length == 0 && missingPayload.Length == 0 && hasSignatures && hasUsmap ? 0 : 2;
    }

    public static int RunInstallSmokeTest()
    {
        var payload = Path.Combine(AppContext.BaseDirectory, "payload");
        var installRoot = Path.Combine(Path.GetTempPath(), $"VeinDumpManagerInstallerSmoke-{Guid.NewGuid():N}");
        try
        {
            CopyDirectory(payload, installRoot);
            var exe = Path.Combine(installRoot, "Vein Dump Manager.exe");
            if (!File.Exists(exe))
            {
                Console.Error.WriteLine($"Installed EXE missing: {exe}");
                return 2;
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                ArgumentList = { "--self-test" },
                WorkingDirectory = installRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process is null)
            {
                Console.Error.WriteLine("Could not start installed app self-test.");
                return 2;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(30_000);
            Console.Write(output);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.Write(error);
            }

            Console.WriteLine($"install-smoke-test-root={installRoot}");
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(installRoot))
                {
                    Directory.Delete(installRoot, recursive: true);
                }
            }
            catch
            {
                // Test cleanup failure should not hide the real install result.
            }
        }
    }

    public static int RenderScreenshot()
    {
        try
        {
            var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "outputs", "vein-dump-manager-installer"));
            Directory.CreateDirectory(outputDir);
            using var form = new InstallerForm
            {
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Opacity = 0
            };
            form.Show();
            form.ClientSize = new Size(960, 760);
            form.PerformLayout();
            var waitUntil = DateTime.UtcNow.AddMilliseconds(4_600);
            while (DateTime.UtcNow < waitUntil)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }

            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            var path = Path.Combine(outputDir, "installer-960x760.png");
            bitmap.Save(path);
            Console.WriteLine(path);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Installer payload was not found: {source}");
        }

        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}

internal static class DependencyScanner
{
    internal const string Ue4ssInstallAction = "install-ue4ss-runtime";
    internal const string CheatEngineDownloadUrl = "https://www.cheatengine.org/downloads.php";
    internal static readonly string[] ExpectedDependencyNames =
    [
        "Windows x64",
        "Bundled app payload",
        ".NET runtime",
        "Visual C++ Runtime",
        "VEIN game install",
        "UE4SS-Vein runtime"
    ];

    private const string SteamAppId = "1857950";
    public static IEnumerable<DependencyCheck> Scan(string payloadDir)
    {
        yield return new DependencyCheck(
            "Windows x64",
            Environment.Is64BitOperatingSystem,
            DependencySeverity.Required,
            "Vein Dump Manager is packaged for 64-bit Windows.",
            "Use a 64-bit Windows machine.",
            null);

        yield return new DependencyCheck(
            "Bundled app payload",
            File.Exists(Path.Combine(payloadDir, "Vein Dump Manager.exe")),
            DependencySeverity.Required,
            "The installer includes the app and the runtime files a C# desktop app needs to run.",
            "Re-download the full installer package. The payload folder is missing.",
            null);

        yield return new DependencyCheck(
            ".NET runtime",
            true,
            DependencySeverity.Included,
            "Included inside this self-contained Vein Dump Manager build.",
            "No action needed.",
            null);

        yield return new DependencyCheck(
            "Visual C++ Runtime",
            true,
            DependencySeverity.Included,
            "No separate Visual C++ redistributable is required by the packaged Vein Dump Manager app.",
            "No action needed.",
            null);

        var veinRoot = FindVeinRoot();
        yield return new DependencyCheck(
            "VEIN game install",
            veinRoot is not null,
            DependencySeverity.Feature,
            veinRoot is null
                ? "VEIN was not found. The app can install, but dumping requires VEIN to be installed through Steam."
                : $"Found VEIN at {veinRoot}.",
            "Install VEIN through Steam, then click Verify.",
            $"steam://install/{SteamAppId}");

        var missingUe4ss = veinRoot is null ? new[] { "VEIN game folder" } : GetMissingUe4ssRuntimeFiles(veinRoot).ToArray();
        yield return new DependencyCheck(
            "UE4SS-Vein runtime",
            missingUe4ss.Length == 0,
            DependencySeverity.Feature,
            missingUe4ss.Length == 0
                ? "Required UE4SS-Vein runtime files are present for UE4SS dumps, Lua logs, mod reloads, and hotkeys."
                : $"Missing required UE4SS-Vein runtime pieces: {string.Join(", ", missingUe4ss.Take(3))}{(missingUe4ss.Length > 3 ? "..." : "")}",
            "Install only the UE4SS-Vein runtime package pieces: copy dwmapi.dll into Win64 and the ue4ss folder contents into Win64\\ue4ss. VeinToolkit is not required for this dumper unless you want VeinCF mod-building features.",
            Ue4ssInstallAction);

    }

    internal static IEnumerable<string> GetMissingUe4ssRuntimeFiles(string veinRoot)
    {
        var win64 = Path.Combine(veinRoot, "Binaries", "Win64");
        var ue4ss = Path.Combine(win64, "ue4ss");

        var requiredFiles = new Dictionary<string, string>
        {
            ["Win64\\dwmapi.dll"] = Path.Combine(win64, "dwmapi.dll"),
            ["ue4ss\\UE4SS.dll"] = Path.Combine(ue4ss, "UE4SS.dll"),
            ["ue4ss\\UE4SS-settings.ini"] = Path.Combine(ue4ss, "UE4SS-settings.ini"),
            ["ue4ss\\MemberVariableLayout.ini"] = Path.Combine(ue4ss, "MemberVariableLayout.ini"),
            ["ue4ss\\VTableLayout.ini"] = Path.Combine(ue4ss, "VTableLayout.ini"),
            ["ue4ss\\Mods\\mods.txt"] = Path.Combine(ue4ss, "Mods", "mods.txt"),
            ["ue4ss\\Mods\\Keybinds\\Scripts\\main.lua"] = Path.Combine(ue4ss, "Mods", "Keybinds", "Scripts", "main.lua")
        };

        foreach (var pair in requiredFiles)
        {
            if (!File.Exists(pair.Value))
            {
                yield return pair.Key;
            }
        }

        if (!Directory.Exists(Path.Combine(ue4ss, "UE4SS_Signatures")))
        {
            yield return "ue4ss\\UE4SS_Signatures";
        }

        if (!Directory.Exists(ue4ss) || !Directory.EnumerateFiles(ue4ss, "Vein-*.usmap", SearchOption.TopDirectoryOnly).Any())
        {
            yield return "ue4ss\\Vein-*.usmap";
        }
    }

    internal static string? FindVeinRoot()
    {
        var defaultRoot = @"C:\Program Files (x86)\Steam\steamapps\common\Vein\Vein";
        if (File.Exists(Path.Combine(defaultRoot, "Binaries", "Win64", "Vein-Win64-Test.exe")))
        {
            return defaultRoot;
        }

        foreach (var library in EnumerateSteamLibraries())
        {
            var candidate = Path.Combine(library, "steamapps", "common", "Vein", "Vein");
            if (File.Exists(Path.Combine(candidate, "Binaries", "Win64", "Vein-Win64-Test.exe")))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSteamLibraries()
    {
        var roots = new[]
        {
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam"
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            yield return root;
            var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
            {
                continue;
            }

            foreach (var line in File.ReadLines(vdf))
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains("\"path\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var path = parts[^1].Replace(@"\\", @"\");
                    if (Directory.Exists(path))
                    {
                        yield return path;
                    }
                }
            }
        }
    }

    internal static string? FindCheatEngine()
    {
        var envPath = Environment.GetEnvironmentVariable("CHEAT_ENGINE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        var candidates = new[]
        {
            @"C:\Program Files\Cheat Engine\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files\Cheat Engine\cheatengine-x86_64.exe",
            @"C:\Program Files\Cheat Engine 7.6\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files\Cheat Engine 7.6\cheatengine-x86_64.exe",
            @"C:\Program Files\Cheat Engine 7.5\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files\Cheat Engine 7.5\cheatengine-x86_64.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.6\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.6\cheatengine-x86_64.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.5\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.5\cheatengine-x86_64.exe"
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    internal static string? FindBundledCheatEngineInstaller(string baseDirectory)
    {
        var installers = Path.Combine(baseDirectory, "Assets", "Installers");
        if (!Directory.Exists(installers))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(installers, "*Cheat*Engine*.exe", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    internal static string? DownloadOfficialCheatEngineInstaller()
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(45)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VeinDumpManagerInstaller/1.0");
            var html = client.GetStringAsync(CheatEngineDownloadUrl).GetAwaiter().GetResult();
            var match = Regex.Match(html, "https://[^\"']+\\.exe", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            var downloadUrl = System.Net.WebUtility.HtmlDecode(match.Value);
            var tempDir = Path.Combine(Path.GetTempPath(), "VeinDumpManagerInstaller");
            Directory.CreateDirectory(tempDir);
            var target = Path.Combine(tempDir, "CheatEngineSetup.exe");
            var bytes = client.GetByteArrayAsync(downloadUrl).GetAwaiter().GetResult();
            if (bytes.Length < 1_000_000)
            {
                return null;
            }

            File.WriteAllBytes(target, bytes);
            return target;
        }
        catch
        {
            return null;
        }
    }

}

internal sealed class SmoothFlowLayoutPanel : FlowLayoutPanel
{
    public SmoothFlowLayoutPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}

internal sealed class ThemedCheckBox : CheckBox
{
    private bool _hovered;
    private static readonly Color Surface = Color.FromArgb(8, 16, 30);

    public ThemedCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        AutoSize = true;
        MinimumSize = new Size(0, 20);
    }

    protected override bool ShowFocusCues => false;

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent is RoundedPanel panel ? panel.FillColor : Surface);

        var box = new Rectangle(0, 3, 14, 14);
        var border = Enabled
            ? (_hovered ? Color.FromArgb(55, 145, 255) : Color.FromArgb(32, 68, 112))
            : Color.FromArgb(56, 80, 110);
        var fill = Checked
            ? (Enabled ? Color.FromArgb(55, 145, 255) : Color.FromArgb(58, 86, 122))
            : Color.FromArgb(5, 12, 22);

        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border, 1.4F))
        {
            graphics.FillRectangle(brush, box);
            graphics.DrawRectangle(pen, box);
        }

        if (Checked)
        {
            using var pen = new Pen(Color.White, 1.7F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            graphics.DrawLines(pen, new[] { new Point(3, 10), new Point(6, 13), new Point(12, 6) });
        }

        var textColor = Enabled ? ForeColor : Color.FromArgb(120, ForeColor);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            new Rectangle(22, 0, Math.Max(0, Width - 22), Height),
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class ThemedRadioButton : RadioButton
{
    private bool _hovered;
    private static readonly Color Surface = Color.FromArgb(8, 16, 30);

    public ThemedRadioButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        AutoSize = true;
        MinimumSize = new Size(0, 20);
    }

    protected override bool ShowFocusCues => false;

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent is RoundedPanel panel ? panel.FillColor : Surface);

        var outer = new Rectangle(0, 3, 14, 14);
        var border = _hovered || Checked
            ? Color.FromArgb(55, 145, 255)
            : Color.FromArgb(32, 68, 112);
        using (var brush = new SolidBrush(Color.FromArgb(5, 12, 22)))
        using (var pen = new Pen(border, 1.5F))
        {
            graphics.FillEllipse(brush, outer);
            graphics.DrawEllipse(pen, outer);
        }

        if (Checked)
        {
            using var brush = new SolidBrush(Color.FromArgb(55, 145, 255));
            graphics.FillEllipse(brush, new Rectangle(4, 7, 6, 6));
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            new Rectangle(22, 0, Math.Max(0, Width - 22), Height),
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class ThemedButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public Color BorderColor { get; set; } = Color.FromArgb(32, 68, 112);
    public Color HoverBackColor { get; set; } = Color.FromArgb(28, 58, 96);
    public Color PressedBackColor { get; set; } = Color.FromArgb(18, 38, 68);
    public int BorderSize { get; set; } = 1;

    public ThemedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
    }

    protected override bool ShowFocusCues => false;

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.Clear(Parent?.BackColor ?? Color.Transparent);

        var fill = !Enabled
            ? Color.FromArgb(30, BackColor.R, BackColor.G, BackColor.B)
            : _pressed
                ? PressedBackColor
                : _hovered
                    ? HoverBackColor
                    : BackColor;

        using var brush = new SolidBrush(fill);
        var rect = new Rectangle(0, 0, Width, Height);
        graphics.FillRectangle(brush, rect);

        if (BorderSize > 0)
        {
            using var pen = new Pen(BorderColor, BorderSize);
            graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        var textColor = Enabled ? ForeColor : Color.FromArgb(120, ForeColor);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            rect,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class DependencyRow : Panel
{
    private readonly Label _statusLabel;
    private readonly Label _nameLabel;
    private readonly Label _messageLabel;
    private readonly Button _installButton;

    public DependencyRow()
    {
        Size = new Size(610, 40);
        BackColor = Color.FromArgb(5, 12, 22);
        Margin = new Padding(0, 0, 0, 6);
        DoubleBuffered = true;

        _statusLabel = new Label
        {
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Location = new Point(12, 9),
            Size = new Size(30, 22),
            BackColor = Color.Transparent
        };
        Controls.Add(_statusLabel);

        _nameLabel = new Label
        {
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
            Location = new Point(48, 4),
            Size = new Size(220, 18),
            BackColor = Color.Transparent
        };
        Controls.Add(_nameLabel);

        _messageLabel = new Label
        {
            ForeColor = Color.FromArgb(188, 215, 250),
            Font = new Font("Segoe UI", 8.3F),
            Location = new Point(48, 21),
            Size = new Size(450, 17),
            BackColor = Color.Transparent
        };
        Controls.Add(_messageLabel);

        _installButton = new ThemedButton
        {
            Text = "Install",
            Location = new Point(520, 6),
            Size = new Size(74, 28),
            BackColor = Color.FromArgb(196, 17, 48),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            TabStop = false,
            BorderSize = 0,
            BorderColor = Color.FromArgb(196, 17, 48),
            HoverBackColor = Color.FromArgb(215, 25, 58),
            PressedBackColor = Color.FromArgb(150, 12, 38)
        };
        Controls.Add(_installButton);
    }

    public DependencyRow(DependencyCheck check, Action action, bool isChecking = false, string activitySymbol = "", bool isPending = false, bool showActions = true)
        : this()
    {
        UpdateState(check, action, isChecking, activitySymbol, isPending, showActions);
    }

    public void UpdateState(DependencyCheck check, Action action, bool isChecking = false, string activitySymbol = "", bool isPending = false, bool showActions = true)
    {
        var statusText = isChecking
            ? activitySymbol
            : isPending
                ? "\u2022"
                : check.IsSatisfied
                ? "\u2713"
                : "X";
        var statusColor = isChecking
            ? Color.FromArgb(55, 145, 255)
            : isPending
                ? Color.FromArgb(70, 100, 140)
                : InstallerForm.StatusColor(check);

        _statusLabel.Text = statusText;
        _statusLabel.ForeColor = statusColor;
        _nameLabel.Text = check.Name;
        _messageLabel.Text = check.Message;

        _installButton.Enabled = !isChecking && !check.IsSatisfied && check.ActionUri is not null;
        _installButton.Visible = showActions && !check.IsSatisfied && !isChecking && !isPending && check.ActionUri is not null;
        _installButton.Click -= InstallButtonClick;
        _installButton.Tag = action;
        _installButton.Click += InstallButtonClick;
    }

    private static void InstallButtonClick(object? sender, EventArgs e)
    {
        if (sender is Control { Tag: Action action })
        {
            action();
        }
    }
}

internal sealed class DependencyDialog : Form
{
    public DependencyDialog(DependencyCheck check, Image? logo)
    {
        Text = "Vein Dump Manager Dependency";
        ClientSize = new Size(560, 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        Controls.Add(new PictureBox
        {
            Image = logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(22, 24),
            Size = new Size(58, 58)
        });

        Controls.Add(new Label
        {
            Text = $"{check.Name} is {(check.Severity == DependencySeverity.Required ? "required" : "needed for a feature")}.",
            ForeColor = Color.FromArgb(20, 40, 70),
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Location = new Point(100, 28),
            Size = new Size(420, 32)
        });

        Controls.Add(new Label
        {
            Text = check.Message + Environment.NewLine + Environment.NewLine + check.ActionHelp,
            ForeColor = Color.FromArgb(55, 65, 78),
            Location = new Point(102, 68),
            Size = new Size(420, 78)
        });

        var install = new Button
        {
            Text = check.ActionUri is null ? "OK" : "Install",
            DialogResult = check.ActionUri is null ? DialogResult.Cancel : DialogResult.OK,
            Location = new Point(316, 184),
            Size = new Size(100, 34),
            BackColor = Color.FromArgb(0, 102, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(install);

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.Cancel,
            Location = new Point(426, 184),
            Size = new Size(88, 34)
        };
        Controls.Add(close);
        AcceptButton = install;
        CancelButton = close;
    }
}

internal sealed record DependencyCheck(
    string Name,
    bool IsSatisfied,
    DependencySeverity Severity,
    string Message,
    string ActionHelp,
    string? ActionUri);

internal enum DependencySeverity
{
    Required,
    Feature,
    Included
}

internal sealed class RoundedPanel : Panel
{
    public Color FillColor { get; init; }
    public Color BorderColor { get; init; }

    protected override void OnCreateControl()
    {
        BackColor = FillColor;
        base.OnCreateControl();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Color.Black);
        using var fill = new SolidBrush(FillColor);
        using var pen = new Pen(BorderColor);
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        e.Graphics.FillRectangle(fill, rect);
        e.Graphics.DrawRectangle(pen, rect);
    }
}
