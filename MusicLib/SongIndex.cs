using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace MusicLib
{
    public class SongIndex
    {
        public string RootPath { get; set; } = "";
        public List<SongData> Songs { get; set; } = new();

        public void WriteToJson()
        {
            if (string.IsNullOrWhiteSpace(RootPath))
                throw new InvalidOperationException("RootPath is not set");

            WriteToJson(Path.Combine(RootPath, "index.json"));
        }

        public void WriteToJson(string outputPath)
        {
            var serializerOptions = new JsonSerializerOptions()
            {
                Converters = {
                   new JsonStringEnumConverter()
                },
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { AlbumArtistModifier }
                },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true
            };
            
            using (FileStream stream = File.Create(outputPath))
            {
                JsonSerializer.Serialize(stream, this, serializerOptions);
            }
        }

        static void AlbumArtistModifier(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Type != typeof(SongData)) return;

            foreach (var property in typeInfo.Properties)
            {
                if (property.Name == "AlbumArtist")
                {
                    property.ShouldSerialize = (obj, value) =>
                        ((SongData)obj).AlbumArtist != ((SongData)obj).Artist;
                }
            }
        }
        public static SongIndex ReadFromJson(string jsonPath)
        {
            using (FileStream stream = File.OpenRead(jsonPath))
            {
                SongIndex index = ReadFromJson(stream);

                index.RootPath = Path.GetDirectoryName(jsonPath);

                return index;
            }
        }

        public static SongIndex ReadFromJson(Stream jsonStream)
        {
            return JsonSerializer.Deserialize<SongIndex>(jsonStream);
        }

        public static SongIndex CreateFromPath(string libraryPath)
        {
            SongIndex index = new();

            index.RootPath = libraryPath;

            index.AddFolder(libraryPath);

            return index;
        }

        void AddFolder(string folderPath)
        {
            foreach (string folder in Directory.GetDirectories(folderPath))
            {
                AddFolder(folder);
            }

            foreach (string songFile in Directory.GetFiles(folderPath, "*.mp3",
                new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                }))
            {
                try
                {
                    var tagFile = TagLib.File.Create(songFile);

                    SongData songData = new()
                    {
                        Title = tagFile.Tag.Title,
                        Artist = tagFile.Tag.Performers[0],
                        Album = tagFile.Tag.Album,
                        Year = tagFile.Tag.Year,
                        Genre = tagFile.Tag.Genres[0],
                        TrackNumber = tagFile.Tag.Track,
                        FileName = Path.GetRelativePath(RootPath, songFile),
                        PlayTime = (uint)tagFile.Properties.Duration.TotalSeconds
                    };

                    Songs.Add(songData);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public static string GetArtistPath(SongData song)
        {
            return GetSafeFilename(song.AlbumArtist);
        }

        public static string GetAlbumPath(SongData song)
        {
            return Path.Combine(GetArtistPath(song), GetSafeFilename(song.Album));
        }

        public static string GetSongPath(SongData song)
        {
            return Path.Combine(GetAlbumPath(song), GetSafeFilename(song.Title)) + ".mp3";
        }

        public static string GetSafeFilename(string path)
        {
            var normalizedString = path.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            string normalized = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            return Regex.Replace(normalized, "[^a-zA-Z0-9]", String.Empty).Trim();
        }

        public List<List<SongData>> GetAllAlbums()
        {
            Dictionary<string, List<SongData>> albums = new();

            foreach (SongData songData in Songs)
            {
                string albumDir = Path.GetDirectoryName(songData.FileName);

                if (!albums.ContainsKey(albumDir))
                    albums[albumDir] = new();

                albums[albumDir].Add(songData);
            }

            return albums.Values.ToList();
        }

        public void AddVariousArtists()
        {
            Dictionary<string, List<SongData>> albums = new();

            foreach (SongData songData in Songs)
            {
                string albumDir = Path.GetDirectoryName(songData.FileName);

                if (!albums.ContainsKey(albumDir))
                    albums[albumDir] = new();

                albums[albumDir].Add(songData);
            }

            foreach (var album in albums.Values)
            {
                string albumArtist = "";

                var counts = album.Select(a => a.Artist).GroupBy(x => x)
                  .ToDictionary(g => g.Key, g => g.Count());

                if (counts.Count > 1)
                {
                    int max = counts.Max(c => c.Value);

                    if (max > album.Count / 2)
                    {
                        albumArtist = counts.Where(c => c.Value == max).FirstOrDefault().Key;
                    }
                    else
                    {
                        albumArtist = "Various";
                    }

                    foreach (SongData song in album)
                    {
                        // Patch up incomplete artist names
                        if (albumArtist.StartsWith(song.Artist))
                        {
                            song.Artist = albumArtist;
                        }
                        else
                        {
                            song.AlbumArtist = albumArtist;
                        }
                    }
                }
            }
        }

        public void CopySongsToPath(string path)
        {
            foreach (SongData song in Songs)
            {
                string artistDir = Path.Combine(path, GetArtistPath(song));

                if (!Directory.Exists(artistDir))
                {
                    Directory.CreateDirectory(artistDir);
                }

                string albumDir = Path.Combine(path, GetAlbumPath(song));

                if (!Directory.Exists(albumDir))
                {
                    Directory.CreateDirectory(albumDir);
                }

                string songFile = Path.Combine(path, GetSongPath(song));

                File.Copy(Path.Combine(RootPath, song.FileName), songFile, overwrite: true);
            }
        }

        public void CopyAdditionalAlbumFilesToPath(string path)
        {
            var albums = GetAllAlbums();

            foreach (var album in albums)
            {
                string srcFolder = Path.Combine(RootPath, Path.GetDirectoryName(album[0].FileName));
                string destFolder = Path.Combine(path, GetAlbumPath(album[0]));

                foreach (string file in Directory.GetFiles(Path.Combine(RootPath, srcFolder)))
                {
                    if (Path.GetExtension(file).ToLower() != ".mp3")
                    {
                        File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), overwrite: true);
                    }
                }
            }
        }
    }

    public class SongData
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string AlbumArtist
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(field))
                    return field;

                return Artist;
            }

            set
            {
                field = value;
            }
        }
        public uint Year { get; set; }
        public string Genre { get; set; } = "";
        public uint TrackNumber { get; set; }
        public uint PlayTime { get; set; }
        public DateOnly DateAdded { get; set; } = DateOnly.MinValue;
        public string FileName { get; set; } = "";
    }
}
