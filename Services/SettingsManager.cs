using System;
using System.Collections.Generic;
using System.IO;

namespace WinSub
{
    public class SettingsManager
    {
        private string _filePath;
        private string _serversPath;

        public string ServerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<ServerProfile> Servers { get; set; }

        public SettingsManager()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinSub");
            _filePath = Path.Combine(dir, "settings.txt");
            _serversPath = Path.Combine(dir, "servers.txt");
            Servers = new List<ServerProfile>();
            Load();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(_filePath,
                    ServerUrl + "\n" +
                    Username + "\n" +
                    Password);
                SaveServers();
            }
            catch { }
        }

        public void SaveServers()
        {
            try
            {
                string dir = Path.GetDirectoryName(_serversPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                List<string> lines = new List<string>();
                foreach (ServerProfile s in Servers)
                {
                    lines.Add(s.Name);
                    lines.Add(s.Url);
                    lines.Add(s.Username);
                    lines.Add(s.Password);
                }
                File.WriteAllText(_serversPath, string.Join("\n", lines.ToArray()));
            }
            catch { }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string[] lines = File.ReadAllLines(_filePath);
                    if (lines.Length >= 3)
                    {
                        ServerUrl = lines[0];
                        Username = lines[1];
                        Password = lines[2];
                    }
                }
            }
            catch
            {
                ServerUrl = "";
                Username = "";
                Password = "";
            }

            LoadServers();
        }

        private void LoadServers()
        {
            Servers.Clear();
            try
            {
                if (File.Exists(_serversPath))
                {
                    string[] lines = File.ReadAllLines(_serversPath);
                    for (int i = 0; i + 3 < lines.Length; i += 4)
                    {
                        Servers.Add(new ServerProfile(lines[i], lines[i + 1], lines[i + 2], lines[i + 3]));
                    }
                }
            }
            catch { }

            if (Servers.Count == 0 && !string.IsNullOrEmpty(ServerUrl))
            {
                Servers.Add(new ServerProfile("Default", ServerUrl, Username, Password));
            }
        }
    }
}
