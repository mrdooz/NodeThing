using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class DisplayForm : Form
    {
        List<Panel> _sinkPanels = new List<Panel>();
        private int _sinkCount;

        private Font _font = new Font("Arial", 20);

        class WindowData
        {
            public string Name { get; set; }
            public Panel Panel { get; set; }
        }

        private Dictionary<IntPtr, WindowData> _windowHandles = new Dictionary<IntPtr, WindowData>();

        public DisplayForm()
        {
            InitializeComponent();
            _sinkPanels.Add(sinkPanel);
            _windowHandles[previewPanel.Handle] = new WindowData {Name = "Preview", Panel = previewPanel};
        }

        public void BeginAddPanels()
        {
            _sinkCount = 0;
        }

        public IntPtr GetPreviewHandle()
        {
            return previewPanel.Handle;
        }

        public IntPtr GetSinkHandle(string name)
        {
            // Check if we need to create a new sink handle
            if (++_sinkCount > _sinkPanels.Count) {
                var newPanel = new Panel { Size = new Size(512, 512) };
                flowLayoutPanel.Controls.Add(newPanel);
                flowLayoutPanel.PerformLayout();
                flowLayoutPanel.Refresh();
                _sinkPanels.Add(newPanel);
            }

            var panel = _sinkPanels[_sinkCount - 1];
            var handle = panel.Handle;
            _windowHandles[handle] = new WindowData {Name = name, Panel = panel};
            return handle;
        }

        public void EndAddPanels()
        {
            // Remove any superflous panels
            if (_sinkPanels.Count > _sinkCount) {
                for (int i = _sinkCount; i < _sinkPanels.Count; ++i) {
                    flowLayoutPanel.Controls.Remove(_sinkPanels[i]);
                }
                flowLayoutPanel.PerformLayout();
                flowLayoutPanel.Refresh();
            }
        }

        private void flowLayoutPanel_Paint(object sender, PaintEventArgs e)
        {
            // Hm, redraw is pretty broken right now, but I can probably
            // save the bitmap I get from the compelted callback, and use this..
        }

        public void OnTextureCompleted(IntPtr hwnd)
        {
            // Invoke the callback on the forms thread
            Invoke((MethodInvoker)delegate {
                if (!displayTextureName.Checked)
                    return;

                WindowData data;
                _windowHandles.TryGetValue(hwnd, out data);
                Graphics g = data.Panel.CreateGraphics();
                g.DrawString(data.Name, _font, Brushes.White, 0, 0);
            });
        }
    }

    public class DisplayFlowLayoutPanel : FlowLayoutPanel
    {
        public DisplayFlowLayoutPanel()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
        }
    }
}
