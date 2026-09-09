using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinSub
{
    public class ArtistDetailPage : UserControl
    {
        private Label _lblArtistName;
        private FlowLayoutPanel _albumsPanel;
        private Panel _scrollPanel;
        private string _artistId;

        public event EventHandler AlbumClicked;

        public ArtistDetailPage()
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

            _lblArtistName = new Label();
            _lblArtistName.Dock = DockStyle.Top;
            _lblArtistName.Height = 40;
            _lblArtistName.ForeColor = Color.White;
            _lblArtistName.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblArtistName.Padding = new Padding(0, 8, 0, 0);

            _albumsPanel = new FlowLayoutPanel();
            _albumsPanel.Dock = DockStyle.Top;
            _albumsPanel.Height = 500;
            _albumsPanel.FlowDirection = FlowDirection.LeftToRight;
            _albumsPanel.WrapContents = true;
            _albumsPanel.AutoSize = false;

            _scrollPanel.Controls.Add(_albumsPanel);
            _scrollPanel.Controls.Add(_lblArtistName);

            this.Controls.Add(_scrollPanel);
        }

        public void LoadData(string artistId, string artistName)
        {
            _artistId = artistId;
            _lblArtistName.Text = artistName;
            _albumsPanel.Controls.Clear();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                e.Result = App.Client.GetArtistAlbums(artistId);
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null) return;
                List<AlbumItem> albums = (List<AlbumItem>)e.Result;

                _albumsPanel.SuspendLayout();
                foreach (AlbumItem album in albums)
                {
                    _albumsPanel.Controls.Add(CreateAlbumCard(album));
                }
                _albumsPanel.ResumeLayout();
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

            PictureBox pic = new PictureBox();
            pic.Size = new Size(150, 150);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = Color.FromArgb(50, 50, 50);
            pic.Dock = DockStyle.Top;

            Label lblTitle = new Label();
            lblTitle.Text = album.Title;
            lblTitle.ForeColor = Color.FromArgb(200, 200, 200);
            lblTitle.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblTitle.Dock = DockStyle.Bottom;
            lblTitle.Height = 22;
            lblTitle.TextAlign = ContentAlignment.TopCenter;

            Label lblInfo = new Label();
            lblInfo.Text = string.Format("{0}  {1}", album.Year > 0 ? album.Year.ToString() : "", album.SongCount > 0 ? album.SongCount.ToString() + " треков" : "");
            lblInfo.ForeColor = Color.FromArgb(120, 120, 120);
            lblInfo.Font = new Font("Segoe UI", 7f);
            lblInfo.Dock = DockStyle.Bottom;
            lblInfo.Height = 18;
            lblInfo.TextAlign = ContentAlignment.TopCenter;

            card.Controls.Add(lblInfo);
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

            if (!string.IsNullOrEmpty(album.CoverArtId))
            {
                App.Cache.DownloadCoverArt(album.CoverArtId, 150, (bmp) =>
                {
                    try
                    {
                        if (IsHandleCreated && InvokeRequired)
                            Invoke((Action)(() =>
                            {
                                Image old = pic.Image;
                                pic.Image = bmp;
                                if (old != null && old != bmp) old.Dispose();
                            }));
                        else
                            pic.Image = bmp;
                    }
                    catch { }
                });
            }

            return card;
        }
    }
}
