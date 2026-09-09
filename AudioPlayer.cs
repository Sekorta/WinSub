using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NAudio.Wave;

namespace WinSub
{
    public class AudioPlayer : IDisposable
    {
        private WaveOutEvent _waveOut;
        private WaveStream _waveStream;
        private string _tempFile;
        private string _pcmFile;
        private Process _ffmpegProcess;
        private volatile bool _isPlaying;
        private volatile bool _isLoading;
        private int _playId;
        private Control _invokeTarget;
        private string _currentFormat;
        private int _currentBitRate;
        private readonly object _preBufferLock = new object();
        private string _preBufferedId;
        private string _preBufferedFile;
        private bool _preBufferInFlight;
        private bool _preBufferDone;
        private bool _preBufferError;
        private ManualResetEventSlim _preBufferWait;
        private float _lastVolume = 0.8f;

        private static string FFmpegPath
        {
            get
            {
                string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
                string local = Path.Combine(appDir, "ffmpeg", "ffmpeg.exe");
                if (File.Exists(local)) return local;
                local = Path.Combine(appDir, "ffmpeg.exe");
                if (File.Exists(local)) return local;
                return "ffmpeg";
            }
        }

        public bool IsPlaying { get { return _isPlaying; } }
        public bool IsLoading { get { return _isLoading; } }
        public string CurrentFormat { get { return _currentFormat; } }
        public int CurrentBitRate { get { return _currentBitRate; } }
        public float Volume
        {
            get { return _waveOut != null ? _waveOut.Volume : _lastVolume; }
            set { _lastVolume = value; if (_waveOut != null) _waveOut.Volume = value; }
        }

        public long Length
        {
            get
            {
                try
                {
                    if (_waveStream != null) return _waveStream.Length;
                    return 0;
                }
                catch { return 0; }
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                try
                {
                    if (_waveStream != null) return _waveStream.CurrentTime;
                    return TimeSpan.Zero;
                }
                catch { return TimeSpan.Zero; }
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                try
                {
                    if (_waveStream != null) return _waveStream.TotalTime;
                    return TimeSpan.Zero;
                }
                catch { return TimeSpan.Zero; }
            }
        }

        public event EventHandler PlaybackStarted;
        public event EventHandler PlaybackStopped;
        public event EventHandler PlaybackFinished;
        public event EventHandler Error;

        public void SetInvokeTarget(Control target)
        {
            _invokeTarget = target;
        }

        public AudioPlayer()
        {
            _waveOut = new WaveOutEvent();
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Volume = 0.8f;
        }

        public void PreBufferNextTrack(string songId, string url)
        {
            if (string.IsNullOrEmpty(songId)) return;
            lock (_preBufferLock)
            {
                if (_preBufferedId == songId && _preBufferInFlight) return;
                if (_preBufferedId == songId && _preBufferDone && !_preBufferError && _preBufferedFile != null) return;
                TryDelete(_preBufferedFile);
                _preBufferedId = songId;
                _preBufferedFile = null;
                _preBufferInFlight = true;
                _preBufferDone = false;
                _preBufferError = false;
                _preBufferWait = new ManualResetEventSlim(false);
            }

            string tempFile = Path.Combine(Path.GetTempPath(),
                "ssp_prebuf_" + Guid.NewGuid().ToString("N") + ".tmp");

            ThreadPool.QueueUserWorkItem(state =>
            {
                bool ok = false;
                try
                {
                    WinHttpClient.DownloadFile(url, tempFile);
                    ok = true;
                }
                catch { }
                if (!ok) TryDelete(tempFile);

                lock (_preBufferLock)
                {
                    if (_preBufferedId == songId)
                    {
                        if (ok)
                            _preBufferedFile = tempFile;
                        else
                            _preBufferError = true;
                        _preBufferInFlight = false;
                        _preBufferDone = true;
                        if (_preBufferWait != null)
                        {
                            try { _preBufferWait.Set(); }
                            catch { }
                        }
                    }
                    else
                    {
                        TryDelete(tempFile);
                    }
                }
            });
        }

        private string ConsumePreBuffer(string songId)
        {
            lock (_preBufferLock)
            {
                if (!string.IsNullOrEmpty(songId) && _preBufferedId == songId && !string.IsNullOrEmpty(_preBufferedFile))
                {
                    string file = _preBufferedFile;
                    _preBufferedId = null;
                    _preBufferedFile = null;
                    return file;
                }
                return null;
            }
        }

        public void Play(string url, string songId = null)
        {
            _playId++;
            Stop();
            _isPlaying = true;
            _isLoading = true;

            int currentPlayId = _playId;

            string preBuffered = ConsumePreBuffer(songId);
            if (preBuffered != null && File.Exists(preBuffered))
            {
                ThreadPool.QueueUserWorkItem(state => PlayFromFile(preBuffered, currentPlayId));
                return;
            }

            ManualResetEventSlim waitOn = null;
            lock (_preBufferLock)
            {
                if (!string.IsNullOrEmpty(songId) && _preBufferedId == songId && _preBufferInFlight)
                    waitOn = _preBufferWait;
            }

            if (waitOn != null)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    bool ready = false;
                    try { ready = waitOn.Wait(25000); }
                    catch { }

                    if (currentPlayId != _playId) return;

                    string file = null;
                    lock (_preBufferLock)
                    {
                        if (_preBufferedId == songId && _preBufferDone && !_preBufferError && _preBufferedFile != null)
                        {
                            file = _preBufferedFile;
                            _preBufferedId = null;
                            _preBufferedFile = null;
                        }
                    }

                    if (file != null && File.Exists(file))
                        PlayFromFile(file, currentPlayId);
                    else
                        DownloadAndPlay(url, songId, currentPlayId);
                });
                return;
            }

            DownloadAndPlay(url, songId, currentPlayId);
        }

        private void DownloadAndPlay(string url, string songId, int currentPlayId)
        {
            string tempFile = Path.Combine(Path.GetTempPath(),
                "ssp_dl_" + Guid.NewGuid().ToString("N") + ".tmp");

            ThreadPool.QueueUserWorkItem(state =>
            {
                int rounds = 0;
                while (currentPlayId == _playId && rounds < 2)
                {
                    bool ok = false;
                    for (int attempt = 0; attempt < 3 && !ok && currentPlayId == _playId; attempt++)
                    {
                        try
                        {
                            WinHttpClient.DownloadFile(url, tempFile);
                            ok = true;
                        }
                        catch
                        {
                            TryDelete(tempFile);
                            try { Thread.Sleep(1500); } catch { }
                        }
                    }

                    if (currentPlayId != _playId) { TryDelete(tempFile); return; }

                    if (ok)
                    {
                        PlayFromFile(tempFile, currentPlayId);
                        return;
                    }

                    TryDelete(tempFile);
                    rounds++;
                    if (currentPlayId == _playId)
                    {
                        try { Thread.Sleep(2500); } catch { }
                        tempFile = Path.Combine(Path.GetTempPath(),
                            "ssp_dl_" + Guid.NewGuid().ToString("N") + ".tmp");
                    }
                }

                if (currentPlayId != _playId) return;
                TryDelete(tempFile);
                _isLoading = false;
                _isPlaying = false;
                SafeInvoke(() => { if (Error != null) Error(this, EventArgs.Empty); });
            });
        }

        private void PlayFromFile(string tempFile, int currentPlayId)
        {
            try
            {
                if (currentPlayId != _playId) { TryDelete(tempFile); return; }

                string ext = ".mp3";
                if (File.Exists(tempFile) && tempFile.Length > 4)
                {
                    byte[] header = new byte[4];
                    using (FileStream fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                        fs.Read(header, 0, 4);
                    if (header[0] == 0x66 && header[1] == 0x4C && header[2] == 0x61 && header[3] == 0x43)
                        ext = ".flac";
                    else if (header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53)
                        ext = ".ogg";
                }
                bool useFFmpeg = ext == ".flac" || ext == ".ogg";
                string playFile = Path.ChangeExtension(tempFile, ext);
                try { File.Move(tempFile, playFile); }
                catch { playFile = tempFile; }

                if (currentPlayId != _playId) { TryDelete(playFile); return; }

                WaveStream newStream = null;

                if (useFFmpeg)
                {
                    newStream = DecodeWithFFmpeg(playFile, currentPlayId);
                }
                else
                {
                    try { newStream = new Mp3FileReader(playFile); }
                    catch
                    {
                        try { newStream = new MediaFoundationReader(playFile); }
                        catch { }
                    }
                }

                if (newStream == null || currentPlayId != _playId)
                {
                    if (newStream != null) newStream.Dispose();
                    TryDelete(playFile);
                    if (currentPlayId == _playId)
                    {
                        _isLoading = false;
                        _isPlaying = false;
                        SafeInvoke(() => { if (Error != null) Error(this, EventArgs.Empty); });
                    }
                    return;
                }

                string format = ext == ".flac" ? "FLAC" : ext == ".ogg" ? "OGG" : "MP3";
                WaveStream stream = newStream;
                string file = playFile;

                SafeInvoke(() =>
                {
                    if (currentPlayId != _playId)
                    {
                        try { stream.Dispose(); } catch { }
                        TryDelete(file);
                        return;
                    }
                    try
                    {
                        _isLoading = false;
                        _waveOut.Init(stream);
                        _waveOut.Play();
                        _waveStream = stream;
                        _tempFile = file;
                        _currentFormat = format;
                        _currentBitRate = 0;
                        if (PlaybackStarted != null) PlaybackStarted(this, EventArgs.Empty);
                    }
                    catch
                    {
                        try { stream.Dispose(); } catch { }
                        TryDelete(file);
                        _isLoading = false;
                        _isPlaying = false;
                        if (Error != null) Error(this, EventArgs.Empty);
                    }
                });
            }
            catch
            {
                if (currentPlayId == _playId)
                {
                    _isLoading = false;
                    _isPlaying = false;
                    SafeInvoke(() => { if (Error != null) Error(this, EventArgs.Empty); });
                }
            }
        }

        private WaveStream DecodeWithFFmpeg(string filePath, int currentPlayId)
        {
            string pcmFile = Path.Combine(Path.GetTempPath(),
                "ssp_ffm_" + Guid.NewGuid().ToString("N") + ".raw");

            try
            {
                string ffmpegExe = FFmpegPath;
                string workDir = Path.GetDirectoryName(ffmpegExe) ?? ".";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegExe,
                    Arguments = "-y -i \"" + filePath + "\" -f s16le -acodec pcm_s16le -ac 2 -ar 44100 -loglevel error \"" + pcmFile + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workDir
                };

                Process proc = Process.Start(psi);
                if (proc == null) return null;

                _ffmpegProcess = proc;

                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (currentPlayId != _playId)
                {
                    TryDelete(pcmFile);
                    return null;
                }

                if (!File.Exists(pcmFile) || new FileInfo(pcmFile).Length == 0)
                {
                    TryDelete(pcmFile);
                    return null;
                }

                FileStream fs = new FileStream(pcmFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                _pcmFile = pcmFile;
                WaveFormat format = new WaveFormat(44100, 16, 2);
                return new RawSourceWaveStream(fs, format);
            }
            catch
            {
                TryDelete(pcmFile);
                return null;
            }
            finally
            {
                _ffmpegProcess = null;
            }
        }

        private void SafeInvoke(Action action)
        {
            if (_invokeTarget != null && _invokeTarget.InvokeRequired)
            {
                try { _invokeTarget.BeginInvoke(action); }
                catch { }
            }
            else
            {
                try { action(); }
                catch { }
            }
        }

        public void Pause()
        {
            if (_waveOut != null && _isPlaying)
            {
                _waveOut.Pause();
                _isPlaying = false;
            }
        }

        public void Resume()
        {
            if (_waveOut != null && !_isPlaying && _waveStream != null)
            {
                _waveOut.Play();
                _isPlaying = true;
            }
        }

        public void Stop()
        {
            _playId++;
            _isPlaying = false;
            _isLoading = false;

            if (_waveOut != null)
            {
                try { _waveOut.Stop(); }
                catch { }
            }
            if (_ffmpegProcess != null)
            {
                try { _ffmpegProcess.Kill(); } catch { }
                _ffmpegProcess = null;
            }
            if (_waveStream != null)
            {
                try { _waveStream.Dispose(); }
                catch { }
                _waveStream = null;
            }
            TryDelete(_tempFile);
            TryDelete(_pcmFile);
        }

        public void Seek(TimeSpan position)
        {
            try { if (_waveStream != null) _waveStream.CurrentTime = position; }
            catch { }
        }

        public void SeekPercent(float percent)
        {
            try
            {
                if (_waveStream != null && _waveStream.Length > 0)
                {
                    long pos = (long)(_waveStream.Length * Math.Max(0, Math.Min(1, percent)));
                    _waveStream.Position = pos;
                }
            }
            catch { }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                if (_waveStream != null && _isPlaying &&
                    _waveStream.Position >= _waveStream.Length - 1024)
                {
                    _isPlaying = false;
                    SafeInvoke(() => { if (PlaybackFinished != null) PlaybackFinished(this, EventArgs.Empty); });
                }
            }
            catch { }

            SafeInvoke(() => { if (PlaybackStopped != null) PlaybackStopped(this, EventArgs.Empty); });
        }

        private void TryDelete(string file)
        {
            try { if (!string.IsNullOrEmpty(file) && File.Exists(file)) File.Delete(file); }
            catch { }
        }

        public void Dispose()
        {
            Stop();
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Dispose();
                _waveOut = null;
            }
            TryDelete(_tempFile);
            TryDelete(_pcmFile);
        }
    }
}
