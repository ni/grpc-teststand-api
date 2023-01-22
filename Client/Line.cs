using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ExampleClient;

/// <summary>
/// Draws a vertical or horizontal line
/// </summary>
public class Line : Control
{
    private int _thickness = 1;
    private bool _isVertical = false;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Color lineForeColor = Color.FromArgb(203, 205, 205);

			using var pen = new Pen(lineForeColor, _thickness);
			if (_isVertical)
			{
				e.Graphics.DrawLine(pen, 0, 0, 0, Height);
			}
			else
			{
				e.Graphics.DrawLine(pen, 0, 0, Width, 0);
			}
		}

    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        Invalidate();
    }

    public bool IsVertical
    {
        get => _isVertical;
        set
        {
            _isVertical = value;
            Invalidate();
        }
    }

    public int Thickness
    {
        get => _thickness;
        set
        {
            Debug.Assert(_thickness > 0);

            _thickness = value;
            Invalidate();
        }
    }
}
