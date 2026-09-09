namespace WinSub
{
    public static class App
    {
        public static SubsonicClient Client = new SubsonicClient();
        public static AudioPlayer Player = new AudioPlayer();
        public static PlaybackManager Playback = new PlaybackManager(Player);
        public static CacheManager Cache = new CacheManager();
        public static SettingsManager Settings = new SettingsManager();
        public static AutoDJService AutoDJ = new AutoDJService();
        public static string Username;

        public static string Token;
        public static string Salt;
    }
}
