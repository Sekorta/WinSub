using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class PlaylistsPage : UserControl
    {
        private DarkListView _list;
        private Label _lblTitle;
        private Label _lblError;

        public event EventHandler PlaylistClicked;

        public PlaylistsPage()
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

            _lblTitle = new Label();
            _lblTitle.Text = "Плейлисты";
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
            _list.Columns.Add("Название", 250);
            _list.Columns.Add("Треков", 80);
            _list.Columns.Add("Владелец", 150);
            _list.DoubleClick += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0)
                {
                    PlaylistItem pl = (PlaylistItem)_list.SelectedItems[0].Tag;
                    if (PlaylistClicked != null)
                        PlaylistClicked(this, new EventArgs<PlaylistItem>(pl));
                }
            };

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
                e.Result = App.Client.GetPlaylists();
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
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
                List<PlaylistItem> playlists = (List<PlaylistItem>)e.Result;
                foreach (PlaylistItem pl in playlists)
                {
                    ListViewItem item = new ListViewItem(pl.Name);
                    item.SubItems.Add(pl.SongCount.ToString());
                    item.SubItems.Add(pl.Owner);
                    item.Tag = pl;
                    _list.Items.Add(item);
                }
                if (playlists.Count == 0)
                {
                    _list.Visible = false;
                    _lblError.Text = "Плейлисты не найдены";
                    if (!Controls.Contains(_lblError)) Controls.Add(_lblError);
                    _lblError.Dock = DockStyle.Fill;
                    _lblError.Visible = true;
                    _lblError.BringToFront();
                }
            };
            worker.RunWorkerAsync();
        }
    }
}
