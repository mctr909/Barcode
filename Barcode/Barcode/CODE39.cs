using System.Collections.Generic;
using System.Linq;

class CODE39 : BaseCode {
	private const int QUIET_SIZE = 15;
	private const int WIDE_SIZE = 3;

	private static readonly Dictionary<char, int> CODE = new Dictionary<char, int> {
		{ '0', 0b0001101000 },
		{ '1', 0b1001000010 },
		{ '2', 0b0011000010 },
		{ '3', 0b1011000000 },
		{ '4', 0b0001100010 },
		{ '5', 0b1001100000 },
		{ '6', 0b0011100000 },
		{ '7', 0b0001001010 },
		{ '8', 0b1001001000 },
		{ '9', 0b0011001000 },
		{ 'A', 0b1000010010 },
		{ 'B', 0b0010010010 },
		{ 'C', 0b1010010000 },
		{ 'D', 0b0000110010 },
		{ 'E', 0b1000110000 },
		{ 'F', 0b0010110000 },
		{ 'G', 0b0000011010 },
		{ 'H', 0b1000011000 },
		{ 'I', 0b0010011000 },
		{ 'J', 0b0000111000 },
		{ 'K', 0b1000000110 },
		{ 'L', 0b0010000110 },
		{ 'M', 0b1010000100 },
		{ 'N', 0b0000100110 },
		{ 'O', 0b1000100100 },
		{ 'P', 0b0010100100 },
		{ 'Q', 0b0000001110 },
		{ 'R', 0b1000001100 },
		{ 'S', 0b0010001100 },
		{ 'T', 0b0000101100 },
		{ 'U', 0b1100000010 },
		{ 'V', 0b0110000010 },
		{ 'W', 0b1110000000 },
		{ 'X', 0b0100100010 },
		{ 'Y', 0b1100100000 },
		{ 'Z', 0b0110100000 },
		{ '$', 0b0101010000 },
		{ '/', 0b0101000100 },
		{ '+', 0b0100010100 },
		{ '-', 0b0100001010 },
		{ '*', 0b0100101000 },
		{ ' ', 0b0110001000 },
		{ '%', 0b0001010100 },
		{ '.', 0b1100001000 }
	};

	public override float Width {
		get {
			var codeWidth = Pitch * Value.Length * (3 * WIDE_SIZE + 7);
			var spaceWidth = Pitch * QUIET_SIZE + BorderWidth;
			return codeWidth + spaceWidth * 2;
		}
	}

	public override void Draw() {
		var spaceWidth = Pitch * QUIET_SIZE + BorderWidth;
		var wide = Pitch * WIDE_SIZE;

		/* 開始 */
		Cur = Left;
		Cur += spaceWidth;

		for (int pos = 0; pos < Value.Length; pos++) {
			var chr = Value.ElementAt(pos);
			if (!CODE.ContainsKey(chr)) {
				chr = ' ';
			}

			if (EnablePrintValue) {
				/* テキスト描画 */
				DrawString($"{chr}", Cur, Bottom);
			}

			/* コード描画 */
			var code = CODE[chr];
			for (int mask = 0b1000000000, m = 0; mask != 0; mask >>= 1, m ^= 1) {
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
		var ret = value.Replace("\r", "").ToUpper();
		if ('*' != ret.ElementAt(0)) {
			ret = "*" + ret;
		}
		if ('*' != ret.ElementAt(ret.Length - 1)) {
			ret += "*";
		}
		return ret;
	}
}
