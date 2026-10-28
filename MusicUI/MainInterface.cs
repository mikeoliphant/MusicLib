using System.IO;
using System.Net.Http;
using MusicLib;
using SkiaSharp;
using UILayout;

namespace MusicUI
{
    public class MainInterface : Dock
    {
        public SongPlayContext PlayContext { get; private set; }

        OutlinedText songTitleText;
        OutlinedText songArtistText;
        OutlinedText timeText;
        UIElementWrapper splashWrapper;
        SongData lastSongData = null;
        UnsplashSearchResponse currentPhotos;
        int currentPhotoIndex = 0;
        HttpClient httpClient = new HttpClient();
        Random random = new();
        bool needPhotoSkip = false;

        public MainInterface(SongPlayContext playContext)
        {
            PlayContext = playContext;

            Layout.Current.DefaultFont = new UIFont { Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), TextSize = 48 };

            PlayContext.SongChanged += PlayContext_SongChanged;

            BackgroundColor = UIColor.Black;

            splashWrapper = new UIElementWrapper { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Stretch };
            Children.Add(splashWrapper);

            Dock uiDock = new Dock { Margin = 20 };
            Children.Add(uiDock);

            VerticalStack songInfoStack = new VerticalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Left,
                VerticalAlignment = EVerticalAlignment.Bottom,
                ChildSpacing = 20
            };

            uiDock.Children.Add(songInfoStack);

            songTitleText = new OutlinedText();
            songArtistText = new OutlinedText();

            songInfoStack.Children.Add(songTitleText);
            songInfoStack.Children.Add(songArtistText);

            timeText = new OutlinedText
            {
                HorizontalAlignment = EHorizontalAlignment.Right,
                VerticalAlignment = EVerticalAlignment.Top
            };

            uiDock.Children.Add(timeText);

            try
            {
                using (StreamReader reader = new StreamReader(PlayContext.SongFileProvider.GetInputStream("Blacklist.txt")))
                {
                    string blacklist = reader.ReadToEnd();

                    UnsplashClient.UserBlacklist.Clear();

                    foreach (string user in blacklist.Split(','))
                    {
                        UnsplashClient.UserBlacklist.Add(user);
                    }
                }
            }
            catch { }

            UpdateDisplay();

            Task.Run(CycleImages);
        }

        async Task CycleImages()
        {
            if (PlayContext.CurrentSong == null)
            {
                await PlayContext.SetPlaylist("rockish_new");

                UpdateDisplay();
            }

            while (true)
            {
                for (int i = 0; i < 30; i++)
                {
                    if (needPhotoSkip)
                    {
                        needPhotoSkip = false;

                        break;
                    }

                    if (PlayContext.CurrentSong != lastSongData)
                    {
                        lastSongData = PlayContext.CurrentSong;

                        currentPhotos = await GetSplashPhotos();

                        currentPhotoIndex = 0;

                        break;
                    }

                    await Task.Delay(1000);
                }

                if ((currentPhotos != null) && (currentPhotos.Results.Count > 0))
                {
                    try
                    {
                        using (Stream photoStream = await GetSplashPhotoStream(currentPhotos.Results[currentPhotoIndex], (int)Layout.Current.Bounds.Width, (int)Layout.Current.Bounds.Height))
                        {
                            if (photoStream != null)
                            {
                                var skData = SKData.Create(photoStream);

                                var bitmap = SKBitmap.Decode(skData);

                                var oldChild = splashWrapper.Child as ImageElement;

                                UIImage splashImage = new UIImage(bitmap);

                                splashWrapper.Child = new ImageElement(splashImage);

                                Layout.Current.AddDirtyRect(Layout.Current.Bounds);

                                if (oldChild != null)
                                    oldChild.Image.Bitmap.Dispose();
                            }
                        }

                        UpdateDisplay();

                        currentPhotoIndex++;

                        if (currentPhotoIndex == currentPhotos.Results.Count)
                            currentPhotoIndex = 0;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public async Task<UnsplashSearchResponse> GetSplashPhotos()
        {
            var client = new UnsplashClient(Secrets.UnsplashAccessKey);

            return await client.SearchPhotos(PlayContext.CurrentSong.Title);
        }

        public async Task<Stream> GetSplashPhotoStream(Photo photo, int width, int height)
        {
            return await httpClient.GetStreamAsync(photo.Urls.Raw + $"&w={width}&h={height}&fit=crop");
        }

        private void PlayContext_SongChanged(object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        public void BlacklistCurrentPhoto()
        {
            if (currentPhotoIndex > 0)
            {
                try
                {
                    string userName = currentPhotos.Results[currentPhotoIndex - 1].User.Username;

                    if (!UnsplashClient.UserBlacklist.Contains(userName))
                    {
                        UnsplashClient.UserBlacklist.Add(userName);

                        PlayContext.SongFileProvider.WriteTextToFile(string.Join(',', UnsplashClient.UserBlacklist), "Blacklist.txt");
                    }

                    needPhotoSkip = true;
                }
                catch { }
            }
        }

        public void UpdateDisplay()
        {
            SongData currentSong = PlayContext.CurrentSong;

            if (currentSong == null)
            {
                songTitleText.Text = "";
                songArtistText.Text = "";
            }
            else
            {
                songTitleText.Text = currentSong.Title;
                songArtistText.Text = $"{currentSong.Artist} - {currentSong.Album}";

                if (currentSong.Year > 0)
                {
                    songArtistText.Text += $" ({currentSong.Year})";
                }
            }

            timeText.Text = DateTime.Now.ToString("h:mmtt").ToLower();

            UpdateContentLayout();
        }

        public override bool HandleTouch(in Touch touch)
        {
            if (touch.TouchState == ETouchState.Pressed)
            {
                PlayContext.NextSong();
            }

            return true;
        }
    }

    public class OutlinedText : TextBlock
    {
        public UIColor OutlineColor { get; set; } = UIColor.Black;
        public float OutlineSize { get; set; } = 2.0f;

        protected override void DrawContents()
        {
            for (float yOffset = -OutlineSize; yOffset <= OutlineSize; yOffset += OutlineSize)
            {
                for (float xOffset = -OutlineSize; xOffset <= OutlineSize; xOffset += OutlineSize)
                {
                    Layout.Current.GraphicsContext.DrawText(Text, TextFont, ContentBounds.X + xOffset, ContentBounds.Y + yOffset, OutlineColor);

                }
            }

            Layout.Current.GraphicsContext.DrawText(Text, TextFont, ContentBounds.X, ContentBounds.Y, TextColor);
        }

    }
}
