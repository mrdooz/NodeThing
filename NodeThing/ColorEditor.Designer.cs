namespace NodeThing
{
    partial class ColorEditor
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cWheel = new ColorWheel.CWheel();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxR = new System.Windows.Forms.TextBox();
            this.textBoxG = new System.Windows.Forms.TextBox();
            this.textBoxB = new System.Windows.Forms.TextBox();
            this.textBoxA = new System.Windows.Forms.TextBox();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // cWheel
            // 
            this.cWheel.Alignment = System.Windows.Forms.HorizontalAlignment.Left;
            this.cWheel.BackColor = System.Drawing.SystemColors.Control;
            this.cWheel.Color = System.Drawing.Color.Red;
            this.cWheel.DisplayStyle = ColorWheel.ColorWheelDisplayStyle.ColorWheel;
            this.cWheel.Location = new System.Drawing.Point(6, 19);
            this.cWheel.Name = "cWheel";
            this.cWheel.SegmentRotation = 3D;
            this.cWheel.ShowSelector = true;
            this.cWheel.Size = new System.Drawing.Size(168, 168);
            this.cWheel.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            this.cWheel.TabIndex = 0;
            this.cWheel.TabStop = false;
            this.cWheel.Text = "cWheel1";
            // 
            // groupBox
            // 
            this.groupBox.Controls.Add(this.textBoxA);
            this.groupBox.Controls.Add(this.textBoxB);
            this.groupBox.Controls.Add(this.textBoxG);
            this.groupBox.Controls.Add(this.textBoxR);
            this.groupBox.Controls.Add(this.label1);
            this.groupBox.Controls.Add(this.cWheel);
            this.groupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox.Location = new System.Drawing.Point(0, 0);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(180, 230);
            this.groupBox.TabIndex = 2;
            this.groupBox.TabStop = false;
            this.groupBox.Text = "groupBox1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 201);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "RGBA";
            // 
            // textBoxR
            // 
            this.textBoxR.Location = new System.Drawing.Point(52, 198);
            this.textBoxR.Name = "textBoxR";
            this.textBoxR.Size = new System.Drawing.Size(26, 20);
            this.textBoxR.TabIndex = 5;
            this.textBoxR.TextChanged += new System.EventHandler(this.textBoxR_TextChanged);
            // 
            // textBoxG
            // 
            this.textBoxG.Location = new System.Drawing.Point(82, 198);
            this.textBoxG.Name = "textBoxG";
            this.textBoxG.Size = new System.Drawing.Size(26, 20);
            this.textBoxG.TabIndex = 6;
            this.textBoxG.TextChanged += new System.EventHandler(this.textBoxG_TextChanged);
            // 
            // textBoxB
            // 
            this.textBoxB.Location = new System.Drawing.Point(112, 198);
            this.textBoxB.Name = "textBoxB";
            this.textBoxB.Size = new System.Drawing.Size(26, 20);
            this.textBoxB.TabIndex = 7;
            this.textBoxB.TextChanged += new System.EventHandler(this.textBoxB_TextChanged);
            // 
            // textBoxA
            // 
            this.textBoxA.Location = new System.Drawing.Point(142, 198);
            this.textBoxA.Name = "textBoxA";
            this.textBoxA.Size = new System.Drawing.Size(26, 20);
            this.textBoxA.TabIndex = 8;
            this.textBoxA.TextChanged += new System.EventHandler(this.textBoxA_TextChanged);
            // 
            // ColorEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox);
            this.Name = "ColorEditor";
            this.Size = new System.Drawing.Size(180, 230);
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private ColorWheel.CWheel cWheel;
        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.TextBox textBoxA;
        private System.Windows.Forms.TextBox textBoxB;
        private System.Windows.Forms.TextBox textBoxG;
        private System.Windows.Forms.TextBox textBoxR;
        private System.Windows.Forms.Label label1;
    }
}
