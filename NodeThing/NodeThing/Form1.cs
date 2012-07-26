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

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            foreach (var item in _factory.nodeNames())
                nodeList.Items.Add(item);
        }


        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
            using (Graphics g = mainPanel.CreateGraphics())
            {
                _graph.render(g);
/*
                Pen pen = new Pen(Color.Black, 2);
                Brush brush = new SolidBrush(mainPanel.BackColor);

                g.DrawRectangle(pen, 100, 100, 100, 200);
                pen.Dispose();
 */ 
            }

        }

        private void nodeList_SelectedValueChanged(object sender, EventArgs e)
        {
            _createNode = (string)((ListBox)sender).SelectedItem;
           
        }

        private void mainPanel_MouseUp(object sender, MouseEventArgs e)
        {
            var node = _factory.createNode(_createNode, new Point(e.X, e.Y));
            if (node != null)
            {
                _graph.addNode(node);
                mainPanel.Invalidate();

            }
        }

        NodeFactory _factory = new NodeFactory();
        Graph _graph = new Graph();
        string _createNode;

    }
}
