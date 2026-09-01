using System;
using System.Collections.Generic;
using System.Threading;

namespace SubsonicPlayer
{
    public class AutoDJService
    {
        private bool _enabled;
        private int _queueThreshold = 3;
        private int _fetchCount = 10;
        private bool _fetching;
        private HashSet<string> _recentTrackIds = new HashSet<string>();

        public bool Enabled { get { return _enabled; } }
        public int QueueThreshold { get { return _queueThreshold; } set { _queueThreshold = value; } }
        public int FetchCount { get { return _fetchCount; } set { _fetchCount = value; } }

        public event EventHandler TracksAdded;

        public AutoDJService()
        {
            App.Playback.QueueChanged += OnQueueChanged;
            App.Playback.TrackChanged += OnTrackChanged;
        }

        public void Toggle()
        {
            _enabled = !_enabled;
            if (_enabled)
                CheckAndFill();
        }

        private void OnTrackChanged(object sender, EventArgs e)
        {
            TrackItem track = App.Playback.CurrentTrack;
            if (track != null)
            {
                _recentTrackIds.Add(track.Id);
                if (_recentTrackIds.Count > 100)
                    _recentTrackIds.Clear();
            }
            if (_enabled)
                CheckAndFill();
        }

        private void OnQueueChanged(object sender, EventArgs e)
        {
            if (_enabled)
                CheckAndFill();
        }

        private void CheckAndFill()
        {
            if (_fetching) return;
            if (App.Playback.Queue.Count == 0) return;

            int remaining = App.Playback.Queue.Count - App.Playback.CurrentIndex - 1;
            if (remaining > _queueThreshold) return;

            TrackItem current = App.Playback.CurrentTrack;
            if (current == null) return;

            _fetching = true;
            string genre = current.Genre;
            string artistId = current.ArtistId;

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    List<TrackItem> candidates = new List<TrackItem>();

                    if (!string.IsNullOrEmpty(genre))
                    {
                        try
                        {
                            List<TrackItem> byGenre = App.Client.GetSongsByGenre(genre, _fetchCount * 2);
                            candidates.AddRange(byGenre);
                        }
                        catch { }
                    }

                    if (candidates.Count < _fetchCount)
                    {
                        try
                        {
                            List<TrackItem> random = App.Client.GetRandomSongs(_fetchCount * 2);
                            candidates.AddRange(random);
                        }
                        catch { }
                    }

                    List<TrackItem> toAdd = new List<TrackItem>();
                    foreach (TrackItem t in candidates)
                    {
                        if (toAdd.Count >= _fetchCount) break;
                        if (_recentTrackIds.Contains(t.Id)) continue;
                        bool alreadyInQueue = false;
                        foreach (TrackItem q in App.Playback.Queue)
                        {
                            if (q.Id == t.Id) { alreadyInQueue = true; break; }
                        }
                        if (!alreadyInQueue)
                        {
                            toAdd.Add(t);
                            _recentTrackIds.Add(t.Id);
                        }
                    }

                    if (toAdd.Count > 0)
                    {
                        App.Playback.AddToQueue(toAdd);
                        if (TracksAdded != null)
                            TracksAdded(this, EventArgs.Empty);
                    }
                }
                catch { }
                finally
                {
                    _fetching = false;
                }
            });
        }
    }
}
