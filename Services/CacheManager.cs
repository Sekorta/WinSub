using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace WinSub
{
    public class CacheManager
    {
        private string _cacheDir;
        private static readonly Dictionary<string, object> _fileLocks = new Dictionary<string, object>();
        private static readonly SemaphoreSlim _downloadGate = new SemaphoreSlim(4, 4);

        public CacheManager()
        {
            _cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinSub", "cache");
            if (!Directory.Exists(_cacheDir))
                Directory.CreateDirectory(_cacheDir);
        }

        public string GetCoverArtPath(string id, int size)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string fileName = string.Format("{0}_{1}.jpg", id, size);
            string path = Path.Combine(_cacheDir, "covers", fileName);
            if (File.Exists(path)) return path;
            return null;
        }

        public string GetCoverArtExpectedPath(string id, int size)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string fileName = string.Format("{0}_{1}.jpg", id, size);
            return Path.Combine(_cacheDir, "covers", fileName);
        }

        public void SaveCoverArt(string id, int size, byte[] data)
        {
            if (string.IsNullOrEmpty(id) || data == null) return;
            string coversDir = Path.Combine(_cacheDir, "covers");
            if (!Directory.Exists(coversDir))
                Directory.CreateDirectory(coversDir);
            string fileName = string.Format("{0}_{1}.jpg", id, size);
            string path = Path.Combine(coversDir, fileName);
            lock (GetFileLock(path))
            {
                try
                {
                    string temp = path + ".tmp";
                    File.WriteAllBytes(temp, data);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(temp, path);
                }
                catch { }
            }
        }

        public void DownloadCoverArt(string id, int size, Action<Bitmap> callback)
        {
            if (string.IsNullOrEmpty(id) || App.Client == null) return;

            string existingPath = GetCoverArtPath(id, size);
            if (existingPath != null)
            {
                Bitmap cached = LoadCachedBitmap(existingPath);
                if (cached != null)
                {
                    if (callback != null) callback(cached);
                    return;
                }
                try { File.Delete(existingPath); } catch { }
            }

            string url = App.Client.GetCoverArtUrl(id, size);
            if (url == null) return;

            ThreadPool.QueueUserWorkItem(state =>
            {
                Bitmap result = null;
                try
                {
                    object fileLock = GetFileLock(GetCoverArtExpectedPath(id, size));
                    lock (fileLock)
                    {
                        string ready = GetCoverArtPath(id, size);
                        if (ready != null)
                        {
                            result = LoadCachedBitmap(ready);
                            if (result == null)
                                try { File.Delete(ready); } catch { }
                        }
                        if (result == null)
                        {
                            for (int attempt = 0; attempt < 3 && result == null; attempt++)
                            {
                                _downloadGate.Wait();
                                try
                                {
                                    byte[] data = WinHttpClient.DownloadData(url);
                                    if (data != null && data.Length > 0)
                                    {
                                        Bitmap bmp = DecodeBitmap(data);
                                        if (bmp != null)
                                        {
                                            result = bmp;
                                            SaveCoverArt(id, size, data);
                                        }
                                    }
                                }
                                catch { }
                                finally { _downloadGate.Release(); }
                                if (result == null)
                                    try { Thread.Sleep(1200); } catch { }
                            }
                        }
                    }
                }
                catch { }
                if (result != null && callback != null)
                {
                    try { callback(result); }
                    catch { try { result.Dispose(); } catch { } }
                }
            });
        }

        private static object GetFileLock(string key)
        {
            if (string.IsNullOrEmpty(key)) return new object();
            lock (_fileLocks)
            {
                object l;
                if (!_fileLocks.TryGetValue(key, out l))
                {
                    l = new object();
                    _fileLocks[key] = l;
                }
                return l;
            }
        }

        private Bitmap LoadCachedBitmap(string path)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                return DecodeBitmap(data);
            }
            catch { return null; }
        }

        private Bitmap DecodeBitmap(byte[] data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(data, false))
                using (Bitmap temp = new Bitmap(ms))
                {
                    return new Bitmap(temp);
                }
            }
            catch { return null; }
        }

        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                    Directory.Delete(_cacheDir, true);
                Directory.CreateDirectory(_cacheDir);
            }
            catch { }
        }
    }
}
