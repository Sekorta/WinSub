using System;
using System.Drawing;
using System.IO;
using System.Net;

namespace SubsonicPlayer
{
    public class CacheManager
    {
        private string _cacheDir;

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

        public void SaveCoverArt(string id, int size, byte[] data)
        {
            if (string.IsNullOrEmpty(id) || data == null) return;
            string coversDir = Path.Combine(_cacheDir, "covers");
            if (!Directory.Exists(coversDir))
                Directory.CreateDirectory(coversDir);
            string fileName = string.Format("{0}_{1}.jpg", id, size);
            File.WriteAllBytes(Path.Combine(coversDir, fileName), data);
        }

        public void DownloadCoverArt(string id, int size, Action<Bitmap> callback)
        {
            if (string.IsNullOrEmpty(id) || App.Client == null) return;

            string existingPath = GetCoverArtPath(id, size);
            if (existingPath != null)
            {
                try
                {
                    using (FileStream fs = new FileStream(existingPath, FileMode.Open, FileAccess.Read))
                    {
                        Bitmap bmp = new Bitmap(fs);
                        if (callback != null) callback(bmp);
                    }
                    return;
                }
                catch { }
            }

            string url = App.Client.GetCoverArtUrl(id, size);
            if (url == null) return;

            WebClient client = new WebClient();
            client.DownloadDataCompleted += (s, e) =>
            {
                if (e.Error == null && e.Result != null)
                {
                    SaveCoverArt(id, size, e.Result);
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(e.Result))
                        {
                            Bitmap bmp = new Bitmap(ms);
                            if (callback != null) callback(bmp);
                        }
                    }
                    catch { }
                }
            };
            client.DownloadDataAsync(new Uri(url));
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
