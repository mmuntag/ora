using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Ora;

internal enum MediaAction { Play, Pause, Stop }

internal sealed class MediaButton : Button
{
    private MediaAction action;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MediaAction Action
    {
        get => action;
        set { action = value; Invalidate(); }
    }

    public MediaButton(MediaAction action)
    {
        Action = action;
        Size = new Size(48, 40);
        BackColor = Color.White;
        ForeColor = Color.FromArgb(20, 88, 100);
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Enabled ? ForeColor : SystemColors.GrayText);
        float x = ClientSize.Width / 2f, y = ClientSize.Height / 2f;
        switch (Action)
        {
            case MediaAction.Play:
                e.Graphics.FillPolygon(brush, [new PointF(x - 5, y - 8), new PointF(x + 8, y), new PointF(x - 5, y + 8)]);
                break;
            case MediaAction.Pause:
                e.Graphics.FillRectangle(brush, x - 7, y - 8, 5, 16);
                e.Graphics.FillRectangle(brush, x + 2, y - 8, 5, 16);
                break;
            case MediaAction.Stop:
                e.Graphics.FillRectangle(brush, x - 7, y - 7, 14, 14);
                break;
        }
    }
}
