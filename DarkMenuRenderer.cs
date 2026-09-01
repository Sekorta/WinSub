using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            Color bgColor = e.Item.Selected ? Color.FromArgb(60, 60, 60) : Color.FromArgb(40, 40, 40);
            using (SolidBrush brush = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(brush, rc);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(200, 200, 200);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(70, 70, 70)))
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
            }
        }
    }
}
