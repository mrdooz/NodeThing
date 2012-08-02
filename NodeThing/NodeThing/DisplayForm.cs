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

        public DisplayForm()
        {
            InitializeComponent();
            _sinkPanels.Add(sinkPanel);
        }

        public void BeginAddPanels()
        {
            _sinkCount = 0;
        }

        public IntPtr GetPreviewHandle()
        {
            return previewPanel.Handle;
        }

        public IntPtr GetSinkHandle()
        {
            // Check if we need to create a new sink handle
            if (++_sinkCount > _sinkPanels.Count) {
                //var panel = new Panel { Size = new Size(512, 512), Location = new Point((1 + _sinkCount) * 512, 0) };
                var panel = new Panel { Size = new Size(512, 512) };
                flowLayoutPanel.Controls.Add(panel);
                flowLayoutPanel.PerformLayout();
                flowLayoutPanel.Refresh();
                _sinkPanels.Add(panel);
            }

            return _sinkPanels[_sinkCount - 1].Handle;
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
    }
}
