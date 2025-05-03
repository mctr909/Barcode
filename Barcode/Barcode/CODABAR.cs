using System.Collections.Generic;
using System.Drawing;
using System.Linq;

class CODABAR : BaseCode {
	static readonly Dictionary<char, int> CODE = new Dictionary<char, int> {
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

	static readonly char[] TERM = { 'A', 'B', 'C', 'D' };

	static readonly char[] LONG = { 'A', 'B', 'C', 'D', '+', ':', '/', '.' };

	const int QUIET_SIZE = 15;

	public override float Width {
		get {
			var longCount = 0;
			var shortCount = 0;
			for (int i = 0; i < mValue.Length; i++) {
				if (LONG.Contains(mValue.ElementAt(i))) {
					longCount++;
				} else {
					shortCount++;
				}
			}
			var spaceWidth = (Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0)) * 2;
			return longCount * (Pitch * 14)
				+ shortCount * (Pitch * 12)
				+ spaceWidth;
		}
	}

	public override void Draw(Graphics g) {
		var spaceWidth = Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0);
		var wide = Pitch * 3;

		mCur = X;

		/* 開始 */
		mCur += spaceWidth;

		for (int i = 0; i < mValue.Length; i++) {
			var chr = mValue.ElementAt(i);
			if (!CODE.ContainsKey(chr)) {
				chr = '-';
			}
			if (1 <= i && i < mValue.Length - 1 && TERM.Contains(chr)) {
				chr = '-';
			}

			if (ShowValue) {
				/* テキスト描画 */
				g.DrawString($"{chr}", FONT, Brushes.Black, mCur, Bottom);
			}

			/* コード描画 */
			var code = CODE[chr];
			for (int mask = 0b10000000, m = 0; mask != 0; mask >>= 1, m ^= 1) {
				var width = 0 == (code & mask) ? Pitch : wide;
				if (0 == m) {
					DrawBar(g, width);
				}
				mCur += width;
			}
		}

		/* 終了 */
		mCur += spaceWidth;

		/* 枠の描画 */
		DrawBorder(g);
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
