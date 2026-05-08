using System.IO;
using MusicLib;
using TagLib;

namespace WpfApp
{
    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        public StreamFileAbstraction(string name, Stream stream)
        {
            Name = name;
            ReadStream = stream;
            WriteStream = stream;
        }

        public string Name { get; private set; }
        public Stream ReadStream { get; private set; }
        public Stream WriteStream { get; private set; }

        public void CloseStream(Stream stream)
        {
            // Usually, you don't want to close the stream here if you 
            // plan to use it later, but TagLib calls this when done.
            stream.Position = 0;
        }
    }

    public class WindowsSongPlayContext : SongPlayContext
    {
        public WindowsSongPlayContext(FileProvider fileProvider)
            : base(fileProvider)
        {

        }

        public override void PlayCurrentSong()
        {
            base.PlayCurrentSong();

            using (Stream songStream = SongFileProvider.GetFileStream(CurrentSong.FileName))
            {
                var tagFile = TagLib.File.Create(new StreamFileAbstraction(CurrentSong.FileName, songStream));

                double gain = tagFile.Tag.ReplayGainAlbumGain;
            }
        }
    }
}
