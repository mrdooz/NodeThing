using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NodeThing;

namespace NodeThing
{

    public partial class MainForm : Form
    {
        private class StateBase
        {
            protected StateBase(MainForm form)
            {
                _form = form;
            }

            public virtual void Render(Graphics g)
            {
            }

            public virtual StateBase MouseUp(object sender, MouseEventArgs e)
            {
                return this;
            }

            public virtual StateBase MouseDown(object sender, MouseEventArgs e)
            {
                return this;
            }

            public virtual StateBase MouseMove(object sender, MouseEventArgs e)
            {
                return this;
            }

            protected MainForm _form;
        }

        private class DefaultState : StateBase
        {
            public DefaultState(MainForm form)
                : base(form)
            {
                form.ClearCreateObject();
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);

                if (_form._selectedNodes.Count > 0) {
                    // Start moving if the node we're clicking inside is in the selected list
                    var hitNode = _form._graph.PointInsideNode(pt);
                    if (e.Button == MouseButtons.Left && hitNode != null && hitNode.Selected) {
                        return new MovingState(_form, pt);
                    }
                }

                return base.MouseMove(sender, e);
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);

                if (_form._graph.PointInsideNode(pt) == null) {
                    // Create a new node
                    var newNode = _form._factory.CreateNode(_form._createNode, pt);
                    if (newNode != null) {
                        _form._graph.AddNode(newNode);
                    }
                    _form.mainPanel.Invalidate();
                }

                return this;
            }

            public override StateBase MouseDown(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);

                _form.ClearSelectedConnections();

                // Check for selecting a connection
                var hitConnection = _form._graph.PointInsideConnection(pt);
                if (hitConnection != null && (hitConnection.Direction == Connection.Io.Output || !hitConnection.Used)) {
                    _form.mainPanel.Invalidate();
                    return new ClickedConnectionState(_form, hitConnection.Node.ConnectionPos(hitConnection.Direction, hitConnection.Slot).Item2, hitConnection);
                }

                // Check for selecting a node
                var hitNode = _form._graph.PointInsideNode(pt);
                if (hitNode != null) {
                    // ctrl-click for multiple select
                    if (ModifierKeys != Keys.Control && !hitNode.Selected) {
                        _form.ClearSelectedNodes();
                    }

                    _form.propertyGrid1.Settings = hitNode.Properties;

                    _form._selectedNodes.Add(hitNode);
                    hitNode.Selected = true;
                    _form.mainPanel.Invalidate();
                    return this;
                }

                // Check for selecting a line between two connections
                var conPair = _form._graph.PointOnConnection(pt);
                if (conPair.Item1 != null && conPair.Item2 != null) {
                    conPair.Item1.Selected = true;
                    conPair.Item2.Selected = true;
                    // Note, the pair is (parent, child)
                    _form._selectedConnections.Add(conPair);
                }

                _form.ClearSelectedNodes();
                return this;

            }
        }

        private class ClickedConnectionState : StateBase
        {
            public ClickedConnectionState(MainForm form, Point pt, Connection start)
                : base(form)
            {
                Start = start;
                StartPos = pt;
                _curPos = pt;
            }

            public override void Render(Graphics g)
            {
                var pen = new Pen(Color.Black, 2);
                g.DrawLine(pen, StartPos, _curPos);
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e)
            {
                if (_prevHover != null) {
                    _prevHover.Hovering = false;
                    _prevHover.ErrorState = false;
                }

                _curPos = new Point(e.X, e.Y);

                var conn = _form._graph.PointInsideConnection(_curPos);
                if (conn != null && conn != Start) {
                    _prevHover = conn;
                    if (Start.LegalConnection(conn)) {
                        conn.Hovering = true;
                    } else {
                        conn.ErrorState = true;
                    }
                }
                _form.mainPanel.Invalidate();
                return this;
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                if (_prevHover != null) {
                    _prevHover.Hovering = false;
                    _prevHover.ErrorState = false;
                }

                var pt = new Point(e.X, e.Y);
                var end = _form._graph.PointInsideConnection(pt);
                if (end == null) {
                    _form.mainPanel.Invalidate();
                    return new DefaultState(_form);
                }

                // Check that we can connect the nodes
                // Note, the parent is the node with the input
                if (Start.LegalConnection(end)) {
                    var parent = Start.Direction == Connection.Io.Input ? Start : end;
                    var child = Start.Direction == Connection.Io.Output ? Start : end;
                    parent.Used = true;
                    child.Used = true;
                    _form._graph.AddConnection(parent.Node, parent.Slot, child.Node, child.Slot);
                }

                _form.mainPanel.Invalidate();
                return new DefaultState(_form);
            }

            private Point _curPos;
            public Point StartPos { get; set; }
            public Connection Start { get; set; }
            private Connection _prevHover;
        }

        private class MovingState : StateBase
        {
            public MovingState(MainForm form, Point pos)
                : base(form)
            {
                foreach (var node in form._selectedNodes) {
                    _startingNodePositions.Add(new Tuple<Node, Point>(node, node.Pos));
                }

                _startPos = pos;
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e)
            {
                var dx = e.X - _startPos.X;
                var dy = e.Y - _startPos.Y;

                foreach (var n in _startingNodePositions) {
                    var node = n.Item1;
                    node.Pos = new Point(n.Item2.X + dx, n.Item2.Y + dy);
                }
                _form.mainPanel.Invalidate();
                return this;
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                return new DefaultState(_form);
            }

            private Point _startPos;
            private List<Tuple<Node, Point>> _startingNodePositions = new List<Tuple<Node, Point>>();
        }
    }
}