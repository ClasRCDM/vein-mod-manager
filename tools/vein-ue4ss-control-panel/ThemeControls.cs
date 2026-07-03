using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Vein.Ue4ss.DumpLog;

internal static class Theme
{
    public static readonly Color TitleBarBack = Color.FromArgb(18, 21, 30);
    public static readonly Color AppBack = Color.FromArgb(5, 10, 18);
    public static readonly Color SidebarBack = Color.FromArgb(8, 15, 27);
    public static readonly Color PanelBack = Color.FromArgb(9, 17, 31);
    public static readonly Color LogBack = Color.FromArgb(3, 9, 18);
    public static readonly Color Border = Color.FromArgb(27, 52, 86);
    public static readonly Color BorderSoft = Color.FromArgb(20, 39, 67);
    public static readonly Color TextMain = Color.White;
    public static readonly Color TextMuted = Color.FromArgb(184, 202, 232);
    public static readonly Color Red = Color.FromArgb(174, 17, 39);
    public static readonly Color RedHover = Color.FromArgb(204, 31, 55);
    public static readonly Color ButtonBack = Color.FromArgb(11, 21, 37);
    public static readonly Color ButtonHover = Color.FromArgb(20, 38, 63);
    public static readonly Color Green = Color.FromArgb(38, 232, 116);
    public static readonly Color Amber = Color.FromArgb(255, 190, 86);
    public static readonly Color Error = Color.FromArgb(255, 92, 92);
}

internal static class PaintHelpers
{
    public static Color ResolveBackColor(Control? control, Color fallback)
    {
        while (control is not null)
        {
            if (!control.BackColor.IsEmpty && control.BackColor != Color.Transparent)
            {
                return control.BackColor;
            }

            control = control.Parent;
        }

        return fallback;
    }
}

internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }
}

internal static class AppAssets
{
    public static Image? LoadLogo()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.png"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "vein-logo.png")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "vein-logo.png"))
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            using var image = Image.FromFile(candidate);
            return new Bitmap(image);
        }

        return null;
    }

    public static Icon? LoadIcon()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "vein-logo.ico"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "vein-logo.ico")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "vein-logo.ico"))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return new Icon(candidate);
            }
        }

        return null;
    }

    public static Image? LoadAssetImage(params string[] pathParts)
    {
        var relative = Path.Combine(pathParts);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", relative),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", relative)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", relative))
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            using var image = Image.FromFile(candidate);
            return new Bitmap(image);
        }

        return null;
    }

    public static void DrawImageCover(Graphics graphics, Image image, Rectangle target)
    {
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var scale = Math.Max((float)target.Width / image.Width, (float)target.Height / image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;
        var x = target.Left + (target.Width - width) / 2F;
        var y = target.Top + (target.Height - height) / 2F;
        graphics.DrawImage(image, x, y, width, height);
    }
}

internal sealed class DiscordAvatar : Control
{
    public Image? Avatar { get; set; }
    public Color RingColor { get; set; } = Theme.Border;
    public Color StatusColor { get; set; } = Theme.Green;

    public DiscordAvatar()
    {
        Size = new Size(42, 42);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Avatar?.Dispose();
            Avatar = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.AppBack));

        var avatarSize = Math.Max(1, Math.Min(Width, Height) - 6);
        var avatarRect = new Rectangle(1, 1, avatarSize, avatarSize);
        using var avatarPath = new GraphicsPath();
        avatarPath.AddEllipse(avatarRect);

        using (var fill = new SolidBrush(Theme.PanelBack))
        {
            e.Graphics.FillEllipse(fill, avatarRect);
        }

        if (Avatar is not null)
        {
            var previousClip = e.Graphics.Clip;
            using var clipRegion = new Region(avatarPath);
            e.Graphics.SetClip(clipRegion, CombineMode.Replace);
            AppAssets.DrawImageCover(e.Graphics, Avatar, avatarRect);
            e.Graphics.Clip = previousClip;
        }

        using (var ring = new Pen(RingColor, 2F))
        {
            e.Graphics.DrawEllipse(ring, avatarRect);
        }

        var statusSize = 14;
        var statusRect = new Rectangle(Width - statusSize - 1, Height - statusSize - 1, statusSize, statusSize);
        using var statusBack = new SolidBrush(PaintHelpers.ResolveBackColor(Parent, Theme.AppBack));
        e.Graphics.FillEllipse(statusBack, statusRect);
        using var statusFill = new SolidBrush(StatusColor);
        e.Graphics.FillEllipse(statusFill, Rectangle.Inflate(statusRect, -3, -3));
    }
}

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 8;
    public Color FillColor { get; set; } = Theme.PanelBack;
    public Color BorderColor { get; set; } = Theme.Border;
    private int _regionWidth;
    private int _regionHeight;
    private int _regionRadius;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.AppBack));
        UpdateRoundedRegion();

        using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
        using var fill = new SolidBrush(FillColor);
        using var pen = new Pen(BorderColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UpdateRoundedRegion();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        UpdateRoundedRegion();
    }

    private void UpdateRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        if (_regionWidth == Width && _regionHeight == Height && _regionRadius == Radius)
        {
            return;
        }

        _regionWidth = Width;
        _regionHeight = Height;
        _regionRadius = Radius;
        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), Radius);
        Region?.Dispose();
        Region = new Region(path);
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal class ThemeButton : Button
{
    private bool _hover;

    public int Radius { get; set; } = 8;
    public Color FillColor { get; set; } = Theme.ButtonBack;
    public Color HoverColor { get; set; } = Theme.ButtonHover;
    public Color BorderColor { get; set; } = Theme.Border;
    public bool ShowChevron { get; set; }

    protected bool IsHovered => _hover;
    protected override bool ShowFocusCues => false;
    protected override bool ShowKeyboardCues => false;

    public ThemeButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        ForeColor = Theme.TextMain;
        Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.AppBack));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPanel.RoundedRect(rect, Radius);
        using var fill = new SolidBrush(_hover ? HoverColor : FillColor);
        using var pen = new Pen(BorderColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);

        var textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        var textRect = rect;
        if (ShowChevron)
        {
            textRect = new Rectangle(rect.Left + 10, rect.Top, Math.Max(0, rect.Width - 38), rect.Height);
            textFlags |= TextFormatFlags.Left;
        }
        else
        {
            textFlags |= TextFormatFlags.HorizontalCenter;
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textRect,
            ForeColor,
            textFlags);

        if (ShowChevron)
        {
            var centerX = rect.Right - 16;
            var centerY = rect.Top + (rect.Height / 2) + 1;
            using var chevronPen = new Pen(Color.FromArgb(190, 214, 246), 1.5F);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawLines(chevronPen, new Point[]
            {
                new Point(centerX - 5, centerY - 3),
                new Point(centerX, centerY + 2),
                new Point(centerX + 5, centerY - 3)
            });
        }
    }
}

internal sealed class ThemeContextMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var fill = new SolidBrush(Theme.LogBack);
        e.Graphics.FillRectangle(fill, e.AffectedBounds);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var fill = new SolidBrush(Theme.LogBack);
        e.Graphics.FillRectangle(fill, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        var isChecked = e.Item is ToolStripMenuItem { Checked: true };
        var fillColor = e.Item.Selected
            ? Color.FromArgb(24, 48, 80)
            : isChecked
                ? Color.FromArgb(13, 29, 50)
                : Theme.LogBack;

        using var fill = new SolidBrush(fillColor);
        e.Graphics.FillRectangle(fill, bounds);

        if (e.Item.Selected)
        {
            using var border = new Pen(Color.FromArgb(58, 104, 158));
            e.Graphics.DrawRectangle(border, 1, 1, bounds.Width - 3, bounds.Height - 3);
        }

        if (isChecked)
        {
            using var accent = new SolidBrush(Theme.Red);
            e.Graphics.FillRectangle(accent, 0, 2, 3, Math.Max(0, bounds.Height - 4));
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Color.White : Theme.TextMain;
        e.TextFont = e.Item.Font;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Theme.BorderSoft);
        e.Graphics.DrawLine(pen, 4, e.Item.Height / 2, e.Item.Width - 4, e.Item.Height / 2);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var border = new Pen(Theme.Border);
        e.Graphics.DrawRectangle(border, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }
}

internal sealed class ThemeComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private bool _hover;

    public Color FillColor { get; set; } = Theme.LogBack;
    public Color ArrowColor { get; set; } = Color.FromArgb(14, 24, 40);
    public Color BorderColor { get; set; } = Theme.Border;
    public Color FocusBorderColor { get; set; } = Color.FromArgb(56, 147, 255);
    public Color SelectedColor { get; set; } = Color.FromArgb(0, 122, 204);
    public Color HoverColor { get; set; } = Theme.ButtonHover;
    public Color TextColor { get; set; } = Theme.TextMain;
    public Color MutedColor { get; set; } = Theme.TextMuted;

    public ThemeComboBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        BackColor = FillColor;
        ForeColor = TextColor;
        ItemHeight = 20;
        IntegralHeight = false;
        MaxDropDownItems = 8;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyNativeDarkTheme();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0) return;

        var isEditArea = e.State.HasFlag(DrawItemState.ComboBoxEdit);
        var isSelected = e.State.HasFlag(DrawItemState.Selected) && !isEditArea;
        var fillColor = isSelected ? SelectedColor : FillColor;
        var text = e.Index >= 0 ? Convert.ToString(Items[e.Index]) ?? string.Empty : Text;

        using (var fill = new SolidBrush(fillColor))
        {
            e.Graphics.FillRectangle(fill, e.Bounds);
        }

        var textBounds = new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height);
        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            textBounds,
            TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnDropDown(EventArgs e)
    {
        base.OnDropDown(e);
        ApplyNativeDarkTheme();
        BeginInvoke(PaintChrome);
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);
        BeginInvoke(PaintChrome);
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is WmPaint or WmNcPaint)
        {
            PaintChrome();
        }
    }

    private void PaintChrome()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0) return;

        var hdc = GetWindowDC(Handle);
        if (hdc == IntPtr.Zero) return;

        try
        {
            using var graphics = Graphics.FromHdc(hdc);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            var arrowBounds = new Rectangle(Math.Max(0, Width - 28), 1, 27, Math.Max(0, Height - 2));
            var borderColor = Focused ? FocusBorderColor : (_hover ? Color.FromArgb(58, 92, 140) : BorderColor);

            using (var fill = new SolidBrush(FillColor))
            using (var arrowFill = new SolidBrush(ArrowColor))
            using (var border = new Pen(borderColor))
            using (var divider = new Pen(BorderColor))
            {
                graphics.FillRectangle(fill, bounds);
                graphics.FillRectangle(arrowFill, arrowBounds);
                graphics.DrawLine(divider, arrowBounds.Left, 3, arrowBounds.Left, Height - 4);
                graphics.DrawRectangle(border, bounds);
            }

            var textBounds = new Rectangle(7, 1, Math.Max(0, Width - 38), Math.Max(0, Height - 2));
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                textBounds,
                TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            DrawChevron(graphics, arrowBounds);
        }
        finally
        {
            _ = ReleaseDC(Handle, hdc);
        }
    }

    private void ApplyNativeDarkTheme()
    {
        if (!IsHandleCreated) return;

        _ = SetWindowTheme(Handle, "DarkMode_Explorer", null);
        var info = new ComboBoxInfo
        {
            cbSize = Marshal.SizeOf<ComboBoxInfo>()
        };

        if (!GetComboBoxInfo(Handle, ref info)) return;
        if (info.hwndCombo != IntPtr.Zero) _ = SetWindowTheme(info.hwndCombo, "DarkMode_Explorer", null);
        if (info.hwndEdit != IntPtr.Zero) _ = SetWindowTheme(info.hwndEdit, "DarkMode_Explorer", null);
        if (info.hwndList != IntPtr.Zero) _ = SetWindowTheme(info.hwndList, "DarkMode_Explorer", null);
    }

    private void DrawChevron(Graphics graphics, Rectangle bounds)
    {
        var centerX = bounds.Left + bounds.Width / 2;
        var centerY = bounds.Top + bounds.Height / 2 + 1;
        var points = new[]
        {
            new Point(centerX - 5, centerY - 2),
            new Point(centerX, centerY + 3),
            new Point(centerX + 5, centerY - 2)
        };

        using var pen = new Pen(MutedColor, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        graphics.DrawLines(pen, points);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr handle, IntPtr hdc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref ComboBoxInfo info);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

    [StructLayout(LayoutKind.Sequential)]
    private struct ComboBoxInfo
    {
        public int cbSize;
        public Rect rcItem;
        public Rect rcButton;
        public int stateButton;
        public IntPtr hwndCombo;
        public IntPtr hwndEdit;
        public IntPtr hwndList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}

internal sealed class LogTextView : Control
{
    private string[] _lines = [string.Empty];
    private int[] _lineStarts = [0];
    private int _firstLine;
    private int _selectionStart;
    private int _selectionLength;
    private bool _scrollbarDragging;
    private int _scrollbarDragOffset;

    public int TextLength => Text.Length;

    public int SelectionStart
    {
        get => _selectionStart;
        set
        {
            _selectionStart = Math.Clamp(value, 0, TextLength);
            Invalidate();
        }
    }

    public int SelectionLength
    {
        get => _selectionLength;
        set
        {
            _selectionLength = Math.Max(0, Math.Min(value, TextLength - SelectionStart));
            Invalidate();
        }
    }

#pragma warning disable CS8765
    public override string Text
    {
        get => base.Text ?? string.Empty;
        set
        {
            var normalized = value ?? string.Empty;
            if (base.Text == normalized)
            {
                return;
            }

            base.Text = normalized;
        }
    }
#pragma warning restore CS8765

    public LogTextView()
    {
        BackColor = Theme.LogBack;
        ForeColor = Color.FromArgb(224, 238, 255);
        Font = new Font("Consolas", 8F);
        TabStop = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
    }

    public void Clear()
    {
        Text = string.Empty;
        _selectionStart = 0;
        _selectionLength = 0;
    }

    public void ScrollToCaret()
    {
        var line = GetLineFromIndex(SelectionStart);
        var visible = Math.Max(1, VisibleLineCount);
        if (line < _firstLine)
        {
            _firstLine = line;
        }
        else if (line >= _firstLine + visible)
        {
            _firstLine = Math.Max(0, line - visible + 1);
        }

        ClampOffsets();
        Invalidate();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        RebuildLines();
        ClampOffsets();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RebuildLines();
        ClampOffsets();
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _firstLine -= Math.Sign(e.Delta) * 3;
        ClampOffsets();
        Invalidate();
        base.OnMouseWheel(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        if (VerticalScrollbarVisible && ScrollThumbRectangle.Contains(e.Location))
        {
            _scrollbarDragging = true;
            _scrollbarDragOffset = e.Y - ScrollThumbRectangle.Top;
            Capture = true;
            return;
        }

        if (VerticalScrollbarVisible && ScrollTrackRectangle.Contains(e.Location))
        {
            _firstLine += e.Y < ScrollThumbRectangle.Top ? -VisibleLineCount : VisibleLineCount;
            ClampOffsets();
            Invalidate();
            return;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_scrollbarDragging)
        {
            ScrollToThumbY(e.Y - _scrollbarDragOffset);
            return;
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_scrollbarDragging)
        {
            _scrollbarDragging = false;
            Capture = false;
            return;
        }

        base.OnMouseUp(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End or Keys.Left or Keys.Right || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                _firstLine--;
                break;
            case Keys.Down:
                _firstLine++;
                break;
            case Keys.PageUp:
                _firstLine -= VisibleLineCount;
                break;
            case Keys.PageDown:
                _firstLine += VisibleLineCount;
                break;
            case Keys.Home:
                if (e.Control)
                {
                    _firstLine = 0;
                }
                break;
            case Keys.End:
                if (e.Control)
                {
                    _firstLine = MaxFirstLine;
                }
                break;
            default:
                base.OnKeyDown(e);
                return;
        }

        ClampOffsets();
        Invalidate();
        e.Handled = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Theme.LogBack);
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var lineHeight = LineHeight;
        var textArea = TextArea;
        var visibleLines = VisibleLineCount;
        var x = textArea.Left + 2;
        var y = textArea.Top + 2;
        using var textBrush = new SolidBrush(ForeColor);
        using var selectionBrush = new SolidBrush(Color.FromArgb(42, 78, 123));

        e.Graphics.SetClip(textArea);
        var selectedLine = SelectionLength > 0 ? GetLineFromIndex(SelectionStart) : -1;
        for (var i = 0; i < visibleLines; i++)
        {
            var lineIndex = _firstLine + i;
            if (lineIndex >= _lines.Length)
            {
                break;
            }

            var lineY = y + (i * lineHeight);
            if (lineIndex == selectedLine)
            {
                e.Graphics.FillRectangle(selectionBrush, textArea.Left, lineY, textArea.Width, lineHeight);
            }

            e.Graphics.DrawString(_lines[lineIndex], Font, textBrush, x, lineY);
        }

        e.Graphics.ResetClip();
        DrawVerticalScrollbar(e.Graphics);
    }

    private Rectangle TextArea => new(8, 8, Math.Max(10, Width - 30), Math.Max(10, Height - 16));
    private int LineHeight => Math.Max(12, Font.Height);
    private int CharWidth
    {
        get
        {
            const string sample = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            return Math.Max(5, TextRenderer.MeasureText(sample, Font, Size.Empty, TextFormatFlags.NoPadding).Width / sample.Length);
        }
    }
    private int VisibleLineCount => Math.Max(1, TextArea.Height / LineHeight);
    private int MaxFirstLine => Math.Max(0, _lines.Length - VisibleLineCount);
    private bool VerticalScrollbarVisible => _lines.Length > VisibleLineCount;
    private Rectangle ScrollTrackRectangle => new(Math.Max(0, Width - 17), 8, 8, Math.Max(10, Height - 16));
    private Rectangle ScrollThumbRectangle
    {
        get
        {
            var track = ScrollTrackRectangle;
            if (!VerticalScrollbarVisible)
            {
                return Rectangle.Empty;
            }

            var visibleRatio = Math.Clamp((float)VisibleLineCount / Math.Max(1, _lines.Length), 0.08F, 1F);
            var thumbHeight = Math.Max(24, (int)(track.Height * visibleRatio));
            var scrollableTrack = Math.Max(1, track.Height - thumbHeight);
            var top = track.Top + (int)(scrollableTrack * ((float)_firstLine / Math.Max(1, MaxFirstLine)));
            return new Rectangle(track.Left, top, track.Width, thumbHeight);
        }
    }

    private void DrawVerticalScrollbar(Graphics graphics)
    {
        var track = ScrollTrackRectangle;
        using var trackBrush = new SolidBrush(Color.FromArgb(9, 19, 34));
        using var trackPen = new Pen(Color.FromArgb(20, 42, 72));
        graphics.FillRectangle(trackBrush, track);
        graphics.DrawRectangle(trackPen, track);

        if (!VerticalScrollbarVisible)
        {
            return;
        }

        var thumb = ScrollThumbRectangle;
        using var thumbBrush = new SolidBrush(Color.FromArgb(31, 70, 115));
        using var thumbHotBrush = new SolidBrush(Color.FromArgb(42, 102, 163));
        graphics.FillRectangle(_scrollbarDragging ? thumbHotBrush : thumbBrush, thumb);
    }

    private void ScrollToThumbY(int thumbY)
    {
        if (!VerticalScrollbarVisible)
        {
            return;
        }

        var track = ScrollTrackRectangle;
        var thumb = ScrollThumbRectangle;
        var scrollableTrack = Math.Max(1, track.Height - thumb.Height);
        var relative = Math.Clamp(thumbY - track.Top, 0, scrollableTrack);
        _firstLine = (int)Math.Round(MaxFirstLine * (relative / (double)scrollableTrack));
        ClampOffsets();
        Invalidate();
    }

    private void RebuildLines()
    {
        var wrappedLines = new List<string>();
        var starts = new List<int>();
        var maxChars = Math.Max(18, (TextArea.Width - 28) / CharWidth);
        var normalized = Text.Replace("\r\n", "\n").Replace('\r', '\n');
        var rawLines = normalized.Split('\n');
        var sourceIndex = 0;

        foreach (var rawLine in rawLines)
        {
            if (rawLine.Length == 0)
            {
                wrappedLines.Add(string.Empty);
                starts.Add(sourceIndex);
                sourceIndex++;
                continue;
            }

            var offset = 0;
            while (offset < rawLine.Length)
            {
                var take = Math.Min(maxChars, rawLine.Length - offset);
                if (take == maxChars)
                {
                    var space = rawLine.LastIndexOf(' ', offset + take - 1, take);
                    if (space > offset + Math.Max(8, maxChars / 2))
                    {
                        take = space - offset;
                    }
                }

                wrappedLines.Add(rawLine.Substring(offset, take));
                starts.Add(sourceIndex + offset);
                offset += take;
                while (offset < rawLine.Length && rawLine[offset] == ' ')
                {
                    offset++;
                }
            }

            sourceIndex += rawLine.Length + 1;
        }

        if (wrappedLines.Count == 0)
        {
            wrappedLines.Add(string.Empty);
            starts.Add(0);
        }

        _lines = [.. wrappedLines];
        _lineStarts = [.. starts];
    }

    private int GetLineFromIndex(int index)
    {
        var line = Array.BinarySearch(_lineStarts, index);
        if (line >= 0)
        {
            return Math.Min(line, _lines.Length - 1);
        }

        return Math.Clamp(~line - 1, 0, _lines.Length - 1);
    }

    private void ClampOffsets()
    {
        _firstLine = Math.Clamp(_firstLine, 0, MaxFirstLine);
        _selectionStart = Math.Clamp(_selectionStart, 0, TextLength);
        _selectionLength = Math.Max(0, Math.Min(_selectionLength, TextLength - _selectionStart));
    }
}

internal sealed class GradientPanel : Panel
{
    public Color LeftColor { get; set; } = Theme.AppBack;
    public Color RightColor { get; set; } = Color.FromArgb(61, 10, 21);

    public GradientPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.AppBack));

        if (Width <= 16 || Height <= 16)
        {
            return;
        }

        var left = Color.FromArgb(0, LeftColor);
        var rightStrong = Color.FromArgb(150, RightColor);
        var rightSoft = Color.FromArgb(58, RightColor);
        var lineWidth = Math.Max(180, Width - 34);
        var leftEdge = Width - lineWidth - 14;

        using var topBrush = new LinearGradientBrush(new Rectangle(leftEdge, 16, lineWidth, 2), left, rightStrong, 0F);
        using var middleBrush = new LinearGradientBrush(new Rectangle(leftEdge, 44, lineWidth, 18), left, rightSoft, 0F);
        using var bottomBrush = new LinearGradientBrush(new Rectangle(leftEdge, 82, lineWidth, 2), left, rightStrong, 0F);
        e.Graphics.FillRectangle(topBrush, leftEdge, 16, lineWidth, 2);
        e.Graphics.FillRectangle(middleBrush, leftEdge, 44, lineWidth, 18);
        e.Graphics.FillRectangle(bottomBrush, leftEdge, 82, lineWidth, 2);
    }
}

internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private bool _hover;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ToggleSwitch()
    {
        Cursor = Cursors.Hand;
        Size = new Size(72, 34);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.StandardClick, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.PanelBack));

        var fillColor = Checked ? Theme.Green : Theme.Red;
        if (_hover)
        {
            fillColor = Checked ? Color.FromArgb(71, 196, 123) : Color.FromArgb(214, 50, 70);
        }

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPanel.RoundedRect(rect, Height / 2);
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(Color.FromArgb(72, 99, 137));
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        var knobSize = Height - 8;
        var knobX = Checked ? Width - knobSize - 5 : 5;
        var knobRect = new Rectangle(knobX, 4, knobSize, knobSize);
        using var knobBrush = new SolidBrush(Color.FromArgb(238, 246, 255));
        e.Graphics.FillEllipse(knobBrush, knobRect);

        using var textFont = new Font("Segoe UI", 7F, FontStyle.Bold);
        var text = Checked ? "ON" : "OFF";
        var textRect = Checked
            ? new Rectangle(8, 0, Width - knobSize - 12, Height)
            : new Rectangle(knobSize + 8, 0, Width - knobSize - 12, Height);
        TextRenderer.DrawText(e.Graphics, text, textFont, textRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

internal sealed class NavButton : ThemeButton
{
    public string IconText { get; set; } = "";
    public Color HoverBorderColor { get; set; } = Color.Empty;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.SidebarBack));
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPanel.RoundedRect(rect, Radius);
        using var fill = new SolidBrush(IsHovered ? HoverColor : FillColor);
        var border = IsHovered && !HoverBorderColor.IsEmpty ? HoverBorderColor : BorderColor;
        using var pen = new Pen(border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);

        using var iconFont = new Font("Segoe MDL2 Assets", 11.5F, FontStyle.Regular);
        TextRenderer.DrawText(e.Graphics, IconText, iconFont, new Rectangle(14, 0, 28, Height), ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(48, 0, Width - 54, Height), ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}

internal enum TitleButtonKind
{
    Minimize,
    Maximize,
    Restore,
    Close
}

internal sealed class TitleBarButton : Control
{
    private bool _hover;
    private bool _pressed;

    public TitleButtonKind Kind { get; set; }

    public TitleBarButton()
    {
        Size = new Size(47, 36);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.StandardClick, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        var back = Theme.TitleBarBack;
        if (_hover)
        {
            back = Kind == TitleButtonKind.Close
                ? Color.FromArgb(196, 43, 58)
                : Color.FromArgb(34, 44, 63);
        }
        if (_pressed)
        {
            back = Kind == TitleButtonKind.Close
                ? Color.FromArgb(148, 28, 40)
                : Color.FromArgb(43, 56, 79);
        }

        using var background = new SolidBrush(back);
        e.Graphics.FillRectangle(background, ClientRectangle);

        var glyph = Kind switch
        {
            TitleButtonKind.Minimize => "\uE921",
            TitleButtonKind.Maximize => "\uE922",
            TitleButtonKind.Restore => "\uE923",
            TitleButtonKind.Close => "\uE8BB",
            _ => ""
        };
        using var iconFont = new Font("Segoe MDL2 Assets", 10.5F, FontStyle.Regular);
        TextRenderer.DrawText(
            e.Graphics,
            glyph,
            iconFont,
            ClientRectangle,
            Color.FromArgb(154, 185, 228),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

internal sealed class LogoPanel : Control
{
    private readonly Image? _logo = AppAssets.LoadLogo();

    public LogoPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.SidebarBack));

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPanel.RoundedRect(bounds, 2);
        using var fill = new SolidBrush(Color.Black);
        using var border = new Pen(Color.FromArgb(70, 22, 28));
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        e.Graphics.SetClip(path);
        if (_logo is not null)
        {
            AppAssets.DrawImageCover(e.Graphics, _logo, bounds);
        }
        else
        {
            using var titleFont = new Font("Segoe UI Black", 22F, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, "VEIN", titleFont, bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        e.Graphics.ResetClip();

        var strip = new Rectangle(8, Height - 29, Width - 16, 24);
        using var stripBrush = new SolidBrush(Color.FromArgb(230, 0, 0, 0));
        e.Graphics.FillRectangle(stripBrush, strip);
        using var subFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, "VEIN DUMPER", subFont, strip, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logo?.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal sealed class TitleLogo : Control
{
    private readonly Image? _logo = AppAssets.LoadLogo();

    public TitleLogo()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintHelpers.ResolveBackColor(Parent, Theme.TitleBarBack));

        if (_logo is not null)
        {
            AppAssets.DrawImageCover(e.Graphics, _logo, ClientRectangle);
            return;
        }

        using var fill = new SolidBrush(Theme.Red);
        e.Graphics.FillRectangle(fill, 2, 2, Width - 4, Height - 4);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logo?.Dispose();
        }

        base.Dispose(disposing);
    }
}
