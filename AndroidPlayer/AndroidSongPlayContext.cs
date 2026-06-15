using Android.Media;
using MusicLib;
using SkiaSharp;

namespace AndroidPlayer
{
    public class StreamMediaDataSource : MediaDataSource, TagLib.File.IFileAbstraction
    {
        private readonly System.IO.Stream sourceStream;
        private readonly object _lock = new object();

        public StreamMediaDataSource(System.IO.Stream sourceStream)
        {
            if (!sourceStream.CanSeek)
                throw new ArgumentException("The provided stream must be seekable.", nameof(sourceStream));

            this.sourceStream = sourceStream;
        }

        // Android queries this to know how large the resource is (in bytes)
        public override long Size => sourceStream.Length;

        public string Name { get; set;  }

        public System.IO.Stream ReadStream => sourceStream;

        public System.IO.Stream WriteStream => sourceStream;

        // Android invokes this method repeatedly to pull audio binary chunks
        public override int ReadAt(long position, byte[] buffer, int offset, int size)
        {
            lock (_lock)
            {
                try
                {
                    // Ensure the underlying stream matches Android's read head position
                    if (sourceStream.Position != position)
                    {
                        sourceStream.Seek(position, SeekOrigin.Begin);
                    }

                    // Read directly from the .NET stream into Android's buffer wrapper
                    int bytesRead = sourceStream.Read(buffer, offset, size);

                    // Return -1 if End-of-Stream is encountered, otherwise return byte count
                    return bytesRead == 0 ? -1 : bytesRead;
                }
                catch (Exception)
                {
                    return -1; // Notify MediaPlayer that an unrecoverable stream read failure occurred
                }
            }
        }

        public override void Close()
        {
            lock (_lock)
            {
                sourceStream?.Dispose();
            }
        }

        public void CloseStream(System.IO.Stream stream)
        {
            //sourceStream.Flush();
        }
    }

    public class AndroidSongPlayContext : SongPlayContext
    {
        Android.Media.MediaPlayer player;

        public AndroidSongPlayContext(FileProvider fileProvider)
            : base(fileProvider)
        {
            player = new();

            var audioAttributes = new AudioAttributes.Builder()
                .SetContentType(AudioContentType.Music)
                .SetUsage(AudioUsageKind.Media)
                .Build();

            player.SetAudioAttributes(audioAttributes);

            player.Completion += Player_Completion;
        }

        private void Player_Completion(object? sender, EventArgs e)
        {
            NextSong();
        }

        public override void PlayCurrentSong()
        {
            base.PlayCurrentSong();

            try
            {
                player.Stop();
                player.Reset();

                StreamMediaDataSource mp3Stream = new StreamMediaDataSource(SongFileProvider.GetFileStream(CurrentSong.FileName))
                {
                    Name = CurrentSong.FileName
                };

                //{

                //    string tempPath = Path.Combine(Android.App.Application.Context.CacheDir.Path, "temp" + Path.GetExtension(CurrentSong.FileName));

                //    using (var fileStream = File.Create(tempPath))
                //    {
                //        mp3Stream.CopyTo(fileStream);
                //    }
                //}

                var tagFile = TagLib.File.Create(mp3Stream);

                float dbGain = (float)tagFile.Tag.ReplayGainTrackGain;

                float dbOffset = -11;

                if (!double.IsNaN(dbGain))
                {
                    float linearGain = (float)Math.Pow(10, ((dbGain + dbOffset) / 20));

                    player.SetVolume(linearGain, linearGain);
                }

                player.SetDataSource(mp3Stream);
                player.Prepare();
                player.Start();
            }
            catch (Exception ex)
            {

            }
        }

        public override void Stop()
        {
            base.Stop();

            player.Stop();
            player.Reset();
        }

        public override void Play()
        {
            base.Play();

            player.Start();
        }

        public override void Pause()
        {
            base.Pause();

            player.Pause();
        }
    }
}
