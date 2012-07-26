namespace NodeThing
{
    partial class Form1
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
            this.mainPanel = new System.Windows.Forms.Panel();
            this.nodeList = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.mainPanel.Location = new System.Drawing.Point(160, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(394, 483);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.mainPanel_Paint);
            this.mainPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mainPanel_MouseUp);
            // 
            // nodeList
            // 
            this.nodeList.Dock = System.Windows.Forms.DockStyle.Left;
            this.nodeList.FormattingEnabled = true;
            this.nodeList.ItemHeight = 16;
            this.nodeList.Location = new System.Drawing.Point(0, 0);
            this.nodeList.Name = "nodeList";
            this.nodeList.Size = new System.Drawing.Size(154, 483);
            this.nodeList.TabIndex = 1;
            this.nodeList.SelectedValueChanged += new System.EventHandler(this.nodeList_SelectedValueChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 483);
            this.Controls.Add(this.nodeList);
            this.Controls.Add(this.mainPanel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.ListBox nodeList;
    }
}

