using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NodeThing
{
    public class Node
    {
        public Node()
        {
            inputs = new List<Connection>();
        }

        public void render(Graphics g)
        {
            g.ResetTransform();
            g.TranslateTransform(pos.X, pos.Y);

            var brush = new SolidBrush(Color.LightGray);
            g.FillEllipse(brush, 0, 0, 50, 50);
            g.FillEllipse(brush, 50, 0, 50, 50);
            g.FillRectangle(brush, 25, 0, 50, 25);

            brush = new SolidBrush(Color.GhostWhite);
            g.FillRectangle(brush, 0, 25, 100, 50);

            var pen = new Pen(Color.DarkGray, 1);
            g.DrawArc(pen, 0, 0, 50, 50, -180, 90);
            g.DrawArc(pen, 50, 0, 50, 50, -0, -91);
            g.DrawLine(pen, 25, 0, 75, 0);
            g.DrawLine(pen, 0, 25, 0, 75);
            g.DrawLine(pen, 100, 25, 100, 75);
            g.DrawLine(pen, 0, 75, 100, 75);
            g.DrawLine(pen, 0, 25, 100, 25);

            brush = new SolidBrush(Color.Yellow);
            g.FillEllipse(brush, -5, 50, 10, 10);
            g.DrawEllipse(pen, -5, 50, 10, 10);

            var format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            brush = new SolidBrush(Color.Black);
            var font = new Font("Arial", 7);
            //g.DrawString(name, font, brush, 10, 55, format);
            g.DrawString(name, font, brush, 20, 5);
        }

        public string name { get; set; }
        public Point pos { get; set; }

        public List<Connection> inputs { get; set; }
        public Connection output { get; set; }

    }
}
