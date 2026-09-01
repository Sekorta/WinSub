namespace SubsonicPlayer
{
    public class ServerProfile
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ServerProfile() { }

        public ServerProfile(string name, string url, string username, string password)
        {
            Name = name;
            Url = url;
            Username = username;
            Password = password;
        }

        public override string ToString()
        {
            return Name ?? Url;
        }
    }
}
