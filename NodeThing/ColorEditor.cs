using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ColorWheel;

namespace NodeThing
{
    public partial class ColorEditor : UserControl
    {
        public event EventHandler ValueChanged;
        private NodePropertyBase _property;
        private bool _updatingTextbox;

        public ColorEditor(string name, NodePropertyBase property, EventHandler handler)
        {
            InitializeComponent();

            cWheel.OnCalculateColorGroup = HSVTriangle.CalculateColorGroup;
            cWheel.OnCalculateLayout = HSVTriangle.CalculateLayout;
            cWheel.OnCalculateSelectedPoint = HSVTriangle.CalculateSelectedPoint;
            cWheel.OnColorHitTest = HSVTriangle.ColorHitTest;
            cWheel.OnDrawColorWheel = HSVTriangle.DrawWheel;
            cWheel.OnDrawSelector = HSVTriangle.DrawSelector;
            cWheel.OnCalculateSelectorColor = HSVTriangle.CalculateSelectorColor;
            cWheel.DisplayStyle = ColorWheelDisplayStyle.HSVTriangle;
            cWheel.Color = ((NodeProperty<Color>)property).Value;

            cWheel.SelectedColorChanging += delegate(object sender, SelectedColorChangingEventArgs args) {
                ((NodeProperty<Color>)_property).Value = args.Color;
                ValueChanged(this, new EventArgs());
                UpdateTextBox();
            };

            cWheel.SelectedColorChanged += delegate(object sender, SelectedColorChangedEventArgs args) {
                ((NodeProperty<Color>)_property).Value = args.Color;
                ValueChanged(this, new EventArgs());
                UpdateTextBox();
            };

            _property = property;
            groupBox.Text = name;
            ValueChanged += handler;
            UpdateTextBox();
        }

        private void UpdateTextBox()
        {
            _updatingTextbox = true;
            textBoxR.Text = cWheel.Color.R.ToString();
            textBoxG.Text = cWheel.Color.G.ToString();
            textBoxB.Text = cWheel.Color.B.ToString();
            textBoxA.Text = cWheel.Color.A.ToString();
            _updatingTextbox = false;
        }

        private int Saturate(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private void TextboxChanged()
        {
            if (ValueChanged == null || _updatingTextbox)
                return;

            int r, g, b, a;
            if (!int.TryParse(textBoxR.Text, out r) || !int.TryParse(textBoxG.Text, out g) || !int.TryParse(textBoxB.Text, out b) || !int.TryParse(textBoxA.Text, out a))
                return;

            cWheel.Color = Color.FromArgb(Saturate(a), Saturate(r), Saturate(g), Saturate(b));
        }

        private void textBoxR_TextChanged(object sender, EventArgs e)
        {
            TextboxChanged();
        }

        private void textBoxG_TextChanged(object sender, EventArgs e)
        {
            TextboxChanged();
        }

        private void textBoxB_TextChanged(object sender, EventArgs e)
        {
            TextboxChanged();
        }

        private void textBoxA_TextChanged(object sender, EventArgs e)
        {
            TextboxChanged();
        }
    }
}
