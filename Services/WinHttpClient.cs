using System;
using System.Net;
using System.Text;

namespace WinSub
{
    public static class WinHttpClient
    {
        public static string DownloadString(string url)
        {
            if (IsHttps(url))
                return BouncyHttpClient.DownloadString(url);
            return WebClientDownloadString(url);
        }

        public static void DownloadFile(string url, string path)
        {
            if (IsHttps(url))
            {
                BouncyHttpClient.DownloadFile(url, path);
                return;
            }
            WebClientDownloadFile(url, path);
        }

        public static byte[] DownloadData(string url)
        {
            if (IsHttps(url))
                return BouncyHttpClient.DownloadData(url);
            return WebClientDownloadData(url);
        }

        private static bool IsHttps(string url)
        {
            return url.StartsWith("https", StringComparison.OrdinalIgnoreCase);
        }

        private static string WebClientDownloadString(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                return client.DownloadString(url);
            }
        }

        private static void WebClientDownloadFile(string url, string path)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, path);
            }
        }

        private static byte[] WebClientDownloadData(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadData(url);
            }
        }
    }
}
