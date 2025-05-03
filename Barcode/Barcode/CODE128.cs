using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

class CODE128 : BaseCode {
	static readonly int[] CODE = {
		0x222212, 0x221222, 0x122222, 0x322121,
		0x223121, 0x222131, 0x312221, 0x213221,
		0x212231, 0x312122, 0x213122, 0x212132,
		0x232211, 0x231221, 0x132221, 0x222311,

		0x221321, 0x122321, 0x112322, 0x231122,
		0x132122, 0x212312, 0x211322, 0x131213,
		0x222113, 0x221123, 0x122123, 0x212213,
		0x211223, 0x112223, 0x321212, 0x123212,

		0x121232, 0x323111, 0x321131, 0x123131,
		0x313211, 0x311231, 0x113231, 0x313112,
		0x311132, 0x113132, 0x331211, 0x133211,
		0x131231, 0x321311, 0x123311, 0x121331,

		0x121313, 0x133112, 0x131132, 0x311312,
		0x113312, 0x131312, 0x321113, 0x123113,
		0x121133, 0x311213, 0x113213, 0x111233,
		0x111413, 0x114122, 0x111134, 0x422111,

		0x224111, 0x421121, 0x124121, 0x221141,
		0x122141, 0x412211, 0x214211, 0x411221,
		0x114221, 0x211241, 0x112241, 0x112142,
		0x411122, 0x111314, 0x211142, 0x111431,

		0x242111, 0x241121, 0x142121, 0x212411,
		0x211421, 0x112421, 0x212114, 0x211124,
		0x112124, 0x141212, 0x121412, 0x121214,
		0x341111, 0x143111, 0x141131, 0x311411,

		0x113411, 0x311114, 0x113114, 0x141311,
		0x131411, 0x141113, 0x131114, 0x214112,
		0x412112, 0x232112, 0x2111332
	};

	protected static readonly List<string> TABLE_A = new List<string> {
		" ", "!", "\"","#",
		"$", "%", "&", "'",
		"(", ")", "*", "+",
		",", "-", ".", "/",

		"0", "1", "2", "3",
		"4", "5", "6", "7",
		"8", "9", ":", ";",
		"<", "=", ">", "?",

		"@", "A", "B", "C",
		"D", "E", "F", "G",
		"H", "I", "J", "K",
		"L", "M", "N", "O",

		"P", "Q", "R", "S",
		"T", "U", "V", "W",
		"X", "Y", "Z", "[",
		"\\","]", "^", "_",

		"\0", "SOH","STX","ETX",
		"EOT","ENQ","ACK","\a",
		"\b", "\t", "\n", "VT",
		"FF", "\r", "SO", "SI",

		"DLE","DC1","DC2","DC3",
		"DC4","NAK","SYN","ETB",
		"CAN","EM", "SUB","ESC",
		"FS", "GS", "RS", "US",

		"FNC3", "FNC2", "SHIFT","CODE_C",
		"CODE_B","FNC4","FNC1", "START_A",
		"START_B", "START_C", "STOP"
	};

	protected static readonly List<string> TABLE_B = new List<string> {
		" ", "!", "\"","#",
		"$", "%", "&", "'",
		"(", ")", "*", "+",
		",", "-", ".", "/",

		"0", "1", "2", "3",
		"4", "5", "6", "7",
		"8", "9", ":", ";",
		"<", "=", ">", "?",

		"@", "A", "B", "C",
		"D", "E", "F", "G",
		"H", "I", "J", "K",
		"L", "M", "N", "O",

		"P", "Q", "R", "S",
		"T", "U", "V", "W",
		"X", "Y", "Z", "[",
		"\\","]", "^", "_",

		"`", "a", "b", "c",
		"d", "e", "f", "g",
		"h", "i", "j", "k",
		"l", "m", "n", "o",

		"p", "q", "r", "s",
		"t", "u", "v", "w",
		"x", "y", "z", "{",
		"|", "}", "~", "DEL",

		"FNC3", "FNC2", "SHIFT", "CODE_C",
		"FNC4", "CODE_A", "FNC1", "START_A",
		"START_B", "START_C", "STOP"
	};

	protected static readonly List<string> TABLE_C = new List<string> {
		"00", "01", "02", "03",
		"04", "05", "06", "07",
		"08", "09", "10", "11",
		"12", "13", "14", "15",

		"16", "17", "18", "19",
		"20", "21", "22", "23",
		"24", "25", "26", "27",
		"28", "29", "30", "31",

		"32", "33", "34", "35",
		"36", "37", "38", "39",
		"40", "41", "42", "43",
		"44", "45", "46", "47",

		"48", "49", "50", "51",
		"52", "53", "54", "55",
		"56", "57", "58", "59",
		"60", "61", "62", "63",

		"64", "65", "66", "67",
		"68", "69", "70", "71",
		"72", "73", "74", "75",
		"76", "77", "78", "79",

		"80", "81", "82", "83",
		"84", "85", "86", "87",
		"88", "89", "90", "91",
		"92", "93", "94", "95",

		"96", "97", "98", "99",
		"CODE_B", "CODE_A", "FNC1", "START_A",
		"START_B", "START_C", "STOP"
	};

	const int QUIET_SIZE = 15;

	public override float Width {
		get {
			/* 数字用途/文字用途を決定 */
			int len;
			if (Regex.IsMatch(mValue, "^[0-9]+$")) {
				len = mValue.Length >> 1;
			} else {
				len = mValue.Length;
			}
			var spaceWidth = (Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0)) * 2;
			return len * Pitch * 11 + Pitch * 35 + spaceWidth;
		}
	}

	public override void Draw(Graphics g) {
		var spaceWidth = Pitch * QUIET_SIZE + (Border ? BORDER_WEIGHT : 0);

		int val;

		/* 数字用途/文字用途を決定 */
		List<string> table;
		int readLen;
		if (Regex.IsMatch(mValue, "^[0-9]+$")) {
			table = TABLE_C;
			val = table.IndexOf("START_C");
			readLen = 2;
		} else {
			table = TABLE_B;
			val = table.IndexOf("START_B");
			readLen = 1;
		}

		var sum = val;
		mCur = X;

		/* 開始コード描画 */
		mCur += spaceWidth;
		DrawCode(g, val);

		for (int i = 0, weight = 1; i < mValue.Length; i += readLen, weight++) {
			var chr = mValue.Substring(i, readLen);
			if (TABLE_B == table && !table.Contains(chr)) {
				chr = " ";
			}
			val = table.IndexOf(chr);
			sum += val * weight;

			if (ShowValue) {
				/* テキスト描画 */
				g.DrawString(chr, FONT, Brushes.Black, mCur, Bottom);
			}

			/* コード描画 */
			DrawCode(g, val);
		}

		/* チェックディジット描画 */
		DrawCode(g, sum % 103);

		/* 終了コード描画 */
		DrawCode(g, table.IndexOf("STOP"));
		mCur += spaceWidth;

		/* 枠の描画 */
		DrawBorder(g);
	}

	protected override string TrimAndPad(string value) {
		if (!Regex.IsMatch(value, "^[0-9]+$")) {
			return value;
		}
		var ret = value.Replace("\r", "").Replace(" ", "");
		if (1 == ret.Length % 2) {
			ret += "0";
		}
		return ret;
	}

	protected void DrawCode(Graphics g, int val) {
		for (int code = CODE[val], m = 0; code != 0; code >>= 4, m ^= 1) {
			var width = (code & 0xF) * Pitch;
			if (0 == m) {
				DrawBar(g, width);
			}
			mCur += width;
		}
	}
}
