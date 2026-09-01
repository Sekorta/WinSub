namespace SubsonicPlayer
{
    public class PlaylistItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public int SongCount { get; set; }
        public int DurationSeconds { get; set; }
        public string Owner { get; set; }

        public string DurationFormatted
        {
            get
            {
                int min = DurationSeconds / 60;
                int sec = DurationSeconds % 60;
                return string.Format("{0}:{1:D2}", min, sec);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
