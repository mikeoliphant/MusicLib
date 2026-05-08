using System.IO;
using SkiaSharp;
using UILayout;
using MusicLib;

namespace MusicUI
{
    public class MainInterface : Dock
    {
        public SongPlayContext PlayContext { get; private set; }

        TextBlock songTitleText;
        TextBlock songArtistText;
        UIElementWrapper splashWrapper;

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
                using (Stream photoStream = await PlayContext.GetSplashPhotoStream((int)Layout.Current.Bounds.Width, (int)Layout.Current.Bounds.Height))
                {
                    if (photoStream != null)
                    {
                        try
                        {
                            var skData = SKData.Create(photoStream);

                            var bitmap = SKBitmap.Decode(skData);

                            var oldChild = splashWrapper.Child as ImageElement;

                            UIImage splashImage = new UIImage(bitmap);

                            splashWrapper.Child = new ImageElement(splashImage);

                            if (oldChild != null)
                                oldChild.Image.Bitmap.Dispose();
                        }
                        catch (Exception ex)
                        {

                        }

                        Layout.Current.AddDirtyRect(Layout.Current.Bounds);

                        UpdateDisplay();
                    }
                }

                await Task.Delay(30000);
            }
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
