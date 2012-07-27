using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace NodeThing
{
    [DataContract(IsReference = true)]
    public class Node
    {
        public Node()
        {
            Inputs = new List<Connection>();
            Properties = new NodeProperties();
        }

        private int CalcWidth(Graphics g)
        {
            const int defaultWidth = 100;
            var maxInput = 0;
            foreach (var input in Inputs) {
                var bounds = g.MeasureString(input.Name, _font);
                maxInput = Math.Max(maxInput, (int)(bounds.Width + 0.5));
            }

            if (Output != null) {
                var bounds = g.MeasureString(Output.Name, _font);
                maxInput += _padding + (int)bounds.Width + 4 * _connectionRadius;
            } else {
                maxInput += 2 * _connectionRadius;
            }

            return Math.Max(defaultWidth, maxInput);
        }

        public void AddInput(string name, Connection.Type type)
        {
            Inputs.Add(new Connection { Name = name, DataType = type, Direction = Connection.Io.Input, Node = this, Slot = Inputs.Count });
        }

        public void SetOutput(string name, Connection.Type type)
        {
            Output = new Connection { Name = name, DataType = type, Direction = Connection.Io.Output, Node = this, Slot = 0 };
        }

        public Connection PointInsideConnection(Point pt)
        {
            var x = Pos.X;
            var y = Pos.Y;

            for (var i = 0; i < Inputs.Count; ++i) {
                var topY = _headerHeight + _padding + i * _connectionHeight;
                var bottomY = topY + _connectionHeight;
                var middleY = (topY + bottomY) / 2;

                var dx = pt.X - x;
                var dy = pt.Y - (y + middleY);
                if (Math.Sqrt(dx * dx + dy * dy) < _connectionRadius)
                    return Inputs[i];
            }

            if (Output != null) {
                var dx = pt.X - (x + _width);
                var dy = pt.Y - (y + _headerHeight + _height / 2);
                if (Math.Sqrt(dx * dx + dy * dy) < _connectionRadius)
                    return Output;
            }

            return null;
        }

        public Tuple<bool, Point> ConnectionPos(Connection.Io io, int slot)
        {
            // Check for invalid inputs
            if (io == Connection.Io.Output && Output == null || io == Connection.Io.Input && slot >= Inputs.Count)
                return new Tuple<bool, Point>(false, new Point(0, 0));

            if (io == Connection.Io.Output)
                return new Tuple<bool, Point>(true, new Point(Pos.X + _width, Pos.Y + _headerHeight + _height / 2));

            return new Tuple<bool, Point>(true, new Point(Pos.X, Pos.Y + _headerHeight + _padding + _connectionHeight / 2 + slot * _connectionHeight));
        }

        public bool PointInsideBody(Point pt)
        {
            var x = Pos.X;
            var y = Pos.Y;

            var dx = pt.X - (x + _connectionRadius);
            var dy = pt.Y - (y + _connectionRadius);
            var leftDist = Math.Sqrt(dx * dx + dy * dy);

            dx = pt.X - (x + _width - _connectionRadius);
            var rightDist = Math.Sqrt(dx * dx + dy * dy);

            var middle = new Rectangle(x + _connectionRadius, y, _width - 2 * _connectionRadius, _headerHeight);
            var body = new Rectangle(Pos.X, Pos.Y + _headerHeight, _width, _height);

            return body.Contains(pt) || middle.Contains(pt) || leftDist < _connectionRadius || rightDist < _connectionRadius;
        }

        public void Render(Graphics g)
        {
            g.TranslateTransform(Pos.X, Pos.Y);

            if (_needsUpdate) {
                _needsUpdate = false;
                var numSlots = Math.Max(Inputs.Count, Output != null ? 1 : 0);
                _height = 2 * _padding + _connectionHeight * numSlots;
                _width = CalcWidth(g);
            }

            // Draw header
            var brush = new SolidBrush(Color.LightGray);
            g.FillEllipse(brush, 0, 0, _headerHeight * 2, _headerHeight * 2);
            g.FillEllipse(brush, _width - 2 * _headerHeight, 0, _headerHeight * 2, _headerHeight * 2);
            g.FillRectangle(brush, _headerHeight, 0, _width - 2 * _headerHeight, _headerHeight);

            // Draw main body
            brush = new SolidBrush(Selected ? Color.Khaki : Color.GhostWhite);
            g.FillRectangle(brush, 0, _headerHeight, _width, _height);

            // Draw outline
            var pen = new Pen(Color.DarkGray, 1);
            g.DrawArc(pen, 0, 0, 2 * _headerHeight, 2 * _headerHeight, -180, 90);
            g.DrawArc(pen, _width - 2 * _headerHeight, 0, 2 * _headerHeight, 2 * _headerHeight, -0, -91);
            g.DrawLine(pen, _headerHeight, 0, _width - _headerHeight, 0);
            g.DrawLine(pen, 0, _headerHeight, 0, _headerHeight + _height);
            g.DrawLine(pen, _width, _headerHeight, _width, _headerHeight + _height);
            g.DrawLine(pen, 0, _headerHeight + _height, _width, _headerHeight + _height);
            g.DrawLine(pen, 0, _headerHeight, _width, _headerHeight);

            var headerFormat = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
            var inputFormat = new StringFormat { LineAlignment = StringAlignment.Center };

            // Draw connections
            var connectionBrush = new SolidBrush(Color.Yellow);
            var hoverBrush = new SolidBrush(Color.MediumSeaGreen);
            var errorBrush = new SolidBrush(Color.OrangeRed);

            for (var i = 0; i < Inputs.Count; ++i) {
                var con = Inputs[i];
                var conBrush = con.Hovering ? hoverBrush : con.ErrorState ? errorBrush : connectionBrush;
                var topY = _headerHeight + _padding + i * _connectionHeight;
                var bottomY = topY + _connectionHeight;
                var middleY = (topY + bottomY) / 2;
                g.FillEllipse(conBrush, -_connectionRadius, middleY - _connectionRadius, _connectionDiameter, _connectionDiameter);
                g.DrawEllipse(pen, -_connectionRadius, middleY - _connectionRadius, _connectionDiameter, _connectionDiameter);
                var r = 2 * _connectionRadius;
                g.DrawString(Inputs[i].Name, _font, _blackBrush, new RectangleF(r, topY, _width - r, _connectionHeight),
                             inputFormat);
            }

            if (Output != null) {
                var conBrush = Output.Hovering ? hoverBrush : Output.ErrorState ? errorBrush : connectionBrush;
                var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far };

                var y = _headerHeight + _height / 2 - _connectionRadius;
                g.FillEllipse(conBrush, _width - _connectionRadius, y, _connectionDiameter, _connectionDiameter);
                g.DrawEllipse(pen, _width - _connectionRadius, y, _connectionDiameter, _connectionDiameter);
                var r = 2 * _connectionRadius;
                g.DrawString(Output.Name, _font, _blackBrush,
                             new RectangleF(0, _headerHeight + _height / 2 - _connectionHeight / 2, _width - r, _connectionHeight),
                             format);
            }

            g.DrawString(Name, _font, _blackBrush, new RectangleF(0, 0, _width, _headerHeight), headerFormat);
            g.ResetTransform();
        }

        [OnDeserialized]
        private void OnDeserializerd(StreamingContext sc)
        {
            _blackBrush = new SolidBrush(Color.Black);
            _font = new Font("Arial", 7);

            _padding = 10;
            _connectionHeight = 25;
            _connectionRadius = 5;
            _connectionDiameter = 2 * _connectionRadius;
            _headerHeight = 20;

            _needsUpdate = true;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Point Pos { get; set; }

        [DataMember]
        public List<Connection> Inputs { get; set; }

        [DataMember]
        public Connection Output { get; set; }

        [DataMember]
        public NodeProperties Properties { get; set; }

        public bool Selected { get; set; }

        private Brush _blackBrush = new SolidBrush(Color.Black);
        private Font _font = new Font("Arial", 7);

        private static int _padding = 10;
        private static int _connectionHeight = 25;
        private static int _connectionRadius = 5;
        private static int _connectionDiameter = 2 * _connectionRadius;
        private static int _headerHeight = 20;
        private int _width;
        private int _height;
        private bool _needsUpdate = true;
    }
}
