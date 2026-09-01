using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class LoginPage : UserControl
    {
        private Label _lblTitle;
        private Label _lblServer;
        private TextBox _txtServer;
        private Label _lblName;
        private TextBox _txtName;
        private Label _lblUser;
        private TextBox _txtUser;
        private Label _lblPass;
        private TextBox _txtPass;
        private Button _btnConnect;
        private Label _lblStatus;
        private PictureBox _picLogo;
        private ListBox _serverList;
        private Button _btnAdd;
        private Button _btnDelete;

        private Color _bgColor = Color.FromArgb(30, 30, 30);
        private Color _accentColor = Color.FromArgb(53, 116, 252);
        private Color _textColor = Color.FromArgb(200, 200, 200);
        private Color _inputBg = Color.FromArgb(45, 45, 45);
        private Color _dimClr = Color.FromArgb(120, 120, 120);
        private Color _listBg = Color.FromArgb(35, 35, 35);
        private Color _listSelected = Color.FromArgb(53, 116, 252);

        public event EventHandler Connected;

        public LoginPage()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.BackColor = _bgColor;
            this.Resize += (s, e) => CenterForm();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && this.Width > 0)
            {
                CenterForm();
                RefreshServerList();
                LoadSavedCredentials();
            }
        }

        private void CenterForm()
        {
            Controls.Clear();

            int formW = this.Width;
            int formH = this.Height;
            int centerX = formW / 2;
            int formY = (formH - 480) / 2;
            if (formY < 20) formY = 20;

            int leftX = centerX - 230;
            int rightX = centerX + 10;

            _picLogo = new PictureBox();
            _picLogo.Size = new Size(56, 56);
            _picLogo.Location = new Point(centerX - 28, formY);
            _picLogo.BackColor = _accentColor;
            _picLogo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Font f = new Font("Segoe UI", 24f, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(Color.White))
                    e.Graphics.DrawString("\u266B", f, b, 8, 6);
            };
            Controls.Add(_picLogo);

            _lblTitle = CreateLabel("WinSub", 0, formY + 64, 16f, FontStyle.Bold, _textColor);
            using (Font f = new Font("Segoe UI", 16f, FontStyle.Bold))
            {
                int tw = TextRenderer.MeasureText("WinSub", f).Width;
                _lblTitle.Location = new Point(centerX - tw / 2, formY + 64);
            }
            Controls.Add(_lblTitle);

            _lblServer = CreateLabel("Серверы", leftX, formY + 100, 9f, FontStyle.Bold, _textColor);
            Controls.Add(_lblServer);

            _serverList = new ListBox();
            _serverList.Location = new Point(leftX, formY + 120);
            _serverList.Size = new Size(210, 180);
            _serverList.BackColor = _listBg;
            _serverList.ForeColor = _textColor;
            _serverList.Font = new Font("Segoe UI", 9f);
            _serverList.BorderStyle = BorderStyle.FixedSingle;
            _serverList.SelectedIndexChanged += (s, e) =>
            {
                if (_serverList.SelectedIndex >= 0)
                {
                    ServerProfile sp = App.Settings.Servers[_serverList.SelectedIndex];
                    _txtName.Text = sp.Name;
                    _txtServer.Text = sp.Url;
                    _txtUser.Text = sp.Username;
                    _txtPass.Text = sp.Password;
                }
            };
            Controls.Add(_serverList);

            _btnAdd = new Button();
            _btnAdd.Text = "+";
            _btnAdd.Location = new Point(leftX, formY + 306);
            _btnAdd.Size = new Size(32, 28);
            _btnAdd.FlatStyle = FlatStyle.Flat;
            _btnAdd.FlatAppearance.BorderSize = 0;
            _btnAdd.BackColor = Color.FromArgb(60, 60, 60);
            _btnAdd.ForeColor = Color.FromArgb(200, 200, 200);
            _btnAdd.Font = new Font("Segoe UI", 10f);
            _btnAdd.Cursor = Cursors.Hand;
            _btnAdd.Click += (s, e) =>
            {
                ServerProfile sp = new ServerProfile("Новый сервер", "", "", "");
                App.Settings.Servers.Add(sp);
                App.Settings.SaveServers();
                RefreshServerList();
                _serverList.SelectedIndex = _serverList.Items.Count - 1;
            };
            Controls.Add(_btnAdd);

            _btnDelete = new Button();
            _btnDelete.Text = "\u2715";
            _btnDelete.Location = new Point(leftX + 38, formY + 306);
            _btnDelete.Size = new Size(32, 28);
            _btnDelete.FlatStyle = FlatStyle.Flat;
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.BackColor = Color.FromArgb(60, 60, 60);
            _btnDelete.ForeColor = Color.FromArgb(200, 200, 200);
            _btnDelete.Font = new Font("Segoe UI", 9f);
            _btnDelete.Cursor = Cursors.Hand;
            _btnDelete.Click += (s, e) =>
            {
                if (_serverList.SelectedIndex >= 0)
                {
                    App.Settings.Servers.RemoveAt(_serverList.SelectedIndex);
                    App.Settings.SaveServers();
                    RefreshServerList();
                    if (App.Settings.Servers.Count > 0)
                        _serverList.SelectedIndex = 0;
                    else
                    {
                        _txtName.Text = "";
                        _txtServer.Text = "";
                        _txtUser.Text = "";
                        _txtPass.Text = "";
                    }
                }
            };
            Controls.Add(_btnDelete);

            _lblName = CreateLabel("Наименование", rightX, formY + 100, 9f, FontStyle.Regular, _dimClr);
            Controls.Add(_lblName);

            _txtName = CreateInput(rightX, formY + 120, "");
            Controls.Add(_txtName);

            _lblServer = CreateLabel("URL сервера", rightX, formY + 160, 9f, FontStyle.Regular, _dimClr);
            Controls.Add(_lblServer);

            _txtServer = CreateInput(rightX, formY + 180, "http://");
            Controls.Add(_txtServer);

            _lblUser = CreateLabel("Логин", rightX, formY + 220, 9f, FontStyle.Regular, _dimClr);
            Controls.Add(_lblUser);

            _txtUser = CreateInput(rightX, formY + 240, "");
            Controls.Add(_txtUser);

            _lblPass = CreateLabel("Пароль", rightX, formY + 280, 9f, FontStyle.Regular, _dimClr);
            Controls.Add(_lblPass);

            _txtPass = CreateInput(rightX, formY + 300, "");
            _txtPass.UseSystemPasswordChar = true;
            Controls.Add(_txtPass);

            _btnConnect = new Button();
            _btnConnect.Text = "Подключиться";
            _btnConnect.Location = new Point(rightX, formY + 350);
            _btnConnect.Size = new Size(210, 36);
            _btnConnect.FlatStyle = FlatStyle.Flat;
            _btnConnect.FlatAppearance.BorderSize = 0;
            _btnConnect.BackColor = _accentColor;
            _btnConnect.ForeColor = Color.White;
            _btnConnect.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _btnConnect.Cursor = Cursors.Hand;
            _btnConnect.Click += btnConnect_Click;
            Controls.Add(_btnConnect);

            _lblStatus = CreateLabel("", rightX, formY + 396, 8f, FontStyle.Regular, Color.FromArgb(255, 100, 100));
            Controls.Add(_lblStatus);
        }

        private Label CreateLabel(string text, int x, int y, float fontSize, FontStyle style, Color color)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            lbl.ForeColor = color;
            lbl.Font = new Font("Segoe UI", fontSize, style);
            return lbl;
        }

        private TextBox CreateInput(int x, int y, string text)
        {
            TextBox txt = new TextBox();
            txt.Text = text;
            txt.Location = new Point(x, y);
            txt.Size = new Size(210, 26);
            txt.BackColor = _inputBg;
            txt.ForeColor = _textColor;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = new Font("Segoe UI", 9f);
            return txt;
        }

        private void RefreshServerList()
        {
            _serverList.Items.Clear();
            foreach (ServerProfile sp in App.Settings.Servers)
                _serverList.Items.Add(sp.Name ?? sp.Url);
        }

        private void LoadSavedCredentials()
        {
            if (App.Settings != null)
            {
                if (!string.IsNullOrEmpty(App.Settings.ServerUrl)) _txtServer.Text = App.Settings.ServerUrl;
                if (!string.IsNullOrEmpty(App.Settings.Username)) _txtUser.Text = App.Settings.Username;
                if (!string.IsNullOrEmpty(App.Settings.Password)) _txtPass.Text = App.Settings.Password;
                if (App.Settings.Servers.Count > 0 && _serverList.Items.Count > 0)
                {
                    _serverList.SelectedIndex = 0;
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtServer.Text) || string.IsNullOrEmpty(_txtUser.Text))
            {
                _lblStatus.Text = "Заполните URL и логин";
                return;
            }

            _btnConnect.Enabled = false;
            _btnConnect.Text = "Подключение...";
            _lblStatus.Text = "";
            _lblStatus.ForeColor = _textColor;

            App.Settings.ServerUrl = _txtServer.Text;
            App.Settings.Username = _txtUser.Text;
            App.Settings.Password = _txtPass.Text;

            int selectedIdx = _serverList.SelectedIndex;
            if (selectedIdx >= 0)
            {
                App.Settings.Servers[selectedIdx].Name = _txtName.Text;
                App.Settings.Servers[selectedIdx].Url = _txtServer.Text;
                App.Settings.Servers[selectedIdx].Username = _txtUser.Text;
                App.Settings.Servers[selectedIdx].Password = _txtPass.Text;
            }
            else
            {
                ServerProfile sp = new ServerProfile(
                    string.IsNullOrEmpty(_txtName.Text) ? _txtServer.Text : _txtName.Text,
                    _txtServer.Text, _txtUser.Text, _txtPass.Text);
                App.Settings.Servers.Add(sp);
            }

            App.Settings.Save();

            App.Client.SetCredentials(_txtServer.Text, _txtUser.Text, _txtPass.Text);
            App.Username = _txtUser.Text;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                string result = App.Client.PingDetailed();
                if (result != "ok")
                    throw new Exception(result);
            };
            worker.RunWorkerCompleted += (s, ev) =>
            {
                _btnConnect.Enabled = true;
                _btnConnect.Text = "Подключиться";

                if (ev.Error != null)
                {
                    _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
                    _lblStatus.Text = ev.Error.InnerException != null
                        ? ev.Error.InnerException.Message
                        : ev.Error.Message;
                    return;
                }

                _lblStatus.ForeColor = Color.FromArgb(100, 255, 100);
                _lblStatus.Text = "Подключено!";
                RefreshServerList();
                if (Connected != null)
                    Connected(this, EventArgs.Empty);
            };
            worker.RunWorkerAsync();
        }
    }
}
