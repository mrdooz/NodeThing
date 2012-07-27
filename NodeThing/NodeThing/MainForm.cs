using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Linq;

namespace NodeThing
{

    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();

            foreach (var item in _factory.NodeNames())
                nodeList.Items.Add(item);

            _currentState = new DefaultState(this);
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var brush = new SolidBrush(mainPanel.BackColor);
            _graph.Render(g);

            _currentState.Render(g);
        }

        private void nodeList_SelectedValueChanged(object sender, EventArgs e)
        {
            _createNode = (string)((ListBox)sender).SelectedItem;
        }

        private void mainPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _currentState = _currentState.MouseUp(sender, e);
        }

        private void mainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            _currentState = _currentState.MouseMove(sender, e);
        }

        private void mainPanel_MouseDown(object sender, MouseEventArgs e)
        {
            _currentState = _currentState.MouseDown(sender, e);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try {
                var ds = new DataContractSerializer(typeof(Graph));
                var settings = new XmlWriterSettings() { Indent = true };
                using (var x = XmlWriter.Create(@"c:\temp\tjong.xml", settings)) {
                    ds.WriteObject(x, _graph);
                }
            } catch (IOException) {

            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ds = new DataContractSerializer(typeof(Graph));
            using (var fileStream = new FileStream(@"c:\temp\tjong.xml", FileMode.Open, FileAccess.Read, FileShare.Read)) {
                _graph = (Graph)ds.ReadObject(fileStream);
            }
            mainPanel.Invalidate();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected void ClearCreateObject()
        {
            nodeList.ClearSelected();
            _createNode = "";
        }

        protected void ClearSelectedNodes()
        {
            foreach (var n in _selectedNodes) {
                n.Selected = false;
            }
            _selectedNodes.Clear();
        }

        protected void ClearSelectedConnections()
        {
            foreach (var c in _selectedConnections) {
                c.Item1.Selected = false;
                c.Item2.Selected = false;
            }
            _selectedConnections.Clear();
        }


        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode) {
                case Keys.Escape:
                    ClearCreateObject();
                    ClearSelectedNodes();
                    ClearSelectedConnections();
                    mainPanel.Invalidate();
                    break;

                case Keys.Delete:
                    if (_selectedNodes.Count > 0) {
                        var res = MessageBox.Show("Are you sure you want to delete the selected node(s)?", "Delete node?", MessageBoxButtons.YesNoCancel);
                        if (res == DialogResult.Yes) {
                            foreach (var n in _selectedNodes) {
                                _graph.DeleteNode(n);
                            }
                            ClearSelectedNodes();
                            mainPanel.Invalidate();
                        }
                    } else if (_selectedConnections.Count > 0) {
                        foreach (var c in _selectedConnections) {
                            _graph.DeleteConnection(c.Item1, c.Item2);
                        }
                        ClearSelectedConnections();
                        mainPanel.Invalidate();
                        
                    }
                    break;
            }
        }

        private NodeFactory _factory = new TextureFactory();
        private Graph _graph = new Graph();
        private string _createNode;

        private StateBase _currentState;

        private List<Node> _selectedNodes = new List<Node>();
        private List<Tuple<Connection,Connection>> _selectedConnections = new List<Tuple<Connection, Connection>>();
    }
}
