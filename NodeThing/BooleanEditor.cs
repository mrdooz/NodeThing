using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class BooleanEditor : UserControl
    {

        public event EventHandler ValueChanged;
        private NodeProperty<bool> _property;

        public BooleanEditor(string name, NodePropertyBase property, EventHandler handler)
        {
            _property = (NodeProperty<bool>)property;
            InitializeComponent();
            checkBox1.Text = name;
            checkBox1.Checked = _property.Value;
            ValueChanged += handler;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _property.Value = checkBox1.Checked;
            if (ValueChanged != null)
                ValueChanged(this, new EventArgs());
        }
    }
}
