using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinSub
{
    public class SearchPage : UserControl
    {
        private TabControl _tabs;
        private DarkListView _tracksList;
        private DarkListView _artistsList;
        private DarkListView _albumsList;
        private Label _lblQuery;
        private List<TrackItem> _currentTracks = new List<TrackItem>();

        public List<TrackItem> CurrentTracks { get { return _currentTracks; } }

        public event EventHandler PlayAllRequested;
        public event EventHandler ArtistClicked;
        public event EventHandler AlbumClicked;
        public event EventHandler AddToQueueRequested;
        public event EventHandler PlayNextRequested;

        public SearchPage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            _lblQuery = new Label();
            _lblQuery.Dock = DockStyle.Top;
            _lblQuery.Height = 40;
            _lblQuery.ForeColor = Color.White;
            _lblQuery.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblQuery.Padding = new Padding(10, 8, 0, 0);

            _tabs = new TabControl();
            _tabs.Dock = DockStyle.Fill;
            _tabs.BackColor = Color.FromArgb(30, 30, 30);
            _tabs.ForeColor = Color.FromArgb(200, 200, 200);
            _tabs.Font = new Font("Segoe UI", 9f);

            TabPage tabTracks = new TabPage("Треки");
            TabPage tabArtists = new TabPage("Артисты");
            TabPage tabAlbums = new TabPage("Альбомы");

            _tracksList = CreateTrackList();
            _artistsList = CreateArtistList();
            _albumsList = CreateAlbumList();

            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ctxMenu.BackColor = Color.FromArgb(40, 40, 40);
            ctxMenu.ForeColor = Color.FromArgb(200, 200, 200);
            ctxMenu.Renderer = new DarkMenuRenderer();

            ToolStripMenuItem mnuPlayNext = new ToolStripMenuItem("Воспроизвести следующим");
            mnuPlayNext.Click += (s, e) =>
            {
                if (_tracksList.SelectedItems.Count > 0 && PlayNextRequested != null)
                    PlayNextRequested(this, new EventArgs<int>(_tracksList.SelectedItems[0].Index));
            };

            ToolStripMenuItem mnuAddToQueue = new ToolStripMenuItem("Добавить в очередь");
            mnuAddToQueue.Click += (s, e) =>
            {
                if (_tracksList.SelectedItems.Count > 0 && AddToQueueRequested != null)
                    AddToQueueRequested(this, new EventArgs<int>(_tracksList.SelectedItems[0].Index));
            };

            ToolStripMenuItem mnuStar = new ToolStripMenuItem("★ В избранное");
            mnuStar.Click += (s, e) =>
            {
                if (_tracksList.SelectedItems.Count > 0)
                {
                    TrackItem t = (TrackItem)_tracksList.SelectedItems[0].Tag;
                    if (t.IsStarred) { App.Client.Unstar(t.Id); t.IsStarred = false; mnuStar.Text = "★ В избранное"; }
                    else { App.Client.Star(t.Id); t.IsStarred = true; mnuStar.Text = "★ Убрать из избранного"; }
                }
            };

            ctxMenu.Items.Add(mnuPlayNext);
            ctxMenu.Items.Add(mnuAddToQueue);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(mnuStar);
            _tracksList.ContextMenuStrip = ctxMenu;

            tabTracks.Controls.Add(_tracksList);
            tabArtists.Controls.Add(_artistsList);
            tabAlbums.Controls.Add(_albumsList);

            _tabs.TabPages.Add(tabTracks);
            _tabs.TabPages.Add(tabArtists);
            _tabs.TabPages.Add(tabAlbums);

            this.Controls.Add(_tabs);
            this.Controls.Add(_lblQuery);
        }

        public void Search(string query)
        {
            _lblQuery.Text = "Поиск: " + query;
            _tracksList.Items.Clear();
            _artistsList.Items.Clear();
            _albumsList.Items.Clear();
            _currentTracks.Clear();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                e.Result = App.Client.Search((string)e.Argument);
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null) return;
                SearchResults results = (SearchResults)e.Result;

                _currentTracks = results.Songs;
                foreach (TrackItem t in results.Songs)
                {
                    ListViewItem item = new ListViewItem(t.Title);
                    item.SubItems.Add(t.Artist);
                    item.SubItems.Add(t.Album);
                    item.SubItems.Add(t.DurationFormatted);
                    item.Tag = t;
                    _tracksList.Items.Add(item);
                }

                foreach (ArtistItem a in results.Artists)
                {
                    ListViewItem item = new ListViewItem(a.Name);
                    item.SubItems.Add(a.AlbumCount.ToString());
                    item.Tag = a;
                    _artistsList.Items.Add(item);
                }

                foreach (AlbumItem a in results.Albums)
                {
                    ListViewItem item = new ListViewItem(a.Title);
                    item.SubItems.Add(a.Artist);
                    item.SubItems.Add(a.Year > 0 ? a.Year.ToString() : "");
                    item.Tag = a;
                    _albumsList.Items.Add(item);
                }
            };
            worker.RunWorkerAsync(query);
        }

        private DarkListView CreateTrackList()
        {
            DarkListView lv = new DarkListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.BorderStyle = BorderStyle.None;
            lv.Font = new Font("Segoe UI", 9f);
            lv.BackColor = Color.FromArgb(30, 30, 30);
            lv.ForeColor = Color.FromArgb(200, 200, 200);
            lv.Columns.Add("Название", 250);
            lv.Columns.Add("Исполнитель", 200);
            lv.Columns.Add("Альбом", 200);
            lv.Columns.Add("Длит.", 60);
            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0)
                {
                    int selectedIndex = lv.SelectedItems[0].Index;
                    if (PlayAllRequested != null && _currentTracks.Count > 0)
                        PlayAllRequested(this, new EventArgs<int>(selectedIndex));
                }
            };
            return lv;
        }

        private DarkListView CreateArtistList()
        {
            DarkListView lv = new DarkListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.BorderStyle = BorderStyle.None;
            lv.Font = new Font("Segoe UI", 9f);
            lv.BackColor = Color.FromArgb(30, 30, 30);
            lv.ForeColor = Color.FromArgb(200, 200, 200);
            lv.Columns.Add("Артист", 300);
            lv.Columns.Add("Альбомов", 100);
            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0 && ArtistClicked != null)
                    ArtistClicked(this, new EventArgs<ArtistItem>((ArtistItem)lv.SelectedItems[0].Tag));
            };
            return lv;
        }

        private DarkListView CreateAlbumList()
        {
            DarkListView lv = new DarkListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.BorderStyle = BorderStyle.None;
            lv.Font = new Font("Segoe UI", 9f);
            lv.BackColor = Color.FromArgb(30, 30, 30);
            lv.ForeColor = Color.FromArgb(200, 200, 200);
            lv.Columns.Add("Альбом", 250);
            lv.Columns.Add("Артист", 200);
            lv.Columns.Add("Год", 80);
            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0 && AlbumClicked != null)
                    AlbumClicked(this, new EventArgs<AlbumItem>((AlbumItem)lv.SelectedItems[0].Tag));
            };
            return lv;
        }
    }
}
