
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MusicLib;
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

            using (Stream indexStream = songFileProvider.GetFileStream("index.json"))
            {
                SongIndex = SongIndex.ReadFromJson(indexStream);
            }

            SongIndex.Songs = SongIndex.Songs.Where(s => !StartsWith(s.Genre, "Chorus", "Classical", "Blues", "Jazz", "Celtic", "World")).ToList();

            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(SongIndex.Songs));

            CurrentSong = SongIndex.Songs[currentSongIndex];

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

                    if (request.HttpMethod == "GET")
                    {
                        using HttpListenerResponse response = context.Response;
                        string responseString = JsonSerializer.Serialize<SongData>(CurrentSong);

                        byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "application/json";

                        using Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                    }
                    else if (request.HttpMethod == "POST")
                    {
                        NextSong();

                        using HttpListenerResponse response = context.Response;

                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.OutputStream.Close();
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
