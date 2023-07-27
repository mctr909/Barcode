using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

class Barcode {
    const int BORDER_WEIGHT = 6;
    const int QUIET_SIZE = 15;

    readonly int[] CODE128 = {
        0x212222, 0x222122, 0x222221, 0x121223,
        0x121322, 0x131222, 0x122213, 0x122312,
        0x132212, 0x221213, 0x221312, 0x231212,
        0x112232, 0x122132, 0x122231, 0x113222,

        0x123122, 0x123221, 0x223211, 0x221132,
        0x221231, 0x213212, 0x223112, 0x312131,
        0x311222, 0x321122, 0x321221, 0x312212,
        0x322112, 0x322211, 0x212123, 0x212321,

        0x232121, 0x111323, 0x131123, 0x131321,
        0x112313, 0x132113, 0x132311, 0x211313,
        0x231113, 0x231311, 0x112133, 0x112331,
        0x132131, 0x113123, 0x113321, 0x133121,

        0x313121, 0x211331, 0x231131, 0x213113,
        0x213311, 0x213131, 0x311123, 0x311321,
        0x331121, 0x312113, 0x312311, 0x332111,
        0x314111, 0x221411, 0x431111, 0x111224,

        0x111422, 0x121124, 0x121421, 0x141122,
        0x141221, 0x112214, 0x112412, 0x122114,
        0x122411, 0x142112, 0x142211, 0x241211,
        0x221114, 0x413111, 0x241112, 0x134111,

        0x111242, 0x121142, 0x121241, 0x114212,
        0x124112, 0x124211, 0x411212, 0x421112,
        0x421211, 0x212141, 0x214121, 0x412121,
        0x111143, 0x111341, 0x131141, 0x114113,

        0x114311, 0x411113, 0x411311, 0x113141,
        0x114131, 0x311141, 0x411131, 0x211412,
        0x211214, 0x211232, 0x2331112
    };
    readonly List<string> CODE128_A = new List<string> {
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
    readonly List<string> CODE128_B = new List<string> {
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
    readonly List<string> CODE128_C = new List<string> {
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

    readonly Dictionary<string, int> CODE39 = new Dictionary<string, int> {
        { "0", 0b0001101000 },
        { "1", 0b1001000010 },
        { "2", 0b0011000010 },
        { "3", 0b1011000000 },
        { "4", 0b0001100010 },
        { "5", 0b1001100000 },
        { "6", 0b0011100000 },
        { "7", 0b0001001010 },
        { "8", 0b1001001000 },
        { "9", 0b0011001000 },

        { "A", 0b1000010010 },
        { "B", 0b0010010010 },
        { "C", 0b1010010000 },
        { "D", 0b0000110010 },
        { "E", 0b1000110000 },
        { "F", 0b0010110000 },
        { "G", 0b0000011010 },
        { "H", 0b1000011000 },
        { "I", 0b0010011000 },
        { "J", 0b0000111000 },
        { "K", 0b1000000110 },
        { "L", 0b0010000110 },
        { "M", 0b1010000100 },
        { "N", 0b0000100110 },
        { "O", 0b1000100100 },
        { "P", 0b0010100100 },
        { "Q", 0b0000001110 },
        { "R", 0b1000001100 },
        { "S", 0b0010001100 },
        { "T", 0b0000101100 },
        { "U", 0b1100000010 },
        { "V", 0b0110000010 },
        { "W", 0b1110000000 },
        { "X", 0b0100100010 },
        { "Y", 0b1100100000 },
        { "Z", 0b0110100000 },

        { "$", 0b0101010000 },
        { "/", 0b0101000100 },
        { "+", 0b0100010100 },
        { "-", 0b0100001010 },
        { "*", 0b0100101000 },
        { " ", 0b0110001000 },
        { "%", 0b0001010100 },
        { ".", 0b1100001000 }
    };

    readonly Dictionary<string, int> NW7 = new Dictionary<string, int> {
        { "0", 0b00000110 },
        { "1", 0b00001100 },
        { "2", 0b00010010 },
        { "3", 0b11000000 },
        { "4", 0b00100100 },
        { "5", 0b10000100 },
        { "6", 0b01000010 },
        { "7", 0b01001000 },
        { "8", 0b01100000 },
        { "9", 0b10010000 },
        { "-", 0b00011000 },
        { "$", 0b00110000 },

        { "A", 0b00110100 },
        { "B", 0b01010010 },
        { "C", 0b00010110 },
        { "D", 0b00011100 },
        { "+", 0b00101010 },
        { ":", 0b10001010 },
        { "/", 0b10100010 },
        { ".", 0b10101000 }
    };
    readonly char[] NW7_TERM = { 'A', 'B', 'C', 'D' };
    readonly char[] NW7_LONG = { 'A', 'B', 'C', 'D', '+', ':', '/', '.' };

    readonly int[] ITF = {
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

    readonly int[,] EAN_L = {
        { 0x32110, 0x11230 },
        { 0x22210, 0x12220 },
        { 0x21220, 0x22120 },
        { 0x14110, 0x11410 },
        { 0x11320, 0x23110 },
        { 0x12310, 0x13210 },
        { 0x11140, 0x41110 },
        { 0x13120, 0x21310 },
        { 0x12130, 0x31210 },
        { 0x31120, 0x21130 }
    };
    readonly int[] EAN_R = {
        0x13210,
        0x12220,
        0x12121,
        0x11410,
        0x11131,
        0x11230,
        0x11113,
        0x11311,
        0x11212,
        0x13111
    };
    readonly int[] EAN_P = {
        0b000000,
        0b001011,
        0b001101,
        0b001110,
        0b010011,
        0b011001,
        0b011100,
        0b010101,
        0b010110,
        0b011010
    };

    public enum Type {
        CODE128,
        CODE39,
        NW7_CODABAR,
        ITF,
        GTIN14,
        EAN_JAN
    }

    public Bitmap Bmp;
    public int Height {
        get { return mHeight; }
        set {
            mHeight = value;
            mBarHeight = value - 10;
        }
    }
    public float PosX = 0.0f;
    public float PosY = 0.0f;
    public float Pitch = 1.0f;
    public bool Border = false;

    Font mFont = new Font("MS Gothic", 9.0f);
    Graphics mG;
    int mHeight = 50;
    int mBarHeight = 40;

    public double GetWidth(string value, Type type) {
        var temp = TrimAndPad(value, type);
        double spaceWidth = Pitch * QUIET_SIZE * 2;
        switch (type) {
        case Type.CODE128: {
            int len;
            if (Regex.IsMatch(temp, "^[0-9]+$")) {
                len = temp.Length >> 1;
            } else {
                len = temp.Length;
            }
            return (len * (Pitch * 11) + Pitch * (11 + 11 + 13) + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        }
        case Type.CODE39:
            return (temp.Length * (Pitch * 7 + Pitch * 3 * 3) + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.NW7_CODABAR: {
            var longCount = 0.0;
            var shortCount = 0.0;
            for (int i = 0; i < temp.Length; i++) {
                if (0 <= (temp[i] + "").IndexOfAny(NW7_LONG)) {
                    longCount++;
                } else {
                    shortCount++;
                }
            }
            longCount *= Pitch * 5 + Pitch * 3 * 3;
            shortCount *= Pitch * 6 + Pitch * 3 * 2;
            return (longCount + shortCount + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        }
        case Type.ITF:
            return (temp.Length * (Pitch * 3 + Pitch * 3 * 2) + Pitch * 9 + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.GTIN14:
            return (14 * (Pitch * 3 + Pitch * 3 * 2) + Pitch * 9 + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.EAN_JAN:
            return (Pitch * (6 * 12 + 3 + 5 + 4) + Pitch * 10 + spaceWidth + (Border ? 2 : 0));
        default:
            return 0;
        }
    }

    public void CreateCanvas(int width, int height) {
        if (null != mG) {
            mG.Dispose();
            mG = null;
        }
        if (null != Bmp) {
            Bmp.Dispose();
            Bmp = null;
        }
        if (width < 1) {
            width = 1;
        }
        if (height < 1) {
            height = 1;
        }
        Bmp = new Bitmap(width, height);
        mG = Graphics.FromImage(Bmp);
        PosX = 0.0f;
        PosY = 0.0f;
    }

    public void Draw(string value, Type type) {
        var temp = TrimAndPad(value, type);
        var beginX = PosX;
        var beginY = PosY;
        switch (type) {
        case Type.CODE128:
            PosX += Border ? (BORDER_WEIGHT / 2) : 0;
            DrawCode128(temp);
            DrawBorder();
            break;
        case Type.CODE39:
            PosX += Border ? (BORDER_WEIGHT / 2) : 0;
            DrawCode39(temp);
            DrawBorder();
            break;
        case Type.NW7_CODABAR:
            PosX += Border ? (BORDER_WEIGHT / 2) : 0;
            DrawNW7(temp);
            DrawBorder();
            break;
        case Type.ITF:
            PosX += Border ? (BORDER_WEIGHT / 2) : 0;
            DrawITF(temp);
            DrawBorder();
            break;
        case Type.GTIN14:
            PosX += Border ? (BORDER_WEIGHT / 2) : 0;
            DrawGTIN14(temp);
            DrawBorder();
            break;
        case Type.EAN_JAN:
            DrawEAN(temp);
            break;
        }
        PosX = beginX;
        PosY = beginY;
    }

    string TrimAndPad(string value, Type type) {
        switch (type) {
        case Type.CODE128:
            if (Regex.IsMatch(value, "^[0-9]+$")) {
                if (1 == value.Length % 2) {
                    value = "0" + value;
                }
            }
            return value;
        case Type.CODE39:
            return "*" + value.Replace("\r", "").Replace("*", "").ToUpper() + "*";
        case Type.NW7_CODABAR: {
            var temp = value.Replace("\r", "").Replace(" ", "").ToUpper();
            if (temp.Substring(0, 1).IndexOfAny(NW7_TERM) < 0) {
                temp = "A" + temp;
            }
            if (temp.Substring(temp.Length - 1, 1).IndexOfAny(NW7_TERM) < 0) {
                temp += "A";
            }
            return temp;
        }
        case Type.ITF: {
            var temp = value.Replace("\r", "").Replace(" ", "");
            if (1 == temp.Length % 2) {
                temp += "0";
            }
            return temp;
        }
        case Type.GTIN14:
            return value.Replace("\r", "").Replace(" ", "").PadRight(13, '0').Substring(0, 13) + "0";
        case Type.EAN_JAN:
            return value.Replace("\r", "").Replace(" ", "").PadRight(12, '0').Substring(0, 12) + "0";
        default:
            return "";
        }
    }

    void DrawBar(double width, double ofsY = 0.0) {
        var x = (int)PosX;
        var y = (int)PosY;
        var w = (float)width;
        var h = (float)(mBarHeight + ofsY);
        var dx = PosX - x;
        var dw = width - (int)width;
        var gray = new Pen(Color.FromArgb(95, 0, 0, 0));
        if (0.0 < dx) {
            mG.FillRectangle(Brushes.Black, x + 1, y, w - 1, h);
            if (0.0 < dw) {
                mG.DrawLine(gray,
                    x + w + 1, y,
                    x + w + 1, y + h - 1
                );
            } else {
                mG.DrawLine(gray,
                    x + w, y,
                    x + w, y + h - 1
                );
            }
        } else {
            if (0.0 < dw) {
                mG.FillRectangle(Brushes.Black, x, y, w - 1, h);
                mG.DrawLine(gray,
                    x + w, y,
                    x + w, y + h - 1
                );
            } else {
                mG.FillRectangle(Brushes.Black, x, y, w, h);
            }
        }
    }

    void DrawBorder() {
        if (Border) {
            mG.DrawRectangle(new Pen(Brushes.Black, BORDER_WEIGHT),
                BORDER_WEIGHT / 2, PosY + BORDER_WEIGHT / 2,
                PosX - BORDER_WEIGHT / 2, mBarHeight - BORDER_WEIGHT
            );
        }
    }

    void DrawCode128(string value) {
        var spaceWidth = Pitch * QUIET_SIZE;

        int sum;
        int val;
        int symbol;

        /* 数字/文字のタイプを決定 */
        List<string> table;
        int readLen;
        if (Regex.IsMatch(value, "^[0-9]+$")) {
            table = CODE128_C;
            val = table.IndexOf("START_C");
            readLen = 2;
        } else {
            table = CODE128_B;
            val = table.IndexOf("START_B");
            readLen = 1;
        }

        /* 開始コード描画 */
        PosX += spaceWidth;
        sum = val;
        symbol = CODE128[val];
        for (int s = 20; 0 <= s; s -= 4) {
            var barWidth = Pitch * ((symbol >> s) & 0xF);
            if (4 == s % 8) {
                DrawBar(barWidth);
            }
            PosX += barWidth;
        }

        /* データ */
        for (int i = 0, weight = 1; i < value.Length; i += readLen, weight++) {
            var chr = value.Substring(i, readLen);
            if (CODE128_B == table && !table.Contains(chr)) {
                chr = " ";
            }
            /* テキスト描画 */
            mG.DrawString(chr, mFont, Brushes.Black,
                PosX, PosY + mBarHeight
            );
            /* シンボル描画 */
            val = table.IndexOf(chr);
            sum += val * weight;
            symbol = CODE128[val];
            for (int s = 20; 0 <= s; s -= 4) {
                var barWidth = Pitch * ((symbol >> s) & 0xF);
                if (4 == s % 8) {
                    DrawBar(barWidth);
                }
                PosX += barWidth;
            }
        }

        /* チェックディジット描画 */
        symbol = CODE128[sum % 103];
        for (int s = 20; 0 <= s; s -= 4) {
            var barWidth = Pitch * ((symbol >> s) & 0xF);
            if (4 == s % 8) {
                DrawBar(barWidth);
            }
            PosX += barWidth;
        }

        /* 終了コード描画 */
        symbol = CODE128[table.IndexOf("STOP")];
        for (int s = 24; 0 <= s; s -= 4) {
            var barWidth = Pitch * ((symbol >> s) & 0xF);
            if (0 == s % 8) {
                DrawBar(barWidth);
            }
            PosX += barWidth;
        }
        PosX += spaceWidth;
    }

    void DrawCode39(string value) {
        var narrow = Pitch;
        var wide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        /* 開始 */
        PosX += spaceWidth;

        /* データ */
        for (int i = 0; i < value.Length; i++) {
            var chr = value.Substring(i, 1);
            if (!CODE39.ContainsKey(chr)) {
                chr = " ";
            }
            /* テキスト描画 */
            mG.DrawString(chr, mFont, Brushes.Black,
                PosX, PosY + mBarHeight
            );
            /* シンボル描画 */
            var symbol = CODE39[chr];
            for (int s = 9; 0 <= s; s--) {
                float barWidth;
                if (1 == ((symbol >> s) & 1)) {
                    barWidth = wide;
                } else {
                    barWidth = narrow;
                }
                if (1 == s % 2) {
                    DrawBar(barWidth);
                }
                PosX += barWidth;
            }
        }

        /* 終了 */
        PosX += spaceWidth;
    }

    void DrawNW7(string value) {
        var narrow = Pitch;
        var wide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        /* 開始 */
        PosX += spaceWidth;

        /* データ */
        for (int i = 0; i < value.Length; i++) {
            var chr = value.Substring(i, 1);
            if (!NW7.ContainsKey(chr)) {
                chr = "-";
            }
            if (1 <= i && i < value.Length - 1 && 0 <= chr.IndexOfAny(NW7_TERM)) {
                chr = "-";
            }
            /* テキスト描画 */
            mG.DrawString(chr, mFont, Brushes.Black,
                PosX, PosY + mBarHeight
            );
            /* シンボル描画 */
            var symbol = NW7[chr];
            for (int s = 7; 0 <= s; s--) {
                float barWidth;
                if (1 == ((symbol >> s) & 1)) {
                    barWidth = wide;
                } else {
                    barWidth = narrow;
                }
                if (1 == s % 2) {
                    DrawBar(barWidth);
                }
                PosX += barWidth;
            }
        }

        /* 終了 */
        PosX += spaceWidth;
    }

    void DrawITF(string value) {
        var narrow = Pitch;
        var wide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        /* 開始コード描画 */
        PosX += spaceWidth;
        DrawBar(narrow);
        PosX += narrow * 2;
        DrawBar(narrow);
        PosX += narrow * 2;

        /* データ */
        for (int i = 0; i < value.Length; i += 2) {
            var chr1 = value.Substring(i, 1);
            var chr2 = value.Substring(i + 1, 1);
            int val1;
            int val2;
            if (!int.TryParse(chr1, out val1)) {
                chr1 = "0";
            }
            if (!int.TryParse(chr2, out val2)) {
                chr2 = "0";
            }
            /* テキスト描画 */
            mG.DrawString(chr1 + chr2, mFont, Brushes.Black,
                PosX, PosY + mBarHeight
            );
            /* シンボル描画 */
            var symbol1 = ITF[val1];
            var symbol2 = ITF[val2];
            for (int s = 4; 0 <= s; s--) {
                if (1 == ((symbol1 >> s) & 1)) {
                    DrawBar(wide);
                    PosX += wide;
                } else {
                    DrawBar(narrow);
                    PosX += narrow;
                }
                if (1 == ((symbol2 >> s) & 1)) {
                    PosX += wide;
                } else {
                    PosX += narrow;
                }
            }
        }

        /* 終了コード描画 */
        DrawBar(wide);
        PosX += wide + narrow;
        DrawBar(narrow);
        PosX += narrow;
        PosX += spaceWidth;
    }

    void DrawGTIN14(string value) {
        var narrow = Pitch;
        var wide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        /* 開始コード描画 */
        PosX += spaceWidth;
        DrawBar(narrow);
        PosX += narrow * 2;
        DrawBar(narrow);
        PosX += narrow * 2;

        /* データ */
        var sum = 0;
        var str = "";
        for (int i = 0; i < 14; i += 2) {
            var chr1 = value.Substring(i, 1);
            var chr2 = value.Substring(i + 1, 1);
            int val1;
            int val2;
            if (!int.TryParse(chr1, out val1)) {
                chr1 = "0";
            }
            if (!int.TryParse(chr2, out val2)) {
                chr2 = "0";
            }
            sum += val1 * 3 + val2;
            var symbol1 = ITF[val1];
            var symbol2 = ITF[val2];
            if (12 == i) {
                /* チェックディジット */
                val2 = sum % 10;
                val2 = (10 - val2) % 10;
                chr2 = val2.ToString();
                symbol2 = ITF[val2];
            }
            str += chr1 + chr2;
            /* シンボル描画 */
            for (int s = 4; 0 <= s; s--) {
                if (1 == ((symbol1 >> s) & 1)) {
                    DrawBar(wide);
                    PosX += wide;
                } else {
                    DrawBar(narrow);
                    PosX += narrow;
                }
                if (1 == ((symbol2 >> s) & 1)) {
                    PosX += wide;
                } else {
                    PosX += narrow;
                }
            }
        }

        /* 終了コード描画 */
        DrawBar(wide);
        PosX += wide + narrow;
        DrawBar(narrow);
        PosX += narrow;
        PosX += spaceWidth;

        /* テキスト描画 */
        str = string.Format("{0} {1} {2} {3}",
            str.Substring(0, 3),
            str.Substring(3, 5),
            str.Substring(8, 5),
            str.Substring(13, 1)
        );
        var w = mG.MeasureString(str, mFont).Width;
        mG.DrawString(str, mFont, Brushes.Black,
            (PosX - w) / 2.0f, PosY + mBarHeight
        );
    }

    void DrawEAN(string value) {
        var ofsY = 3;
        var notchHeight = 6;
        var spaceWidth = Pitch * QUIET_SIZE;

        PosY += ofsY;

        /* 開始コード描画 */
        PosX += Border ? 1 : 0;
        PosX += spaceWidth;
        DrawBar(Pitch);
        PosX += Pitch * 2;
        DrawBar(Pitch);
        PosX += Pitch;

        /* データ */
        var sum = 0;
        var parity = 0;
        for (int i = 0; i < value.Length; i++) {
            var chr = value.Substring(i, 1);
            int val;
            if (!int.TryParse(chr, out val)) {
                chr = "0";
            }
            sum += val * (0 == i % 2 ? 1 : 3);
            int symbol;
            if (0 == i) {
                /* パリティ指定桁 */
                parity = EAN_P[val];
                mG.DrawString(chr, mFont, Brushes.Black,
                    PosX - Pitch * 12, PosY + mBarHeight - notchHeight
                );
                /* 次の桁へ */
                continue;
            } else if (i < 7) {
                /* 左側シンボル */
                symbol = EAN_L[val, (parity >> (6 - i)) & 1];
            } else if (7 == i) {
                /* センターバー描画 */
                PosX += Pitch;
                DrawBar(Pitch);
                PosX += Pitch * 2;
                DrawBar(Pitch);
                PosX += Pitch;
                /* 右側シンボル */
                symbol = EAN_R[val];
            } else if (i < 12) {
                /* 右側シンボル */
                symbol = EAN_R[val];
            } else {
                /* チェックディジット */
                val = sum % 10;
                val = (10 - val) % 10;
                chr = val.ToString();
                /* 右側シンボル */
                symbol = EAN_R[val];
            }
            /* テキスト描画 */
            mG.DrawString(chr, mFont, Brushes.Black,
                PosX, PosY + mBarHeight - notchHeight
            );
            /* シンボル描画 */
            for (int s = 16; 0 <= s; s -= 4) {
                var barWidth = Pitch * ((symbol >> s) & 0xF);
                if (4 == s % 8) {
                    DrawBar(barWidth, -notchHeight);
                }
                PosX += barWidth;
            }
        }

        /* 終了コード描画 */
        PosX += Pitch;
        DrawBar(Pitch);
        PosX += Pitch * 2;
        DrawBar(Pitch);
        PosX += spaceWidth;

        if (Border) {
            mG.DrawRectangle(Pens.Black, 0, PosY - ofsY, PosX, Height - 1);
        }
    }
}
