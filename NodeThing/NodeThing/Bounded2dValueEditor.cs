using System;
using System.Drawing;
using System.Windows.Forms;

namespace NodeThing
{
    public partial class Bounded2dValueEditor : UserControl
    {

        public event EventHandler ValueChanged;
        private NodePropertyBase _property;

        public Bounded2dValueEditor(NodePropertyBase property, EventHandler handler)
        {
            InitializeComponent();
            _property = property;
            ValueChanged += handler;
        }

        private void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.FillRectangle(Brushes.BurlyWood, 0, 0, drawPanel.Width, drawPanel.Height);
            var pt = drawPanel.PointToClient(new Point(MousePosition.X, MousePosition.Y));
            var pen = new Pen(Color.Black);
            g.DrawLine(pen, 0, pt.Y, drawPanel.Width, pt.Y);
            g.DrawLine(pen, pt.X, 0, pt.X, drawPanel.Height);
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                ProcessMouseEvent(e);
            }
        }

        public static float Clamp(float value, float minValue, float maxValue)
        {
            return Math.Min(maxValue, Math.Max(value, minValue));
        }

        private void ProcessMouseEvent(MouseEventArgs e)
        {
            var prop = (NodeProperty<Tuple<float, float>>)_property;
            float valueX = prop.Min.Item1 + e.X / (float)drawPanel.Width * (prop.Max.Item1 - prop.Min.Item1);
            float valueY = prop.Min.Item2 + e.Y / (float)drawPanel.Height * (prop.Max.Item2 - prop.Min.Item2);

            valueX = Clamp(valueX, prop.Min.Item1, prop.Max.Item1);
            valueY = Clamp(valueY, prop.Min.Item2, prop.Max.Item2);

            textBox1.Text = valueX.ToString();
            textBox2.Text = valueY.ToString();

            ValueChanged(this, new EventArgs<Tuple<float, float>> { Value = new Tuple<float, float>(valueX, valueY) });
            drawPanel.Invalidate();
        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
        }
    }
}
