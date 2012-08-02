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
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.previewPanel = new NodeThing.DoublBufferedPanel();
            this.sinkPanel = new NodeThing.DoublBufferedPanel();
            this.flowLayoutPanel.SuspendLayout();
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
            this.flowLayoutPanel.Size = new System.Drawing.Size(1084, 562);
            this.flowLayoutPanel.TabIndex = 2;
            // 
            // previewPanel
            // 
            this.previewPanel.Location = new System.Drawing.Point(3, 3);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(512, 512);
            this.previewPanel.TabIndex = 0;
            // 
            // sinkPanel
            // 
            this.sinkPanel.Location = new System.Drawing.Point(521, 3);
            this.sinkPanel.Name = "sinkPanel";
            this.sinkPanel.Size = new System.Drawing.Size(512, 512);
            this.sinkPanel.TabIndex = 1;
            // 
            // DisplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1084, 562);
            this.Controls.Add(this.flowLayoutPanel);
            this.Name = "DisplayForm";
            this.Text = "DisplayForm";
            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        //private DoubleBufferedFlowLayoutPanel flowLayoutPanel;
        private FlowLayoutPanel flowLayoutPanel;
        private DoublBufferedPanel previewPanel;
        private DoublBufferedPanel sinkPanel;
    }
}