using Android.Runtime;
using Android.Views;
using MusicLib;
using MusicUI;
using SkiaSharp.Views.Android;
using UILayout;

namespace AndroidPlayer
{
    [Activity(Label = "@string/app_name",
        Icon = "@mipmap/ic_launcher",
        Banner = "@drawable/banner",
        MainLauncher = true
        )]
    [IntentFilter(
        actions: new string[] { "android.intent.action.MAIN" },
        Categories = new string[] { "android.intent.category.LEANBACK_LAUNCHER" })]
    public class MainActivity : Activity
    {
        LayoutView layoutView;
        SongPlayContext playContext;

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent? e)
        {
            if (e.KeyCode == Keycode.DpadRight)
            {
                playContext.NextSong();

                return true;
            }

            return false;
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            this.ActionBar?.Hide();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            SkiaLayout.DefaultTextureNamespace = "AndroidPlayer";

            layoutView = new(this);

            SMBFileProvider provider = new SMBFileProvider(@"smb://USBDISK/Share/MusicNew", "WORKGROUP", Secrets.SMBUser, Secrets.SMBPassword);

            playContext = new AndroidSongPlayContext(provider);

            var canvasView = new SKCanvasView(this);
            SetContentView(layoutView.CanvasView);

            layoutView.RootUIElement = new MainInterface(playContext);
        }
    }
}