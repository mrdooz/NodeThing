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

        public ValueEditor2d(string name, NodePropertyBase property, EventHandler handler)
        {
            InitializeComponent();
            _property = property;
            groupBox1.Text = name;
            ValueChanged += handler;
            UpdateTextBox();

            // Hide the 2d panel if the property isn't a float pair
            if (!_property.IsBounded || _property.PropertyType != PropertyType.Float2) {
                drawPanel.Hide();
                useBounds.Hide();
            }
        }

        private void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.FillRectangle(Brushes.BurlyWood, 0, 0, drawPanel.Width, drawPanel.Height);
            var prop = (NodeProperty<Tuple<float, float>>)_property;
            var x = drawPanel.Width * (prop.Value.Item1 - prop.Min.Item1) / (prop.Max.Item1 - prop.Min.Item1);
            var y = drawPanel.Height * (prop.Value.Item2 - prop.Min.Item2) / (prop.Max.Item2 - prop.Min.Item2);
            var pen = new Pen(Color.Black);
            g.DrawLine(pen, 0, y, drawPanel.Width, y);
            g.DrawLine(pen, x, 0, x, drawPanel.Height);
        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                ProcessMouseEvent(e);
            }
        }

        private void ProcessMouseEvent(MouseEventArgs e)
        {
            var prop = (NodeProperty<Tuple<float, float>>)_property;
            float valueX = prop.Min.Item1 + e.X / (float)drawPanel.Width * (prop.Max.Item1 - prop.Min.Item1);
            float valueY = prop.Min.Item2 + e.Y / (float)drawPanel.Height * (prop.Max.Item2 - prop.Min.Item2);

            prop.Value = new Tuple<float, float>(
                Utils.Clamp(valueX, prop.Min.Item1, prop.Max.Item1),
                Utils.Clamp(valueY, prop.Min.Item2, prop.Max.Item2)
                );

            UpdateTextBox();

            ValueChanged(this, new EventArgs());
            drawPanel.Invalidate();
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

            if (p.IsBounded && useBounds.Checked) {
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
                drawPanel.Invalidate();
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
