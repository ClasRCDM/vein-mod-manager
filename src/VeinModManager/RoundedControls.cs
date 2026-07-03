using System.Drawing.Drawing2D;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VEIN_Item_And_Container_Modifier;

public class RoundedPanel : Panel
{
    public int Radius { get; set; } = 14;
    public Color FillColor { get; set; } = Color.FromArgb(10, 18, 30);
    public Color BorderColor { get; set; } = Color.FromArgb(32, 53, 82);
    public Color GradientTopColor { get; set; } = Color.Empty;
    public Color GradientBottomColor { get; set; } = Color.Empty;
    public Color HighlightColor { get; set; } = Color.Empty;
    public Color InnerShadowColor { get; set; } = Color.Empty;
    public Color GlowColor { get; set; } = Color.Empty;
    public float BorderThickness { get; set; } = 1f;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintRoundedSurface(e.Graphics, dashed: false);
    }

    protected void PaintRoundedSurface(Graphics graphics, bool dashed)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(VisualBackColor(Parent, BackColor));
        if (Width <= 2 || Height <= 2) return;

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = RoundedRect(rect, Radius);
        using var fill = CreateSurfaceBrush(rect);
        graphics.FillPath(fill, path);

        if (!GlowColor.IsEmpty)
        {
            using var glowPath = new GraphicsPath();
            glowPath.AddEllipse(rect.Left + rect.Width / 4, rect.Top - rect.Height / 2, Math.Max(1, rect.Width), Math.Max(1, rect.Height));
            using var glowBrush = new PathGradientBrush(glowPath)
            {
                CenterColor = Color.FromArgb(86, GlowColor),
                SurroundColors = new[] { Color.FromArgb(0, GlowColor) }
            };
            var state = graphics.Save();
            graphics.SetClip(path);
            graphics.FillPath(glowBrush, glowPath);
            graphics.Restore(state);
        }

        if (!InnerShadowColor.IsEmpty)
        {
            var state = graphics.Save();
            graphics.SetClip(path);
            using var shadow = new Pen(InnerShadowColor, Math.Max(2f, Radius * 0.38f));
            graphics.DrawPath(shadow, path);
            graphics.Restore(state);
        }

        if (!HighlightColor.IsEmpty)
        {
            var highlightBounds = Rectangle.Inflate(rect, -Math.Max(2, Radius / 3), -Math.Max(2, Radius / 3));
            if (highlightBounds.Width > 0 && highlightBounds.Height > 0)
            {
                using var highlight = new Pen(HighlightColor, 1f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                graphics.DrawLine(highlight, highlightBounds.Left, highlightBounds.Top, highlightBounds.Right, highlightBounds.Top);
            }
        }

        using var pen = new Pen(BorderColor, BorderThickness);
        if (dashed)
        {
            pen.DashPattern = new[] { 6f, 6f };
        }
        graphics.DrawPath(pen, path);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    internal Brush CreateSurfaceBrush(Rectangle bounds)
    {
        if (!GradientTopColor.IsEmpty || !GradientBottomColor.IsEmpty)
        {
            var top = GradientTopColor.IsEmpty ? FillColor : GradientTopColor;
            var bottom = GradientBottomColor.IsEmpty ? FillColor : GradientBottomColor;
            return new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical);
        }

        return new SolidBrush(FillColor);
    }

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0) return path;
        var diameter = Math.Max(1, Math.Min(Math.Max(1, radius) * 2, Math.Min(bounds.Width, bounds.Height)));
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

    internal static Color Blend(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)Math.Round(from.A + (to.A - from.A) * amount),
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));
    }

    internal static Color WithAlpha(Color color, int alpha)
    {
        return Color.FromArgb(Math.Clamp(alpha, 0, 255), color);
    }

    internal static void DrawFocusRing(Graphics graphics, Rectangle bounds, int radius, Color color)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2) return;
        using var ring = RoundedRect(Rectangle.Inflate(bounds, -2, -2), Math.Max(1, radius - 2));
        using var pen = new Pen(color, 1.4f);
        graphics.DrawPath(pen, ring);
    }
}


public sealed class DashedRoundedPanel : RoundedPanel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        PaintRoundedSurface(e.Graphics, dashed: true);
    }
}

public sealed class RedGlowPanel : RoundedPanel
{
    public RedGlowPanel()
    {
        GlowColor = Color.FromArgb(125, 20, 28);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(BackColor);

        using var path = RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), Radius);
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, path);

        using var glowPath = new GraphicsPath();
        glowPath.AddEllipse(Width / 3, -Height / 2, Width, Height);
        using var glowBrush = new PathGradientBrush(glowPath)
        {
            CenterColor = Color.FromArgb(92, GlowColor),
            SurroundColors = new[] { Color.FromArgb(0, GlowColor) }
        };
        e.Graphics.SetClip(path);
        e.Graphics.FillPath(glowBrush, glowPath);
        e.Graphics.ResetClip();

        using var pen = new Pen(BorderColor, 1f);
        e.Graphics.DrawPath(pen, path);
    }
}

public sealed class VeinLogoPanel : Control
{
    public Color GlowColor { get; set; } = Color.FromArgb(185, 24, 38);
    public Color MainColor { get; set; } = Color.White;
    public Color AccentColor { get; set; } = Color.FromArgb(185, 24, 38);

    public VeinLogoPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(BackColor);

        using var glowPath = new GraphicsPath();
        glowPath.AddEllipse(4, 24, Math.Max(1, Width - 8), 76);
        using var glowBrush = new PathGradientBrush(glowPath)
        {
            CenterColor = Color.FromArgb(135, GlowColor),
            SurroundColors = new[] { Color.FromArgb(0, GlowColor) }
        };
        e.Graphics.FillPath(glowBrush, glowPath);

        using var veinFont = new Font("Segoe UI", 39F, FontStyle.Bold, GraphicsUnit.Point);
        using var subFont = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        using var shadowBrush = new SolidBrush(Color.FromArgb(180, AccentColor));
        using var mainBrush = new SolidBrush(MainColor);
        using var subBrush = new SolidBrush(Color.FromArgb(238, 242, 255));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

        var veinRect = new RectangleF(0, 8, Width, 70);
        var shadowRect = new RectangleF(2, 12, Width, 70);
        e.Graphics.DrawString("VEIN", veinFont, shadowBrush, shadowRect, format);
        e.Graphics.DrawString("VEIN", veinFont, mainBrush, veinRect, format);
        e.Graphics.DrawString("MOD MANAGER", subFont, subBrush, new RectangleF(0, 88, Width, 30), format);
    }
}

public enum ScriptIconKind
{
    Clock,
    Bell,
    Disk,
    Bulb,
    Code
}

public sealed class ScriptIconPanel : Control
{
    public ScriptIconKind Kind { get; set; }
    public Color IconColor { get; set; } = Color.White;

    public ScriptIconPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        const int scale = 3;
        using var bitmap = new Bitmap(Math.Max(1, Width * scale), Math.Max(1, Height * scale));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.Clear(BackColor);
        graphics.ScaleTransform(scale, scale);

        switch (Kind)
        {
            case ScriptIconKind.Clock:
                DrawClock(graphics, ClientRectangle, IconColor);
                break;
            case ScriptIconKind.Bell:
                DrawBell(graphics, ClientRectangle, IconColor);
                break;
            case ScriptIconKind.Disk:
                DrawDisk(graphics, ClientRectangle, IconColor);
                break;
            case ScriptIconKind.Bulb:
                DrawBulb(graphics, ClientRectangle, IconColor);
                break;
            case ScriptIconKind.Code:
                DrawCode(graphics, ClientRectangle, IconColor);
                break;
        }

        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(bitmap, ClientRectangle);
    }

    private static void DrawClock(Graphics graphics, Rectangle bounds, Color color)
    {
        var size = Math.Min(bounds.Width, bounds.Height);
        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        var face = new RectangleF(cx - size * 0.29f, cy - size * 0.25f, size * 0.58f, size * 0.58f);
        var rim = RectangleF.Inflate(face, size * 0.035f, size * 0.035f);
        using var rimBrush = new LinearGradientBrush(rim, Color.FromArgb(250, 252, 255), Color.FromArgb(156, 171, 200), 90f);
        using var faceBrush = new SolidBrush(Color.FromArgb(253, 254, 255));
        using var rimPen = new Pen(Color.FromArgb(226, 236, 255), Math.Max(1.9f, size * 0.032f));
        using var handPen = new Pen(Color.FromArgb(22, 29, 46), Math.Max(2.2f, size * 0.04f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var markerBrush = new SolidBrush(Color.FromArgb(70, 82, 108));
        using var darkBrush = new SolidBrush(Color.FromArgb(35, 43, 62));
        graphics.FillEllipse(rimBrush, rim);
        graphics.DrawEllipse(rimPen, rim);
        graphics.FillEllipse(faceBrush, face);
        var marker = Math.Max(2.1f, size * 0.038f);
        graphics.FillEllipse(markerBrush, cx - marker / 2f, face.Top + face.Height * 0.12f, marker, marker);
        graphics.FillEllipse(markerBrush, face.Right - face.Width * 0.15f - marker / 2f, cy - marker / 2f, marker, marker);
        graphics.FillEllipse(markerBrush, cx - marker / 2f, face.Bottom - face.Height * 0.15f - marker / 2f, marker, marker);
        graphics.FillEllipse(markerBrush, face.Left + face.Width * 0.15f - marker / 2f, cy - marker / 2f, marker, marker);
        graphics.DrawLine(handPen, cx, cy, cx, cy - face.Height * 0.25f);
        graphics.DrawLine(handPen, cx, cy, cx + face.Width * 0.2f, cy + face.Height * 0.13f);
        graphics.FillEllipse(darkBrush, cx - size * 0.04f, cy - size * 0.04f, size * 0.08f, size * 0.08f);
        using var knobBrush = new SolidBrush(Color.FromArgb(234, 240, 252));
        graphics.FillRectangle(knobBrush, cx - size * 0.055f, rim.Top - size * 0.075f, size * 0.11f, size * 0.07f);
        graphics.FillEllipse(knobBrush, rim.Right - size * 0.08f, rim.Top + rim.Height * 0.14f, size * 0.09f, size * 0.09f);
    }

    private static void DrawBell(Graphics graphics, Rectangle bounds, Color color)
    {
        var size = Math.Min(bounds.Width, bounds.Height);
        var cx = bounds.Left + bounds.Width / 2f;
        var top = bounds.Top + size * 0.18f;
        var bottom = bounds.Top + size * 0.74f;
        using var path = new GraphicsPath();
        path.AddBezier(cx - size * 0.23f, bottom - size * 0.1f, cx - size * 0.27f, top + size * 0.2f, cx - size * 0.12f, top, cx, top);
        path.AddBezier(cx, top, cx + size * 0.12f, top, cx + size * 0.27f, top + size * 0.2f, cx + size * 0.23f, bottom - size * 0.1f);
        path.AddLine(cx + size * 0.23f, bottom - size * 0.1f, cx + size * 0.34f, bottom);
        path.AddBezier(cx + size * 0.23f, bottom + size * 0.04f, cx - size * 0.23f, bottom + size * 0.04f, cx - size * 0.34f, bottom, cx - size * 0.34f, bottom);
        path.CloseFigure();
        using var fill = new PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(255, 232, 87),
            SurroundColors = new[] { Color.FromArgb(217, 151, 21) }
        };
        using var pen = new Pen(Color.FromArgb(255, 217, 79), Math.Max(1.6f, size * 0.035f)) { LineJoin = LineJoin.Round };
        using var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
        using var shine = new Pen(Color.FromArgb(190, 255, 249, 170), Math.Max(1f, size * 0.02f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.FillEllipse(shadow, cx - size * 0.23f, bottom + size * 0.02f, size * 0.46f, size * 0.12f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(pen, path);
        graphics.DrawArc(shine, cx - size * 0.16f, top + size * 0.12f, size * 0.18f, size * 0.34f, 190, 92);
        using var clapper = new SolidBrush(Color.FromArgb(255, 230, 95));
        graphics.FillEllipse(clapper, cx - size * 0.075f, bottom + size * 0.025f, size * 0.15f, size * 0.15f);
        graphics.DrawArc(pen, cx - size * 0.11f, top - size * 0.08f, size * 0.22f, size * 0.16f, 205, 130);
    }

    private static void DrawDisk(Graphics graphics, Rectangle bounds, Color color)
    {
        var size = Math.Min(bounds.Width, bounds.Height);
        var rect = new RectangleF(bounds.Left + size * 0.22f, bounds.Top + size * 0.15f, size * 0.56f, size * 0.66f);
        using var path = RoundedPanel.RoundedRect(Rectangle.Round(rect), Math.Max(2, (int)(size * 0.045f)));
        using var fill = new LinearGradientBrush(rect, Color.FromArgb(255, 255, 255), Color.FromArgb(178, 194, 224), 90f);
        using var pen = new Pen(Color.FromArgb(226, 235, 255), Math.Max(1.6f, size * 0.035f)) { LineJoin = LineJoin.Round };
        using var dark = new SolidBrush(Color.FromArgb(42, 54, 78));
        using var slot = new SolidBrush(Color.FromArgb(235, 241, 255));
        graphics.FillPath(fill, path);
        graphics.DrawPath(pen, path);
        graphics.FillRectangle(dark, rect.Left + rect.Width * 0.16f, rect.Top + rect.Height * 0.1f, rect.Width * 0.66f, rect.Height * 0.26f);
        graphics.FillRectangle(slot, rect.Left + rect.Width * 0.58f, rect.Top + rect.Height * 0.12f, rect.Width * 0.16f, rect.Height * 0.2f);
        using var linePen = new Pen(Color.FromArgb(72, 85, 115), Math.Max(1.2f, size * 0.026f));
        graphics.DrawLine(linePen, rect.Left + rect.Width * 0.18f, rect.Bottom - rect.Height * 0.25f, rect.Right - rect.Width * 0.18f, rect.Bottom - rect.Height * 0.25f);
        graphics.DrawLine(linePen, rect.Left + rect.Width * 0.18f, rect.Bottom - rect.Height * 0.15f, rect.Right - rect.Width * 0.18f, rect.Bottom - rect.Height * 0.15f);
    }

    private static void DrawBulb(Graphics graphics, Rectangle bounds, Color color)
    {
        var size = Math.Min(bounds.Width, bounds.Height);
        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        using var rayPen = new Pen(Color.FromArgb(115, color), Math.Max(1f, size * 0.032f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        for (var i = 0; i < 5; i++)
        {
            var angle = Math.PI * 2d * i / 5d - Math.PI / 2d;
            var x1 = cx + (float)Math.Cos(angle) * size * 0.25f;
            var y1 = cy + (float)Math.Sin(angle) * size * 0.25f;
            var x2 = cx + (float)Math.Cos(angle) * size * 0.34f;
            var y2 = cy + (float)Math.Sin(angle) * size * 0.34f;
            graphics.DrawLine(rayPen, x1, y1, x2, y2);
        }
        var bulb = new RectangleF(cx - size * 0.21f, bounds.Top + size * 0.16f, size * 0.42f, size * 0.42f);
        using var bulbPath = new GraphicsPath();
        bulbPath.AddEllipse(bulb);
        using var fill = new PathGradientBrush(bulbPath)
        {
            CenterColor = Color.FromArgb(255, 248, 138),
            SurroundColors = new[] { Color.FromArgb(232, 172, 28) }
        };
        using var pen = new Pen(Color.FromArgb(255, 226, 94), Math.Max(1.2f, size * 0.035f));
        graphics.FillPath(fill, bulbPath);
        graphics.DrawPath(pen, bulbPath);
        using var basePen = new Pen(Color.FromArgb(226, 210, 132), Math.Max(1.4f, size * 0.045f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawLine(basePen, cx - size * 0.11f, bounds.Top + size * 0.61f, cx + size * 0.11f, bounds.Top + size * 0.61f);
        graphics.DrawLine(basePen, cx - size * 0.09f, bounds.Top + size * 0.72f, cx + size * 0.09f, bounds.Top + size * 0.72f);
    }

    private static void DrawCode(Graphics graphics, Rectangle bounds, Color color)
    {
        var size = Math.Min(bounds.Width, bounds.Height);
        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        using var pen = new Pen(color, Math.Max(1.4f, size * 0.07f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        graphics.DrawLine(pen, cx - size * 0.32f, cy, cx - size * 0.18f, cy - size * 0.14f);
        graphics.DrawLine(pen, cx - size * 0.32f, cy, cx - size * 0.18f, cy + size * 0.14f);
        graphics.DrawLine(pen, cx + size * 0.32f, cy, cx + size * 0.18f, cy - size * 0.14f);
        graphics.DrawLine(pen, cx + size * 0.32f, cy, cx + size * 0.18f, cy + size * 0.14f);
        graphics.DrawLine(pen, cx + size * 0.04f, cy - size * 0.2f, cx - size * 0.04f, cy + size * 0.2f);
    }
}

public sealed class RoundedButton : Button
{
    public int Radius { get; set; } = 10;
    public Color FillColor { get; set; } = Color.FromArgb(32, 49, 75);
    public Color HoverColor { get; set; } = Color.FromArgb(43, 65, 99);
    public Color PressedColor { get; set; } = Color.Empty;
    public Color BorderColor { get; set; } = Color.FromArgb(54, 78, 116);
    public Color FocusBorderColor { get; set; } = Color.FromArgb(167, 139, 250);
    public Color DisabledFillColor { get; set; } = Color.FromArgb(18, 27, 43);
    public Color DisabledTextColor { get; set; } = Color.FromArgb(105, 119, 145);
    public string IconText { get; set; } = string.Empty;
    public Font? IconFont { get; set; }
    public int ContentLeftPadding { get; set; }
    public int IconTextGap { get; set; } = 12;
    public Color AccentColor { get; set; } = Color.Empty;
    public int AccentWidth { get; set; }
    private bool _hover;
    private bool _pressed;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(32, 49, 75);
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
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
            Focus();
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = false;
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(RoundedPanel.VisualBackColor(Parent, BackColor));

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        var currentFill = !Enabled
            ? DisabledFillColor
            : _pressed ? (PressedColor.IsEmpty ? RoundedPanel.Blend(HoverColor, Color.Black, 0.12f) : PressedColor)
            : _hover ? HoverColor : FillColor;
        var currentBorder = Focused && Enabled ? FocusBorderColor : BorderColor;
        using var path = RoundedPanel.RoundedRect(rect, Radius);
        using var fill = new SolidBrush(currentFill);
        using var pen = new Pen(currentBorder, Focused && Enabled ? 1.35f : 1f);
        graphics.FillPath(fill, path);
        using (var highlight = new Pen(RoundedPanel.WithAlpha(Color.White, _hover || Focused ? 34 : 18), 1f))
        {
            graphics.DrawLine(highlight, rect.Left + Radius, rect.Top + 1, rect.Right - Radius, rect.Top + 1);
        }
        graphics.DrawPath(pen, path);
        if (Focused && Enabled)
        {
            RoundedPanel.DrawFocusRing(graphics, rect, Radius, FocusBorderColor);
        }

        if (!AccentColor.IsEmpty && AccentWidth > 0)
        {
            var accentBounds = new Rectangle(rect.Left + 8, rect.Top + 14, AccentWidth, Math.Max(0, rect.Height - 28));
            using var accentPath = RoundedPanel.RoundedRect(accentBounds, Math.Max(1, AccentWidth / 2));
            using var accentBrush = new SolidBrush(AccentColor);
            graphics.FillPath(accentBrush, accentPath);
        }

        var textColor = Enabled ? ForeColor : DisabledTextColor;
        if (string.IsNullOrEmpty(IconText))
        {
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            return;
        }

        var iconBounds = new Rectangle(ContentLeftPadding, 0, 22, Height);
        TextRenderer.DrawText(
            graphics,
            IconText,
            IconFont ?? Font,
            iconBounds,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var textLeft = iconBounds.Right + IconTextGap;
        var textBounds = new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft - 12), Height);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textBounds,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
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
    public Color HoverBorderColor { get; set; } = Color.FromArgb(73, 106, 153);
    public Color FocusedBorderColor { get; set; } = Color.FromArgb(126, 58, 242);
    public Color TextColor { get; set; } = Color.White;
    public Color PlaceholderColor { get; set; } = Color.FromArgb(118, 137, 163);
    public int Radius { get; set; } = 8;
    private string _centeredPlaceholderText = string.Empty;
    private bool _hover;

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
        _hover = true;
        base.OnMouseEnter(e);
        PaintChrome();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        base.OnMouseLeave(e);
        PaintChrome();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        ApplyVisualColors();
        ApplyTextMargins();
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        ApplyVisualColors();
        ApplyTextMargins();
        Invalidate();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyTextMargins();
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
            var borderColor = Focused ? FocusedBorderColor : _hover ? HoverBorderColor : BorderColor;
            using var border = new Pen(borderColor, Focused ? 1.35f : 1.15f);
            using var path = RoundedPanel.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), Math.Max(1, Radius - 1));
            graphics.DrawPath(border, path);
            if (Focused)
            {
                RoundedPanel.DrawFocusRing(graphics, new Rectangle(0, 0, Width - 1, Height - 1), Radius, FocusedBorderColor);
            }

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
        var verticalInset = Math.Max(6, (Height - textHeight) / 2 + 4);
        var rect = new EditRect
        {
            Left = HorizontalTextMargin,
            Top = verticalInset,
            Right = Math.Max(HorizontalTextMargin, Width - HorizontalTextMargin),
            Bottom = Math.Max(verticalInset + textHeight, Height - 2)
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
    public Color PressedFillColor { get; set; } = Color.FromArgb(8, 19, 35);
    public Color BorderColor { get; set; } = Color.FromArgb(54, 78, 116);
    public Color HoverBorderColor { get; set; } = Color.FromArgb(73, 106, 153);
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

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
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
        var borderColor = Focused || _pressed ? SelectedColor : _hover ? HoverBorderColor : BorderColor;
        var fillColor = _pressed ? PressedFillColor : FillColor;
        var arrowColor = _hover || _pressed ? RoundedPanel.Blend(PressedFillColor, SelectedColor, 0.18f) : Color.FromArgb(14, 24, 40);

        using (var fill = new SolidBrush(fillColor))
        using (var arrowFill = new SolidBrush(arrowColor))
        using (var border = new Pen(borderColor, Focused || _pressed ? 1.35f : 1f))
        using (var divider = new Pen(_hover || Focused ? HoverBorderColor : BorderColor, 1f))
        using (var path = RoundedPanel.RoundedRect(bounds, Radius))
        {
            graphics.FillPath(fill, path);
            graphics.SetClip(path);
            graphics.FillRectangle(arrowFill, arrowBounds);
            graphics.DrawLine(divider, arrowBounds.Left, 4, arrowBounds.Left, Height - 5);
            graphics.ResetClip();
            using var highlight = new Pen(RoundedPanel.WithAlpha(Color.White, Focused || _hover ? 32 : 16), 1f);
            graphics.DrawLine(highlight, bounds.Left + Radius, bounds.Top + 1, bounds.Right - Radius, bounds.Top + 1);
            graphics.DrawPath(border, path);
            if (Focused)
            {
                RoundedPanel.DrawFocusRing(graphics, bounds, Radius, SelectedColor);
            }
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
            e.Graphics.DrawLines(pen, new[]
            {
                new Point(4, boxY + 8),
                new Point(7, boxY + 11),
                new Point(12, boxY + 5)
            });
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
    }
}
