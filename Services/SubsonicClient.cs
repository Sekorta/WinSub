using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace WinSub
{
    public class SubsonicClient
    {
        private string _serverUrl;
        private string _username;
        private string _password;
        private string _salt;
        private string _token;
        private static readonly XNamespace NS = "http://subsonic.org/restapi";

        public string ServerUrl { get { return _serverUrl; } }

        public SubsonicClient() { }

        public void SetCredentials(string serverUrl, string username, string password)
        {
            serverUrl = serverUrl.Trim();
            if (!serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                serverUrl = "http://" + serverUrl;
            _serverUrl = serverUrl.TrimEnd('/');
            _username = username;
            _password = password;
        }

        private void GenerateToken()
        {
            _salt = Guid.NewGuid().ToString("N");
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(_password + _salt));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                _token = sb.ToString();
            }
        }

        private string BuildUrl(string command, params string[] extraParams)
        {
            GenerateToken();
            StringBuilder sb = new StringBuilder();
            sb.Append(_serverUrl);
            sb.Append("/rest/");
            sb.Append(command);
            sb.Append("?u=").Append(Uri.EscapeDataString(_username));
            sb.Append("&t=").Append(_token);
            sb.Append("&s=").Append(_salt);
            sb.Append("&v=1.13.0");
            sb.Append("&c=WinSub&f=xml");
            for (int i = 0; i < extraParams.Length; i += 2)
            {
                sb.Append("&").Append(extraParams[i]);
                sb.Append("=").Append(Uri.EscapeDataString(extraParams[i + 1]));
            }
            return sb.ToString();
        }

        public string GetCoverArtUrl(string id, int size)
        {
            if (string.IsNullOrEmpty(id)) return null;
            GenerateToken();
            return string.Format("{0}/rest/getCoverArt?id={1}&size={2}&u={3}&t={4}&s={5}&v=1.13.0&c=WinSub",
                _serverUrl, Uri.EscapeDataString(id), size,
                Uri.EscapeDataString(_username), _token, _salt);
        }

        public string GetStreamUrl(string songId)
        {
            if (string.IsNullOrEmpty(songId)) return null;
            GenerateToken();
            bool isXp = Environment.OSVersion.Version.Major < 6;
            string format = isXp ? "&maxBitRate=320" : "";
            return string.Format("{0}/rest/stream?id={1}&u={2}&t={3}&s={4}&v=1.13.0&c=WinSub{5}",
                _serverUrl, Uri.EscapeDataString(songId),
                Uri.EscapeDataString(_username), _token, _salt, format);
        }

        private XDocument Request(string command, params string[] extraParams)
        {
            string url = BuildUrl(command, extraParams);
            string xml = WinHttpClient.DownloadString(url);
            XDocument doc = XDocument.Parse(xml);
            XElement root = doc.Root;
            if (root != null && root.Attribute("status") != null &&
                root.Attribute("status").Value == "failed")
            {
                XElement error = root.Element(NS + "error") ?? root.Element("error");
                string msg = error != null && error.Attribute("message") != null
                    ? error.Attribute("message").Value : "Unknown error";
                throw new Exception("API: " + msg);
            }
            return doc;
        }

        private XElement FindElement(XElement parent, string name)
        {
            XElement el = parent.Element(NS + name);
            if (el != null) return el;
            el = parent.Element(name);
            if (el != null) return el;
            return null;
        }

        private IEnumerable<XElement> FindElements(XElement parent, string name)
        {
            IEnumerable<XElement> els = parent.Elements(NS + name);
            if (els.GetEnumerator().MoveNext())
                return parent.Elements(NS + name);
            els = parent.Elements(name);
            if (els.GetEnumerator().MoveNext())
                return parent.Elements(name);
            return parent.Elements(NS + name);
        }

        public string PingDetailed()
        {
            try { Request("ping"); return "ok"; }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                int d = 0;
                while (ex != null && d < 5)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(ex.Message);
                    ex = ex.InnerException;
                    d++;
                }
                return sb.ToString();
            }
        }

        public List<TrackItem> GetRandomSongs(int count = 500)
        {
            List<TrackItem> tracks = new List<TrackItem>();
            XDocument doc = Request("getRandomSongs", "size", count.ToString());
            XElement root = doc.Root;
            if (root == null) return tracks;
            XElement songs = FindElement(root, "randomSongs");
            if (songs == null) return tracks;
            foreach (XElement song in FindElements(songs, "song"))
            {
                TrackItem t = ParseSong(song);
                if (t != null) tracks.Add(t);
            }
            return tracks;
        }

        public List<AlbumItem> GetAlbumList(string type = "recent", int size = 50, int offset = 0)
        {
            List<AlbumItem> albums = new List<AlbumItem>();
            XDocument doc = Request("getAlbumList2", "type", type, "size", size.ToString(), "offset", offset.ToString());
            XElement root = doc.Root;
            if (root == null) return albums;
            XElement albumList = FindElement(root, "albumList2");
            if (albumList == null) albumList = FindElement(root, "albumList");
            if (albumList == null) return albums;
            foreach (XElement album in FindElements(albumList, "album"))
            {
                AlbumItem a = ParseAlbum(album);
                if (a != null) albums.Add(a);
            }
            return albums;
        }

        public List<TrackItem> GetAlbumSongs(string albumId)
        {
            List<TrackItem> tracks = new List<TrackItem>();
            XDocument doc = Request("getAlbum", "id", albumId);
            XElement root = doc.Root;
            if (root == null) return tracks;
            XElement album = FindElement(root, "album");
            if (album == null) return tracks;
            foreach (XElement song in FindElements(album, "song"))
            {
                TrackItem t = ParseSong(song);
                if (t != null) tracks.Add(t);
            }
            return tracks;
        }

        public List<ArtistItem> GetArtists()
        {
            List<ArtistItem> artists = new List<ArtistItem>();
            XDocument doc = Request("getArtists");
            XElement root = doc.Root;
            if (root == null) return artists;
            XElement artistsIndex = FindElement(root, "artists");
            if (artistsIndex == null) return artists;
            foreach (XElement index in FindElements(artistsIndex, "index"))
            {
                foreach (XElement artist in FindElements(index, "artist"))
                {
                    ArtistItem a = ParseArtist(artist);
                    if (a != null) artists.Add(a);
                }
            }
            return artists;
        }

        public List<AlbumItem> GetArtistAlbums(string artistId)
        {
            List<AlbumItem> albums = new List<AlbumItem>();
            XDocument doc = Request("getArtist", "id", artistId);
            XElement root = doc.Root;
            if (root == null) return albums;
            XElement artist = FindElement(root, "artist");
            if (artist == null) return albums;
            foreach (XElement album in FindElements(artist, "album"))
            {
                AlbumItem a = ParseAlbum(album);
                if (a != null) albums.Add(a);
            }
            return albums;
        }

        public List<GenreItem> GetGenres()
        {
            List<GenreItem> genres = new List<GenreItem>();
            XDocument doc = Request("getGenres");
            XElement root = doc.Root;
            if (root == null) return genres;
            XElement genresElem = FindElement(root, "genres");
            if (genresElem == null) return genres;
            foreach (XElement g in FindElements(genresElem, "genre"))
            {
                GenreItem gi = new GenreItem();
                string nameAttr = GetAttr(g, "name");
                gi.Name = !string.IsNullOrEmpty(nameAttr) ? nameAttr : g.Value;
                gi.SongCount = GetIntAttr(g, "songCount");
                gi.AlbumCount = GetIntAttr(g, "albumCount");
                if (!string.IsNullOrEmpty(gi.Name))
                    genres.Add(gi);
            }
            return genres;
        }

        public List<TrackItem> GetSongsByGenre(string genre, int count = 50, int offset = 0)
        {
            List<TrackItem> tracks = new List<TrackItem>();
            XDocument doc = Request("getSongsByGenre", "genre", genre, "count", count.ToString(), "offset", offset.ToString());
            XElement root = doc.Root;
            if (root == null) return tracks;
            XElement songs = FindElement(root, "songsByGenre");
            if (songs == null) return tracks;
            foreach (XElement song in FindElements(songs, "song"))
            {
                TrackItem t = ParseSong(song);
                if (t != null) tracks.Add(t);
            }
            return tracks;
        }

        public List<PlaylistItem> GetPlaylists()
        {
            List<PlaylistItem> playlists = new List<PlaylistItem>();
            XDocument doc = Request("getPlaylists");
            XElement root = doc.Root;
            if (root == null) return playlists;
            XElement playlistsElem = FindElement(root, "playlists");
            if (playlistsElem == null) return playlists;
            foreach (XElement pl in FindElements(playlistsElem, "playlist"))
            {
                PlaylistItem pi = new PlaylistItem();
                pi.Id = GetAttr(pl, "id");
                pi.Name = GetAttr(pl, "name");
                pi.Comment = GetAttr(pl, "comment");
                pi.SongCount = GetIntAttr(pl, "songCount");
                pi.DurationSeconds = GetIntAttr(pl, "duration");
                pi.Owner = GetAttr(pl, "owner");
                if (!string.IsNullOrEmpty(pi.Id))
                    playlists.Add(pi);
            }
            return playlists;
        }

        public List<TrackItem> GetPlaylistTracks(string playlistId)
        {
            List<TrackItem> tracks = new List<TrackItem>();
            XDocument doc = Request("getPlaylist", "id", playlistId);
            XElement root = doc.Root;
            if (root == null) return tracks;
            XElement playlist = FindElement(root, "playlist");
            if (playlist == null) return tracks;
            // Navidrome returns <entry> elements directly in <playlist>
            IEnumerable<XElement> entries = FindElements(playlist, "entry");
            foreach (XElement entry in entries)
            {
                TrackItem t = ParseSong(entry);
                if (t != null) tracks.Add(t);
            }
            // Fallback: try <songList>/<song> (standard Subsonic)
            if (tracks.Count == 0)
            {
                XElement songList = FindElement(playlist, "songList");
                if (songList != null)
                {
                    foreach (XElement song in FindElements(songList, "song"))
                    {
                        TrackItem t = ParseSong(song);
                        if (t != null) tracks.Add(t);
                    }
                }
            }
            return tracks;
        }

        public SearchResults Search(string query)
        {
            SearchResults results = new SearchResults();
            XDocument doc = Request("search2", "query", query, "songCount", "50", "artistCount", "20", "albumCount", "20");
            XElement root = doc.Root;
            if (root == null) return results;
            XElement searchResult = FindElement(root, "searchResult2");
            if (searchResult == null) return results;
            foreach (XElement song in FindElements(searchResult, "song"))
            {
                TrackItem t = ParseSong(song);
                if (t != null) results.Songs.Add(t);
            }
            foreach (XElement artist in FindElements(searchResult, "artist"))
            {
                ArtistItem a = ParseArtist(artist);
                if (a != null) results.Artists.Add(a);
            }
            foreach (XElement album in FindElements(searchResult, "album"))
            {
                AlbumItem a = ParseAlbum(album);
                if (a != null) results.Albums.Add(a);
            }
            return results;
        }

        public void Scrobble(string songId)
        {
            try { Request("scrobble", "id", songId, "submission", "true"); }
            catch { }
        }

        public void Star(string id)
        {
            try { Request("star", "id", id); }
            catch { }
        }

        public void Unstar(string id)
        {
            try { Request("unstar", "id", id); }
            catch { }
        }

        public List<TrackItem> GetStarred()
        {
            List<TrackItem> tracks = new List<TrackItem>();
            XDocument doc = Request("getStarred");
            XElement root = doc.Root;
            if (root == null) return tracks;
            XElement starred = FindElement(root, "starred");
            if (starred == null) return tracks;
            foreach (XElement song in FindElements(starred, "song"))
            {
                TrackItem t = ParseSong(song);
                if (t != null) tracks.Add(t);
            }
            return tracks;
        }

        private TrackItem ParseSong(XElement song)
        {
            string id = GetAttr(song, "id");
            if (string.IsNullOrEmpty(id)) return null;
            string coverArt = GetAttr(song, "coverArt");
            if (string.IsNullOrEmpty(coverArt)) coverArt = GetAttr(song, "albumId");
            return new TrackItem
            {
                Id = id,
                Title = GetAttr(song, "title"),
                Artist = GetAttr(song, "artist"),
                ArtistId = GetAttr(song, "artistId"),
                Album = GetAttr(song, "album"),
                AlbumId = GetAttr(song, "albumId"),
                CoverArtId = coverArt,
                Genre = GetAttr(song, "genre"),
                Year = GetIntAttr(song, "year"),
                TrackNumber = GetIntAttr(song, "track"),
                DiscNumber = GetIntAttr(song, "discNumber"),
                DurationSeconds = GetIntAttr(song, "duration"),
                IsStarred = !string.IsNullOrEmpty(GetAttr(song, "starred")),
                BitRate = GetIntAttr(song, "bitRate")
            };
        }

        private AlbumItem ParseAlbum(XElement album)
        {
            string id = GetAttr(album, "id");
            if (string.IsNullOrEmpty(id)) return null;
            string coverArt = GetAttr(album, "coverArt");
            if (string.IsNullOrEmpty(coverArt)) coverArt = id;
            return new AlbumItem
            {
                Id = id,
                Title = GetAttr(album, "name"),
                Artist = GetAttr(album, "artist"),
                ArtistId = GetAttr(album, "artistId"),
                CoverArtId = coverArt,
                Year = GetIntAttr(album, "year"),
                SongCount = GetIntAttr(album, "songCount"),
                DurationSeconds = GetIntAttr(album, "duration"),
                Genre = GetAttr(album, "genre")
            };
        }

        private ArtistItem ParseArtist(XElement artist)
        {
            string id = GetAttr(artist, "id");
            if (string.IsNullOrEmpty(id)) return null;
            return new ArtistItem
            {
                Id = id,
                Name = GetAttr(artist, "name"),
                CoverArtId = GetAttr(artist, "coverArt"),
                AlbumCount = GetIntAttr(artist, "albumCount")
            };
        }

        private string GetAttr(XElement elem, string name)
        {
            XAttribute attr = elem.Attribute(name);
            return attr != null ? attr.Value : string.Empty;
        }

        private int GetIntAttr(XElement elem, string name)
        {
            int result;
            if (int.TryParse(GetAttr(elem, name), out result))
                return result;
            return 0;
        }
    }

    public class SearchResults
    {
        public List<TrackItem> Songs = new List<TrackItem>();
        public List<ArtistItem> Artists = new List<ArtistItem>();
        public List<AlbumItem> Albums = new List<AlbumItem>();
    }
}
