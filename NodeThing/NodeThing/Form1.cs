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

    public partial class MainForm : Form
    {
        class StateBase
        {
            public StateBase(MainForm form)
            {
                _form = form;
            }

            public virtual void render(Graphics g) { }
            public virtual StateBase mouseUp(object sender, MouseEventArgs e) { return this; }
            public virtual StateBase mouseMove(object sender, MouseEventArgs e) { return this; }
            protected MainForm _form;
        }

        class DefaultState : StateBase
        {
            public DefaultState(MainForm form) : base(form) { }
            public override StateBase mouseUp(object sender, MouseEventArgs e) 
            {
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
                        var node = _form._factory.createNode(_form._createNode, pt);
                        if (node != null)
                        {
                            _form._graph.addNode(node);
                            _form.mainPanel.Invalidate();

                        }
                    }
                }

                return this;
            }
        }

        class ClickedConnectionState : StateBase
        {
            public ClickedConnectionState(MainForm form) : base(form) { }

            public override void render(Graphics g) 
            {
                var pen = new Pen(Color.Black, 2);
                var h = _curPos.Y - StartPos.Y;
                var middle = new Point((_curPos.X + StartPos.X) / 2, (_curPos.Y + StartPos.Y) / 2);
                g.DrawBezier(pen, StartPos, new Point(middle.X, middle.Y + h), new Point(middle.X, middle.Y - h),_curPos);
            }

            public override StateBase mouseMove(object sender, MouseEventArgs e) 
            {
                _curPos = new Point(e.X, e.Y);
                _form.mainPanel.Invalidate();
                return this; 
            }

            public override StateBase mouseUp(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);
                var selectedConnection = _form._graph.pointInsideConnection(pt);
                if (selectedConnection != null)
                {
                    _form._clickedConnectionState.Start = selectedConnection;
                    return _form._defaultState;
                }
                else 
                {
                    return _form._defaultState;
                }
            }

            Point _curPos;
            public Point StartPos { get; set; }
            public Connection Start { get; set; }
            public Connection End { get; set; }
        }

        class ClickedNodeState : StateBase
        {
            public ClickedNodeState(MainForm form) : base(form) { }
        }


        public MainForm()
        {
            InitializeComponent();

            foreach (var item in _factory.nodeNames())
                nodeList.Items.Add(item);

            _defaultState = new DefaultState(this);
            _clickedConnectionState = new ClickedConnectionState(this);
            _clickedNodeState = new ClickedNodeState(this);
            _currentState = _defaultState;
        }


        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
            using (Graphics g = mainPanel.CreateGraphics())
            {
                var brush = new SolidBrush(mainPanel.BackColor);
                //g.FillRectangle(brush, mainPanel.Bounds);
                _graph.render(g);

                _currentState.render(g);
            }
        }

        private void nodeList_SelectedValueChanged(object sender, EventArgs e)
        {
            _createNode = (string)((ListBox)sender).SelectedItem;
        }

        private void mainPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _currentState = _currentState.mouseUp(sender, e);
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
            _currentState = _currentState.mouseMove(sender, e);
        }

    }
}
