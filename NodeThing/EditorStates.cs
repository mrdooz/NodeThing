using System.Drawing;
using System.Windows.Forms;

namespace NodeThing
{

    public partial class MainForm
    {
        private class StateBase
        {
            protected StateBase(MainForm form)
            {
                _form = form;
                _transform = _form.Settings.Transform;
            }

            public virtual void Render(Graphics g, Point scrollOffset, int zoomFactor)
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
            protected ClientTransform _transform;
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
                var scrolledPt = _transform.PointToScrolled(pt);

                if (_form._selectedNodes.Count > 0) {
                    // Start moving if the node we're clicking inside is in the selected list
                    var hitNode = _form.Settings.Graph.PointInsideNode(scrolledPt);
                    if (e.Button == MouseButtons.Left && hitNode != null && hitNode.Selected) {
                        return new MovingState(_form, pt);
                    }
                } else {
                    // Start multiselect
                    if (e.Button == MouseButtons.Left) {
                        return new MultiSelectState(_form, pt);
                    }
                }

                return base.MouseMove(sender, e);
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);
                var scrolledPt = _transform.PointToScrolled(pt);

                if (_form.Settings.Graph.PointInsideNode(scrolledPt) == null) {
                    // Create a new node
                    var newNode = _form._factory.CreateNode(_form._createNode, scrolledPt);
                    if (newNode != null) {
                        _form.Settings.Graph.AddNode(newNode);
                    }
                    _form.mainPanel.Invalidate();
                }

                return this;
            }

            public override StateBase MouseDown(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);
                var scrolledPt = _transform.PointToScrolled(pt);

                _form.ClearSelectedConnections();

                // Check for selecting a connection
                var hitConnection = _form.Settings.Graph.PointInsideConnection(scrolledPt);
                if (hitConnection != null && (hitConnection.Direction == Connection.Io.Output || !hitConnection.Used)) {
                    _form.mainPanel.Invalidate();
                    var startPos = _transform.PointToClient(hitConnection.Node.ConnectionPos(hitConnection.Direction, hitConnection.Slot).Item2);
                    return new ClickedConnectionState(_form, startPos, hitConnection);
                }

                // Check for selecting a node
                var hitNode = _form.Settings.Graph.PointInsideNode(scrolledPt);
                if (hitNode != null) {
                    if (!hitNode.Selected) {
                        // ctrl-click for multiple select
                        if (ModifierKeys != Keys.Control)
                            _form.ClearSelectedNodes();

                        hitNode.Selected = true;
                        _form._selectedNodes.Add(hitNode);
                        _form.NodeSelected(hitNode);
                        _form.mainPanel.Invalidate();
                    }
                    return this;
                }

                _form.ClearSelectedNodes();

                // Check for selecting a line between two connections
                var conPair = _form.Settings.Graph.PointOnConnection(scrolledPt);
                if (conPair.Item1 != null && conPair.Item2 != null) {
                    conPair.Item1.Selected = true;
                    conPair.Item2.Selected = true;
                    // Note, the pair is (parent, child)
                    _form._selectedConnections.Add(conPair);
                    return this;
                }

                // Check for entering canvas resize mode
                if (ModifierKeys == Keys.Shift) {
                    return new PanningState(_form, pt);
                }

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

            public override void Render(Graphics g, Point scrollOffset, int zoomFactor)
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
                var scrolledPt = _transform.PointToScrolled(_curPos);

                var conn = _form.Settings.Graph.PointInsideConnection(scrolledPt);
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
                var scrolledPt = _transform.PointToScrolled(pt);

                var end = _form.Settings.Graph.PointInsideConnection(scrolledPt);
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
                    _form.Settings.Graph.AddConnection(parent.Node, parent.Slot, child.Node, child.Slot);
                }

                _form.mainPanel.Invalidate();
                return new DefaultState(_form);
            }

            private Point _curPos;
            private Point StartPos { get; set; }
            private Connection Start { get; set; }
            private Connection _prevHover;
        }

        private class MovingState : StateBase
        {
            private Point _prevPos;

            public MovingState(MainForm form, Point pos)
                : base(form)
            {
                _prevPos = pos;
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e)
            {
                int dx = e.X - _prevPos.X;
                int dy = e.Y - _prevPos.Y;

                var newPos = new Point(e.X, e.Y);
                if (newPos != _prevPos) {
                    _prevPos = newPos;
                    _form.OnMoveSelected(dx, dy);
                }

                return this;
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                return new DefaultState(_form);
            }
        }

        private class PanningState : StateBase
        {
            private Point _prevPos;

            public PanningState(MainForm form, Point startPos) : base(form)
            {
                _prevPos = startPos;
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                return new DefaultState(_form);
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e)
            {
                int dx = e.X - _prevPos.X;
                int dy = e.Y - _prevPos.Y;

                var newPos = new Point(e.X, e.Y);
                if (newPos != _prevPos) {
                    _prevPos = newPos;
                    _form.OnPan(dx, dy);
                }
                return this;
            }
        }

        private class MultiSelectState : StateBase
        {
            private Point _startPos;

            public MultiSelectState(MainForm form, Point startPos) : base(form)
            {
                _startPos = startPos;
            }

            public override StateBase MouseUp(object sender, MouseEventArgs e)
            {
                var pt = new Point(e.X, e.Y);
                _form.MultiSelect(PointMath.Min(_startPos, pt), PointMath.Max(_startPos, pt));

                _form.mainPanel.Invalidate();
                return new DefaultState(_form);
            }

            public override StateBase MouseMove(object sender, MouseEventArgs e)
            {
                _form.mainPanel.Invalidate();
                return this;
            }

            public override void Render(Graphics g, Point scrollOffset, int zoomFactor)
            {
                var pen = new Pen(Color.Black, 2);
                var pt = _form.mainPanel.PointToClient(MousePosition);
                var tl = PointMath.Min(pt, _startPos);
                var br = PointMath.Max(pt, _startPos);
                g.DrawRectangle(pen, new Rectangle(tl, PointMath.Diff(br, tl)));
            }
        }
    }
}
