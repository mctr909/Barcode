using System.Collections.Generic;
using System.Drawing;

class Barcode {
    const int BORDER_WEIGHT = 6;
    const int QUIET_SIZE = 15;

    int[] ITF = {
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

    int[,] EAN = {
        { 0b001101, 0b100111, 0b111001, 0b000000 },
        { 0b011001, 0b110011, 0b110011, 0b001011 },
        { 0b010011, 0b011011, 0b110110, 0b001101 },
        { 0b111101, 0b100001, 0b100001, 0b001110 },
        { 0b100011, 0b011101, 0b101110, 0b010011 },
        { 0b110001, 0b111001, 0b100111, 0b011001 },
        { 0b101111, 0b000101, 0b101000, 0b011100 },
        { 0b111011, 0b010001, 0b100010, 0b010101 },
        { 0b110111, 0b001001, 0b100100, 0b010110 },
        { 0b001011, 0b010111, 0b111010, 0b011010 }
    };

    readonly Dictionary<string, int> NW7 = new Dictionary<string, int> {
        { "0", 0b0000011 },
        { "1", 0b0000110 },
        { "2", 0b0001001 },
        { "3", 0b1100000 },
        { "4", 0b0010010 },
        { "5", 0b1000010 },
        { "6", 0b0100001 },
        { "7", 0b0100100 },
        { "8", 0b0110000 },
        { "9", 0b1001000 },

        { "-", 0b0001100 },
        { "$", 0b0011000 },
        { ":", 0b1000101 },
        { "/", 0b1010001 },
        { ".", 0b1010100 },
        { "+", 0b0010101 },

        { "A", 0b0011010 },
        { "B", 0b0101001 },
        { "C", 0b0001011 },
        { "D", 0b0001110 }
    };
    char[] NW7_TERM = { 'A', 'B', 'C', 'D' };

    readonly Dictionary<string, int> CODE39 = new Dictionary<string, int> {
        { "0", 0b000110100 },
        { "1", 0b100100001 },
        { "2", 0b001100001 },
        { "3", 0b101100000 },
        { "4", 0b000110001 },
        { "5", 0b100110000 },
        { "6", 0b001110000 },
        { "7", 0b000100101 },
        { "8", 0b100100100 },
        { "9", 0b001100100 },

        { "A", 0b100001001 },
        { "B", 0b001001001 },
        { "C", 0b101001000 },
        { "D", 0b000011001 },
        { "E", 0b100011000 },
        { "F", 0b001011000 },
        { "G", 0b000001101 },
        { "H", 0b100001100 },
        { "I", 0b001001100 },
        { "J", 0b000011100 },
        { "K", 0b100000011 },
        { "L", 0b001000011 },
        { "M", 0b101000010 },
        { "N", 0b000010011 },
        { "O", 0b100010010 },
        { "P", 0b001010010 },
        { "Q", 0b000000111 },
        { "R", 0b100000110 },
        { "S", 0b001000110 },
        { "T", 0b000010110 },
        { "U", 0b110000001 },
        { "V", 0b011000001 },
        { "W", 0b111000000 },
        { "X", 0b010010001 },
        { "Y", 0b110010000 },
        { "Z", 0b011010000 },

        { "$", 0b010101000 },
        { "/", 0b010100010 },
        { "+", 0b010001010 },
        { "-", 0b010000101 },
        { "*", 0b010010100 },
        { " ", 0b011000100 },
        { "%", 0b000101010 },
        { ".", 0b110000100 }
    };

    int[] CODE128 = {
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

    public enum Type {
        CODE128,
        CODE39,
        NW7_CODABAR,
        ITF,
        GTIN14,
        EAN_JAN
    }

    public Bitmap Bmp;
    public int CodeHeight = 40;
    public float PosX = 0.0f;
    public float PosY = 0.0f;
    public float Pitch = 1.0f;
    public bool Border = false;

    Font mFont = new Font("MS Gothic", 9.0f);
    Graphics mG;

    public double GetWidth(string value, Type type) {
        var temp = TrimAndPad(value, type);
        double spaceWidth = Pitch * QUIET_SIZE * 2;
        switch (type) {
        case Type.CODE128:
            return (temp.Length * (Pitch * 11) + Pitch * (11 + 11 + 13) + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.CODE39:
            return (temp.Length * (Pitch * 7 + Pitch * 3 * 3) + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.NW7_CODABAR:
            return (temp.Length * (Pitch * 5 + Pitch * 3 * 3) + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.ITF:
            return (temp.Length * (Pitch * 3 + Pitch * 3 * 2) + Pitch * 9 + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.GTIN14:
            return (14 * (Pitch * 3 + Pitch * 3 * 2) + Pitch * 9 + spaceWidth + (Border ? BORDER_WEIGHT : 0));
        case Type.EAN_JAN:
            return (Pitch * (6 * 12 + 3 + 5 + 3) + Pitch * 10 + spaceWidth + (Border ? 2 : 0));
        default:
            return 0;
        }
    }

    public void CreateCanvas(int width, int height) {
        if (width < 1 || height < 1) {
            return;
        }
        if (null != mG) {
            mG.Dispose();
            mG = null;
        }
        if (null != Bmp) {
            Bmp.Dispose();
            Bmp = null;
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
            DrawCode128(temp);
            break;
        case Type.CODE39:
            DrawCode39(temp);
            break;
        case Type.NW7_CODABAR:
            DrawNW7(temp);
            break;
        case Type.ITF:
            DrawITF(temp);
            break;
        case Type.GTIN14:
            DrawITF(temp, true);
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
            return value;
        case Type.CODE39:
            return "*" + value.Replace("\r", "").Replace("*", "").ToUpper() + "*";
        case Type.NW7_CODABAR: {
            var temp = value.Replace("\r", "").ToUpper();
            if (temp.Substring(0, 1).IndexOfAny(NW7_TERM) < 0) {
                temp = "A" + temp;
            }
            if (temp.Substring(temp.Length - 1, 1).IndexOfAny(NW7_TERM) < 0) {
                temp += "A";
            }
            return temp;
        }
        case Type.ITF: {
            var temp = value.Replace("\r", "");
            if (1 == temp.Length % 2) {
                temp += "0";
            }
            return temp;
        }
        case Type.GTIN14:
            return value.Replace("\r", "").PadRight(13, '0').Substring(0, 13) + "0";
        case Type.EAN_JAN:
            return value.Replace("\r", "").PadRight(12, '0').Substring(0, 12) + "0";
        default:
            return "";
        }
    }

    void DrawBar(double width, double ofsY = 0.0) {
        var x = (int)PosX;
        var y = (int)PosY;
        var w = (float)width;
        var h = (float)(CodeHeight + ofsY);
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

    void DrawBorder(double width, double height) {
        mG.DrawRectangle(new Pen(Brushes.Black, BORDER_WEIGHT),
            BORDER_WEIGHT / 2, (float)height,
            (float)width - BORDER_WEIGHT / 2, CodeHeight
        );
    }

    void DrawCode128(string value) {
        var codePitch = Pitch;
        var spaceWidth = Pitch * QUIET_SIZE;

        PosX += Border ? (BORDER_WEIGHT / 2) : 0;

        /* begin of code */
        var sum = 0;
        PosX += spaceWidth;
        var val = CODE128_B.IndexOf("START_B");
        sum += val;
        var start = CODE128[val];
        for (int j = 5; 0 <= j; j--) {
            var codeWidth = codePitch * ((start >> (j * 4)) & 0xF);
            if (1 == j % 2) {
                DrawBar(codeWidth);
            }
            PosX += codeWidth;
        }

        /* draw data */
        var table = CODE128_B;
        var readLen = 1;
        for (int i = 0; 1 <= value.Length; i++) {
            var chr = value.Substring(0, readLen);
            if (!table.Contains(chr)) {
                chr = " ";
            }
            val = table.IndexOf(chr);
            sum += val * (i + 1);
            var code = CODE128[val];
            for (int j = 5; 0 <= j; j--) {
                if (5 == j) {
                    mG.DrawString(chr, mFont, Brushes.Black,
                        PosX, PosY + CodeHeight + (Border ? (BORDER_WEIGHT / 2) : 0)
                    );
                }
                var codeWidth = codePitch * ((code >> (j * 4)) & 0xF);
                if (1 == j % 2) {
                    DrawBar(codeWidth);
                }
                PosX += codeWidth;
            }
            value = value.Substring(readLen, value.Length - readLen);
        }

        /* end of code */
        sum = sum % 103;
        var check = CODE128[sum];
        for (int j = 5; 0 <= j; j--) {
            var codeWidth = codePitch * ((check >> (j * 4)) & 0xF);
            if (1 == j % 2) {
                DrawBar(codeWidth);
            }
            PosX += codeWidth;
        }
        val = CODE128[CODE128_B.IndexOf("STOP")];
        for (int j = 6; 0 <= j; j--) {
            var codeWidth = codePitch * ((val >> (j * 4)) & 0xF);
            if (0 == j % 2) {
                DrawBar(codeWidth);
            }
            PosX += codeWidth;
        }
        PosX += spaceWidth;

        /* draw border */
        if (Border) {
            DrawBorder(PosX, PosY);
        }
    }

    void DrawCode39(string value) {
        var codeNarrow = Pitch;
        var codeWide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        PosX += Border ? (BORDER_WEIGHT / 2) : 0;

        /* begin of code */
        PosX += spaceWidth;

        /* draw data */
        for (int i = 0; i < value.Length; i++) {
            var chr = value.Substring(i, 1);
            if (!CODE39.ContainsKey(chr)) {
                chr = " ";
            }
            var code = CODE39[chr];
            for (int j = 8; 0 <= j; j--) {
                if (8 == j) {
                    mG.DrawString(chr, mFont, Brushes.Black,
                        PosX, PosY + CodeHeight + (Border ? (BORDER_WEIGHT / 2) : 0)
                    );
                }
                float codeWidth;
                if (1 == ((code >> j) & 1)) {
                    codeWidth = codeWide;
                } else {
                    codeWidth = codeNarrow;
                }
                if (0 == j % 2) {
                    DrawBar(codeWidth);
                }
                PosX += codeWidth;
            }
            PosX += codeNarrow;
        }

        /* end of code */
        PosX += spaceWidth;

        /* draw border */
        if (Border) {
            DrawBorder(PosX, PosY);
        }
    }

    void DrawNW7(string value) {
        var codeNarrow = Pitch;
        var codeWide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        PosX += Border ? (BORDER_WEIGHT / 2) : 0;

        /* begin of code */
        PosX += spaceWidth;

        /* draw data */
        for (int i = 0; i < value.Length; i++) {
            var chr = value.Substring(i, 1);
            if (!NW7.ContainsKey(chr)) {
                chr = "-";
            }
            if (1 <= i && i < value.Length - 1 && 0 <= chr.IndexOfAny(NW7_TERM)) {
                chr = "-";
            }
            var code = NW7[chr];
            for (int j = 6; 0 <= j; j--) {
                if (6 == j) {
                    mG.DrawString(chr, mFont, Brushes.Black,
                        PosX, PosY + CodeHeight + (Border ? (BORDER_WEIGHT / 2) : 0)
                    );
                }
                float codeWidth;
                if (1 == ((code >> j) & 1)) {
                    codeWidth = codeWide;
                } else {
                    codeWidth = codeNarrow;
                }
                if (0 == j % 2) {
                    DrawBar(codeWidth);
                }
                PosX += codeWidth;
            }
            PosX += codeNarrow;
        }

        /* end of code */
        PosX += spaceWidth;

        /* draw border */
        if (Border) {
            DrawBorder(PosX, PosY);
        }
    }

    void DrawITF(string value, bool gtin14 = false) {
        var codeNarrow = Pitch;
        var codeWide = Pitch * 3;
        var spaceWidth = Pitch * QUIET_SIZE;

        PosX += Border ? (BORDER_WEIGHT / 2) : 0;

        /* begin of code */
        PosX += spaceWidth;
        DrawBar(codeNarrow);
        PosX += codeNarrow * 2;
        DrawBar(codeNarrow);
        PosX += codeNarrow * 2;

        /* draw data */
        var sum = 0;
        var str = "";
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
            var code1 = ITF[val1];
            var code2 = ITF[val2];
            if (gtin14) {
                sum += val1 * 3 + val2;
                if (12 == i) {
                    val2 = sum % 10;
                    val2 = (10 - val2) % 10;
                    chr2 = val2.ToString();
                    code2 = ITF[val2];
                    str += chr1 + chr2;
                } else {
                    str += chr1 + chr2;
                }
            } else {
                mG.DrawString(chr1 + chr2, mFont, Brushes.Black,
                    PosX, PosY + CodeHeight + (Border ? (BORDER_WEIGHT / 2) : 0)
                );
            }
            for (int j = 4; 0 <= j; j--) {
                if (1 == ((code1 >> j) & 1)) {
                    DrawBar(codeWide);
                    PosX += codeWide;
                } else {
                    DrawBar(codeNarrow);
                    PosX += codeNarrow;
                }
                if (1 == ((code2 >> j) & 1)) {
                    PosX += codeWide;
                } else {
                    PosX += codeNarrow;
                }
            }
        }

        /* end of code */
        DrawBar(codeWide);
        PosX += codeWide + codeNarrow;
        DrawBar(codeNarrow);
        PosX += codeNarrow;
        PosX += spaceWidth;

        if (gtin14) {
            str = string.Format("{0} {1} {2} {3}",
                str.Substring(0, 3),
                str.Substring(3, 5),
                str.Substring(8, 5),
                str.Substring(13, 1)
            );
            var w = mG.MeasureString(str, mFont).Width;
            mG.DrawString(str, mFont, Brushes.Black,
                (PosX - w) / 2.0f, PosY + CodeHeight + (Border ? (BORDER_WEIGHT / 2) : 0)
            );
        }

        /* draw border */
        if (Border) {
            DrawBorder(PosX, PosY);
        }
    }

    void DrawEAN(string value) {
        var codePitch = Pitch;
        var spaceWidth = Pitch * QUIET_SIZE;
        var notchHeight = 5;

        PosX += Border ? 1 : 0;

        /* begin of code */
        PosX += spaceWidth;
        DrawBar(codePitch);
        PosX += codePitch * 2;
        DrawBar(codePitch);
        PosX += codePitch;

        /* draw data */
        var sum = 0;
        var oddEven = 0;
        for (int i = 0; i < value.Length; i++) {
            var chr = value.Substring(i, 1);
            int val;
            if (!int.TryParse(chr, out val)) {
                chr = "0";
            }
            sum += val * (0 == i % 2 ? 1 : 3);
            if (0 == i) {
                oddEven = EAN[val, 3];
                mG.DrawString(chr, mFont, Brushes.Black,
                    spaceWidth - codePitch * 8, PosY + CodeHeight - notchHeight
                );
                continue;
            }
            if (7 == i) {
                PosX += codePitch;
                DrawBar(codePitch);
                PosX += codePitch * 2;
                DrawBar(codePitch);
                PosX += codePitch;
            }
            int code;
            if (i < 7) {
                code = EAN[val, (oddEven >> (6 - i)) & 1];
            } else if (12 == i) {
                val = sum % 10;
                val = (10 - val) % 10;
                chr = val.ToString();
                code = EAN[val, 2];
            } else {
                code = EAN[val, 2];
            }
            int len = 0;
            for (int j = 6; 0 <= j; j--) {
                if (6 == j) {
                    mG.DrawString(chr, mFont, Brushes.Black,
                        PosX, PosY + CodeHeight - notchHeight
                    );
                }
                if (1 == ((code >> j) & 1)) {
                    len++;
                } else {
                    if (0 < len) {
                        DrawBar(codePitch * len, -notchHeight);
                        PosX += codePitch * len;
                        len = 0;
                    }
                    PosX += codePitch;
                }
            }
            if (0 < len) {
                DrawBar(codePitch * len, -notchHeight);
                PosX += codePitch * len;
            }
        }

        /* end of code */
        DrawBar(codePitch);
        PosX += codePitch * 2;
        DrawBar(codePitch);
        PosX += spaceWidth;

        /* draw border */
        if (Border) {
            mG.DrawRectangle(Pens.Black, 0, PosY - 2, PosX, CodeHeight + 8);
        }
    }
}
