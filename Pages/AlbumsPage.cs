using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class AlbumsPage : UserControl
    {
        private Label _lblTitle;
        private ComboBox _cmbType;
        private FlowLayoutPanel _albumsPanel;
        private Panel _scrollPanel;

        public event EventHandler AlbumClicked;

        public AlbumsPage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 44;
            topBar.BackColor = Color.FromArgb(30, 30, 30);

            _lblTitle = new Label();
            _lblTitle.Text = "Альбомы";
            _lblTitle.ForeColor = Color.White;
            _lblTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblTitle.Location = new Point(10, 10);
            _lblTitle.AutoSize = true;

            _cmbType = new ComboBox();
            _cmbType.Location = new Point(150, 12);
            _cmbType.Size = new Size(150, 24);
            _cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbType.BackColor = Color.FromArgb(45, 45, 45);
            _cmbType.ForeColor = Color.FromArgb(200, 200, 200);
            _cmbType.Font = new Font("Segoe UI", 9f);
            _cmbType.Items.AddRange(new object[] { "Недавние", "Новые", "Частые" });
            _cmbType.SelectedIndex = 0;
            _cmbType.SelectedIndexChanged += (s, e) => LoadData();

            topBar.Controls.Add(_lblTitle);
            topBar.Controls.Add(_cmbType);

            _scrollPanel = new Panel();
            _scrollPanel.Dock = DockStyle.Fill;
            _scrollPanel.AutoScroll = true;
            _scrollPanel.BackColor = Color.FromArgb(30, 30, 30);
            _scrollPanel.Padding = new Padding(20);

            _albumsPanel = new FlowLayoutPanel();
            _albumsPanel.Dock = DockStyle.Top;
            _albumsPanel.Height = 600;
            _albumsPanel.FlowDirection = FlowDirection.LeftToRight;
            _albumsPanel.WrapContents = true;
            _albumsPanel.AutoSize = false;

            _scrollPanel.Controls.Add(_albumsPanel);
            this.Controls.Add(_scrollPanel);
            this.Controls.Add(topBar);
        }

        public void LoadData()
        {
            string type = "recent";
            int idx = _cmbType.SelectedIndex;
            if (idx == 1) type = "newest";
            else if (idx == 2) type = "frequent";

            _albumsPanel.Controls.Clear();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                e.Result = App.Client.GetAlbumList(type, 50);
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
    }
}
