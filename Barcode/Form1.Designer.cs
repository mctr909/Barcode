namespace Code {
    partial class Form1 {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.btnSave = new System.Windows.Forms.Button();
			this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
			this.panel1 = new System.Windows.Forms.Panel();
			this.cmbType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.chkBorder = new System.Windows.Forms.CheckBox();
			this.chkDispString = new System.Windows.Forms.CheckBox();
			this.numPitch = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.grpControl = new System.Windows.Forms.GroupBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numPitch)).BeginInit();
			this.grpControl.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Location = new System.Drawing.Point(4, 4);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(284, 84);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// textBox1
			// 
			this.textBox1.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.textBox1.Location = new System.Drawing.Point(5, 5);
			this.textBox1.Margin = new System.Windows.Forms.Padding(4);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBox1.Size = new System.Drawing.Size(493, 200);
			this.textBox1.TabIndex = 1;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// btnSave
			// 
			this.btnSave.Location = new System.Drawing.Point(6, 165);
			this.btnSave.Margin = new System.Windows.Forms.Padding(4);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(100, 29);
			this.btnSave.TabIndex = 2;
			this.btnSave.Text = "保存";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// panel1
			// 
			this.panel1.AutoScroll = true;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.pictureBox1);
			this.panel1.Location = new System.Drawing.Point(5, 213);
			this.panel1.Margin = new System.Windows.Forms.Padding(4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(667, 211);
			this.panel1.TabIndex = 3;
			// 
			// cmbType
			// 
			this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbType.FormattingEnabled = true;
			this.cmbType.Items.AddRange(new object[] {
            "Code128",
            "Code39",
            "Codabar/NW7",
            "ITF",
            "ITF(GTIN-14)",
            "EAN/JAN"});
			this.cmbType.Location = new System.Drawing.Point(7, 36);
			this.cmbType.Margin = new System.Windows.Forms.Padding(4);
			this.cmbType.Name = "cmbType";
			this.cmbType.Size = new System.Drawing.Size(152, 23);
			this.cmbType.TabIndex = 6;
			this.cmbType.SelectedIndexChanged += new System.EventHandler(this.cmbType_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(7, 18);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(37, 15);
			this.label2.TabIndex = 7;
			this.label2.Text = "形式";
			// 
			// chkBorder
			// 
			this.chkBorder.AutoSize = true;
			this.chkBorder.Location = new System.Drawing.Point(6, 110);
			this.chkBorder.Margin = new System.Windows.Forms.Padding(4);
			this.chkBorder.Name = "chkBorder";
			this.chkBorder.Size = new System.Drawing.Size(65, 19);
			this.chkBorder.TabIndex = 8;
			this.chkBorder.Text = "枠あり";
			this.chkBorder.UseVisualStyleBackColor = true;
			this.chkBorder.CheckedChanged += new System.EventHandler(this.chkBorder_CheckedChanged);
			// 
			// chkDispString
			// 
			this.chkDispString.AutoSize = true;
			this.chkDispString.Location = new System.Drawing.Point(6, 137);
			this.chkDispString.Margin = new System.Windows.Forms.Padding(4);
			this.chkDispString.Name = "chkDispString";
			this.chkDispString.Size = new System.Drawing.Size(89, 19);
			this.chkDispString.TabIndex = 9;
			this.chkDispString.Text = "文字表示";
			this.chkDispString.UseVisualStyleBackColor = true;
			this.chkDispString.CheckedChanged += new System.EventHandler(this.chkDispString_CheckedChanged);
			// 
			// numPitch
			// 
			this.numPitch.DecimalPlaces = 1;
			this.numPitch.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
			this.numPitch.Location = new System.Drawing.Point(7, 81);
			this.numPitch.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.numPitch.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numPitch.Name = "numPitch";
			this.numPitch.Size = new System.Drawing.Size(65, 22);
			this.numPitch.TabIndex = 10;
			this.numPitch.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numPitch.ValueChanged += new System.EventHandler(this.numPitch_ValueChanged_1);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(7, 63);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(39, 15);
			this.label1.TabIndex = 11;
			this.label1.Text = "ピッチ";
			// 
			// grpControl
			// 
			this.grpControl.Controls.Add(this.label2);
			this.grpControl.Controls.Add(this.label1);
			this.grpControl.Controls.Add(this.btnSave);
			this.grpControl.Controls.Add(this.numPitch);
			this.grpControl.Controls.Add(this.cmbType);
			this.grpControl.Controls.Add(this.chkDispString);
			this.grpControl.Controls.Add(this.chkBorder);
			this.grpControl.Location = new System.Drawing.Point(505, 5);
			this.grpControl.Name = "grpControl";
			this.grpControl.Size = new System.Drawing.Size(166, 200);
			this.grpControl.TabIndex = 12;
			this.grpControl.TabStop = false;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(683, 436);
			this.Controls.Add(this.grpControl);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.textBox1);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "Form1";
			this.Text = "Barcode";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.Resize += new System.EventHandler(this.Form1_Resize);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numPitch)).EndInit();
			this.grpControl.ResumeLayout(false);
			this.grpControl.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkBorder;
		private System.Windows.Forms.CheckBox chkDispString;
		private System.Windows.Forms.NumericUpDown numPitch;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox grpControl;
	}
}

