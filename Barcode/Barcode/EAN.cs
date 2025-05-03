using System.Linq;

class EAN : BaseCode {
	private const int QUIET_SIZE = 15;

	private static readonly int[,] CODE_L = {
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

	private static readonly int[] CODE_R = {
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

	private static readonly int[] PARITY = {
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

	public override float Width {
		get {
			var codeWidth = Pitch * 94;
			var spaceWidth = Pitch * QUIET_SIZE;
			return codeWidth + spaceWidth * 2;
		}
	}

	public override void Draw() {
		var quietSize = QUIET_SIZE * Pitch;
		var notchHeight = MeasureString("0").Height - 3;

		/* 開始コード描画 */
		Cur = Left;
		Cur += quietSize;
		DrawBar(Pitch);
		Cur += Pitch * 2;
		DrawBar(Pitch);
		Cur += Pitch;

		var sum = 0;
		var parity = 0;
		for (int pos = 0; pos < Value.Length; pos++) {
			var index = Value.ElementAt(pos) - '0';
			if (index < 0 || index > 9) {
				index = 0;
			}
			sum += index * (0 == pos % 2 ? 1 : 3);

			switch (pos) {
			case 0:
				/* パリティ指定桁 */
				parity = PARITY[index];
				DrawString($"{index}", Cur - Pitch * 11, Bottom - notchHeight);
				/* 次の桁へ */
				continue;
			case 7:
				/* センターバー描画 */
				Cur += Pitch;
				DrawBar(Pitch);
				Cur += Pitch * 2;
				DrawBar(Pitch);
				Cur += Pitch;
				break;
			case 12:
				/* チェックディジット */
				index = (10 - sum % 10) % 10;
				break;
			}

			/* テキスト描画 */
			DrawString($"{index}", Cur, Bottom - notchHeight);

			/* コード描画 */
			var code = pos < 7 ? CODE_L[index, (parity >> pos) & 1] : CODE_R[index];
			for (int m = 1; code != 0; code >>= 4, m ^= 1) {
				var width = (code & 0xF) * Pitch;
				if (0 == m) {
					DrawBar(width, -notchHeight);
				}
				Cur += width;
			}
		}

		/* 終了コード描画 */
		Cur += Pitch;
		DrawBar(Pitch);
		Cur += Pitch * 2;
		DrawBar(Pitch);
		Cur += quietSize;
	}

	protected override string TrimAndPad(string value) {
		return value.Replace("\r", "")
			.Replace(" ", "")
			.PadRight(12, '0')
			.Substring(0, 12) + "0";
	}
}
