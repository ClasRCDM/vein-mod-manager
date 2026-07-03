using System.Drawing.Drawing2D;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VEIN_Item_And_Container_Modifier;

public sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 14;
    public Color FillColor { get; set; } = Color.FromArgb(10, 18, 30);
    public Color BorderColor { get; set; } = Color.FromArgb(32, 53, 82);

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(BackColor);
        using var path = RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), Radius);
        using var fill = new SolidBrush(FillColor);
        using var pen = new Pen(BorderColor, 1f);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // OnPaint paints the full rounded surface, including the outside corners.
    }

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
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

    internal static Color VisualBackColor(Control? control, Color fallback)
    {
        return control switch
        {
            RoundedPanel panel => panel.FillColor,
            null => fallback,
            _ => control.BackColor
        };
    }
}

public sealed class RoundedButton : Button
{
    public int Radius { get; set; } = 10;
    public Color FillColor { get; set; } = Color.FromArgb(32, 49, 75);
    public Color HoverColor { get; set; } = Color.FromArgb(43, 65, 99);
    public Color BorderColor { get; set; } = Color.FromArgb(54, 78, 116);
    private bool _hover;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(32, 49, 75);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(RoundedPanel.VisualBackColor(Parent, BackColor));

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = RoundedPanel.RoundedRect(rect, Radius);
        using var fill = new SolidBrush(_hover ? HoverColor : FillColor);
        using var pen = new Pen(BorderColor, 1f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(pen, path);
        TextRenderer.DrawText(graphics, Text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Avoid the native rectangular background pass behind the rounded shape.
    }
}

public sealed partial class ThemedTextBox : TextBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private const int WmMouseMove = 0x0200;
    private const int WmMouseLeave = 0x02A3;
    private const int WmSetCursor = 0x0020;
    private const int EmSetMargins = 0x00D3;
    private const int EmSetRect = 0x00B3;
    private const int EcLeftMargin = 0x0001;
    private const int EcRightMargin = 0x0002;
    private const int HorizontalTextMargin = 16;

    public Color FillColor { get; set; } = Color.FromArgb(3, 11, 20);
    public Color FocusedFillColor { get; set; } = Color.FromArgb(6, 18, 32);
    public Color BorderColor { get; set; } = Color.FromArgb(54, 78, 116);
    public Color FocusedBorderColor { get; set; } = Color.FromArgb(126, 58, 242);
    public Color TextColor { get; set; } = Color.White;
    public Color PlaceholderColor { get; set; } = Color.FromArgb(118, 137, 163);
    public int Radius { get; set; } = 8;
    private string _centeredPlaceholderText = string.Empty;

    public string CenteredPlaceholderText
    {
        get => _centeredPlaceholderText;
        set
        {
            _centeredPlaceholderText = value;
            base.PlaceholderText = string.Empty;
            Invalidate();
        }
    }

    public ThemedTextBox()
    {
        AutoSize = false;
        AcceptsReturn = false;
        BorderStyle = BorderStyle.None;
        ApplyVisualColors();
        Multiline = true;
        ScrollBars = ScrollBars.None;
        WordWrap = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyVisualColors();
        ApplyTextMargins();
        UpdateRoundedRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRoundedRegion();
        ApplyTextMargins();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyTextMargins();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is WmPaint or WmNcPaint)
        {
            PaintChrome();
        }
        else if (m.Msg is WmMouseMove or WmMouseLeave or WmSetCursor)
        {
            PaintChrome();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        PaintChrome();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        PaintChrome();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        ApplyVisualColors();
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        ApplyVisualColors();
        Invalidate();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    private void PaintChrome()
    {
        var hdc = GetWindowDC(Handle);
        if (hdc == IntPtr.Zero) return;

        try
        {
            using var graphics = Graphics.FromHdc(hdc);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var border = new Pen(Focused ? FocusedBorderColor : BorderColor, 1.15f);
            using var path = RoundedPanel.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), Math.Max(1, Radius - 1));
            graphics.DrawPath(border, path);

            if (!Focused && TextLength == 0 && !string.IsNullOrWhiteSpace(CenteredPlaceholderText))
            {
                var textBounds = new Rectangle(
                    HorizontalTextMargin,
                    0,
                    Math.Max(0, Width - HorizontalTextMargin * 2),
                    Height);
                TextRenderer.DrawText(
                    graphics,
                    CenteredPlaceholderText,
                    Font,
                    textBounds,
                    PlaceholderColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }
        finally
        {
            _ = ReleaseDC(Handle, hdc);
        }
    }

    private void UpdateRoundedRegion()
    {
        if (Width <= 0 || Height <= 0) return;

        using var path = RoundedPanel.RoundedRect(new Rectangle(0, 0, Width, Height), Radius);
        var previousRegion = Region;
        Region = new Region(path);
        previousRegion?.Dispose();
    }

    private void ApplyVisualColors()
    {
        BackColor = Focused ? FocusedFillColor : FillColor;
        ForeColor = TextColor;
    }

    private void ApplyTextMargins()
    {
        if (!IsHandleCreated) return;

        SendMessage(
            Handle,
            EmSetMargins,
            (IntPtr)(EcLeftMargin | EcRightMargin),
            (IntPtr)((HorizontalTextMargin << 16) | HorizontalTextMargin));

        var textHeight = TextRenderer.MeasureText("Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
        var verticalInset = Math.Max(3, (Height - textHeight) / 2);
        var rect = new EditRect
        {
            Left = HorizontalTextMargin,
            Top = verticalInset,
            Right = Math.Max(HorizontalTextMargin, Width - HorizontalTextMargin),
            Bottom = Math.Max(verticalInset + textHeight, Height - verticalInset)
        };
        SendMessage(Handle, EmSetRect, IntPtr.Zero, ref rect);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr handle, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, ref EditRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct EditRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

public sealed class ThemedComboBox : Control
{
    private readonly ThemedComboBoxItemCollection _items;
    private ToolStripDropDown? _dropDown;
    private int _selectedIndex = -1;
    private bool _hover;
    private bool _pressed;
    private bool _closingDropDownFromCode;
    private bool _suppressNextMouseUp;

    public event EventHandler? SelectedIndexChanged;

    public Color FillColor { get; set; } = Color.FromArgb(3, 11, 20);
    public Color BorderColor { get; set; } = Color.FromArgb(54, 78, 116);
    public Color SelectedColor { get; set; } = Color.FromArgb(126, 58, 242);
    public Color TextColor { get; set; } = Color.White;
    public Color MutedColor { get; set; } = Color.FromArgb(177, 207, 242);
    public int Radius { get; set; } = 8;
    public int ItemHeight { get; set; } = 22;
    public int MaxDropDownItems { get; set; } = 14;
    public int DropDownWidth { get; set; }
    public int DropDownHeight { get; set; }
    public ThemedComboBoxItemCollection Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetSelectedIndex(value, notify: true);
    }

    public object? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
        set => SelectedIndex = _items.IndexOf(value);
    }

    [AllowNull]
    public override string Text
    {
        get => Convert.ToString(SelectedItem) ?? base.Text;
        set
        {
            var index = _items.IndexOfText(value);
            if (index >= 0)
            {
                SelectedIndex = index;
                return;
            }

            base.Text = value;
            Invalidate();
        }
    }

    public ThemedComboBox()
    {
        _items = new ThemedComboBoxItemCollection(this);
        BackColor = FillColor;
        ForeColor = TextColor;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable,
            true);
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_dropDown?.Visible == true)
            {
                CloseDropDown(suppressNextMouseUp: true);
                base.OnMouseDown(e);
                return;
            }

            _pressed = true;
            Focus();
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_suppressNextMouseUp)
            {
                _suppressNextMouseUp = false;
                _pressed = false;
                Invalidate();
                base.OnMouseUp(e);
                return;
            }

            _pressed = false;
            Invalidate();
            ShowDropDown();
        }

        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter or Keys.F4 || e.Alt && e.KeyCode == Keys.Down)
        {
            ToggleDropDown();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Up && SelectedIndex > 0)
        {
            SelectedIndex--;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Down && SelectedIndex < Items.Count - 1)
        {
            SelectedIndex++;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            CloseDropDown();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(RoundedPanel.VisualBackColor(Parent, BackColor));

        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        var arrowBounds = new Rectangle(Math.Max(0, Width - 28), 1, 26, Math.Max(0, Height - 3));
        var borderColor = Focused || _pressed ? SelectedColor : BorderColor;
        var arrowColor = _hover || _pressed ? Color.FromArgb(18, 31, 51) : Color.FromArgb(14, 24, 40);

        using (var fill = new SolidBrush(FillColor))
        using (var arrowFill = new SolidBrush(arrowColor))
        using (var border = new Pen(borderColor, 1f))
        using (var divider = new Pen(BorderColor, 1f))
        using (var path = RoundedPanel.RoundedRect(bounds, Radius))
        {
            graphics.FillPath(fill, path);
            graphics.SetClip(path);
            graphics.FillRectangle(arrowFill, arrowBounds);
            graphics.DrawLine(divider, arrowBounds.Left, 4, arrowBounds.Left, Height - 5);
            graphics.ResetClip();
            graphics.DrawPath(border, path);
        }

        var textBounds = new Rectangle(8, 0, Math.Max(0, Width - 42), Height);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textBounds,
            TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        DrawChevron(graphics, arrowBounds);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // OnPaint clears and draws the entire rounded control without native hover chrome.
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dropDown?.Dispose();
            _dropDown = null;
        }

        base.Dispose(disposing);
    }

    internal void OnItemsChanged()
    {
        if (_selectedIndex >= _items.Count) SetSelectedIndex(_items.Count - 1, notify: true);
        Invalidate();
    }

    private void SetSelectedIndex(int value, bool notify)
    {
        if (value < -1 || value >= _items.Count) value = -1;
        if (_selectedIndex == value) return;

        _selectedIndex = value;
        base.Text = Convert.ToString(SelectedItem) ?? string.Empty;
        Invalidate();
        if (notify) SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleDropDown()
    {
        if (_dropDown?.Visible == true) CloseDropDown();
        else ShowDropDown();
    }

    private void ShowDropDown()
    {
        if (_items.Count == 0) return;

        CloseDropDown();

        var list = new ThemedDropDownList
        {
            BackColor = FillColor,
            ForeColor = TextColor,
            BorderColor = BorderColor,
            SelectedColor = SelectedColor,
            TextColor = TextColor,
            Font = Font,
            ItemHeight = ItemHeight,
            Width = Math.Max(Width, DropDownWidth > 0 ? DropDownWidth : Width),
            Height = CalculateDropDownHeight()
        };
        list.Items.AddRange(_items.ToArray());
        if (list.Items.Count > 0)
        {
            var selectedIndex = SelectedIndex >= 0 && SelectedIndex < list.Items.Count
                ? SelectedIndex
                : 0;
            list.SelectedIndex = selectedIndex;
        }

        list.MouseMove += (_, args) =>
        {
            var index = list.IndexFromPoint(args.Location);
            if (index >= 0 && index < list.Items.Count) list.SelectedIndex = index;
        };
        list.MouseClick += (_, _) =>
        {
            if (list.SelectedIndex >= 0 && list.SelectedIndex < _items.Count) SelectedIndex = list.SelectedIndex;
            CloseDropDown();
        };

        _dropDown = new ToolStripDropDown
        {
            AutoClose = true,
            BackColor = FillColor,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        _dropDown.Items.Add(new ToolStripControlHost(list)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Size = list.Size
        });
        _dropDown.Closed += (sender, args) =>
        {
            if (!_closingDropDownFromCode && args.CloseReason != ToolStripDropDownCloseReason.CloseCalled)
            {
                _suppressNextMouseUp = ClientRectangle.Contains(PointToClient(Cursor.Position));
                if (ReferenceEquals(_dropDown, sender)) _dropDown = null;
                if (sender is ToolStripDropDown closedDropDown) DisposeDropDownLater(closedDropDown);
            }

            _pressed = false;
            Invalidate();
        };
        _dropDown.Show(this, new Point(0, Height));
        _pressed = true;
        Invalidate();
    }

    private void CloseDropDown(bool suppressNextMouseUp = false)
    {
        if (suppressNextMouseUp) _suppressNextMouseUp = true;

        var dropDown = _dropDown;
        if (dropDown is null)
        {
            _pressed = false;
            Invalidate();
            return;
        }

        _closingDropDownFromCode = true;
        try
        {
            dropDown.Close(ToolStripDropDownCloseReason.CloseCalled);
        }
        finally
        {
            _closingDropDownFromCode = false;
            if (ReferenceEquals(_dropDown, dropDown)) _dropDown = null;
            DisposeDropDownLater(dropDown);
            _pressed = false;
            Invalidate();
        }
    }

    private void DisposeDropDownLater(ToolStripDropDown dropDown)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            dropDown.Dispose();
            return;
        }

        BeginInvoke(() =>
        {
            if (!dropDown.IsDisposed) dropDown.Dispose();
        });
    }

    private int CalculateDropDownHeight()
    {
        var visibleRows = Math.Clamp(Math.Min(_items.Count, MaxDropDownItems), 1, Math.Max(1, MaxDropDownItems));
        var naturalHeight = visibleRows * ItemHeight + 2;
        return DropDownHeight > 0 ? Math.Min(DropDownHeight, naturalHeight) : naturalHeight;
    }

    private void DrawChevron(Graphics graphics, Rectangle bounds)
    {
        var centerX = bounds.Left + bounds.Width / 2;
        var centerY = bounds.Top + bounds.Height / 2 + 1;
        using var pen = new Pen(MutedColor, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawLine(pen, centerX - 5, centerY - 2, centerX, centerY + 3);
        graphics.DrawLine(pen, centerX, centerY + 3, centerX + 5, centerY - 2);
    }

    public sealed class ThemedComboBoxItemCollection
    {
        private readonly ThemedComboBox _owner;
        private readonly List<object> _items = new();

        internal ThemedComboBoxItemCollection(ThemedComboBox owner)
        {
            _owner = owner;
        }

        public int Count => _items.Count;

        public object this[int index] => _items[index];

        public void Add(object item)
        {
            _items.Add(item);
            _owner.OnItemsChanged();
        }

        public void AddRange(object[] items)
        {
            _items.AddRange(items);
            _owner.OnItemsChanged();
        }

        public void Clear()
        {
            _items.Clear();
            _owner.SetSelectedIndex(-1, notify: true);
            _owner.OnItemsChanged();
        }

        public object[] ToArray() => _items.ToArray();

        internal int IndexOf(object? value)
        {
            if (value == null) return -1;
            return _items.FindIndex(item => Equals(item, value));
        }

        internal int IndexOfText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return -1;
            return _items.FindIndex(item => string.Equals(Convert.ToString(item), value, StringComparison.Ordinal));
        }
    }

    private sealed class ThemedDropDownList : ListBox
    {
        private const int WsVScroll = 0x00200000;
        private const int ScrollBarWidth = 12;
        private const int ScrollBarPadding = 3;
        private const int MinimumThumbHeight = 30;

        private bool _draggingThumb;
        private int _thumbDragOffset;

        public Color BorderColor { get; set; }
        public Color SelectedColor { get; set; }
        public Color TextColor { get; set; }

        public ThemedDropDownList()
        {
            BorderStyle = BorderStyle.None;
            DrawMode = DrawMode.OwnerDrawFixed;
            IntegralHeight = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.Style &= ~WsVScroll;
                return createParams;
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count || e.Bounds.Width <= 0 || e.Bounds.Height <= 0) return;
            var itemText = Convert.ToString(Items[e.Index]) ?? string.Empty;

            var selected = e.State.HasFlag(DrawItemState.Selected);
            using var fill = new SolidBrush(selected ? SelectedColor : BackColor);
            e.Graphics.FillRectangle(fill, e.Bounds);

            var reservedScrollWidth = NeedsScrollBar ? ScrollBarWidth + 8 : 0;
            var textBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, Math.Max(0, e.Bounds.Width - 16 - reservedScrollWidth), e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                itemText,
                Font,
                textBounds,
                TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawScrollBar(e.Graphics);
            using var pen = new Pen(BorderColor, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var direction = e.Delta > 0 ? -1 : 1;
            var wheelSteps = Math.Max(1, Math.Abs(e.Delta) / 120);
            ScrollRows(direction * wheelSteps);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (NeedsScrollBar && e.Button == MouseButtons.Left)
            {
                var thumb = GetThumbBounds();
                if (thumb.Contains(e.Location))
                {
                    _draggingThumb = true;
                    _thumbDragOffset = e.Y - thumb.Top;
                    Capture = true;
                    return;
                }

                if (GetScrollTrackBounds().Contains(e.Location))
                {
                    ScrollRows(e.Y < thumb.Top ? -VisibleItemCount : VisibleItemCount);
                    return;
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_draggingThumb)
            {
                MoveThumbTo(e.Y - _thumbDragOffset);
                return;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_draggingThumb)
            {
                _draggingThumb = false;
                Capture = false;
                Invalidate();
                return;
            }

            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        private int VisibleItemCount => Math.Max(1, ClientSize.Height / Math.Max(1, ItemHeight));

        private int MaxTopIndex => Math.Max(0, Items.Count - VisibleItemCount);

        private bool NeedsScrollBar => Items.Count > VisibleItemCount;

        private void ScrollRows(int rows)
        {
            if (!NeedsScrollBar) return;

            TopIndex = Math.Clamp(TopIndex + rows, 0, MaxTopIndex);
            Invalidate();
        }

        private Rectangle GetScrollTrackBounds()
        {
            return new Rectangle(
                Math.Max(0, Width - ScrollBarWidth - 2),
                2,
                ScrollBarWidth,
                Math.Max(0, Height - 4));
        }

        private Rectangle GetThumbBounds()
        {
            var track = GetScrollTrackBounds();
            if (!NeedsScrollBar || track.Height <= 0) return Rectangle.Empty;

            var thumbHeight = Math.Clamp(
                (int)Math.Round(track.Height * VisibleItemCount / (double)Math.Max(1, Items.Count)),
                MinimumThumbHeight,
                Math.Max(MinimumThumbHeight, track.Height - ScrollBarPadding * 2));
            var availableTravel = Math.Max(0, track.Height - thumbHeight - ScrollBarPadding * 2);
            var thumbTop = track.Top + ScrollBarPadding;
            if (availableTravel > 0 && MaxTopIndex > 0)
            {
                thumbTop += (int)Math.Round(availableTravel * (TopIndex / (double)MaxTopIndex));
            }

            return new Rectangle(
                track.Left + 3,
                thumbTop,
                Math.Max(4, track.Width - 6),
                thumbHeight);
        }

        private void MoveThumbTo(int y)
        {
            var track = GetScrollTrackBounds();
            var thumb = GetThumbBounds();
            var availableTravel = Math.Max(0, track.Height - thumb.Height - ScrollBarPadding * 2);
            if (availableTravel == 0 || MaxTopIndex == 0) return;

            var localTop = Math.Clamp(y - track.Top - ScrollBarPadding, 0, availableTravel);
            TopIndex = Math.Clamp((int)Math.Round(MaxTopIndex * (localTop / (double)availableTravel)), 0, MaxTopIndex);
            Invalidate();
        }

        private void DrawScrollBar(Graphics graphics)
        {
            if (!NeedsScrollBar) return;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var track = GetScrollTrackBounds();
            var thumb = GetThumbBounds();

            using var trackBrush = new SolidBrush(Color.FromArgb(2, 8, 15));
            using var thumbBrush = new SolidBrush(Color.FromArgb(42, 62, 91));
            using var thumbHoverBrush = new SolidBrush(Color.FromArgb(58, 84, 122));
            using var trackPath = RoundedPanel.RoundedRect(track, 5);
            using var thumbPath = RoundedPanel.RoundedRect(thumb, 4);

            graphics.FillPath(trackBrush, trackPath);
            graphics.FillPath(_draggingThumb ? thumbHoverBrush : thumbBrush, thumbPath);
        }
    }
}

public sealed class ThemedCheckBox : CheckBox
{
    public Color FillColor { get; set; } = Color.FromArgb(3, 11, 20);
    public Color BorderColor { get; set; } = Color.FromArgb(54, 78, 116);
    public Color CheckedColor { get; set; } = Color.FromArgb(126, 58, 242);
    public Color CheckColor { get; set; } = Color.White;
    public Color TextColor { get; set; } = Color.FromArgb(177, 207, 242);
    private bool _hover;

    public ThemedCheckBox()
    {
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var boxSize = 16;
        var boxY = (Height - boxSize) / 2;
        var box = new Rectangle(0, boxY, boxSize, boxSize);
        var radius = 4;

        using (var path = RoundedPanel.RoundedRect(box, radius))
        using (var fill = new SolidBrush(Checked ? CheckedColor : FillColor))
        using (var border = new Pen(Checked ? CheckedColor : (_hover ? Color.FromArgb(78, 114, 166) : BorderColor), 1.2f))
        {
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        if (Checked)
        {
            using var pen = new Pen(CheckColor, 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            e.Graphics.DrawLine(pen, 4, boxY + 8, 7, boxY + 11);
            e.Graphics.DrawLine(pen, 7, boxY + 11, 12, boxY + 5);
        }

        var textBounds = new Rectangle(box.Right + 8, 0, Math.Max(0, Width - box.Right - 8), Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // The custom checkbox renderer clears and paints the whole surface.
    }
}

public sealed class ToggleSwitch : Control
{
    private bool _checked;
    private bool _hover;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Color OnColor { get; set; } = Color.FromArgb(126, 58, 242);
    public Color OnColor2 { get; set; } = Color.FromArgb(21, 220, 210);
    public Color OffColor { get; set; } = Color.FromArgb(14, 24, 40);
    public Color OffColor2 { get; set; } = Color.FromArgb(28, 44, 70);
    public Color KnobColor { get; set; } = Color.White;
    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        Cursor = Cursors.Hand;
        BackColor = Color.FromArgb(3, 11, 20);
        MinimumSize = new Size(64, 28);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
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
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        var track = new Rectangle(1, 3, Width - 3, Height - 7);
        var borderColor = Checked
            ? Color.FromArgb(_hover ? 235 : 205, OnColor2)
            : Color.FromArgb(_hover ? 210 : 150, OffColor2);
        using (var path = RoundedPanel.RoundedRect(track, track.Height / 2))
        using (var fill = new LinearGradientBrush(track, Checked ? OnColor : OffColor, Checked ? OnColor2 : OffColor2, LinearGradientMode.Horizontal))
        using (var border = new Pen(borderColor, 1.2f))
        using (var shine = new Pen(Color.FromArgb(Checked ? 55 : 28, Color.White), 1f))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);

            var shineY = track.Top + 5;
            graphics.DrawLine(shine, track.Left + track.Height / 2, shineY, track.Right - track.Height / 2, shineY);
        }

        var knob = Math.Max(18, Height - 12);
        var knobX = Checked ? Width - knob - 7 : 7;
        var knobY = (Height - knob) / 2;

        using (var shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
        {
            graphics.FillEllipse(shadow, knobX + 1, knobY + 2, knob, knob);
        }

        var knobBounds = new Rectangle(knobX, knobY, knob, knob);
        using var knobBrush = new LinearGradientBrush(knobBounds, Color.White, Color.FromArgb(200, 222, 250), LinearGradientMode.Vertical);
        using var knobBorder = new Pen(Color.FromArgb(195, 218, 244), 1f);
        graphics.FillEllipse(knobBrush, knobBounds);
        graphics.DrawEllipse(knobBorder, knobBounds);

        var glintBounds = new Rectangle(knobX + 5, knobY + 4, Math.Max(4, knob / 3), Math.Max(3, knob / 4));
        using var glint = new SolidBrush(Color.FromArgb(135, Color.White));
        graphics.FillEllipse(glint, glintBounds);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // The switch uses a custom rounded track and clears its own background.
    }
}
