using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using NAudio.Wave;

namespace SubsonicPlayer
{
    public class AudioPlayer : IDisposable
    {
        private WaveOutEvent _waveOut;
        private WaveStream _waveStream;
        private string _tempFile;
        private string _pcmFile;
        private Process _ffmpegProcess;
        private volatile bool _isPlaying;
        private int _playId;
        private Control _invokeTarget;
        private string _currentFormat;
        private int _currentBitRate;

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
        public string CurrentFormat { get { return _currentFormat; } }
        public int CurrentBitRate { get { return _currentBitRate; } }
        public float Volume
        {
            get { return _waveOut != null ? _waveOut.Volume : 1f; }
            set { if (_waveOut != null) _waveOut.Volume = value; }
        }

        public long Length
        {
            get { try { return _waveStream != null ? _waveStream.Length : 0; } catch { return 0; } }
        }

        public TimeSpan CurrentTime
        {
            get { try { return _waveStream != null ? _waveStream.CurrentTime : TimeSpan.Zero; } catch { return TimeSpan.Zero; } }
        }

        public TimeSpan TotalTime
        {
            get { try { return _waveStream != null ? _waveStream.TotalTime : TimeSpan.Zero; } catch { return TimeSpan.Zero; } }
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

        public void Play(string url)
        {
            _playId++;
            Stop();
            _isPlaying = true;

            int currentPlayId = _playId;
            string tempFile = Path.Combine(Path.GetTempPath(),
                "ssp_" + Guid.NewGuid().ToString("N") + ".tmp");

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(url, tempFile);
                    }

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
                            _isPlaying = false;
                            SafeInvoke(() => { if (Error != null) Error(this, EventArgs.Empty); });
                        }
                        return;
                    }

                    _tempFile = playFile;
                    _waveStream = newStream;
                    if (ext == ".flac") _currentFormat = "FLAC";
                    else if (ext == ".ogg") _currentFormat = "OGG";
                    else _currentFormat = "MP3";
                    _currentBitRate = 0;

                    SafeInvoke(() =>
                    {
                        if (currentPlayId != _playId) return;
                        try
                        {
                            _waveOut.Init(_waveStream);
                            _waveOut.Play();
                            if (PlaybackStarted != null) PlaybackStarted(this, EventArgs.Empty);
                        }
                        catch
                        {
                            _isPlaying = false;
                            if (Error != null) Error(this, EventArgs.Empty);
                        }
                    });
                }
                catch
                {
                    if (currentPlayId == _playId)
                    {
                        _isPlaying = false;
                        SafeInvoke(() => { if (Error != null) Error(this, EventArgs.Empty); });
                    }
                }
            });
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
