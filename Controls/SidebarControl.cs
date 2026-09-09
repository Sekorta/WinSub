using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinSub
{
    public class SidebarControl : UserControl
    {
        private Panel _navPanel;
        private TextBox _txtSearch;
        private Label _activeLabel;

        private Color _bgColor = Color.FromArgb(25, 25, 25);
        private Color _hoverColor = Color.FromArgb(40, 40, 40);
        private Color _activeColor = Color.FromArgb(53, 116, 252);
        private Color _textColor = Color.FromArgb(180, 180, 180);
        private Color _activeTextColor = Color.White;
        private Color _dimTextColor = Color.FromArgb(120, 120, 120);

        private PictureBox _picCover;
        private Label _lblTrackTitle;
        private Label _lblTrackArtist;
        private Button _btnBackRef;

        public event EventHandler NavigationChanged;
        public event EventHandler SearchSubmitted;
        public event EventHandler BackRequested;

        public SidebarControl()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = _bgColor;
            this.Dock = DockStyle.Left;
            this.Width = 200;

            Panel _bottomPanel = new Panel();
            _bottomPanel.Dock = DockStyle.Bottom;
            _bottomPanel.Height = 225;
            _bottomPanel.BackColor = _bgColor;

            _picCover = new PictureBox();
            _picCover.Size = new Size(170, 170);
            _picCover.SizeMode = PictureBoxSizeMode.Zoom;
            _picCover.BackColor = Color.FromArgb(40, 40, 40);
            _picCover.Location = new Point(15, 10);

            _lblTrackTitle = new Label();
            _lblTrackTitle.Text = "";
            _lblTrackTitle.ForeColor = _textColor;
            _lblTrackTitle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _lblTrackTitle.Location = new Point(15, 186);
            _lblTrackTitle.Size = new Size(170, 14);
            _lblTrackTitle.TextAlign = ContentAlignment.TopCenter;
            _lblTrackTitle.AutoEllipsis = true;

            _lblTrackArtist = new Label();
            _lblTrackArtist.Text = "";
            _lblTrackArtist.ForeColor = _dimTextColor;
            _lblTrackArtist.Font = new Font("Segoe UI", 7.5f);
            _lblTrackArtist.Location = new Point(15, 201);
            _lblTrackArtist.Size = new Size(170, 14);
            _lblTrackArtist.TextAlign = ContentAlignment.TopCenter;
            _lblTrackArtist.AutoEllipsis = true;

            _bottomPanel.Controls.Add(_picCover);
            _bottomPanel.Controls.Add(_lblTrackTitle);
            _bottomPanel.Controls.Add(_lblTrackArtist);

            Panel _searchRow = new Panel();
            _searchRow.Dock = DockStyle.Top;
            _searchRow.Height = 48;
            _searchRow.BackColor = _bgColor;

            Button btnBack = new Button();
            btnBack.Text = "<";
            btnBack.Size = new Size(28, 28);
            btnBack.Location = new Point(6, 10);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.BackColor = Color.FromArgb(60, 60, 60);
            btnBack.ForeColor = Color.White;
            btnBack.Font = new Font("Segoe UI", 11f);
            btnBack.Cursor = Cursors.Hand;
            btnBack.Visible = false;
            btnBack.TextAlign = ContentAlignment.MiddleCenter;
            btnBack.BringToFront();
            btnBack.Click += (s, e) =>
            {
                if (BackRequested != null)
                    BackRequested(this, EventArgs.Empty);
            };
            _btnBackRef = btnBack;

            _txtSearch = new TextBox();
            _txtSearch.Location = new Point(40, 12);
            _txtSearch.Size = new Size(152, 28);
            _txtSearch.BackColor = Color.FromArgb(45, 45, 45);
            _txtSearch.ForeColor = Color.FromArgb(180, 180, 180);
            _txtSearch.BorderStyle = BorderStyle.FixedSingle;
            _txtSearch.Font = new Font("Segoe UI", 9f);
            _txtSearch.Text = "Поиск...";
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Поиск...") _txtSearch.Text = ""; };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_txtSearch.Text)) _txtSearch.Text = "Поиск..."; };
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(_txtSearch.Text) && _txtSearch.Text != "Поиск...")
                {
                    if (SearchSubmitted != null)
                        SearchSubmitted(this, new EventArgs<string>(_txtSearch.Text));
                    this.ActiveControl = null;
                }
            };

            _searchRow.Controls.Add(_txtSearch);
            _searchRow.Controls.Add(btnBack);

            _navPanel = new Panel();
            _navPanel.Dock = DockStyle.Fill;
            _navPanel.BackColor = _bgColor;

            Label lblHome = CreateNavItem("#", "Домой");
            Label lblArtists = CreateNavItem("A", "Артисты");
            Label lblAlbums = CreateNavItem("B", "Альбомы");
            Label lblGenres = CreateNavItem("G", "Жанры");
            Label lblPlaylists = CreateNavItem("P", "Плейлисты");
            Label lblQueue = CreateNavItem("Q", "Очередь");
            Label lblFavorites = CreateNavItem("*", "Избранное");

            _navPanel.Controls.Add(lblQueue);
            _navPanel.Controls.Add(lblFavorites);
            _navPanel.Controls.Add(lblPlaylists);
            _navPanel.Controls.Add(lblGenres);
            _navPanel.Controls.Add(lblAlbums);
            _navPanel.Controls.Add(lblArtists);
            _navPanel.Controls.Add(lblHome);

            this.Controls.Add(_navPanel);
            this.Controls.Add(_searchRow);
            this.Controls.Add(_bottomPanel);

            SetActive(lblHome);
        }

        private Label CreateNavItem(string icon, string text)
        {
            Label lbl = new Label();
            lbl.Text = "  " + icon + "  " + text;
            lbl.Tag = text;
            lbl.Dock = DockStyle.Top;
            lbl.Height = 36;
            lbl.ForeColor = _textColor;
            lbl.BackColor = Color.Transparent;
            lbl.Font = new Font("Segoe UI", 10f);
            lbl.Cursor = Cursors.Hand;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Padding = new Padding(10, 0, 0, 0);

            lbl.MouseEnter += (s, e) =>
            {
                if (_activeLabel != lbl)
                {
                    lbl.BackColor = _hoverColor;
                    lbl.Invalidate();
                }
            };
            lbl.MouseLeave += (s, e) =>
            {
                if (_activeLabel != lbl)
                {
                    lbl.BackColor = Color.Transparent;
                    lbl.Invalidate();
                }
            };
            lbl.Click += (s, e) =>
            {
                SetActive(lbl);
                if (NavigationChanged != null)
                    NavigationChanged(this, new EventArgs<string>((string)lbl.Tag));
            };

            return lbl;
        }

        private void SetActive(Label label)
        {
            if (_activeLabel != null)
            {
                _activeLabel.BackColor = Color.Transparent;
                _activeLabel.ForeColor = _textColor;
                _activeLabel.Invalidate();
            }
            _activeLabel = label;
            _activeLabel.BackColor = _activeColor;
            _activeLabel.ForeColor = _activeTextColor;
            _activeLabel.Invalidate();
        }

        public void UpdateTrackInfo(string title, string artist, Bitmap cover)
        {
            _lblTrackTitle.Text = title ?? "";
            _lblTrackArtist.Text = artist ?? "";
            Image old = _picCover.Image;
            _picCover.Image = cover;
            if (old != null && !ReferenceEquals(old, cover))
                old.Dispose();
        }

        public void SetBackButtonVisible(bool visible)
        {
            if (_btnBackRef != null)
                _btnBackRef.Visible = visible;
        }

        public void FocusSearch()
        {
            if (_txtSearch != null)
            {
                _txtSearch.Focus();
                _txtSearch.SelectAll();
            }
        }

    }
}
