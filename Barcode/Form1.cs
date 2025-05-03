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
			Form1_Resize();
		}

		private void Form1_Resize(object sender = null, EventArgs e = null) {
			textBox1.Width = Width - grpControl.Width - 28;
			grpControl.Left = textBox1.Right + 4;
			panel1.Width = Width - 24;
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

		private void chkDispString_CheckedChanged(object sender, EventArgs e) {
			Draw();
		}

		private void numPitch_ValueChanged_1(object sender, EventArgs e) {
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
				bmp = Factory.Draw(textBox1.Text, numPitch.Value, chkBorder.Checked, chkDispString.Checked, Factory.Type.CODE128);
				break;
			case 1:
				bmp = Factory.Draw(textBox1.Text, numPitch.Value, chkBorder.Checked, chkDispString.Checked, Factory.Type.CODE39);
				break;
			case 2:
				bmp = Factory.Draw(textBox1.Text, numPitch.Value, chkBorder.Checked, chkDispString.Checked, Factory.Type.CODABAR);
				break;
			case 3:
				bmp = Factory.Draw(textBox1.Text, numPitch.Value, chkBorder.Checked, chkDispString.Checked, Factory.Type.ITF);
				break;
			case 4:
				bmp = Factory.Draw(textBox1.Text, numPitch.Value, chkBorder.Checked, chkDispString.Checked, Factory.Type.GTIN14);
				break;
			case 5:
				bmp = Factory.Draw(textBox1.Text, numPitch.Value, chkBorder.Checked, chkDispString.Checked, Factory.Type.EAN_JAN);
				break;
			}
			if (null != bmp) {
				pictureBox1.Image = bmp;
				pictureBox1.Width = bmp.Width;
				pictureBox1.Height = bmp.Height;
			}
		}
	}
}
