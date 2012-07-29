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
        public DisplayForm()
        {
            InitializeComponent();
        }

        public IntPtr DisplayHandle()
        {
            return displayPanel.Handle;
        }

        public IntPtr PreviewHandle()
        {
            return previewPanel.Handle;
        }
    }
}
