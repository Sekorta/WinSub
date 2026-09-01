using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class DarkListView : ListView
    {
        public DarkListView()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}
