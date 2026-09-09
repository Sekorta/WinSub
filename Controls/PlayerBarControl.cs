using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinSub
{
    public class PlayerBarControl : UserControl
    {
        private Button _btnPrev;
        private Button _btnPlay;
        private Button _btnPause;
        private Button _btnNext;
        private Button _btnShuffle;
        private Button _btnRepeat;
        private Button _btnAutoDJ;
        private Button _btnStar;
        private TrackBar _seekBar;
        private TrackBar _volumeBar;
        private Label _lblTime;
        private Label _lblVolumePercent;
        private Label _lblQueueCount;
        private Timer _positionTimer;
        private bool _isSeeking;
        private bool _isStarred;

        private Color _bgColor = Color.FromArgb(30, 30, 30);
        private Color _accentColor = Color.FromArgb(53, 116, 252);
        private Color _textColor = Color.FromArgb(200, 200, 200);
        private Color _dimTextColor = Color.FromArgb(160, 160, 160);
        private Color _starColor = Color.FromArgb(255, 200, 50);

        public event EventHandler PlayClicked;
        public event EventHandler PauseClicked;
        public event EventHandler NextClicked;
        public event EventHandler PrevClicked;
        public event EventHandler ShuffleClicked;
        public event EventHandler RepeatClicked;
        public event EventHandler AutoDJClicked;
        public event EventHandler SeekPerformed;
        public event EventHandler VolumeChanged;
        public event EventHandler QueueClicked;
        public event EventHandler StarClicked;

        public PlayerBarControl()
        {
            InitializeUI();
            _positionTimer = new Timer();
            _positionTimer.Interval = 250;
            _positionTimer.Tick += OnPositionTick;
        }

        private void InitializeUI()
        {
            this.BackColor = _bgColor;
            this.Dock = DockStyle.Bottom;
            this.Height = 72;
            this.Padding = new Padding(8);

            _btnShuffle = CreateControlButton("SH", 6, 22, 36);
            _btnShuffle.ForeColor = _dimTextColor;
            _btnShuffle.Click += (s, e) => { if (ShuffleClicked != null) ShuffleClicked(this, EventArgs.Empty); };

            _btnPrev = CreateControlButton("<<", 46, 22, 36);
            _btnPrev.Click += (s, e) => { if (PrevClicked != null) PrevClicked(this, EventArgs.Empty); };

            _btnPlay = CreateControlButton(">", 86, 22, 36);
            _btnPlay.Click += (s, e) => { if (PlayClicked != null) PlayClicked(this, EventArgs.Empty); };

            _btnPause = CreateControlButton("||", 86, 22, 36);
            _btnPause.Click += (s, e) => { if (PauseClicked != null) PauseClicked(this, EventArgs.Empty); };
            _btnPause.Visible = false;

            _btnNext = CreateControlButton(">>", 126, 22, 36);
            _btnNext.Click += (s, e) => { if (NextClicked != null) NextClicked(this, EventArgs.Empty); };

            _btnRepeat = CreateControlButton("RP", 166, 22, 36);
            _btnRepeat.ForeColor = _dimTextColor;
            _btnRepeat.Click += (s, e) => { if (RepeatClicked != null) RepeatClicked(this, EventArgs.Empty); };

            _btnAutoDJ = CreateControlButton("DJ", 206, 22, 36);
            _btnAutoDJ.ForeColor = _dimTextColor;
            _btnAutoDJ.Click += (s, e) => { if (AutoDJClicked != null) AutoDJClicked(this, EventArgs.Empty); };

            _btnStar = CreateControlButton("*", 246, 22, 36);
            _btnStar.ForeColor = _dimTextColor;
            _btnStar.Font = new Font("Arial", 12f, FontStyle.Bold);
            _btnStar.Click += (s, e) =>
            {
                _isStarred = !_isStarred;
                UpdateStarState();
                if (StarClicked != null) StarClicked(this, EventArgs.Empty);
            };

            _seekBar = new TrackBar();
            _seekBar.Minimum = 0;
            _seekBar.Maximum = 1000;
            _seekBar.TickStyle = TickStyle.None;
            _seekBar.LargeChange = 10;
            _seekBar.SmallChange = 1;
            _seekBar.Location = new Point(370, 18);
            _seekBar.Size = new Size(180, 30);
            _seekBar.BackColor = _bgColor;
            _seekBar.MouseDown += (s, e) => _isSeeking = true;
            _seekBar.MouseUp += (s, e) =>
            {
                if (_isSeeking && SeekPerformed != null)
                {
                    float pct = (float)_seekBar.Value / _seekBar.Maximum;
                    SeekPerformed(this, new EventArgs<float>(pct));
                }
                _isSeeking = false;
            };

            _lblTime = new Label();
            _lblTime.Text = "0:00 / 0:00";
            _lblTime.ForeColor = _textColor;
            _lblTime.BackColor = _bgColor;
            _lblTime.Font = new Font("Arial", 9f);
            _lblTime.Location = new Point(290, 26);
            _lblTime.AutoSize = true;

            _volumeBar = new TrackBar();
            _volumeBar.Minimum = 0;
            _volumeBar.Maximum = 100;
            _volumeBar.Value = 80;
            _volumeBar.TickStyle = TickStyle.None;
            _volumeBar.LargeChange = 10;
            _volumeBar.Location = new Point(560, 18);
            _volumeBar.Size = new Size(80, 30);
            _volumeBar.BackColor = _bgColor;
            _volumeBar.Scroll += (s, e) =>
            {
                _lblVolumePercent.Text = _volumeBar.Value + "%";
                if (VolumeChanged != null) VolumeChanged(this, new EventArgs<int>(_volumeBar.Value));
            };

            _lblVolumePercent = new Label();
            _lblVolumePercent.Text = "80%";
            _lblVolumePercent.ForeColor = _textColor;
            _lblVolumePercent.BackColor = _bgColor;
            _lblVolumePercent.Font = new Font("Arial", 9f);
            _lblVolumePercent.Location = new Point(648, 26);
            _lblVolumePercent.AutoSize = true;

            _lblQueueCount = new Label();
            _lblQueueCount.Text = "[0]";
            _lblQueueCount.ForeColor = _textColor;
            _lblQueueCount.BackColor = _bgColor;
            _lblQueueCount.Font = new Font("Arial", 9f);
            _lblQueueCount.Location = new Point(780, 26);
            _lblQueueCount.AutoSize = true;
            _lblQueueCount.Cursor = Cursors.Hand;
            _lblQueueCount.Click += (s, e) => { if (QueueClicked != null) QueueClicked(this, EventArgs.Empty); };

            Controls.Add(_btnShuffle);
            Controls.Add(_btnPrev);
            Controls.Add(_btnPlay);
            Controls.Add(_btnPause);
            Controls.Add(_btnNext);
            Controls.Add(_btnRepeat);
            Controls.Add(_btnAutoDJ);
            Controls.Add(_btnStar);
            Controls.Add(_seekBar);
            Controls.Add(_lblTime);
            Controls.Add(_volumeBar);
            Controls.Add(_lblVolumePercent);
            Controls.Add(_lblQueueCount);
        }

        private Button CreateControlButton(string text, int x, int y, int width)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(width, 28);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(50, 50, 50);
            btn.ForeColor = _textColor;
            btn.Font = new Font("Arial", 10f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleCenter;
            return btn;
        }

        public void SetTrackInfo(string title, string artist, Bitmap cover)
        {
        }

        public void SetPlaying(bool playing)
        {
            _btnPlay.Visible = !playing;
            _btnPause.Visible = playing;
        }

        public void SetShuffleActive(bool active)
        {
            _btnShuffle.ForeColor = active ? _accentColor : _dimTextColor;
        }

        public void SetRepeatMode(RepeatMode mode)
        {
            switch (mode)
            {
                case RepeatMode.Off:
                    _btnRepeat.Text = "RP";
                    _btnRepeat.ForeColor = _dimTextColor;
                    break;
                case RepeatMode.All:
                    _btnRepeat.Text = "RP";
                    _btnRepeat.ForeColor = _accentColor;
                    break;
                case RepeatMode.One:
                    _btnRepeat.Text = "R1";
                    _btnRepeat.ForeColor = _accentColor;
                    break;
            }
        }

        public void SetAutoDJActive(bool active)
        {
            _btnAutoDJ.ForeColor = active ? _accentColor : _dimTextColor;
        }

        public void SetStarred(bool starred)
        {
            _isStarred = starred;
            UpdateStarState();
        }

        private void UpdateStarState()
        {
            _btnStar.ForeColor = _isStarred ? _starColor : _dimTextColor;
        }

        public void SetQueueCount(int count)
        {
            _lblQueueCount.Text = string.Format("[{0}]", count);
        }

        public void SetVolumePercent(int percent)
        {
            _lblVolumePercent.Text = percent + "%";
            _volumeBar.Value = percent;
        }

        public int GetVolume()
        {
            return _volumeBar.Value;
        }

        public void StartTimer()
        {
            _positionTimer.Start();
        }

        public void StopTimer()
        {
            _positionTimer.Stop();
        }

        public void UpdatePosition(TimeSpan current, TimeSpan total)
        {
            if (_isSeeking) return;
            if (total.TotalMilliseconds <= 0) return;

            double pct = current.TotalMilliseconds / total.TotalMilliseconds;
            _seekBar.Value = Math.Min(_seekBar.Maximum, (int)(pct * _seekBar.Maximum));
            _lblTime.Text = string.Format("{0} / {1}",
                FormatTime(current), FormatTime(total));
        }

        private void OnPositionTick(object sender, EventArgs e)
        {
            if (App.Player != null && App.Player.IsPlaying)
            {
                TimeSpan cur = App.Player.CurrentTime;
                TimeSpan total = App.Player.TotalTime;
                if (total.TotalMilliseconds > 0)
                    UpdatePosition(cur, total);
                else
                    _lblTime.Text = FormatTime(cur);
            }
        }

        private string FormatTime(TimeSpan ts)
        {
            return string.Format("{0}:{1:D2}", (int)ts.TotalMinutes, ts.Seconds);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(40, 40, 40)))
                e.Graphics.DrawLine(p, 0, 0, Width, 0);
        }
    }
}
