using System.Drawing;

abstract class BaseCode {
	private const int BORDER_WEIGHT = 6;
	private static readonly Font FONT = new Font("MS Gothic", 9);

	public Bitmap Bmp { get; private set; }

	public abstract float Width { get; }
	public float Height { get; set; } = 40;
	public float Left { get; set; }
	public float Top { get; set; }
	public float Right => Left + Width;
	public float Bottom => Top + Height * Pitch;

	public float Pitch { get; set; } = 1.5f;
	public bool EnableBorder { get; set; }
	public bool EnablePrintValue { get; set; }

	public string Value {
		get { return mValue; }
		set { mValue = TrimAndPad(value); }
	}

	protected float Cur { get; set; }

	protected float BorderWidth => EnableBorder ? BORDER_WEIGHT : 0.0f;

	private Graphics mG;
	private string mValue = "";

	public void CreateCanvas(int width, int height) {
		mG?.Dispose();
		mG = null;
		Bmp?.Dispose();
		Bmp = null;
		if (width < 1) {
			width = 1;
		}
		if (height < 1) {
			height = 1;
		}
		Bmp = new Bitmap(width, height);
		mG = Graphics.FromImage(Bmp);
	}

	public float GetWidth(string value) {
		Value = value;
		return Width + BorderWidth * 0.5f;
	}

	public abstract void Draw();

	protected void DrawBoader() {
		if (EnableBorder) {
			var dw = BORDER_WEIGHT * 0.5f;
			mG.DrawRectangle(new Pen(Brushes.Black, BORDER_WEIGHT),
				Left + dw, Top + dw,
				Cur - dw, Height * Pitch - BORDER_WEIGHT
			);
		}
	}

	protected void DrawBar(float width, float ofsY = 0.0f) {
		var x = (int)Cur;
		var y = (int)Top;
		var w = width;
		var h = Height * Pitch + ofsY;
		var dx = Left - x;
		var dw = width - (int)width;
		var gray = new Pen(Color.FromArgb(95, 0, 0, 0));
		if (0.0 < dx) {
			mG.FillRectangle(Brushes.Black, x + 1, y, w - 1, h);
			if (0.0 < dw) {
				mG.DrawLine(gray,
					x + w + 1, y,
					x + w + 1, y + h - 1
				);
			} else {
				mG.DrawLine(gray,
					x + w, y,
					x + w, y + h - 1
				);
			}
		} else {
			if (0.0 < dw) {
				mG.FillRectangle(Brushes.Black, x, y, w - 1, h);
				mG.DrawLine(gray,
					x + w, y,
					x + w, y + h - 1
				);
			} else {
				mG.FillRectangle(Brushes.Black, x, y, w, h);
			}
		}
	}

	protected void DrawString(string value, float x, float y) {
		mG.DrawString(value, FONT, Brushes.Black, x, y);
	}

	protected SizeF MeasureString(string text) {
		return mG.MeasureString(text, FONT);
	}

	protected virtual string TrimAndPad(string value) { return value; }
}
