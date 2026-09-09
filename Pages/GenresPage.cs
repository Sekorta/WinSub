using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinSub
{
    public class GenresPage : UserControl
    {
        private DarkListView _list;
        private Label _lblTitle;
        private Label _lblError;

        public event EventHandler GenreClicked;

        public GenresPage()
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
            _lblTitle.Text = "Жанры";
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
            _list.Columns.Add("Жанр", 250);
            _list.Columns.Add("Треков", 100);
            _list.Columns.Add("Альбомов", 100);
            _list.DoubleClick += (s, e) =>
            {
                if (_list.SelectedItems.Count > 0)
                {
                    string genre = (string)_list.SelectedItems[0].Tag;
                    if (GenreClicked != null)
                        GenreClicked(this, new EventArgs<string>(genre));
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
                e.Result = App.Client.GetGenres();
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
                List<GenreItem> genres = (List<GenreItem>)e.Result;
                foreach (GenreItem g in genres)
                {
                    ListViewItem item = new ListViewItem(g.Name);
                    item.SubItems.Add(g.SongCount.ToString());
                    item.SubItems.Add(g.AlbumCount.ToString());
                    item.Tag = g.Name;
                    _list.Items.Add(item);
                }
                if (genres.Count == 0)
                {
                    _list.Visible = false;
                    _lblError.Text = "Жанры не найдены";
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
