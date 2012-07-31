using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using ColorWheel;

namespace NodeThing
{

    public partial class MainForm : Form
    {
        private readonly Type[] _knownTypes = {
            typeof (Size), typeof (Color),
            typeof (NodeProperty<float>), typeof (NodeProperty<string>), typeof (NodeProperty<Size>), typeof (NodeProperty<Color>),
            typeof (NodeProperty<Tuple<float, float>>)
        };

        private NodeFactory _factory = new TextureFactory();
        private string _createNode;

        private StateBase _currentState;

        private List<Node> _selectedNodes = new List<Node>();
        private List<Tuple<Connection, Connection>> _selectedConnections = new List<Tuple<Connection, Connection>>();

        private DisplayForm _displayForm;
        private Timer _redrawTimer = new Timer();

        [DataContract]
        public class InstanceSettings
        {
            public InstanceSettings()
            {
                Graph = new Graph();
            }

            [DataMember]
            public Graph Graph { get; set; }

            [DataMember]
            public Size CanvasSize { get; set; }

            [DataMember]
            public Point ScrollOffset { get; set; }
        }

        public InstanceSettings Settings { get; private set; }

        public MainForm()
        {
            InitializeComponent();

            _displayForm = new DisplayForm();
            _displayForm.Show();

            foreach (var item in _factory.NodeNames())
                nodeList.Items.Add(item);

            _currentState = new DefaultState(this);

            mainPanel.AutoScrollMinSize = mainPanel.Size;

            Settings = new InstanceSettings();
        }

        public void PropertyChanged(object sender, EventArgs args)
        {
            GenerateCode();
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var brush = new SolidBrush(mainPanel.BackColor);
            Settings.Graph.Render(g, Settings.ScrollOffset);

            _currentState.Render(g, Settings.ScrollOffset);
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
                var ds = new DataContractSerializer(typeof (Graph), _knownTypes);
                var settings = new XmlWriterSettings() { Indent = true };
                using (var x = XmlWriter.Create(@"c:\temp\tjong.xml", settings)) {
                    ds.WriteObject(x, Settings.Graph);
                }
            } catch (IOException) {

            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ds = new DataContractSerializer(typeof(Graph), _knownTypes);
            using (var fileStream = new FileStream(@"c:\temp\tjong.xml", FileMode.Open, FileAccess.Read, FileShare.Read)) {
                Settings.Graph = (Graph)ds.ReadObject(fileStream);
                Settings.Graph.SetPropertyListener(PropertyChanged);
            }
            mainPanel.AutoScrollMinSize = Settings.CanvasSize;
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
                                Settings.Graph.DeleteNode(n);
                            }
                            ClearSelectedNodes();
                            mainPanel.Invalidate();
                        }
                    } else if (_selectedConnections.Count > 0) {
                        foreach (var c in _selectedConnections) {
                            Settings.Graph.DeleteConnection(c.Item1, c.Item2);
                        }
                        ClearSelectedConnections();
                        mainPanel.Invalidate();
                        
                    }
                    break;
            }
        }

        protected void NodeSelected(Node node)
        {
            flowLayoutPanel1.Controls.Clear();

            var propertyChanged = new EventHandler(delegate {
                // Limit the code gen to 10 fps
                if (!_redrawTimer.Enabled) {
                    _redrawTimer.Interval = 100;
                    _redrawTimer.Tick += delegate
                    {
                        _redrawTimer.Stop();
                        GenerateCode();
                    };
                    _redrawTimer.Start();
                }
            });

            foreach (var kv in node.Properties) {
                var key = kv.Key;
                var prop = kv.Value;

                switch (prop.PropertyType) {
                    case PropertyType.Float:
                    case PropertyType.Int:
                    case PropertyType.String: {
                        var editor = new ValueEditor(key, prop, propertyChanged);
                        flowLayoutPanel1.Controls.Add(editor);
                        break;
                    }

                    case PropertyType.Float2:
                    case PropertyType.Int2:
                    case PropertyType.Size: {
                        var editor = new ValueEditor2d(key, prop, propertyChanged);
                        flowLayoutPanel1.Controls.Add(editor);
                        break;
                    }


                    case PropertyType.Color: {
                        var editor = new ColorEditor(key, prop, propertyChanged);
                        flowLayoutPanel1.Controls.Add(editor);

                        break;
                    }
                }

            }

            var seq = Settings.Graph.GenerateCodeFromSelected(node, new Size(512, 512), "Preview");
            if (seq.Sequence.Count > 0)
                _factory.GenerateCode(seq, _displayForm.PreviewHandle());
        }

        private void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GenerateCode();
        }

        private void GenerateCode()
        {
            var code = Settings.Graph.GenerateCode();
            var displayedReal = false;
            var displayedPreview = false;
            foreach (var c in code) {
                if (c.IsPreview) {
                    if (!displayedPreview) {
                        displayedPreview = true;
                        _factory.GenerateCode(c, _displayForm.PreviewHandle());
                    }
                } else {
                    if (!displayedReal) {
                        displayedReal = true;
                        _factory.GenerateCode(c, _displayForm.DisplayHandle());
                    }
                }
            }
        }

        private void mainPanel_Scroll(object sender, ScrollEventArgs e)
        {
            Settings.ScrollOffset = e.ScrollOrientation == ScrollOrientation.HorizontalScroll
                ? new Point(e.NewValue, Settings.ScrollOffset.Y) : new Point(Settings.ScrollOffset.X, e.NewValue);
            mainPanel.Invalidate();
        }

        public Point PointToScrolled(Point pt)
        {
            // convert a point in client space to a point in virtual mainPanel space
            return new Point(pt.X + Settings.ScrollOffset.X, pt.Y + Settings.ScrollOffset.Y);
        }

        public void MultiSelect(Point topLeft, Point bottomRight)
        {
            var tl = PointMath.Add(topLeft, Settings.ScrollOffset);
            var rel = PointMath.Sub(bottomRight, topLeft);
            var rect = new Rectangle(PointMath.Add(topLeft, Settings.ScrollOffset), new Size(rel.X, rel.Y));
            foreach (var n in Settings.Graph.NodesInsideRect(rect)) {
                n.Selected = true;
                _selectedNodes.Add(n);
            }
        }

        public void UpdateCanvasSize(int width, int height, int dx, int dy)
        {
            Settings.CanvasSize = new Size(width, height);
            mainPanel.AutoScrollMinSize = Settings.CanvasSize;
            mainPanel.HorizontalScroll.Value = dx;
        }

    }
}
