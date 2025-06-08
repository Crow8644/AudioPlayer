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
using System.Threading.Tasks;
using System.Threading;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool paused = false;
        private double lastVolState;
        
        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            KeyDown += MainWindow_KeyDown;
            CompositionTarget.Rendering += update_progress_slider;
            lastVolState = 100;
            mute_button.IsChecked = false;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Connects arrow keys to skip buttons
            if (e.Key == Key.Left)
            {
                skip_back_Click(new object(), new RoutedEventArgs());
            }
            else if (e.Key == Key.Right)
            {
                skip_forward_Click(new object(), new RoutedEventArgs());
            }
            else if (e.Key == Key.Space)
            {
                play_pause_button_Click(new object(), new RoutedEventArgs());
            }
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
            // Thank you so much to https://stackoverflow.com/a/4331287/503969 for solving a huge bug
            // We need to stop the playback in a different SynchronizationContext before returning and loading using this SynchronizationContext
            void internalLoad(Task oldTask)
            {
                bool x = Files.getAudioFile(track_title_display, cover);
                paused = !x;
                play_pause_button.Content = (paused) ? "Play" : "Pause";
            }
            Task t = new Task(Sounds.stopAndWait);
            Task UITask = t.ContinueWith(internalLoad, TaskScheduler.FromCurrentSynchronizationContext());

            t.Start();
        }

        private void play_pause_button_Click(object sender, RoutedEventArgs e)
        {
            // Toggle logic for the button that plays and pauses audio
            if (paused)
            {
                if (Sounds.play(sender)) {
                    play_pause_button.Content = "Pause";
                    paused = false;
                }
            }
            else
            {
                if (Sounds.pause())
                {
                    play_pause_button.Content = "Play";
                    paused = true;
                }
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
            track_title_display.Content = Files.rewindFile(cover);
        }

        private void foward_button_Click(object sender, RoutedEventArgs e)
        {
            track_title_display.Content = Files.advanceFile(cover, true);
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
            // Scales the volume slider by a power of two, which sounds more natural
            Sounds.adjustVol((float)Math.Pow(volume_slider.Value / 100.0, 2));
            mute_button.IsChecked = false;
        }

        private void skip_back_Click(object sender, RoutedEventArgs e)
        {
            Sounds.skip(-5);
        }

        private void skip_forward_Click(object sender, RoutedEventArgs e)
        {
            Sounds.skip(5);
        }

        private void mute_button_Checked(object sender, RoutedEventArgs e)
        {
            lastVolState = volume_slider.Value;
            volume_slider.Value = 0;
            Sounds.adjustVol(0.0f);
        }

        private void mute_button_Unchecked(object sender, RoutedEventArgs e)
        {
            volume_slider.Value = lastVolState;
            Sounds.adjustVol((float)Math.Pow(lastVolState / 100.0, 2));
        }

        /*
        private void auto_advance_button_Checked(object sender, RoutedEventArgs e)
        {
            Sounds.continuing = true;
        }

        private void auto_advance_button_Unchecked(object sender, RoutedEventArgs e)
        {
            Sounds.continuing = false;
        }
        */

        public ContentControl getTrackTitleDisplay()
        {
            return track_title_display;
        }
        public Image getImageControl()
        {
            return cover;
        }
    }
}

// References:
// https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.progressbar?view=windowsdesktop-9.0
// https://connelhooley.uk/blog/2017/04/30/f-sharp-to-c-sharp/
// https://www.christianfindlay.com/blog/how-to-use-fsharp-and-csharp
// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-render-on-a-per-frame-interval-using-compositiontarget