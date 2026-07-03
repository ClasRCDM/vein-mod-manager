using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.ComponentModel;

namespace VEIN_Item_And_Container_Modifier;

public sealed partial class MainForm : Form
{
    private static readonly Color AppBack = Color.FromArgb(5, 10, 18);
    private static readonly Color PanelBack = Color.FromArgb(10, 18, 30);
    private static readonly Color InnerBack = Color.FromArgb(3, 11, 20);
    private static readonly Color Border = Color.FromArgb(34, 58, 90);
    private static readonly Color BorderSoft = Color.FromArgb(28, 49, 78);
    private static readonly Color TextMain = Color.White;
    private static readonly Color TextMuted = Color.FromArgb(177, 207, 242);
    private static readonly Color Purple = Color.FromArgb(126, 58, 242);
    private static readonly Color PurpleLight = Color.FromArgb(154, 85, 255);
    private static readonly Color Green = Color.FromArgb(0, 255, 102);
    private static readonly Color Orange = Color.FromArgb(255, 112, 0);
    private static readonly Color Red = Color.FromArgb(255, 87, 87);
    private static readonly Color Cyan = Color.FromArgb(18, 223, 213);
    private const int MaxVisibleComboRows = 14;
    private const int MaxLogLines = 500;
    private static readonly string[] BoolChoices = { "Game Default", "True", "False" };
    private const string SettingsDirectoryName = "VeinModManager";
    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions SettingsJsonOptions = new() { WriteIndented = true };

    private readonly UiConfigState _state = new();
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 1000 };

    private TextBox _gameFolderBox = null!;
    private TextBox _modFolderBox = null!;
    private Label _gameStatus = null!;
    private Label _ue4ssStatus = null!;
    private Label _modStatus = null!;
    private Label _headerSubtitle = null!;
    private Label _unsavedStatus = null!;
    private Panel _contentShell = null!;
    private readonly List<Control> _overviewControls = new();
    private readonly Dictionary<string, Label> _dashboardValues = new(StringComparer.Ordinal);
    private readonly List<string> _recentActivityLines = new();
    private readonly ServerManagerUiState _serverState = new();
    private RichTextBox _dashboardActivity = null!;
    private RichTextBox _log = null!;
    private readonly ToolTip _toolTip = new();
    private ToggleSwitch _toolTipsToggle = null!;
    private Label _toolTipsStatus = null!;
    private RoundedPanel _importDropZone = null!;
    private Label _importConfigPathLabel = null!;
    private RoundedPanel _sidebar = null!;
    private readonly Dictionary<string, Control> _tabPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoundedButton> _tabButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _setupSubPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoundedButton> _setupSubButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _serverSubPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoundedButton> _serverSubButtons = new(StringComparer.Ordinal);
    private readonly List<Control> _serverOverviewControls = new();
    private FlowLayoutPanel _serverTabFlow = null!;
    private Label _serverTypeLabel = null!;
    private RoundedButton? _settingsButton;
    private ThemedComboBox _serverTypeCombo = null!;
    private Panel _windowsServerPanel = null!;
    private Panel _linuxServerPanel = null!;
    private RoundedPanel _windowsServerActionsPanel = null!;
    private RoundedPanel _linuxServerActionsPanel = null!;
    private Panel _serverModePanel = null!;
    private Control _serverManagementPane = null!;
    private Control _serverBackupsPane = null!;
    private Control _serverLogsPane = null!;
    private Control _serverIntegrationsPane = null!;
    private RoundedPanel _linuxHelperPanel = null!;
    private Label _serverStatusValue = null!;
    private Label _configStatusValue = null!;
    private Label _connectionStatusValue = null!;
    private Label _lastBackupValue = null!;
    private RichTextBox _serverManagerLog = null!;
    private ToggleSwitch _serverLogAutoScrollToggle = null!;
    private ToggleSwitch _backupBeforeSaveToggle = null!;
    private ToggleSwitch _backupBeforeUploadToggle = null!;
    private ToggleSwitch _backupBeforeRestartToggle = null!;
    private ListBox _recentBackupsList = null!;
    private ListBox _modParityList = null!;
    private CheckBox _modParityAllowExtraMods = null!;
    private ThemedComboBox _modParityEnforcementCombo = null!;
    private TextBox _modParityKickMessageBox = null!;
    private Label _modParityStatusLabel = null!;
    private readonly List<string> _modParityFolders = new();
    private string? _lastModParityPackageZip;
    private TextBox _windowsServerFolderBox = null!;
    private TextBox _windowsSteamCmdBox = null!;
    private TextBox _windowsServerNameBox = null!;
    private TextBox _windowsDescriptionBox = null!;
    private TextBox _windowsSessionNameBox = null!;
    private TextBox _windowsServerPasswordBox = null!;
    private TextBox _windowsMapSelectionBox = null!;
    private TextBox _windowsGamePortBox = null!;
    private TextBox _windowsQueryPortBox = null!;
    private TextBox _windowsMaxPlayersBox = null!;
    private ToggleSwitch _windowsEnableRconToggle = null!;
    private TextBox _windowsRconPortBox = null!;
    private TextBox _windowsRconPasswordBox = null!;
    private ToggleSwitch _windowsEnableHttpApiToggle = null!;
    private TextBox _windowsHttpApiPortBox = null!;
    private TextBox _windowsSuperAdminsBox = null!;
    private TextBox _linuxHostBox = null!;
    private TextBox _linuxPortBox = null!;
    private TextBox _linuxUsernameBox = null!;
    private ThemedComboBox _linuxAuthTypeCombo = null!;
    private TextBox _linuxSshKeyBox = null!;
    private TextBox _linuxPasswordBox = null!;
    private TextBox _linuxRemoteServerPathBox = null!;
    private TextBox _linuxRemoteConfigPathBox = null!;
    private string? _lastLinuxHelperZip;
    private DateTime? _lastConfigSaveAt;
    private DateTime? _lastConfigBackupAt;
    private Process? _windowsServerProcess;
    private bool _linuxConnectionTested;

    private ThemedComboBox _categoryCombo = null!;
    private ToggleSwitch _categoryEnabled = null!;
    private FlowLayoutPanel _categoryFields = null!;
    private readonly Dictionary<string, Control> _categoryInputs = new(StringComparer.OrdinalIgnoreCase);

    private ThemedComboBox _itemCategoryCombo = null!;
    private TextBox _itemSearchBox = null!;
    private ThemedComboBox _itemCombo = null!;
    private CheckBox _itemAdvanced = null!;
    private Label _itemClassLabel = null!;
    private Label _itemCdoLabel = null!;
    private FlowLayoutPanel _itemFields = null!;
    private readonly Dictionary<string, (CheckBox DefaultCheck, Control Input)> _itemInputs = new(StringComparer.OrdinalIgnoreCase);

    private ModData? _modData;
    private bool _loadingUi;
    private bool _hasUnsavedChanges;

    public MainForm()
    {
        InitializeComponent();
        if (IsDesignerHosted) return;

        ConfigureToolTips();
        if (LoadWindowIcon() is { } windowIcon) Icon = windowIcon;
        BuildUi();

        LoadPathSettings();
        AutoDetectPaths(log: true);
        LoadModFromPath();

        _statusTimer.Tick += (_, _) => UpdateStatuses();
        _statusTimer.Start();
        UpdateStatuses();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusTimer.Stop();
            _statusTimer.Dispose();
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.None;
        Text = "Vein Mod Manager";
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(1180, 720);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppBack;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 11F, FontStyle.Regular);
        DoubleBuffered = true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (IsDesignerHosted) return;

        UseDarkTitleBar(Handle);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!e.Cancel && _hasUnsavedChanges)
        {
            var result = MessageBox.Show(
                this,
                "You have unsaved config changes. Choose Yes to save, No to discard them, or Cancel to keep editing.",
                "Save changes before closing?",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else if (result == DialogResult.Yes && (!TrySaveConfig() || _hasUnsavedChanges))
            {
                e.Cancel = true;
                MessageBox.Show(
                    this,
                    "The config could not be saved, so Vein Mod Manager will stay open. Check the log for details.",
                    "Save failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!IsDesignerHosted || Controls.Count > 0) return;

        DrawDesignerPreview(e.Graphics);
    }

    private static readonly bool IsDesignerHosted = DetectDesignerHosted();

    private static bool DetectDesignerHosted()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return true;

        var processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "";
        return processName.Contains("devenv", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("DesignToolsServer", StringComparison.OrdinalIgnoreCase);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsDesignerHosted) return;

        ApplyResponsiveLayout();
    }

    private void BuildUi()
    {
        Controls.Clear();
        BuildSidebar();
        BuildHeader();
        BuildStatusCards();
        BuildServerTopStatusCards();
        BuildTabs();
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (_sidebar != null)
        {
            _sidebar.Height = Math.Max(0, ClientSize.Height - 40);
        }

        if (_settingsButton != null)
        {
            _settingsButton.Left = Math.Max(220, ClientSize.Width - 84);
        }

        if (_headerSubtitle != null)
        {
            _headerSubtitle.Width = Math.Max(360, ClientSize.Width - 340);
        }

        if (_contentShell != null)
        {
            var dashboardSelected = _tabPages.TryGetValue("Dashboard", out var dashboardPage) && dashboardPage.Visible;
            var setupSelected = _tabPages.TryGetValue("Setup", out var setupPage) && setupPage.Visible;
            var serverSelected = _tabPages.TryGetValue("Server Manager", out var serverPage) && serverPage.Visible;
            _contentShell.Top = serverSelected ? 222 : 116;
            _contentShell.Width = Math.Max(720, ClientSize.Width - 260);
            _contentShell.Height = Math.Max(420, ClientSize.Height - _contentShell.Top - 40);

            foreach (var page in _tabPages.Values)
            {
                page.Width = _contentShell.Width;
                page.Height = _contentShell.Height;
            }

            ResizeServerManagerLayout();
        }
    }

    private void ResizeServerManagerLayout()
    {
        if (_serverOverviewControls.Count > 0)
        {
            var x = 220;
            var y = 116;
            var gap = 18;
            var available = Math.Max(900, ClientSize.Width - x - 40);
            var cardWidth = Math.Max(210, Math.Min(250, (available - gap * 3) / 4));

            for (var index = 0; index < _serverOverviewControls.Count; index++)
            {
                var card = _serverOverviewControls[index];
                card.Left = x + (cardWidth + gap) * index;
                card.Top = y;
                card.Width = cardWidth;
            }
        }

        if (!_tabPages.TryGetValue("Server Manager", out var serverPage) || serverPage is not Control panel)
        {
            return;
        }

        var innerWidth = Math.Max(900, panel.Width - 56);
        if (_serverTypeCombo != null)
        {
            _serverTypeCombo.Width = 310;
            _serverTypeCombo.Left = Math.Max(610, panel.Width - _serverTypeCombo.Width - 30);
            ConfigureComboDropDown(_serverTypeCombo);
        }

        if (_serverTypeLabel != null && _serverTypeCombo != null)
        {
            _serverTypeLabel.Left = _serverTypeCombo.Left - _serverTypeLabel.Width - 16;
        }

        if (_serverTabFlow != null)
        {
            _serverTabFlow.Width = innerWidth;
        }

        foreach (var page in _serverSubPages.Values)
        {
            page.Width = innerWidth;
            page.Height = Math.Max(300, panel.Height - page.Top - 28);
        }

        if (_serverModePanel != null)
        {
            _serverModePanel.Width = Math.Max(930, innerWidth);
            _serverModePanel.Height = 300;
        }

        var modeWidth = _serverModePanel?.Width ?? innerWidth;
        if (_windowsServerPanel != null)
        {
            _windowsServerPanel.Width = Math.Max(930, modeWidth);
            _windowsServerPanel.Height = 300;
        }

        if (_linuxServerPanel != null)
        {
            _linuxServerPanel.Width = Math.Max(930, modeWidth);
            _linuxServerPanel.Height = 300;
        }
    }

    private void DrawDesignerPreview(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(AppBack);

        DrawPreviewPanel(graphics, new Rectangle(20, 20, 172, 740), 14, PanelBack, BorderSoft);
        DrawPreviewLogo(graphics, new Rectangle(46, 74, 122, 122));
        DrawPreviewLine(graphics, 38, 230, 136);
        DrawPreviewTab(graphics, "Setup", 42, 258, 128, selected: true);
        DrawPreviewTab(graphics, "Server Manager", 42, 310, 128, selected: false);
        DrawPreviewTab(graphics, "Log", 42, 362, 128, selected: false);

        DrawPreviewText(graphics, "Vein Manager", 220, 72, 28, FontStyle.Bold, TextMain);
        DrawPreviewText(graphics, "Modify VEIN item, backpack, vehicle, and container values without editing config files.", 222, 116, 13, FontStyle.Regular, TextMuted);
        DrawPreviewPanel(graphics, new Rectangle(1196, 58, 52, 52), 12, InnerBack, BorderSoft);
        DrawPreviewText(graphics, "\uE713", 1211, 72, 17, FontStyle.Regular, TextMain, "Segoe MDL2 Assets");

        DrawPreviewStatusCard(graphics, "Game", "Closed", "VEIN process", Orange, 220, 148);
        DrawPreviewStatusCard(graphics, "UE4SS", "Found", "Detected in game folder", Green, 498, 148);
        DrawPreviewStatusCard(graphics, "Mod", "Found", "ItemAndContainerModifier", Green, 776, 148);

        DrawPreviewPanel(graphics, new Rectangle(220, 222, 1020, 570), 12, PanelBack, Border);
        DrawPreviewText(graphics, "Setup", 256, 254, 20, FontStyle.Bold, TextMain);
        DrawPreviewButton(graphics, "Readme", 1054, 250, 126, 44, main: false);
        DrawPreviewText(graphics, "Select your VEIN install and the UE4SS mod folder. The editor writes generated overrides only.", 256, 292, 13, FontStyle.Regular, TextMuted);
        DrawPreviewText(graphics, "Game folder", 256, 342, 13, FontStyle.Bold, TextMuted);
        DrawPreviewTextBox(graphics, @"C:\Program Files (x86)\Steam\steamapps\common\Vein", 256, 366, 660);
        DrawPreviewButton(graphics, "Browse", 930, 362, 110, 44, main: false);
        DrawPreviewButton(graphics, "Auto Detect", 1054, 362, 126, 44, main: false);
        DrawPreviewText(graphics, "Mod folder", 256, 438, 13, FontStyle.Bold, TextMuted);
        DrawPreviewTextBox(graphics, @"C:\Program Files (x86)\Steam\steamapps\common\Vein\Vein\Binaries\Win64\ue4ss\Mods\ItemAndContainerModifier", 256, 462, 660);
        DrawPreviewButton(graphics, "Browse", 930, 458, 110, 44, main: false);
        DrawPreviewButton(graphics, "Open Folder", 1054, 458, 126, 44, main: false);
        DrawPreviewText(graphics, "No unsaved changes (7 edits)", 256, 616, 13, FontStyle.Bold, Cyan);
        DrawPreviewButton(graphics, "Save Config", 256, 652, 190, 54, main: true);
        DrawPreviewButton(graphics, "Backup Now", 462, 652, 170, 54, main: false);
        DrawPreviewButton(graphics, "Launch VEIN", 652, 652, 170, 54, main: false);
    }

    private static void DrawPreviewLogo(Graphics graphics, Rectangle bounds)
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.png");
        if (!File.Exists(logoPath))
        {
            logoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "vein-logo.png"));
        }

        if (File.Exists(logoPath))
        {
            using var image = Image.FromFile(logoPath);
            graphics.DrawImage(image, bounds);
            return;
        }

        DrawPreviewPanel(graphics, bounds, 2, Color.Black, BorderSoft);
        DrawPreviewText(graphics, "VEIN", bounds.Left + 18, bounds.Top + 42, 24, FontStyle.Bold, TextMain);
    }

    private static void DrawPreviewStatusCard(Graphics graphics, string title, string value, string subtitle, Color valueColor, int x, int y)
    {
        DrawPreviewPanel(graphics, new Rectangle(x, y, 260, 92), 10, PanelBack, BorderSoft);
        DrawPreviewText(graphics, title, x + 24, y + 14, 12, FontStyle.Regular, TextMuted);
        DrawPreviewText(graphics, value, x + 24, y + 34, 20, FontStyle.Regular, valueColor);
        DrawPreviewText(graphics, subtitle, x + 24, y + 62, 9, FontStyle.Regular, TextMuted);
    }

    private static void DrawPreviewTab(Graphics graphics, string text, int x, int y, int width, bool selected)
    {
        DrawPreviewPanel(graphics, new Rectangle(x, y, width, 44), 14, selected ? Purple : InnerBack, selected ? PurpleLight : BorderSoft);
        DrawPreviewText(graphics, text, x, y + 13, 11, FontStyle.Bold, TextMain, width: width, alignment: StringAlignment.Center);
    }

    private static void DrawPreviewTextBox(Graphics graphics, string text, int x, int y, int width)
    {
        DrawPreviewPanel(graphics, new Rectangle(x, y, width, 36), 8, InnerBack, Border);
        DrawPreviewText(graphics, text, x + 10, y + 8, 11, FontStyle.Regular, TextMain, width: width - 20);
    }

    private static void DrawPreviewButton(Graphics graphics, string text, int x, int y, int width, int height, bool main)
    {
        DrawPreviewPanel(graphics, new Rectangle(x, y, width, height), 10, main ? Purple : Color.FromArgb(32, 49, 75), main ? PurpleLight : Color.FromArgb(54, 78, 116));
        DrawPreviewText(graphics, text, x, y + (height - 18) / 2, 11, FontStyle.Bold, TextMain, width: width, alignment: StringAlignment.Center);
    }

    private static void DrawPreviewLine(Graphics graphics, int x, int y, int width)
    {
        using var pen = new Pen(BorderSoft);
        graphics.DrawLine(pen, x, y, x + width, y);
    }

    private static void DrawPreviewPanel(Graphics graphics, Rectangle bounds, int radius, Color fillColor, Color borderColor)
    {
        using var path = RoundedPanel.RoundedRect(bounds, radius);
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(borderColor);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
    }

    private static void DrawPreviewText(
        Graphics graphics,
        string text,
        int x,
        int y,
        float size,
        FontStyle style,
        Color color,
        string family = "Segoe UI",
        int width = 900,
        StringAlignment alignment = StringAlignment.Near)
    {
        using var font = new Font(family, size, style);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(text, font, brush, new RectangleF(x, y, width, 120), format);
    }

    private void ConfigureToolTips()
    {
        _toolTip.Active = true;
        _toolTip.AutoPopDelay = 12000;
        _toolTip.InitialDelay = 450;
        _toolTip.ReshowDelay = 120;
        _toolTip.ShowAlways = true;
        _toolTip.OwnerDraw = true;
        _toolTip.BackColor = Color.Black;
        _toolTip.ForeColor = TextMain;
        _toolTip.Popup += ToolTip_Popup;
        _toolTip.Draw += ToolTip_Draw;
    }

    private void ToolTip_Popup(object? sender, PopupEventArgs e)
    {
        const int maxWidth = 420;
        const int horizontalPadding = 18;
        const int verticalPadding = 12;
        var text = e.AssociatedControl == null ? string.Empty : _toolTip.GetToolTip(e.AssociatedControl);
        var proposedSize = new Size(maxWidth - horizontalPadding, int.MaxValue);
        var measured = TextRenderer.MeasureText(text, Font, proposedSize, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        e.ToolTipSize = new Size(measured.Width + horizontalPadding, measured.Height + verticalPadding);
    }

    private void ToolTip_Draw(object? sender, DrawToolTipEventArgs e)
    {
        using var background = new SolidBrush(Color.Black);
        using var border = new Pen(Border);
        e.Graphics.FillRectangle(background, e.Bounds);
        e.Graphics.DrawRectangle(border, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));

        var textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 6, e.Bounds.Width - 16, e.Bounds.Height - 12);
        TextRenderer.DrawText(e.Graphics, e.ToolTipText, Font, textBounds, Color.White, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
    }

    private void AddTip(Control control, string text)
    {
        _toolTip.SetToolTip(control, text);
    }

    private void SetToolTipsEnabled(bool enabled)
    {
        _toolTip.Active = enabled;

        if (_toolTipsStatus != null)
        {
            _toolTipsStatus.Text = enabled ? "ON" : "OFF";
            _toolTipsStatus.ForeColor = enabled ? Green : Red;
        }

        if (_toolTipsToggle != null)
        {
            _toolTipsToggle.Checked = enabled;
        }

        Log("Tooltips " + (enabled ? "enabled." : "disabled."));
    }

    private void BuildSidebar()
    {
        _sidebar = NewPanel(18, 20, 20, 172, 740);
        Controls.Add(_sidebar);

        var logo = new PictureBox
        {
            Left = 24,
            Top = 24,
            Width = 124,
            Height = 124,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = InnerBack
        };

        try
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.png");
            if (!File.Exists(logoPath))
            {
                logoPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "vein-logo.png");
            }
            if (File.Exists(logoPath))
            {
                using var stream = File.OpenRead(logoPath);
                using var image = Image.FromStream(stream);
                logo.Image = new Bitmap(image);
            }
        }
        catch
        {
        }

        _sidebar.Controls.Add(logo);
        _sidebar.Controls.Add(Line(18, 178, 136));
    }

    private static Icon? LoadWindowIcon()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.ico"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "vein-logo.ico"))
        };

        var iconPath = candidates.FirstOrDefault(File.Exists);
        return iconPath == null ? null : new Icon(iconPath);
    }

    private void BuildHeader()
    {
        Controls.Add(MakeLabel("Vein Manager", 220, 30, 610, 42, 28, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, AppBack));
        _headerSubtitle = MakeLabel("Modify VEIN item, backpack, vehicle, and container values without editing config files.", 222, 74, 880, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, AppBack);
        Controls.Add(_headerSubtitle);

        _settingsButton = MakeButton("\uE713", 1196, 28, 52, 52, () => ShowTab("Settings"));
        _settingsButton.AccessibleName = "Settings";
        _settingsButton.Font = new Font("Segoe MDL2 Assets", 17F, FontStyle.Regular);
        _settingsButton.FillColor = InnerBack;
        _settingsButton.HoverColor = Color.FromArgb(18, 31, 50);
        _settingsButton.BorderColor = BorderSoft;
        AddTip(_settingsButton, "Open settings, config import, and tooltip options.");
        Controls.Add(_settingsButton);
    }

    private void BuildStatusCards()
    {
        _overviewControls.Clear();
        var x = 220;
        var y = 116;
        var w = 260;
        var h = 92;
        var gap = 18;

        var game = NewStatCard("Game", "Closed", "VEIN process", Orange, x, y, w, h);
        _gameStatus = (Label)game.Tag!;
        _overviewControls.Add(game);
        Controls.Add(game);

        var ue4ss = NewStatCard("UE4SS", "Missing", "Detected in game folder", Orange, x + w + gap, y, w, h);
        _ue4ssStatus = (Label)ue4ss.Tag!;
        _overviewControls.Add(ue4ss);
        Controls.Add(ue4ss);

        var mod = NewStatCard("Mod", "Missing", "ItemAndContainerModifier", Orange, x + (w + gap) * 2, y, w, h);
        _modStatus = (Label)mod.Tag!;
        _overviewControls.Add(mod);
        Controls.Add(mod);
    }

    private void BuildServerTopStatusCards()
    {
        _serverOverviewControls.Clear();
        var x = 220;
        var y = 116;
        var w = 220;
        var h = 92;
        var gap = 18;

        var server = NewStatCard("Server Status", "Stopped", "Local/remote process", Orange, x, y, w, h);
        var config = NewStatCard("Config Status", "Not Saved", "No server config write", Orange, x + w + gap, y, w, h);
        var connection = NewStatCard("Connection Status", "Not Connected", "Test required", Orange, x + (w + gap) * 2, y, w, h);
        var backup = NewStatCard("Last Backup", "None", "Backup before changes", TextMuted, x + (w + gap) * 3, y, w, h);

        _serverStatusValue = (Label)server.Tag!;
        _configStatusValue = (Label)config.Tag!;
        _connectionStatusValue = (Label)connection.Tag!;
        _lastBackupValue = (Label)backup.Tag!;

        foreach (var card in new[] { server, config, connection, backup })
        {
            card.Visible = false;
            _serverOverviewControls.Add(card);
            Controls.Add(card);
        }
    }

    private void BuildTabs()
    {
        _tabPages.Clear();
        _tabButtons.Clear();

        var shell = new Panel
        {
            Left = 220,
            Top = 222,
            Width = 1020,
            Height = 570,
            BackColor = AppBack
        };
        _contentShell = shell;

        _tabPages["Dashboard"] = BuildDashboardTab();
        _tabPages["Setup"] = BuildSetupTab();
        _tabPages["Settings"] = BuildSettingsTab();
        _tabPages["Server Manager"] = BuildServerManagerTab();
        _tabPages["Log"] = BuildLogTab();

        var y = 214;
        foreach (var title in new[] { "Dashboard", "Setup", "Server Manager", "Log" })
        {
            var button = NewSidebarTabButton(title, 18, y);
            button.Click += (_, _) => ShowTab(title);
            _tabButtons[title] = button;
            _sidebar.Controls.Add(button);
            y += 52;
        }

        foreach (var page in _tabPages.Values)
        {
            page.Visible = false;
            shell.Controls.Add(page);
        }

        Controls.Add(shell);
        ShowTab("Dashboard");
    }

    private void ShowTab(string title)
    {
        foreach (var (name, page) in _tabPages)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            page.Visible = selected;
            if (selected)
            {
                page.BringToFront();
            }
        }

        foreach (var (name, button) in _tabButtons)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            button.FillColor = selected ? Purple : InnerBack;
            button.HoverColor = selected ? PurpleLight : Color.FromArgb(18, 31, 50);
            button.BorderColor = selected ? PurpleLight : BorderSoft;
            button.Invalidate();
        }

        if (_settingsButton != null)
        {
            var settingsSelected = title.Equals("Settings", StringComparison.Ordinal);
            _settingsButton.FillColor = settingsSelected ? Purple : InnerBack;
            _settingsButton.HoverColor = settingsSelected ? PurpleLight : Color.FromArgb(18, 31, 50);
            _settingsButton.BorderColor = settingsSelected ? PurpleLight : BorderSoft;
            _settingsButton.Invalidate();
        }

        var dashboardSelected = title.Equals("Dashboard", StringComparison.Ordinal);
        var setupSelected = title.Equals("Setup", StringComparison.Ordinal);
        var serverSelected = title.Equals("Server Manager", StringComparison.Ordinal);
        _headerSubtitle.Visible = dashboardSelected || setupSelected;
        foreach (var control in _overviewControls)
        {
            control.Visible = false;
        }

        foreach (var control in _serverOverviewControls)
        {
            control.Visible = serverSelected;
        }

        if (_contentShell != null)
        {
            _contentShell.Top = serverSelected ? 222 : 116;
            _contentShell.Height = Math.Max(420, ClientSize.Height - _contentShell.Top - 40);
        }

        ApplyResponsiveLayout();
        RefreshDashboard();
    }

    private RoundedPanel BuildDashboardTab()
    {
        var panel = NewPanel(12, 0, 0, 1020, 570);
        panel.Controls.Add(MakeLabel("Dashboard", 28, 24, 360, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        panel.Controls.Add(MakeLabel("Overview, loaded data, config activity, and server status at a glance.", 30, 62, 760, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        AddDashboardMetric(panel, "Game Status", "Closed", "VEIN process", "GameStatus", 28, 112);
        AddDashboardMetric(panel, "UE4SS Status", "Missing", "Detected in game folder", "Ue4ssStatus", 274, 112);
        AddDashboardMetric(panel, "Mod Status", "Missing", "ItemAndContainerModifier", "ModStatus", 520, 112);
        AddDashboardMetric(panel, "Server Summary", "No data yet", "Server Manager status", "ServerSummary", 766, 112);

        AddDashboardMetric(panel, "Loaded Categories", "0", "Real count from mod data", "LoadedCategories", 28, 214);
        AddDashboardMetric(panel, "Loaded Entries", "0", "Real item/container entries", "LoadedEntries", 274, 214);
        AddDashboardMetric(panel, "Unsaved Edits", "0", "Generated config edits", "UnsavedEdits", 520, 214);
        AddDashboardMetric(panel, "Last Config Save", "Never", "Updates after Save Config", "LastSave", 766, 214);

        AddDashboardMetric(panel, "Last Backup", "Never", "Updates after backups", "LastBackup", 28, 316);
        AddDashboardChart(panel, "Config Edits", "No data yet", "ConfigEditsChart", 274, 316, 218, 96);
        AddDashboardChart(panel, "Backups Over Time", "No data yet", "BackupsChart", 520, 316, 218, 96);
        AddDashboardChart(panel, "Status History", "No data yet", "StatusHistoryChart", 766, 316, 218, 96);

        AddDashboardChart(panel, "Loaded Data Summary", "No mod data loaded yet", "LoadedSummaryChart", 28, 434, 300, 112);
        AddDashboardChart(panel, "Server Activity Summary", "No server activity yet", "ServerActivityChart", 352, 434, 300, 112);

        _dashboardActivity = new RichTextBox
        {
            Left = 28,
            Top = 570,
            Width = 948,
            Height = 28,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            Font = new Font("Consolas", 9.5F, FontStyle.Regular),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.None,
            WordWrap = false,
            Text = "Recent activity: No data yet"
        };
        panel.Controls.Add(_dashboardActivity);
        return panel;
    }

    private void AddDashboardMetric(Control parent, string title, string value, string sub, string key, int x, int y)
    {
        var card = NewStatCard(title, value, sub, TextMuted, x, y, 218, 86);
        _dashboardValues[key] = (Label)card.Tag!;
        parent.Controls.Add(card);
    }

    private void AddDashboardChart(Control parent, string title, string emptyText, string key, int x, int y, int w, int h)
    {
        var chart = NewPanel(12, x, y, w, h);
        chart.Controls.Add(MakeLabel(title, 20, 12, w - 40, 24, 12, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        var value = MakeLabel(emptyText, 20, 46, w - 40, h - 58, 11, FontStyle.Regular, TextMuted, ContentAlignment.MiddleCenter, PanelBack);
        chart.Controls.Add(value);
        _dashboardValues[key] = value;
        parent.Controls.Add(chart);
    }

    private RoundedPanel BuildSetupTab()
    {
        var panel = NewPanel(12, 0, 0, 1020, 570);
        _setupSubPages.Clear();
        _setupSubButtons.Clear();

        var x = 28;
        foreach (var (title, width) in new[] { ("Setup", 136), ("Item Editor", 170), ("Defaults", 150) })
        {
            var button = NewTabButton(title, x, 18, width);
            button.Click += (_, _) => ShowSetupSubTab(title);
            _setupSubButtons[title] = button;
            panel.Controls.Add(button);
            x += width + 8;
        }

        _setupSubPages["Setup"] = BuildSetupDetailsPane();
        _setupSubPages["Item Editor"] = BuildItemTab();
        _setupSubPages["Defaults"] = BuildDefaultsTab();

        foreach (var page in _setupSubPages.Values)
        {
            page.Top = 76;
            page.Visible = false;
            panel.Controls.Add(page);
        }

        ShowSetupSubTab("Setup");
        return panel;
    }

    private void ShowSetupSubTab(string title)
    {
        foreach (var (name, page) in _setupSubPages)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            page.Visible = selected;
            if (selected) page.BringToFront();
        }

        foreach (var (name, button) in _setupSubButtons)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            button.FillColor = selected ? Purple : InnerBack;
            button.HoverColor = selected ? PurpleLight : Color.FromArgb(18, 31, 50);
            button.BorderColor = selected ? PurpleLight : BorderSoft;
            button.Invalidate();
        }
    }

    private RoundedPanel BuildSetupDetailsPane()
    {
        var panel = NewContentPanel();
        panel.Controls.Add(MakeLabel("Setup", 28, 24, 300, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        var readme = MakeButton("Readme", 834, 20, 126, 44, ShowReadmePopup);
        AddTip(readme, "Open the quick setup steps without leaving the manager.");
        panel.Controls.Add(readme);
        panel.Controls.Add(MakeLabel("Select your VEIN install and the UE4SS mod folder. The editor writes generated overrides only.", 30, 62, 760, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        panel.Controls.Add(MakeLabel("Game folder", 30, 112, 180, 28, 13, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _gameFolderBox = NewTextBox(30, 144, 660, 36);
        panel.Controls.Add(_gameFolderBox);
        var browseGame = MakeButton("Browse", 710, 140, 110, 44, BrowseGameFolder);
        var autoDetect = MakeButton("Auto Detect", 834, 140, 126, 44, () => AutoDetectPaths(log: true));
        AddTip(_gameFolderBox, "The main VEIN Steam folder. Auto Detect usually finds this for you.");
        AddTip(browseGame, "Pick the VEIN Steam folder manually if Auto Detect cannot find it.");
        AddTip(autoDetect, "Search Steam libraries for VEIN and fill the expected UE4SS mod path.");
        panel.Controls.Add(browseGame);
        panel.Controls.Add(autoDetect);

        panel.Controls.Add(MakeLabel("Mod folder", 30, 208, 180, 28, 13, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _modFolderBox = NewTextBox(30, 240, 660, 36);
        panel.Controls.Add(_modFolderBox);
        var browseMod = MakeButton("Browse", 710, 236, 110, 44, BrowseModFolder);
        var openMod = MakeButton("Open Folder", 834, 236, 126, 44, OpenModFolder);
        AddTip(_modFolderBox, "The UE4SS ItemAndContainerModifier folder where Scripts\\ui_config.lua is saved.");
        AddTip(browseMod, "Pick the ItemAndContainerModifier folder manually.");
        AddTip(openMod, "Open the selected mod folder in File Explorer.");
        panel.Controls.Add(browseMod);
        panel.Controls.Add(openMod);

        _unsavedStatus = MakeLabel("No unsaved changes", 30, 340, 420, 32, 13, FontStyle.Bold, Cyan, ContentAlignment.MiddleLeft, PanelBack);
        panel.Controls.Add(_unsavedStatus);
        var saveConfig = MakeButton("Save Config", 30, 386, 190, 54, SaveConfig, main: true);
        var backupNow = MakeButton("Backup Now", 240, 386, 170, 54, BackupNow);
        var launchVein = MakeButton("Launch VEIN", 430, 386, 170, 54, LaunchVein);
        AddTip(saveConfig, "Write your generated values to Scripts\\ui_config.lua. Restart VEIN after saving.");
        AddTip(backupNow, "Create a backup of config.lua, ui_config.lua, and category files.");
        AddTip(launchVein, "Start VEIN from the detected Steam folder.");
        panel.Controls.Add(saveConfig);
        panel.Controls.Add(backupNow);
        panel.Controls.Add(launchVein);

        return panel;
    }

    private RoundedPanel BuildSettingsTab()
    {
        var panel = NewContentPanel();
        panel.Controls.Add(MakeLabel("Settings", 28, 24, 300, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        panel.Controls.Add(MakeLabel("Import a generated config file, open the saved config folder, and control beginner help.", 30, 62, 820, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        var configCard = NewPanel(14, 30, 112, 930, 248);
        configCard.BackColor = PanelBack;
        WireConfigImportDrop(configCard);
        panel.Controls.Add(configCard);

        configCard.Controls.Add(MakeLabel("Configuration file", 28, 22, 260, 28, 15, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        configCard.Controls.Add(MakeLabel("Drop ui_config.lua below. The manager installs it into the correct Scripts folder automatically.", 30, 54, 820, 24, 12, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        _importDropZone = NewPanel(12, 30, 90, 870, 92);
        _importDropZone.FillColor = InnerBack;
        _importDropZone.BorderColor = Border;
        _importDropZone.BackColor = PanelBack;
        WireConfigImportDrop(_importDropZone);
        configCard.Controls.Add(_importDropZone);

        var dropTitle = MakeLabel("Drop ui_config.lua here", 20, 16, 830, 28, 14, FontStyle.Bold, TextMain, ContentAlignment.MiddleCenter, InnerBack);
        var dropHint = MakeLabel("It will be copied to ItemAndContainerModifier\\Scripts\\ui_config.lua and the old file is backed up first.", 20, 44, 830, 22, 10.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleCenter, InnerBack);
        _importConfigPathLabel = MakeLabel("Waiting for file", 20, 66, 830, 18, 8.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleCenter, InnerBack);
        WireConfigImportDrop(dropTitle);
        WireConfigImportDrop(dropHint);
        WireConfigImportDrop(_importConfigPathLabel);
        _importDropZone.Controls.Add(dropTitle);
        _importDropZone.Controls.Add(dropHint);
        _importDropZone.Controls.Add(_importConfigPathLabel);

        var loadCurrent = MakeButton("Load Current", 30, 192, 150, 42, LoadCurrentConfigFile);
        var openScripts = MakeButton("Open Config Folder", 200, 192, 190, 42, OpenConfigFolder);
        AddTip(loadCurrent, "Reload the ui_config.lua already installed in the selected mod folder.");
        AddTip(openScripts, "Open the Scripts folder that contains ui_config.lua.");
        AddTip(_importDropZone, "Drop ui_config.lua here. It installs automatically to the selected ItemAndContainerModifier\\Scripts folder.");
        configCard.Controls.Add(loadCurrent);
        configCard.Controls.Add(openScripts);

        var helpCard = NewPanel(14, 30, 376, 930, 104);
        helpCard.BackColor = PanelBack;
        panel.Controls.Add(helpCard);

        helpCard.Controls.Add(MakeLabel("Tooltips", 28, 24, 180, 34, 16, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        _toolTipsToggle = new ToggleSwitch
        {
            Left = 790,
            Top = 26,
            Width = 56,
            Height = 28,
            BackColor = PanelBack,
            Checked = true,
            OnColor = Color.FromArgb(8, 84, 54),
            OnColor2 = Color.FromArgb(10, 135, 82),
            OffColor = Color.FromArgb(82, 20, 28),
            OffColor2 = Color.FromArgb(122, 32, 42)
        };
        _toolTipsToggle.CheckedChanged += (_, _) => SetToolTipsEnabled(_toolTipsToggle.Checked);
        helpCard.Controls.Add(_toolTipsToggle);

        _toolTipsStatus = MakeLabel("ON", 858, 24, 48, 32, 11, FontStyle.Bold, Green, ContentAlignment.MiddleCenter, PanelBack);
        helpCard.Controls.Add(_toolTipsStatus);
        helpCard.Controls.Add(MakeLabel("Turn hover help on for first-time modders, or off once you know the workflow.", 30, 62, 760, 24, 12, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        AddTip(_toolTipsToggle, "Green means tooltips are on. Red means tooltips are off.");

        return panel;
    }

    private RoundedPanel BuildItemTab()
    {
        var panel = NewContentPanel();
        panel.Controls.Add(MakeLabel("Item Editor", 28, 24, 300, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        panel.Controls.Add(MakeLabel("Edit one item or container, or apply a quick preset as a starting point.", 30, 62, 760, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        var itemOverridesPane = BuildItemOverridesPane();
        itemOverridesPane.Top = 104;
        panel.Controls.Add(itemOverridesPane);

        _itemCategoryCombo.SelectedIndex = 0;
        return panel;
    }

    private RoundedPanel BuildDefaultsTab()
    {
        var panel = NewContentPanel();
        panel.Controls.Add(MakeLabel("Defaults", 28, 24, 300, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        panel.Controls.Add(MakeLabel("Set simple defaults for a whole category. Fields change based on what the category supports.", 30, 62, 760, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        var defaultsPane = BuildDefaultsPane();
        defaultsPane.Top = 104;
        panel.Controls.Add(defaultsPane);

        _categoryCombo.SelectedIndex = 0;
        return panel;
    }

    private RoundedPanel BuildDefaultsPane()
    {
        var pane = NewPanel(12, 30, 158, 930, 382);
        pane.BackColor = PanelBack;

        _categoryCombo = NewCombo(28, 24, 280, CategoryNames.Ordered);
        _categoryCombo.SelectedIndexChanged += (_, _) => RefreshCategoryEditor();
        AddTip(_categoryCombo, "Choose the group you want to edit. Vehicles and containers use Max Weight; item groups use item properties.");
        pane.Controls.Add(_categoryCombo);

        var card = NewPanel(14, 28, 76, 874, 270);
        card.BackColor = PanelBack;
        pane.Controls.Add(card);

        card.Controls.Add(MakeLabel("Enable this category", 28, 20, 280, 34, 16, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        _categoryEnabled = new ToggleSwitch { Left = 324, Top = 22, Width = 84, Height = 34, BackColor = PanelBack };
        _categoryEnabled.CheckedChanged += (_, _) => MarkUnsaved();
        AddTip(_categoryEnabled, "Turn this whole category on or off in the generated config.");
        card.Controls.Add(_categoryEnabled);

        _categoryFields = NewFieldFlow(28, 72, 818, 128);
        _categoryFields.AutoScroll = true;
        card.Controls.Add(_categoryFields);

        card.Controls.Add(MakeButton("Save Category Default", 28, 216, 210, 42, SaveCategoryDefault, main: true));
        card.Controls.Add(MakeButton("Reset This Category", 258, 216, 190, 42, ResetCategory));
        var resetAll = MakeButton("Reset All Defaults", 468, 216, 190, 42, PresetResetToDefaults);
        AddTip(resetAll, "Clear all generated category defaults, item overrides, and container/vehicle overrides.");
        card.Controls.Add(resetAll);

        return pane;
    }

    private RoundedPanel BuildItemOverridesPane()
    {
        var pane = NewPanel(12, 30, 158, 930, 382);
        pane.BackColor = PanelBack;

        _itemCategoryCombo = NewCombo(28, 24, 220, CategoryNames.Ordered);
        _itemCategoryCombo.SelectedIndexChanged += (_, _) => RefreshItemList();
        AddTip(_itemCategoryCombo, "Pick the item group to search inside.");
        pane.Controls.Add(_itemCategoryCombo);

        var itemSearchBox = NewTextBox(268, 24, 230, 36);
        itemSearchBox.CenteredPlaceholderText = "Search item name";
        itemSearchBox.TextAlign = HorizontalAlignment.Center;
        itemSearchBox.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
        _itemSearchBox = itemSearchBox;
        _itemSearchBox.TextChanged += (_, _) => RefreshItemList();
        AddTip(_itemSearchBox, "Type part of an item name or raw class name to filter the list.");
        pane.Controls.Add(_itemSearchBox);

        _itemCombo = NewCombo(520, 24, 280, Array.Empty<string>());
        _itemCombo.SelectedIndexChanged += (_, _) => RefreshItemEditor();
        AddTip(_itemCombo, "Pick the exact item, vehicle, backpack, or container to override.");
        pane.Controls.Add(_itemCombo);

        _itemAdvanced = NewCheckBox("Advanced", 812, 30, 112);
        _itemAdvanced.CheckedChanged += (_, _) => RefreshItemEditor();
        AddTip(_itemAdvanced, "Show raw Unreal class names and CDO paths for advanced users.");
        pane.Controls.Add(_itemAdvanced);

        var card = NewPanel(14, 28, 76, 874, 270);
        card.BackColor = PanelBack;
        pane.Controls.Add(card);

        _itemClassLabel = MakeLabel("Selected item: -", 28, 20, 820, 30, 14, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack);
        _itemCdoLabel = MakeLabel("CDO path: -", 28, 58, 850, 30, 11, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack);
        card.Controls.Add(_itemClassLabel);
        card.Controls.Add(_itemCdoLabel);

        _itemFields = NewFieldFlow(28, 64, 818, 144);
        _itemFields.AutoScroll = false;
        card.Controls.Add(_itemFields);

        card.Controls.Add(MakeButton("Save Item Override", 28, 216, 190, 42, SaveItemOverride, main: true));
        card.Controls.Add(MakeButton("Clear Item Override", 238, 216, 190, 42, ClearItemOverride));

        return pane;
    }

    private RoundedPanel BuildServerManagerTab()
    {
        var panel = NewContentPanel();
        _serverSubPages.Clear();
        _serverSubButtons.Clear();

        panel.Controls.Add(MakeLabel("Server Manager", 28, 24, 360, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        panel.Controls.Add(MakeLabel("Manage VEIN server setup, config backups, mod parity, logs, and helper packages.", 30, 62, 720, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _serverTypeLabel = MakeLabel("Server Type", 594, 28, 110, 28, 12, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack);
        _serverTypeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        panel.Controls.Add(_serverTypeLabel);

        _serverTypeCombo = NewCombo(704, 24, 310, new[] { "Windows Server Setup", "Linux Server Setup" });
        _serverTypeCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _serverTypeCombo.SelectedIndexChanged += (_, _) => UpdateServerTypeUi();
        AddTip(_serverTypeCombo, "Choose whether you are configuring a local Windows server or a remote Linux server.");
        panel.Controls.Add(_serverTypeCombo);

        _serverTabFlow = new FlowLayoutPanel
        {
            Left = 28,
            Top = 104,
            Width = 964,
            Height = 92,
            BackColor = PanelBack,
            AutoScroll = false,
            WrapContents = true
        };
        _serverTabFlow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(_serverTabFlow);

        foreach (var (title, width) in new[]
                 {
                     ("Connection / Config", 180),
                     ("Actions", 110),
                     ("Backups", 110),
                     ("Mod Parity", 130),
                     ("Logs", 90),
                     ("Linux Helper", 140)
                 })
        {
            var button = NewTabButton(title, 0, 0, width);
            button.Margin = new Padding(0, 0, 8, 8);
            button.Click += (_, _) => ShowServerSubTab(title);
            _serverSubButtons[title] = button;
            _serverTabFlow.Controls.Add(button);
        }

        _serverSubPages["Connection / Config"] = BuildServerMainSettingsPage();
        _serverSubPages["Actions"] = BuildServerManagementPage();
        _serverSubPages["Backups"] = BuildServerBackupsPage();
        _serverSubPages["Mod Parity"] = BuildModParityPage();
        _serverSubPages["Logs"] = BuildServerLogsPage();
        _serverSubPages["Linux Helper"] = BuildServerIntegrationsPage();

        foreach (var page in _serverSubPages.Values)
        {
            page.Left = 28;
            page.Top = 204;
            page.Width = 964;
            page.Height = 300;
            page.Visible = false;
            page.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel.Controls.Add(page);
        }

        ShowServerSubTab("Connection / Config");
        UpdateServerTypeUi();
        LogServer("Server Manager ready. Pick Windows or Linux setup.");
        return panel;
    }

    private void ShowServerSubTab(string title)
    {
        foreach (var (name, page) in _serverSubPages)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            page.Visible = selected;
            if (selected) page.BringToFront();
        }

        foreach (var (name, button) in _serverSubButtons)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            button.FillColor = selected ? Purple : InnerBack;
            button.HoverColor = selected ? PurpleLight : Color.FromArgb(18, 31, 50);
            button.BorderColor = selected ? PurpleLight : BorderSoft;
            button.Invalidate();
        }

        _serverTabFlow?.PerformLayout();
        _serverTabFlow?.Invalidate(true);
    }

    private Panel BuildServerMainSettingsPage()
    {
        var page = new Panel
        {
            BackColor = PanelBack,
            AutoScroll = true,
            AutoScrollMargin = new Size(0, 18)
        };
        _serverModePanel = new Panel
        {
            Left = 0,
            Top = 0,
            Width = 944,
            Height = 300,
            BackColor = PanelBack,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        page.Controls.Add(_serverModePanel);

        _windowsServerPanel = BuildWindowsServerPanel();
        _linuxServerPanel = BuildLinuxServerPanel();
        _serverModePanel.Controls.Add(_windowsServerPanel);
        _serverModePanel.Controls.Add(_linuxServerPanel);
        return page;
    }

    private Panel BuildServerManagementPage()
    {
        var page = new Panel { BackColor = PanelBack };
        _serverManagementPane = page;

        var windows = NewServerSection("Windows Server Management", 0, 0, 456, 200);
        _windowsServerActionsPanel = windows;
        page.Controls.Add(windows);
        windows.Controls.Add(MakeButton("Save Server Config", 22, 46, 180, 42, SaveWindowsServerConfig, main: true));
        windows.Controls.Add(MakeButton("Validate or Update Server", 222, 46, 190, 42, ValidateOrUpdateWindowsServer));
        windows.Controls.Add(MakeButton("Start Server", 22, 106, 122, 42, StartWindowsServerFromUi));
        windows.Controls.Add(MakeButton("Stop Server", 154, 106, 122, 42, StopWindowsServer));
        windows.Controls.Add(MakeButton("Restart Server", 286, 106, 122, 42, RestartWindowsServerFromUi));
        windows.Controls.Add(MakeButton("Open Server Folder", 22, 154, 180, 36, OpenSelectedServerFolder));
        windows.Controls.Add(MakeButton("View Logs", 222, 154, 160, 36, OpenSelectedServerLogs));

        var linux = NewServerSection("Linux Server Management", 476, 0, 456, 260);
        _linuxServerActionsPanel = linux;
        page.Controls.Add(linux);
        linux.Controls.Add(MakeButton("Test Connection", 22, 46, 180, 42, TestLinuxConnectionFromUi, main: true));
        linux.Controls.Add(MakeButton("Save Connection Profile", 222, 46, 190, 42, SaveLinuxProfileFromUi));
        linux.Controls.Add(MakeButton("Download Remote Config", 22, 106, 180, 42, DownloadRemoteConfigFromUi));
        linux.Controls.Add(MakeButton("Upload Config To Server", 222, 106, 190, 42, UploadRemoteConfigFromUi));
        linux.Controls.Add(MakeButton("Backup Remote Server", 22, 166, 180, 42, BackupRemoteServerFromUi));
        linux.Controls.Add(MakeButton("Restart Linux Server", 222, 166, 190, 42, RestartLinuxServerFromUi));
        linux.Controls.Add(MakeButton("View Remote Logs", 22, 218, 180, 36, ViewRemoteLogsFromUi));
        return page;
    }

    private Panel BuildServerBackupsPage()
    {
        var page = new Panel { BackColor = PanelBack };
        _serverBackupsPane = page;
        var section = NewServerSection("Backups", 0, 0, 456, 244);
        page.Controls.Add(section);
        section.Controls.Add(MakeButton("Backup now", 22, 46, 140, 42, BackupSelectedServerConfig, main: true));
        _backupBeforeSaveToggle = AddToggleRow(section, "Backup before save", 190, 46, isChecked: true);
        _backupBeforeUploadToggle = AddToggleRow(section, "Backup before upload", 190, 86, isChecked: true);
        _backupBeforeRestartToggle = AddToggleRow(section, "Backup before restart", 190, 126, isChecked: true);
        _recentBackupsList = new ListBox
        {
            Left = 22,
            Top = 104,
            Width = 140,
            Height = 72,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9F, FontStyle.Regular)
        };
        _recentBackupsList.Items.Add("No backups yet");
        section.Controls.Add(_recentBackupsList);
        var restore = NewSmallButton("Restore backup", 22, 188, 140, 32);
        restore.Enabled = false;
        AddTip(restore, "Disabled until restore can be implemented with validation and rollback checks.");
        section.Controls.Add(restore);
        return page;
    }

    private Panel BuildModParityPage()
    {
        var page = new Panel
        {
            BackColor = PanelBack,
            AutoScroll = true,
            AutoScrollMargin = new Size(0, 18)
        };

        var approved = NewServerSection("Approved Mod List", 0, 0, 456, 286);
        page.Controls.Add(approved);
        approved.Controls.Add(MakeLabel("Add every UE4SS mod folder players must match.", 22, 34, 390, 24, 10.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        _modParityList = new ListBox
        {
            Left = 22,
            Top = 68,
            Width = 410,
            Height = 132,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9.5F, FontStyle.Regular),
            HorizontalScrollbar = true
        };
        approved.Controls.Add(_modParityList);

        approved.Controls.Add(MakeButton("Add Mod Folder", 22, 216, 150, 42, AddModParityFolder, main: true));
        approved.Controls.Add(MakeButton("Remove Selected", 188, 216, 150, 42, RemoveSelectedModParityFolder));

        var settings = NewServerSection("Parity Settings", 476, 0, 456, 286);
        page.Controls.Add(settings);
        _modParityAllowExtraMods = NewCheckBox("Allow extra client mods", 22, 44, 210);
        settings.Controls.Add(_modParityAllowExtraMods);

        settings.Controls.Add(MakeLabel("Enforcement", 22, 88, 160, 24, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _modParityEnforcementCombo = NewCombo(22, 114, 190, new[] { "Log Only", "Off" });
        settings.Controls.Add(_modParityEnforcementCombo);

        settings.Controls.Add(MakeLabel("Kick message", 22, 164, 180, 24, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _modParityKickMessageBox = NewTextBox(22, 190, 410, 34);
        _modParityKickMessageBox.Text = "This server requires the approved modpack.";
        settings.Controls.Add(_modParityKickMessageBox);

        AddTip(_modParityAllowExtraMods, "When off, clients with unapproved extra mods fail the parity check.");
        AddTip(_modParityEnforcementCombo, "Runtime kick enforcement is hidden until the server-authoritative handshake is verified.");
        AddTip(_modParityKickMessageBox, "Message to show when a future runtime kick rejects a mismatched client.");

        var actions = NewServerSection("Generate / Install", 0, 306, 932, 148);
        actions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        page.Controls.Add(actions);
        actions.Controls.Add(MakeButton("Generate Manifest", 22, 46, 170, 42, GenerateModParityManifest, main: true));
        actions.Controls.Add(MakeButton("Export Package", 212, 46, 150, 42, ExportModParityPackage));
        actions.Controls.Add(MakeButton("Install Server Mod", 382, 46, 170, 42, InstallWindowsModParityServer));
        actions.Controls.Add(MakeButton("Open Package Folder", 572, 46, 180, 42, OpenModParityPackageFolder));

        _modParityStatusLabel = MakeWrappedLabel(
            "Phase 1 builds approved per-file manifests and installs/export templates. Runtime handshake and kick enforcement still need in-game UE4SS testing.",
            22,
            98,
            860,
            40,
            10.5F,
            FontStyle.Regular,
            TextMuted,
            PanelBack);
        actions.Controls.Add(_modParityStatusLabel);

        return page;
    }

    private Panel BuildServerLogsPage()
    {
        var page = new Panel { BackColor = PanelBack };
        _serverLogsPane = page;
        var section = NewServerSection("Logs", 0, 0, 932, 286);
        section.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        page.Controls.Add(section);
        section.Controls.Add(MakeButton("Refresh Logs", 22, 42, 140, 38, () => LogServer("Server Manager log refreshed.")));
        section.Controls.Add(MakeButton("Clear Logs", 180, 42, 120, 38, () => _serverManagerLog.Clear()));
        section.Controls.Add(MakeLabel("Auto-scroll", 332, 48, 90, 24, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _serverLogAutoScrollToggle = new ToggleSwitch { Left = 424, Top = 48, Width = 56, Height = 28, BackColor = PanelBack, Checked = true };
        section.Controls.Add(_serverLogAutoScrollToggle);

        _serverManagerLog = new RichTextBox
        {
            Left = 22,
            Top = 92,
            Width = 886,
            Height = 164,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            Font = new Font("Consolas", 10F, FontStyle.Regular),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        section.Controls.Add(_serverManagerLog);
        return page;
    }

    private Panel BuildServerIntegrationsPage()
    {
        var page = new Panel { BackColor = PanelBack };
        _serverIntegrationsPane = page;
        _linuxHelperPanel = BuildLinuxHelperSection();
        _linuxHelperPanel.Left = 0;
        _linuxHelperPanel.Top = 0;
        page.Controls.Add(_linuxHelperPanel);
        page.Controls.Add(MakeLabel("Linux Helper is available only when Linux Server Setup is selected.", 22, 188, 720, 28, 12F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        return page;
    }

    private static Panel BuildServerPlaceholderPage(string title, string message)
    {
        var page = new Panel { BackColor = PanelBack };
        var section = NewServerSection(title, 0, 0, 520, 150);
        section.Controls.Add(MakeLabel(message, 22, 56, 460, 42, 12F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        page.Controls.Add(section);
        return page;
    }

    private void BuildServerStatusSection(Control parent)
    {
        var cardWidth = 214;
        var gap = 14;
        var server = NewStatCard("Server Status", "Stopped", "Local/remote process", Orange, 0, 0, cardWidth, 76);
        var config = NewStatCard("Config Status", "Not Saved", "No server config write", Orange, cardWidth + gap, 0, cardWidth, 76);
        var connection = NewStatCard("Connection Status", "Not Connected", "Test required", Orange, (cardWidth + gap) * 2, 0, cardWidth, 76);
        var backup = NewStatCard("Last Backup", "None", "Backup before changes", TextMuted, (cardWidth + gap) * 3, 0, cardWidth, 76);
        _serverStatusValue = (Label)server.Tag!;
        _configStatusValue = (Label)config.Tag!;
        _connectionStatusValue = (Label)connection.Tag!;
        _lastBackupValue = (Label)backup.Tag!;
        parent.Controls.Add(server);
        parent.Controls.Add(config);
        parent.Controls.Add(connection);
        parent.Controls.Add(backup);
    }

    private Panel BuildWindowsServerPanel()
    {
        var panel = new Panel { Left = 0, Top = 0, Width = 930, Height = 300, BackColor = PanelBack };
        var connection = NewServerSection("Connection", 0, 0, 456, 142);
        panel.Controls.Add(connection);

        var serverFolder = AddPathField(connection, "Server folder path", 22, 50, 284, "Browse Folder", browseFolder: true);
        _windowsServerFolderBox = serverFolder.Box;
        var steamCmd = AddPathField(connection, "SteamCMD path", 22, 104, 284, "SteamCMD", browseFolder: false);
        _windowsSteamCmdBox = steamCmd.Box;
        AddTip(serverFolder.Box, "Folder containing the dedicated VEIN server files.");
        AddTip(steamCmd.Box, "Path to steamcmd.exe for validate/update workflows.");

        var config = NewServerSection("Server Config", 476, 0, 456, 300);
        panel.Controls.Add(config);
        var serverName = AddCompactTextField(config, "Server name", 22, 58, 184, "VEIN Server");
        var description = AddCompactTextField(config, "Server description", 226, 58, 184, "");
        var sessionName = AddCompactTextField(config, "Session name", 22, 110, 184, "Server");
        var serverPassword = AddCompactTextField(config, "Server password", 226, 110, 184, "", password: true);
        var mapSelection = AddCompactTextField(config, "Map selection", 22, 162, 250, "/Game/Vein/Maps/ChamplainValley?listen");
        var maxPlayers = AddCompactTextField(config, "Max players", 292, 162, 118, "16");
        var gamePort = AddCompactTextField(config, "Game port", 22, 214, 118, "7779");
        var queryPort = AddCompactTextField(config, "Query port", 160, 214, 118, "27015");
        config.Controls.Add(MakeLabel("Super Admin SteamIDs", 22, 246, 388, 20, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var superAdmins = NewTextBox(22, 268, 388, 30);
        config.Controls.Add(superAdmins);
        _windowsServerNameBox = serverName;
        _windowsDescriptionBox = description;
        _windowsSessionNameBox = sessionName;
        _windowsServerPasswordBox = serverPassword;
        _windowsMapSelectionBox = mapSelection;
        _windowsMaxPlayersBox = maxPlayers;
        _windowsGamePortBox = gamePort;
        _windowsQueryPortBox = queryPort;
        _windowsSuperAdminsBox = superAdmins;

        var network = NewServerSection("Network Options", 0, 158, 456, 142);
        panel.Controls.Add(network);
        var enableRcon = AddToggleRow(network, "Enable RCON", 22, 50);
        network.Controls.Add(MakeLabel("RCON port", 250, 26, 90, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var rconPort = NewTextBox(250, 50, 80, 34);
        rconPort.Text = "27020";
        network.Controls.Add(rconPort);
        network.Controls.Add(MakeLabel("Password", 342, 26, 90, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var rconPassword = NewTextBox(342, 50, 90, 34, password: true);
        network.Controls.Add(rconPassword);
        var enableHttpApi = AddToggleRow(network, "Enable HTTP API", 22, 104);
        network.Controls.Add(MakeLabel("HTTP API port", 250, 80, 130, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var httpApiPort = NewTextBox(250, 104, 120, 34);
        httpApiPort.Text = "8080";
        network.Controls.Add(httpApiPort);
        _windowsEnableRconToggle = enableRcon;
        _windowsRconPortBox = rconPort;
        _windowsRconPasswordBox = rconPassword;
        _windowsEnableHttpApiToggle = enableHttpApi;
        _windowsHttpApiPortBox = httpApiPort;
        return panel;
    }

    private Panel BuildLinuxServerPanel()
    {
        var panel = new Panel { Left = 0, Top = 0, Width = 930, Height = 300, BackColor = PanelBack };
        var connection = NewServerSection("Connection", 0, 0, 456, 300);
        panel.Controls.Add(connection);
        var host = AddTextField(connection, "Server host or IP", 22, 54, 184, "");
        var port = AddTextField(connection, "SSH port", 226, 54, 84, "22");
        var username = AddTextField(connection, "SSH username", 22, 112, 184, "root");
        connection.Controls.Add(MakeLabel("Authentication type", 226, 88, 184, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var authType = NewCombo(226, 112, 184, new[] { "SSH Key", "Password" });
        connection.Controls.Add(authType);
        var sshKey = AddPathField(connection, "SSH key path", 22, 194, 270, "Browse SSH Key", browseFolder: false);
        var passwordLabel = MakeLabel("Password", 22, 222, 184, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack);
        var password = NewTextBox(22, 246, 184, 34, password: true);
        connection.Controls.Add(passwordLabel);
        connection.Controls.Add(password);
        passwordLabel.Visible = false;
        password.Visible = false;
        authType.SelectedIndexChanged += (_, _) =>
        {
            var passwordAuth = Convert.ToString(authType.SelectedItem)?.Equals("Password", StringComparison.OrdinalIgnoreCase) == true;
            passwordLabel.Visible = passwordAuth;
            password.Visible = passwordAuth;
            sshKey.Label.Visible = !passwordAuth;
            sshKey.Box.Visible = !passwordAuth;
            sshKey.Button.Visible = !passwordAuth;
        };

        var paths = NewServerSection("Remote Paths", 476, 0, 456, 190);
        panel.Controls.Add(paths);
        var remoteServerPath = AddTextField(paths, "Remote VEIN server path", 22, 54, 388, "/home/steam/vein-server");
        var remoteConfigPath = AddTextField(paths, "Remote config path", 22, 112, 388, "/home/steam/vein-server/Vein/Saved/Config/LinuxServer/Game.ini");
        _linuxHostBox = host;
        _linuxPortBox = port;
        _linuxUsernameBox = username;
        _linuxAuthTypeCombo = authType;
        _linuxSshKeyBox = sshKey.Box;
        _linuxPasswordBox = password;
        _linuxRemoteServerPathBox = remoteServerPath;
        _linuxRemoteConfigPathBox = remoteConfigPath;
        foreach (var box in new[]
                 {
                     _linuxHostBox,
                     _linuxPortBox,
                     _linuxUsernameBox,
                     _linuxSshKeyBox,
                     _linuxPasswordBox,
                     _linuxRemoteServerPathBox,
                     _linuxRemoteConfigPathBox
                 })
        {
            box.TextChanged += ResetLinuxConnectionTrust;
        }

        _linuxAuthTypeCombo.SelectedIndexChanged += ResetLinuxConnectionTrust;
        return panel;
    }

    private RoundedPanel BuildServerActionsSection()
    {
        var section = NewServerSection("Actions", 0, 610, 448, 118);
        section.Controls.Add(MakeButton("Open Server Folder", 22, 46, 180, 42, OpenSelectedServerFolder));
        section.Controls.Add(MakeButton("View Logs", 222, 46, 160, 42, OpenSelectedServerLogs));
        return section;
    }

    private RoundedPanel BuildServerBackupsSection()
    {
        var section = NewServerSection("Backups", 466, 610, 448, 188);
        section.Controls.Add(MakeButton("Backup now", 22, 42, 130, 38, BackupSelectedServerConfig, main: true));
        _backupBeforeSaveToggle = AddToggleRow(section, "Backup before save", 180, 42, isChecked: true);
        _backupBeforeUploadToggle = AddToggleRow(section, "Backup before upload", 180, 80, isChecked: true);
        _backupBeforeRestartToggle = AddToggleRow(section, "Backup before restart", 180, 118, isChecked: true);
        _recentBackupsList = new ListBox
        {
            Left = 22,
            Top = 92,
            Width = 130,
            Height = 54,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9F, FontStyle.Regular)
        };
        _recentBackupsList.Items.Add("No backups yet");
        section.Controls.Add(_recentBackupsList);
        var restore = NewSmallButton("Restore backup", 22, 150, 130, 30);
        restore.Enabled = false;
        AddTip(restore, "Disabled until restore can be implemented with validation and rollback checks.");
        section.Controls.Add(restore);
        return section;
    }

    private RoundedPanel BuildServerLogsSection()
    {
        var section = NewServerSection("Logs", 0, 814, 914, 220);
        section.Controls.Add(MakeButton("Refresh Logs", 22, 42, 140, 38, () => LogServer("Server Manager log refreshed.")));
        section.Controls.Add(MakeButton("Clear Logs", 180, 42, 120, 38, () => _serverManagerLog.Clear()));
        section.Controls.Add(MakeLabel("Auto-scroll", 332, 48, 90, 24, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        _serverLogAutoScrollToggle = new ToggleSwitch { Left = 424, Top = 48, Width = 56, Height = 28, BackColor = PanelBack, Checked = true };
        section.Controls.Add(_serverLogAutoScrollToggle);

        _serverManagerLog = new RichTextBox
        {
            Left = 22,
            Top = 92,
            Width = 866,
            Height = 102,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            Font = new Font("Consolas", 10F, FontStyle.Regular),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true
        };
        section.Controls.Add(_serverManagerLog);
        return section;
    }

    private RoundedPanel BuildLinuxHelperSection()
    {
        var section = NewServerSection("Linux Helper", 0, 1050, 914, 168);
        section.Controls.Add(MakeLabel("Generate a private helper package for your Linux server. It backs up before writes and exposes no public API.", 22, 38, 820, 26, 11F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        section.Controls.Add(MakeButton("Generate Linux Helper", 22, 82, 190, 42, () =>
        {
            try
            {
                var package = ServerManagerService.GenerateLinuxHelperPackage();
                _lastLinuxHelperZip = package.ZipPath;
                LogServer("Generated Linux helper package: " + package.ZipPath);
            }
            catch (Exception ex)
            {
                SetServerManagerError("Helper generation failed: " + ex.Message);
            }
        }, main: true));
        section.Controls.Add(MakeButton("Download Helper Package", 232, 82, 200, 42, () =>
        {
            if (string.IsNullOrWhiteSpace(_lastLinuxHelperZip) || !File.Exists(_lastLinuxHelperZip))
            {
                SetServerManagerError("Generate the Linux helper package first.");
                return;
            }
            Process.Start(new ProcessStartInfo { FileName = Path.GetDirectoryName(_lastLinuxHelperZip)!, UseShellExecute = true });
            LogServer("Opened helper package folder.");
        }));
        section.Controls.Add(MakeButton("Copy Install Command", 452, 82, 190, 42, () =>
        {
            Clipboard.SetText("unzip vein-linux-helper-*.zip && cd vein-linux-helper-* && chmod +x install.sh && ./install.sh");
            LogServer("Copied Linux helper install command.");
        }));
        section.Controls.Add(MakeButton("Verify Helper Installed", 662, 82, 190, 42, () =>
        {
            if (!RequireLinuxConnection("Verify Helper Installed")) return;

            try
            {
                var profile = BuildLinuxProfileFromUi();
                var result = ServerManagerService.RunLinuxHelperCommand(profile, "status");
                result.ThrowIfFailed("Helper verification failed.");
                SetServerStatus(result.Output.Trim(), Green);
                LogServer("Linux helper verified. Server status: " + result.Output.Trim());
            }
            catch (Exception ex)
            {
                SetServerManagerError(ex.Message);
            }
        }));
        return section;
    }

    private void UpdateServerTypeUi()
    {
        if (_serverTypeCombo == null || _windowsServerPanel == null || _linuxServerPanel == null) return;

        var linux = Convert.ToString(_serverTypeCombo.SelectedItem)?.Equals("Linux Server Setup", StringComparison.OrdinalIgnoreCase) == true;
        _windowsServerPanel.Visible = !linux;
        _linuxServerPanel.Visible = linux;
        _windowsServerActionsPanel.Visible = !linux;
        _linuxServerActionsPanel.Visible = linux;
        _linuxServerActionsPanel.Left = linux ? 0 : 476;
        _linuxHelperPanel.Visible = linux;
        if (_serverSubButtons.TryGetValue("Linux Helper", out var linuxHelperButton))
        {
            linuxHelperButton.Visible = linux;
        }

        if (!linux
            && _serverSubPages.TryGetValue("Linux Helper", out var linuxHelperPage)
            && linuxHelperPage.Visible)
        {
            ShowServerSubTab("Connection / Config");
        }

        _serverTabFlow.PerformLayout();
        _serverTabFlow.Invalidate(true);

        _linuxConnectionTested = false;
        SetConnectionStatus(linux ? "Not Connected" : "Local", linux ? Orange : Green);
        SetServerStatus("Stopped", Orange);
        LogServer(linux ? "Linux Server Setup selected." : "Windows Server Setup selected.");
    }

    private static RoundedPanel NewServerSection(string title, int x, int y, int w, int h)
    {
        var section = NewPanel(12, x, y, w, h);
        section.BackColor = PanelBack;
        section.Controls.Add(MakeLabel(title, 22, 8, w - 44, 22, 13F, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        return section;
    }

    private static (Label Label, ThemedTextBox Box, RoundedButton Button) AddPathField(Control parent, string label, int x, int y, int width, string buttonText, bool browseFolder)
    {
        var fieldLabel = MakeLabel(label, x, y - 24, width, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack);
        var box = NewTextBox(x, y, width, 34);
        var button = NewSmallButton(buttonText, x + width + 12, y - 2, 126, 38);
        button.Click += (_, _) =>
        {
            if (browseFolder)
            {
                using var dialog = new FolderBrowserDialog { Description = label };
                if (dialog.ShowDialog() == DialogResult.OK) box.Text = dialog.SelectedPath;
                return;
            }

            using var fileDialog = new OpenFileDialog { Title = label, CheckFileExists = true };
            if (fileDialog.ShowDialog() == DialogResult.OK) box.Text = fileDialog.FileName;
        };
        parent.Controls.Add(fieldLabel);
        parent.Controls.Add(box);
        parent.Controls.Add(button);
        return (fieldLabel, box, button);
    }

    private static ThemedTextBox AddTextField(Control parent, string label, int x, int y, int width, string value, bool password = false)
    {
        parent.Controls.Add(MakeLabel(label, x, y - 24, width, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var box = NewTextBox(x, y, width, 34, password);
        box.Text = value;
        parent.Controls.Add(box);
        return box;
    }

    private static ThemedTextBox AddCompactTextField(Control parent, string label, int x, int y, int width, string value, bool password = false)
    {
        parent.Controls.Add(MakeLabel(label, x, y - 22, width, 20, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var box = NewTextBox(x, y, width, 30, password);
        box.Text = value;
        parent.Controls.Add(box);
        return box;
    }

    private static ToggleSwitch AddToggleRow(Control parent, string label, int x, int y, bool isChecked = false)
    {
        var toggle = new ToggleSwitch
        {
            Left = x,
            Top = y,
            Width = 56,
            Height = 28,
            BackColor = PanelBack,
            Checked = isChecked,
            OnColor = Color.FromArgb(8, 84, 54),
            OnColor2 = Color.FromArgb(10, 135, 82),
            OffColor = Color.FromArgb(82, 20, 28),
            OffColor2 = Color.FromArgb(122, 32, 42)
        };
        parent.Controls.Add(toggle);
        parent.Controls.Add(MakeLabel(label, x + 68, y, 160, 28, 10.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        return toggle;
    }

    private static WindowsServerProfile BuildWindowsProfile(
        TextBox serverFolder,
        TextBox steamCmd,
        TextBox serverName,
        TextBox description,
        TextBox sessionName,
        TextBox serverPassword,
        TextBox mapSelection,
        TextBox gamePort,
        TextBox queryPort,
        TextBox maxPlayers,
        ToggleSwitch enableRcon,
        TextBox rconPort,
        TextBox rconPassword,
        ToggleSwitch enableHttpApi,
        TextBox httpApiPort,
        TextBox superAdmins)
    {
        return new WindowsServerProfile(
            serverFolder.Text.Trim(),
            steamCmd.Text.Trim(),
            serverName.Text.Trim(),
            description.Text.Trim(),
            sessionName.Text.Trim(),
            serverPassword.Text,
            mapSelection.Text.Trim(),
            ReadPort(gamePort.Text, "Game port"),
            ReadPort(queryPort.Text, "Query port"),
            ReadPositiveNumber(maxPlayers.Text, "Max players", 1, 999),
            enableRcon.Checked,
            ReadPort(rconPort.Text, "RCON port"),
            rconPassword.Text,
            enableHttpApi.Checked,
            ReadPort(httpApiPort.Text, "HTTP API port"),
            superAdmins.Text.Trim());
    }

    private static LinuxServerProfile BuildLinuxProfile(
        TextBox host,
        TextBox port,
        TextBox username,
        ThemedComboBox authType,
        TextBox sshKeyPath,
        TextBox password,
        TextBox remoteServerPath,
        TextBox remoteConfigPath)
    {
        return new LinuxServerProfile(
            host.Text.Trim(),
            ReadPort(port.Text, "SSH port"),
            username.Text.Trim(),
            Convert.ToString(authType.SelectedItem) ?? "SSH Key",
            sshKeyPath.Text.Trim(),
            password.Text,
            remoteServerPath.Text.Trim(),
            remoteConfigPath.Text.Trim());
    }

    private LinuxServerProfile BuildLinuxProfileFromUi()
    {
        return BuildLinuxProfile(
            _linuxHostBox,
            _linuxPortBox,
            _linuxUsernameBox,
            _linuxAuthTypeCombo,
            _linuxSshKeyBox,
            _linuxPasswordBox,
            _linuxRemoteServerPathBox,
            _linuxRemoteConfigPathBox);
    }

    private WindowsServerProfile BuildWindowsProfileFromUi()
    {
        return BuildWindowsProfile(
            _windowsServerFolderBox,
            _windowsSteamCmdBox,
            _windowsServerNameBox,
            _windowsDescriptionBox,
            _windowsSessionNameBox,
            _windowsServerPasswordBox,
            _windowsMapSelectionBox,
            _windowsGamePortBox,
            _windowsQueryPortBox,
            _windowsMaxPlayersBox,
            _windowsEnableRconToggle,
            _windowsRconPortBox,
            _windowsRconPasswordBox,
            _windowsEnableHttpApiToggle,
            _windowsHttpApiPortBox,
            _windowsSuperAdminsBox);
    }

    private static int ReadPort(string raw, string label)
    {
        return ReadPositiveNumber(raw, label, 1, 65535);
    }

    private static int ReadPositiveNumber(string raw, string label, int minimum, int maximum)
    {
        if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
        {
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"{label} must be a number from {minimum} to {maximum}."));
        }

        return value;
    }

    private bool RequireLinuxConnection(string action)
    {
        if (!_linuxConnectionTested)
        {
            SetServerManagerError(action + " requires Test Connection first.");
            return false;
        }

        LogServer(action + " is using the verified Linux helper workflow.");
        return true;
    }

    private void SaveWindowsServerConfig()
    {
        try
        {
            var profile = BuildWindowsProfileFromUi();
            var configPath = ServerManagerService.ResolveWindowsConfigPath(profile.ServerFolderPath);
            var hadExistingConfig = File.Exists(configPath);
            var backupBeforeSave = _backupBeforeSaveToggle?.Checked == true;
            var writtenPath = ServerManagerService.WriteWindowsServerConfig(profile, backupBeforeSave);
            var profilePath = ServerManagerService.SaveWindowsProfile(profile);

            _lastConfigSaveAt = DateTime.Now;
            SetConfigStatus("Saved", Green);
            if (backupBeforeSave && hadExistingConfig)
            {
                SetLastServerBackup(DateTime.Now);
            }

            LogServer("Saved Windows server config: " + writtenPath);
            LogServer("Saved Windows profile without passwords: " + profilePath);
            RefreshDashboard();
        }
        catch (Exception ex)
        {
            SetServerManagerError("Save failed: " + ex.Message);
        }
    }

    private void ValidateOrUpdateWindowsServer()
    {
        try
        {
            var profile = BuildWindowsProfileFromUi();
            var process = ServerManagerService.StartWindowsValidateOrUpdate(profile);
            SetServerStatus(process == null ? "Not Started" : "Updating", process == null ? Orange : Cyan);
            LogServer(process == null
                ? "SteamCMD validate/update did not start."
                : "Started SteamCMD validate/update for VEIN server files.");
        }
        catch (Exception ex)
        {
            SetServerManagerError("Validate/update failed: " + ex.Message);
        }
    }

    private void StartWindowsServerFromUi()
    {
        try
        {
            var profile = BuildWindowsProfileFromUi();
            var process = ServerManagerService.StartWindowsServer(profile.ServerFolderPath);
            if (process == null)
            {
                SetServerManagerError("VEIN server executable was not found in the selected server folder.");
                return;
            }

            TrackWindowsServerProcess(process);
            SetServerStatus("Running", Green);
            LogServer("Started Windows VEIN server.");
        }
        catch (Exception ex)
        {
            SetServerManagerError("Start failed: " + ex.Message);
        }
    }

    private void RestartWindowsServerFromUi()
    {
        try
        {
            BackupWindowsConfigBeforeRestartIfNeeded();
            StopWindowsServer();
            StartWindowsServerFromUi();
        }
        catch (Exception ex)
        {
            SetServerManagerError("Restart failed: " + ex.Message);
        }
    }

    private void TestLinuxConnectionFromUi()
    {
        var profile = BuildLinuxProfileFromUi();
        var result = ServerManagerService.TestSshKeyConnection(profile);
        _linuxConnectionTested = result.Connected;
        SetConnectionStatus(result.Connected ? "Connected" : "Error", result.Connected ? Green : Red);
        if (result.Connected) LogServer("Linux SSH key connection test passed.");
        else SetServerManagerError(result.Message);
    }

    private void SaveLinuxProfileFromUi()
    {
        try
        {
            var path = ServerManagerService.SaveLinuxProfile(BuildLinuxProfileFromUi());
            LogServer("Saved Linux profile without password: " + path);
        }
        catch (Exception ex)
        {
            SetServerManagerError("Profile save failed: " + ex.Message);
        }
    }

    private void DownloadRemoteConfigFromUi()
    {
        try
        {
            if (!RequireLinuxConnection("Download Remote Config")) return;

            var path = ServerManagerService.DownloadRemoteConfig(BuildLinuxProfileFromUi());
            SetConfigStatus("Downloaded", Green);
            LogServer("Downloaded remote config to: " + path);
        }
        catch (Exception ex)
        {
            SetServerManagerError(ex.Message);
        }
    }

    private void UploadRemoteConfigFromUi()
    {
        try
        {
            if (_backupBeforeUploadToggle?.Checked != true)
            {
                SetServerManagerError("Backup before upload must stay enabled before uploading remote config.");
                return;
            }

            if (!RequireLinuxConnection("Upload Config To Server")) return;

            using var dialog = new OpenFileDialog
            {
                Title = "Select VEIN Linux Game.ini",
                Filter = "INI config (*.ini)|*.ini|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            var output = ServerManagerService.UploadRemoteConfig(BuildLinuxProfileFromUi(), dialog.FileName);
            SetConfigStatus("Uploaded", Green);
            LogServer("Uploaded config through Linux helper: " + output);
        }
        catch (Exception ex)
        {
            SetServerManagerError(ex.Message);
        }
    }

    private void BackupRemoteServerFromUi()
    {
        try
        {
            if (!RequireLinuxConnection("Backup Remote Server")) return;

            var backupPath = ServerManagerService.BackupRemoteConfig(BuildLinuxProfileFromUi());
            AddRecentBackup(backupPath);
            LogServer("Remote backup created: " + backupPath);
        }
        catch (Exception ex)
        {
            SetServerManagerError(ex.Message);
        }
    }

    private void RestartLinuxServerFromUi()
    {
        try
        {
            if (_backupBeforeRestartToggle?.Checked != true)
            {
                SetServerManagerError("Backup before restart must stay enabled before restart.");
                return;
            }

            if (!RequireLinuxConnection("Restart Linux Server")) return;

            var profile = BuildLinuxProfileFromUi();
            var backupPath = ServerManagerService.BackupRemoteConfig(profile);
            AddRecentBackup(backupPath);
            var output = ServerManagerService.RestartRemoteServer(profile);
            SetServerStatus("Restarted", Green);
            LogServer("Remote backup before restart: " + backupPath);
            LogServer("Linux server restart result: " + output);
        }
        catch (Exception ex)
        {
            SetServerManagerError(ex.Message);
        }
    }

    private void ViewRemoteLogsFromUi()
    {
        try
        {
            if (!RequireLinuxConnection("View Remote Logs")) return;

            var logs = ServerManagerService.ReadRemoteLogs(BuildLinuxProfileFromUi());
            LogServer("Remote logs:");
            LogServer(string.IsNullOrWhiteSpace(logs) ? "(no log output)" : logs.TrimEnd());
        }
        catch (Exception ex)
        {
            SetServerManagerError(ex.Message);
        }
    }

    private void OpenSelectedServerFolder()
    {
        if (CurrentServerModeIsLinux())
        {
            RequireLinuxConnection("Open Server Folder");
            return;
        }

        var serverFolder = _windowsServerFolderBox.Text.Trim();
        if (!Directory.Exists(serverFolder))
        {
            SetServerManagerError("Select a valid Windows server folder first.");
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = serverFolder, UseShellExecute = true });
        LogServer("Opened Windows server folder.");
    }

    private void OpenSelectedServerLogs()
    {
        if (CurrentServerModeIsLinux())
        {
            RequireLinuxConnection("View Remote Logs");
            return;
        }

        var serverFolder = _windowsServerFolderBox.Text.Trim();
        var logsFolder = Path.Combine(serverFolder, "Vein", "Saved", "Logs");
        if (!Directory.Exists(logsFolder))
        {
            SetServerManagerError("Windows server logs folder was not found: " + logsFolder);
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = logsFolder, UseShellExecute = true });
        LogServer("Opened Windows server logs folder.");
    }

    private void BackupSelectedServerConfig()
    {
        if (CurrentServerModeIsLinux())
        {
            RequireLinuxConnection("Backup Remote Server");
            return;
        }

        try
        {
            var configPath = ServerManagerService.ResolveWindowsConfigPath(_windowsServerFolderBox.Text.Trim());
            var backupPath = ServerManagerService.CreateServerConfigBackup(configPath);
            AddRecentBackup(backupPath);
            LogServer("Backup created: " + backupPath);
        }
        catch (Exception ex)
        {
            SetServerManagerError("Backup failed: " + ex.Message);
        }
    }

    private bool WindowsServerIsRunning()
    {
        try
        {
            return _windowsServerProcess is { HasExited: false };
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void TrackWindowsServerProcess(Process process)
    {
        _windowsServerProcess = process;
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            if (IsDisposed) return;

            BeginInvoke(() =>
            {
                SetServerStatus("Stopped", Orange);
                LogServer("Windows VEIN server process exited.");
            });
        };
    }

    private void StopWindowsServer()
    {
        try
        {
            if (!WindowsServerIsRunning())
            {
                SetServerStatus("Stopped", Orange);
                LogServer("No manager-started Windows server process is currently running.");
                return;
            }

            var process = _windowsServerProcess!;
            if (!process.CloseMainWindow() || !process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }

            SetServerStatus("Stopped", Orange);
            LogServer("Stopped Windows VEIN server.");
        }
        catch (Exception ex)
        {
            SetServerManagerError("Stop failed: " + ex.Message);
        }
    }

    private void BackupWindowsConfigBeforeRestartIfNeeded()
    {
        if (!_backupBeforeRestartToggle.Checked) return;

        var configPath = ServerManagerService.ResolveWindowsConfigPath(_windowsServerFolderBox.Text.Trim());
        if (!File.Exists(configPath))
        {
            LogServer("No Windows server config found to backup before restart.");
            return;
        }

        var backupPath = ServerManagerService.CreateServerConfigBackup(configPath);
        AddRecentBackup(backupPath);
        LogServer("Backup before restart created: " + backupPath);
    }

    private void AddRecentBackup(string backupPath)
    {
        SetLastServerBackup(DateTime.Now);

        if (_recentBackupsList.Items.Count == 1 && Convert.ToString(_recentBackupsList.Items[0]) == "No backups yet")
        {
            _recentBackupsList.Items.Clear();
        }

        var backupName = Path.GetFileName(Path.GetDirectoryName(backupPath)) ?? Path.GetFileName(backupPath);
        _recentBackupsList.Items.Insert(0, backupName);
        RefreshDashboard();
    }

    private void AddModParityFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select an approved UE4SS mod folder" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var path = Path.GetFullPath(dialog.SelectedPath);
        var validation = ModParityService.ValidateModFolder(path);
        if (!validation.IsValid)
        {
            SetModParityStatus(validation.Message, Red);
            return;
        }

        if (_modParityFolders.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            SetModParityStatus("That mod folder is already in the approved list.", Orange);
            return;
        }

        _modParityFolders.Add(path);
        RefreshModParityList();
        SetModParityStatus("Added approved mod folder: " + Path.GetFileName(path), Green);
    }

    private void RemoveSelectedModParityFolder()
    {
        if (_modParityList.SelectedIndex < 0 || _modParityList.SelectedIndex >= _modParityFolders.Count)
        {
            SetModParityStatus("Select a mod folder to remove first.", Orange);
            return;
        }

        var removed = _modParityFolders[_modParityList.SelectedIndex];
        _modParityFolders.RemoveAt(_modParityList.SelectedIndex);
        RefreshModParityList();
        SetModParityStatus("Removed approved mod folder: " + Path.GetFileName(removed), TextMuted);
    }

    private void GenerateModParityManifest()
    {
        try
        {
            var package = ModParityService.ExportPackage(_modParityFolders, BuildModParitySettings(), GetModTemplateRoot());
            _lastModParityPackageZip = package.ZipPath;
            SetModParityStatus($"Generated manifest for {package.Manifest.RequiredMods.Count} approved mods.", Green);
            LogServer("Generated mod parity manifest package: " + package.ZipPath);
        }
        catch (Exception ex)
        {
            SetServerManagerError("Mod parity manifest failed: " + ex.Message);
        }
    }

    private void ExportModParityPackage()
    {
        try
        {
            var package = ModParityService.ExportPackage(_modParityFolders, BuildModParitySettings(), GetModTemplateRoot());
            _lastModParityPackageZip = package.ZipPath;
            SetModParityStatus("Exported mod parity package: " + package.ZipPath, Green);
            LogServer("Exported mod parity package: " + package.ZipPath);
        }
        catch (Exception ex)
        {
            SetServerManagerError("Mod parity export failed: " + ex.Message);
        }
    }

    private void InstallWindowsModParityServer()
    {
        try
        {
            var target = ModParityService.InstallWindowsServerMod(
                _windowsServerFolderBox.Text.Trim(),
                _modParityFolders,
                BuildModParitySettings(),
                GetModTemplateRoot());
            SetModParityStatus("Installed server parity scaffold: " + target, Green);
            LogServer("Installed server parity scaffold: " + target);
        }
        catch (Exception ex)
        {
            SetServerManagerError("Mod parity install failed: " + ex.Message);
        }
    }

    private void OpenModParityPackageFolder()
    {
        if (string.IsNullOrWhiteSpace(_lastModParityPackageZip) || !File.Exists(_lastModParityPackageZip))
        {
            SetModParityStatus("Export a mod parity package first.", Orange);
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = Path.GetDirectoryName(_lastModParityPackageZip)!, UseShellExecute = true });
        LogServer("Opened mod parity package folder.");
    }

    private void RefreshModParityList()
    {
        if (_modParityList == null) return;

        _modParityList.Items.Clear();
        foreach (var folder in _modParityFolders)
        {
            _modParityList.Items.Add(Path.GetFileName(folder) + "  |  " + folder);
        }
    }

    private ModParitySettings BuildModParitySettings()
    {
        return new ModParitySettings(
            _modParityAllowExtraMods.Checked,
            NormalizeModParityEnforcement(Convert.ToString(_modParityEnforcementCombo.SelectedItem)),
            string.IsNullOrWhiteSpace(_modParityKickMessageBox.Text)
                ? "This server requires the approved modpack."
                : _modParityKickMessageBox.Text.Trim());
    }

    private static string NormalizeModParityEnforcement(string? selected)
    {
        return string.Equals(selected, "Off", StringComparison.OrdinalIgnoreCase) ? "Off" : "Log Only";
    }

    private void SetModParityStatus(string message, Color color)
    {
        if (_modParityStatusLabel != null)
        {
            _modParityStatusLabel.Text = message;
            _modParityStatusLabel.ForeColor = color;
        }
    }

    private static string GetModTemplateRoot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var published = Path.Combine(baseDirectory, "ModTemplate");
        if (Directory.Exists(published)) return published;

        var source = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "ModTemplate"));
        return source;
    }

    private bool CurrentServerModeIsLinux()
    {
        return Convert.ToString(_serverTypeCombo.SelectedItem)?.Equals("Linux Server Setup", StringComparison.OrdinalIgnoreCase) == true;
    }

    private void ResetLinuxConnectionTrust(object? sender, EventArgs e)
    {
        if (!_linuxConnectionTested) return;

        _linuxConnectionTested = false;
        SetConnectionStatus("Retest Required", Orange);
        LogServer("Linux connection settings changed. Test Connection again before remote actions.");
    }

    private void SetServerManagerError(string message)
    {
        SetConnectionStatus("Error", Red);
        LogServer("ERROR: " + message, Red);
    }

    private void SetServerStatus(string text, Color color)
    {
        _serverState.StatusText = text;
        _serverState.StatusColor = color;
        if (_serverStatusValue != null)
        {
            _serverStatusValue.Text = text;
            _serverStatusValue.ForeColor = color;
        }

        RefreshDashboard();
    }

    private void SetConfigStatus(string text, Color color)
    {
        _serverState.ConfigText = text;
        _serverState.ConfigColor = color;
        if (_configStatusValue != null)
        {
            _configStatusValue.Text = text;
            _configStatusValue.ForeColor = color;
        }

        RefreshDashboard();
    }

    private void SetConnectionStatus(string text, Color color)
    {
        _serverState.ConnectionText = text;
        _serverState.ConnectionColor = color;
        if (_connectionStatusValue != null)
        {
            _connectionStatusValue.Text = text;
            _connectionStatusValue.ForeColor = color;
        }

        RefreshDashboard();
    }

    private void SetLastServerBackup(DateTime when)
    {
        _serverState.LastBackupAt = when;
        _lastConfigBackupAt = when;
        if (_lastBackupValue != null)
        {
            _lastBackupValue.Text = when.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            _lastBackupValue.ForeColor = Green;
        }

        RefreshDashboard();
    }

    private void LogServer(string message)
    {
        LogServer(message, TextMuted);
    }

    private void LogServer(string message, Color color)
    {
        if (_serverManagerLog == null) return;

        var scrollParent = _serverManagerLog.Parent?.Parent as ScrollableControl;
        var scrollPosition = scrollParent?.AutoScrollPosition ?? Point.Empty;
        var line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " | " + message + Environment.NewLine;
        _serverManagerLog.SelectionStart = _serverManagerLog.TextLength;
        _serverManagerLog.SelectionColor = color;
        _serverManagerLog.AppendText(line);
        _serverManagerLog.SelectionColor = _serverManagerLog.ForeColor;
        if (_serverLogAutoScrollToggle == null || _serverLogAutoScrollToggle.Checked)
        {
            _serverManagerLog.ScrollToCaret();
        }

        if (scrollParent != null)
        {
            scrollParent.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
        }

        if (!message.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)) Log("Server Manager: " + message);
        _serverState.HasActivity = true;
        RefreshDashboard();
    }

    private RoundedPanel BuildLogTab()
    {
        var panel = NewContentPanel();
        panel.Controls.Add(MakeLabel("Status Log", 28, 24, 300, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        panel.Controls.Add(MakeLabel("Copy friendly status messages, backup paths, and errors from here.", 30, 62, 760, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));

        var logShell = NewPanel(12, 30, 112, 930, 330);
        logShell.BackColor = PanelBack;
        logShell.FillColor = InnerBack;
        logShell.BorderColor = Border;
        panel.Controls.Add(logShell);

        _log = new RichTextBox
        {
            Left = 14,
            Top = 12,
            Width = 902,
            Height = 306,
            BackColor = InnerBack,
            ForeColor = TextMuted,
            Font = new Font("Consolas", 10.5F, FontStyle.Regular),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true
        };
        logShell.Controls.Add(_log);
        Log("VEIN Item And Container Modifier loaded.");
        return panel;
    }

    private void AutoDetectPaths(bool log)
    {
        var savedGameFolder = _gameFolderBox.Text.Trim();
        var savedModFolder = _modFolderBox.Text.Trim();
        var hasSavedGameFolder = IsValidGameFolder(savedGameFolder);
        var hasSavedModFolder = LuaModService.IsValidModFolder(savedModFolder);

        if (hasSavedGameFolder)
        {
            var modFolder = hasSavedModFolder
                ? savedModFolder
                : LuaModService.DetectModFolder(savedGameFolder) ?? LuaModService.GetExpectedModFolder(savedGameFolder);
            _gameFolderBox.Text = savedGameFolder;
            _modFolderBox.Text = modFolder;

            TryInstallBundledMod(savedGameFolder, modFolder, log);

            if (log) Log("Using saved VEIN path: " + savedGameFolder);
            if (log) Log("Using mod path: " + modFolder);
            SavePathSettings();
            LoadModFromPath(loadExistingState: true);
            UpdateStatuses();
            return;
        }

        var gameFolder = LuaModService.DetectGameFolder();
        if (!string.IsNullOrWhiteSpace(gameFolder))
        {
            _gameFolderBox.Text = gameFolder;
            var modFolder = hasSavedModFolder
                ? savedModFolder
                : LuaModService.DetectModFolder(gameFolder) ?? LuaModService.GetExpectedModFolder(gameFolder);
            _modFolderBox.Text = modFolder;

            TryInstallBundledMod(gameFolder, modFolder, log);

            if (log) Log("Detected VEIN path: " + gameFolder);
            if (log) Log("Using mod path: " + modFolder);
            LoadModFromPath(loadExistingState: true);
        }
        else if (log)
        {
            Log(hasSavedModFolder
                ? "VEIN path was not auto-detected. Keeping the saved mod folder."
                : "VEIN path was not auto-detected. Use Browse.");
        }

        SavePathSettings();
        UpdateStatuses();
    }

    private void BrowseGameFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = "Select the VEIN Steam folder" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _gameFolderBox.Text = dlg.SelectedPath;
        var expected = LuaModService.DetectModFolder(dlg.SelectedPath) ?? LuaModService.GetExpectedModFolder(dlg.SelectedPath);
        _modFolderBox.Text = expected;
        TryInstallBundledMod(dlg.SelectedPath, expected, log: true);
        SavePathSettings();
        LoadModFromPath(loadExistingState: true);
        MarkUnsaved(false);
        Log("Selected VEIN path: " + dlg.SelectedPath);
    }

    private void TryInstallBundledMod(string gameFolder, string modFolder, bool log)
    {
        if (LuaModService.IsValidModFolder(modFolder)) return;
        if (!LuaModService.HasUe4ss(gameFolder))
        {
            if (log) Log("UE4SS was not found yet. Install UE4SS, then Auto Detect will install the bundled mod template.");
            return;
        }

        try
        {
            if (LuaModService.EnsureBundledModInstalled(modFolder) && log)
            {
                Log("Installed bundled ItemAndContainerModifier template: " + modFolder);
            }
        }
        catch (Exception ex)
        {
            if (log) LogError("Could not auto-install bundled mod: " + ex.Message);
        }
    }

    private void LoadPathSettings()
    {
        var path = GetLocalSettingsPath();
        if (path == null || !File.Exists(path)) return;

        try
        {
            var settings = JsonSerializer.Deserialize<LocalPathSettings>(File.ReadAllText(path));
            if (settings == null) return;

            var gameFolder = settings.GameFolder;
            if (!string.IsNullOrWhiteSpace(gameFolder) && IsValidGameFolder(gameFolder))
            {
                _gameFolderBox.Text = gameFolder;
            }

            var modFolder = settings.ModFolder;
            if (!string.IsNullOrWhiteSpace(modFolder) && LuaModService.IsValidModFolder(modFolder))
            {
                _modFolderBox.Text = modFolder;
            }
        }
        catch (Exception ex)
        {
            LogError("Saved path settings could not be loaded. Auto Detect will be used instead. " + ex.Message);
        }
    }

    private void SavePathSettings()
    {
        var path = GetLocalSettingsPath();
        if (path == null) return;

        try
        {
            var settings = new LocalPathSettings
            {
                GameFolder = _gameFolderBox.Text.Trim(),
                ModFolder = _modFolderBox.Text.Trim()
            };
            WriteTextAtomic(path, JsonSerializer.Serialize(settings, SettingsJsonOptions));
        }
        catch (Exception ex)
        {
            LogError("Path settings could not be saved, but the app can continue. " + ex.Message);
        }
    }

    private static string? GetLocalSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(localAppData)
            ? null
            : Path.Combine(localAppData, SettingsDirectoryName, SettingsFileName);
    }

    private static void WriteTextAtomic(string path, string content)
    {
        var directory = Path.GetDirectoryName(path) ?? ".";
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path)) File.Replace(tempPath, path, null);
            else File.Move(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static bool IsValidGameFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return false;
        return File.Exists(Path.Combine(path, "Vein", "Binaries", "Win64", "Vein-Win64-Test.exe"))
            || File.Exists(Path.Combine(path, "Vein", "Binaries", "Win64", "Vein.exe"))
            || Directory.Exists(Path.Combine(path, "Vein", "Content", "Paks"));
    }

    private void BrowseModFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = "Select ItemAndContainerModifier folder" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _modFolderBox.Text = dlg.SelectedPath;
        LoadModFromPath(loadExistingState: true);
        SavePathSettings();
        Log("Selected mod folder: " + dlg.SelectedPath);
    }

    private void OpenModFolder()
    {
        var path = _modFolderBox.Text.Trim();
        if (Directory.Exists(path))
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                Log("Opened mod folder.");
            }
            catch (Exception ex)
            {
                LogError("Could not open mod folder: " + ex.Message);
            }
        }
        else
        {
            LogError("Mod folder does not exist.");
        }
    }

    private bool ImportConfigFile(string path, bool markUnsaved)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            LogError("Select or drop a ui_config.lua file first.");
            return false;
        }

        if (!File.Exists(path))
        {
            LogError("Config file does not exist: " + path);
            return false;
        }

        try
        {
            SetImportConfigStatus(path);
            var imported = LuaModService.LoadUiConfigStateFromFile(path);
            _state.CopyFrom(imported);

            if (_modData == null && LuaModService.IsValidModFolder(_modFolderBox.Text.Trim()))
            {
                _modData = LuaModService.LoadModData(_modFolderBox.Text.Trim());
            }

            MarkUnsaved(markUnsaved);
            RefreshCategoryEditor();
            RefreshItemList();
            UpdateStatuses();

            var editCount = _state.CountEdits(_modData);
            Log(markUnsaved
                ? $"Imported {editCount} generated edits into the editor."
                : $"Loaded {editCount} generated edits from the current mod folder.");
            SavePathSettings();
            return true;
        }
        catch (Exception ex)
        {
            LogError("Config import failed: " + ex.Message);
            return false;
        }
    }

    private void InstallConfigFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            LogError("Select or drop a ui_config.lua file first.");
            return;
        }

        if (!File.Exists(path))
        {
            LogError("Config file does not exist: " + path);
            return;
        }

        if (!Path.GetFileName(path).Equals("ui_config.lua", StringComparison.OrdinalIgnoreCase))
        {
            LogError("Drop or install a file named ui_config.lua.");
            return;
        }

        var modFolder = ResolveConfigInstallModFolder();
        if (modFolder == null) return;

        try
        {
            var install = LuaModService.InstallUiConfig(modFolder, path);
            _state.CopyFrom(install.State);
            MarkUnsaved(false);
            SetImportConfigStatus("Installed to Scripts\\ui_config.lua");
            _lastConfigBackupAt = DateTime.Now;
            Log("Backup created: " + install.BackupPath);
            Log("Installed ui_config.lua to: " + install.InstalledPath);
            LoadModFromPath(loadExistingState: true);
            SavePathSettings();
        }
        catch (Exception ex)
        {
            LogError("Config install failed: " + ex.Message);
        }
    }

    private string? ResolveConfigInstallModFolder()
    {
        var modFolder = _modFolderBox.Text.Trim();
        if (LuaModService.IsValidModFolder(modFolder)) return modFolder;

        Log("Mod folder was not ready. Trying Auto Detect before installing the dropped config.");
        AutoDetectPaths(log: true);

        modFolder = _modFolderBox.Text.Trim();
        if (LuaModService.IsValidModFolder(modFolder)) return modFolder;

        LogError("Could not find ItemAndContainerModifier automatically. Use Setup > Auto Detect once, then drop ui_config.lua again.");
        return null;
    }

    private void LoadCurrentConfigFile()
    {
        var modFolder = _modFolderBox.Text.Trim();
        if (!LuaModService.IsValidModFolder(modFolder))
        {
            LogError("Select a valid ItemAndContainerModifier folder first.");
            return;
        }

        var path = Path.Combine(modFolder, "Scripts", "ui_config.lua");
        SetImportConfigStatus(path);
        ImportConfigFile(path, markUnsaved: false);
    }

    private void OpenConfigFolder()
    {
        var modFolder = _modFolderBox.Text.Trim();
        var scripts = Path.Combine(modFolder, "Scripts");
        if (!Directory.Exists(scripts))
        {
            LogError("Scripts folder does not exist. Select a valid mod folder first.");
            return;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo { FileName = scripts, UseShellExecute = true });
            Log("Opened config folder.");
        }
        catch (Exception ex)
        {
            LogError("Could not open config folder: " + ex.Message);
        }
    }

    private void ConfigImport_DragEnter(object? sender, DragEventArgs e)
    {
        var canDrop = e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        e.Effect = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        if (canDrop && _importDropZone != null)
        {
            _importDropZone.BorderColor = PurpleLight;
            _importDropZone.Invalidate();
        }
    }

    private void ConfigImport_DragLeave(object? sender, EventArgs e)
    {
        ResetImportDropZoneBorder();
    }

    private void ConfigImport_DragDrop(object? sender, DragEventArgs e)
    {
        ResetImportDropZoneBorder();
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;

        SetImportConfigStatus(files[0]);
        InstallConfigFile(files[0]);
    }

    private void SetImportConfigStatus(string message)
    {
        if (_importConfigPathLabel == null) return;

        _importConfigPathLabel.Text = string.IsNullOrWhiteSpace(message)
            ? "Waiting for file"
            : message;
    }

    private void WireConfigImportDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += ConfigImport_DragEnter;
        control.DragLeave += ConfigImport_DragLeave;
        control.DragDrop += ConfigImport_DragDrop;
    }

    private void ResetImportDropZoneBorder()
    {
        if (_importDropZone == null) return;

        _importDropZone.BorderColor = Border;
        _importDropZone.Invalidate();
    }

    private void LoadModFromPath(bool loadExistingState = true)
    {
        var path = _modFolderBox.Text.Trim();
        if (!LuaModService.IsValidModFolder(path))
        {
            _modData = null;
            if (loadExistingState) _state.Clear();
            RefreshCategoryEditor();
            RefreshItemList();
            UpdateStatuses();
            Log("Setup needed: select a valid ItemAndContainerModifier folder.");
            return;
        }

        try
        {
            _modData = LuaModService.LoadModData(path);
            if (loadExistingState)
            {
                try
                {
                    _state.CopyFrom(LuaModService.LoadUiConfigState(path));
                    MarkUnsaved(false);
                    var editCount = _state.CountEdits(_modData);
                    if (editCount > 0) Log($"Loaded {editCount} generated UI config edits.");
                }
                catch (Exception ex)
                {
                    _state.Clear();
                    MarkUnsaved(false);
                    LogError("Failed loading generated ui_config.lua: " + ex.Message);
                }
            }
            Log($"Loaded {_modData.Categories.Count} categories and {_modData.ItemCount} entries.");
            foreach (var category in _modData.Categories.Values.Where(category => category.ParseError != null))
            {
                LogError($"{category.Name}: {category.ParseError}");
            }
            RefreshCategoryEditor();
            RefreshItemList();
        }
        catch (Exception ex)
        {
            _modData = null;
            LogError("Failed loading mod: " + ex.Message);
        }
        UpdateStatuses();
    }

    private void SaveConfig()
    {
        _ = TrySaveConfig();
    }

    private bool TrySaveConfig()
    {
        LoadModFromPath(loadExistingState: false);
        var modFolder = _modFolderBox.Text.Trim();
        if (!LuaModService.IsValidModFolder(modFolder))
        {
            LogError("Cannot save. Select a valid ItemAndContainerModifier folder first.");
            return false;
        }

        if (IsGameRunning())
        {
            var result = MessageBox.Show(
                this,
                "VEIN appears to be running. Save anyway, then restart the game for changes to apply?",
                "VEIN is running",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                Log("Save canceled because VEIN is running.");
                return false;
            }
        }

        try
        {
            var backup = LuaModService.CreateBackup(modFolder);
            Log("Backup created: " + backup);
            _lastConfigBackupAt = DateTime.Now;
            LuaModService.ApplyConfig(modFolder, _state);
            _lastConfigSaveAt = DateTime.Now;
            MarkUnsaved(false);
            SavePathSettings();
            Log("Config saved. Restart VEIN if the game is open.");
            LoadModFromPath(loadExistingState: true);
            return !_hasUnsavedChanges;
        }
        catch (Exception ex)
        {
            LogError("Save failed. Your unsaved changes are still open in the editor. " + ex.Message);
            return false;
        }
    }

    private void BackupNow()
    {
        var modFolder = _modFolderBox.Text.Trim();
        if (!LuaModService.IsValidModFolder(modFolder))
        {
            LogError("Cannot backup. Select the ItemAndContainerModifier folder first.");
            return;
        }

        try
        {
            Log("Backup created: " + LuaModService.CreateBackup(modFolder));
            _lastConfigBackupAt = DateTime.Now;
            RefreshDashboard();
        }
        catch (Exception ex)
        {
            LogError("Backup failed: " + ex.Message);
        }
    }

    private void LaunchVein()
    {
        try
        {
            using var process = LuaModService.LaunchVein(_gameFolderBox.Text.Trim());
            Log(process == null ? "VEIN executable was not found." : "Launched VEIN.");
        }
        catch (Exception ex)
        {
            LogError("Launch failed: " + ex.Message);
        }
    }

    private void SaveCategoryDefault()
    {
        var category = CurrentCategory();
        if (category == null) return;

        var values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
        var inputsValid = true;
        inputsValid &= AddCategoryNumber(values, "Weight");
        inputsValid &= AddCategoryNumber(values, "MaxStack", allowNegative: false);
        AddCategoryBool(values, "bStackable");
        inputsValid &= AddCategoryNumber(values, "MaxWeight");
        inputsValid &= AddCategoryNumber(values, "ExtraWeightCapacity");
        inputsValid &= AddCategoryNumber(values, "RunSpeedMultiplier");
        if (!inputsValid) return;

        _state.EnabledCategories[category] = _categoryEnabled.Checked;
        if (values.Count == 0) _state.CategoryDefaults.Remove(category);
        else _state.CategoryDefaults[category] = values;

        MarkUnsaved();
        Log("Saved category defaults in the editor for " + category + ".");
        RefreshCategoryEditor();
        UpdateStatuses();
    }

    private void ResetCategory()
    {
        var category = CurrentCategory();
        if (category == null) return;
        _state.EnabledCategories.Remove(category);
        _state.CategoryDefaults.Remove(category);
        _state.ItemOverrides.Remove(category);
        _state.ContainerWeightOverrides.Remove(category);
        MarkUnsaved();
        Log("Reset generated overrides for " + category + ".");
        RefreshCategoryEditor();
        RefreshItemEditor();
        UpdateStatuses();
    }

    private void SaveItemOverride()
    {
        var item = CurrentItem();
        if (item == null)
        {
            LogError("Pick an item first.");
            return;
        }

        if (CategoryNames.ContainerLike.Contains(item.Category))
        {
            if (_itemInputs.TryGetValue("MaxWeight", out var field) && field.DefaultCheck.Checked)
            {
                if (_state.ContainerWeightOverrides.TryGetValue(item.Category, out var existing)) existing.Remove(item.ClassName);
            }
            else
            {
                if (!ReadItemNumber("MaxWeight", "Max Weight", out var maxWeight, out var hasMaxWeight)) return;
                if (hasMaxWeight) GetContainerOverrides(item.Category)[item.ClassName] = maxWeight;
            }
        }
        else
        {
            var values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
            var inputsValid = true;
            inputsValid &= AddItemNumber(values, "Weight", "Weight");
            inputsValid &= AddItemNumber(values, "MaxStack", "Max Stack", allowNegative: false);
            AddItemBool(values, "bStackable");
            inputsValid &= AddItemNumber(values, "ExtraWeightCapacity", "Extra Weight Capacity");
            inputsValid &= AddItemNumber(values, "RunSpeedMultiplier", "Run Speed Multiplier");
            if (!inputsValid) return;

            if (values.Count == 0)
            {
                if (_state.ItemOverrides.TryGetValue(item.Category, out var existing)) existing.Remove(item.ClassName);
            }
            else
            {
                GetItemOverrides(item.Category)[item.ClassName] = values;
            }
        }

        MarkUnsaved();
        Log("Saved item override in the editor for " + item.ClassName + ".");
        RefreshItemEditor();
        UpdateStatuses();
    }

    private void ClearItemOverride()
    {
        var item = CurrentItem();
        if (item == null) return;

        if (CategoryNames.ContainerLike.Contains(item.Category))
        {
            if (_state.ContainerWeightOverrides.TryGetValue(item.Category, out var values)) values.Remove(item.ClassName);
        }
        else if (_state.ItemOverrides.TryGetValue(item.Category, out var items))
        {
            items.Remove(item.ClassName);
        }

        MarkUnsaved();
        Log("Cleared generated override for " + item.ClassName + ".");
        RefreshItemEditor();
        UpdateStatuses();
    }

    private void ClearDynamicFieldControls(Control parent)
    {
        foreach (Control control in parent.Controls.Cast<Control>().ToArray())
        {
            ClearToolTipTree(control);
            control.Dispose();
        }

        parent.Controls.Clear();
    }

    private void ClearToolTipTree(Control control)
    {
        _toolTip.SetToolTip(control, null);
        foreach (Control child in control.Controls)
        {
            ClearToolTipTree(child);
        }
    }

    private void RefreshCategoryEditor()
    {
        if (_categoryCombo == null || _categoryFields == null) return;
        var category = CurrentCategory();
        ClearDynamicFieldControls(_categoryFields);
        _categoryInputs.Clear();

        if (string.IsNullOrWhiteSpace(category))
        {
            AddSetupMessage(_categoryFields, "Select a category to edit defaults.");
            return;
        }

        _loadingUi = true;
        _categoryEnabled.Checked = _state.EnabledCategories.TryGetValue(category, out var enabled)
            ? enabled
            : _modData?.BaseEnabledCategories.TryGetValue(category, out var baseEnabled) == true && baseEnabled;

        var fields = GetFieldsForCategory(category);
        foreach (var field in fields)
        {
            AddCategoryField(field.Key, field.Label, field.Kind);
        }

        if (_state.CategoryDefaults.TryGetValue(category, out var values))
        {
            foreach (var pair in values)
            {
                SetInputValue(_categoryInputs, pair.Key, pair.Value);
            }
        }

        _loadingUi = false;
    }

    private void RefreshItemList()
    {
        if (_itemCombo == null || _itemCategoryCombo == null) return;
        var category = Convert.ToString(_itemCategoryCombo.SelectedItem) ?? CategoryNames.Ordered[0];
        var search = _itemSearchBox?.Text.Trim() ?? "";
        var items = _modData?.Categories.TryGetValue(category, out var data) == true
            ? data.Items
            : new List<CategoryItem>();

        var filtered = items
            .Select(item => new ItemChoice(item.ClassName, FriendlyClassName(item.ClassName)))
            .Where(item => string.IsNullOrWhiteSpace(search)
                || item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.ClassName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Cast<object>()
            .ToArray();

        _loadingUi = true;
        _itemCombo.Items.Clear();
        _itemCombo.Items.AddRange(filtered);
        ConfigureComboDropDown(_itemCombo);
        if (_itemCombo.Items.Count > 0) _itemCombo.SelectedIndex = 0;
        _loadingUi = false;
        RefreshItemEditor();
    }

    private void RefreshItemEditor()
    {
        if (_loadingUi || _itemFields == null) return;
        _loadingUi = true;
        try
        {
            var item = CurrentItem();
            ClearDynamicFieldControls(_itemFields);
            _itemInputs.Clear();

            if (item == null)
            {
                _itemClassLabel.Text = _modData == null
                    ? "Setup needed: select a valid mod folder first."
                    : "Selected item: -";
                _itemCdoLabel.Visible = false;
                AddSetupMessage(_itemFields, "Pick a category and item to edit overrides.");
                return;
            }

            _itemClassLabel.Text = _itemAdvanced.Checked ? "Raw class: " + item.ClassName : "Selected item: " + FriendlyClassName(item.ClassName);
            _itemCdoLabel.Text = "CDO path: " + (item.CdoPath ?? "-");
            _itemCdoLabel.Visible = _itemAdvanced.Checked;
            _itemFields.Top = _itemAdvanced.Checked ? 88 : 64;
            _itemFields.Height = _itemAdvanced.Checked ? 120 : 144;

            foreach (var field in GetFieldsForCategory(item.Category))
            {
                AddItemField(field.Key, field.Label, field.Kind);
            }

            if (CategoryNames.ContainerLike.Contains(item.Category))
            {
                if (_state.ContainerWeightOverrides.TryGetValue(item.Category, out var values) && values.TryGetValue(item.ClassName, out var value))
                {
                    SetItemInputValue("MaxWeight", value);
                }
            }
            else if (_state.ItemOverrides.TryGetValue(item.Category, out var items) && items.TryGetValue(item.ClassName, out var props))
            {
                foreach (var pair in props)
                {
                    SetItemInputValue(pair.Key, pair.Value);
                }
            }
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void PresetLightweightItems()
    {
        foreach (var category in CategoryNames.ItemLike)
        {
            GetCategoryDefaults(category)["Weight"] = new LuaValue(0.1m);
        }
        MarkUnsaved();
        Log("Preset applied: Lightweight Items.");
        RefreshCategoryEditor();
        UpdateStatuses();
    }

    private void PresetBigStacks()
    {
        foreach (var category in CategoryNames.ItemLike)
        {
            var defaults = GetCategoryDefaults(category);
            defaults["MaxStack"] = new LuaValue(999m);
            defaults["bStackable"] = new LuaValue(true);
        }
        MarkUnsaved();
        Log("Preset applied: Big Stacks.");
        RefreshCategoryEditor();
        UpdateStatuses();
    }

    private void PresetHugeContainers()
    {
        GetCategoryDefaults("containers")["MaxWeight"] = new LuaValue(999999m);
        MarkUnsaved();
        Log("Preset applied: Huge Containers.");
        RefreshCategoryEditor();
        UpdateStatuses();
    }

    private void PresetHugeVehicles()
    {
        GetCategoryDefaults("vehicles")["MaxWeight"] = new LuaValue(999999m);
        MarkUnsaved();
        Log("Preset applied: Huge Vehicles.");
        RefreshCategoryEditor();
        UpdateStatuses();
    }

    private void PresetBackpacksBoosted()
    {
        GetCategoryDefaults("backpacks")["ExtraWeightCapacity"] = new LuaValue(999999m);
        MarkUnsaved();
        Log("Preset applied: Backpacks Boosted.");
        RefreshCategoryEditor();
        UpdateStatuses();
    }

    private void PresetResetToDefaults()
    {
        _state.Clear();
        MarkUnsaved();
        Log("Preset applied: Reset To Game Defaults.");
        RefreshCategoryEditor();
        RefreshItemEditor();
        UpdateStatuses();
    }

    private static bool IsGameRunning()
    {
        return IsAnyProcessRunning(
            "Vein-Win64-Test",
            "Vein",
            "Vein-Win64-Shipping");
    }

    private static bool IsAnyProcessRunning(params string[] processNames)
    {
        foreach (var processName in processNames)
        {
            var processes = Process.GetProcessesByName(processName);
            try
            {
                if (processes.Length > 0) return true;
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }

        return false;
    }

    private void UpdateStatuses()
    {
        var gameRunning = IsGameRunning();
        _gameStatus.Text = gameRunning ? "Open" : "Closed";
        _gameStatus.ForeColor = gameRunning ? Green : Orange;

        var ue4ssFound = LuaModService.HasUe4ss(_gameFolderBox.Text.Trim());
        _ue4ssStatus.Text = ue4ssFound ? "Found" : "Missing";
        _ue4ssStatus.ForeColor = ue4ssFound ? Green : Orange;

        var modFound = LuaModService.IsValidModFolder(_modFolderBox.Text.Trim());
        _modStatus.Text = modFound ? "Found" : "Missing";
        _modStatus.ForeColor = modFound ? Green : Orange;
        RefreshDashboard();
    }

    private void MarkUnsaved(bool unsaved = true)
    {
        if (_loadingUi) return;
        _hasUnsavedChanges = unsaved;
        var edits = _state.CountEdits(_modData);
        _unsavedStatus.Text = unsaved
            ? $"Unsaved changes ({edits})"
            : $"No unsaved changes ({edits} edits)";
        _unsavedStatus.ForeColor = unsaved ? Orange : Cyan;
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        if (_dashboardValues.Count == 0) return;

        var categories = _modData?.Categories.Count ?? 0;
        var entries = _modData?.ItemCount ?? 0;
        var edits = _state.CountEdits(_modData);

        SetDashboardValue("GameStatus", _gameStatus?.Text ?? "Closed", _gameStatus?.ForeColor ?? Orange);
        SetDashboardValue("Ue4ssStatus", _ue4ssStatus?.Text ?? "Missing", _ue4ssStatus?.ForeColor ?? Orange);
        SetDashboardValue("ModStatus", _modStatus?.Text ?? "Missing", _modStatus?.ForeColor ?? Orange);
        SetDashboardValue("LoadedCategories", categories.ToString(CultureInfo.InvariantCulture), categories > 0 ? Green : TextMuted);
        SetDashboardValue("LoadedEntries", entries.ToString(CultureInfo.InvariantCulture), entries > 0 ? Green : TextMuted);
        SetDashboardValue("UnsavedEdits", edits.ToString(CultureInfo.InvariantCulture), _hasUnsavedChanges ? Orange : Cyan);
        SetDashboardValue("LastSave", _lastConfigSaveAt?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "Never", _lastConfigSaveAt.HasValue ? Green : TextMuted);
        var lastBackupAt = LatestBackupAt(_lastConfigBackupAt, _serverState.LastBackupAt);
        SetDashboardValue("LastBackup", lastBackupAt?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "Never", lastBackupAt.HasValue ? Green : TextMuted);

        var serverSummary = _serverState.StatusText + " / " + _serverState.ConnectionText;
        SetDashboardValue("ServerSummary", serverSummary, _serverState.StatusColor);
        SetDashboardValue("ConfigEditsChart", edits > 0 ? $"{edits} current generated edits" : "No data yet", edits > 0 ? Cyan : TextMuted);
        SetDashboardValue("BackupsChart", lastBackupAt.HasValue ? "Last backup at " + lastBackupAt.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture) : "No data yet", lastBackupAt.HasValue ? Green : TextMuted);
        SetDashboardValue("LoadedSummaryChart", _modData == null ? "No mod data loaded yet" : $"{categories} categories\n{entries} entries", _modData == null ? TextMuted : Green);
        SetDashboardValue("ServerActivityChart", _serverState.HasActivity ? "Server Manager log has activity" : "No server activity yet", _serverState.HasActivity ? Cyan : TextMuted);
        SetDashboardValue("StatusHistoryChart", _recentActivityLines.Count == 0 ? "No data yet" : $"{_recentActivityLines.Count} recent events", _recentActivityLines.Count == 0 ? TextMuted : Cyan);

        if (_dashboardActivity != null)
        {
            _dashboardActivity.Text = _recentActivityLines.Count == 0
                ? "Recent activity: No data yet"
                : "Recent activity: " + string.Join("    |    ", _recentActivityLines.TakeLast(3));
        }
    }

    private void SetDashboardValue(string key, string value, Color color)
    {
        if (!_dashboardValues.TryGetValue(key, out var label)) return;

        label.Text = value;
        label.ForeColor = color;
    }

    private static DateTime? LatestBackupAt(params DateTime?[] values)
    {
        return values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty()
            .Max() is { Ticks: > 0 } latest
                ? latest
                : null;
    }

    private string? CurrentCategory() => Convert.ToString(_categoryCombo.SelectedItem);

    private CategoryItem? CurrentItem()
    {
        var category = Convert.ToString(_itemCategoryCombo.SelectedItem);
        var className = _itemCombo.SelectedItem is ItemChoice choice
            ? choice.ClassName
            : Convert.ToString(_itemCombo.SelectedItem);
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(className) || _modData == null) return null;
        return _modData.Categories.TryGetValue(category, out var data)
            ? data.Items.FirstOrDefault(item => item.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
            : null;
    }

    private static string FriendlyClassName(string className)
    {
        var name = className;
        if (name.StartsWith("BP_", StringComparison.OrdinalIgnoreCase)) name = name[3..];
        if (name.EndsWith("_C", StringComparison.OrdinalIgnoreCase)) name = name[..^2];
        name = name.Replace('_', ' ');
        name = LowerToUpperOrDigitPattern.Replace(name, " ");
        name = AcronymBoundaryPattern.Replace(name, " ");
        name = NumberBoundaryPattern.Replace(name, " ");
        name = WhitespacePattern.Replace(name, " ").Trim();
        return string.IsNullOrWhiteSpace(name) ? className : name;
    }

    private static (string Key, string Label, FieldKind Kind)[] GetFieldsForCategory(string category)
    {
        if (category.Equals("vehicles", StringComparison.OrdinalIgnoreCase) || category.Equals("containers", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { ("MaxWeight", "Max Weight", FieldKind.Number) };
        }

        if (category.Equals("backpacks", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                ("Weight", "Weight", FieldKind.Number),
                ("MaxStack", "Max Stack", FieldKind.Number),
                ("bStackable", "Stackable", FieldKind.Bool),
                ("ExtraWeightCapacity", "Extra Weight Capacity", FieldKind.Number),
                ("RunSpeedMultiplier", "Run Speed Multiplier", FieldKind.Number)
            };
        }

        return new[]
        {
            ("Weight", "Weight", FieldKind.Number),
            ("MaxStack", "Max Stack", FieldKind.Number),
            ("bStackable", "Stackable", FieldKind.Bool)
        };
    }

    private void AddCategoryField(string key, string label, FieldKind kind)
    {
        var field = NewFieldPanel(label, 250, 96);
        field.Margin = new Padding(0, 0, 18, 10);
        Control input = kind == FieldKind.Bool
            ? NewCombo(20, 48, 210, BoolChoices)
            : NewNumberTextBox(20, 50, 210, 34);
        AddTip(input, kind == FieldKind.Bool
            ? $"Choose a category-wide default for {label}, or leave Game Default."
            : $"Enter a category-wide number for {label}, or leave blank for game default.");
        input.TextChanged += (_, _) => MarkUnsaved();
        if (input is ThemedComboBox combo) combo.SelectedIndexChanged += (_, _) => MarkUnsaved();
        field.Controls.Add(input);
        _categoryInputs[key] = input;
        _categoryFields.Controls.Add(field);
    }

    private void AddItemField(string key, string label, FieldKind kind)
    {
        var field = NewFieldPanel(label, 250, 110);
        field.Margin = new Padding(0, 0, 18, 10);
        var check = NewCheckBox("Use Game Default", 34, 42, 182);
        check.Checked = true;
        check.BackColor = InnerBack;
        AddTip(check, "Leave checked to keep the game's value. Uncheck to type a generated override.");
        Control input = kind == FieldKind.Bool
            ? NewCombo(25, 68, 200, BoolChoices)
            : NewNumberTextBox(25, 70, 200, 34);
        AddTip(input, kind == FieldKind.Bool
            ? $"Choose an override for {label}, or keep Game Default."
            : $"Type the {label} override for this one item.");
        SetItemFieldEditMode(field, input, editMode: false);
        check.CheckedChanged += (_, _) =>
        {
            SetItemFieldEditMode(field, input, editMode: !check.Checked);
            _itemFields?.PerformLayout();
            MarkUnsaved();
        };
        if (input is ThemedComboBox combo) combo.SelectedIndexChanged += (_, _) => MarkUnsaved();
        else input.TextChanged += (_, _) => MarkUnsaved();
        field.Controls.Add(check);
        field.Controls.Add(input);
        _itemInputs[key] = (check, input);
        _itemFields.Controls.Add(field);
    }

    private static void SetItemFieldEditMode(Control field, Control input, bool editMode)
    {
        input.Enabled = editMode;
        input.Visible = editMode;
        field.Height = editMode ? 112 : 82;
    }

    private bool AddCategoryNumber(Dictionary<string, LuaValue> values, string key, bool allowNegative = true)
    {
        if (!_categoryInputs.TryGetValue(key, out var input) || input is not TextBox box) return true;
        if (!TryReadOptionalNumber(box.Text, key, out var value, out var hasValue, allowNegative)) return false;
        if (hasValue) values[key] = value;
        return true;
    }

    private void AddCategoryBool(Dictionary<string, LuaValue> values, string key)
    {
        if (!_categoryInputs.TryGetValue(key, out var input) || input is not ThemedComboBox combo) return;
        var value = ReadBoolCombo(combo);
        if (!value.IsNil) values[key] = value;
    }

    private bool AddItemNumber(Dictionary<string, LuaValue> values, string key, string label, bool allowNegative = true)
    {
        if (!ReadItemNumber(key, label, out var value, out var hasValue, allowNegative)) return false;
        if (hasValue) values[key] = value;
        return true;
    }

    private void AddItemBool(Dictionary<string, LuaValue> values, string key)
    {
        if (!_itemInputs.TryGetValue(key, out var field) || field.DefaultCheck.Checked || field.Input is not ThemedComboBox combo) return;
        var value = ReadBoolCombo(combo);
        if (!value.IsNil) values[key] = value;
    }

    private bool ReadItemNumber(string key, string label, out LuaValue value, out bool hasValue, bool allowNegative = true)
    {
        value = LuaValue.Nil;
        hasValue = false;
        if (!_itemInputs.TryGetValue(key, out var field) || field.DefaultCheck.Checked || field.Input is not TextBox box) return true;

        var raw = box.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("nil", StringComparison.OrdinalIgnoreCase))
        {
            LogError(label + " must be a number or check Use Game Default.");
            return false;
        }

        return TryReadOptionalNumber(raw, label, out value, out hasValue, allowNegative);
    }

    private bool TryReadOptionalNumber(string raw, string label, out LuaValue value, out bool hasValue, bool allowNegative = true)
    {
        value = LuaValue.Nil;
        hasValue = false;
        raw = raw.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("nil", StringComparison.OrdinalIgnoreCase)) return true;

        if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            LogError(label + " must be a number or blank.");
            return false;
        }

        if (!allowNegative && number < 0)
        {
            LogError(label + " cannot be negative.");
            return false;
        }

        if (number >= 999999) Log(label + " is massive. Allowed for private testing.");
        value = new LuaValue(number);
        hasValue = true;
        return true;
    }

    private static LuaValue ReadBoolCombo(ThemedComboBox combo)
    {
        return combo.SelectedIndex switch
        {
            1 => new LuaValue(true),
            2 => new LuaValue(false),
            _ => LuaValue.Nil
        };
    }

    private static void SetInputValue(Dictionary<string, Control> inputs, string key, LuaValue value)
    {
        if (!inputs.TryGetValue(key, out var input)) return;
        if (input is TextBox box) box.Text = value.ToLua();
        if (input is ThemedComboBox combo && value.Value is bool b) combo.SelectedIndex = b ? 1 : 2;
    }

    private void SetItemInputValue(string key, LuaValue value)
    {
        if (!_itemInputs.TryGetValue(key, out var field)) return;
        field.DefaultCheck.Checked = false;
        SetItemFieldEditMode(field.DefaultCheck.Parent!, field.Input, editMode: true);
        if (field.Input is TextBox box) box.Text = value.ToLua();
        if (field.Input is ThemedComboBox combo && value.Value is bool b) combo.SelectedIndex = b ? 1 : 2;
    }

    private Dictionary<string, LuaValue> GetCategoryDefaults(string category)
    {
        if (!_state.CategoryDefaults.TryGetValue(category, out var values))
        {
            values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
            _state.CategoryDefaults[category] = values;
        }
        return values;
    }

    private Dictionary<string, Dictionary<string, LuaValue>> GetItemOverrides(string category)
    {
        if (!_state.ItemOverrides.TryGetValue(category, out var values))
        {
            values = new Dictionary<string, Dictionary<string, LuaValue>>(StringComparer.OrdinalIgnoreCase);
            _state.ItemOverrides[category] = values;
        }
        return values;
    }

    private Dictionary<string, LuaValue> GetContainerOverrides(string category)
    {
        if (!_state.ContainerWeightOverrides.TryGetValue(category, out var values))
        {
            values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
            _state.ContainerWeightOverrides[category] = values;
        }
        return values;
    }

    private void Log(string msg)
    {
        if (_log == null) return;
        AppendLog(msg, TextMuted);
    }

    private void LogError(string msg)
    {
        if (_log == null) return;
        AppendLog("ERROR: " + msg, Red);
    }

    private void AppendLog(string msg, Color color)
    {
        msg = RedactUserProfile(msg);
        var line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " | " + msg + Environment.NewLine;
        _log.SelectionStart = _log.TextLength;
        _log.SelectionColor = color;
        _log.AppendText(line);
        _log.SelectionColor = _log.ForeColor;
        TrimLog();
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
        TrackRecentActivity(line.TrimEnd());
    }

    private void TrackRecentActivity(string line)
    {
        _recentActivityLines.Add(line);
        if (_recentActivityLines.Count > 12)
        {
            _recentActivityLines.RemoveAt(0);
        }

        RefreshDashboard();
    }

    private void TrimLog()
    {
        var lineCount = _log.Lines.Length;
        if (lineCount <= MaxLogLines) return;

        var firstCharToKeep = _log.GetFirstCharIndexFromLine(lineCount - MaxLogLines);
        if (firstCharToKeep <= 0) return;

        _log.Select(0, firstCharToKeep);
        _log.SelectedText = "";
    }

    private static string RedactUserProfile(string text)
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(profile)
            ? text
            : text.Replace(profile, "~", StringComparison.OrdinalIgnoreCase);
    }

    private static RoundedPanel NewContentPanel()
    {
        return NewPanel(12, 0, 0, 1020, 512);
    }

    private static FlowLayoutPanel NewFieldFlow(int x, int y, int w, int h)
    {
        return new FlowLayoutPanel
        {
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            BackColor = PanelBack,
            AutoScroll = true,
            WrapContents = true
        };
    }

    private static RoundedPanel NewFieldPanel(string label, int width, int height)
    {
        var panel = new RoundedPanel
        {
            Width = width,
            Height = height,
            Margin = new Padding(0, 0, 18, 14),
            Radius = 12,
            FillColor = InnerBack,
            BorderColor = BorderSoft,
            BackColor = PanelBack
        };
        panel.Controls.Add(MakeLabel(label, 18, 12, width - 36, 26, 12, FontStyle.Bold, TextMain, ContentAlignment.MiddleCenter, InnerBack));
        return panel;
    }

    private void ShowReadmePopup()
    {
        using var popup = new Form
        {
            Text = "Readme",
            ClientSize = new Size(520, 400),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppBack,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            ShowInTaskbar = false
        };

        popup.Shown += (_, _) => UseDarkTitleBar(popup.Handle);

        var shell = NewPanel(14, 18, 18, 484, 364);
        shell.BackColor = AppBack;
        popup.Controls.Add(shell);

        shell.Controls.Add(MakeLabel("Readme", 28, 24, 220, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeLabel("Quick setup steps", 30, 64, 360, 24, 12, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeWrappedLabel(
            "1. Select or auto-detect your VEIN folder.\n\n2. Pick a category or item to edit.\n\n3. Change only the values you want.\n\n4. Save Config, then restart VEIN so the Lua mod reloads the new values.",
            30,
            112,
            420,
            190,
            12.5F,
            FontStyle.Regular,
            TextMuted,
            PanelBack));

        popup.ShowDialog(this);
    }

    private static RoundedPanel NewPresetCard(string title, string description, int x, int y, Action action)
    {
        var card = NewPanel(14, x, y, 430, 92);
        card.BackColor = PanelBack;
        card.Controls.Add(MakeLabel(title, 20, 14, 240, 28, 15, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        card.Controls.Add(MakeWrappedLabel(description, 20, 44, 276, 38, 10.5F, FontStyle.Regular, TextMuted, PanelBack));
        var button = NewSmallButton("Apply", 318, 26, 86, 40, main: true);
        button.Click += (_, _) => action();
        card.Controls.Add(button);
        return card;
    }

    private static void AddSetupMessage(Control parent, string text)
    {
        parent.Controls.Add(MakeLabel(text, 8, 8, Math.Max(260, parent.Width - 20), 36, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, parent.BackColor));
    }

    private static ThemedTextBox NewTextBox(int x, int y, int w, int h, bool password = false)
    {
        var box = new ThemedTextBox
        {
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            BackColor = InnerBack,
            ForeColor = TextMain,
            FillColor = InnerBack,
            FocusedFillColor = Color.FromArgb(6, 18, 32),
            BorderColor = Border,
            FocusedBorderColor = Purple,
            TextColor = TextMain,
            Radius = 8,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 12F, FontStyle.Regular)
        };

        if (password)
        {
            box.Multiline = false;
            box.UseSystemPasswordChar = true;
        }

        return box;
    }

    private static ThemedTextBox NewNumberTextBox(int x, int y, int w, int h)
    {
        var box = NewTextBox(x, y, w, h);
        box.TextAlign = HorizontalAlignment.Center;
        return box;
    }

    private static ThemedComboBox NewCombo(int x, int y, int w, IEnumerable<string> items)
    {
        var combo = new ThemedComboBox
        {
            Left = x,
            Top = y,
            Width = w,
            Height = 36,
            BackColor = InnerBack,
            ForeColor = TextMain,
            FillColor = InnerBack,
            BorderColor = Border,
            SelectedColor = Purple,
            TextColor = TextMain,
            MutedColor = TextMuted,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular)
        };
        combo.Items.AddRange(items.Cast<object>().ToArray());
        ConfigureComboDropDown(combo);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    private static void ConfigureComboDropDown(ThemedComboBox combo)
    {
        combo.MaxDropDownItems = Math.Clamp(combo.Items.Count, 1, MaxVisibleComboRows);
        combo.DropDownWidth = combo.Width;
        combo.DropDownHeight = combo.ItemHeight * combo.MaxDropDownItems + 2;
    }

    private static ThemedCheckBox NewCheckBox(string text, int x, int y, int w)
    {
        return new ThemedCheckBox
        {
            Text = text,
            Left = x,
            Top = y,
            Width = w,
            Height = 30,
            ForeColor = TextMuted,
            BackColor = PanelBack,
            FillColor = InnerBack,
            BorderColor = Border,
            CheckedColor = Purple,
            TextColor = TextMuted,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular)
        };
    }

    private static RoundedButton MakeButton(string text, int x, int y, int w, int h, Action action, bool main = false)
    {
        var button = NewSmallButton(text, x, y, w, h, main);
        button.Click += (_, _) => action();
        return button;
    }

    private static RoundedPanel NewPanel(int radius, int x, int y, int w, int h)
    {
        return new RoundedPanel
        {
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            Radius = radius,
            FillColor = PanelBack,
            BorderColor = Border,
            BackColor = AppBack
        };
    }

    private static RoundedPanel NewStatCard(string title, string value, string sub, Color valueColor, int x, int y, int w, int h)
    {
        var panel = NewPanel(12, x, y, w, h);
        const int textLeft = 20;
        const int valueLeft = 16;
        panel.Controls.Add(MakeLabel(title, textLeft, 12, w - 40, 20, 11, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var valueLabel = MakeLabel(value, valueLeft, 28, w - 38, 30, 16, FontStyle.Regular, valueColor, ContentAlignment.MiddleLeft, PanelBack);
        panel.Controls.Add(valueLabel);
        panel.Controls.Add(MakeLabel(sub, textLeft, 58, w - 40, 18, 8.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        panel.Tag = valueLabel;
        return panel;
    }

    private static RoundedPanel NewPill(string text, int x, int y, int w, int h, Color color)
    {
        var panel = new RoundedPanel
        {
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            Radius = 12,
            FillColor = color,
            BorderColor = PurpleLight,
            BackColor = PanelBack
        };
        panel.Controls.Add(MakeLabel(text, 0, 0, w, h, 9, FontStyle.Bold, TextMain, ContentAlignment.MiddleCenter, color));
        return panel;
    }

    private static Panel Line(int x, int y, int w)
    {
        return new Panel { Left = x, Top = y, Width = w, Height = 1, BackColor = BorderSoft };
    }

    private static Label MakeLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color, ContentAlignment align, Color back)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = back,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = align,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
    }

    private static Label MakeWrappedLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color, Color back)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = back,
            AutoSize = false,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.TopLeft,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
    }

    private static RoundedButton NewSmallButton(string text, int x, int y, int w, int h, bool main = false)
    {
        return new RoundedButton
        {
            Text = text,
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            Radius = 10,
            FillColor = main ? Purple : Color.FromArgb(32, 49, 75),
            HoverColor = main ? PurpleLight : Color.FromArgb(43, 65, 99),
            BorderColor = Color.FromArgb(54, 78, 116),
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            TabStop = false,
            FlatStyle = FlatStyle.Flat
        };
    }

    private static RoundedButton NewTabButton(string text, int x, int y, int width)
    {
        return new RoundedButton
        {
            Text = text,
            Left = x,
            Top = y,
            Width = width,
            Height = 44,
            Radius = 14,
            FillColor = InnerBack,
            HoverColor = Color.FromArgb(18, 31, 50),
            BorderColor = BorderSoft,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
    }

    private static RoundedButton NewSidebarTabButton(string text, int x, int y)
    {
        return new RoundedButton
        {
            Text = text,
            Left = x,
            Top = y,
            Width = 136,
            Height = 44,
            Radius = 12,
            FillColor = InnerBack,
            HoverColor = Color.FromArgb(18, 31, 50),
            BorderColor = BorderSoft,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
    }

    private static void UseDarkTitleBar(IntPtr handle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return;

        var enabled = 1;
        if (DwmSetWindowAttribute(handle, 20, ref enabled, Marshal.SizeOf<int>()) != 0)
        {
            _ = DwmSetWindowAttribute(handle, 19, ref enabled, Marshal.SizeOf<int>());
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static readonly Regex LowerToUpperOrDigitPattern = new(@"(?<=[a-z])(?=[A-Z0-9])", RegexOptions.Compiled);
    private static readonly Regex AcronymBoundaryPattern = new(@"(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);
    private static readonly Regex NumberBoundaryPattern = new(@"(?<=\D)(?=\d)", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    private sealed class LocalPathSettings
    {
        public string? GameFolder { get; set; }
        public string? ModFolder { get; set; }
    }

    private enum FieldKind
    {
        Number,
        Bool
    }

    private sealed record ItemChoice(string ClassName, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed class ServerManagerUiState
    {
        public string StatusText { get; set; } = "Stopped";
        public Color StatusColor { get; set; } = Orange;
        public string ConfigText { get; set; } = "Not Saved";
        public Color ConfigColor { get; set; } = Orange;
        public string ConnectionText { get; set; } = "Not Connected";
        public Color ConnectionColor { get; set; } = Orange;
        public DateTime? LastBackupAt { get; set; }
        public bool HasActivity { get; set; }
    }
}
