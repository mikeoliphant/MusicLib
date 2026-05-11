using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MusicLib
{
    public class MusicBrainzClient
    {
        HttpClient httpClient;
        DateTime lastRequest = DateTime.MinValue;

        public string CacheFolder = @"C:\Share\MusicBrainz";

        public MusicBrainzClient()
        {
            httpClient = new();

            httpClient.DefaultRequestHeaders.Add("User-Agent", "MusicLib/1.0.0 (contact@nostatic.org)");

            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);
        }

        public async Task<MusicBrainzReleaseGroups> GetReleaseGroup(SongData song)
        {
            string cachePath = Path.Combine(CacheFolder, SongIndex.GetAlbumPath(song));

            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);

            string cacheFile = Path.Combine(cachePath, "ReleaseGroup.json");

            string json = null;

            if (File.Exists(cacheFile))
            {
                json = File.ReadAllText(cacheFile);
            }
            else
            {
                if ((DateTime.Now - lastRequest).TotalSeconds < 1)
                {
                    await Task.Delay(2000);
                }

                string query = Uri.EscapeDataString($"artist:\"{song.AlbumArtist}\" AND release:\"{song.Album}\"");
                string url = $"https://musicbrainz.org/ws/2/release-group?query={query}&status:official&fmt=json";

                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                json = await response.Content.ReadAsStringAsync();

                lastRequest = DateTime.Now;

                File.WriteAllText(cacheFile, json);
            }

            return JsonSerializer.Deserialize<MusicBrainzReleaseGroups>(json);
        }
    }

    public class MusicBrainzReleaseGroups
    {
        [JsonPropertyName("release-groups")]
        public List<MusicBrainzReleaseGroup> ReleaseGroups { get; set; }
    }

    public class MusicBrainzReleaseGroup
    {
        [JsonPropertyName("first-release-date")]
        public string FirstReleaseDate { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}
