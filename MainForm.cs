using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WinSub
{
    public partial class MainForm : Form
    {
        private SidebarControl _sidebar;
        private PlayerBarControl _playerBar;
        private ContentPanel _content;

        private LoginPage _loginPage;
        private HomePage _homePage;
        private ArtistsPage _artistsPage;
        private ArtistDetailPage _artistDetailPage;
        private AlbumsPage _albumsPage;
        private GenresPage _genresPage;
        private GenreDetailPage _genreDetailPage;
        private PlaylistsPage _playlistsPage;
        private PlaylistDetailPage _playlistDetailPage;
        private SearchPage _searchPage;
        private QueuePage _queuePage;
        private FavoritesPage _favoritesPage;

        private string _currentPage;
        private bool _isFullscreen;
        private FormWindowState _prevWindowState;
        private FormBorderStyle _prevBorderStyle;
        private bool _prevSidebarVisible;
        private bool _prevPlayerBarVisible;

        private const int WM_APPCOMMAND = 0x0319;
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 0x00070000;
        private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 0x000B0000;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 0x000E0000;
        private const int APPCOMMAND_MEDIA_STOP = 0x000D0000;
        private const int APPCOMMAND_MEDIA_VOLUME_UP = 0x000A0000;
        private const int APPCOMMAND_MEDIA_VOLUME_DOWN = 0x00090000;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "WinSub";
            this.MinimumSize = new Size(900, 600);
            this.Size = new Size(1050, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);

            App.Player.SetInvokeTarget(this);
            App.Playback.TrackChanged += OnTrackChanged;
            App.Playback.QueueChanged += OnQueueChanged;
            App.Player.Error += OnPlayerError;

            GlobalMediaHook.MediaPlayPause += OnGlobalPlayPause;
            GlobalMediaHook.MediaNext += OnGlobalNext;
            GlobalMediaHook.MediaPrev += OnGlobalPrev;
            GlobalMediaHook.MediaStop += OnGlobalStop;
            GlobalMediaHook.Start();

            BuildLayout();
            SetupEvents();

            ShowLogin();
        }

        private void BuildLayout()
        {
            _sidebar = new SidebarControl();
            _sidebar.Dock = DockStyle.Left;
            _sidebar.Width = 200;

            _playerBar = new PlayerBarControl();
            _playerBar.Dock = DockStyle.Bottom;

            _content = new ContentPanel();
            _content.Dock = DockStyle.Fill;

            _loginPage = new LoginPage();
            _homePage = new HomePage();
            _artistsPage = new ArtistsPage();
            _artistDetailPage = new ArtistDetailPage();
            _albumsPage = new AlbumsPage();
            _genresPage = new GenresPage();
            _genreDetailPage = new GenreDetailPage();
            _playlistsPage = new PlaylistsPage();
            _playlistDetailPage = new PlaylistDetailPage();
            _searchPage = new SearchPage();
            _queuePage = new QueuePage();
            _favoritesPage = new FavoritesPage();

            _content.RegisterPage("login", _loginPage);
            _content.RegisterPage("home", _homePage);
            _content.RegisterPage("artists", _artistsPage);
            _content.RegisterPage("artistDetail", _artistDetailPage);
            _content.RegisterPage("albums", _albumsPage);
            _content.RegisterPage("genres", _genresPage);
            _content.RegisterPage("genreDetail", _genreDetailPage);
            _content.RegisterPage("playlists", _playlistsPage);
            _content.RegisterPage("playlistDetail", _playlistDetailPage);
            _content.RegisterPage("search", _searchPage);
            _content.RegisterPage("queue", _queuePage);
            _content.RegisterPage("favorites", _favoritesPage);

            this.Controls.Add(_content);
            this.Controls.Add(_playerBar);
            this.Controls.Add(_sidebar);
        }

        private void SetupEvents()
        {
            _sidebar.BackRequested += (s, e) =>
            {
                if (_content.History.Count > 0)
                {
                    string prev = _content.History.Pop();
                    _currentPage = prev;
                    _content.ShowPage(prev);
                }
                UpdateBackButton();
            };

            _sidebar.NavigationChanged += (s, e) =>
            {
                string page = ((EventArgs<string>)e).Value;
                _content.History.Clear();
                UpdateBackButton();
                switch (page)
                {
                    case "Домой":
                        ShowPage("home");
                        _homePage.LoadData();
                        break;
                    case "Артисты":
                        ShowPage("artists");
                        _artistsPage.LoadData();
                        break;
                    case "Альбомы":
                        ShowPage("albums");
                        _albumsPage.LoadData();
                        break;
                    case "Жанры":
                        ShowPage("genres");
                        _genresPage.LoadData();
                        break;
                    case "Плейлисты":
                        ShowPage("playlists");
                        _playlistsPage.LoadData();
                        break;
                    case "Очередь":
                        ShowPage("queue");
                        _queuePage.RefreshList();
                        break;
                    case "Избранное":
                        ShowPage("favorites");
                        _favoritesPage.LoadData();
                        break;
                }
            };

            _sidebar.SearchSubmitted += (s, e) =>
            {
                string query = ((EventArgs<string>)e).Value;
                _content.History.Clear();
                UpdateBackButton();
                ShowPage("search");
                _searchPage.Search(query);
            };

            _loginPage.Connected += (s, e) =>
            {
                _sidebar.Visible = true;
                _playerBar.Visible = true;
                _content.History.Clear();
                UpdateBackButton();
                ShowPage("home");
                _homePage.LoadData();
            };

            _playerBar.PlayClicked += (s, e) => PlayOrResume();
            _playerBar.PauseClicked += (s, e) => PauseTrack();
            _playerBar.NextClicked += (s, e) => App.Playback.Next();
            _playerBar.PrevClicked += (s, e) => App.Playback.Previous();
            _playerBar.ShuffleClicked += (s, e) =>
            {
                App.Playback.ToggleShuffle();
                _playerBar.SetShuffleActive(App.Playback.Shuffle);
            };
            _playerBar.RepeatClicked += (s, e) =>
            {
                App.Playback.ToggleRepeat();
                _playerBar.SetRepeatMode(App.Playback.Repeat);
            };
            _playerBar.AutoDJClicked += (s, e) =>
            {
                App.AutoDJ.Toggle();
                _playerBar.SetAutoDJActive(App.AutoDJ.Enabled);
            };
            _playerBar.SeekPerformed += (s, e) =>
            {
                float pct = ((EventArgs<float>)e).Value;
                App.Player.SeekPercent(pct);
            };
            _playerBar.VolumeChanged += (s, e) =>
            {
                int vol = ((EventArgs<int>)e).Value;
                App.Player.Volume = vol / 100f;
            };
            _playerBar.QueueClicked += (s, e) =>
            {
                ShowPage("queue");
                _queuePage.RefreshList();
            };
            _playerBar.StarClicked += (s, e) =>
            {
                TrackItem track = App.Playback.CurrentTrack;
                if (track == null) return;
                if (track.IsStarred)
                {
                    App.Client.Unstar(track.Id);
                    track.IsStarred = false;
                }
                else
                {
                    App.Client.Star(track.Id);
                    track.IsStarred = true;
                }
            };

            _homePage.PlayTrackRequested += (s, e) =>
            {
                TrackItem track = ((EventArgs<TrackItem>)e).Value;
                List<TrackItem> tracks = new List<TrackItem>();
                foreach (Control c in _homePage.Controls)
                {
                    if (c is Panel scroll && scroll.AutoScroll)
                    {
                        foreach (Control inner in scroll.Controls)
                        {
                            if (inner is FlowLayoutPanel flow)
                            {
                                foreach (Control card in flow.Controls)
                                {
                                    if (card.Tag is TrackItem t)
                                        tracks.Add(t);
                                }
                            }
                        }
                    }
                }
                if (tracks.Count == 0) tracks.Add(track);
                int idx = tracks.IndexOf(track);
                if (idx < 0) idx = 0;
                PlayFromList(tracks, idx);
            };

            _homePage.AlbumClicked += (s, e) =>
            {
                AlbumItem album = ((EventArgs<AlbumItem>)e).Value;
                ShowAlbumTracks(album, "home");
            };

            _artistsPage.ArtistClicked += (s, e) =>
            {
                ArtistItem artist = ((EventArgs<ArtistItem>)e).Value;
                _content.History.Push("artists"); UpdateBackButton();
                ShowPage("artistDetail");
                _artistDetailPage.LoadData(artist.Id, artist.Name);
            };

            _artistDetailPage.AlbumClicked += (s, e) =>
            {
                AlbumItem album = ((EventArgs<AlbumItem>)e).Value;
                ShowAlbumTracks(album, "artistDetail");
            };

            _albumsPage.AlbumClicked += (s, e) =>
            {
                AlbumItem album = ((EventArgs<AlbumItem>)e).Value;
                ShowAlbumTracks(album, "albums");
            };

            _genresPage.GenreClicked += (s, e) =>
            {
                string genre = ((EventArgs<string>)e).Value;
                _content.History.Push("genres"); UpdateBackButton();
                ShowPage("genreDetail");
                _genreDetailPage.LoadData(genre);
            };

            _genreDetailPage.PlayAllRequested += (s, e) =>
            {
                List<TrackItem> tracks = ((EventArgs<List<TrackItem>>)e).Value;
                PlayFromList(tracks, 0);
            };

            _genreDetailPage.PlayTrackAtRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetGenreDetailTracks();
                if (tracks.Count > idx) PlayFromList(tracks, idx);
            };

            _playlistsPage.PlaylistClicked += (s, e) =>
            {
                PlaylistItem pl = ((EventArgs<PlaylistItem>)e).Value;
                _content.History.Push("playlists"); UpdateBackButton();
                ShowPage("playlistDetail");
                _playlistDetailPage.LoadData(pl.Id, pl.Name);
            };

            _playlistDetailPage.PlayAllRequested += (s, e) =>
            {
                List<TrackItem> tracks = ((EventArgs<List<TrackItem>>)e).Value;
                PlayFromList(tracks, 0);
            };

            _playlistDetailPage.PlayTrackAtRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetPlaylistDetailTracks();
                if (tracks.Count > idx) PlayFromList(tracks, idx);
            };

            _searchPage.PlayAllRequested += (s, e) =>
            {
                int selectedIndex = ((EventArgs<int>)e).Value;
                PlayFromList(_searchPage.CurrentTracks, selectedIndex);
            };

            _searchPage.ArtistClicked += (s, e) =>
            {
                ArtistItem artist = ((EventArgs<ArtistItem>)e).Value;
                _content.History.Push("search"); UpdateBackButton();
                ShowPage("artistDetail");
                _artistDetailPage.LoadData(artist.Id, artist.Name);
            };

            _searchPage.AlbumClicked += (s, e) =>
            {
                AlbumItem album = ((EventArgs<AlbumItem>)e).Value;
                ShowAlbumTracks(album, "search");
            };

            _queuePage.PlayTrackAtRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                App.Playback.PlayTrack(idx);
            };

            _queuePage.RemoveTrackRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                App.Playback.RemoveFromQueue(idx);
            };

            _queuePage.ClearRequested += (s, e) => App.Playback.ClearQueue();

            _queuePage.MoveTrackRequested += (s, e) =>
            {
                int[] indices = ((EventArgs<int[]>)e).Value;
                App.Playback.MoveTrack(indices[0], indices[1]);
            };

            _homePage.AddToQueueRequested += (s, e) =>
            {
                TrackItem track = ((EventArgs<TrackItem>)e).Value;
                App.Playback.AddToQueue(track);
            };

            _homePage.PlayNextRequested += (s, e) =>
            {
                TrackItem track = ((EventArgs<TrackItem>)e).Value;
                App.Playback.AddToQueueNext(track);
            };

            _genreDetailPage.AddToQueueRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetGenreDetailTracks();
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueue(tracks[idx]);
            };

            _genreDetailPage.PlayNextRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetGenreDetailTracks();
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueueNext(tracks[idx]);
            };

            _playlistDetailPage.AddToQueueRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetPlaylistDetailTracks();
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueue(tracks[idx]);
            };

            _playlistDetailPage.PlayNextRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetPlaylistDetailTracks();
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueueNext(tracks[idx]);
            };

            _searchPage.AddToQueueRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetSearchTracks();
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueue(tracks[idx]);
            };

            _searchPage.PlayNextRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = GetSearchTracks();
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueueNext(tracks[idx]);
            };

            _favoritesPage.PlayAllRequested += (s, e) =>
            {
                List<TrackItem> tracks = ((EventArgs<List<TrackItem>>)e).Value;
                PlayFromList(tracks, 0);
            };

            _favoritesPage.PlayTrackAtRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = _favoritesPage.Tracks;
                if (tracks.Count > idx) PlayFromList(tracks, idx);
            };

            _favoritesPage.AddToQueueRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = _favoritesPage.Tracks;
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueue(tracks[idx]);
            };

            _favoritesPage.PlayNextRequested += (s, e) =>
            {
                int idx = ((EventArgs<int>)e).Value;
                List<TrackItem> tracks = _favoritesPage.Tracks;
                if (idx >= 0 && idx < tracks.Count) App.Playback.AddToQueueNext(tracks[idx]);
            };
        }

        private void ShowPage(string name)
        {
            _currentPage = name;
            _content.ShowPage(name);
        }

        private void ShowLogin()
        {
            _sidebar.Visible = false;
            _playerBar.Visible = false;
            ShowPage("login");
        }

        private void UpdateBackButton()
        {
            _sidebar.SetBackButtonVisible(_content.History.Count > 0);
        }

        private void ShowAlbumTracks(AlbumItem album, string fromPage)
        {
            _content.History.Push(fromPage); UpdateBackButton();
            ShowPage("playlistDetail");
            _playlistDetailPage.LoadAlbumData(album.Id, album.Title + " - " + album.Artist);
        }

        private void PlayFromList(List<TrackItem> tracks, int startIndex)
        {
            App.Playback.PlayQueue(tracks, startIndex);
        }

        private void PlayOrResume()
        {
            if (App.Player.IsPlaying) return;
            if (App.Player.IsLoading) return;
            if (App.Player.Length == 0 && App.Playback.CurrentTrack != null)
            {
                App.Playback.PlayTrack(App.Playback.CurrentIndex >= 0 ? App.Playback.CurrentIndex : 0);
                return;
            }
            App.Player.Resume();
            _playerBar.SetPlaying(true);
            _playerBar.StartTimer();
        }

        private void PauseTrack()
        {
            App.Player.Pause();
            _playerBar.SetPlaying(false);
            _playerBar.StopTimer();
        }

        private void OnTrackChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(OnTrackChanged), sender, e);
                return;
            }

            TrackItem track = App.Playback.CurrentTrack;
            if (track == null) return;

            _playerBar.SetPlaying(true);
            _playerBar.StartTimer();
            _playerBar.SetQueueCount(App.Playback.Queue.Count);
            _playerBar.SetStarred(track.IsStarred);

            _sidebar.UpdateTrackInfo(track.Title, track.Artist, null);

            if (!string.IsNullOrEmpty(track.CoverArtId))
            {
                string artId = track.CoverArtId;
                App.Cache.DownloadCoverArt(artId, 170, (bmp) =>
                {
                    try
                    {
                        if (_sidebar.InvokeRequired && _sidebar.IsHandleCreated)
                        {
                            _sidebar.Invoke((Action)(() =>
                            {
                                if (App.Playback.CurrentTrack != null && App.Playback.CurrentTrack.CoverArtId == artId)
                                    _sidebar.UpdateTrackInfo(track.Title, track.Artist, bmp);
                                else
                                    bmp.Dispose();
                            }));
                        }
                        else
                        {
                            if (App.Playback.CurrentTrack != null && App.Playback.CurrentTrack.CoverArtId == artId)
                                _sidebar.UpdateTrackInfo(track.Title, track.Artist, bmp);
                            else
                                bmp.Dispose();
                        }
                    }
                    catch
                    {
                        try { bmp.Dispose(); } catch { }
                    }
                });
            }

            string scrobbleId = track.Id;
            ThreadPool.QueueUserWorkItem(o => { try { App.Client.Scrobble(scrobbleId); } catch { } });
            if (_currentPage == "queue")
                _queuePage.RefreshList();
        }

        private void OnQueueChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(OnQueueChanged), sender, e);
                return;
            }
            _playerBar.SetQueueCount(App.Playback.Queue.Count);
            if (_currentPage == "queue")
                _queuePage.RefreshList();
        }

        private void OnPlayerError(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(OnPlayerError), sender, e);
                return;
            }
            _playerBar.SetPlaying(false);
            _playerBar.StopTimer();
        }

        private void OnGlobalPlayPause()
        {
            if (InvokeRequired) { BeginInvoke((Action)OnGlobalPlayPause); return; }
            if (App.Player.IsPlaying) PauseTrack();
            else PlayOrResume();
        }

        private void OnGlobalNext()
        {
            if (InvokeRequired) { BeginInvoke((Action)OnGlobalNext); return; }
            App.Playback.Next();
        }

        private void OnGlobalPrev()
        {
            if (InvokeRequired) { BeginInvoke((Action)OnGlobalPrev); return; }
            App.Playback.Previous();
        }

        private void OnGlobalStop()
        {
            if (InvokeRequired) { BeginInvoke((Action)OnGlobalStop); return; }
            PauseTrack();
        }

        private List<TrackItem> GetGenreDetailTracks()
        {
            List<TrackItem> tracks = new List<TrackItem>();
            foreach (Control c in _genreDetailPage.Controls)
            {
                if (c is ListView lv)
                {
                    foreach (ListViewItem item in lv.Items)
                        if (item.Tag is TrackItem t) tracks.Add(t);
                }
            }
            return tracks;
        }

        private List<TrackItem> GetPlaylistDetailTracks()
        {
            List<TrackItem> tracks = new List<TrackItem>();
            foreach (Control c in _playlistDetailPage.Controls)
            {
                if (c is ListView lv)
                {
                    foreach (ListViewItem item in lv.Items)
                        if (item.Tag is TrackItem t) tracks.Add(t);
                }
            }
            return tracks;
        }

        private List<TrackItem> GetSearchTracks()
        {
            return _searchPage.CurrentTracks;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_APPCOMMAND)
            {
                int cmd = (int)(m.LParam.ToInt64() & 0xFFFF0000);
                switch (cmd)
                {
                    case APPCOMMAND_MEDIA_PLAY_PAUSE:
                        if (App.Player.IsPlaying) PauseTrack();
                        else PlayOrResume();
                        m.Result = (IntPtr)1;
                        return;
                    case APPCOMMAND_MEDIA_NEXTTRACK:
                        App.Playback.Next();
                        m.Result = (IntPtr)1;
                        return;
                    case APPCOMMAND_MEDIA_PREVIOUSTRACK:
                        App.Playback.Previous();
                        m.Result = (IntPtr)1;
                        return;
                    case APPCOMMAND_MEDIA_STOP:
                        PauseTrack();
                        m.Result = (IntPtr)1;
                        return;
                }
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Space:
                    if (App.Player.IsPlaying) PauseTrack();
                    else PlayOrResume();
                    return true;
                case Keys.Right:
                    SeekRelative(5);
                    return true;
                case Keys.Left:
                    SeekRelative(-5);
                    return true;
                case Keys.F11:
                    ToggleFullscreen();
                    return true;
                case Keys.Control | Keys.F:
                    _sidebar.FocusSearch();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SeekRelative(int seconds)
        {
            if (App.Player.Length == 0) return;
            TimeSpan cur = App.Player.CurrentTime;
            TimeSpan newTime = cur.Add(TimeSpan.FromSeconds(seconds));
            if (newTime < TimeSpan.Zero) newTime = TimeSpan.Zero;
            if (newTime > App.Player.TotalTime) newTime = App.Player.TotalTime;
            App.Player.Seek(newTime);
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                this.FormBorderStyle = _prevBorderStyle;
                this.WindowState = _prevWindowState;
                _sidebar.Visible = _prevSidebarVisible;
                _playerBar.Visible = _prevPlayerBarVisible;
                _isFullscreen = false;
            }
            else
            {
                _prevBorderStyle = this.FormBorderStyle;
                _prevWindowState = this.WindowState;
                _prevSidebarVisible = _sidebar.Visible;
                _prevPlayerBarVisible = _playerBar.Visible;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                _isFullscreen = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GlobalMediaHook.Stop();
            App.Player.Dispose();
            base.OnFormClosing(e);
        }
    }
}
