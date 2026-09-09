namespace WinSub
{
    public class GenreItem
    {
        public string Name { get; set; }
        public int SongCount { get; set; }
        public int AlbumCount { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, SongCount);
        }
    }
}
