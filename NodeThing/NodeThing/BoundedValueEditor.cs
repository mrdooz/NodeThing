using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class BoundedValueEditor : UserControl
    {
        public event EventHandler ValueChanged;

        private NodePropertyBase _property;

        public BoundedValueEditor(NodePropertyBase property, EventHandler handler)
        {
            _property = property;
            InitializeComponent();
            textBox.Text = _property.ToString();
        }

        private void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.FillRectangle(Brushes.Black, 0, 0, drawPanel.Width, drawPanel.Height);
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if (ValueChanged != null) {
                float value;
                if (float.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                    var p = _property as NodeProperty<float>;
                    if (p.IsBounded) {
                        value = Math.Min(p.Max, Math.Max(p.Min, value));
                    }
                    ValueChanged(this, new EventArgs<float> { Value = value });
                }
            }
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var prop = (NodeProperty<float>)_property;
            float value = prop.Min + e.X / (float)drawPanel.Width * (prop.Max - prop.Min);
            ValueChanged(this, new EventArgs<float> {Value = value});
        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {

        }
    }
}
