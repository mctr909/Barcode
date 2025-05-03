using System.Drawing;

abstract class BaseCode {
	public float X { get; set; }
	public float Y { get; set; }
	public abstract float Width { get; }
	public float Height { get; set; }
	public float Right => X + Width;
	public float Bottom => Y + Height;

	public float Pitch { get; set; }
	public bool Border { get; set; }
	public bool ShowValue { get; set; }
	public string Value {
		get { return mValue; }
		set { mValue = TrimAndPad(value); }
	}

	protected float mCur;
	protected string mValue = "";

	protected const int BORDER_WEIGHT = 6;

	protected static readonly Font FONT = new Font("MS Gothic", 9);

	public abstract void Draw(Graphics g);

	protected void DrawBorder(Graphics g) {
		if (Border) {
			var dw = BORDER_WEIGHT * 0.5f;
			g.DrawRectangle(new Pen(Brushes.Black, BORDER_WEIGHT),
				X + dw, Y + dw,
				mCur - dw, Height - BORDER_WEIGHT
			);
		}
	}

	protected void DrawBar(Graphics g, float width, float ofsY = 0.0f) {
		var x = (int)mCur;
		var y = (int)Y;
		var w = width;
		var h = Height + ofsY;
		var dx = X - x;
		var dw = width - (int)width;
		var gray = new Pen(Color.FromArgb(95, 0, 0, 0));
		if (0.0 < dx) {
			g.FillRectangle(Brushes.Black, x + 1, y, w - 1, h);
			if (0.0 < dw) {
				g.DrawLine(gray,
					x + w + 1, y,
					x + w + 1, y + h - 1
				);
			} else {
				g.DrawLine(gray,
					x + w, y,
					x + w, y + h - 1
				);
			}
		} else {
			if (0.0 < dw) {
				g.FillRectangle(Brushes.Black, x, y, w - 1, h);
				g.DrawLine(gray,
					x + w, y,
					x + w, y + h - 1
				);
			} else {
				g.FillRectangle(Brushes.Black, x, y, w, h);
			}
		}
	}

	protected virtual string TrimAndPad(string value) { return value; }
}
