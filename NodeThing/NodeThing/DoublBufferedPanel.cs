namespace NodeThing
{
  using System.Windows.Forms;

  class DoublBufferedPanel : Panel
  {
    public DoublBufferedPanel()
    {
      SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }
  }

}
