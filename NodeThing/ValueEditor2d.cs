using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class ValueEditor2d : UserControl
    {
        public event EventHandler ValueChanged;
        private NodePropertyBase _property;
        private bool _updatingTextbox;

        private bool[] _useBounds = {true, true};

        public ValueEditor2d(string name, NodePropertyBase property, EventHandler handler)
        {
            InitializeComponent();
            _property = property;
            groupBox1.Text = name;
            ValueChanged += handler;
            UpdateTextBox();

            // Hide the 2d panel if the property isn't a float pair
            if (!_property.IsBounded || _property.PropertyType != PropertyType.Float2) {
                drawPanel1.Hide();
                drawPanel2.Hide();
            }
        }

        private void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            var g = e.Graphics;
            var usingBounds = _useBounds[panel == drawPanel1 ? 0 : 1];
            g.FillRectangle(usingBounds ? Brushes.BurlyWood : Brushes.DarkOliveGreen, 0, 0, panel.Width, panel.Height);
            var prop = (NodeProperty<Tuple<float, float>>)_property;
            var pen = new Pen(Color.Black);

            if (panel == drawPanel1) {
                var x = panel.Width * (prop.Value.Item1 - prop.Min.Item1) / (prop.Max.Item1 - prop.Min.Item1);
                g.DrawLine(pen, x, 0, x, panel.Height);
                
            } else {
                var x = panel.Width * (prop.Value.Item2 - prop.Min.Item2) / (prop.Max.Item2 - prop.Min.Item2);
                g.DrawLine(pen, x, 0, x, panel.Height);
            }

        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            var panel = (Panel)sender;
            if (e.Button == MouseButtons.Left) {
                ProcessMouseEvent(panel, e);

            } else if (e.Button == MouseButtons.Right) {
                var idx = panel == drawPanel1 ? 0 : 1;
                _useBounds[idx] = !_useBounds[idx];
                panel.Invalidate();
            }
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                ProcessMouseEvent((Panel)sender, e);
            }
        }

        private void ProcessMouseEvent(Panel panel, MouseEventArgs e)
        {
            var prop = (NodeProperty<Tuple<float, float>>)_property;
            float valueX = prop.Value.Item1;
            float valueY = prop.Value.Item2;

            if (panel == drawPanel1) {
                valueX = prop.Min.Item1 + e.X / (float)panel.Width * (prop.Max.Item1 - prop.Min.Item1);
            } else {
                valueY = prop.Min.Item2 + e.X / (float)panel.Width * (prop.Max.Item1 - prop.Min.Item1);
            }

            prop.Value = new Tuple<float, float>(
                Utils.Clamp(valueX, prop.Min.Item1, prop.Max.Item1),
                Utils.Clamp(valueY, prop.Min.Item2, prop.Max.Item2)
                );

            UpdateTextBox();

            ValueChanged(this, new EventArgs());

            panel.Invalidate();
        }

        private void UpdateTextBox()
        {
            _updatingTextbox = true;
            if (_property.PropertyType == PropertyType.Float2) {
                var prop = (NodeProperty<Tuple<float, float>>)_property;
                textBox1.Text = prop.Value.Item1.ToString();
                textBox2.Text = prop.Value.Item2.ToString();

            } else if (_property.PropertyType == PropertyType.Int2) {
                var prop = (NodeProperty<Tuple<int, int>>)_property;
                textBox1.Text = prop.Value.Item1.ToString();
                textBox2.Text = prop.Value.Item2.ToString();

            } else if (_property.PropertyType == PropertyType.Size) {
                var prop = (NodeProperty<Size>)_property;
                textBox1.Text = prop.Value.Width.ToString();
                textBox2.Text = prop.Value.Height.ToString();
            }
            _updatingTextbox = false;
        }

        private void SetValuePair<T>(T value, int item) where T : IComparable<T>
        {
            var p = (NodeProperty<Tuple<T, T>>)_property;
            var orgValue = value;

            if (p.IsBounded && _useBounds[item]) {
                var maxValue = item == 0 ? p.Max.Item1 : p.Max.Item2;
                var minValue = item == 0 ? p.Min.Item1 : p.Min.Item2;
                if (value.CompareTo(maxValue) > 0)
                    value = maxValue;
                if (value.CompareTo(minValue) < 0)
                    value = minValue;
            }

            p.Value = item == 0 ? new Tuple<T, T>(value, p.Value.Item2) : new Tuple<T, T>(p.Value.Item1, value);

            if (value.CompareTo(orgValue) != 0) {
                _updatingTextbox = true;
                if (item == 0)
                    textBox1.Text = value.ToString();
                else
                    textBox2.Text = value.ToString();
                _updatingTextbox = false;
            }
        }

        private void handleTextBox(string text, int item)
        {
            if (ValueChanged == null || _updatingTextbox)
                return;

            var newValue = false;

            switch (_property.PropertyType) {

                case PropertyType.Int2: {
                        int value;
                        if (int.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                            newValue = true;
                            SetValuePair(value, item);
                        }
                        break;
                    }

                case PropertyType.Float2: {
                        float value;
                        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                            newValue = true;
                            SetValuePair(value, item);
                        }
                        break;
                    }

                case PropertyType.Size:
                    break;
            }

            if (newValue) {
                ValueChanged(this, new EventArgs());
                drawPanel1.Invalidate();
                drawPanel2.Invalidate();
            }
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            handleTextBox(textBox1.Text, 0);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            handleTextBox(textBox2.Text, 1);
        }
    }
}
