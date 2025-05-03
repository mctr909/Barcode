using System.Drawing;
using System.Linq;

class EAN : BaseCode {
	static readonly int[,] CODE_L = {
		{ 0x1123, 0x3211 },
		{ 0x1222, 0x2221 },
		{ 0x2212, 0x2122 },
		{ 0x1141, 0x1411 },
		{ 0x2311, 0x1132 },
		{ 0x1321, 0x1231 },
		{ 0x4111, 0x1114 },
		{ 0x2131, 0x1312 },
		{ 0x3121, 0x1213 },
		{ 0x2113, 0x3112 }
	};

	static readonly int[] CODE_R = {
		0x01231,
		0x02221,
		0x12121,
		0x01411,
		0x13111,
		0x03211,
		0x31111,
		0x11311,
		0x21211,
		0x11131
	};

	static readonly int[] PARITY = {
		0b0000000,
		0b1101000,
		0b1011000,
		0b0111000,
		0b1100100,
		0b1001100,
		0b0011100,
		0b1010100,
		0b0110100,
		0b0101100
	};

	const int QUIET_SIZE = 15;

	public override float Width {
		get {
			var spaceWidth = Pitch * QUIET_SIZE * 2;
			return Pitch * 94 + spaceWidth;
		}
	}

	public override void Draw(Graphics g) {
		var quietSize = QUIET_SIZE * Pitch;
		var notchHeight = g.MeasureString("0", FONT).Height - 3;

		mCur = X;

		/* 開始コード描画 */
		mCur += quietSize;
		DrawBar(g, Pitch);
		mCur += Pitch * 2;
		DrawBar(g, Pitch);
		mCur += Pitch;

		var sum = 0;
		var parity = 0;
		for (int i = 0; i < mValue.Length; i++) {
			var val = mValue.ElementAt(i) - '0';
			if (val < 0 || val > 9) {
				val = 0;
			}
			sum += val * (0 == i % 2 ? 1 : 3);

			switch (i) {
			case 0:
				/* パリティ指定桁 */
				parity = PARITY[val];
				g.DrawString($"{val}", FONT, Brushes.Black, mCur - Pitch * 11, Bottom - notchHeight);
				/* 次の桁へ */
				continue;
			case 7:
				/* センターバー描画 */
				mCur += Pitch;
				DrawBar(g, Pitch);
				mCur += Pitch * 2;
				DrawBar(g, Pitch);
				mCur += Pitch;
				break;
			case 12:
				/* チェックディジット */
				val = (10 - sum % 10) % 10;
				break;
			}

			/* テキスト描画 */
			g.DrawString($"{val}", FONT, Brushes.Black, mCur, Bottom - notchHeight);

			/* コード描画 */
			var code = i < 7 ? CODE_L[val, (parity >> i) & 1] : CODE_R[val];
			for (int m = 1; code != 0; code >>= 4, m ^= 1) {
				var width = (code & 0xF) * Pitch;
				if (0 == m) {
					DrawBar(g, width, -notchHeight);
				}
				mCur += width;
			}
		}

		/* 終了コード描画 */
		mCur += Pitch;
		DrawBar(g, Pitch);
		mCur += Pitch * 2;
		DrawBar(g, Pitch);
		mCur += quietSize;
	}

	protected override string TrimAndPad(string value) {
		return value.Replace("\r", "")
			.Replace(" ", "")
			.PadRight(12, '0')
			.Substring(0, 12) + "0";
	}
}
