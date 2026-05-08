using MusicLib;
using MusicUI;
using System.Windows;
using UILayout;
using UILayout.Skia.WPF;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LayoutWindow layoutWindow = new LayoutWindow()
            {
                Width = 1024,
                Height = 800
            };

            SMBFileProvider provider = new SMBFileProvider(@"smb://USBDISK/Share/Music", "WORKGROUP", Secrets.SMBUser, Secrets.SMBPassword);

            WindowsSongPlayContext playContext = new(provider);

            SkiaLayout ui = new SkiaLayout();

            ui.RootUIElement = new MainInterface(playContext);

            layoutWindow.SkiaCanvas.SetLayout(ui);

            layoutWindow.Show();

            playContext.PlayCurrentSong();
        }
    }
}
