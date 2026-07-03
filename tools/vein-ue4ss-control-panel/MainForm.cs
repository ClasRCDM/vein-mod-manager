using System.Runtime.InteropServices;

namespace Vein.Ue4ss.DumpLog;

internal sealed partial class MainForm : Form
{
    private enum Page
    {
        Console,
        LiveView,
        Watches,
        Dumpers,
        BpMods,
        LuaDebugger,
        Files,
        Settings
    }

    private enum SettingsSection
    {
        General,
        Credits
    }

    private const int WmNcHitTest = 0x0084;
    private const int WmNcLeftButtonDown = 0x00A1;
    private const int WmSetRedraw = 0x000B;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;
    private const int ResizeGrip = 8;
    private const int PageTitleY = 2;
    private const int PageSubtitleY = 38;
    private const int PageToolbarY = 72;
    private const int PageLogY = 112;

    private readonly Ue4ssService _service;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 10000 };
    private readonly List<string> _messages = [];
    private readonly List<string> _watchTerms = ["Error", "Warning", "[Lua]", "patched", "scan complete"];
    private readonly Dictionary<Page, NavButton> _sidebarButtons = [];
    private readonly Dictionary<Page, NavButton> _topTabButtons = [];
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 12000,
        InitialDelay = 350,
        ReshowDelay = 120,
        ShowAlways = true
    };

    private TitleBarButton _maximizeButton = null!;
    private Label _footerStatus = null!;
    private Label _statusLabel = null!;
    private Label _activityLabel = null!;
    private Panel _pageHost = null!;
    private Panel _topTabsHost = null!;
    private Panel _topTabsDivider = null!;
    private LogTextView? _logBox;
    private TextBox? _searchBox;
    private ThemeButton? _dumpModeButton;
    private ContextMenuStrip? _dumpMethodMenu;
    private FlowLayoutPanel? _modListPanel;
    private Page _activePage = Page.Dumpers;
    private bool _autoScroll = true;
    private bool _tooltipsEnabled = true;
    private bool _manualMaximized;
    private Rectangle _restoreBounds;
    private long? _consoleClearLogOffset;
    private string _lastRenderedText = string.Empty;
    private string _liveViewSearchText = string.Empty;
    private string _liveViewSearchResults = string.Empty;
    private string _dumpMode = "UE4SS Dump";
    private bool _cheatEngineLiveMemoryEnabled = true;
    private bool _serviceActionBusy;
    private SettingsSection _activeSettingsSection = SettingsSection.General;

    public MainForm(Ue4ssService service)
    {
        _service = service;
        InitializeComponent();
        BuildUi();
        WireEvents();
        AddMessage("VEIN UE4SS console loaded.");
        AddMessage($"UE4SS folder: {_service.Paths.Ue4ssDirectory}");
        AddMessage("Launch VEIN, wait for the menu, then use the Dump page.");
        RefreshView();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.None;
        Text = "Vein Dump Manager";
        ClientSize = new Size(1072, 682);
        MinimumSize = new Size(1072, 682);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.AppBack;
        ForeColor = Theme.TextMain;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FormBorderStyle = FormBorderStyle.None;
        Padding = new Padding(1);
        var appIcon = AppAssets.LoadIcon();
        if (appIcon is not null)
        {
            Icon = appIcon;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcHitTest && WindowState == FormWindowState.Normal && !_manualMaximized)
        {
            base.WndProc(ref m);
            if (m.Result.ToInt32() == HtClient)
            {
                var point = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                if (point.Y <= 36 && point.X >= ClientSize.Width - 150)
                {
                    m.Result = HtClient;
                    return;
                }

                var left = point.X <= ResizeGrip;
                var right = point.X >= ClientSize.Width - ResizeGrip;
                var top = point.Y <= ResizeGrip;
                var bottom = point.Y >= ClientSize.Height - ResizeGrip;

                if (left && top) m.Result = HtTopLeft;
                else if (right && top) m.Result = HtTopRight;
                else if (left && bottom) m.Result = HtBottomLeft;
                else if (right && bottom) m.Result = HtBottomRight;
                else if (left) m.Result = HtLeft;
                else if (right) m.Result = HtRight;
                else if (top) m.Result = HtTop;
                else if (bottom) m.Result = HtBottom;
            }
            return;
        }

        base.WndProc(ref m);
    }

    private void BuildUi()
    {
        Controls.Clear();
        _sidebarButtons.Clear();
        _topTabButtons.Clear();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Theme.AppBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(root);

        root.Controls.Add(BuildTitleBar(), 0, 0);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Theme.AppBack
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 204F));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(shell, 0, 1);

        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildMain(), 1, 0);
        ShowPage(Page.Dumpers);
    }

    private Control BuildTitleBar()
    {
        var titleBar = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.TitleBarBack, Margin = Padding.Empty };

        var icon = new TitleLogo { Location = new Point(12, 8), Size = new Size(18, 18) };
        titleBar.Controls.Add(icon);

        var title = new Label
        {
            Text = "Vein Dump Manager",
            ForeColor = Color.FromArgb(156, 181, 216),
            Font = new Font("Segoe UI", 9F),
            Location = new Point(42, 8),
            Size = new Size(240, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        titleBar.Controls.Add(title);

        var close = MakeTitleButton(TitleButtonKind.Close, (_, _) => Close());
        SetTooltip(close, "Close Vein Dump Manager.");
        titleBar.Controls.Add(close);

        _maximizeButton = MakeTitleButton(TitleButtonKind.Maximize, (_, _) => ToggleMaximize());
        SetTooltip(_maximizeButton, "Maximize or restore the window.");
        titleBar.Controls.Add(_maximizeButton);

        var minimize = MakeTitleButton(TitleButtonKind.Minimize, (_, _) => WindowState = FormWindowState.Minimized);
        SetTooltip(minimize, "Minimize Vein Dump Manager to the taskbar.");
        titleBar.Controls.Add(minimize);

        void LayoutTitleButtons()
        {
            close.Left = titleBar.Width - 47;
            _maximizeButton.Left = titleBar.Width - 94;
            minimize.Left = titleBar.Width - 141;
        }

        titleBar.Resize += (_, _) => LayoutTitleButtons();
        titleBar.MouseDown += TitleBarMouseDown;
        title.MouseDown += TitleBarMouseDown;
        icon.MouseDown += TitleBarMouseDown;
        titleBar.DoubleClick += (_, _) => ToggleMaximize();
        SizeChanged += (_, _) =>
        {
            UpdateMaximizeButton();
        };

        return titleBar;
    }

    private static TitleBarButton MakeTitleButton(TitleButtonKind kind, EventHandler onClick)
    {
        var button = new TitleBarButton
        {
            Kind = kind,
            BackColor = Theme.TitleBarBack,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        button.Click += onClick;
        return button;
    }

    private Control BuildSidebar()
    {
        var sidebar = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.SidebarBack };

        var shell = new RoundedPanel
        {
            FillColor = Color.FromArgb(8, 15, 27),
            BorderColor = Theme.Border,
            Radius = 16,
            Location = new Point(18, 18),
            Size = new Size(180, 608),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        sidebar.Controls.Add(shell);

        shell.Controls.Add(new LogoPanel { Location = new Point(24, 28), Size = new Size(126, 104) });
        shell.Controls.Add(new Panel { BackColor = Theme.BorderSoft, Location = new Point(20, 164), Size = new Size(136, 1) });
        shell.Controls.Add(MakeCommandNav("Launch VEIN", "\uE768", 176, () => HandleResult(_service.LaunchGame()), hoverColor: Color.FromArgb(35, 153, 83)));
        shell.Controls.Add(MakeSidebarNav(Page.Dumpers, "Dump", "\uE896", 215));

        var settingsButton = MakeSidebarNav(Page.Settings, "Settings", "\uE713", 254);
        settingsButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        shell.Controls.Add(settingsButton);

        var version = new RoundedPanel
        {
            FillColor = Color.FromArgb(7, 14, 25),
            BorderColor = Theme.BorderSoft,
            Radius = 10,
            Location = new Point(12, 540),
            Size = new Size(150, 62),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        shell.Controls.Add(version);

        version.Controls.Add(new Label
        {
            Text = "v1.0.0\r\nVein Dump Manager",
            ForeColor = Theme.TextMuted,
            Font = new Font("Segoe UI", 8.3F, FontStyle.Bold),
            BackColor = Color.Transparent,
            Location = new Point(14, 8),
            Size = new Size(132, 30)
        });

        _footerStatus = new Label
        {
            Text = "System Online",
            ForeColor = Theme.Green,
            Font = new Font("Segoe UI", 8.3F, FontStyle.Bold),
            BackColor = Color.Transparent,
            Location = new Point(14, 42),
            Size = new Size(120, 18)
        };
        version.Controls.Add(_footerStatus);

        void LayoutSidebarFooter()
        {
            var footerTop = Math.Max(534, shell.ClientSize.Height - 80);
            version.Location = new Point(12, footerTop);
            version.Size = new Size(Math.Max(120, shell.ClientSize.Width - 24), 62);
            settingsButton.Location = new Point(12, 254);
            settingsButton.Size = new Size(Math.Max(120, shell.ClientSize.Width - 24), 36);
        }

        void LayoutSidebarShell()
        {
            shell.Location = new Point(18, 18);
            shell.Size = new Size(Math.Max(170, sidebar.ClientSize.Width - 22), Math.Max(608, sidebar.ClientSize.Height - 36));
            LayoutSidebarFooter();
        }

        sidebar.Resize += (_, _) => LayoutSidebarShell();
        shell.Resize += (_, _) => LayoutSidebarFooter();
        LayoutSidebarShell();
        return sidebar;
    }

    private NavButton MakeSidebarNav(Page page, string text, string icon, int top)
    {
        var button = new NavButton
        {
            Text = text,
            IconText = icon,
            Location = new Point(12, top),
            Size = new Size(150, 36),
            Radius = 8,
            ForeColor = Theme.TextMain,
            FillColor = Theme.ButtonBack,
            HoverColor = Theme.ButtonHover,
            HoverBorderColor = Theme.Border,
            BorderColor = Theme.Border
        };
        button.Click += (_, _) => SafeUiAction(() => ShowPage(page));
        SetTooltip(button, GetNavTooltip(page));
        _sidebarButtons[page] = button;
        return button;
    }

    private NavButton MakeTopTab(Page page, string text, string icon, int left, int width)
    {
        var button = new NavButton
        {
            Text = text,
            IconText = icon,
            Location = new Point(left, 8),
            Size = new Size(width, 40),
            Radius = 0,
            ForeColor = Theme.TextMain,
            FillColor = Theme.PanelBack,
            HoverColor = Theme.ButtonHover,
            HoverBorderColor = Theme.Border,
            BorderColor = Theme.PanelBack
        };
        button.Click += (_, _) => SafeUiAction(() => ShowPage(page));
        SetTooltip(button, GetNavTooltip(page));
        _topTabButtons[page] = button;
        return button;
    }

    private NavButton MakeCommandNav(string text, string icon, int top, Action action, bool primary = false, Color? hoverColor = null)
    {
        var resolvedHoverColor = hoverColor ?? (primary ? Theme.RedHover : Theme.ButtonHover);
        var button = new NavButton
        {
            Text = text,
            IconText = icon,
            Location = new Point(12, top),
            Size = new Size(150, 36),
            Radius = 8,
            ForeColor = Theme.TextMain,
            FillColor = Theme.ButtonBack,
            HoverColor = resolvedHoverColor,
            HoverBorderColor = resolvedHoverColor,
            BorderColor = Theme.Border
        };
        button.Click += (_, _) => SafeUiAction(action);
        SetTooltip(button, "Launch VEIN through Steam. This button turns green when hovered.");
        return button;
    }

    private Control BuildMain()
    {
        var main = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.AppBack };

        var title = MakeLabel("Vein Dump Manager", 22, 38, 460, 42, 24F, FontStyle.Bold, Theme.TextMain);
        main.Controls.Add(title);

        var subtitle = MakeLabel("UE4SS dumps, live logs, mod reloads, and object lookup for VEIN.", 22, 84, 760, 24, 10.5F, FontStyle.Regular, Theme.TextMuted);
        main.Controls.Add(subtitle);

        _statusLabel = MakeLabel("Checking...", 24, 124, 820, 22, 9F, FontStyle.Regular, Theme.TextMuted);
        main.Controls.Add(_statusLabel);

        var card = new RoundedPanel
        {
            FillColor = Theme.PanelBack,
            BorderColor = Theme.Border,
            Radius = 12,
            Location = new Point(16, 156),
            Size = new Size(830, 410),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        main.Controls.Add(card);

        _topTabsHost = new BufferedPanel
        {
            Location = new Point(0, 0),
            Size = new Size(830, 52),
            BackColor = Theme.PanelBack,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        card.Controls.Add(_topTabsHost);
        _topTabsHost.Controls.Add(MakeTopTab(Page.LiveView, "Live View", "\uE890", 0, 150));
        _topTabsHost.Controls.Add(MakeTopTab(Page.Console, "Console", "\uE756", 152, 150));
        _topTabsHost.Controls.Add(MakeTopTab(Page.Watches, "Watches", "\uE81C", 304, 150));
        _topTabsHost.Controls.Add(MakeTopTab(Page.Files, "Files", "\uE8B7", 456, 150));
        _topTabsHost.Controls.Add(MakeTopTab(Page.LuaDebugger, "Lua Log", "\uE7BA", 608, 150));

        _topTabsDivider = new Panel
        {
            BackColor = Theme.BorderSoft,
            Location = new Point(0, 52),
            Size = new Size(830, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        card.Controls.Add(_topTabsDivider);

        _pageHost = new BufferedPanel
        {
            Location = new Point(16, 62),
            Size = new Size(806, 328),
            BackColor = Theme.PanelBack,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        card.Controls.Add(_pageHost);

        _activityLabel = MakeLabel("", 12, 606, 820, 22, 8.7F, FontStyle.Regular, Theme.TextMuted);
        _activityLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        main.Controls.Add(_activityLabel);

        void LayoutMain()
        {
            var width = main.ClientSize.Width;
            var height = main.ClientSize.Height;
            var headerTextWidth = Math.Max(420, width - 64);
            title.Width = headerTextWidth;
            subtitle.Width = headerTextWidth;
            _statusLabel.Width = headerTextWidth;
            card.Size = new Size(Math.Max(720, width - 48), Math.Max(350, height - 214));
            _topTabsHost.Size = new Size(card.Width - 2, 52);
            _topTabsDivider.Size = new Size(card.Width - 2, 1);
            LayoutSectionChrome();
            _activityLabel.Location = new Point(12, card.Bottom + 6);
            _activityLabel.Size = new Size(Math.Max(300, width - 40), 22);
        }

        main.Resize += (_, _) => LayoutMain();
        LayoutMain();

        return main;
    }

    private void ShowPage(Page page)
    {
        _activePage = page;
        _logBox = null;
        _searchBox = null;
        _dumpModeButton = null;
        _modListPanel = null;
        _lastRenderedText = string.Empty;
        SuspendLayout();
        _pageHost.SuspendLayout();
        SetRedraw(_pageHost, false);
        try
        {
            ClearPageHost();
            UpdateNavigation();
            LayoutSectionChrome();

            switch (page)
            {
                case Page.Console:
                    BuildConsolePage();
                    break;
                case Page.LiveView:
                    BuildLiveViewPage();
                    break;
                case Page.Watches:
                    BuildWatchesPage();
                    break;
                case Page.Dumpers:
                    BuildDumpersPage();
                    break;
                case Page.BpMods:
                    BuildBpModsPage();
                    break;
                case Page.LuaDebugger:
                    BuildLuaDebuggerPage();
                    break;
                case Page.Files:
                    BuildFilesPage();
                    break;
                case Page.Settings:
                    BuildSettingsPage();
                    break;
            }
        }
        finally
        {
            SetRedraw(_pageHost, true);
            _pageHost.ResumeLayout(true);
            ResumeLayout(true);
            _pageHost.Invalidate(true);
            _pageHost.Update();
        }

        RefreshView();
    }

    private void LayoutSectionChrome()
    {
        if (_pageHost?.Parent is not Control card)
        {
            return;
        }

        var settingsPage = _activePage == Page.Settings;
        _topTabsHost.Visible = !settingsPage;
        _topTabsDivider.Visible = !settingsPage;

        var top = settingsPage ? 20 : 62;
        var bottomPadding = settingsPage ? 24 : 10;
        _pageHost.Location = new Point(16, top);
        _pageHost.Size = new Size(
            Math.Max(300, card.Width - 32),
            Math.Max(240, card.Height - top - bottomPadding));
    }

    private void ClearPageHost()
    {
        var oldControls = _pageHost.Controls.Cast<Control>().ToArray();
        _pageHost.Controls.Clear();
        foreach (var control in oldControls)
        {
            control.Dispose();
        }
    }

    internal void SelectPageForTesting(string pageName)
    {
        if (Enum.TryParse<Page>(pageName, ignoreCase: true, out var page))
        {
            ShowPage(page);
        }
    }

    internal void SelectSettingsSectionForTesting(string sectionName)
    {
        if (Enum.TryParse<SettingsSection>(sectionName, ignoreCase: true, out var section))
        {
            _activeSettingsSection = section;
            ShowPage(Page.Settings);
        }
    }

    private void UpdateNavigation()
    {
        foreach (var (page, button) in _sidebarButtons)
        {
            var selected = page == _activePage;
            button.FillColor = Theme.ButtonBack;
            button.HoverColor = Theme.ButtonHover;
            button.HoverBorderColor = Theme.Border;
            button.BorderColor = selected ? Theme.BorderSoft : Theme.Border;
            button.Invalidate();
        }

        foreach (var (page, button) in _topTabButtons)
        {
            var selected = page == _activePage;
            button.FillColor = selected ? Theme.ButtonHover : Theme.PanelBack;
            button.HoverColor = Theme.ButtonHover;
            button.HoverBorderColor = Theme.Border;
            button.BorderColor = selected ? Theme.Border : Theme.PanelBack;
            button.Invalidate();
        }

    }

    private void BuildConsolePage()
    {
        _pageHost.Controls.Add(MakeLabel("Live Console Log", 2, PageTitleY, 320, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Live UE4SS output, search, copy, and dump status.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        _searchBox = new TextBox
        {
            Location = new Point(2, PageToolbarY),
            Size = new Size(240, 28),
            BackColor = Theme.LogBack,
            ForeColor = Theme.TextMain,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F)
        };
        _pageHost.Controls.Add(_searchBox);

        AddPageButton("Search Log", 254, PageToolbarY - 2, 104, SearchLog);
        AddPageButton("Open Log", 368, PageToolbarY - 2, 92, () => HandleResult(_service.OpenLogFile()));
        AddPageButton("Copy Log", 470, PageToolbarY - 2, 92, CopyLog);
        AddPageButton("Clear View", 572, PageToolbarY - 2, 104, ClearView);

        _logBox = MakeLogBox(2, PageLogY, _pageHost.Width - 4, Math.Max(180, _pageHost.Height - PageLogY));
    }

    private void BuildLiveViewPage()
    {
        _pageHost.Controls.Add(MakeLabel("Live View", 2, PageTitleY, 320, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Search the latest UE4SS object dump by object name, type, package, or address.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        var toolbarWidth = Math.Max(520, _pageHost.ClientSize.Width - 4);
        const int toolbarGap = 8;
        const int searchButtonWidth = 104;
        const int openButtonWidth = 92;
        const int copyButtonWidth = 84;
        const int exportButtonWidth = 98;
        const int liveMemoryButtonWidth = 98;
        var searchWidth = Math.Min(260, Math.Max(180, toolbarWidth - searchButtonWidth - openButtonWidth - copyButtonWidth - exportButtonWidth - liveMemoryButtonWidth - (toolbarGap * 5)));
        var searchButtonX = 2 + searchWidth + toolbarGap;
        var openButtonX = searchButtonX + searchButtonWidth + toolbarGap;
        var copyButtonX = openButtonX + openButtonWidth + toolbarGap;
        var exportButtonX = copyButtonX + copyButtonWidth + toolbarGap;
        var liveMemoryButtonX = exportButtonX + exportButtonWidth + toolbarGap;

        _searchBox = new TextBox
        {
            Location = new Point(2, PageToolbarY),
            Size = new Size(searchWidth, 28),
            BackColor = Theme.LogBack,
            ForeColor = Theme.TextMain,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F)
        };
        _pageHost.Controls.Add(_searchBox);
        AddPageButton("Search Objects", searchButtonX, PageToolbarY - 2, searchButtonWidth, SearchLiveView);
        AddPageButton("Open Dumps", openButtonX, PageToolbarY - 2, openButtonWidth, () => HandleResult(_service.OpenDumpsFolder()));
        AddPageButton("Copy View", copyButtonX, PageToolbarY - 2, copyButtonWidth, CopyLog);
        AddPageButton("Export Files", exportButtonX, PageToolbarY - 2, exportButtonWidth, () => RunServiceActionAsync(_service.ExportReadableObjectDump, "Refreshing readable object dump exports..."));
        AddPageButton(_cheatEngineLiveMemoryEnabled ? "Memory: On" : "Memory: Off", liveMemoryButtonX, PageToolbarY - 2, liveMemoryButtonWidth, ToggleCheatEngineLiveMemory);

        _logBox = MakeLogBox(2, PageLogY, _pageHost.Width - 4, Math.Max(180, _pageHost.Height - PageLogY));
        _searchBox.Text = _liveViewSearchText;
        _logBox.Text = BuildLiveViewIntro();
        _logBox.SelectionStart = 0;
        _logBox.ScrollToCaret();
    }

    private void BuildWatchesPage()
    {
        _pageHost.Controls.Add(MakeLabel("Watches", 2, PageTitleY, 320, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Track live UE4SS log terms and see the newest matching lines.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        _searchBox = new TextBox
        {
            Location = new Point(2, PageToolbarY),
            Size = new Size(260, 28),
            BackColor = Theme.LogBack,
            ForeColor = Theme.TextMain,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F)
        };
        _pageHost.Controls.Add(_searchBox);
        AddPageButton("Add Watch", 274, PageToolbarY - 2, 104, AddWatchTerm);
        AddPageButton("Reset Watches", 388, PageToolbarY - 2, 120, ResetWatchTerms);
        AddPageButton("Copy View", 518, PageToolbarY - 2, 96, CopyLog);

        _logBox = MakeLogBox(2, PageLogY, _pageHost.Width - 4, Math.Max(180, _pageHost.Height - PageLogY));
    }

    private void BuildDumpersPage()
    {
        _pageHost.Controls.Add(MakeLabel("Dump", 2, PageTitleY, 260, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Every button below sends the real UE4SS hotkey to the VEIN game window.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        var actions = new RoundedPanel
        {
            FillColor = Color.FromArgb(6, 12, 22),
            BorderColor = Theme.BorderSoft,
            Radius = 8,
            Location = new Point(2, PageToolbarY + 4),
            Size = new Size(500, 276),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        _pageHost.Controls.Add(actions);
        actions.Controls.Add(MakeLabel("Dump Actions", 22, 18, 180, 24, 12F, FontStyle.Bold, Theme.TextMain));
        actions.Controls.Add(MakeLabel("Wait until VEIN is at the menu before dumping.", 22, 44, 258, 20, 9F, FontStyle.Regular, Theme.TextMuted));

        actions.Controls.Add(MakeLabel("Method", 294, 18, 84, 20, 8.4F, FontStyle.Bold, Theme.TextMuted));
        _dumpModeButton = MakeActionButton(_dumpMode, 184, () => { });
        _dumpModeButton.Location = new Point(294, 42);
        _dumpModeButton.Size = new Size(184, 28);
        _dumpModeButton.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
        _dumpModeButton.TextAlign = ContentAlignment.MiddleLeft;
        _dumpModeButton.ShowChevron = true;
        _dumpModeButton.Click += (_, _) => ShowDumpMethodMenu(_dumpModeButton);
        SetTooltip(_dumpModeButton, "Choose UE4SS hotkey dumps or the throttled headless Cheat Engine Lua memory dump.");
        actions.Controls.Add(_dumpModeButton);

        const int actionWidth = 146;
        const int actionGap = 8;
        const int actionRight = 22;
        var actionX3 = actions.Width - actionRight - actionWidth;
        var actionX2 = actionX3 - actionGap - actionWidth;
        var actionX1 = actionX2 - actionGap - actionWidth;
        var actionY1 = 78;
        var actionY2 = 126;
        var actionY3 = 174;
        var actionY4 = 222;

        AddPanelButton(actions, "Dump All", actionX1, actionY1, actionWidth, RunSelectedDumpMode, primary: true);
        AddPanelButton(actions, "Open Dumps", actionX2, actionY1, actionWidth, () => HandleResult(_service.OpenDumpsFolder()));
        AddPanelButton(actions, "Export Files", actionX3, actionY1, actionWidth, () => RunServiceActionAsync(_service.ExportReadableObjectDump, "Refreshing readable object dump exports..."));
        AddPanelButton(actions, "Objects + Props", actionX1, actionY2, actionWidth, () => RunServiceActionAsync(_service.SendObjectDump, "Sending object/property dump hotkey..."));
        AddPanelButton(actions, "Dump USMAP", actionX2, actionY2, actionWidth, () => RunServiceActionAsync(_service.SendUsmapDump, "Sending USMAP dump hotkey..."));
        AddPanelButton(actions, "Generate SDK", actionX3, actionY2, actionWidth, () => RunServiceActionAsync(_service.SendSdkDump, "Sending SDK generation hotkey..."));
        AddPanelButton(actions, "Generate UHT", actionX1, actionY3, actionWidth, () => RunServiceActionAsync(_service.SendUhtDump, "Sending UHT header generation hotkey..."));
        AddPanelButton(actions, "Dump Actors", actionX2, actionY3, actionWidth, () => RunServiceActionAsync(_service.SendActorDump, "Sending actor dump hotkey..."));
        AddPanelButton(actions, "Static Meshes", actionX3, actionY3, actionWidth, () => RunServiceActionAsync(_service.SendStaticMeshDump, "Sending static mesh dump hotkey..."));
        AddPanelButton(actions, "Restart Mods", actionX1, actionY4, (actionWidth * 3) + (actionGap * 2), () => HandleResult(_service.ClickRestartAllMods()));

        var notes = new RoundedPanel
        {
            FillColor = Color.FromArgb(6, 12, 22),
            BorderColor = Theme.BorderSoft,
            Radius = 8,
            Location = new Point(522, PageToolbarY + 4),
            Size = new Size(Math.Max(240, _pageHost.Width - 520), 276),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _pageHost.Controls.Add(notes);
        notes.Controls.Add(MakeLabel("Output Targets", 22, 18, 180, 24, 12F, FontStyle.Bold, Theme.TextMain));
        AddOutputTargetRow(notes, "Dumps", _service.DumpsRoot, 48, Math.Max(190, notes.Width - 44));
        AddOutputTargetRow(notes, "Markdown", _service.MarkdownDirectory, 90, Math.Max(190, notes.Width - 44));
        AddOutputTargetRow(notes, "JSON", _service.JsonDirectory, 132, Math.Max(190, notes.Width - 44));
        AddOutputTargetRow(notes, "Plain text", _service.PlainTextDirectory, 174, Math.Max(190, notes.Width - 44));
        AddOutputTargetRow(notes, "Export zip", _service.LatestExportZipPath, 216, Math.Max(190, notes.Width - 44));
    }

    private void RunSelectedDumpMode()
    {
        if (_dumpMode.Equals("Cheat Engine Memory Dump", StringComparison.OrdinalIgnoreCase))
        {
            RunServiceActionAsync(_service.StartCheatEngineMemoryDump, "Starting throttled headless Cheat Engine Lua memory dump...");
            return;
        }

        RunServiceActionAsync(_service.SendAllDumps, "Sending every UE4SS dump hotkey...");
    }

    private void ShowDumpMethodMenu(Control anchor)
    {
        if (_dumpMethodMenu is null || _dumpMethodMenu.IsDisposed)
        {
            _dumpMethodMenu = new ContextMenuStrip
            {
                BackColor = Theme.LogBack,
                ForeColor = Theme.TextMain,
                ShowImageMargin = false,
                ShowCheckMargin = false,
                Font = new Font("Segoe UI", 8.6F, FontStyle.Bold)
            };
            _dumpMethodMenu.Renderer = new ThemeContextMenuRenderer();
            _dumpMethodMenu.Padding = new Padding(1);
        }
        else
        {
            _dumpMethodMenu.Close();
            _dumpMethodMenu.Items.Clear();
        }

        var menu = _dumpMethodMenu;
        menu.MinimumSize = new Size(anchor.Width, 0);

        foreach (var mode in new[] { "UE4SS Dump", "Cheat Engine Memory Dump" })
        {
            var item = new ToolStripMenuItem(mode)
            {
                AutoSize = false,
                Checked = _dumpMode.Equals(mode, StringComparison.OrdinalIgnoreCase),
                BackColor = Theme.LogBack,
                ForeColor = Theme.TextMain,
                Padding = new Padding(10, 0, 10, 0),
                Size = new Size(Math.Max(anchor.Width - 2, 180), 24)
            };
            item.Click += (_, _) =>
            {
                _dumpMode = mode;
                if (_dumpModeButton is not null)
                {
                    _dumpModeButton.Text = _dumpMode;
                    _dumpModeButton.Invalidate();
                }

                HandleResult(OperationResult.Ok($"Dump method set to {_dumpMode}."));
            };
            menu.Items.Add(item);
        }

        menu.Show(anchor, new Point(0, anchor.Height + 2));
    }

    private void BuildBpModsPage()
    {
        _pageHost.Controls.Add(MakeLabel("BP Mods", 2, PageTitleY, 320, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Read and toggle real UE4SS mods.txt entries. A backup is created before changes.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));
        AddPageButton("Refresh", 2, PageToolbarY - 2, 92, () => { BuildBpModsPageOnly(); HandleResult(OperationResult.Ok("Refreshed UE4SS mod list.")); }, primary: true);
        AddPageButton("Open Mods", 104, PageToolbarY - 2, 104, () => HandleResult(_service.OpenModsFolder()));
        AddPageButton("Restart All Mods", 218, PageToolbarY - 2, 136, () => HandleResult(_service.ClickRestartAllMods()));

        _modListPanel = new FlowLayoutPanel
        {
            Location = new Point(2, PageLogY),
            Size = new Size(_pageHost.Width - 4, Math.Max(180, _pageHost.Height - PageLogY)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.PanelBack,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 0, 18, 16)
        };
        _pageHost.Controls.Add(_modListPanel);
        BuildBpModsPageOnly();
    }

    private void BuildBpModsPageOnly()
    {
        if (_modListPanel is null)
        {
            return;
        }

        _modListPanel.Controls.Clear();
        foreach (var mod in _service.ReadMods())
        {
            var row = new RoundedPanel
            {
                FillColor = Color.FromArgb(6, 12, 22),
                BorderColor = mod.Enabled ? Color.FromArgb(35, 112, 69) : Theme.BorderSoft,
                Radius = 8,
                Width = Math.Max(540, _modListPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 28),
                Height = 58,
                Margin = new Padding(0, 0, 0, 8)
            };
            row.Controls.Add(MakeLabel(mod.Name, 16, 8, 300, 22, 10F, FontStyle.Bold, Theme.TextMain));
            row.Controls.Add(MakeLabel(mod.Enabled ? "Enabled" : "Disabled", 16, 30, 90, 18, 8.3F, FontStyle.Bold, mod.Enabled ? Theme.Green : Theme.TextMuted));
            row.Controls.Add(MakeLabel(mod.FolderFound ? "folder found" : "folder missing", 110, 30, 140, 18, 8.3F, FontStyle.Regular, mod.FolderFound ? Theme.TextMuted : Theme.Error));
            AddPanelButton(row, mod.Enabled ? "Disable" : "Enable", row.Width - 112, 12, 88, () =>
            {
                HandleResult(_service.SetModEnabled(mod.Name, !mod.Enabled));
                BuildBpModsPageOnly();
            }, primary: !mod.Enabled);
            _modListPanel.Controls.Add(row);
        }
    }

    private void BuildLuaDebuggerPage()
    {
        _pageHost.Controls.Add(MakeLabel("Lua Log", 2, PageTitleY, 320, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Inspect Lua and mod log lines inside Vein Dump Manager without opening the UE4SS popup.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        AddPageButton("Open Log", 2, PageToolbarY - 2, 104, () => HandleResult(_service.OpenLogFile()), primary: true);
        AddPageButton("Restart Mods", 116, PageToolbarY - 2, 128, () => HandleResult(_service.ClickRestartAllMods()));
        AddPageButton("Open Mods", 270, PageToolbarY - 2, 104, () => HandleResult(_service.OpenModsFolder()));
        AddPageButton("Copy Lua Log", 384, PageToolbarY - 2, 116, CopyLog);

        _logBox = MakeLogBox(2, PageLogY, _pageHost.Width - 4, Math.Max(180, _pageHost.Height - PageLogY));
    }

    private void BuildFilesPage()
    {
        _pageHost.Controls.Add(MakeLabel("Files", 2, PageTitleY, 260, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Open the exact folders, logs, and generated dump files this dumper watches.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        AddPageButton("Open Dumps", 2, PageToolbarY - 2, 120, () => HandleResult(_service.OpenDumpsFolder()), primary: true);
        AddPageButton("Open UE4SS Folder", 134, PageToolbarY - 2, 150, () => HandleResult(_service.OpenUe4ssFolder()));
        AddPageButton("Open Log", 296, PageToolbarY - 2, 100, () => HandleResult(_service.OpenLogFile()));

        var card = new RoundedPanel
        {
            FillColor = Color.FromArgb(6, 12, 22),
            BorderColor = Theme.BorderSoft,
            Radius = 8,
            Location = new Point(2, PageLogY),
            Size = new Size(_pageHost.Width, Math.Max(190, _pageHost.Height - PageLogY)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _pageHost.Controls.Add(card);

        card.Controls.Add(MakeLabel("Paths and Generated Files", 22, 18, Math.Max(300, card.Width - 44), 24, 12F, FontStyle.Bold, Theme.TextMain));
        var filesView = new LogTextView
        {
            Location = new Point(22, 50),
            Size = new Size(Math.Max(300, card.Width - 44), Math.Max(80, card.Height - 72)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 8F),
            BackColor = Theme.LogBack,
            ForeColor = Color.FromArgb(218, 234, 255),
            Text = BuildFilesPanelText()
        };
        card.Controls.Add(filesView);
    }

    private void BuildSettingsPage()
    {
        _pageHost.Controls.Add(MakeLabel("Settings", 2, PageTitleY, 260, 34, 18F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeLabel("Control helper behavior for the VEIN dumper UI.", 4, PageSubtitleY, 760, 22, 10F, FontStyle.Regular, Theme.TextMuted));

        _pageHost.Controls.Add(MakeSettingsTab("General", SettingsSection.General, 2, PageToolbarY, 132));
        _pageHost.Controls.Add(MakeSettingsTab("Credits", SettingsSection.Credits, 146, PageToolbarY, 132));

        if (_activeSettingsSection == SettingsSection.Credits)
        {
            BuildCreditsSettingsCard();
            return;
        }

        BuildGeneralSettingsCard();
    }

    private ThemeButton MakeSettingsTab(string text, SettingsSection section, int left, int top, int width)
    {
        var selected = _activeSettingsSection == section;
        var button = new ThemeButton
        {
            Text = text,
            Location = new Point(left, top),
            Size = new Size(width, 36),
            Radius = 8,
            FillColor = selected ? Theme.ButtonHover : Theme.ButtonBack,
            HoverColor = Theme.ButtonHover,
            BorderColor = selected ? Theme.Border : Theme.BorderSoft
        };
        button.Click += (_, _) =>
        {
            _activeSettingsSection = section;
            ShowPage(Page.Settings);
        };
        SetTooltip(button, section == SettingsSection.Credits
            ? "View project contributors and roles."
            : "Adjust helper behavior for the dumper UI.");
        return button;
    }

    private void BuildGeneralSettingsCard()
    {
        var contentTop = PageToolbarY + 62;
        var contentWidth = Math.Max(520, _pageHost.Width - 4);

        _pageHost.Controls.Add(MakeLabel("Guidance", 22, contentTop, 180, 24, 12F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeWrappedLabel("Tooltips explain what each dump button does and when it is safe to use it.", 22, contentTop + 30, Math.Max(260, contentWidth - 150), 42));

        var toggle = new ToggleSwitch
        {
            Location = new Point(Math.Max(300, contentWidth - 112), contentTop + 38),
            Checked = _tooltipsEnabled,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        toggle.CheckedChanged += (_, _) =>
        {
            _tooltipsEnabled = toggle.Checked;
            _toolTip.Active = _tooltipsEnabled;
            HandleResult(OperationResult.Ok(_tooltipsEnabled ? "Tooltips enabled." : "Tooltips disabled."));
            ShowPage(Page.Settings);
        };
        SetTooltip(toggle, "Turn helper tooltips on or off. On is green, off is red.");
        _pageHost.Controls.Add(toggle);

    }

    private void BuildCreditsSettingsCard()
    {
        var contentTop = PageToolbarY + 60;
        var contentWidth = Math.Max(520, _pageHost.Width - 4);

        _pageHost.Controls.Add(MakeLabel("Project Credits", 22, contentTop, 260, 28, 13F, FontStyle.Bold, Theme.TextMain));
        _pageHost.Controls.Add(MakeWrappedLabel("The people behind Vein Dump Manager and its VEIN modding workflow.", 22, contentTop + 30, Math.Max(300, contentWidth - 44), 36));

        AddCreditRow(_pageHost, 22, contentTop + 76, "Cyberfox1337x", "Full Stack Developer", "cyberfox1337x.png", Theme.Green);
        AddCreditRow(_pageHost, 22, contentTop + 134, "CrazyUncleSole", "UI Designer", "crazyunclesole.png", Theme.Green);
        AddCreditRow(_pageHost, 22, contentTop + 192, "Alustrial", "Full Stack Developer", "alustrial.png", Theme.Green);
    }

    private void AddCreditRow(Control parent, int left, int top, string name, string role, string avatarFileName, Color statusColor)
    {
        var row = new RoundedPanel
        {
            FillColor = Theme.ButtonBack,
            BorderColor = Theme.Border,
            Radius = 8,
            Location = new Point(left, top),
            Size = new Size(Math.Max(320, parent.Width - 44), 48),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        parent.Controls.Add(row);

        var avatar = new DiscordAvatar
        {
            Location = new Point(14, 6),
            Size = new Size(38, 38),
            Avatar = AppAssets.LoadAssetImage("Credits", avatarFileName),
            RingColor = Theme.Border,
            StatusColor = statusColor
        };
        row.Controls.Add(avatar);

        row.Controls.Add(MakeLabel(name, 66, 10, Math.Max(160, row.Width - 370), 28, 10F, FontStyle.Bold, Theme.TextMain));

        var roleLabel = MakeLabel(role, Math.Max(180, row.Width - 286), 10, 260, 28, 9.2F, FontStyle.Regular, Theme.TextMuted);
        roleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        roleLabel.TextAlign = ContentAlignment.MiddleRight;
        row.Controls.Add(roleLabel);

        SetTooltip(row, $"{name} - {role}");
        SetTooltip(avatar, $"{name} - {role}");
        SetTooltip(roleLabel, $"{name} - {role}");
    }

    private LogTextView MakeLogBox(int left, int top, int width, int height)
    {
        var shell = new RoundedPanel
        {
            FillColor = Theme.LogBack,
            BorderColor = Color.FromArgb(39, 68, 108),
            Radius = 6,
            Location = new Point(left, top),
            Size = new Size(width, height),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _pageHost.Controls.Add(shell);

        var logBox = new LogTextView
        {
            Location = new Point(10, 10),
            Size = new Size(Math.Max(80, width - 20), Math.Max(80, height - 20)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.LogBack,
            ForeColor = Color.FromArgb(224, 238, 255),
            Font = new Font("Consolas", 8F)
        };
        shell.Controls.Add(logBox);
        return logBox;
    }

    private string BuildLiveViewIntro()
    {
        if (IsCheatEngineLiveViewActive())
        {
            return _service.BuildLiveCheatEngineDumpView();
        }

        if (!_service.GetStatus().GameRunning)
        {
            _liveViewSearchResults = string.Empty;
            return _service.BuildLiveObjectDumpView(null);
        }

        return string.IsNullOrWhiteSpace(_liveViewSearchText)
            ? _service.BuildLiveObjectDumpView(null, 120)
            : _liveViewSearchResults;
    }

    private void SearchLiveView()
    {
        if (_logBox is null || _searchBox is null)
        {
            return;
        }

        if (IsCheatEngineLiveViewActive())
        {
            HandleResult(OperationResult.Fail("Turn Memory Live Off to search the UE4SS object dump. Memory Live is showing the Cheat Engine dump tail."));
            return;
        }

        var term = _searchBox.Text.Trim();
        if (!_service.GetStatus().GameRunning)
        {
            _liveViewSearchText = string.Empty;
            _liveViewSearchResults = string.Empty;
            HandleResult(OperationResult.Fail("Live View is waiting for VEIN. Launch the game before searching the live dump."));
            return;
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            _liveViewSearchText = string.Empty;
            _liveViewSearchResults = string.Empty;
            _lastRenderedText = string.Empty;
            _autoScroll = true;
            HandleResult(OperationResult.Ok("Live View returned to real-time object dump tail."));
            return;
        }

        try
        {
            _liveViewSearchText = term;
            _liveViewSearchResults = _service.BuildLiveObjectDumpView(term, 400);
            _logBox.Text = _liveViewSearchResults;
            _lastRenderedText = _logBox.Text;
            _autoScroll = false;
            _activityLabel.Text = $"{DateTime.Now:HH:mm:ss} | Live View search locked on: {term}";
            _activityLabel.ForeColor = Theme.TextMuted;
        }
        catch (Exception ex)
        {
            HandleResult(OperationResult.Fail($"Could not search object dump: {ex.Message}"));
        }
    }

    private bool IsCheatEngineLiveViewActive()
    {
        return _activePage == Page.LiveView &&
               _cheatEngineLiveMemoryEnabled &&
               _dumpMode.Equals("Cheat Engine Memory Dump", StringComparison.OrdinalIgnoreCase);
    }

    private void ToggleCheatEngineLiveMemory()
    {
        _cheatEngineLiveMemoryEnabled = !_cheatEngineLiveMemoryEnabled;
        _liveViewSearchResults = string.Empty;
        _lastRenderedText = string.Empty;
        _autoScroll = true;
        ShowPage(Page.LiveView);
        HandleResult(OperationResult.Ok(_cheatEngineLiveMemoryEnabled
            ? "Memory Live is on. Live View will tail the latest Cheat Engine memory dump."
            : "Memory Live is off. Live View will show the UE4SS object dump preview."));
    }

    private void AddWatchTerm()
    {
        var term = _searchBox?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            HandleResult(OperationResult.Fail("Type a watch term first."));
            return;
        }

        if (!_watchTerms.Any(existing => existing.Equals(term, StringComparison.OrdinalIgnoreCase)))
        {
            _watchTerms.Add(term);
        }

        _searchBox!.Clear();
        HandleResult(OperationResult.Ok($"Watching log term: {term}"));
    }

    private void ResetWatchTerms()
    {
        _watchTerms.Clear();
        _watchTerms.AddRange(["Error", "Warning", "[Lua]", "patched", "scan complete"]);
        HandleResult(OperationResult.Ok("Reset watch terms."));
    }

    private string BuildWatchText()
    {
        var status = _service.GetStatus();
        var logLines = _service.ReadLogTail(2000);
        var dumpFiles = _service.EnumerateDumpFiles();
        var output = new List<string>
        {
            "VEIN / UE4SS status:",
            $"  Game executable: {(status.GameExeFound ? "found" : "missing")} ({_service.Paths.Win64Directory})",
            $"  VEIN process: {(status.GameRunning ? $"running PID {status.GameProcessId}" : "not running")}",
            $"  UE4SS.dll: {(status.Ue4ssDllFound ? "found" : "missing")} ({_service.Paths.Ue4ssDirectory})",
            $"  Proxy DLL: {(status.ProxyDllFound ? "ready" : "missing")} ({Path.Combine(_service.Paths.Win64Directory, "dwmapi.dll")})",
            $"  Dump keybinds: {(status.KeybindsFound ? "ready" : "missing")} ({_service.Paths.KeybindsScript})",
            $"  UE4SS log: {BuildLogStatus(status)}",
            "",
            "Dump output status:",
            $"  Output root: {_service.DumpsRoot}",
            $"  Files generated: {dumpFiles.Count:N0}",
            $"  Latest export: {_service.LatestExportZipPath}",
            $"  Object dump source: {_service.ObjectDumpPath}",
            "",
            "Watch term counts from the current UE4SS.log tail:",
            ""
        };

        foreach (var term in _watchTerms)
        {
            var hits = logLines.Where(line => line.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();
            output.Add($"{term} = {hits.Length}");
            foreach (var hit in hits.TakeLast(5))
            {
                output.Add($"  {hit}");
            }
            output.Add("");
        }

        return string.Join(Environment.NewLine, output);
    }

    private string BuildGeneratedFilesText()
    {
        var files = _service.EnumerateDumpFiles();
        var latestMemoryDump = _service.GetLatestCheatEngineDumpDirectory();
        if (files.Count == 0)
        {
            var emptyLines = new List<string>
            {
                "No organized dump files have been generated yet.",
                "",
                "Output paths:"
            };
            emptyLines.AddRange(FormatPathLines("UE4SS object dump", _service.ObjectDumpPath));
            emptyLines.AddRange(FormatPathLines("Plain text", Path.Combine(_service.PlainTextDirectory, "objects.txt")));
            emptyLines.AddRange(FormatPathLines("Markdown", Path.Combine(_service.MarkdownDirectory, "objects.md")));
            emptyLines.AddRange(FormatPathLines("Mod suggestions", Path.Combine(_service.MarkdownDirectory, "mod_suggestions.md")));
            emptyLines.AddRange(FormatPathLines("JSON", Path.Combine(_service.JsonDirectory, "objects.json")));
            emptyLines.AddRange(FormatPathLines("Dump index", Path.Combine(_service.MarkdownDirectory, "dump_index.md")));
            emptyLines.AddRange(FormatPathLines("Full game guide", Path.Combine(_service.GameFilesDirectory, "full_game_dump.md")));

            if (latestMemoryDump is not null)
            {
                emptyLines.AddRange(FormatPathLines("Latest Cheat Engine memory dump", latestMemoryDump.FullName));
            }

            emptyLines.AddRange(new[]
            {
                "",
                $"Dumps root: {_service.DumpsRoot}",
                "Use Dump All or Export Files after UE4SS has created UE4SS_ObjectDump.txt."
            });

            return string.Join(Environment.NewLine, emptyLines);
        }

        var lines = new List<string>
        {
            "Output paths:"
        };
        lines.AddRange(FormatPathLines("UE4SS object dump", _service.ObjectDumpPath));
        lines.AddRange(FormatPathLines("Plain text", Path.Combine(_service.PlainTextDirectory, "objects.txt")));
        lines.AddRange(FormatPathLines("Markdown", Path.Combine(_service.MarkdownDirectory, "objects.md")));
        lines.AddRange(FormatPathLines("Mod suggestions", Path.Combine(_service.MarkdownDirectory, "mod_suggestions.md")));
        lines.AddRange(FormatPathLines("JSON", Path.Combine(_service.JsonDirectory, "objects.json")));
        lines.AddRange(FormatPathLines("Dump index", Path.Combine(_service.MarkdownDirectory, "dump_index.md")));
        lines.AddRange(FormatPathLines("Full game guide", Path.Combine(_service.GameFilesDirectory, "full_game_dump.md")));
        lines.Add("");
        lines.AddRange(FormatPathLines("Dumps root", _service.DumpsRoot));
        lines.AddRange(new[]
        {
            $"Generated files: {files.Count:N0}",
            ""
        });

        if (latestMemoryDump is not null)
        {
            lines.AddRange(FormatPathLines("Latest Cheat Engine memory dump", latestMemoryDump.FullName));
            lines.Add("");
        }

        foreach (var file in files)
        {
            lines.Add($"{file.RelativePath} | {FormatBytes(file.Length)} | {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            lines.AddRange(FormatPathLines("Path", file.FullPath, 84));
            lines.Add("");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildFilesPanelText()
    {
        var lines = new List<string>
        {
            "Game and UE4SS paths:"
        };
        lines.AddRange(FormatPathLines("Game root", _service.Paths.VeinRoot));
        lines.AddRange(FormatPathLines("Win64", _service.Paths.Win64Directory));
        lines.AddRange(FormatPathLines("UE4SS", _service.Paths.Ue4ssDirectory));
        lines.AddRange(FormatPathLines("UE4SS log", _service.Paths.Ue4ssLog));
        lines.AddRange(FormatPathLines("Keybinds", _service.Paths.KeybindsScript));
        lines.Add("");
        lines.Add("Organized dump output:");
        lines.AddRange(FormatPathLines("Dumps root", _service.DumpsRoot));
        lines.AddRange(FormatPathLines("Markdown", _service.MarkdownDirectory));
        lines.AddRange(FormatPathLines("JSON", _service.JsonDirectory));
        lines.AddRange(FormatPathLines("Plain text", _service.PlainTextDirectory));
        lines.AddRange(FormatPathLines("Logs", _service.LogsDirectory));
        lines.AddRange(FormatPathLines("Game files", _service.GameFilesDirectory));
        lines.AddRange(FormatPathLines("Export zip", _service.LatestExportZipPath));
        lines.AddRange(FormatPathLines("Open first", Path.Combine(_service.MarkdownDirectory, "dump_index.md")));
        lines.AddRange(FormatPathLines("Mod ideas", Path.Combine(_service.MarkdownDirectory, "mod_suggestions.md")));
        lines.AddRange(FormatPathLines("Full game guide", Path.Combine(_service.GameFilesDirectory, "full_game_dump.md")));
        lines.Add("");

        var latestMemoryDump = _service.GetLatestCheatEngineDumpDirectory();
        if (latestMemoryDump is not null)
        {
            lines.Add($"  Latest Cheat Engine memory dump: {latestMemoryDump.FullName}");
            lines.Add("");
        }

        lines.Add("Generated files:");
        lines.Add(BuildGeneratedFilesText());
        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> FormatPathLines(string label, string value, int lineLength = 84)
    {
        var wrapped = FormatPathForCard(value, lineLength)
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (wrapped.Length == 0)
        {
            yield return $"  {label}:";
            yield break;
        }

        yield return $"  {label}: {wrapped[0]}";
        foreach (var part in wrapped.Skip(1))
        {
            yield return $"    {part}";
        }
    }

    private string BuildLuaDebuggerText()
    {
        var luaLines = _service.ReadLogTail(2000)
            .Where(line =>
                line.Contains("[Lua]", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("RegisterHook", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Mod", StringComparison.OrdinalIgnoreCase))
            .TakeLast(600)
            .ToArray();

        if (luaLines.Length == 0)
        {
            return "No Lua/mod lines found in the current UE4SS.log tail.";
        }

        return string.Join(Environment.NewLine, luaLines);
    }

    private Label MakeWrappedLabel(string text, int left, int top, int width, int height)
    {
        return new Label
        {
            Text = text,
            ForeColor = Theme.TextMuted,
            Font = new Font("Segoe UI", 8.8F),
            Location = new Point(left, top),
            Size = new Size(width, height),
            BackColor = Color.Transparent
        };
    }

    private void AddPathValueRow(Control parent, string label, string value, int top, int width, int left = 22)
    {
        var labelWidth = Math.Min(110, Math.Max(82, width / 4));
        var labelControl = new Label
        {
            Text = label,
            ForeColor = Theme.TextMuted,
            Font = new Font("Segoe UI", 8.4F, FontStyle.Bold),
            Location = new Point(left, top),
            Size = new Size(labelWidth, 18),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };
        parent.Controls.Add(labelControl);

        var valueControl = new Label
        {
            Text = value,
            ForeColor = Color.FromArgb(218, 234, 255),
            Font = new Font("Consolas", 7.6F, FontStyle.Regular),
            Location = new Point(left + labelWidth, top),
            Size = new Size(Math.Max(120, width - labelWidth), 44),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = false
        };
        SetTooltip(valueControl, value);
        parent.Controls.Add(valueControl);
    }

    private void AddOutputTargetRow(Control parent, string label, string value, int top, int width, int left = 22)
    {
        var row = new RoundedPanel
        {
            FillColor = Theme.LogBack,
            BorderColor = Theme.BorderSoft,
            Radius = 6,
            Location = new Point(left, top),
            Size = new Size(width, 38),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        row.Click += (_, _) => OpenOutputTarget(label, value);
        SetTooltip(row, $"Open {label}: {value}");
        parent.Controls.Add(row);

        var labelControl = new Label
        {
            Text = label,
            ForeColor = Theme.TextMuted,
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            Location = new Point(10, 5),
            Size = new Size(row.ClientSize.Width - 20, 14),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        labelControl.Click += (_, _) => OpenOutputTarget(label, value);
        row.Controls.Add(labelControl);

        var valueControl = new Label
        {
            Text = BuildOutputTargetStatusText(value),
            ForeColor = Color.FromArgb(224, 238, 255),
            Font = new Font("Segoe UI", 8F, FontStyle.Regular),
            Location = new Point(10, 18),
            Size = new Size(Math.Max(84, width - 20), 16),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopLeft,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        valueControl.Click += (_, _) => OpenOutputTarget(label, value);
        SetTooltip(valueControl, $"Open {label}: {value}");
        row.Controls.Add(valueControl);

        row.Resize += (_, _) =>
        {
            labelControl.Size = new Size(Math.Max(84, row.ClientSize.Width - 20), 14);
            valueControl.Size = new Size(Math.Max(84, row.ClientSize.Width - 20), 16);
        };
    }

    private static string BuildOutputTargetStatusText(string value)
    {
        if (Directory.Exists(value))
        {
            return "Open folder";
        }

        if (File.Exists(value))
        {
            return "Open file";
        }

        var parent = Path.GetDirectoryName(value);
        return !string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent)
            ? "Pending file"
            : "Missing path";
    }

    private static string FormatPathForCard(string value, int targetLineLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split(new[] { '\\' }, StringSplitOptions.None);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var part in parts)
        {
            var segment = string.IsNullOrEmpty(current) ? part : "\\" + part;
            if (current.Length > 0 && current.Length + segment.Length > targetLineLength)
            {
                lines.Add(current);
                current = part;
                continue;
            }

            current += segment;
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void OpenOutputTarget(string label, string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                HandleResult(OperationResult.Ok($"Opened {label}: {path}"));
                return;
            }

            if (File.Exists(path))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
                HandleResult(OperationResult.Ok($"Selected {label}: {path}"));
                return;
            }

            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = parent,
                    UseShellExecute = true
                });
                HandleResult(OperationResult.Fail($"{label} has not been created yet. Opened containing folder: {parent}"));
                return;
            }

            HandleResult(OperationResult.Fail($"{label} path was not found: {path}"));
        }
        catch (Exception ex)
        {
            HandleResult(OperationResult.Fail($"Could not open {label}: {ex.Message}"));
        }
    }

    private void AddPathListBox(Control parent, int left, int top, int width, int height, params (string Label, string Value)[] rows)
    {
        var shell = new RoundedPanel
        {
            FillColor = Theme.LogBack,
            BorderColor = Theme.BorderSoft,
            Radius = 6,
            Location = new Point(left, top),
            Size = new Size(width, height),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        parent.Controls.Add(shell);

        var text = string.Join(Environment.NewLine, rows.Select(row => $"{row.Label}: {row.Value}"));

        var paths = new LogTextView
        {
            Text = text.TrimEnd(),
            Location = new Point(8, 8),
            Size = new Size(Math.Max(120, width - 16), Math.Max(80, height - 16)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 7.2F),
            BackColor = Theme.LogBack,
            ForeColor = Color.FromArgb(218, 234, 255)
        };
        foreach (var row in rows)
        {
            SetTooltip(paths, string.Join(Environment.NewLine, rows.Select(item => $"{item.Label}: {item.Value}")));
        }
        shell.Controls.Add(paths);
    }

    private static Label MakeLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color)
    {
        return new Label
        {
            Text = text,
            ForeColor = color,
            Font = new Font("Segoe UI", size, style),
            Location = new Point(left, top),
            Size = new Size(width, height),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private ThemeButton MakeActionButton(string text, int width, Action action, bool primary = false)
    {
        var button = new ThemeButton
        {
            Text = text,
            Size = new Size(width, 36),
            Margin = new Padding(0, 0, 8, 8),
            Radius = 8,
            FillColor = primary ? Theme.Red : Theme.ButtonBack,
            HoverColor = primary ? Theme.RedHover : Theme.ButtonHover,
            BorderColor = primary ? Theme.Red : Theme.Border
        };
        button.Click += (_, _) => SafeUiAction(action);
        SetTooltip(button, GetActionTooltip(text));
        return button;
    }

    private void AddPageButton(string text, int left, int top, int width, Action action, bool primary = false)
    {
        var button = MakeActionButton(text, width, action, primary);
        button.Location = new Point(left, top);
        _pageHost.Controls.Add(button);
    }

    private void AddPanelButton(Control parent, string text, int left, int top, int width, Action action, bool primary = false)
    {
        var button = MakeActionButton(text, width, action, primary);
        button.Location = new Point(left, top);
        parent.Controls.Add(button);
    }

    private void WireEvents()
    {
        _service.LogFileChanged += (_, _) => BeginInvokeIfReady(RefreshView);
        _service.StartWatching();
        _refreshTimer.Tick += (_, _) => RefreshView();
        _refreshTimer.Start();
        FormClosing += (_, _) =>
        {
            _refreshTimer.Stop();
            _service.StopWatching();
        };
    }

    private void RefreshView()
    {
        var status = _service.GetStatus();
        var targetInterval = status.GameRunning ? 2000 : 10000;
        if (_refreshTimer.Interval != targetInterval)
        {
            _refreshTimer.Interval = targetInterval;
        }

        var footerText = BuildFooterStatusText(status);
        var footerColor = status.GameExeFound && status.ProxyDllFound && status.Ue4ssDllFound
            ? (status.GameRunning ? Theme.Green : Theme.TextMuted)
            : Theme.Error;
        if (_footerStatus.Text != footerText)
        {
            _footerStatus.Text = footerText;
        }
        if (_footerStatus.ForeColor != footerColor)
        {
            _footerStatus.ForeColor = footerColor;
        }

        var statusText = BuildStatusText(status);
        if (_statusLabel.Text != statusText)
        {
            _statusLabel.Text = statusText;
        }

        if (_logBox is null || _logBox.IsDisposed)
        {
            return;
        }

        var text = BuildVisibleLogText();
        if (text == _lastRenderedText)
        {
            return;
        }

        var shouldScroll = _activePage != Page.LiveView && _autoScroll && IsNearBottom();
        _lastRenderedText = text;
        _logBox.Text = text;
        if (shouldScroll)
        {
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        }
    }

    private string BuildVisibleLogText()
    {
        if (_activePage == Page.Watches)
        {
            return BuildWatchText();
        }

        if (_activePage == Page.LuaDebugger)
        {
            return BuildLuaDebuggerText();
        }

        if (_activePage == Page.LiveView)
        {
            return BuildLiveViewIntro();
        }

        var lines = new List<string>(_messages)
        {
            "",
            "---- UE4SS.log live tail ----"
        };
        if (_consoleClearLogOffset.HasValue)
        {
            var newLines = _service.ReadLogTailFromOffset(_consoleClearLogOffset.Value, 520);
            lines.AddRange(newLines.Length == 0
                ? ["View cleared. Waiting for new UE4SS.log lines..."]
                : newLines);
            return string.Join(Environment.NewLine, lines);
        }

        lines.AddRange(_service.ReadLogTail(520));
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildStatusText(Ue4ssStatus status)
    {
        var game = status.GameRunning ? $"VEIN running PID {status.GameProcessId}" : "VEIN not running";
        var proxy = status.ProxyDllFound ? "proxy ready" : "proxy missing";
        var ue4ss = status.Ue4ssDllFound ? "UE4SS found" : "UE4SS missing";
        var log = BuildLogStatus(status);
        var keybinds = status.KeybindsFound ? "dump keybinds ready" : "dump keybinds missing";
        return $"{ue4ss}  |  {proxy}  |  {log}  |  {keybinds}  |  {game}";
    }

    private static string BuildFooterStatusText(Ue4ssStatus status)
    {
        if (!status.GameExeFound)
        {
            return "Game Missing";
        }

        if (!status.ProxyDllFound || !status.Ue4ssDllFound)
        {
            return "UE4SS Missing";
        }

        return status.GameRunning ? "VEIN Running" : "Ready";
    }

    private static string BuildLogStatus(Ue4ssStatus status)
    {
        if (!status.LogFound)
        {
            return "log waiting";
        }

        if (status.LogLive)
        {
            return "log live";
        }

        return status.LogLastWrite.HasValue
            ? $"log found {status.LogLastWrite.Value:HH:mm:ss}"
            : "log found";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private static string GetNavTooltip(Page page) => page switch
    {
        Page.Console => "Watch UE4SS.log in real time and search or copy the live console output.",
        Page.LiveView => "Read the latest object dump as names, paths, types, and addresses instead of raw hex only.",
        Page.Watches => "Track important log terms like errors, Lua messages, patches, and scan completion.",
        Page.Dumpers => "Send UE4SS dump hotkeys to VEIN. Use this after the game reaches the main menu.",
        Page.BpMods => "View UE4SS mods.txt entries and enable or disable mods safely with a backup.",
        Page.LuaDebugger => "Inspect Lua and mod log output inside Vein Dump Manager.",
        Page.Files => "Open the exact folders and logs used by this dumper.",
        Page.Settings => "Change Vein Dump Manager helper settings.",
        _ => string.Empty
    };

    private static string GetActionTooltip(string text) => text switch
    {
        "Search Log" => "Find the next matching line in the visible UE4SS log.",
        "Open Log" => "Open UE4SS.log in Notepad.",
        "Copy Log" => "Copy the visible log text to the clipboard.",
        "Copy View" => "Copy the current page output to the clipboard.",
        "Clear View" => "Clear only this app's visible console messages. It does not delete UE4SS.log.",
        "Search Objects" => "Search UE4SS_ObjectDump.txt for object names, types, packages, functions, or addresses.",
        "Open Dumps" => "Open the organized Dumps folder with Markdown, JSON, PlainText, Logs, and Exports.",
        "Export Files" => "Convert UE4SS_ObjectDump.txt into organized Markdown, JSON, PlainText, Logs, and latest_export.zip.",
        "Memory: On" => "Cheat Engine memory live view is enabled. Click to switch Live View back to the UE4SS object dump preview.",
        "Memory: Off" => "Cheat Engine memory live view is disabled. Click to tail the live Cheat Engine memory dump status.",
        "Dump All" => "Send all configured UE4SS dump hotkeys, refresh readable exports, and mirror/catalog the full VEIN game install under Dumps/GameFiles.",
        "Objects + Props" => "Dump UE4SS objects and properties, then refresh readable exports.",
        "Dump USMAP" => "Ask UE4SS to generate a USMAP dump.",
        "Generate SDK" => "Ask UE4SS to generate SDK/CXX headers.",
        "Generate UHT" => "Ask UE4SS to generate UHT-compatible headers.",
        "Dump Actors" => "Ask UE4SS to dump all actors.",
        "Static Meshes" => "Ask UE4SS to dump static mesh data.",
        "Restart Mods" => "Send UE4SS hot reload Ctrl+R to VEIN. The UE4SS popup is not required.",
        "Restart All Mods" => "Send UE4SS hot reload Ctrl+R to VEIN. The UE4SS popup is not required.",
        "Refresh" => "Reload the current information from disk.",
        "Open Mods" => "Open the UE4SS Mods folder.",
        "Open UE4SS Folder" => "Open the folder containing UE4SS.dll, logs, and dump files.",
        "Focus UE4SS" => "Open UE4SS.log. The old UE4SS popup is disabled in replacement mode.",
        "Copy Lua Log" => "Copy the visible Lua/mod log lines.",
        _ => $"Run {text}."
    };

    private void SetTooltip(Control control, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _toolTip.SetToolTip(control, text);
        _toolTip.Active = _tooltipsEnabled;
    }

    private void HandleResult(OperationResult result)
    {
        AddMessage(result.Message, result.Success);
        _activityLabel.Text = $"{DateTime.Now:HH:mm:ss} | {result.Message}";
        _activityLabel.ForeColor = result.Success ? Theme.TextMuted : Theme.Error;
        RefreshView();
    }

    private void RunServiceActionAsync(Func<OperationResult> action, string startedMessage)
    {
        if (_serviceActionBusy)
        {
            HandleResult(OperationResult.Fail("Another dumper action is still running. Wait for it to finish before starting another one."));
            return;
        }

        _serviceActionBusy = true;
        AddMessage(startedMessage);
        _activityLabel.Text = $"{DateTime.Now:HH:mm:ss} | {startedMessage}";
        _activityLabel.ForeColor = Theme.TextMuted;
        Task.Run(action).ContinueWith(task =>
        {
            BeginInvokeIfReady(() =>
            {
                try
                {
                    if (task.Exception is not null)
                    {
                        HandleResult(OperationResult.Fail(task.Exception.GetBaseException().Message));
                        return;
                    }

                    HandleResult(task.Result);
                }
                finally
                {
                    _serviceActionBusy = false;
                }
            });
        });
    }

    private void AddMessage(string message, bool success = true)
    {
        var prefix = success ? "" : "ERROR: ";
        _messages.Add($"{DateTime.Now:HH:mm:ss} | {prefix}{message}");
        _service.AppendAppLog(message, error: !success);
        while (_messages.Count > 120)
        {
            _messages.RemoveAt(0);
        }
    }

    private void SearchLog()
    {
        if (_logBox is null || _searchBox is null)
        {
            ShowPage(Page.Console);
            return;
        }

        var term = _searchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            HandleResult(OperationResult.Fail("Type text in the search box first."));
            return;
        }

        var logText = _logBox.Text;
        var start = Math.Min(_logBox.SelectionStart + Math.Max(1, _logBox.SelectionLength), _logBox.TextLength);
        var index = logText.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
        if (index < 0 && start > 0)
        {
            index = logText.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        }

        if (index < 0)
        {
            HandleResult(OperationResult.Fail($"No log match for: {term}"));
            return;
        }

        _logBox.Focus();
        _logBox.SelectionStart = index;
        _logBox.SelectionLength = term.Length;
        _logBox.ScrollToCaret();
        _autoScroll = false;
        _activityLabel.Text = $"{DateTime.Now:HH:mm:ss} | Found log match: {term}";
        _activityLabel.ForeColor = Theme.TextMuted;
    }

    private void CopyLog()
    {
        try
        {
            Clipboard.SetText(_logBox?.Text ?? BuildVisibleLogText());
            HandleResult(OperationResult.Ok("Copied visible log text to clipboard."));
        }
        catch (Exception ex)
        {
            HandleResult(OperationResult.Fail($"Could not copy log: {ex.Message}"));
        }
    }

    private void ClearView()
    {
        _messages.Clear();
        _consoleClearLogOffset = _activePage == Page.Console ? _service.GetLogFileLength() : _consoleClearLogOffset;
        if (_activePage == Page.LiveView)
        {
            _liveViewSearchText = string.Empty;
            _liveViewSearchResults = string.Empty;
        }

        _lastRenderedText = string.Empty;
        _autoScroll = true;
        _logBox?.Clear();
        AddMessage("View cleared. Source files were not deleted.");
        RefreshView();
    }

    private bool IsNearBottom()
    {
        if (_logBox is null)
        {
            return true;
        }

        return _logBox.TextLength == 0 || _logBox.SelectionStart >= Math.Max(0, _logBox.TextLength - 8);
    }

    private void SafeUiAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            HandleResult(OperationResult.Fail(ex.Message));
        }
    }

    private void BeginInvokeIfReady(Action action)
    {
        if (!IsDisposed && IsHandleCreated)
        {
            BeginInvoke(action);
        }
    }

    private static void SetRedraw(Control control, bool enabled)
    {
        if (control.IsHandleCreated)
        {
            SendMessage(control.Handle, WmSetRedraw, enabled ? 1 : 0, 0);
        }
    }

    private void TitleBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (_manualMaximized)
        {
            return;
        }

        ReleaseCapture();
        SendMessage(Handle, WmNcLeftButtonDown, HtCaption, 0);
    }

    private void ToggleMaximize()
    {
        if (_manualMaximized)
        {
            RestoreManualMaximize();
            return;
        }

        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        _restoreBounds = Bounds;
        var center = new Point(Left + Width / 2, Top + Height / 2);
        var screen = Screen.FromPoint(center);
        Bounds = screen.WorkingArea;
        _manualMaximized = true;
        UpdateMaximizeButton();
    }

    private void RestoreManualMaximize()
    {
        _manualMaximized = false;
        Bounds = _restoreBounds == Rectangle.Empty
            ? new Rectangle(Screen.FromPoint(Cursor.Position).WorkingArea.Location, ClientSize)
            : _restoreBounds;
        UpdateMaximizeButton();
    }

    private void UpdateMaximizeButton()
    {
        _maximizeButton.Kind = _manualMaximized || WindowState == FormWindowState.Maximized
            ? TitleButtonKind.Restore
            : TitleButtonKind.Maximize;
        _maximizeButton.Invalidate();
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
