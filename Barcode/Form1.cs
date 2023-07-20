using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Code {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
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

        void Draw() {
            Bitmap bmp = null;
            switch (cmbType.SelectedIndex) {
            case 0:
                bmp = DrawCode(textBox1.Text, Barcode.Type.CODE128);
                break;
            case 1:
                bmp = DrawCode(textBox1.Text, Barcode.Type.CODE39);
                break;
            case 2:
                bmp = DrawCode(textBox1.Text, Barcode.Type.NW7_CODABAR);
                break;
            case 3:
                bmp = DrawCode(textBox1.Text, Barcode.Type.ITF);
                break;
            case 4:
                bmp = DrawCode(textBox1.Text, Barcode.Type.GTIN14);
                break;
            case 5:
                bmp = DrawCode(textBox1.Text, Barcode.Type.EAN_JAN);
                break;
            case 6:
                bmp = QR.Draw(textBox1.Text, (float)numPitch.Value);
                break;
            }
            if (null != bmp) {
                panel1.Width = bmp.Width + 25;
                pictureBox1.Image = bmp;
                pictureBox1.Width = bmp.Width;
                pictureBox1.Height = bmp.Height;
            }
        }

        Bitmap DrawCode(string value, Barcode.Type type) {
            var code = new Barcode();
            code.Pitch = (float)numPitch.Value;
            code.Border = chkBorder.Checked;
            var lines = value.Replace("\r", "").Split('\n');
            double maxWidth = 0;
            int codeCount = 0;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                var length = code.GetWidth(line, type);
                if (maxWidth < length) {
                    maxWidth = length;
                }
                codeCount++;
            }
            const int SPACE_HEIGHT = 40;
            code.CreateCanvas((int)maxWidth, codeCount * (code.CodeHeight + SPACE_HEIGHT) + 4);
            code.PosY = SPACE_HEIGHT / 2.0f;
            for (int l = 0; l < lines.Length; l++) {
                var line = lines[l];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                code.Draw(line, type);
                code.PosY += code.CodeHeight;
                code.PosY += SPACE_HEIGHT;
            }
            return code.Bmp;
        }
    }
}
