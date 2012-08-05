using System;
using System.Drawing;
using System.Windows.Forms;

namespace NodeThing
{

    public partial class MainForm : Form
    {
        class StateBase
        {
          protected StateBase(MainForm form)
            {
                _form = form;
            }

            public virtual void Render(Graphics g) { }
            public virtual StateBase MouseUp(object sender, MouseEventArgs e) { return this; }
            public virtual StateBase MouseDown(object sender, MouseEventArgs e) { return this; }
            public virtual StateBase MouseMove(object sender, MouseEventArgs e) { return this; }
            protected MainForm _form;
        }

        class DefaultState : StateBase
        {
            public DefaultState(MainForm form) : base(form) { }

            public override StateBase MouseDown(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);
                var selectedConnection = _form._graph.PointInsideConnection(pt);
                if (selectedConnection != null)
                {
                    _form._clickedConnectionState.Start = selectedConnection;
                    _form._clickedConnectionState.StartPos = pt;
                    return _form._clickedConnectionState;
                }
                else
                {
                    var selectedNode = _form._graph.PointInsideNode(pt);
                    if (selectedNode != null)
                    {
                        MessageBox.Show("selectedNode!");
                    }
                    else
                    {
                        var node = _form._factory.CreateNode(_form._createNode, pt);
                        if (node != null)
                        {
                            _form._graph.AddNode(node);
                            _form.mainPanel.Invalidate();

                        }
                    }
                }

                return this;
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e) 
            {
/*
                var pt = new Point(e.X, e.Y);
                var selectedConnection = _form._graph.pointInsideConnection(pt);
                if (selectedConnection != null)
                {
                    _form._clickedConnectionState.Start = selectedConnection;
                    _form._clickedConnectionState.StartPos = pt;
                    return _form._clickedConnectionState;
                }
                else
                {
                    var selectedNode = _form._graph.pointInsideNode(pt);
                    if (selectedNode != null)
                    {
                        MessageBox.Show("selectedNode!");
                    }
                    else
                    {
                        var node = _form._factory.CreateNode(_form._createNode, pt);
                        if (node != null)
                        {
                            _form._graph.addNode(node);
                            _form.mainPanel.Invalidate();

                        }
                    }
                }
*/
                return this;
            }
        }

        class ClickedConnectionState : StateBase
        {
            public ClickedConnectionState(MainForm form) : base(form) { }

            public override void Render(Graphics g) 
            {
                var pen = new Pen(Color.Black, 2);
                var h = _curPos.Y - StartPos.Y;
                var middle = new Point((_curPos.X + StartPos.X) / 2, (_curPos.Y + StartPos.Y) / 2);
                g.DrawBezier(pen, StartPos, new Point(middle.X, middle.Y + h), new Point(middle.X, middle.Y - h),_curPos);
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e) 
            {
                _curPos = new Point(e.X, e.Y);
                _form.mainPanel.Invalidate();
                return this; 
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);
                var end = _form._graph.PointInsideConnection(pt);
                if (end == null)
                    return _form._defaultState;

                // Check that we can connect the nodes
                // Note, the parent is the node with the input
                if (Start.Direction != end.Direction && Start.DataType == end.DataType && !Start.Used && !end.Used) {
                    var parent = Start.Direction == Connection.Io.Input ? Start : end;
                    var child = Start.Direction == Connection.Io.Output ? Start : end;
                    _form._graph.AddConnection(parent.Node, parent.Slot, child.Node, child.Slot);
                }

                //_form._clickedConnectionState.Start = selectedConnection;
                return _form._defaultState;
            }

            Point _curPos;
            public Point StartPos { get; set; }
            public Connection Start { get; set; }
        }

        class ClickedNodeState : StateBase
        {
            public ClickedNodeState(MainForm form) : base(form) { }
        }


        public MainForm()
        {
            InitializeComponent();

            foreach (var item in _factory.NodeNames())
                nodeList.Items.Add(item);

            _defaultState = new DefaultState(this);
            _clickedConnectionState = new ClickedConnectionState(this);
            _clickedNodeState = new ClickedNodeState(this);
            _currentState = _defaultState;
        }


        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
          var g = e.Graphics;
          var brush = new SolidBrush(mainPanel.BackColor);
          //g.FillRectangle(brush, mainPanel.Bounds);
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

        NodeFactory _factory = new NodeFactory();
        Graph _graph = new Graph();
        string _createNode;

        StateBase _currentState;
        DefaultState _defaultState;
        ClickedConnectionState _clickedConnectionState;
        ClickedNodeState _clickedNodeState;

        private void mainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            _currentState = _currentState.MouseMove(sender, e);
        }

        private void mainPanel_MouseDown(object sender, MouseEventArgs e)
        {
            _currentState = _currentState.MouseDown(sender, e);
        }

    }
}
