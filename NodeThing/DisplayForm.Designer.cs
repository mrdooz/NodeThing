using System.Windows.Forms;
namespace NodeThing
{
    partial class DisplayForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.flowLayoutPanel = new NodeThing.DisplayFlowLayoutPanel();
            this.previewPanel = new NodeThing.DoublBufferedPanel();
            this.sinkPanel = new NodeThing.DoublBufferedPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.displayTextureName = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.AutoScroll = true;
            this.flowLayoutPanel.AutoSize = true;
            this.flowLayoutPanel.Controls.Add(this.previewPanel);
            this.flowLayoutPanel.Controls.Add(this.sinkPanel);
            this.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(1082, 519);
            this.flowLayoutPanel.TabIndex = 2;
            // 
            // previewPanel
            // 
            this.previewPanel.Location = new System.Drawing.Point(3, 3);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(512, 512);
            this.previewPanel.TabIndex = 0;
            this.previewPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.previewPanel_Paint);
            // 
            // sinkPanel
            // 
            this.sinkPanel.Location = new System.Drawing.Point(521, 3);
            this.sinkPanel.Name = "sinkPanel";
            this.sinkPanel.Size = new System.Drawing.Size(512, 512);
            this.sinkPanel.TabIndex = 1;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.displayTextureName);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.flowLayoutPanel);
            this.splitContainer1.Size = new System.Drawing.Size(1084, 562);
            this.splitContainer1.SplitterDistance = 37;
            this.splitContainer1.TabIndex = 0;
            // 
            // displayTextureName
            // 
            this.displayTextureName.AutoSize = true;
            this.displayTextureName.Checked = true;
            this.displayTextureName.CheckState = System.Windows.Forms.CheckState.Checked;
            this.displayTextureName.Location = new System.Drawing.Point(12, 12);
            this.displayTextureName.Name = "displayTextureName";
            this.displayTextureName.Size = new System.Drawing.Size(124, 17);
            this.displayTextureName.TabIndex = 0;
            this.displayTextureName.Text = "Display texture name";
            this.displayTextureName.UseVisualStyleBackColor = true;
            this.displayTextureName.CheckedChanged += new System.EventHandler(this.displayTextureName_CheckedChanged);
            // 
            // DisplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1084, 562);
            this.Controls.Add(this.splitContainer1);
            this.MaximumSize = new System.Drawing.Size(1100, 1100);
            this.Name = "DisplayForm";
            this.Text = "DisplayForm";
            this.flowLayoutPanel.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DisplayFlowLayoutPanel flowLayoutPanel;
        private DoublBufferedPanel previewPanel;
        private DoublBufferedPanel sinkPanel;
        private SplitContainer splitContainer1;
        private CheckBox displayTextureName;
    }
}