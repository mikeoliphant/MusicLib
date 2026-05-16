
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MusicLib;
using SharpCifs.Netbios;
using SharpCifs.Util.Transport;
using TagLib.Ape;

namespace MusicLib
{
    public class SongPlayContext
    {
        public static string HttpListenUrl = "http://*:8080/";

        public SongIndex SongIndex { get; private set; }
        public FileProvider SongFileProvider { get; private set; }
        public SongData CurrentSong { get; private set; }

        public event EventHandler SongChanged;
        HttpListener httpListener;

        int currentSongIndex = 0;

        public SongPlayContext(FileProvider songFileProvider)
        {
            this.SongFileProvider = songFileProvider;

            Task.Run(RunHttpServer);
        }

        static bool StartsWith(string str, params string[] words)
        {
            foreach (string word in words)
            {
                if (str.StartsWith(word, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
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

        public async Task SetPlaylist(string playlistName)
        {
            SongIndex songIndex = null;

            using (Stream indexStream = SongFileProvider.GetFileStream("index.json"))
            {
                songIndex = SongIndex.ReadFromJson(indexStream);
            }

            // Exclude songs that are too short or too long
            songIndex.Songs = songIndex.Songs.Where(s => (s.PlayTime > 60) && (s.PlayTime < (6 * 60))).ToList();

            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(songIndex.Songs));

            switch (playlistName)
            {
                case "rockish":
                    songIndex.Songs = songIndex.Songs.Where(s => !StartsWith(s.Genre, "Chorus", "Classical", "Blues", "Jazz", "Celtic", "World")).ToList();
                    break;
                case "rockish_new":
                    songIndex.Songs = songIndex.Songs.Where(s => !StartsWith(s.Genre, "Chorus", "Classical", "Blues", "Jazz", "Celtic", "World")).ToList();

                    var newSongs = songIndex.Songs.Where(s => (s.DateAdded > DateOnly.FromDateTime(DateTime.Now - TimeSpan.FromDays(30)))).ToList();

                    songIndex.Songs.RemoveAll(s => newSongs.Contains(s));

                    int insertIndex = 0;

                    foreach (var song in newSongs)
                    {
                        songIndex.Songs.Insert(insertIndex, song);

                        insertIndex += 2 + Random.Shared.Next(3);
                    }

                    break;
                case "rock":
                    songIndex.Songs = songIndex.Songs.Where(s =>
                        ((s.Genre == "Rock") || (s.Genre == "Rock") || (s.Genre == "Alternative") || (s.Genre == "Alternative Rock") || (s.Genre == "Hard Rock") || (s.Genre == "Grunge") || (s.Genre == "Garage"))
                        && (s.Year > 1965)
                        ).ToList();

                    break;
            }

            SongIndex = songIndex;

            CurrentSong = SongIndex.Songs[currentSongIndex];

            PlayCurrentSong();
        }

        async Task RunHttpServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(HttpListenUrl);

            httpListener.Start();

            while (true)
            {
                try
                {
                    HttpListenerContext context = httpListener.GetContext();
                    HttpListenerRequest request = context.Request;

                    if (request.HttpMethod == "POST")
                    {
                        switch (request.Url.AbsolutePath)
                        {
                            case "/next_song":
                                NextSong();
                                break;
                            case "/playlist":
                                string playlistType = request.QueryString.Get("type");

                                SetPlaylist(playlistType);

                                break;
                        }

                    }

                    using HttpListenerResponse response = context.Response;

                    string responseString = $"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Currently Playing</title>
</head>
<body>
    <h1>{CurrentSong.Title}</h1>
    <h1>{CurrentSong.Artist}</h1>
    <h1>{CurrentSong.Album} ({CurrentSong.Year}) - {CurrentSong.Genre}</h1>
    <form method="post">
        <button formaction="/next_song" type="submit">Next Song</button>
        <button formaction="/playlist?type=rockish" type="submit">Rockish</button>
        <button formaction="/playlist?type=rockish_new" type="submit">Rockish New</button>
        <button formaction="/playlist?type=rock" type="submit">Rock Only</button>
    </form>
  </body>
</html>
""";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html";

                    using Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {

                }                
            }
        }
    }
}
