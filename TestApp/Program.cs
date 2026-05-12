using System.Diagnostics;
using MusicLib;
using TagLib;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //CreateIndex(@"\\USBDISK\Share\MusicNew");

            SongIndex songIndex = SongIndex.ReadFromJson(@"\\USBDISK\Share\MusicNew\index.json");
            //SongIndex songIndex = SongIndex.ReadFromJson(@"C:\Share\MusicNew\index.json");

            //var albums = songIndex.GetAllAlbums();

            //foreach (var album in albums)
            //{
            //    string blah = await client.GetReleaseGroup(album[0]);
            //}

            ImportAlbums(songIndex, @"C:\Users\oliph\Downloads\The Lemon Twigs - A Dream Is All We Know");
           
            songIndex.WriteToJson();
        }

        static void ImportAlbums(SongIndex songIndex, string fromPath)
        {
            Process rsGain = Process.Start(@"C:\Share\Audio-Video\rsgain-3.7-win64\rsgain.exe", "easy \"" + fromPath + "\"");

            rsGain.WaitForExit();

            SongIndex fromIndex = SongIndex.CreateFromPath(fromPath);

            fromIndex.AddVariousArtists();

            fromIndex.CopySongsToPath(songIndex.RootPath);
            fromIndex.CopyAdditionalAlbumFilesToPath(songIndex.RootPath);

            foreach (var song in fromIndex.Songs)
            {
                song.FileName = SongIndex.GetSongPath(song);

                if (!songIndex.Songs.Where(s => s.FileName == song.FileName).Any())
                {
                    song.DateAdded = DateOnly.FromDateTime(DateTime.Now);

                    songIndex.Songs.Add(song);
                }
            }
        }

        static async Task FixMissingYears(SongIndex songIndex)
        {
            MusicBrainzClient client = new();

            var albums = songIndex.GetAllAlbums().Where(a => a.Where(s => s.Year == 0).Any()).ToList();

            foreach (var album in albums)
            {
                var releaseGroup = await client.GetReleaseGroup(album[0]);

                if (releaseGroup.ReleaseGroups.Count > 0)
                {
                    string yearStr = releaseGroup.ReleaseGroups[0].FirstReleaseDate;

                    if (!string.IsNullOrEmpty(yearStr) && yearStr.Length > 3)
                    {
                        uint year = 0;

                        if (uint.TryParse(yearStr.Substring(0, 4), out year))
                        {
                            foreach (var song in album)
                            {
                                if (song.Year == 0)
                                {
                                    song.Year = year;

                                    var tagFile = TagLib.File.Create(Path.Combine(songIndex.RootPath, song.FileName));

                                    tagFile.Tag.Year = song.Year;

                                    tagFile.Save();
                                }
                            }
                        }
                    }
                }
            }
        }

        static SongIndex CreateIndex(string path)
        {
            SongIndex songIndex = SongIndex.CreateFromPath(path);

            songIndex.AddVariousArtists();

            FixMultiFolderAlbums(songIndex);

            songIndex.WriteToJson();

            return songIndex;
        }

        static void FixAlbumNames(SongIndex songIndex)
        {
            Dictionary<string, List<SongData>> albums = new();

            foreach (SongData songData in songIndex.Songs)
            {
                string albumDir = Path.GetDirectoryName(songData.FileName);

                if (!albums.ContainsKey(albumDir))
                    albums[albumDir] = new();

                albums[albumDir].Add(songData);
            }

            foreach (var album in albums.Values)
            {
                var names = album.Select(a => a.Album).Distinct().ToList();

                if (names.Count > 1)
                {
                    string longest = names.OrderByDescending(n => n.Length).First();

                    foreach (var song in album)
                    {
                        var tagFile = TagLib.File.Create(Path.Combine(songIndex.RootPath, song.FileName));

                        if (tagFile.Tag.Album != longest)
                        {
                            tagFile.Tag.Album = longest;

                            tagFile.Save();
                        }
                    }
                }
            }
        }

        static void FixMultiFolderAlbums(SongIndex songIndex)
        {
            Dictionary<string, List<SongData>> albums = new();

            foreach (SongData songData in songIndex.Songs)
            {
                string albumDir = Path.GetDirectoryName(songData.FileName);

                if (!albums.ContainsKey(albumDir))
                    albums[albumDir] = new();

                albums[albumDir].Add(songData);
            }

            HashSet<string> fixedAlbums = new();

            foreach (var album in albums.Values)
            {
                if (album.Count < 5)
                {
                    string albumName = album[0].Album;

                    var dupes = songIndex.Songs.Where(s => s.Album == albumName).ToList();

                    if (dupes.Count > album.Count)
                    {
                        if (!fixedAlbums.Contains(albumName))
                        {
                            foreach (var song in dupes)
                            {
                                song.AlbumArtist = "Various";
                            }

                            fixedAlbums.Add(albumName);
                        }

                    }
                }
            }
        }

        static void FixTrackNumbers(SongIndex songIndex)
        {
            Dictionary<string, List<SongData>> albums = new();

            foreach (SongData songData in songIndex.Songs)
            {
                string albumDir = Path.GetDirectoryName(songData.FileName);

                if (!albums.ContainsKey(albumDir))
                    albums[albumDir] = new();

                albums[albumDir].Add(songData);
            }

            foreach (var album in albums.Values)
            {
                if (album.Select(s => s.TrackNumber).Distinct().Count() != album.Count)
                {
                    foreach (var song in album)
                    {
                        var tag = TagLib.File.Create(Path.Combine(songIndex.RootPath, song.FileName));

                        var id3v1Tag = tag.GetTag(TagTypes.Id3v1) as TagLib.Id3v1.Tag;

                        if (tag.Tag.Track != id3v1Tag.Track)
                        {
                            tag.Tag.Track = id3v1Tag.Track;

                            tag.Save();
                        }
                    }
                }
            }
        }
    }
}
