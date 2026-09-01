namespace SubsonicPlayer
{
    public class ArtistItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CoverArtId { get; set; }
        public int AlbumCount { get; set; }
        public int? Rating { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
