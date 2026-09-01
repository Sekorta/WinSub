using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class QueuePage : UserControl
    {
        private Label _lblTitle;
        private Label _lblInfo;
        private DarkListView _list;
        private Button _btnClear;
        private int _dragFromIndex = -1;

        public event EventHandler PlayTrackAtRequested;
        public event EventHandler RemoveTrackRequested;
        public event EventHandler ClearRequested;
        public event EventHandler MoveTrackRequested;

        public QueuePage()
        {
            InitializeUI();
            App.Playback.QueueChanged += (s, e) => RefreshList();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 44;

            _lblTitle = new Label();
            _lblTitle.Text = "Очередь";
            _lblTitle.ForeColor = Color.White;
            _lblTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblTitle.Location = new Point(10, 10);
            _lblTitle.AutoSize = true;

            _lblInfo = new Label();
            _lblInfo.ForeColor = Color.FromArgb(120, 120, 120);
            _lblInfo.Font = new Font("Segoe UI", 9f);
            _lblInfo.Location = new Point(120, 14);
            _lblInfo.AutoSize = true;

            _btnClear = new Button();
            _btnClear.Text = "Очистить";
            _btnClear.Location = new Point(700, 10);
            _btnClear.Size = new Size(80, 28);
            _btnClear.FlatStyle = FlatStyle.Flat;
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.BackColor = Color.FromArgb(60, 60, 60);
            _btnClear.ForeColor = Color.FromArgb(200, 200, 200);
            _btnClear.Font = new Font("Segoe UI", 8f);
            _btnClear.Cursor = Cursors.Hand;
            _btnClear.Click += (s, e) =>
            {
                if (ClearRequested != null)
                    ClearRequested(this, EventArgs.Empty);
            };

            topBar.Controls.Add(_lblTitle);
            topBar.Controls.Add(_lblInfo);
            topBar.Controls.Add(_btnClear);

            _list = new DarkListView();
            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.GridLines = false;
            _list.BorderStyle = BorderStyle.None;
            _list.Font = new Font("Segoe UI", 9f);
            _list.BackColor = Color.FromArgb(30, 30, 30);
            _list.ForeColor = Color.FromArgb(200, 200, 200);
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.AllowDrop = true;
            _list.Columns.Add("#", 40);
            _list.Columns.Add("Название", 250);
            _list.Columns.Add("Исполнитель", 200);
            _list.Columns.Add("Альбом", 200);
            _list.Columns.Add("Длит.", 60);
            _list.DoubleClick += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0 && PlayTrackAtRequested != null)
                    PlayTrackAtRequested(this, new EventArgs<int>(_list.SelectedItems[0].Index));
            };
            _list.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && _list.SelectedItems.Count > 0 && RemoveTrackRequested != null)
                    RemoveTrackRequested(this, new EventArgs<int>(_list.SelectedItems[0].Index));
            };

            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ctxMenu.BackColor = Color.FromArgb(40, 40, 40);
            ctxMenu.ForeColor = Color.FromArgb(200, 200, 200);
            ctxMenu.Renderer = new DarkMenuRenderer();

            ToolStripMenuItem mnuRemove = new ToolStripMenuItem("Удалить из очереди");
            mnuRemove.Click += (s2, e2) =>
            {
                if (_list.SelectedItems.Count > 0 && RemoveTrackRequested != null)
                    RemoveTrackRequested(this, new EventArgs<int>(_list.SelectedItems[0].Index));
            };
            ctxMenu.Items.Add(mnuRemove);
            _list.ContextMenuStrip = ctxMenu;

            _list.ItemDrag += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0)
                {
                    _dragFromIndex = _list.SelectedItems[0].Index;
                    _list.DoDragDrop(_list.SelectedItems[0].Index.ToString(), DragDropEffects.Move);
                }
            };
            _list.DragEnter += (s, e) =>
            {
                e.Effect = e.Data.GetDataPresent(typeof(string)) ? DragDropEffects.Move : DragDropEffects.None;
            };
            _list.DragOver += (s, e) =>
            {
                e.Effect = DragDropEffects.Move;
                Point pt = _list.PointToClient(Cursor.Position);
                ListViewItem hoverItem = _list.GetItemAt(pt.X, pt.Y);
                if (hoverItem != null)
                {
                    _list.SelectedIndices.Clear();
                    hoverItem.Selected = true;
                    hoverItem.Focused = true;
                }
            };
            _list.DragDrop += (s, e) =>
            {
                if (_dragFromIndex < 0) return;
                Point pt = _list.PointToClient(Cursor.Position);
                ListViewItem targetItem = _list.GetItemAt(pt.X, pt.Y);
                if (targetItem == null) { _dragFromIndex = -1; return; }
                int toIndex = targetItem.Index;
                if (_dragFromIndex == toIndex) { _dragFromIndex = -1; return; }
                if (MoveTrackRequested != null)
                    MoveTrackRequested(this, new EventArgs<int[]>(new int[] { _dragFromIndex, toIndex }));
                _dragFromIndex = -1;
            };

            Controls.Add(_list);
            Controls.Add(topBar);
        }

        public void RefreshList()
        {
            if (InvokeRequired)
            {
                Invoke((Action)RefreshList);
                return;
            }

            _list.Items.Clear();
            List<TrackItem> queue = App.Playback.Queue;
            int currentIdx = App.Playback.CurrentIndex;

            _lblInfo.Text = string.Format("{0} треков", queue.Count);

            for (int i = 0; i < queue.Count; i++)
            {
                TrackItem t = queue[i];
                ListViewItem item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(t.Title);
                item.SubItems.Add(t.Artist);
                item.SubItems.Add(t.Album);
                item.SubItems.Add(t.DurationFormatted);
                item.Tag = i;

                if (i == currentIdx)
                    item.BackColor = Color.FromArgb(53, 116, 252);
                else if (i % 2 == 0)
                    item.BackColor = Color.FromArgb(33, 33, 33);
                else
                    item.BackColor = Color.FromArgb(30, 30, 30);

                _list.Items.Add(item);
            }
        }
        public void ScrollToCurrent()
        {
            int currentIdx = App.Playback.CurrentIndex;
            if (currentIdx >= 0 && currentIdx < _list.Items.Count)
            {
                _list.Items[currentIdx].EnsureVisible();
                _list.Items[currentIdx].Selected = true;
            }
        }
    }
}
