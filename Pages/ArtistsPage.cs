using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class ArtistsPage : UserControl
    {
        private DarkListView _list;
        private Label _lblTitle;
        private Label _lblError;

        public event EventHandler ArtistClicked;

        public ArtistsPage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);

            _lblTitle = new Label();
            _lblTitle.Text = "Артисты";
            _lblTitle.Dock = DockStyle.Top;
            _lblTitle.Height = 40;
            _lblTitle.ForeColor = Color.White;
            _lblTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblTitle.Padding = new Padding(10, 8, 0, 0);

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
            _list.Columns.Add("Артист", 300);
            _list.Columns.Add("Альбомов", 100);
            _list.DoubleClick += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0)
                {
                    ArtistItem artist = (ArtistItem)_list.SelectedItems[0].Tag;
                    if (ArtistClicked != null)
                        ArtistClicked(this, new EventArgs<ArtistItem>(artist));
                }
            };

            _lblError = new Label();
            _lblError.ForeColor = Color.FromArgb(255, 100, 100);
            _lblError.Font = new Font("Segoe UI", 10f);
            _lblError.TextAlign = ContentAlignment.MiddleCenter;
            _lblError.BackColor = Color.FromArgb(30, 30, 30);
            _lblError.Dock = DockStyle.Fill;
            _lblError.Visible = false;

            Controls.Add(_list);
            Controls.Add(_lblTitle);
        }

        public void LoadData()
        {
            _list.Items.Clear();
            if (Controls.Contains(_lblError)) Controls.Remove(_lblError);
            _list.Visible = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                e.Result = App.Client.GetArtists();
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (this.IsDisposed) return;
                if (e.Error != null)
                {
                    _list.Visible = false;
                    _lblError.Text = "Ошибка: " + (e.Error.InnerException ?? e.Error).Message;
                    if (!Controls.Contains(_lblError)) Controls.Add(_lblError);
                    _lblError.Dock = DockStyle.Fill;
                    _lblError.Visible = true;
                    _lblError.BringToFront();
                    return;
                }
                List<ArtistItem> artists = (List<ArtistItem>)e.Result;
                foreach (ArtistItem artist in artists)
                {
                    ListViewItem item = new ListViewItem(artist.Name);
                    item.SubItems.Add(artist.AlbumCount.ToString());
                    item.Tag = artist;
                    _list.Items.Add(item);
                }
            };
            worker.RunWorkerAsync();
        }
    }
}
