using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Code {
    public partial class Form1 : Form {
        const int CODE_HEIGHT = 40;
        const int SPACE_HEIGHT = 40;

        public Form1() {
            InitializeComponent();
        }

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

        readonly int[,] EAN = {
            { 0b001101, 0b100111, 0b111001 },
            { 0b011001, 0b110011, 0b110011 },
            { 0b010011, 0b011011, 0b110110 },
            { 0b111101, 0b100001, 0b100001 },
            { 0b100011, 0b011101, 0b101110 },
            { 0b110001, 0b111001, 0b100111 },
            { 0b101111, 0b000101, 0b101000 },
            { 0b111011, 0b010001, 0b100010 },
            { 0b110111, 0b001001, 0b100100 },
            { 0b001011, 0b010111, 0b111010 }
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

        void Draw() {
            switch (cmbType.SelectedIndex) {
            case 0: {
                var bmp = DrawCode39(textBox1.Text);
                panel1.Width = bmp.Width + 25;
                pictureBox1.Image = bmp;
                pictureBox1.Width = bmp.Width;
                pictureBox1.Height = bmp.Height;
                break;
            }
            case 1: {
                var bmp = DrawNW7(textBox1.Text);
                panel1.Width = bmp.Width + 25;
                pictureBox1.Image = bmp;
                pictureBox1.Width = bmp.Width;
                pictureBox1.Height = bmp.Height;
                break;
            }
            case 2:
            case 3: {
                var bmp = DrawITF(textBox1.Text, 3 == cmbType.SelectedIndex);
                panel1.Width = bmp.Width + 25;
                pictureBox1.Image = bmp;
                pictureBox1.Width = bmp.Width;
                pictureBox1.Height = bmp.Height;
                break;
            }
            case 4: {
                var bmp = DrawEAN(textBox1.Text);
                panel1.Width = bmp.Width + 25;
                pictureBox1.Image = bmp;
                pictureBox1.Width = bmp.Width;
                pictureBox1.Height = bmp.Height;
                break;
            }
            }
        }

        void DrawBar(Graphics g, double px, double py, double width, double height) {
            var x = (int)px;
            var y = (int)py;
            var w = (float)width;
            var h = (float)height;
            var dx = px - x;
            var dw = width - (int)width;
            if (0.0 < dx) {
                g.FillRectangle(Brushes.Black, x + 1, y, w - 1, h);
                if (0.0 < dw) {
                    g.DrawLine(new Pen(Color.FromArgb(95, 0, 0, 0)),
                        x + w + 1, y,
                        x + w + 1, y + h - 1
                    );
                } else {
                    g.DrawLine(new Pen(Color.FromArgb(141, 0, 0, 0)),
                        x + w, y,
                        x + w, y + h - 1
                    );
                }
            } else {
                if (0.0 < dw) {
                    g.FillRectangle(Brushes.Black, x, y, w - 1, h);
                    g.DrawLine(new Pen(Color.FromArgb(95, 0, 0, 0)),
                        x + w, y,
                        x + w, y + h - 1
                    );
                } else {
                    g.FillRectangle(Brushes.Black, x, y, w, h);
                }
            }
        }

        Bitmap DrawCode39(string value) {
            int borderWeight = chkBorder.Checked ? 6 : 0;
            float codeNarrow = (float)numPitch.Value;
            float codeWide = codeNarrow * 3;
            float spaceWidth = codeNarrow * 13;

            var lines = value.Replace("\r", "").Split('\n');

            int maxLength = 0;
            int codeCount = 0;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                var length = line.Length + 2;
                if (maxLength < length) {
                    maxLength = length;
                }
                codeCount++;
            }

            var bmp = new Bitmap(
                (int)(maxLength * (codeNarrow * 7 + codeWide * 3) + spaceWidth * 2 + borderWeight),
                codeCount * (CODE_HEIGHT + SPACE_HEIGHT) + 10
            );
            var g = Graphics.FromImage(bmp);

            var posY = SPACE_HEIGHT / 2.0f;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                line = "*" + line + "*";

                var posX = borderWeight / 2.0f;

                /* begin of code */
                posX += spaceWidth;

                /* draw data */
                for (int i = 0; 1 <= line.Length; i++) {
                    var chr = line.Substring(0, 1).ToUpper();
                    if (!CODE39.ContainsKey(chr)) {
                        chr = " ";
                    }
                    var code = CODE39[chr];
                    for (int j = 8; 0 <= j; j--) {
                        if (8 == j) {
                            g.DrawString(chr, new Font("MS Gothic", 9.0f), Brushes.Black,
                                posX, posY + CODE_HEIGHT + borderWeight
                            );
                        }
                        float codeWidth;
                        if (1 == ((code >> j) & 1)) {
                            codeWidth = codeWide;
                        } else {
                            codeWidth = codeNarrow;
                        }
                        if (0 == j % 2) {
                            DrawBar(g, posX, posY, codeWidth, CODE_HEIGHT);
                        }
                        posX += codeWidth;
                    }
                    posX += codeNarrow;
                    line = line.Substring(1, line.Length - 1);
                }

                /* end of code */
                posX += spaceWidth;

                /* draw border */
                if (chkBorder.Checked) {
                    g.DrawRectangle(new Pen(Brushes.Black, borderWeight),
                        borderWeight / 2, posY,
                        posX - borderWeight / 2, CODE_HEIGHT
                    );
                }

                posY += CODE_HEIGHT + SPACE_HEIGHT;
            }
            return bmp;
        }

        Bitmap DrawNW7(string value) {
            int borderWeight = chkBorder.Checked ? 6 : 0;
            float codeNarrow = (float)numPitch.Value;
            float codeWide = codeNarrow * 3;
            float spaceWidth = codeNarrow * 13;

            var lines = value.Replace("\r", "").Split('\n');

            int maxLength = 0;
            int codeCount = 0;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                var length = line.Length;
                if (maxLength < length) {
                    maxLength = length;
                }
                codeCount++;
            }

            var bmp = new Bitmap(
                (int)(maxLength * (codeNarrow * 5 + codeWide * 3) + spaceWidth * 2 + borderWeight),
                codeCount * (CODE_HEIGHT + SPACE_HEIGHT) + 10
            );
            var g = Graphics.FromImage(bmp);

            var posY = SPACE_HEIGHT / 2.0f;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                var posX = borderWeight / 2.0f;

                /* begin of code */
                posX += spaceWidth;

                /* draw data */
                for (int i = 0; 1 <= line.Length; i++) {
                    var chr = line.Substring(0, 1).ToUpper();
                    if (!NW7.ContainsKey(chr)) {
                        chr = "-";
                    }
                    var code = NW7[chr];
                    for (int j = 6; 0 <= j; j--) {
                        if (6 == j) {
                            g.DrawString(chr, new Font("MS Gothic", 9.0f), Brushes.Black,
                                posX, posY + CODE_HEIGHT + borderWeight
                            );
                        }
                        float codeWidth;
                        if (1 == ((code >> j) & 1)) {
                            codeWidth = codeWide;
                        } else {
                            codeWidth = codeNarrow;
                        }
                        if (0 == j % 2) {
                            DrawBar(g, posX, posY, codeWidth, CODE_HEIGHT);
                        }
                        posX += codeWidth;
                    }
                    posX += codeNarrow;
                    line = line.Substring(1, line.Length - 1);
                }

                /* end of code */
                posX += spaceWidth;

                /* draw border */
                if (chkBorder.Checked) {
                    g.DrawRectangle(new Pen(Brushes.Black, borderWeight),
                        borderWeight / 2, posY,
                        posX - borderWeight / 2, CODE_HEIGHT
                    );
                }

                posY += CODE_HEIGHT + SPACE_HEIGHT;
            }
            return bmp;
        }

        Bitmap DrawITF(string value, bool itf14) {
            int borderWeight = chkBorder.Checked ? 6 : 0;
            float codeNarrow = (float)numPitch.Value;
            float codeWide = codeNarrow * 3;
            float spaceWidth = codeNarrow * 13;

            var lines = value.Replace("\r", "").Split('\n');

            int maxLength = 0;
            int codeCount = 0;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                if (itf14) {
                    line = line.PadRight(13, '0').Substring(0, 13) + "0";
                    maxLength = 14;
                } else {
                    var length = line.Length;
                    if (0 != length % 2) {
                        line += "0";
                        length++;
                    }
                    if (maxLength < length) {
                        maxLength = length;
                    }
                }
                lines[l] = line;
                codeCount++;
            }

            var bmp = new Bitmap(
                (int)(maxLength * (codeNarrow * 3 + codeWide * 2) + codeNarrow * 9 + spaceWidth * 2 + borderWeight),
                codeCount * (CODE_HEIGHT + SPACE_HEIGHT) + 10
            );
            var g = Graphics.FromImage(bmp);

            var posY = SPACE_HEIGHT / 2.0f;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                var posX = borderWeight / 2.0f;

                /* begin of code */
                posX += spaceWidth;
                DrawBar(g, posX, posY, codeNarrow, CODE_HEIGHT);
                posX += codeNarrow * 2;
                DrawBar(g, posX, posY, codeNarrow, CODE_HEIGHT);
                posX += codeNarrow * 2;

                /* draw data */
                var sum = 0;
                var str = "";
                for (int i = 0; 2 <= line.Length; i++) {
                    var chr1 = line.Substring(0, 1);
                    var chr2 = line.Substring(1, 1);
                    int val1;
                    int val2;
                    if (!int.TryParse(chr1, out val1)) {
                        val1 = 0;
                        chr1 = "0";
                    }
                    if (!int.TryParse(chr2, out val2)) {
                        val2 = 0;
                        chr2 = "0";
                    }
                    var code1 = ITF[val1];
                    var code2 = ITF[val2];
                    if (itf14) {
                        sum += val1 * 3 + val2;
                        switch(i) {
                        case 6:
                            val2 = sum % 10;
                            val2 = (10 - val2) % 10;
                            chr2 = val2.ToString();
                            code2 = ITF[val2];
                            str += chr1 + chr2;
                            str = str.Substring(0, 3) + " "
                                + str.Substring(3, 5) + " "
                                + str.Substring(8, 5) + " "
                                + str.Substring(13, 1);
                            var font = new Font("MS Gothic", 9.0f);
                            var w = g.MeasureString(str, font).Width;
                            g.DrawString(str, font, Brushes.Black,
                                (bmp.Width - w) / 2.0f, posY + CODE_HEIGHT + borderWeight
                            );
                            break;
                        default:
                            str += chr1 + chr2;
                            break;
                        }
                    }
                    for (int j = 4; 0 <= j; j--) {
                        if (!itf14 && 4 == j) {
                            g.DrawString(chr1 + chr2, new Font("MS Gothic", 9.0f), Brushes.Black,
                                posX, posY + CODE_HEIGHT + borderWeight
                            );
                        }
                        if (1 == ((code1 >> j) & 1)) {
                            DrawBar(g, posX, posY, codeWide, CODE_HEIGHT);
                            posX += codeWide;
                        } else {
                            DrawBar(g, posX, posY, codeNarrow, CODE_HEIGHT);
                            posX += codeNarrow;
                        }
                        if (1 == ((code2 >> j) & 1)) {
                            posX += codeWide;
                        } else {
                            posX += codeNarrow;
                        }
                    }
                    line = line.Substring(2, line.Length - 2);
                }

                /* end of code */
                DrawBar(g, posX, posY, codeWide, CODE_HEIGHT);
                posX += codeWide + codeNarrow;
                DrawBar(g, posX, posY, codeNarrow, CODE_HEIGHT);
                posX += codeNarrow;
                posX += spaceWidth;

                /* draw border */
                if (chkBorder.Checked) {
                    g.DrawRectangle(new Pen(Brushes.Black, borderWeight),
                        borderWeight / 2, posY,
                        posX - borderWeight / 2, CODE_HEIGHT
                    );
                }

                posY += CODE_HEIGHT + SPACE_HEIGHT;
            }
            return bmp;
        }

        Bitmap DrawEAN(string value) {
            var codePitch = (float)numPitch.Value;
            var spaceWidth = codePitch * 13;

            var lines = value.Replace("\r", "").Split('\n');

            int codeCount = 0;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                line = line.PadRight(12, '0').Substring(0, 12) + "0";
                lines[l] = line;
                codeCount++;
            }

            var bmp = new Bitmap(
                (int)((codePitch * (6 * 12 + 3 + 5 + 3)) + codePitch * 9 + spaceWidth * 2),
                codeCount * (CODE_HEIGHT + SPACE_HEIGHT) + 10
            );
            var g = Graphics.FromImage(bmp);

            var posY = SPACE_HEIGHT / 2.0f;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                var posX = 0.0f;

                /* begin of code */
                posX += spaceWidth;
                DrawBar(g, posX, posY, codePitch, CODE_HEIGHT);
                posX += codePitch * 2;
                DrawBar(g, posX, posY, codePitch, CODE_HEIGHT);
                posX += codePitch;

                /* draw data */
                var sum = 0;
                var oddEven = 0;
                for (int i = 0; 1 <= line.Length; i++) {
                    var chr = line.Substring(0, 1);
                    int val;
                    if (!int.TryParse(chr, out val)) {
                        val = 0;
                        chr = "0";
                    }
                    sum += val * (0 == i % 2 ? 1 : 3);
                    if (0 == i) {
                        oddEven = EAN_P[val];
                        g.DrawString(chr, new Font("MS Gothic", 9.0f), Brushes.Black,
                            spaceWidth - codePitch * 8, posY + CODE_HEIGHT - 5
                        );
                        line = line.Substring(1, line.Length - 1);
                        continue;
                    }
                    if (7 == i) {
                        posX += codePitch;
                        DrawBar(g, posX, posY, codePitch, CODE_HEIGHT);
                        posX += codePitch * 2;
                        DrawBar(g, posX, posY, codePitch, CODE_HEIGHT);
                        posX += codePitch;
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
                            g.DrawString(chr, new Font("MS Gothic", 9.0f), Brushes.Black,
                                posX, posY + CODE_HEIGHT - 5
                            );
                        }
                        if (1 == ((code >> j) & 1)) {
                            len++;
                        } else {
                            if (0 < len) {
                                DrawBar(g, posX, posY, codePitch * len, CODE_HEIGHT - 5);
                                posX += codePitch * len;
                                len = 0;
                            }
                            posX += codePitch;
                        }
                    }
                    if (0 < len) {
                        DrawBar(g, posX, posY, codePitch * len, CODE_HEIGHT - 5);
                        posX += codePitch * len;
                    }
                    line = line.Substring(1, line.Length - 1);
                }

                /* end of code */
                DrawBar(g, posX, posY, codePitch, CODE_HEIGHT);
                posX += codePitch * 2;
                DrawBar(g, posX, posY, codePitch, CODE_HEIGHT);

                posY += CODE_HEIGHT + SPACE_HEIGHT;
            }
            return bmp;
        }

        private void Form1_Load(object sender, EventArgs e) {
            cmbType.SelectedIndex = 0;
            pictureBox1.BackColor = Color.White;
            panel1.BackColor = Color.White;
        }

        private void Form1_Resize(object sender, EventArgs e) {
            panel1.Height = Height - panel1.Top - 48;
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            Draw();
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e) {
            Draw();
        }

        private void numPitch_ValueChanged(object sender, EventArgs e) {
            Draw();
        }

        private void chkBorder_CheckedChanged(object sender, EventArgs e) {
            Draw();
        }

        private void btnSave_Click(object sender, EventArgs e) {
            if (null == pictureBox1.Image) {
                return;
            }
            saveFileDialog1.Filter = "PNGファイル(*.png)|*.png";
            saveFileDialog1.FileName = "";
            saveFileDialog1.ShowDialog();
            if (string.IsNullOrWhiteSpace(saveFileDialog1.FileName)) {
                return;
            }
            if (!Directory.Exists(Path.GetDirectoryName(saveFileDialog1.FileName))) {
                return;
            }
            pictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Png);
        }
    }
}
