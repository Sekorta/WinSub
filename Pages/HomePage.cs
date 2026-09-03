using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class HomePage : UserControl
    {
        private Label _lblRecentlyAdded;
        private FlowLayoutPanel _recentlyAddedPanel;
        private Label _lblRandom;
        private FlowLayoutPanel _randomPanel;
        private Panel _scrollPanel;

        public event EventHandler PlayTrackRequested;
        public event EventHandler AlbumClicked;
        public event EventHandler AddToQueueRequested;
        public event EventHandler PlayNextRequested;

        public HomePage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            _scrollPanel = new Panel();
            _scrollPanel.Dock = DockStyle.Fill;
            _scrollPanel.AutoScroll = true;
            _scrollPanel.BackColor = Color.FromArgb(30, 30, 30);
            _scrollPanel.Padding = new Padding(20);

            _lblRecentlyAdded = new Label();
            _lblRecentlyAdded.Text = "Недавно добавленные";
            _lblRecentlyAdded.Dock = DockStyle.Top;
            _lblRecentlyAdded.Height = 36;
            _lblRecentlyAdded.ForeColor = Color.White;
            _lblRecentlyAdded.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblRecentlyAdded.Padding = new Padding(0, 8, 0, 0);

            _recentlyAddedPanel = new FlowLayoutPanel();
            _recentlyAddedPanel.Dock = DockStyle.Top;
            _recentlyAddedPanel.Height = 220;
            _recentlyAddedPanel.FlowDirection = FlowDirection.LeftToRight;
            _recentlyAddedPanel.AutoScroll = false;
            _recentlyAddedPanel.WrapContents = false;

            _lblRandom = new Label();
            _lblRandom.Text = "Случайные треки";
            _lblRandom.Dock = DockStyle.Top;
            _lblRandom.Height = 46;
            _lblRandom.ForeColor = Color.White;
            _lblRandom.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblRandom.Padding = new Padding(0, 18, 0, 0);

            _randomPanel = new FlowLayoutPanel();
            _randomPanel.Dock = DockStyle.Top;
            _randomPanel.Height = 400;
            _randomPanel.FlowDirection = FlowDirection.LeftToRight;
            _randomPanel.AutoScroll = true;
            _randomPanel.WrapContents = true;

            _scrollPanel.Controls.Add(_randomPanel);
            _scrollPanel.Controls.Add(_lblRandom);
            _scrollPanel.Controls.Add(_recentlyAddedPanel);
            _scrollPanel.Controls.Add(_lblRecentlyAdded);

            this.Controls.Add(_scrollPanel);
        }

        public void LoadData()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                List<AlbumItem> albums = App.Client.GetAlbumList("recent", 5);
                List<TrackItem> tracks = App.Client.GetRandomSongs(10);
                e.Result = new object[] { albums, tracks };
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null) return;
                object[] result = (object[])e.Result;
                List<AlbumItem> albums = (List<AlbumItem>)result[0];
                List<TrackItem> tracks = (List<TrackItem>)result[1];

                _recentlyAddedPanel.SuspendLayout();
                _recentlyAddedPanel.Controls.Clear();
                foreach (AlbumItem album in albums)
                {
                    Control card = CreateAlbumCard(album);
                    _recentlyAddedPanel.Controls.Add(card);
                }
                _recentlyAddedPanel.ResumeLayout();

                _randomPanel.SuspendLayout();
                _randomPanel.Controls.Clear();
                foreach (TrackItem track in tracks)
                {
                    Control row = CreateTrackRow(track);
                    _randomPanel.Controls.Add(row);
                }
                _randomPanel.ResumeLayout();
            };
            worker.RunWorkerAsync();
        }

        private Control CreateAlbumCard(AlbumItem album)
        {
            Panel card = new Panel();
            card.Size = new Size(150, 200);
            card.Margin = new Padding(6);
            card.BackColor = Color.FromArgb(40, 40, 40);
            card.Cursor = Cursors.Hand;
            card.Tag = album;

            PictureBox pic = new PictureBox();
            pic.Size = new Size(150, 150);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = Color.FromArgb(50, 50, 50);
            pic.Dock = DockStyle.Top;
            pic.Tag = album;

            Label lblTitle = new Label();
            lblTitle.Text = album.Title;
            lblTitle.ForeColor = Color.FromArgb(200, 200, 200);
            lblTitle.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblTitle.Dock = DockStyle.Bottom;
            lblTitle.Height = 22;
            lblTitle.TextAlign = ContentAlignment.TopCenter;

            Label lblArtist = new Label();
            lblArtist.Text = album.Artist;
            lblArtist.ForeColor = Color.FromArgb(120, 120, 120);
            lblArtist.Font = new Font("Segoe UI", 7f);
            lblArtist.Dock = DockStyle.Bottom;
            lblArtist.Height = 18;
            lblArtist.TextAlign = ContentAlignment.TopCenter;

            card.Controls.Add(lblArtist);
            card.Controls.Add(lblTitle);
            card.Controls.Add(pic);

            EventHandler clickHandler = (s, e) =>
            {
                if (AlbumClicked != null)
                    AlbumClicked(this, new EventArgs<AlbumItem>(album));
            };
            pic.Click += clickHandler;
            card.Click += clickHandler;
            lblTitle.Click += clickHandler;
            lblArtist.Click += clickHandler;

            if (!string.IsNullOrEmpty(album.CoverArtId))
            {
                App.Cache.DownloadCoverArt(album.CoverArtId, 150, (bmp) =>
                {
                    pic.Invoke((Action)(() => pic.Image = bmp));
                });
            }

            return card;
        }

        private Control CreateTrackRow(TrackItem track)
        {
            Panel row = new Panel();
            row.Size = new Size(350, 36);
            row.Margin = new Padding(3);
            row.BackColor = Color.FromArgb(35, 35, 35);
            row.Cursor = Cursors.Hand;
            row.Tag = track;

            PictureBox pic = new PictureBox();
            pic.Size = new Size(32, 32);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = Color.FromArgb(50, 50, 50);
            pic.Location = new Point(2, 2);

            Label lblNum = new Label();
            lblNum.ForeColor = Color.FromArgb(100, 100, 100);
            lblNum.Font = new Font("Segoe UI", 8f);
            lblNum.Location = new Point(40, 8);
            lblNum.AutoSize = true;

            Label lblTitle = new Label();
            lblTitle.Text = track.Title;
            lblTitle.ForeColor = Color.FromArgb(200, 200, 200);
            lblTitle.Font = new Font("Segoe UI", 8.5f);
            lblTitle.Location = new Point(40, 3);
            lblTitle.Size = new Size(160, 16);

            Label lblArtist = new Label();
            lblArtist.Text = track.Artist;
            lblArtist.ForeColor = Color.FromArgb(120, 120, 120);
            lblArtist.Font = new Font("Segoe UI", 7.5f);
            lblArtist.Location = new Point(40, 19);
            lblArtist.Size = new Size(160, 14);

            Label lblDuration = new Label();
            lblDuration.Text = track.DurationFormatted;
            lblDuration.ForeColor = Color.FromArgb(100, 100, 100);
            lblDuration.Font = new Font("Segoe UI", 8f);
            lblDuration.Location = new Point(300, 8);
            lblDuration.AutoSize = true;

            row.Controls.Add(pic);
            row.Controls.Add(lblNum);
            row.Controls.Add(lblTitle);
            row.Controls.Add(lblArtist);
            row.Controls.Add(lblDuration);

            EventHandler clickHandler = (s, e) =>
            {
                if (PlayTrackRequested != null)
                    PlayTrackRequested(this, new EventArgs<TrackItem>(track));
            };
            row.Click += clickHandler;
            pic.Click += clickHandler;
            lblTitle.Click += clickHandler;

            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ctxMenu.BackColor = Color.FromArgb(40, 40, 40);
            ctxMenu.ForeColor = Color.FromArgb(200, 200, 200);
            ctxMenu.Renderer = new DarkMenuRenderer();

            TrackItem capturedTrack = track;
            ToolStripMenuItem mnuPlayNext = new ToolStripMenuItem("Воспроизвести следующим");
            mnuPlayNext.Click += (s2, e2) =>
            {
                if (PlayNextRequested != null)
                    PlayNextRequested(this, new EventArgs<TrackItem>(capturedTrack));
            };

            ToolStripMenuItem mnuAddToQueue = new ToolStripMenuItem("Добавить в очередь");
            mnuAddToQueue.Click += (s2, e2) =>
            {
                if (AddToQueueRequested != null)
                    AddToQueueRequested(this, new EventArgs<TrackItem>(capturedTrack));
            };

            ToolStripMenuItem mnuStar = new ToolStripMenuItem("★ В избранное");
            mnuStar.Click += (s2, e2) =>
            {
                if (capturedTrack.IsStarred) { App.Client.Unstar(capturedTrack.Id); capturedTrack.IsStarred = false; mnuStar.Text = "★ В избранное"; }
                else { App.Client.Star(capturedTrack.Id); capturedTrack.IsStarred = true; mnuStar.Text = "★ Убрать из избранного"; }
            };

            ctxMenu.Items.Add(mnuPlayNext);
            ctxMenu.Items.Add(mnuAddToQueue);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(mnuStar);
            row.ContextMenuStrip = ctxMenu;
            pic.ContextMenuStrip = ctxMenu;
            lblTitle.ContextMenuStrip = ctxMenu;

            if (!string.IsNullOrEmpty(track.CoverArtId))
            {
                App.Cache.DownloadCoverArt(track.CoverArtId, 32, (bmp) =>
                {
                    pic.Invoke((Action)(() => pic.Image = bmp));
                });
            }

            return row;
        }
    }
}
