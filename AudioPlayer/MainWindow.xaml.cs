using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Our F# Files:
using AudioProcesses;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool paused = false;
        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            CompositionTarget.Rendering += update_progress_slider;
        }

        private void track_change(object? sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Sounds.changeFilePostitionTo(track_progress.Value / 10.0f);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Sounds.closeObjects(this);
        }

        // Calls our FSharp code for the file dialog box every time the load file button is clicked
        private void load_button_clicked(object sender, RoutedEventArgs e)
        {
            string x = Files.getAudioFile();
            file_select.Content = x;
        }

        private void play_pause_button_Click(object sender, RoutedEventArgs e)
        {
            // Toggle logic for the button that plays and pauses audio
            if (paused)
            {
                Sounds.play(sender);
                play_pause_button.Content = "Pause";
                paused = false;
            }
            else
            {
                Sounds.pause();
                play_pause_button.Content = "Play";
                paused = true;
            }
        }

        private void stop_button_Click(object sender, RoutedEventArgs e)
        {
            Sounds.stop();
            play_pause_button.Content = "Play";
            paused = true;
        }

        private void rewind_button_Click(object sender, RoutedEventArgs e)
        {
            Files.rewindFile();
        }

        private void foward_button_Click(object sender, RoutedEventArgs e)
        {
            Files.advanceFile(true);
        }

        private void update_progress_slider(object? sender, EventArgs e)
        {
            float progress = Sounds.getFileProgress((int)track_progress.Width);
            if (progress >= 0 && !track_progress.IsMouseCaptureWithin) {
                track_progress.Value = progress / ((float)track_progress.Width / 10.0f); // We divide by ten because the slider is out of ten
            }
        }

        private void volume_slider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Sounds.adjustVol((float)volume_slider.Value / 100.0f);
        }
    }
}

// References:
// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-create-a-button-that-has-an-image
// https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.progressbar?view=windowsdesktop-9.0
// https://connelhooley.uk/blog/2017/04/30/f-sharp-to-c-sharp/
// https://www.christianfindlay.com/blog/how-to-use-fsharp-and-csharp
// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-render-on-a-per-frame-interval-using-compositiontarget