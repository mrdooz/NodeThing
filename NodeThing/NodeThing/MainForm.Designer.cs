namespace NodeThing
{
    partial class MainForm
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
            if (disposing && (components != null))
            {
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
            this.nodeList = new System.Windows.Forms.ListBox();
            this.mainPanel = new NodeThing.DoublBufferedPanel();
            this.SuspendLayout();
            // 
            // nodeList
            // 
            this.nodeList.Dock = System.Windows.Forms.DockStyle.Left;
            this.nodeList.FormattingEnabled = true;
            this.nodeList.Location = new System.Drawing.Point(0, 0);
            this.nodeList.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nodeList.Name = "nodeList";
            this.nodeList.Size = new System.Drawing.Size(116, 392);
            this.nodeList.TabIndex = 1;
            this.nodeList.SelectedValueChanged += new System.EventHandler(this.nodeList_SelectedValueChanged);
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.AutoSize = true;
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(416, 392);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.TabStop = true;
            this.mainPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.mainPanel_Paint);
            this.mainPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mainPanel_MouseDown);
            this.mainPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mainPanel_MouseMove);
            this.mainPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mainPanel_MouseUp);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 392);
            this.Controls.Add(this.nodeList);
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "MainForm";
            this.Text = "It\'s just a node thang";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox nodeList;
        private DoublBufferedPanel mainPanel;
    }
}

