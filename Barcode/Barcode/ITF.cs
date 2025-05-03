using System.Drawing;
using System.Linq;

class ITF : BaseCode {
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

	protected const int QUIET_SIZE = 15;

	public override float Width {
		get {
			var spaceWidth = (Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0)) * 2;
			return mValue.Length * (Pitch * 3 + Pitch * 3 * 2) + Pitch * 9 + spaceWidth;
		}
	}

	public override void Draw(Graphics g) {
		var spaceWidth = Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0);

		mCur = X;

		/* 開始コード描画 */
		mCur += spaceWidth;
		DrawBar(g, Pitch);
		mCur += Pitch * 2;
		DrawBar(g, Pitch);
		mCur += Pitch * 2;

		for (int i = 0; i < mValue.Length; i += 2) {
			var val1 = mValue.ElementAt(i) - '0';
			var val2 = mValue.ElementAt(i + 1) - '0';
			if (val1 < 0 || val1 > 9) {
				val1 = 0;
			}
			if (val2 < 0 || val2 > 9) {
				val2 = 0;
			}

			if (ShowValue) {
				/* テキスト描画 */
				g.DrawString($"{val1}{val2}", FONT, Brushes.Black, mCur, Bottom);
			}

			/* コード描画 */
			DrawCode(g, val1, val2);
		}

		/* 終了コード描画 */
		DrawBar(g, Pitch * 3);
		mCur += Pitch * 4;
		DrawBar(g, Pitch);
		mCur += Pitch;
		mCur += spaceWidth;

		/* 枠の描画 */
		DrawBorder(g);
	}

	protected override string TrimAndPad(string value) {
		var ret = value.Replace("\r", "").Replace(" ", "");
		if (1 == ret.Length % 2) {
			ret += "0";
		}
		return ret;
	}

	protected void DrawCode(Graphics g, int val1, int val2) {
		var wide = Pitch * 3;
		var code1 = CODE[val1];
		var code2 = CODE[val2];
		for (int mask = 0b10000; mask != 0; mask >>= 1) {
			var width1 = 0 == (code1 & mask) ? Pitch : wide;
			DrawBar(g, width1);
			mCur += width1;
			mCur += 0 == (code2 & mask) ? Pitch : wide;
		}
	}
}
