using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.IO;

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

        private string _currentFilename;

        public class ClientTransform
        {
            public ClientTransform()
            {
                ZoomFactor = 1;
            }

            [DataMember]
            public Size CanvasSize { get; set; }

            [DataMember]
            public Point ScrollOffset { get; set; }

            public int ZoomFactor
            {
                get { return _zoomFactor; }
                set
                {
                    float[] zoomScales = {1, 1.25f, 1.5f, 2, 5};

                    _zoomFactor = Math.Max(1, Math.Min(5, value));
                    _zoomValue = zoomScales[_zoomFactor];
                }
            }

            [DataMember] 
            private float _zoomValue;

            [DataMember] 
            private int _zoomFactor;


            public Point PointToScrolled(Point pt)
            {
                // convert a point in client space to a point in virtual mainPanel space
                return new Point(
                    ZoomFactor * (pt.X + ScrollOffset.X),
                    ZoomFactor * (pt.Y + ScrollOffset.Y));
            }

            public Point PointToClient(Point pt)
            {
                // convert from logical space to client space
                return new Point(
                    (pt.X - ScrollOffset.X) / ZoomFactor,
                    (pt.Y - ScrollOffset.Y) / ZoomFactor);
            }

        }

        [DataContract]
        public class InstanceSettings
        {
            public InstanceSettings()
            {
                Graph = new Graph();
                Transform = new ClientTransform();
            }

            [DataMember]
            public Graph Graph { get; set; }

            [DataMember]
            public ClientTransform Transform { get; set; }
        }

        public InstanceSettings Settings { get; private set; }

        public MainForm()
        {
            InitializeComponent();

            _displayForm = new DisplayForm();
            _displayForm.Show();

            foreach (var item in _factory.NodeNames())
                nodeList.Items.Add(item);

            Settings = new InstanceSettings();
            _currentState = new DefaultState(this);

            mainPanel.MouseWheel += OnMouseWheel;
        }

        private void OnMouseWheel(object sender, MouseEventArgs mouseEventArgs)
        {
            // todo: make zoom work..
/*
            if (mouseEventArgs.Delta < 1) {
                Settings.Transform.ZoomFactor = Math.Min(5, Settings.Transform.ZoomFactor + 1);
            } else {
                Settings.Transform.ZoomFactor = Math.Max(1, Settings.Transform.ZoomFactor - 1);
            }

            ((HandledMouseEventArgs)mouseEventArgs).Handled = true;

            mainPanel.Invalidate();
 */
        }

        public void PropertyChanged(object sender, EventArgs args)
        {
            GenerateCode();
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var brush = new SolidBrush(mainPanel.BackColor);
            Settings.Graph.Render(g, Settings.Transform);

            _currentState.Render(g, Settings.Transform.ScrollOffset, Settings.Transform.ZoomFactor);
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

            // Need to set focus here to get mouse wheel to work
            mainPanel.Focus();
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
                UserControl editor = null;

                switch (prop.PropertyType) {
                    case PropertyType.Float:
                    case PropertyType.Int:
                    case PropertyType.String:
                        editor = new ValueEditor(key, prop, propertyChanged);
                        break;

                    case PropertyType.Float2:
                    case PropertyType.Int2:
                    case PropertyType.Size:
                        editor = new ValueEditor2d(key, prop, propertyChanged);
                        break;

                    case PropertyType.Color:
                        editor = new ColorEditor(key, prop, propertyChanged);
                        break;
                }

                if (editor != null)
                    flowLayoutPanel1.Controls.Add(editor);
            }

            var seq = Settings.Graph.GenerateCodeFromSelected(node);
            if (seq.Sequence.Count > 0) {
                seq.Name = "Preview";
                seq.Size = new Size(512, 512);
                _factory.GenerateCode(seq, _displayForm.PreviewHandle());
            }
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
            Settings.Transform.ScrollOffset = e.ScrollOrientation == ScrollOrientation.HorizontalScroll
                ? new Point(e.NewValue, Settings.Transform.ScrollOffset.Y) : new Point(Settings.Transform.ScrollOffset.X, e.NewValue);
            mainPanel.AutoScrollPosition = new Point(Settings.Transform.ScrollOffset.X, Settings.Transform.ScrollOffset.Y);
            mainPanel.Invalidate();
        }

        public void MultiSelect(Point topLeft, Point bottomRight)
        {
            topLeft = Settings.Transform.PointToScrolled(topLeft);
            bottomRight = Settings.Transform.PointToScrolled(bottomRight);
            var size = PointMath.Diff(bottomRight, topLeft);
            var rect = new Rectangle(topLeft, size);
            foreach (var n in Settings.Graph.NodesInsideRect(rect)) {
                n.Selected = true;
                _selectedNodes.Add(n);
            }
        }

        public void OnMoveSelected(int dx, int dy)
        {
            dx = Settings.Transform.ZoomFactor * dx;
            dy = Settings.Transform.ZoomFactor * dy;

            foreach (var n in _selectedNodes) {
                n.Pos = new Point(n.Pos.X + dx, n.Pos.Y + dy);
            }

            mainPanel.Invalidate();
        }

        public void OnPan(int dx, int dy)
        {
            var canvasSize = mainPanel.AutoScrollMinSize;
            var scrollX = Settings.Transform.ScrollOffset.X + dx;
            var scrollY = Settings.Transform.ScrollOffset.Y + dy;

            if (dx < 0) {

                if (scrollX < 0) {
                    // Resize the canvas, and nudge all the components inwards
                    var ofs = Math.Abs(scrollX);
                    Settings.Graph.MoveNodes(ofs, 0);

                    mainPanel.AutoScrollMinSize = new Size(canvasSize.Width + ofs, canvasSize.Height);
                }
                mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, Math.Max(0, scrollX), ScrollOrientation.HorizontalScroll));

            } else if (dx > 0) {
                // Check if we need to expand the canvas to the right
                // Settings.Transform.ScrollOffset.X + dx + mainPanel.Size.Width > canvasSize.Width
                if (scrollX + mainPanel.Size.Width > canvasSize.Width) {
                    mainPanel.AutoScrollMinSize = new Size(scrollX + mainPanel.Size.Width, canvasSize.Height);
                }
                mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, scrollX, ScrollOrientation.HorizontalScroll));
            }

            if (dy < 0) {

                if (scrollY < 0) {
                    // Resize the canvas, and nudge all the components downwards
                    var ofs = Math.Abs(scrollY);
                    Settings.Graph.MoveNodes(0, ofs);

                    mainPanel.AutoScrollMinSize = new Size(canvasSize.Width, canvasSize.Height+ofs);
                }
                mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, Math.Max(0, scrollY), ScrollOrientation.VerticalScroll));

            } else if (dy > 0) {
                // Check if we need to expand the canvas to the right
                if (scrollY + mainPanel.Size.Height > canvasSize.Height) {
                    mainPanel.AutoScrollMinSize = new Size(canvasSize.Width, scrollY + mainPanel.Size.Height);
                }
                mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, scrollY, ScrollOrientation.VerticalScroll));
            }

            Settings.Transform.CanvasSize = mainPanel.AutoScrollMinSize;
        }

        private void cropToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var rect = Settings.Graph.GetBoundingRectangle();
            if (rect.Width == 0)
                return;

            Settings.Graph.MoveNodes(-rect.Left, -rect.Top);
            var size = new Size(rect.Width, rect.Height);
            mainPanel.AutoScrollMinSize = size;
            Settings.Transform.CanvasSize = size;

            mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, 0, ScrollOrientation.HorizontalScroll));
            mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, 0, ScrollOrientation.VerticalScroll));

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            mainPanel.AutoScrollMinSize = mainPanel.Size;
        }

        private void SaveFile()
        {
            try{ 
                var ds = new DataContractSerializer(typeof(InstanceSettings), _knownTypes);
                var settings = new XmlWriterSettings() { Indent = true };
                using (var x = XmlWriter.Create(_currentFilename, settings)) {
                    ds.WriteObject(x, Settings);
                }
            } catch (IOException) {

            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveDlg = new SaveFileDialog {OverwritePrompt = true};
            if (saveDlg.ShowDialog() != DialogResult.OK)
                return;

            _currentFilename = saveDlg.FileName;
            SaveFile();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentFilename == null) {
                saveAsToolStripMenuItem_Click(sender, e);
            } else {
                SaveFile();
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var openDlg = new OpenFileDialog {Filter = "Xml files (*.xml)|*.xml|All Files (*.*)|*.*", FileName = "", CheckFileExists = true, CheckPathExists = true};

            if (openDlg.ShowDialog() != DialogResult.OK)
                return;

            var dir = new FileInfo(openDlg.FileName).DirectoryName;

            _currentFilename = openDlg.FileName;

            var ds = new DataContractSerializer(typeof(InstanceSettings), _knownTypes);
            using (var fileStream = new FileStream(_currentFilename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                Settings = (InstanceSettings)ds.ReadObject(fileStream);
            }

            mainPanel.AutoScrollMinSize = Settings.Transform.CanvasSize;
            mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, Settings.Transform.ScrollOffset.X, ScrollOrientation.HorizontalScroll));
            mainPanel_Scroll(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, Settings.Transform.ScrollOffset.Y, ScrollOrientation.VerticalScroll));
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

    }
}
