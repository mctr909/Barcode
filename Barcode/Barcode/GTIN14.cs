using System.Drawing;
using System.Linq;

class GTIN14 : ITF {
	public override float Width {
		get {
			var spaceWidth = (Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0)) * 2;
			return 14 * (Pitch * 3 + Pitch * 3 * 2) + Pitch * 9 + spaceWidth;
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

		var sum = 0;
		var str = "";
		for (int i = 0; i < 14; i += 2) {
			var val1 = mValue.ElementAt(i) - '0';
			var val2 = mValue.ElementAt(i + 1) - '0';
			if (val1 < 0 || val1 > 9) {
				val1 = 0;
			}
			if (val2 < 0 || val2 > 9) {
				val2 = 0;
			}
			sum += val1 * 3 + val2;

			if (12 == i) {
				/* チェックディジット */
				val2 = (10 - sum % 10) % 10;
			}
			str += $"{val1}{val2}";

			/* コード描画 */
			DrawCode(g, val1, val2);
		}

		/* 終了コード描画 */
		DrawBar(g, Pitch * 3);
		mCur += Pitch * 4;
		DrawBar(g, Pitch);
		mCur += Pitch;
		mCur += spaceWidth;

		/* テキスト描画 */
		str = string.Format("{0} {1} {2} {3}",
			str.Substring(0, 1),
			str.Substring(1, 7),
			str.Substring(8, 5),
			str.Substring(13, 1)
		);
		var w = g.MeasureString(str, FONT).Width;
		g.DrawString(str, FONT, Brushes.Black, (mCur - w) / 2.0f, Bottom);

		/* 枠の描画 */
		DrawBorder(g);
	}

	protected override string TrimAndPad(string value) {
		return value.Replace("\r", "")
			.Replace(" ", "")
			.PadRight(13, '0')
			.Substring(0, 13) + "0";
	}
}
