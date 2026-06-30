using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace VEIN_Item_And_Container_Modifier;

public sealed partial class MainForm : Form
{
    private static readonly Color AppBack = Color.FromArgb(4, 7, 14);
    private static readonly Color PanelBack = Color.FromArgb(9, 15, 27);
    private static readonly Color InnerBack = Color.FromArgb(6, 11, 21);
    private static readonly Color SidebarBack = Color.FromArgb(8, 14, 25);
    private static readonly Color Border = Color.FromArgb(28, 47, 76);
    private static readonly Color BorderSoft = Color.FromArgb(21, 36, 58);
    private static readonly Color TextMain = Color.White;
    private static readonly Color TextMuted = Color.FromArgb(184, 199, 224);
    private static readonly Color TextDim = Color.FromArgb(128, 142, 169);
    private static readonly Color Purple = Color.FromArgb(143, 18, 34);
    private static readonly Color PurpleLight = Color.FromArgb(190, 38, 56);
    private static readonly Color Green = Color.FromArgb(0, 255, 102);
    private static readonly Color Orange = Color.FromArgb(255, 112, 0);
    private static readonly Color Amber = Color.FromArgb(240, 167, 60);
    private static readonly Color Red = Color.FromArgb(255, 87, 87);
    private static readonly Color Cyan = Color.FromArgb(18, 223, 213);
    private const int SidebarLeft = 20;
    private const int SidebarTop = 54;
    private const int SidebarWidth = 220;
    private const int ContentLeft = 296;
    private const int ContentRightPadding = 44;
    private const int ContentTop = 154;
    private const int ScriptsContentTop = 70;
    private const int ServerContentTop = 262;
    private const int ResizeBorderWidth = 8;
    private const int MaxVisibleComboRows = 14;
    private static readonly string[] BoolChoices = { "Game Default", "True", "False" };

    private readonly UiConfigState _state = new();
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 1000 };

    private TextBox _gameFolderBox = null!;
    private TextBox _modFolderBox = null!;
    private Label _gameStatus = null!;
    private Label _ue4ssStatus = null!;
    private Label _modStatus = null!;
    private Label _headerTitle = null!;
    private Label _headerSubtitle = null!;
    private Panel _titleBar = null!;
    private Button _minimizeButton = null!;
    private Button _maximizeButton = null!;
    private Button _closeButton = null!;
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
    private RoundedPanel _sidebarFooter = null!;
    private readonly Dictionary<string, Control> _tabPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoundedButton> _tabButtons = new(StringComparer.Ordinal);
    private string _activeTab = "Scripts";
    private readonly Dictionary<string, Control> _setupSubPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoundedButton> _setupSubButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _modsSubPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoundedButton> _modsSubButtons = new(StringComparer.Ordinal);
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

        AutoDetectPaths(log: true);
        LoadModFromPath();

        _statusTimer.Tick += (_, _) => UpdateStatuses();
        _statusTimer.Start();
        UpdateStatuses();
    }

    private void InitializeComponent()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Vein Mod Manager";
        ClientSize = new Size(1553, 1013);
        MinimumSize = new Size(1280, 820);
        FormBorderStyle = FormBorderStyle.None;
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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!IsDesignerHosted || Controls.Count > 0) return;

        DrawDesignerPreview(e.Graphics);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsDesignerHosted) return;

        ApplyResponsiveLayout();
    }

    protected override void WndProc(ref Message m)
    {
        const int wmNcHitTest = 0x0084;
        const int htClient = 1;

        base.WndProc(ref m);
        if (IsDesignerHosted || m.Msg != wmNcHitTest || WindowState == FormWindowState.Maximized)
        {
            return;
        }

        if ((int)m.Result != htClient)
        {
            return;
        }

        var cursor = PointToClient(Cursor.Position);
        var left = cursor.X <= ResizeBorderWidth;
        var right = cursor.X >= ClientSize.Width - ResizeBorderWidth;
        var top = cursor.Y <= ResizeBorderWidth;
        var bottom = cursor.Y >= ClientSize.Height - ResizeBorderWidth;

        m.Result = (IntPtr)((left, right, top, bottom) switch
        {
            (true, false, true, false) => 13,
            (false, true, true, false) => 14,
            (true, false, false, true) => 16,
            (false, true, false, true) => 17,
            (true, false, false, false) => 10,
            (false, true, false, false) => 11,
            (false, false, true, false) => 12,
            (false, false, false, true) => 15,
            _ => htClient
        });
    }

    private static bool IsDesignerHosted =>
        LicenseManager.UsageMode == LicenseUsageMode.Designtime
        || Process.GetCurrentProcess().ProcessName.Contains("devenv", StringComparison.OrdinalIgnoreCase)
        || Process.GetCurrentProcess().ProcessName.Contains("DesignToolsServer", StringComparison.OrdinalIgnoreCase);

    private void BuildUi()
    {
        Controls.Clear();
        BuildTitleBar();
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
            _sidebar.Height = Math.Max(0, ClientSize.Height - SidebarTop - 20);
        }

        if (_titleBar != null)
        {
            _titleBar.Width = ClientSize.Width;
        }

        if (_minimizeButton != null && _maximizeButton != null && _closeButton != null)
        {
            _closeButton.Left = ClientSize.Width - 48;
            _maximizeButton.Left = ClientSize.Width - 96;
            _minimizeButton.Left = ClientSize.Width - 144;
        }

        if (_sidebarFooter != null && _sidebar != null)
        {
            _sidebarFooter.Height = 120;
            _sidebarFooter.Left = 2;
            _sidebarFooter.Top = Math.Max(0, _sidebar.Height - _sidebarFooter.Height - 20);
            _sidebarFooter.Width = Math.Max(0, _sidebar.Width - 4);
        }

        if (_settingsButton != null)
        {
            _settingsButton.Left = Math.Max(ContentLeft, ClientSize.Width - 84);
        }

        if (_headerSubtitle != null)
        {
            _headerSubtitle.Width = Math.Max(360, ClientSize.Width - ContentLeft - 190);
        }

        if (_contentShell != null)
        {
            var serverSelected = _activeTab.Equals("Server Manager", StringComparison.Ordinal);
            var fullPageSelected = _activeTab.Equals("Mods", StringComparison.Ordinal) || _activeTab.Equals("Scripts", StringComparison.Ordinal);
            _contentShell.Left = ContentLeft;
            _contentShell.Top = fullPageSelected ? ScriptsContentTop : serverSelected ? ServerContentTop : ContentTop;
            _contentShell.Width = Math.Max(720, ClientSize.Width - ContentLeft - ContentRightPadding);
            _contentShell.Height = Math.Max(420, ClientSize.Height - _contentShell.Top - 40);

            foreach (var page in _tabPages.Values)
            {
                page.Width = _contentShell.Width;
                page.Height = _contentShell.Height;
            }

            ResizeServerManagerLayout();
            ResizeModsLayout();
        }
    }

    private void ResizeModsLayout()
    {
        if (_tabPages.TryGetValue("Mods", out var modsPage))
        {
            foreach (var subPage in _modsSubPages.Values)
            {
                subPage.Width = Math.Max(760, modsPage.Width);
                subPage.Height = Math.Max(420, modsPage.Height - subPage.Top);
            }
        }

        if (_contentShell != null && _tabPages.TryGetValue("Scripts", out var scriptsPage))
        {
            scriptsPage.Left = 0;
            scriptsPage.Top = 0;
            scriptsPage.Width = _contentShell.Width;
            scriptsPage.Height = _contentShell.Height;
        }
    }

    private void ResizeServerManagerLayout()
    {
        if (_serverOverviewControls.Count > 0)
        {
            var x = ContentLeft;
            var y = 154;
            var gap = 18;
            var available = Math.Max(900, ClientSize.Width - x - ContentRightPadding);
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

        DrawPreviewPanel(graphics, new Rectangle(SidebarLeft, SidebarTop, SidebarWidth, 900), 18, SidebarBack, BorderSoft);
        DrawPreviewLogo(graphics, new Rectangle(48, 112, 164, 150));
        DrawPreviewLine(graphics, 40, 298, SidebarWidth - 44);
        DrawPreviewTab(graphics, "Dashboard", 36, 256, SidebarWidth - 36, selected: false);
        DrawPreviewTab(graphics, "Setup", 36, 326, SidebarWidth - 36, selected: false);
        DrawPreviewTab(graphics, "Server Manager", 36, 396, SidebarWidth - 36, selected: false);
        DrawPreviewTab(graphics, "Mods", 36, 466, SidebarWidth - 36, selected: false);
        DrawPreviewTab(graphics, "Scripts", 36, 536, SidebarWidth - 36, selected: true);
        DrawPreviewTab(graphics, "Log", 36, 606, SidebarWidth - 36, selected: false);
        DrawPreviewPanel(graphics, new Rectangle(34, 868, SidebarWidth - 32, 120), 8, InnerBack, BorderSoft);
        DrawPreviewText(graphics, "Vein Mod Manager", 50, 886, 9.5F, FontStyle.Regular, TextMuted, width: 150);
        DrawPreviewText(graphics, "v1.0.0", 50, 912, 8.5F, FontStyle.Regular, TextDim, width: 150);
        DrawPreviewText(graphics, "System Online", 50, 944, 8.5F, FontStyle.Bold, Green, width: 150);

        DrawPreviewText(graphics, "Vein Manager", ContentLeft, 76, 28, FontStyle.Bold, TextMain);
        DrawPreviewText(graphics, "Modify VEIN item, backpack, vehicle, and container values without editing config files.", ContentLeft + 2, 120, 13, FontStyle.Regular, TextMuted);
        DrawPreviewPanel(graphics, new Rectangle(1196, 58, 52, 52), 12, InnerBack, BorderSoft);
        DrawPreviewText(graphics, "\uE713", 1211, 72, 17, FontStyle.Regular, TextMain, "Segoe MDL2 Assets");

        DrawPreviewPanel(graphics, new Rectangle(ContentLeft, ContentTop, 998, 606), 12, PanelBack, Border);
        DrawPreviewText(graphics, "Dashboard", ContentLeft + 28, ContentTop + 34, 20, FontStyle.Bold, TextMain);
        DrawPreviewText(graphics, "Overview, loaded data, config activity, and server status at a glance.", ContentLeft + 30, ContentTop + 72, 13, FontStyle.Regular, TextMuted);
        DrawPreviewStatusCard(graphics, "Game Status", "Closed", "VEIN process", Orange, ContentLeft + 28, ContentTop + 116);
        DrawPreviewStatusCard(graphics, "UE4SS Status", "Found", "Detected in game folder", Green, ContentLeft + 274, ContentTop + 116);
        DrawPreviewStatusCard(graphics, "Mod Status", "Found", "ItemAndContainerModifier", Green, ContentLeft + 520, ContentTop + 116);
        DrawPreviewStatusCard(graphics, "Server Summary", "Stopped / Local", "Server Manager status", Orange, ContentLeft + 766, ContentTop + 116);
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
            using var captionBack = new SolidBrush(Color.FromArgb(220, 8, 5, 6));
            graphics.FillRectangle(captionBack, bounds.Left, bounds.Bottom - 34, bounds.Width, 34);
            DrawPreviewText(graphics, "MOD MANAGER", bounds.Left, bounds.Bottom - 26, 9.5F, FontStyle.Bold, TextMuted, width: bounds.Width, alignment: StringAlignment.Center);
            return;
        }

        DrawPreviewPanel(graphics, bounds, 2, Color.Black, BorderSoft);
        DrawPreviewText(graphics, "VEIN", bounds.Left + 18, bounds.Top + 32, 24, FontStyle.Bold, TextMain);
        DrawPreviewText(graphics, "MOD MANAGER", bounds.Left, bounds.Bottom - 26, 9.5F, FontStyle.Bold, TextMuted, width: bounds.Width, alignment: StringAlignment.Center);
    }

    private static void DrawPreviewStatusCard(Graphics graphics, string title, string value, string subtitle, Color valueColor, int x, int y)
    {
        const int width = 218;
        DrawPreviewPanel(graphics, new Rectangle(x, y, width, 86), 12, Color.FromArgb(10, 17, 31), BorderSoft);
        DrawPreviewText(graphics, title, x + 22, y + 12, 10.5F, FontStyle.Regular, TextMuted, width: width - 44);
        DrawPreviewText(graphics, value, x + 20, y + 30, 16, FontStyle.Regular, valueColor, width: width - 42);
        DrawPreviewText(graphics, subtitle, x + 22, y + 60, 8.5F, FontStyle.Regular, TextMuted, width: width - 44);
    }

    private static void DrawPreviewTab(Graphics graphics, string text, int x, int y, int width, bool selected)
    {
        DrawPreviewPanel(graphics, new Rectangle(x, y, width, 46), 8, selected ? Purple : InnerBack, selected ? PurpleLight : BorderSoft);
        DrawPreviewText(graphics, text, x + 44, y + 14, 9.5F, FontStyle.Bold, TextMain, width: width - 56);
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

    private void BuildTitleBar()
    {
        _titleBar = new Panel
        {
            Left = 0,
            Top = 0,
            Width = ClientSize.Width,
            Height = 52,
            BackColor = Color.FromArgb(19, 21, 29)
        };
        _titleBar.MouseDown += TitleBarMouseDown;
        Controls.Add(_titleBar);

        if (LoadLogoImage() is { } iconImage)
        {
            var icon = new PictureBox
            {
                Left = 22,
                Top = 18,
                Width = 20,
                Height = 20,
                Image = iconImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = _titleBar.BackColor
            };
            icon.MouseDown += TitleBarMouseDown;
            _titleBar.Controls.Add(icon);
        }

        var title = MakeLabel("Vein Mod Manager", 56, 14, 220, 28, 10F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, _titleBar.BackColor);
        title.MouseDown += TitleBarMouseDown;
        _titleBar.Controls.Add(title);

        _minimizeButton = MakeWindowButton("\uE921", () => WindowState = FormWindowState.Minimized);
        _maximizeButton = MakeWindowButton("\uE922", ToggleWindowMaximized);
        _closeButton = MakeWindowButton("\uE8BB", Close);
        _titleBar.Controls.Add(_minimizeButton);
        _titleBar.Controls.Add(_maximizeButton);
        _titleBar.Controls.Add(_closeButton);

        _titleBar.Controls.Add(new Panel
        {
            Left = 0,
            Top = 51,
            Width = ClientSize.Width,
            Height = 1,
            BackColor = Color.FromArgb(28, 32, 48),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        });
    }

    private static Button MakeWindowButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Top = 0,
            Width = 48,
            Height = 52,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(19, 21, 29),
            ForeColor = TextDim,
            Font = new Font("Segoe MDL2 Assets", 9F, FontStyle.Regular),
            TabStop = false,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(31, 35, 49);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(41, 46, 64);
        button.Click += (_, _) => action();
        return button;
    }

    private void TitleBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        ReleaseCapture();
        _ = SendMessage(Handle, 0x00A1, (IntPtr)2, IntPtr.Zero);
    }

    private void ToggleWindowMaximized()
    {
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void BuildSidebar()
    {
        _sidebar = NewPanel(18, SidebarLeft, SidebarTop, SidebarWidth, 740);
        _sidebar.FillColor = SidebarBack;
        _sidebar.BorderColor = BorderSoft;
        Controls.Add(_sidebar);

        var logo = new VeinLogoPanel
        {
            Left = 28,
            Top = 58,
            Width = 164,
            Height = 136,
            BackColor = SidebarBack,
            GlowColor = Color.FromArgb(185, 24, 38),
            AccentColor = Color.FromArgb(185, 24, 38),
            MainColor = Color.White
        };
        _sidebar.Controls.Add(logo);
        _sidebar.Controls.Add(Line(20, 200, SidebarWidth - 40));

        _sidebarFooter = NewPanel(8, 2, 814, SidebarWidth - 4, 120);
        _sidebarFooter.FillColor = InnerBack;
        _sidebarFooter.BorderColor = BorderSoft;
        _sidebarFooter.BackColor = SidebarBack;
        _sidebarFooter.Controls.Add(MakeLabel("Vein Mod Manager", 18, 18, _sidebarFooter.Width - 36, 22, 9.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, InnerBack));
        _sidebarFooter.Controls.Add(MakeLabel("v1.0.0", 18, 44, _sidebarFooter.Width - 36, 22, 9.5F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, InnerBack));
        _sidebarFooter.Controls.Add(MakeLabel("\u2022 System Online", 18, 76, 130, 24, 10F, FontStyle.Regular, Green, ContentAlignment.MiddleLeft, InnerBack));
        var footerSettings = MakeButton("\uE713", _sidebarFooter.Width - 42, 76, 30, 30, () => ShowTab("Settings"));
        footerSettings.Font = new Font("Segoe MDL2 Assets", 11F, FontStyle.Regular);
        footerSettings.FillColor = Color.FromArgb(16, 20, 30);
        footerSettings.HoverColor = Color.FromArgb(26, 31, 44);
        footerSettings.BorderColor = Color.FromArgb(36, 42, 56);
        footerSettings.Radius = 8;
        _sidebarFooter.Controls.Add(footerSettings);
        _sidebar.Controls.Add(_sidebarFooter);
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

    private static Image? LoadLogoImage()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.png"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "vein-logo.png"))
        };

        var logoPath = candidates.FirstOrDefault(File.Exists);
        return logoPath == null ? null : Image.FromFile(logoPath);
    }

    private void BuildHeader()
    {
        _headerTitle = MakeLabel("Vein Manager", ContentLeft, 54, 610, 44, 28, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, AppBack);
        Controls.Add(_headerTitle);
        _headerSubtitle = MakeLabel("Modify VEIN item, backpack, vehicle, and container values without editing config files.", ContentLeft + 2, 100, 880, 28, 13, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, AppBack);
        Controls.Add(_headerSubtitle);

        _settingsButton = MakeButton("\uE713", 1196, 50, 52, 52, () => ShowTab("Settings"));
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
        var x = ContentLeft;
        var y = 154;
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
        var x = ContentLeft;
        var y = 154;
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
            Left = ContentLeft,
            Top = ContentTop,
            Width = 998,
            Height = 570,
            BackColor = AppBack
        };
        _contentShell = shell;

        _tabPages["Dashboard"] = BuildDashboardTab();
        _tabPages["Setup"] = BuildSetupTab();
        _tabPages["Settings"] = BuildSettingsTab();
        _tabPages["Server Manager"] = BuildServerManagerTab();
        _tabPages["Mods"] = BuildModsTab();
        _tabPages["Scripts"] = BuildScriptsTab();
        _tabPages["Log"] = BuildLogTab();

        var y = 202;
        foreach (var title in new[] { "Dashboard", "Setup", "Server Manager", "Mods", "Scripts", "Log" })
        {
            var button = NewSidebarTabButton(title, 14, y);
            button.Click += (_, _) => ShowTab(title);
            _tabButtons[title] = button;
            _sidebar.Controls.Add(button);
            y += 70;
        }

        foreach (var page in _tabPages.Values)
        {
            page.Visible = false;
            shell.Controls.Add(page);
        }

        Controls.Add(shell);
        ShowTab("Scripts");
    }


    private void ShowTab(string title)
    {
        _activeTab = title;

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
            var scriptsButton = name.Equals("Scripts", StringComparison.Ordinal);
            button.FillColor = selected ? (scriptsButton ? Color.FromArgb(63, 42, 128) : Purple) : InnerBack;
            button.HoverColor = selected ? (scriptsButton ? Color.FromArgb(82, 55, 166) : PurpleLight) : Color.FromArgb(18, 31, 50);
            button.BorderColor = selected ? (scriptsButton ? Color.FromArgb(126, 88, 255) : PurpleLight) : BorderSoft;
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
        var scriptsSelected = title.Equals("Scripts", StringComparison.Ordinal);
        var fullPageSelected = title.Equals("Mods", StringComparison.Ordinal) || scriptsSelected;
        _headerTitle.Visible = !fullPageSelected;
        _headerSubtitle.Visible = (dashboardSelected || setupSelected) && !fullPageSelected;
        if (_settingsButton != null)
        {
            _settingsButton.Visible = !fullPageSelected;
        }
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
            _contentShell.Top = fullPageSelected
                ? ScriptsContentTop
                : serverSelected ? ServerContentTop : ContentTop;
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

    private RoundedPanel BuildModsTab()
    {
        var panel = new RedGlowPanel
        {
            Left = 0,
            Top = 0,
            Width = 1020,
            Height = 690,
            Radius = 0,
            FillColor = AppBack,
            BorderColor = AppBack,
            BackColor = AppBack,
            GlowColor = Color.FromArgb(120, 18, 30)
        };
        _modsSubPages.Clear();
        _modsSubButtons.Clear();

        var pageTitle = MakeLabel("Mods", 0, 0, 360, 42, 23, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, Color.Transparent);
        var subtitle = MakeLabel("Active mods loaded on your server. Enable, disable or remove them.", 0, 42, 780, 26, 11.5F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, Color.Transparent);
        panel.Controls.Add(pageTitle);
        panel.Controls.Add(subtitle);

        var x = 0;
        foreach (var (title, width) in new[] { ("Installed Mods", 112), ("Nexus Search", 112) })
        {
            var button = NewTabButton(title, x, 78, width);
            button.Height = 38;
            button.Radius = 8;
            button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            button.Click += (_, _) => ShowModsSubTab(title);
            _modsSubButtons[title] = button;
            panel.Controls.Add(button);
            x += width + 10;
        }

        _modsSubPages["Installed Mods"] = BuildInstalledModsPane();
        _modsSubPages["Nexus Search"] = BuildNexusSearchPane();

        foreach (var page in _modsSubPages.Values)
        {
            page.Top = 126;
            page.Width = panel.Width;
            page.Height = Math.Max(420, panel.Height - page.Top);
            page.Visible = false;
            panel.Controls.Add(page);
        }

        ShowModsSubTab("Installed Mods");
        return panel;
    }

    private void ShowModsSubTab(string title)
    {
        foreach (var (name, page) in _modsSubPages)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            page.Visible = selected;
            if (selected)
            {
                page.BringToFront();
            }
        }

        foreach (var (name, button) in _modsSubButtons)
        {
            var selected = name.Equals(title, StringComparison.Ordinal);
            button.FillColor = selected ? Purple : InnerBack;
            button.HoverColor = selected ? PurpleLight : Color.FromArgb(18, 24, 40);
            button.BorderColor = selected ? PurpleLight : BorderSoft;
            button.Invalidate();
        }
    }

    private RoundedPanel BuildInstalledModsPane()
    {
        var pane = NewPanel(0, 0, 0, 1020, 560);
        pane.FillColor = AppBack;
        pane.BorderColor = AppBack;

        pane.Controls.Add(MakeLabel("6 of 8 active", 0, 0, 86, 24, 10F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, Color.Transparent));
        pane.Controls.Add(MakeLabel("1 update available", 88, 0, 140, 24, 10F, FontStyle.Bold, Amber, ContentAlignment.MiddleLeft, Color.Transparent));

        var checkUpdates = MakeButton("\u27F3 Check for updates", 838, -2, 164, 34, () => ShowModsSubTab("Nexus Search"));
        checkUpdates.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        checkUpdates.FillColor = Color.FromArgb(12, 15, 24);
        checkUpdates.BorderColor = Color.FromArgb(49, 43, 55);
        checkUpdates.HoverColor = Color.FromArgb(24, 26, 38);
        pane.Controls.Add(checkUpdates);

        var list = NewPanel(12, 0, 42, 500, 466);
        list.FillColor = Color.FromArgb(10, 16, 31);
        list.BorderColor = Color.FromArgb(18, 28, 47);
        pane.Controls.Add(list);

        list.Controls.Add(MakeLabel("#", 20, 14, 30, 20, 8.5F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, list.FillColor));
        list.Controls.Add(MakeLabel("MOD", 64, 14, 180, 20, 8.5F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, list.FillColor));
        var versionHeader = MakeLabel("VERSION", 328, 14, 82, 20, 8.5F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, list.FillColor);
        var enabledHeader = MakeLabel("ENABLED", 408, 14, 78, 20, 8.5F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, list.FillColor);
        var divider = Line(14, 38, 470);
        list.Controls.Add(versionHeader);
        list.Controls.Add(enabledHeader);
        list.Controls.Add(divider);

        var mods = new[]
        {
            ("01", "Expanded Stash", "GraveDigger", "Inventory", "2.2.0", false, false),
            ("02", "Dynamic Weather", "StormByte", "Environment", "1.4.2", false, false),
            ("03", "Horde Nights", "NecroDev", "Gameplay", "3.0.1", true, false),
            ("04", "Champlain Valley Map", "VeinCartography", "Maps", "1.0.0", true, false),
            ("05", "Quick Stack", "ToolboxModding", "QoL", "0.9.4", true, true),
            ("06", "Reworked Loot Tables", "LootLord", "Gameplay", "1.2.0", true, false),
            ("07", "Drivable Vehicles", "GearHead", "Vehicles", "0.5.0", true, false),
            ("08", "Night Vision Optics", "Spectra", "Equipment", "1.1.3", true, false)
        };

        var rowTop = 42;
        foreach (var mod in mods)
        {
            list.Controls.Add(BuildInstalledModRow(mod.Item1, mod.Item2, mod.Item3, mod.Item4, mod.Item5, mod.Item6, mod.Item7, rowTop));
            rowTop += 45;
        }

        var details = BuildModDetailsPane(516, 42);
        pane.Controls.Add(details);
        pane.Resize += (_, _) => ResizeInstalledModsPane(pane, list, details, checkUpdates, versionHeader, enabledHeader, divider);
        ResizeInstalledModsPane(pane, list, details, checkUpdates, versionHeader, enabledHeader, divider);
        return pane;
    }

    private static void ResizeInstalledModsPane(
        Control pane,
        RoundedPanel list,
        RoundedPanel details,
        Control checkUpdates,
        Control versionHeader,
        Control enabledHeader,
        Control divider)
    {
        var availableWidth = Math.Max(820, pane.Width);
        var availableHeight = Math.Max(450, pane.Height);
        var gap = 18;
        var listWidth = Math.Max(500, Math.Min(820, (int)(availableWidth * 0.58)));
        var detailsWidth = Math.Min(520, Math.Max(310, availableWidth - listWidth - gap));
        var cardHeight = Math.Max(420, Math.Min(466, availableHeight - list.Top - 24));

        checkUpdates.Left = Math.Max(0, availableWidth - checkUpdates.Width - 18);

        list.Width = listWidth;
        list.Height = cardHeight;
        details.Left = list.Right + gap;
        details.Width = detailsWidth;
        details.Height = cardHeight;

        versionHeader.Left = Math.Max(280, list.Width - 170);
        enabledHeader.Left = Math.Max(360, list.Width - 92);
        divider.Width = Math.Max(120, list.Width - 28);

        foreach (Control control in list.Controls)
        {
            if (control is RoundedPanel row && row.Tag is string tag && tag.Equals("installed-mod-row", StringComparison.Ordinal))
            {
                row.Width = list.Width - 2;
            }
        }
    }

    private RoundedPanel BuildInstalledModRow(string index, string name, string author, string category, string version, bool enabled, bool selected, int y)
    {
        var row = NewPanel(0, 0, y, 498, 45);
        row.Tag = "installed-mod-row";
        row.FillColor = selected ? Color.FromArgb(30, 19, 34) : Color.FromArgb(10, 16, 31);
        row.BorderColor = row.FillColor;
        row.BackColor = Color.FromArgb(10, 16, 31);

        if (selected)
        {
            row.Controls.Add(new Panel { Left = 0, Top = 0, Width = 3, Height = 45, BackColor = PurpleLight });
        }

        row.Controls.Add(MakeLabel(index, 18, 12, 30, 20, 9F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, row.FillColor));
        var thumb = NewPanel(5, 52, 8, 30, 30);
        thumb.FillColor = Color.FromArgb(19, 27, 45);
        thumb.BorderColor = Color.FromArgb(28, 39, 63);
        thumb.BackColor = row.FillColor;
        row.Controls.Add(thumb);

        var nameLabel = MakeLabel(name, 96, 5, 160, 20, 10F, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, row.FillColor);
        row.Controls.Add(nameLabel);
        if (selected)
        {
            row.Controls.Add(NewPill("UPDATE", 166, 7, 54, 20, Color.FromArgb(83, 47, 14)));
        }

        var authorLabel = MakeLabel($"by {author} \u00B7 {category}", 96, 24, 194, 18, 8.2F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, row.FillColor);
        row.Controls.Add(authorLabel);
        var versionLabel = MakeLabel(version, 328, 10, 70, 22, 9F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, row.FillColor);
        versionLabel.Font = new Font("Consolas", 9F, FontStyle.Regular);
        row.Controls.Add(versionLabel);

        var toggle = new ToggleSwitch
        {
            Left = 408,
            Top = 12,
            Width = 38,
            Height = 21,
            BackColor = row.FillColor,
            Checked = enabled,
            OnColor = Color.FromArgb(46, 126, 79),
            OnColor2 = Color.FromArgb(92, 217, 138),
            OffColor = Color.FromArgb(29, 38, 61),
            OffColor2 = Color.FromArgb(72, 83, 122)
        };
        row.Controls.Add(toggle);
        var menuLabel = MakeLabel("\u22EE", 466, 10, 18, 24, 12F, FontStyle.Bold, TextDim, ContentAlignment.MiddleCenter, row.FillColor);
        row.Controls.Add(menuLabel);
        row.Resize += (_, _) =>
        {
            versionLabel.Left = Math.Max(300, row.Width - 170);
            toggle.Left = Math.Max(380, row.Width - 90);
            menuLabel.Left = Math.Max(450, row.Width - 32);
            nameLabel.Width = Math.Max(150, versionLabel.Left - nameLabel.Left - 18);
            authorLabel.Width = nameLabel.Width;
        };
        return row;
    }

    private RoundedPanel BuildModDetailsPane(int x, int y)
    {
        var details = NewPanel(12, x, y, 310, 466);
        details.FillColor = Color.FromArgb(10, 16, 31);
        details.BorderColor = Color.FromArgb(18, 28, 47);

        details.Controls.Add(MakeLabel("MOD DETAILS", 18, 16, 160, 22, 9F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, details.FillColor));
        details.Controls.Add(Line(14, 44, 282));
        var thumb = NewPanel(8, 18, 62, 56, 56);
        thumb.FillColor = Color.FromArgb(19, 27, 45);
        thumb.BorderColor = Color.FromArgb(28, 39, 63);
        thumb.BackColor = details.FillColor;
        details.Controls.Add(thumb);

        details.Controls.Add(MakeLabel("Quick Stack", 92, 62, 180, 26, 13F, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, details.FillColor));
        details.Controls.Add(MakeLabel("by ToolboxModding", 92, 86, 180, 18, 8.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, details.FillColor));
        details.Controls.Add(MakeLabel("QoL", 92, 104, 70, 18, 8F, FontStyle.Regular, PurpleLight, ContentAlignment.MiddleLeft, details.FillColor));

        var update = NewPanel(8, 18, 128, 274, 40);
        update.FillColor = Color.FromArgb(39, 32, 29);
        update.BorderColor = Color.FromArgb(77, 59, 39);
        update.BackColor = details.FillColor;
        update.Controls.Add(MakeLabel("Update available: 1.0.0", 14, 10, 150, 20, 9F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, update.FillColor));
        var updateNow = MakeButton("Update now", 182, 7, 84, 26, () => ShowModsSubTab("Nexus Search"));
        updateNow.FillColor = Amber;
        updateNow.HoverColor = Color.FromArgb(255, 190, 84);
        updateNow.BorderColor = Color.FromArgb(255, 198, 104);
        updateNow.ForeColor = Color.FromArgb(20, 14, 8);
        updateNow.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        update.Controls.Add(updateNow);
        details.Controls.Add(update);

        details.Controls.Add(BuildDetailStat("INSTALLED", "0.9.4", 18, 184));
        details.Controls.Add(BuildDetailStat("SIZE", "0.4 MB", 162, 184));
        details.Controls.Add(BuildDetailStat("ENDORSEMENTS", "18.2k", 18, 238));
        details.Controls.Add(BuildDetailStat("STATUS", "Enabled", 162, 238, Green));

        details.Controls.Add(MakeLabel("DESCRIPTION", 18, 288, 140, 18, 8F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, details.FillColor));
        details.Controls.Add(MakeWrappedLabel("Adds a one-click deposit-all button to nearby storage and automatically sorts your inventory by type.", 18, 308, 260, 38, 8.6F, FontStyle.Regular, TextMuted, details.FillColor));
        details.Controls.Add(MakeButton("Disable", 18, 352, 132, 34, () => Log("Select a managed mod before changing status.")));
        var remove = MakeButton("Remove mod", 164, 352, 128, 34, () => Log("Select a managed mod before removing it."));
        remove.FillColor = Color.FromArgb(14, 16, 26);
        remove.HoverColor = Color.FromArgb(42, 18, 27);
        remove.BorderColor = Color.FromArgb(80, 35, 43);
        remove.ForeColor = Color.FromArgb(240, 96, 106);
        details.Controls.Add(remove);
        return details;
    }

    private static RoundedPanel BuildDetailStat(string title, string value, int x, int y, Color? valueColor = null)
    {
        var card = NewPanel(8, x, y, 130, 48);
        card.FillColor = Color.FromArgb(7, 12, 23);
        card.BorderColor = Color.FromArgb(16, 26, 43);
        card.BackColor = Color.FromArgb(10, 16, 31);
        card.Controls.Add(MakeLabel(title, 12, 7, 100, 14, 7.5F, FontStyle.Bold, TextDim, ContentAlignment.MiddleLeft, card.FillColor));
        var valueLabel = MakeLabel(value, 12, 24, 100, 18, 10F, FontStyle.Bold, valueColor ?? TextMuted, ContentAlignment.MiddleLeft, card.FillColor);
        valueLabel.Font = new Font("Consolas", 10F, FontStyle.Bold);
        card.Controls.Add(valueLabel);
        return card;
    }

    private RoundedPanel BuildNexusSearchPane()
    {
        var panel = NewPanel(12, 0, 0, 826, 508);
        panel.FillColor = Color.FromArgb(10, 16, 31);
        panel.BorderColor = Color.FromArgb(18, 28, 47);
        panel.Controls.Add(MakeLabel("Nexus Search", 24, 24, 240, 28, 16F, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, panel.FillColor));
        panel.Controls.Add(MakeLabel("Search and install support will appear here.", 24, 60, 420, 24, 10F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, panel.FillColor));
        return panel;
    }

    private RoundedPanel BuildScriptsTab()
    {
        var panel = NewPanel(0, 0, 0, 1213, 900);
        panel.BorderColor = AppBack;
        panel.FillColor = AppBack;

        panel.Controls.Add(MakeLabel("Scripts", 0, 24, 150, 48, 26F, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, AppBack));
        panel.Controls.Add(MakeLabel("Community automation for your server \u2014 scheduled tasks, webhooks and custom hooks.", 0, 82, 820, 30, 12.5F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, AppBack));

        var newScript = MakeButton("+ New Script", panel.Width - 160, 28, 158, 50, ShowNewScriptDialog, main: true);
        newScript.FillColor = Color.FromArgb(126, 58, 242);
        newScript.HoverColor = Color.FromArgb(147, 82, 255);
        newScript.BorderColor = Color.FromArgb(167, 139, 250);
        newScript.Radius = 10;
        newScript.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        panel.Controls.Add(newScript);

        var filterBar = NewPanel(10, 0, 128, 1213, 66);
        filterBar.FillColor = Color.FromArgb(8, 13, 27);
        filterBar.BorderColor = BorderSoft;
        panel.Controls.Add(filterBar);
        var selectedFilter = "All Scripts";
        Action<string>? applyScriptFilter = null;
        var filterControls = new Dictionary<string, (Control? Icon, Label Label, RoundedPanel Underline)>();
        filterControls["All Scripts"] = AddScriptFilterTab(filterBar, "All Scripts", string.Empty, 28, selected: true, () => applyScriptFilter?.Invoke("All Scripts"));
        filterControls["Scheduled"] = AddScriptFilterTab(filterBar, "Scheduled", "\uE916", 166, selected: false, () => applyScriptFilter?.Invoke("Scheduled"));
        filterControls["Webhooks"] = AddScriptFilterTab(filterBar, "Webhooks", "\uE71B", 328, selected: false, () => applyScriptFilter?.Invoke("Webhooks"));
        filterControls["Custom"] = AddScriptFilterTab(filterBar, "Custom", "</>", 496, selected: false, () => applyScriptFilter?.Invoke("Custom"));

        var scheduledCard = BuildScriptCard(
            "Scheduled Restart",
            "Batch - Every 6 hours",
            "Gracefully warns players, saves the world and restarts the server process on a fixed interval to clear memory.",
            "Active",
            ScriptIconKind.Clock,
            enabled: true,
            x: 18,
            y: 220,
            w: 574,
            h: 255);

        var webhookCard = BuildScriptCard(
            "Discord Status Webhook",
            "Lua - On player join/leave",
            "Posts live player count, server status and join notifications to a configured Discord channel.",
            "Active",
            ScriptIconKind.Bell,
            enabled: true,
            x: 626,
            y: 220,
            w: 574,
            h: 255);

        var backupCard = BuildScriptCard(
            "Nightly Backup",
            "PowerShell - Daily at 04:00",
            "Zips the save folder and config files to a timestamped archive, keeping the last 14 days of backups.",
            "Paused",
            ScriptIconKind.Disk,
            enabled: false,
            x: 18,
            y: 504,
            w: 574,
            h: 255);

        var customZone = BuildCustomScriptDropZone(626, 504, 574, 255);

        panel.Controls.Add(scheduledCard);
        panel.Controls.Add(webhookCard);
        panel.Controls.Add(backupCard);
        panel.Controls.Add(customZone);

        var notice = NewPanel(10, 0, 788, 1213, 72);
        notice.FillColor = Color.FromArgb(13, 14, 35);
        notice.BorderColor = Color.FromArgb(28, 35, 70);
        var noticeIcon = new ScriptIconPanel
        {
            Kind = ScriptIconKind.Bulb,
            IconColor = Color.FromArgb(255, 219, 76),
            BackColor = notice.FillColor
        };
        noticeIcon.SetBounds(28, 20, 28, 28);
        notice.Controls.Add(noticeIcon);
        notice.Controls.Add(MakeLabel("Scripts run inside a sandbox with access to RCON and the server API. Review community scripts before enabling them on a live server.", 70, 22, 1100, 28, 11F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, notice.FillColor));
        panel.Controls.Add(notice);

        void ApplyScriptFilter(string filter)
        {
            selectedFilter = filter;
            foreach (var (name, controls) in filterControls)
            {
                var active = name.Equals(filter, StringComparison.Ordinal);
                controls.Label.ForeColor = active ? TextMain : TextDim;
                controls.Underline.Visible = active;
                if (controls.Icon is ScriptIconPanel scriptIcon)
                {
                    scriptIcon.IconColor = active ? TextMain : TextDim;
                    scriptIcon.Invalidate();
                }
                else if (controls.Icon is Label iconLabel)
                {
                    iconLabel.ForeColor = active ? TextMain : TextDim;
                }
            }
            LayoutScriptsPage(panel, newScript, filterBar, scheduledCard, webhookCard, backupCard, customZone, notice, selectedFilter);
            Log("Scripts filter: " + filter + ".");
        }
        applyScriptFilter = ApplyScriptFilter;

        panel.Resize += (_, _) => LayoutScriptsPage(panel, newScript, filterBar, scheduledCard, webhookCard, backupCard, customZone, notice, selectedFilter);
        LayoutScriptsPage(panel, newScript, filterBar, scheduledCard, webhookCard, backupCard, customZone, notice, selectedFilter);

        return panel;
    }

    private static void LayoutScriptsPage(Control panel, Control newScript, Control filterBar, Control scheduledCard, Control webhookCard, Control backupCard, Control customZone, Control notice, string filter)
    {
        var pageWidth = Math.Max(1000, panel.ClientSize.Width);
        const int margin = 18;
        const int gap = 26;
        var cardWidth = Math.Max(460, (pageWidth - margin * 2 - gap) / 2);
        const int cardHeight = 255;
        var rightX = margin + cardWidth + gap;

        newScript.Left = pageWidth - 160;
        newScript.Top = 28;
        newScript.Width = 158;
        newScript.Height = 50;
        filterBar.Left = 0;
        filterBar.Top = 128;
        filterBar.Width = pageWidth;
        filterBar.Height = 66;

        scheduledCard.Visible = filter is "All Scripts" or "Scheduled";
        backupCard.Visible = filter is "All Scripts" or "Scheduled";
        webhookCard.Visible = filter is "All Scripts" or "Webhooks";
        customZone.Visible = filter is "All Scripts" or "Custom";

        scheduledCard.Left = margin;
        scheduledCard.Top = 220;
        scheduledCard.Width = cardWidth;
        scheduledCard.Height = cardHeight;
        webhookCard.Left = filter.Equals("Webhooks", StringComparison.Ordinal) ? margin : rightX;
        webhookCard.Top = 220;
        webhookCard.Width = cardWidth;
        webhookCard.Height = cardHeight;
        backupCard.Left = filter.Equals("Scheduled", StringComparison.Ordinal) ? rightX : margin;
        backupCard.Top = filter.Equals("Scheduled", StringComparison.Ordinal) ? 220 : 504;
        backupCard.Width = cardWidth;
        backupCard.Height = cardHeight;
        customZone.Left = filter.Equals("Custom", StringComparison.Ordinal) ? margin : rightX;
        customZone.Top = filter.Equals("Custom", StringComparison.Ordinal) ? 220 : 504;
        customZone.Width = cardWidth;
        customZone.Height = cardHeight;
        notice.Left = 0;
        notice.Top = filter.Equals("All Scripts", StringComparison.Ordinal) ? 788 : 504;
        notice.Width = pageWidth;
        notice.Height = 72;
    }

    private static (Control? Icon, Label Label, RoundedPanel Underline) AddScriptFilterTab(Control parent, string text, string icon, int x, bool selected, Action action)
    {
        var width = text switch
        {
            "All Scripts" => 112,
            "Scheduled" => 124,
            "Webhooks" => 128,
            _ => 104
        };
        var textLeft = string.IsNullOrEmpty(icon) ? x : x + 26;
        Control? iconControl = null;

        if (!string.IsNullOrEmpty(icon))
        {
            var tabBack = parent is RoundedPanel roundedParent ? roundedParent.FillColor : parent.BackColor;
            if (icon.Equals("</>", StringComparison.Ordinal))
            {
                var codeIcon = new ScriptIconPanel
                {
                    Kind = ScriptIconKind.Code,
                    IconColor = selected ? TextMain : TextDim,
                    BackColor = tabBack,
                    Cursor = Cursors.Hand
                };
                codeIcon.SetBounds(x, 17, 22, 24);
                codeIcon.Click += (_, _) => action();
                parent.Controls.Add(codeIcon);
                iconControl = codeIcon;
            }
            else
            {
                var iconLabel = MakeLabel(icon, x, 17, 22, 24, 11F, FontStyle.Regular, selected ? TextMain : TextDim, ContentAlignment.MiddleCenter, tabBack);
                iconLabel.Font = new Font("Segoe MDL2 Assets", 11F, FontStyle.Regular);
                iconLabel.Cursor = Cursors.Hand;
                iconLabel.Click += (_, _) => action();
                parent.Controls.Add(iconLabel);
                iconControl = iconLabel;
            }
        }

        var labelBack = parent is RoundedPanel labelParent ? labelParent.FillColor : parent.BackColor;
        var label = MakeLabel(text, textLeft, 16, width, 32, 10.5F, FontStyle.Bold, selected ? TextMain : TextDim, ContentAlignment.MiddleLeft, labelBack);
        label.Cursor = Cursors.Hand;
        label.Click += (_, _) => action();
        parent.Controls.Add(label);

        var underline = new RoundedPanel
        {
            Left = x,
            Top = parent.Height - 4,
            Width = width,
            Height = 4,
            Radius = 2,
            FillColor = Color.FromArgb(147, 82, 255),
            BorderColor = Color.FromArgb(147, 82, 255),
            BackColor = parent is RoundedPanel underlineParent ? underlineParent.FillColor : parent.BackColor,
            Visible = selected
        };
        underline.Cursor = Cursors.Hand;
        underline.Click += (_, _) => action();
        parent.Controls.Add(underline);
        return (iconControl, label, underline);
    }

    private RoundedPanel BuildScriptCard(string title, string subtitle, string description, string status, ScriptIconKind iconKind, bool enabled, int x, int y, int w, int h)
    {
        var card = NewPanel(12, x, y, w, h);
        card.FillColor = Color.FromArgb(11, 18, 34);
        card.BorderColor = Color.FromArgb(22, 31, 52);

        var iconBox = NewPanel(8, 28, 28, 64, 64);
        iconBox.FillColor = Color.FromArgb(16, 23, 43);
        iconBox.BorderColor = Color.FromArgb(31, 38, 70);
        var iconColor = iconKind == ScriptIconKind.Bell ? Color.FromArgb(255, 207, 64) : enabled ? TextMain : TextMuted;
        var iconPanel = new ScriptIconPanel
        {
            Kind = iconKind,
            IconColor = iconColor,
            BackColor = iconBox.FillColor
        };
        iconPanel.SetBounds(0, 0, 64, 64);
        iconBox.BackColor = card.FillColor;
        iconBox.Controls.Add(iconPanel);

        var titleLabel = MakeLabel(title, 112, 30, w - 220, 30, 13.5F, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, card.FillColor);
        var subtitleLabel = MakeLabel(subtitle, 112, 60, w - 220, 24, 10.5F, FontStyle.Regular, TextDim, ContentAlignment.MiddleLeft, card.FillColor);
        var toggle = new ToggleSwitch
        {
            BackColor = card.FillColor,
            Checked = enabled,
            OnColor = Color.FromArgb(8, 84, 54),
            OnColor2 = Color.FromArgb(18, 186, 104),
            OffColor = Color.FromArgb(38, 45, 73),
            OffColor2 = Color.FromArgb(72, 83, 122)
        };
        var descriptionLabel = MakeWrappedLabel(description, 28, 118, w - 56, 56, 11.5F, FontStyle.Regular, TextMuted, card.FillColor);
        var divider = Line(28, h - 62, w - 56);
        var statusDot = MakeLabel("\u25CF", 28, h - 43, 16, 24, 10F, FontStyle.Regular, enabled ? Green : TextDim, ContentAlignment.MiddleLeft, card.FillColor);
        var statusLabel = MakeLabel(status == "Active" ? "Active" : "Paused", 46, h - 42, 100, 24, 10F, FontStyle.Bold, enabled ? Green : TextDim, ContentAlignment.MiddleLeft, card.FillColor);
        var editLabel = MakeActionLabel("Edit", w - 148, h - 42, 44, 24, Color.FromArgb(167, 139, 250), () => ShowScriptEditorDialog(title, subtitle, description, enabled));
        var runLabel = MakeActionLabel("Run now", w - 96, h - 42, 80, 24, TextMuted, () => ShowScriptRunDialog(title, subtitle, description));

        void OpenEditor() => ShowScriptEditorDialog(title, subtitle, description, toggle.Checked);
        card.Cursor = Cursors.Hand;
        titleLabel.Cursor = Cursors.Hand;
        descriptionLabel.Cursor = Cursors.Hand;
        iconBox.Cursor = Cursors.Hand;
        iconPanel.Cursor = Cursors.Hand;
        card.Click += (_, _) => OpenEditor();
        titleLabel.Click += (_, _) => OpenEditor();
        subtitleLabel.Click += (_, _) => OpenEditor();
        descriptionLabel.Click += (_, _) => OpenEditor();
        iconBox.Click += (_, _) => OpenEditor();
        iconPanel.Click += (_, _) => OpenEditor();
        toggle.CheckedChanged += (_, _) => ShowScriptToggleDialog(title, toggle.Checked);

        card.Controls.Add(iconBox);
        card.Controls.Add(titleLabel);
        card.Controls.Add(subtitleLabel);
        card.Controls.Add(toggle);
        card.Controls.Add(descriptionLabel);
        card.Controls.Add(divider);
        card.Controls.Add(statusDot);
        card.Controls.Add(statusLabel);
        card.Controls.Add(editLabel);
        card.Controls.Add(runLabel);

        void LayoutCard()
        {
            var cardWidth = card.ClientSize.Width;
            var cardHeight = card.ClientSize.Height;
            iconBox.SetBounds(28, 28, 64, 64);
            iconPanel.SetBounds(0, 0, 64, 64);
            titleLabel.SetBounds(112, 30, Math.Max(0, cardWidth - 220), 30);
            subtitleLabel.SetBounds(112, 60, Math.Max(0, cardWidth - 220), 24);
            toggle.SetBounds(cardWidth - 78, 32, 58, 30);
            descriptionLabel.SetBounds(28, 118, Math.Max(0, cardWidth - 56), 56);
            divider.SetBounds(28, cardHeight - 62, Math.Max(0, cardWidth - 56), 1);
            statusDot.SetBounds(28, cardHeight - 43, 16, 24);
            statusLabel.SetBounds(46, cardHeight - 42, 100, 24);
            editLabel.SetBounds(cardWidth - 148, cardHeight - 42, 44, 24);
            runLabel.SetBounds(cardWidth - 96, cardHeight - 42, 80, 24);
        }

        card.Resize += (_, _) => LayoutCard();
        LayoutCard();
        return card;
    }

    private RoundedPanel BuildCustomScriptDropZone(int x, int y, int w, int h)
    {
        var zone = new DashedRoundedPanel
        {
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            Radius = 12,
            FillColor = AppBack,
            BorderColor = Color.FromArgb(91, 68, 175),
            BackColor = AppBack
        };
        var plus = NewPanel(8, 0, 52, 54, 54);
        plus.FillColor = Color.FromArgb(9, 14, 28);
        plus.BorderColor = Color.FromArgb(75, 55, 150);
        plus.BackColor = zone.FillColor;
        var plusLabel = MakeLabel("+", 0, 0, 54, 54, 21, FontStyle.Regular, Color.FromArgb(147, 82, 255), ContentAlignment.MiddleCenter, plus.FillColor);
        plus.Controls.Add(plusLabel);
        var titleLabel = MakeLabel("Add a custom script", 0, 128, w, 24, 11, FontStyle.Bold, TextMuted, ContentAlignment.MiddleCenter, zone.FillColor);
        var subtitleLabel = MakeLabel("Lua, Batch or PowerShell", 0, 156, w, 22, 9.5F, FontStyle.Regular, TextDim, ContentAlignment.MiddleCenter, zone.FillColor);
        zone.Controls.Add(plus);
        zone.Controls.Add(titleLabel);
        zone.Controls.Add(subtitleLabel);

        void OpenNewScript() => ShowNewScriptDialog();
        zone.Cursor = Cursors.Hand;
        plus.Cursor = Cursors.Hand;
        plusLabel.Cursor = Cursors.Hand;
        titleLabel.Cursor = Cursors.Hand;
        subtitleLabel.Cursor = Cursors.Hand;
        zone.Click += (_, _) => OpenNewScript();
        plus.Click += (_, _) => OpenNewScript();
        plusLabel.Click += (_, _) => OpenNewScript();
        titleLabel.Click += (_, _) => OpenNewScript();
        subtitleLabel.Click += (_, _) => OpenNewScript();

        void LayoutDropZone()
        {
            var zoneWidth = zone.ClientSize.Width;
            plus.SetBounds(Math.Max(0, (zoneWidth - 54) / 2), 52, 54, 54);
            titleLabel.SetBounds(0, 128, zoneWidth, 24);
            subtitleLabel.SetBounds(0, 156, zoneWidth, 22);
        }

        zone.Resize += (_, _) => LayoutDropZone();
        LayoutDropZone();
        return zone;
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
        var maxPlayers = AddCompactTextField(config, "Max players", 292, 162, 118, "16", numeric: true);
        var gamePort = AddCompactTextField(config, "Game port", 22, 214, 118, "7779", numeric: true);
        var queryPort = AddCompactTextField(config, "Query port", 160, 214, 118, "27015", numeric: true);
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
        var rconPort = NewNumberTextBox(250, 50, 80, 34);
        rconPort.Text = "27020";
        network.Controls.Add(rconPort);
        network.Controls.Add(MakeLabel("Password", 342, 26, 90, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var rconPassword = NewTextBox(342, 50, 90, 34, password: true);
        network.Controls.Add(rconPassword);
        var enableHttpApi = AddToggleRow(network, "Enable HTTP API", 22, 104);
        network.Controls.Add(MakeLabel("HTTP API port", 250, 80, 130, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var httpApiPort = NewNumberTextBox(250, 104, 120, 34);
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
        var port = AddTextField(connection, "SSH port", 226, 54, 84, "22", numeric: true);
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

    private static ThemedTextBox AddTextField(Control parent, string label, int x, int y, int width, string value, bool password = false, bool numeric = false)
    {
        parent.Controls.Add(MakeLabel(label, x, y - 24, width, 22, 10.5F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var box = numeric ? NewNumberTextBox(x, y, width, 34) : NewTextBox(x, y, width, 34, password);
        box.Text = value;
        parent.Controls.Add(box);
        return box;
    }

    private static ThemedTextBox AddCompactTextField(Control parent, string label, int x, int y, int width, string value, bool password = false, bool numeric = false)
    {
        parent.Controls.Add(MakeLabel(label, x, y - 22, width, 20, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var box = numeric ? NewNumberTextBox(x, y, width, 30) : NewTextBox(x, y, width, 30, password);
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
        var gameFolder = LuaModService.DetectGameFolder();
        if (!string.IsNullOrWhiteSpace(gameFolder))
        {
            _gameFolderBox.Text = gameFolder;
            var modFolder = LuaModService.DetectModFolder(gameFolder) ?? LuaModService.GetExpectedModFolder(gameFolder);
            _modFolderBox.Text = modFolder;

            TryInstallBundledMod(gameFolder, modFolder, log);

            if (log) Log("Detected VEIN path: " + gameFolder);
            if (log) Log("Detected mod path: " + modFolder);
            LoadModFromPath(loadExistingState: true);
        }
        else if (log)
        {
            Log("VEIN path was not auto-detected. Use Browse.");
        }

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

    private void BrowseModFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = "Select ItemAndContainerModifier folder" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _modFolderBox.Text = dlg.SelectedPath;
        LoadModFromPath(loadExistingState: true);
        Log("Selected mod folder: " + dlg.SelectedPath);
    }

    private void OpenModFolder()
    {
        var path = _modFolderBox.Text.Trim();
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            Log("Opened mod folder.");
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

        Process.Start(new ProcessStartInfo { FileName = scripts, UseShellExecute = true });
        Log("Opened config folder.");
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
        LoadModFromPath(loadExistingState: false);
        var modFolder = _modFolderBox.Text.Trim();
        if (!LuaModService.IsValidModFolder(modFolder))
        {
            LogError("Cannot save. Select the ItemAndContainerModifier folder first.");
            return;
        }

        try
        {
            var backup = LuaModService.CreateBackup(modFolder);
            Log("Backup created: " + backup);
            _lastConfigBackupAt = DateTime.Now;
            LuaModService.ApplyConfig(modFolder, _state);
            _lastConfigSaveAt = DateTime.Now;
            MarkUnsaved(false);
            Log("Config saved. Restart VEIN if the game is open.");
            LoadModFromPath(loadExistingState: true);
        }
        catch (Exception ex)
        {
            LogError("Save failed: " + ex.Message);
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
            var process = LuaModService.LaunchVein(_gameFolderBox.Text.Trim());
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
        AddCategoryNumber(values, "Weight");
        AddCategoryNumber(values, "MaxStack", allowNegative: false);
        AddCategoryBool(values, "bStackable");
        AddCategoryNumber(values, "MaxWeight");
        AddCategoryNumber(values, "ExtraWeightCapacity");
        AddCategoryNumber(values, "RunSpeedMultiplier");

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
            else if (ReadItemNumber("MaxWeight", "Max Weight", out var maxWeight))
            {
                GetContainerOverrides(item.Category)[item.ClassName] = maxWeight;
            }
        }
        else
        {
            var values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
            AddItemNumber(values, "Weight", "Weight");
            AddItemNumber(values, "MaxStack", "Max Stack", allowNegative: false);
            AddItemBool(values, "bStackable");
            AddItemNumber(values, "ExtraWeightCapacity", "Extra Weight Capacity");
            AddItemNumber(values, "RunSpeedMultiplier", "Run Speed Multiplier");

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

    private void RefreshCategoryEditor()
    {
        if (_categoryCombo == null || _categoryFields == null) return;
        var category = CurrentCategory();
        _categoryFields.Controls.Clear();
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
            _itemFields.Controls.Clear();
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

    private void UpdateStatuses()
    {
        var gameRunning = Process.GetProcessesByName("Vein-Win64-Test").Length > 0
            || Process.GetProcessesByName("Vein").Length > 0
            || Process.GetProcessesByName("Vein-Win64-Shipping").Length > 0;
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

    private void AddCategoryNumber(Dictionary<string, LuaValue> values, string key, bool allowNegative = true)
    {
        if (!_categoryInputs.TryGetValue(key, out var input) || input is not TextBox box) return;
        if (TryReadOptionalNumber(box.Text, key, out var value, allowNegative)) values[key] = value;
    }

    private void AddCategoryBool(Dictionary<string, LuaValue> values, string key)
    {
        if (!_categoryInputs.TryGetValue(key, out var input) || input is not ThemedComboBox combo) return;
        var value = ReadBoolCombo(combo);
        if (!value.IsNil) values[key] = value;
    }

    private void AddItemNumber(Dictionary<string, LuaValue> values, string key, string label, bool allowNegative = true)
    {
        if (ReadItemNumber(key, label, out var value, allowNegative)) values[key] = value;
    }

    private void AddItemBool(Dictionary<string, LuaValue> values, string key)
    {
        if (!_itemInputs.TryGetValue(key, out var field) || field.DefaultCheck.Checked || field.Input is not ThemedComboBox combo) return;
        var value = ReadBoolCombo(combo);
        if (!value.IsNil) values[key] = value;
    }

    private bool ReadItemNumber(string key, string label, out LuaValue value, bool allowNegative = true)
    {
        value = LuaValue.Nil;
        if (!_itemInputs.TryGetValue(key, out var field) || field.DefaultCheck.Checked || field.Input is not TextBox box) return false;
        return TryReadOptionalNumber(box.Text, label, out value, allowNegative);
    }

    private bool TryReadOptionalNumber(string raw, string label, out LuaValue value, bool allowNegative = true)
    {
        value = LuaValue.Nil;
        raw = raw.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("nil", StringComparison.OrdinalIgnoreCase)) return false;

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
        var line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " | " + msg + Environment.NewLine;
        _log.SelectionStart = _log.TextLength;
        _log.SelectionColor = color;
        _log.AppendText(line);
        _log.SelectionColor = _log.ForeColor;
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

    private void ShowNewScriptDialog()
    {
        using var popup = NewScriptsDialog("New Script", 560, 420, out var shell);
        shell.Controls.Add(MakeLabel("New Script", 28, 24, 260, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeLabel("Create a local draft in the selected mod Scripts folder.", 30, 62, 430, 24, 11F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeLabel("Name", 30, 104, 140, 22, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var nameBox = NewTextBox(30, 130, 470, 36);
        nameBox.Text = "custom-script";
        shell.Controls.Add(nameBox);
        shell.Controls.Add(MakeLabel("Type", 30, 178, 140, 22, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var typeCombo = NewCombo(30, 204, 220, new[] { "Lua", "Batch", "PowerShell" });
        shell.Controls.Add(typeCombo);
        shell.Controls.Add(MakeLabel("Trigger", 280, 178, 140, 22, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var triggerBox = NewTextBox(280, 204, 220, 36);
        triggerBox.Text = "Manual";
        shell.Controls.Add(triggerBox);
        shell.Controls.Add(MakeWrappedLabel("The draft is created as a safe starter file. You can edit it manually before running it on a live server.", 30, 262, 470, 54, 10.5F, FontStyle.Regular, TextDim, PanelBack));
        shell.Controls.Add(MakeButton("Create Draft", 280, 326, 138, 42, () =>
        {
            if (CreateScriptDraft(nameBox.Text, Convert.ToString(typeCombo.SelectedItem) ?? "Lua")) popup.Close();
        }, main: true));
        shell.Controls.Add(MakeButton("Open Folder", 430, 326, 110, 42, OpenConfigFolder));
        popup.ShowDialog(this);
    }

    private void ShowScriptEditorDialog(string title, string subtitle, string description, bool enabled)
    {
        using var popup = NewScriptsDialog(title, 600, 462, out var shell);
        shell.Controls.Add(MakeLabel(title, 28, 24, 360, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeLabel(subtitle, 30, 62, 480, 24, 11F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeWrappedLabel(description, 30, 102, 520, 56, 11F, FontStyle.Regular, TextMuted, PanelBack));
        shell.Controls.Add(MakeLabel("Script name", 30, 178, 160, 22, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var nameBox = NewTextBox(30, 204, 260, 36);
        nameBox.Text = title;
        shell.Controls.Add(nameBox);
        shell.Controls.Add(MakeLabel("Runtime", 320, 178, 160, 22, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        var typeCombo = NewCombo(320, 204, 220, new[] { "Lua", "Batch", "PowerShell" });
        var runtime = subtitle.Split('-', 2)[0].Trim();
        var runtimeIndex = Math.Max(0, typeCombo.Items.IndexOf(runtime));
        typeCombo.SelectedIndex = runtimeIndex;
        shell.Controls.Add(typeCombo);
        shell.Controls.Add(MakeLabel("Current state", 30, 260, 160, 22, 10F, FontStyle.Bold, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeLabel(enabled ? "Enabled" : "Paused", 30, 286, 160, 24, 11F, FontStyle.Bold, enabled ? Green : TextDim, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeWrappedLabel("Save Draft writes a local editable script file. Runtime execution still stays behind the Run Now confirmation window.", 30, 326, 520, 42, 10.5F, FontStyle.Regular, TextDim, PanelBack));
        shell.Controls.Add(MakeButton("Save Draft", 318, 384, 118, 42, () =>
        {
            if (CreateScriptDraft(nameBox.Text, Convert.ToString(typeCombo.SelectedItem) ?? runtime)) popup.Close();
        }, main: true));
        shell.Controls.Add(MakeButton("Run Now", 450, 384, 100, 42, () => ShowScriptRunDialog(title, subtitle, description)));
        popup.ShowDialog(this);
    }

    private void ShowScriptRunDialog(string title, string subtitle, string description)
    {
        using var popup = NewScriptsDialog("Run Script", 540, 340, out var shell);
        shell.Controls.Add(MakeLabel("Run " + title, 28, 24, 420, 34, 20, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeLabel(subtitle, 30, 62, 420, 24, 11F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeWrappedLabel(description, 30, 108, 460, 70, 11F, FontStyle.Regular, TextMuted, PanelBack));
        shell.Controls.Add(MakeWrappedLabel("Review scripts before running them on a live server. This action records the request and keeps execution explicit.", 30, 200, 460, 48, 10.5F, FontStyle.Regular, TextDim, PanelBack));
        shell.Controls.Add(MakeButton("Run Now", 294, 262, 104, 42, () =>
        {
            Log("Script run requested: " + title + ".");
            popup.Close();
        }, main: true));
        shell.Controls.Add(MakeButton("Edit", 412, 262, 82, 42, () => ShowScriptEditorDialog(title, subtitle, description, true)));
        popup.ShowDialog(this);
    }

    private void ShowScriptToggleDialog(string title, bool enabled)
    {
        using var popup = NewScriptsDialog("Script Status", 460, 260, out var shell);
        shell.Controls.Add(MakeLabel(title, 28, 24, 340, 34, 18, FontStyle.Bold, TextMain, ContentAlignment.MiddleLeft, PanelBack));
        shell.Controls.Add(MakeWrappedLabel(enabled ? "This automation is now marked active for the local manager view." : "This automation is now paused for the local manager view.", 30, 78, 380, 58, 11F, FontStyle.Regular, TextMuted, PanelBack));
        shell.Controls.Add(MakeButton("OK", 304, 160, 92, 42, () => popup.Close(), main: true));
        Log((enabled ? "Enabled script: " : "Paused script: ") + title + ".");
        popup.ShowDialog(this);
    }

    private Form NewScriptsDialog(string title, int width, int height, out RoundedPanel shell)
    {
        var popup = new Form
        {
            Text = title,
            ClientSize = new Size(width, height),
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
        shell = NewPanel(14, 18, 18, width - 36, height - 36);
        shell.BackColor = AppBack;
        popup.Controls.Add(shell);
        return popup;
    }

    private bool CreateScriptDraft(string name, string type)
    {
        var modFolder = _modFolderBox?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
        {
            MessageBox.Show(this, "Select a valid ItemAndContainerModifier folder before creating script drafts.", "Scripts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LogError("Select a valid mod folder before creating script drafts.");
            return false;
        }

        var scripts = Path.Combine(modFolder, "Scripts");

        Directory.CreateDirectory(scripts);
        var safeName = SafeScriptFileName(name);
        var extension = ScriptExtension(type);
        var path = UniqueScriptPath(scripts, safeName, extension);
        File.WriteAllText(path, ScriptDraftContent(type, safeName));
        Log("Created script draft: " + path);
        return true;
    }

    private static string SafeScriptFileName(string name)
    {
        var safe = Regex.Replace(name.Trim(), @"[^A-Za-z0-9._-]+", "-").Trim('-', '.', '_');
        return string.IsNullOrWhiteSpace(safe) ? "custom-script" : safe;
    }

    private static string ScriptExtension(string type) => type switch
    {
        "Batch" => ".bat",
        "PowerShell" => ".ps1",
        _ => ".lua"
    };

    private static string UniqueScriptPath(string folder, string name, string extension)
    {
        var path = Path.Combine(folder, name + extension);
        if (!File.Exists(path)) return path;
        var suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(folder, name + "-" + suffix + extension);
    }

    private static Label MakeActionLabel(string text, int x, int y, int w, int h, Color color, Action action)
    {
        var label = MakeLabel(text, x, y, w, h, 10F, FontStyle.Bold, color, ContentAlignment.MiddleLeft, Color.Transparent);
        label.Cursor = Cursors.Hand;
        label.Click += (_, _) => action();
        return label;
    }

    private static string ScriptDraftContent(string type, string name) => type switch
    {
        "Batch" => "@echo off\r\necho VEIN script draft: " + name + "\r\n",
        "PowerShell" => "Write-Host \"VEIN script draft: " + name + "\"\r\n",
        _ => "print(\"VEIN script draft: " + name + "\")\n"
    };

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
            Font = new Font("Segoe UI", 11F, FontStyle.Regular)
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
        box.Multiline = false;
        box.AutoSize = true;
        box.TextAlign = HorizontalAlignment.Center;
        box.Top = y + Math.Max(0, (h - box.Height) / 2);
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
        panel.FillColor = Color.FromArgb(10, 17, 31);
        panel.BorderColor = BorderSoft;
        const int textLeft = 22;
        const int valueLeft = 20;
        panel.Controls.Add(MakeLabel(title, textLeft, 12, w - 44, 20, 10.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, panel.FillColor));
        var valueLabel = MakeLabel(value, valueLeft, 30, w - 42, 30, 16, FontStyle.Regular, valueColor, ContentAlignment.MiddleLeft, panel.FillColor);
        panel.Controls.Add(valueLabel);
        panel.Controls.Add(MakeLabel(sub, textLeft, 60, w - 44, 18, 8.5F, FontStyle.Regular, TextMuted, ContentAlignment.MiddleLeft, panel.FillColor));
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
        var label = MakeLabel(text, 0, 0, w, h, 9, FontStyle.Bold, TextMain, ContentAlignment.MiddleCenter, Color.Transparent);
        panel.Controls.Add(label);
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
            Radius = 9,
            FillColor = main ? Purple : Color.FromArgb(29, 44, 69),
            HoverColor = main ? PurpleLight : Color.FromArgb(38, 57, 88),
            BorderColor = main ? PurpleLight : Color.FromArgb(49, 70, 105),
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
            Radius = 12,
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
            Left = 2,
            Top = y,
            Width = SidebarWidth - 4,
            Height = 60,
            Radius = 8,
            FillColor = InnerBack,
            HoverColor = Color.FromArgb(15, 25, 43),
            BorderColor = BorderSoft,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            IconText = SidebarIconFor(text),
            IconFont = text.Equals("Scripts", StringComparison.Ordinal) ? new Font("Segoe UI", 11F, FontStyle.Bold) : new Font("Segoe MDL2 Assets", 12F, FontStyle.Regular),
            ContentLeftPadding = 24,
            IconTextGap = 14,
            FlatStyle = FlatStyle.Flat
        };
    }

    private static string SidebarIconFor(string title) => title switch
    {
        "Dashboard" => "\uE80F",
        "Setup" => "\uE90F",
        "Server Manager" => "\uE968",
        "Mods" => "\uE713",
        "Scripts" => "</>",
        "Log" => "\uE8FD",
        _ => string.Empty
    };

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

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private static readonly Regex LowerToUpperOrDigitPattern = new(@"(?<=[a-z])(?=[A-Z0-9])", RegexOptions.Compiled);
    private static readonly Regex AcronymBoundaryPattern = new(@"(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);
    private static readonly Regex NumberBoundaryPattern = new(@"(?<=\D)(?=\d)", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

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
