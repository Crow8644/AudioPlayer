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
        public MainWindow()
        {
            InitializeComponent();
        }
        
        // Calls our FSharp code for the file dialog box every time the load file button is clicked
        private void load_button_clicked(object sender, RoutedEventArgs e)
        {
            string x = Files.getAudioFile(1);
            file_select.Content = x;
        }

        private void play_button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void pause_button_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}

// References:
// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-create-a-button-that-has-an-image
// https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.progressbar?view=windowsdesktop-9.0
// https://connelhooley.uk/blog/2017/04/30/f-sharp-to-c-sharp/
// https://www.christianfindlay.com/blog/how-to-use-fsharp-and-csharp