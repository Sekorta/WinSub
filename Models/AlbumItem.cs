namespace WinSub
{
    public class AlbumItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string ArtistId { get; set; }
        public string CoverArtId { get; set; }
        public int Year { get; set; }
        public int SongCount { get; set; }
        public int DurationSeconds { get; set; }
        public string Genre { get; set; }

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
