using MusicLib;

namespace AndroidPlayer
{
    public class AndroidSongPlayContext : SongPlayContext
    {
        Android.Media.MediaPlayer player;

        public AndroidSongPlayContext(FileProvider fileProvider)
            : base(fileProvider)
        {
            player = new();

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

                string tempPath = Path.Combine(Android.App.Application.Context.CacheDir.Path, "temp.mp3");

                using (Stream mp3Stream = SongFileProvider.GetFileStream(CurrentSong.FileName))
                {
                    using (var fileStream = File.Create(tempPath))
                    {
                        mp3Stream.CopyTo(fileStream);
                    }
                }

                var tagFile = TagLib.File.Create(tempPath);

                float dbGain = (float)tagFile.Tag.ReplayGainTrackGain;

                float dbOffset = -11;

                if (!double.IsNaN(dbGain))
                {
                    float linearGain = (float)Math.Pow(10, ((dbGain + dbOffset) / 20));

                    player.SetVolume(linearGain, linearGain);
                }

                player.SetDataSource(tempPath);
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
