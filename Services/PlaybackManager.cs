using System;
using System.Collections.Generic;

namespace WinSub
{
    public enum RepeatMode { Off, All, One }

    public class PlaybackManager
    {
        private List<TrackItem> _queue = new List<TrackItem>();
        private List<int> _shuffleOrder = new List<int>();
        private int _currentIndex = -1;
        private Random _random = new Random();
        private AudioPlayer _player;

        public List<TrackItem> Queue { get { return _queue; } }
        public int CurrentIndex { get { return _currentIndex; } }
        public TrackItem CurrentTrack
        {
            get
            {
                if (_currentIndex >= 0 && _currentIndex < _queue.Count)
                    return _queue[_currentIndex];
                return null;
            }
        }
        public bool Shuffle { get; set; }
        public RepeatMode Repeat { get; set; }

        public event EventHandler TrackChanged;
        public event EventHandler QueueChanged;
        public event EventHandler PlaybackFinished;

        public PlaybackManager(AudioPlayer player)
        {
            _player = player;
            _player.PlaybackFinished += OnPlayerFinished;
            _player.PlaybackStarted += OnPlaybackStarted;
        }

        private void OnPlaybackStarted(object sender, EventArgs e)
        {
            PreBufferNext();
        }

        public void PlayTrack(int index)
        {
            if (index < 0 || index >= _queue.Count) return;
            _currentIndex = index;

            if (Shuffle)
            {
                _shuffleOrder.Clear();
                for (int i = 0; i < _queue.Count; i++)
                    _shuffleOrder.Add(i);
                ShuffleList(_shuffleOrder);
                int pos = _shuffleOrder.IndexOf(index);
                if (pos > 0)
                {
                    _shuffleOrder.Remove(index);
                    _shuffleOrder.Insert(0, index);
                }
            }

            TrackItem track = _queue[index];
            string url = App.Client.GetStreamUrl(track.Id);
            _player.Play(url, track.Id);

            if (TrackChanged != null)
                TrackChanged(this, EventArgs.Empty);
        }

        private int GetNextIndex()
        {
            if (_queue.Count == 0) return -1;

            if (Repeat == RepeatMode.One)
                return _currentIndex;

            if (Shuffle && _shuffleOrder.Count > 0)
            {
                int pos = _shuffleOrder.IndexOf(_currentIndex);
                if (pos >= 0 && pos < _shuffleOrder.Count - 1)
                    return _shuffleOrder[pos + 1];
                if (Repeat == RepeatMode.All)
                    return _shuffleOrder[0];
                return -1;
            }

            int nextIndex = _currentIndex + 1;
            if (nextIndex >= _queue.Count)
            {
                if (Repeat == RepeatMode.All)
                    return 0;
                return -1;
            }
            return nextIndex;
        }

        private void PreBufferNext()
        {
            if (_currentIndex < 0) return;
            int nextIndex = GetNextIndex();
            if (nextIndex >= 0 && nextIndex < _queue.Count)
            {
                TrackItem nextTrack = _queue[nextIndex];
                string nextUrl = App.Client.GetStreamUrl(nextTrack.Id);
                _player.PreBufferNextTrack(nextTrack.Id, nextUrl);
            }
        }

        public void PlayQueue(List<TrackItem> tracks, int startIndex)
        {
            _queue.Clear();
            _queue.AddRange(tracks);
            _shuffleOrder.Clear();

            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);

            PlayTrack(startIndex);
        }

        public void AddToQueue(List<TrackItem> tracks)
        {
            _queue.AddRange(tracks);
            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);
            PreBufferNext();
        }

        public void AddToQueue(TrackItem track)
        {
            _queue.Add(track);
            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);
            PreBufferNext();
        }

        public void AddToQueueNext(TrackItem track)
        {
            int insertAt = _currentIndex >= 0 ? _currentIndex + 1 : _queue.Count;
            _queue.Insert(insertAt, track);
            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);
            PreBufferNext();
        }

        public void RemoveFromQueue(int index)
        {
            if (index < 0 || index >= _queue.Count) return;
            _queue.RemoveAt(index);
            if (index < _currentIndex)
                _currentIndex--;
            else if (index == _currentIndex)
            {
                if (_currentIndex >= _queue.Count)
                    _currentIndex = _queue.Count - 1;
            }
            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);
            PreBufferNext();
        }

        public void ClearQueue()
        {
            _player.Stop();
            _queue.Clear();
            _currentIndex = -1;
            _shuffleOrder.Clear();
            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);
        }

        public void MoveTrack(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _queue.Count) return;
            if (toIndex < 0 || toIndex >= _queue.Count) return;

            TrackItem track = _queue[fromIndex];
            _queue.RemoveAt(fromIndex);
            _queue.Insert(toIndex, track);

            if (_currentIndex == fromIndex)
                _currentIndex = toIndex;
            else if (fromIndex < _currentIndex && toIndex >= _currentIndex)
                _currentIndex--;
            else if (fromIndex > _currentIndex && toIndex <= _currentIndex)
                _currentIndex++;

            if (QueueChanged != null)
                QueueChanged(this, EventArgs.Empty);

            PreBufferNext();
        }

        public void Next()
        {
            if (_queue.Count == 0) return;

            if (Repeat == RepeatMode.One)
            {
                PlayTrack(_currentIndex);
                return;
            }

            if (Shuffle && _shuffleOrder.Count > 0)
            {
                int pos = _shuffleOrder.IndexOf(_currentIndex);
                if (pos >= 0 && pos < _shuffleOrder.Count - 1)
                {
                    PlayTrack(_shuffleOrder[pos + 1]);
                    return;
                }
                if (Repeat == RepeatMode.All)
                {
                    PlayTrack(_shuffleOrder[0]);
                    return;
                }
                _player.Stop();
                if (PlaybackFinished != null)
                    PlaybackFinished(this, EventArgs.Empty);
                return;
            }

            int nextIndex = _currentIndex + 1;
            if (nextIndex >= _queue.Count)
            {
                if (Repeat == RepeatMode.All)
                    nextIndex = 0;
                else
                {
                    _player.Stop();
                    if (PlaybackFinished != null)
                        PlaybackFinished(this, EventArgs.Empty);
                    return;
                }
            }

            PlayTrack(nextIndex);
        }

        public void Previous()
        {
            if (_queue.Count == 0) return;

            if (_player.CurrentTime.TotalSeconds > 3)
            {
                _player.Seek(TimeSpan.Zero);
                return;
            }

            if (Shuffle && _shuffleOrder.Count > 0)
            {
                int pos = _shuffleOrder.IndexOf(_currentIndex);
                if (pos > 0)
                {
                    PlayTrack(_shuffleOrder[pos - 1]);
                    return;
                }
                if (Repeat == RepeatMode.All)
                {
                    PlayTrack(_shuffleOrder[_shuffleOrder.Count - 1]);
                    return;
                }
                PlayTrack(_currentIndex);
                return;
            }

            int prevIndex = _currentIndex - 1;
            if (prevIndex < 0)
            {
                if (Repeat == RepeatMode.All)
                    prevIndex = _queue.Count - 1;
                else
                    prevIndex = 0;
            }

            PlayTrack(prevIndex);
        }

        public void ToggleShuffle()
        {
            Shuffle = !Shuffle;
            if (Shuffle && _queue.Count > 0)
            {
                _shuffleOrder.Clear();
                for (int i = 0; i < _queue.Count; i++)
                    _shuffleOrder.Add(i);
                ShuffleList(_shuffleOrder);
            }
            PreBufferNext();
        }

        public void ToggleRepeat()
        {
            if (Repeat == RepeatMode.Off)
                Repeat = RepeatMode.All;
            else if (Repeat == RepeatMode.All)
                Repeat = RepeatMode.One;
            else
                Repeat = RepeatMode.Off;
            PreBufferNext();
        }

        private void ShuffleList(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void OnPlayerFinished(object sender, EventArgs e)
        {
            Next();
        }
    }
}
