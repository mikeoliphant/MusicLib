using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MusicLib
{
    public class UnsplashSearchResponse
    {
        public int Total { get; set; }
        public Photo[] Results { get; set; }
    }

    public class Photo
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public Urls Urls { get; set; }
    }

    public class Urls
    {
        public string Raw { get; set; }
        public string Full { get; set; }
        public string Regular { get; set; }
        public string Small { get; set; }
        public string Thumb { get; set; }
    }

    public class UnsplashClient
    {
        string accessKey;
        HttpClient client = new HttpClient();

        public UnsplashClient(string accessKey)
        {
            this.accessKey = accessKey;
        }

        public async Task<UnsplashSearchResponse> SearchPhotos(string searchString)
        {
            string url = $"https://api.unsplash.com/search/photos?query={searchString}&client_id={accessKey}&orientation=landscape&per_page=30";

            try
            {
                var response = await client.GetStringAsync(url);
                var results = JsonSerializer.Deserialize<UnsplashSearchResponse>(response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null;
        }
    }
}
