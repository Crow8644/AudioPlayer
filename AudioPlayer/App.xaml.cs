using System.Configuration;
using System.Data;
using System.Windows;

namespace AudioPlayer
{
    /// <summary>
    /// This class handles the passing of cmd-line args and consequential startup properties
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();
            if (e.Args.Length > 0)
            {
                window.Show();
                // Gets the controls from the window and passes that to Files
                // Using this function skips the dialog openning and stop-wait proccess but completes the file setup
                Files.setupAudioFile(e.Args[0], window.getTrackTitleDisplay(), window.getArtistDispaly(), window.getTimeDisplay(), window.getImageControl());
            }
            else
            {
                window.lock_controls();
                Files.setImage("", window.getImageControl());
                window.Show();
            }
        }
    }
}

// Referenced:
// https://wpf-tutorial.com/wpf-application/working-with-app-xaml/