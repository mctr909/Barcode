using System.Linq;

class GTIN14 : ITF {
	public override float Width {
		get {
			var codeWidth = Pitch * 14 * (3 + 3*2);
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

		var sum = 0;
		var str = "";
		for (int pos = 0; pos < 14; pos += 2) {
			var index1 = Value.ElementAt(pos) - '0';
			var index2 = Value.ElementAt(pos + 1) - '0';
			if (index1 < 0 || index1 > 9) {
				index1 = 0;
			}
			if (index2 < 0 || index2 > 9) {
				index2 = 0;
			}
			sum += index1 * 3 + index2;

			if (12 == pos) {
				/* チェックディジット */
				index2 = (10 - sum % 10) % 10;
			}
			str += $"{index1}{index2}";

			/* コード描画 */
			DrawCode(index1, index2);
		}

		/* 終了コード描画 */
		DrawBar(Pitch * 3);
		Cur += Pitch * 4;
		DrawBar(Pitch);
		Cur += Pitch;
		Cur += spaceWidth;

		/* テキスト描画 */
		str = string.Format("{0} {1} {2} {3}",
			str.Substring(0, 1),
			str.Substring(1, 7),
			str.Substring(8, 5),
			str.Substring(13, 1)
		);
		var w = MeasureString(str).Width;
		DrawString(str, (Cur - w) / 2.0f, Bottom);

		/* 枠の描画 */
		DrawBoader();
	}

	protected override string TrimAndPad(string value) {
		return value.Replace("\r", "")
			.Replace(" ", "")
			.PadRight(13, '0')
			.Substring(0, 13) + "0";
	}
}
