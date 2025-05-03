using System.Collections.Generic;
using System.Linq;

class CODABAR : BaseCode {
	private const int QUIET_SIZE = 15;
	private const int WIDE_RATE = 3;
	private const int SHORT_WIDTH = 12;
	private const int LONG_WIDTH = 14;

	private static readonly Dictionary<char, int> CODE = new Dictionary<char, int> {
		{ '0', 0b00000110 },
		{ '1', 0b00001100 },
		{ '2', 0b00010010 },
		{ '3', 0b11000000 },
		{ '4', 0b00100100 },
		{ '5', 0b10000100 },
		{ '6', 0b01000010 },
		{ '7', 0b01001000 },
		{ '8', 0b01100000 },
		{ '9', 0b10010000 },
		{ '-', 0b00011000 },
		{ '$', 0b00110000 },
		{ 'A', 0b00110100 },
		{ 'B', 0b01010010 },
		{ 'C', 0b00010110 },
		{ 'D', 0b00011100 },
		{ '+', 0b00101010 },
		{ ':', 0b10001010 },
		{ '/', 0b10100010 },
		{ '.', 0b10101000 }
	};

	private static readonly char[] TERM = { 'A', 'B', 'C', 'D' };

	private static readonly char[] LONG = { 'A', 'B', 'C', 'D', '+', ':', '/', '.' };

	public override float Width {
		get {
			var longCount = 0;
			var shortCount = 0;
			for (int i = 0; i < Value.Length; i++) {
				if (LONG.Contains(Value.ElementAt(i))) {
					longCount++;
				} else {
					shortCount++;
				}
			}
			var spaceWidth = Pitch * QUIET_SIZE + BorderWidth;
			return longCount * (Pitch * LONG_WIDTH)
				+ shortCount * (Pitch * SHORT_WIDTH)
				+ spaceWidth * 2;
		}
	}

	public override void Draw() {
		var spaceWidth = Pitch * QUIET_SIZE + BorderWidth;
		var wide = Pitch * WIDE_RATE;

		/* 開始 */
		Cur = Left;
		Cur += spaceWidth;

		for (int pos = 0; pos < Value.Length; pos++) {
			var chr = Value.ElementAt(pos);
			if (!CODE.ContainsKey(chr)) {
				chr = '-';
			}
			if (1 <= pos && pos < Value.Length - 1 && TERM.Contains(chr)) {
				chr = '-';
			}

			if (EnablePrintValue) {
				/* テキスト描画 */
				DrawString($"{chr}", Cur, Bottom);
			}

			/* コード描画 */
			var code = CODE[chr];
			for (int mask = 0b10000000, m = 0; mask != 0; mask >>= 1, m ^= 1) {
				var width = 0 == (code & mask) ? Pitch : wide;
				if (0 == m) {
					DrawBar(width);
				}
				Cur += width;
			}
		}

		/* 終了 */
		Cur += spaceWidth;

		/* 枠の描画 */
		DrawBoader();
	}

	protected override string TrimAndPad(string value) {
		var ret = value.Replace("\r", "").Replace(" ", "").ToUpper();
		if (!TERM.Contains(ret.ElementAt(0))) {
			ret = "A" + ret;
		}
		if (!TERM.Contains(ret.ElementAt(ret.Length - 1))) {
			ret += "A";
		}
		return ret;
	}
}
