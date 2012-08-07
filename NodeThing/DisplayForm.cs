using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class DisplayForm : Form
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        List<Panel> _sinkPanels = new List<Panel>();
        private int _sinkCount;

        private Font _font = new Font("Arial", 20);

        class BufferData
        {
            public string Name { get; set; }
            public Panel Panel { get; set; }
            public Bitmap Bitmap { get; set; }
        }

        class BackingData
        {
            public string Name { get; set; }
            public Bitmap Bitmap { get; set; }
        }

        private Dictionary<IntPtr, BufferData> _bufferData = new Dictionary<IntPtr, BufferData>();
        private Dictionary<Panel, BackingData> _backingBitmap = new Dictionary<Panel, BackingData>();

        public DisplayForm()
        {
            InitializeComponent();
            _sinkPanels.Add(sinkPanel);
        }

        public void BeginAddPanels()
        {
            _sinkCount = 0;
        }

        public IntPtr GetPreviewHandle(out IntPtr windowKey)
        {
            // Create a GDI bitmap to render to
            var bufferData = new BufferData { Name = "Preview", Panel = previewPanel };
            var handle = new Bitmap(512, 512, PixelFormat.Format24bppRgb).GetHbitmap();
            _bufferData[handle] = bufferData;
            windowKey = bufferData.Panel.Handle;
            return handle;
        }

        public IntPtr GetSinkHandle(string name, out IntPtr windowKey)
        {
            // Check if we need to create a new sink handle
            if (++_sinkCount > _sinkPanels.Count) {
                var newPanel = new Panel { Size = new Size(512, 512) };
                newPanel.Paint += sinkPanel_Paint;
                flowLayoutPanel.Controls.Add(newPanel);
                flowLayoutPanel.PerformLayout();
                flowLayoutPanel.Refresh();
                _sinkPanels.Add(newPanel);
            }

            var panel = _sinkPanels[_sinkCount - 1];
            var bufferData = new BufferData { Name = name, Panel = panel };
            var handle = new Bitmap(512, 512, PixelFormat.Format24bppRgb).GetHbitmap();
            _bufferData[handle] = bufferData;
            windowKey = bufferData.Panel.Handle;
            return handle;
        }

        public void EndAddPanels()
        {
            // Remove any superflous panels
            if (_sinkCount > 0 && _sinkPanels.Count > _sinkCount) {
                for (int i = _sinkCount; i < _sinkPanels.Count; ++i) {
                    var panel = _sinkPanels[i];
                    flowLayoutPanel.Controls.Remove(panel);
                    _backingBitmap.Remove(panel);
                }
                flowLayoutPanel.PerformLayout();
                flowLayoutPanel.Refresh();
            }
        }

        public void OnTextureCompleted(IntPtr handle)
        {
            // Invoke the callback on the forms thread
            Invoke((MethodInvoker)delegate {

                BufferData data;
                if (_bufferData.TryGetValue(handle, out data)) {

                    // Create a new backing bitmap from the handle, and update the panel -> backing mapping
                    var bitmap = Image.FromHbitmap(handle);
                    _backingBitmap[data.Panel] = new BackingData {Bitmap = bitmap, Name = data.Name};

                    data.Panel.Invalidate();
                    _bufferData.Remove(handle);
                }

                DeleteObject(handle);

            });
        }

        private void previewPanel_Paint(object sender, PaintEventArgs e)
        {
            BackingData data;
            var panel = (Panel)sender;
            if (_backingBitmap.TryGetValue(panel, out data)) {
                e.Graphics.DrawImage(data.Bitmap, 0, 0);

                if (displayTextureName.Checked) {
                    e.Graphics.DrawString(data.Name, _font, Brushes.White, 0, 0);
                }

            }
        }

        private void sinkPanel_Paint(object sender, PaintEventArgs e)
        {
            BackingData data;
            var panel = (Panel)sender;
            if (_backingBitmap.TryGetValue(panel, out data)) {
                e.Graphics.DrawImage(data.Bitmap, 0, 0);

                if (displayTextureName.Checked) {
                    e.Graphics.DrawString(data.Name, _font, Brushes.White, 0, 0);
                }

            }
        }

        private void displayTextureName_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var panel in _backingBitmap.Keys) {
                panel.Invalidate();
            }
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
