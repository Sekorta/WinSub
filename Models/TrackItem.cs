namespace SubsonicPlayer
{
    public class TrackItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string ArtistId { get; set; }
        public string Album { get; set; }
        public string AlbumId { get; set; }
        public string CoverArtId { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int TrackNumber { get; set; }
        public int DiscNumber { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsStarred { get; set; }
        public int BitRate { get; set; }

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
            return string.Format("{0} - {1}", Artist, Title);
        }
    }
}
