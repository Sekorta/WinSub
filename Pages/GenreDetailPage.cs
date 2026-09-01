using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class GenreDetailPage : UserControl
    {
        private Label _lblGenre;
        private Label _lblError;
        private DarkListView _list;

        public event EventHandler PlayAllRequested;
        public event EventHandler PlayTrackAtRequested;
        public event EventHandler AddToQueueRequested;
        public event EventHandler PlayNextRequested;

        public GenreDetailPage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            _lblError = new Label();
            _lblError.Dock = DockStyle.Fill;
            _lblError.ForeColor = Color.FromArgb(255, 100, 100);
            _lblError.Font = new Font("Segoe UI", 10f);
            _lblError.TextAlign = ContentAlignment.MiddleCenter;
            _lblError.Visible = false;

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 44;
            topBar.Padding = new Padding(10, 10, 10, 0);

            _lblGenre = new Label();
            _lblGenre.ForeColor = Color.White;
            _lblGenre.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _lblGenre.Dock = DockStyle.Fill;
            _lblGenre.TextAlign = ContentAlignment.MiddleLeft;

            Button btnPlayAll = new Button();
            btnPlayAll.Text = "Воспроизвести все";
            btnPlayAll.Dock = DockStyle.Right;
            btnPlayAll.Size = new Size(140, 22);
            btnPlayAll.FlatStyle = FlatStyle.Flat;
            btnPlayAll.FlatAppearance.BorderSize = 0;
            btnPlayAll.BackColor = Color.FromArgb(53, 116, 252);
            btnPlayAll.ForeColor = Color.White;
            btnPlayAll.Font = new Font("Segoe UI", 8f);
            btnPlayAll.Cursor = Cursors.Hand;
            btnPlayAll.Click += (s, e) =>
            {
                List<TrackItem> tracks = new List<TrackItem>();
                foreach (ListViewItem item in _list.Items)
                    if (item.Tag is TrackItem) tracks.Add((TrackItem)item.Tag);
                if (PlayAllRequested != null && tracks.Count > 0)
                    PlayAllRequested(this, new EventArgs<List<TrackItem>>(tracks));
            };

            topBar.Controls.Add(_lblGenre);
            topBar.Controls.Add(btnPlayAll);

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

            ToolStripMenuItem mnuStar = new ToolStripMenuItem("★ В избранное");
            mnuStar.Click += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0)
                {
                    TrackItem t = (TrackItem)_list.SelectedItems[0].Tag;
                    if (t.IsStarred) { App.Client.Unstar(t.Id); t.IsStarred = false; mnuStar.Text = "★ В избранное"; }
                    else { App.Client.Star(t.Id); t.IsStarred = true; mnuStar.Text = "★ Убрать из избранного"; }
                }
            };

            ctxMenu.Items.Add(mnuPlayNext);
            ctxMenu.Items.Add(mnuAddToQueue);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(mnuStar);
            _list.ContextMenuStrip = ctxMenu;

            Panel spacer = new Panel();
            spacer.Dock = DockStyle.Top;
            spacer.Height = 6;
            spacer.BackColor = Color.FromArgb(30, 30, 30);

            this.Controls.Add(_list);
            this.Controls.Add(spacer);
            this.Controls.Add(topBar);
        }

        public void LoadData(string genre)
        {
            _lblGenre.Text = genre;
            _list.Items.Clear();
            if (Controls.Contains(_lblError)) Controls.Remove(_lblError);
            _list.Visible = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                string g = (string)e.Argument;
                List<TrackItem> allTracks = new List<TrackItem>();
                int offset = 0;
                while (true)
                {
                    List<TrackItem> batch = App.Client.GetSongsByGenre(g, 500, offset);
                    if (batch.Count == 0) break;
                    allTracks.AddRange(batch);
                    offset += batch.Count;
                    if (batch.Count < 500) break;
                }
                e.Result = allTracks;
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    _list.Visible = false;
                    _lblError.Text = "Ошибка загрузки: " + (e.Error.InnerException ?? e.Error).Message;
                    if (!Controls.Contains(_lblError)) Controls.Add(_lblError);
                    _lblError.Dock = DockStyle.Fill;
                    _lblError.Visible = true;
                    _lblError.BringToFront();
                    return;
                }
                List<TrackItem> tracks = (List<TrackItem>)e.Result;
                foreach (TrackItem t in tracks)
                {
                    ListViewItem item = new ListViewItem(t.TrackNumber > 0 ? t.TrackNumber.ToString() : "-");
                    item.SubItems.Add(t.Title);
                    item.SubItems.Add(t.Artist);
                    item.SubItems.Add(t.Album);
                    item.SubItems.Add(t.DurationFormatted);
                    item.Tag = t;
                    _list.Items.Add(item);
                }
            };
            worker.RunWorkerAsync(genre);
        }
    }
}
