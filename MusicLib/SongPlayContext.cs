using MusicLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TagLib.Ape;

namespace MusicLib
{
    public class SongPlayContext
    {
        public SongIndex SongIndex { get; private set; }
        public FileProvider SongFileProvider { get; private set; }
        public SongData CurrentSong { get; private set; }

        public event EventHandler SongChanged;

        int currentSongIndex = 0;

        public SongPlayContext(FileProvider songFileProvider)
        {
            this.SongFileProvider = songFileProvider;

            using (Stream indexStream = songFileProvider.GetFileStream("index.json"))
            {
                SongIndex = SongIndex.ReadFromJson(indexStream);
            }

            SongIndex.Songs = SongIndex.Songs.Where(s => !((s.Genre.StartsWith("Chorus") || (s.Genre.StartsWith("Classical"))))).ToList();

            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(SongIndex.Songs));

            CurrentSong = SongIndex.Songs[currentSongIndex];
        }

        public void NextSong()
        {
            currentSongIndex++;

            CurrentSong = SongIndex.Songs[currentSongIndex];

            PlayCurrentSong();
        }

        public virtual void PlayCurrentSong()
        {
            SongChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Pause()
        {

        }

        public virtual void Stop()
        {

        }

        public virtual void Play()
        {

        }
    }
}
