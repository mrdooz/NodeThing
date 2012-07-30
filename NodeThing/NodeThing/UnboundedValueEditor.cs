using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class UnboundedValueEditor : UserControl
    {
        public event EventHandler ValueChanged;
        private NodePropertyBase _property;

        public UnboundedValueEditor(NodePropertyBase property, EventHandler handler)
        {
            InitializeComponent();
            ValueChanged += handler;
            _property = property;
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            float value;
            if (float.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                ValueChanged(this, new EventArgs<float> { Value = value });
            }
        }
    }

    public class EventArgs<T> : EventArgs
    {
        public T Value { get; set; }
    }

}
