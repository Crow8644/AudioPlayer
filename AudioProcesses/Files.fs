// A module to handle all of the file system requirements for the audio player
// By: Caleb Ausema
// Created 5/24/2025

module Files
// Because F Sharp doesn't set namespaces for files be default, we have to be intentional about it

open System
open System.Windows
open System.IO
open System.Threading.Tasks
open System.Text.RegularExpressions

let mutable default_path: string = "c:\\"

let seperateParentPath(path) =
    // The triple slash - \\\ - is due to an odd quirk I found with .net regexes
    // It seemingly processes escaping the regex special characters and escaping the string characters on seperate occations
    // This makes a \\ process as \ AND THEN cause a \) or \] to be processes as a literal ) or ]
    // The triple slash works as a solution here
    let m = Regex("^(.*[\\\])([^\\\]*)$").Match(path)   // Regex matches two groups: the parent folder and the file name
    if m.Success
    then [for x in m.Groups -> x.Value]                 // Composes the original string and two groups from the regex into a list
    else ["c:\\"]                                       // Default parent directory


// File Dialog Documentation, which I am referencing heavily: 
// https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.filedialog?view=windowsdesktop-9.0

// This function will only run multiple times if we take something as input
let getAudioFile(test) =
    let dialog = new System.Windows.Forms.OpenFileDialog()

    // Provided by OpenFile documentation
    dialog.InitialDirectory <- default_path             // Starts at the highest level
    dialog.Filter <- "wav files (*.wav)|*.wav"          // Only allows the selection of .wav files

    // Triggers the user selection and saves whether a file was selected or cancelled

    if dialog.ShowDialog() = System.Windows.Forms.DialogResult.OK  
        then
        //Setting up file if selection was a success
        default_path <- seperateParentPath(dialog.FileName).Head        // Sets the default path to be used on next open to the parent folder of this selection
        // TODO: Save path data and send the file name to audio player
        "Success"           // Returns "Success"
        else "Failure"      // Otherwise, returns "Failure

//Forum that solved some headaches:
//https://stackoverflow.com/questions/9646684/cant-use-system-windows-forms
//Other useful sources:
//https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
//https://learnxbyexample.com/fsharp/regular-expressions/