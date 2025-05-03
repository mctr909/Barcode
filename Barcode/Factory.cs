using System.Drawing;

class Factory {
	private const int SPACE_HEIGHT = 20;

	public enum Type {
		EAN_JAN,
		GTIN14,
		ITF,
		CODABAR,
		CODE39,
		CODE128
	}

	private static BaseCode Create(Type type) {
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
		default:
			return null;
		}
	}

	public static Bitmap Draw(string value, decimal pitch, bool enableBorder, bool showValue, Type type) {
		var code = Create(type);
		code.Pitch = (float)pitch;
		code.EnableBorder = enableBorder;
		code.EnablePrintValue = showValue;
		var lines = value.Replace("\r", "").Split('\n');
		double maxWidth = 0;
		int lineCount;
		for (lineCount = 0; lineCount < lines.Length; lineCount++) {
			var line = lines[lineCount];
			if (string.IsNullOrWhiteSpace(line)) {
				continue;
			}
			var length = code.GetWidth(line);
			if (maxWidth < length) {
				maxWidth = length;
			}
		}
		code.CreateCanvas((int)maxWidth, lineCount * (int)(code.Height * code.Pitch + SPACE_HEIGHT));
		for (int l = 0; l < lines.Length; l++) {
			var line = lines[l];
			if (!string.IsNullOrWhiteSpace(line)) {
				var beginX = code.Left;
				var beginY = code.Top;
				code.Value = line;
				code.Draw();
				code.Left = beginX;
				code.Top = beginY;
			}
			code.Top += code.Height * code.Pitch;
			code.Top += SPACE_HEIGHT;
		}
		return code.Bmp;
	}
}
