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

        TextBlock songTitleText;
        TextBlock songArtistText;
        UIElementWrapper splashWrapper;
        SongData lastSongData = null;
        UnsplashSearchResponse currentPhotos;
        int currentPhotoIndex = 0;
        HttpClient httpClient = new HttpClient();
        Random random = new();

        public MainInterface(SongPlayContext playContext)
        {
            Layout.Current.DefaultFont = new UIFont { Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), TextSize = 48 };

            PlayContext = playContext;

            PlayContext.SongChanged += PlayContext_SongChanged;

            BackgroundColor = UIColor.Black;

            splashWrapper = new UIElementWrapper { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Stretch };
            Children.Add(splashWrapper);

            VerticalStack textStack = new VerticalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Left,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Margin = 10,
                ChildSpacing = 10
            };

            Children.Add(textStack);

            songTitleText = new TextBlock();
            songArtistText = new TextBlock();

            textStack.Children.Add(songTitleText);
            textStack.Children.Add(songArtistText);
            PlayContext = playContext;

            UpdateDisplay();

            Task.Run(CycleImages);
        }

        async Task CycleImages()
        {
            while (true)
            {
                for (int i = 0; i < 30; i++)
                {
                    if (PlayContext.CurrentSong != lastSongData)
                    {
                        lastSongData = PlayContext.CurrentSong;

                        currentPhotos = await GetSplashPhotos();

                        if (currentPhotos != null)
                        {
                            Random.Shared.Shuffle(currentPhotos.Results);
                        }

                        currentPhotoIndex = 0;

                        break;
                    }

                    await Task.Delay(1000);
                }

                if ((currentPhotos != null) && (currentPhotos.Results.Length > 0))
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

                                UpdateDisplay();
                            }
                        }

                        currentPhotoIndex++;

                        if (currentPhotoIndex == currentPhotos.Results.Length)
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

        public void UpdateDisplay()
        {
            SongData currentSong = PlayContext.CurrentSong;

            songTitleText.Text = currentSong.Title;
            songArtistText.Text = currentSong.Artist;

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
}
