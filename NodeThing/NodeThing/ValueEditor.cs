using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class ValueEditor : UserControl
    {
        public event EventHandler ValueChanged;
        private NodePropertyBase _property;

        private bool _updatingTextbox;

        public ValueEditor(string name, NodePropertyBase property, EventHandler handler)
        {
            InitializeComponent();
            _property = property;
            textBox.Text = _property.ToString();
            groupBox1.Text = name;
            ValueChanged += handler;

            if (!_property.IsBounded || _property.PropertyType != PropertyType.String)
                drawPanel.Hide();
        }

        private void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.FillRectangle(Brushes.Black, 0, 0, drawPanel.Width, drawPanel.Height);
        }

        private void SetValue<T>(T value, NodePropertyBase prop) where T : IComparable<T>
        {
            var p = (NodeProperty<T>)prop;
            var orgValue = value;

            if (p.IsBounded) {
                if (value.CompareTo(p.Max) > 0)
                    value = p.Max;
                if (value.CompareTo(p.Min) < 0)
                    value = p.Min;
            }
            p.Value = value;

            if (value.CompareTo(orgValue) != 0) {
                _updatingTextbox = true;
                textBox.Text = value.ToString();
            }
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if (ValueChanged == null || _updatingTextbox)
                return;

            var newValue = false;
            switch (_property.PropertyType) {

                case PropertyType.Int: {
                        int value;
                        if (int.TryParse(textBox.Text, out value)) {
                            newValue = true;
                            SetValue(value, _property);
                        }
                        break;
                    }

                case PropertyType.Float: {
                        float value;
                        if (float.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                            newValue = true;
                            SetValue(value, _property);
                        }
                        break;
                    }

                case PropertyType.String:
                    newValue = true;
                    SetValue(textBox.Text, _property);

                    break;
            }

            if (newValue)
                ValueChanged(this, new EventArgs());
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var prop = (NodeProperty<float>)_property;
            float value = prop.Min + e.X / (float)drawPanel.Width * (prop.Max - prop.Min);
            ValueChanged(this, new EventArgs());
        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {

        }
    }
}
