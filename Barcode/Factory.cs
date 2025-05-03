using System.Drawing;

class Factory {
	public enum Type {
		EAN_JAN,
		GTIN14,
		ITF,
		CODABAR,
		CODE39,
		CODE128,
		GS1_128
	}

	private BaseCode Code;
	private Bitmap Bmp;
	private Graphics G;
	private bool Border = false;
	private float PosX = 0.0f;
	private float PosY = 0.0f;
	private float Pitch = 1.0f;
	private int Height {
		get { return GHeight; }
		set {
			GHeight = value;
			BarHeight = value - 10;
		}
	}
	private int GHeight = 50;
	private int BarHeight = 40;

	private Factory(Type type) {
		Code = Create(type);
	}

	private BaseCode Create(Type type) {
		switch (type) {
		case Type.EAN_JAN:
			return new EAN();
		case Type.GTIN14:
			return new GTIN14();
		case Type.ITF:
			return new ITF();
		case Type.CODABAR:
			return new CODABAR();
		case Type.CODE39:
			return new CODE39();
		case Type.CODE128:
			return new CODE128();
		case Type.GS1_128:
			return new GS1();
		default:
			return null;
		}
	}

	private void CreateCanvas(int width, int height) {
		if (null != G) {
			G.Dispose();
			G = null;
		}
		if (null != Bmp) {
			Bmp.Dispose();
			Bmp = null;
		}
		if (width < 1) {
			width = 1;
		}
		if (height < 1) {
			height = 1;
		}
		Bmp = new Bitmap(width+2, height);
		G = Graphics.FromImage(Bmp);
		PosX = 0.0f;
		PosY = 0.0f;
	}

	private float GetWidth(string value) {
		if (null == Code) {
			return 0.0f;
		}
		Code.Value = value;
		Code.Pitch = Pitch;
		Code.Border = Border;
		return Code.Width;
	}

	private void Draw(string value, bool showValue) {
		if (null == Code) {
			return;
		}
		var beginX = PosX;
		var beginY = PosY;
		Code.X = PosX;
		Code.Y = PosY;
		Code.Value = value;
		Code.ShowValue = showValue;
		Code.Pitch = Pitch;
		Code.Height = BarHeight;
		Code.Border = Border;
		Code.Draw(G);
		PosX = beginX;
		PosY = beginY;
	}

	public static Bitmap Draw(string value, bool enableBorder, bool showValue, Type type) {
		var f = new Factory(type);
		f.Border = enableBorder;
		var lines = value.Replace("\r", "").Split('\n');
		double maxWidth = 0;
		int lineCount;
		for (lineCount = 0; lineCount < lines.Length; lineCount++) {
			var line = lines[lineCount];
			if (string.IsNullOrWhiteSpace(line)) {
				continue;
			}
			var length = f.GetWidth(line);
			if (maxWidth < length) {
				maxWidth = length;
			}
		}
		const int SPACE_HEIGHT = 20;
		f.CreateCanvas((int)maxWidth, lineCount * (f.Height + SPACE_HEIGHT) - SPACE_HEIGHT);
		for (int l = 0; l < lines.Length; l++) {
			var line = lines[l];
			if (!string.IsNullOrWhiteSpace(line)) {
				f.Draw(line, showValue);
			}
			f.PosY += f.Height;
			f.PosY += SPACE_HEIGHT;
		}
		return f.Bmp;
	}
}
