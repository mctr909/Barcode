using System.Linq;

class ITF : BaseCode {
	protected const int QUIET_SIZE = 15;

	protected static readonly int[] CODE = {
		0b00110,
		0b10001,
		0b01001,
		0b11000,
		0b00101,
		0b10100,
		0b01100,
		0b00011,
		0b10010,
		0b01010
	};

	public override float Width {
		get {
			var codeWidth = Pitch * Value.Length * (3 + 3*2);
			var spaceWidth = Pitch * QUIET_SIZE + BorderWidth;
			return codeWidth + spaceWidth*2 + Pitch * 9;
		}
	}

	public override void Draw() {
		var spaceWidth = Pitch * QUIET_SIZE + BorderWidth;

		/* 開始コード描画 */
		Cur = Left;
		Cur += spaceWidth;
		DrawBar(Pitch);
		Cur += Pitch * 2;
		DrawBar(Pitch);
		Cur += Pitch * 2;

		for (int pos = 0; pos < Value.Length; pos += 2) {
			var index1 = Value.ElementAt(pos) - '0';
			var index2 = Value.ElementAt(pos + 1) - '0';
			if (index1 < 0 || index1 > 9) {
				index1 = 0;
			}
			if (index2 < 0 || index2 > 9) {
				index2 = 0;
			}

			if (EnablePrintValue) {
				/* テキスト描画 */
				DrawString($"{index1}{index2}", Cur, Bottom);
			}

			/* コード描画 */
			DrawCode(index1, index2);
		}

		/* 終了コード描画 */
		DrawBar(Pitch * 3);
		Cur += Pitch * 4;
		DrawBar(Pitch);
		Cur += Pitch;
		Cur += spaceWidth;

		/* 枠の描画 */
		DrawBoader();
	}

	protected override string TrimAndPad(string value) {
		var ret = value.Replace("\r", "").Replace(" ", "");
		if (1 == ret.Length % 2) {
			ret += "0";
		}
		return ret;
	}

	protected void DrawCode(int index1, int index2) {
		var wide = Pitch * 3;
		var code1 = CODE[index1];
		var code2 = CODE[index2];
		for (int mask = 0b10000; mask != 0; mask >>= 1) {
			var width1 = 0 == (code1 & mask) ? Pitch : wide;
			DrawBar(width1);
			Cur += width1;
			Cur += 0 == (code2 & mask) ? Pitch : wide;
		}
	}
}
