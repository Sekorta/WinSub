using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class FavoritesPage : UserControl
    {
        private Label _lblTitle;
        private DarkListView _list;
        private List<TrackItem> _tracks = new List<TrackItem>();

        public event EventHandler PlayAllRequested;
        public event EventHandler PlayTrackAtRequested;
        public event EventHandler AddToQueueRequested;
        public event EventHandler PlayNextRequested;

        public List<TrackItem> Tracks { get { return _tracks; } }

        public FavoritesPage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            _lblTitle = new Label();
            _lblTitle.Text = "Избранное";
            _lblTitle.Dock = DockStyle.Top;
            _lblTitle.Height = 40;
            _lblTitle.ForeColor = Color.White;
            _lblTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblTitle.Padding = new Padding(10, 8, 0, 0);

            Button btnPlayAll = new Button();
            btnPlayAll.Text = "Воспроизвести все";
            btnPlayAll.Dock = DockStyle.Top;
            btnPlayAll.Height = 32;
            btnPlayAll.FlatStyle = FlatStyle.Flat;
            btnPlayAll.FlatAppearance.BorderSize = 0;
            btnPlayAll.BackColor = Color.FromArgb(53, 116, 252);
            btnPlayAll.ForeColor = Color.White;
            btnPlayAll.Font = new Font("Segoe UI", 9f);
            btnPlayAll.Cursor = Cursors.Hand;
            btnPlayAll.Click += (s, e) =>
            {
                if (PlayAllRequested != null && _tracks.Count > 0)
                    PlayAllRequested(this, new EventArgs<List<TrackItem>>(_tracks));
            };

            _list = new DarkListView();
            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.GridLines = false;
            _list.BorderStyle = BorderStyle.None;
            _list.Font = new Font("Segoe UI", 9f);
            _list.BackColor = Color.FromArgb(30, 30, 30);
            _list.ForeColor = Color.FromArgb(200, 200, 200);
            _list.Columns.Add("Название", 250);
            _list.Columns.Add("Исполнитель", 200);
            _list.Columns.Add("Альбом", 200);
            _list.Columns.Add("Длит.", 60);
            _list.DoubleClick += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0 && PlayTrackAtRequested != null)
                    PlayTrackAtRequested(this, new EventArgs<int>(_list.SelectedItems[0].Index));
            };

            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ctxMenu.BackColor = Color.FromArgb(40, 40, 40);
            ctxMenu.ForeColor = Color.FromArgb(200, 200, 200);
            ctxMenu.Renderer = new DarkMenuRenderer();

            ToolStripMenuItem mnuPlayNext = new ToolStripMenuItem("Воспроизвести следующим");
            mnuPlayNext.Click += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0 && PlayNextRequested != null)
                    PlayNextRequested(this, new EventArgs<int>(_list.SelectedItems[0].Index));
            };

            ToolStripMenuItem mnuAddToQueue = new ToolStripMenuItem("Добавить в очередь");
            mnuAddToQueue.Click += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0 && AddToQueueRequested != null)
                    AddToQueueRequested(this, new EventArgs<int>(_list.SelectedItems[0].Index));
            };

            ToolStripMenuItem mnuUnstar = new ToolStripMenuItem("★ Убрать из избранного");
            mnuUnstar.Click += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0)
                {
                    TrackItem t = (TrackItem)_list.SelectedItems[0].Tag;
                    App.Client.Unstar(t.Id);
                    t.IsStarred = false;
                    LoadData();
                }
            };

            ctxMenu.Items.Add(mnuPlayNext);
            ctxMenu.Items.Add(mnuAddToQueue);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(mnuUnstar);
            _list.ContextMenuStrip = ctxMenu;

            Panel spacer = new Panel();
            spacer.Dock = DockStyle.Top;
            spacer.Height = 6;

            this.Controls.Add(_list);
            this.Controls.Add(spacer);
            this.Controls.Add(btnPlayAll);
            this.Controls.Add(_lblTitle);
        }

        public void LoadData()
        {
            _list.Items.Clear();
            _tracks.Clear();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                e.Result = App.Client.GetStarred();
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null) return;
                _tracks = (List<TrackItem>)e.Result;
                foreach (TrackItem t in _tracks)
                {
                    ListViewItem item = new ListViewItem(t.Title);
                    item.SubItems.Add(t.Artist);
                    item.SubItems.Add(t.Album);
                    item.SubItems.Add(t.DurationFormatted);
                    item.Tag = t;
                    _list.Items.Add(item);
                }
                _lblTitle.Text = "Избранное (" + _tracks.Count + ")";
            };
            worker.RunWorkerAsync();
        }
    }
}
