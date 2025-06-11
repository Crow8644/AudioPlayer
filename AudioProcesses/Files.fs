// A module to handle all of the file system requirements for the audio player
// By: Caleb Ausema
// Created 5/24/2025

module Files            // Because F Sharp doesn't set namespaces for files be default, we have to be intentional about it

open System
open System.IO
open System.Linq
open System.Windows.Forms
open System.Windows.Controls
open System.Windows.Media.Imaging
open System.Reflection

// directory_nav module is used as a two way enumerator to track the files around that originally selected
// It has three members:
//  - array; a sorted array of paths in this directory
//  - pos; the position in that array currently being tracked
//  - current; a function that just calls array[pos] and returns an empty string if there was an error
module directory_nav = 
    let mutable array: array<string> = [||]
    let mutable pos: int = 0 
    let current = fun _ ->
        try
            array[pos]
        with
            // The marker :? matches any exception of the following type
            | :? System.IndexOutOfRangeException -> ""  // Give a blank string

let userFolder: string = Environment.SpecialFolder.UserProfile |> Environment.GetFolderPath

// default_path holds the currently active directory to restore to
// We start the proccess trying to use this user profile's directory
let mutable default_path: string = if Directory.Exists userFolder then userFolder else "c:\\"

// -- Partially applied utility functions -- //

// The triple slash - \\\ - is due to an odd quirk I found with .net regexes
// It seemingly processes escaping the regex special characters and escaping the string characters on seperate occations
// This makes a \\ process as \ AND THEN cause a \) or \] to be processed as a literal ) or ]
// The triple slash is necessary for the regex to match a single slash
let seperateParentPath = fun path -> Utilities.regexSeperate("^(.*\\\)([^\\\]*)(\.[^\\\]*)$", default_path, path)

// wav, mp3, aiff, and wma are the four currently supported extentions
let isValidAudioFile = fun path -> Utilities.matchesExtention(path, [|".wav"; ".mp3"; ".aiff"; "wma"|])

let setImage(filename: string, control: System.Windows.Controls.Image) =
    match Metadata.getFilePhoto(filename) with
    | Some(bitmap) ->
        control.Source <- bitmap
    | None ->
        let coverStream: Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DefaultCoverAdjusted.png")
        
        let defaultFrame = BitmapFrame.Create(coverStream, BitmapCreateOptions.None,BitmapCacheOption.OnLoad)

        control.Source <- defaultFrame
    // Image Resource Citations:
    // https://www.codeproject.com/Articles/13573/Extracting-Embedded-Images-From-An-Assembly
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-use-resources-in-localizable-applications

// Starts the enumerator at a specified file
// Used to start at the current playing song when a file is selected from the middle of a directory
let moveNavTo(path: string) =
    directory_nav.pos <- Array.BinarySearch(directory_nav.array, path)

// Calls the file switch in Sounds for the next file in the directory
// continuing may be false if the user has chosen not to automatically advance file
let advanceFile(imageControl: Image, continuing: bool): string =
    directory_nav.pos <- directory_nav.pos + 1                                          // We increment the counter at this point, before checking if it fits in the array
    if directory_nav.pos < directory_nav.array.Length && continuing
    then 
        Sounds.stopAndDo(fun _ -> Sounds.switchToFile(directory_nav.current(), true))   // We do this because we have to wait until playback stops
        setImage(directory_nav.current(), imageControl) |> ignore
        let fileList = (directory_nav.current() |> seperateParentPath)
        if fileList.Length > 1
        then fileList.Item 2                                                            // Returns the current file name for UI reasons
        else directory_nav.current()
    else ""

// Seperated from below function to allow command line parsing
let setupAudioFile(file: string, display: ContentControl, imageControl: Image) = 
    if File.Exists file
    then
        let fileList = seperateParentPath file
        // Sets the default path to be used on next open to the parent folder of this selection
        default_path <- if fileList.Length > 1 then fileList.Item 1 else fileList.Item 0

        if fileList.Length > 0
        then
            directory_nav.array <- Directory.EnumerateFiles(fileList.Item 1, "*").Where(isValidAudioFile).ToArray()
            moveNavTo file                         // Starts our enumerator at the file that has been selected
        else
            directory_nav.array <- [||]                 // Reset to a blank array

        if Sounds.initializeAudio(file, (fun b -> advanceFile(imageControl, b)), 
            fun name -> display.Content <- name)
        then 
            display.Content <- if fileList.Length > 1 then fileList.Item 2 else file                // Displays the name of the file
            setImage(directory_nav.current(), imageControl)
            true
        else 
            MessageBox.Show("There was an error in playing the selected file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
            display.Content <- "Error"                                                              // Displays an error and returns false 
            false
    else
        MessageBox.Show("The selected file does not exist", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
        display.Content <- "Error"                                                                  // Displays an error on the main window
        false

// Switches to the alphabetically previous file in the directory
let rewindFile(imageControl: Image): string =
    if Sounds.getFileProgress 20 = 0 && directory_nav.pos > 0 then           // Goes backwards if the file is in the first 20th of its runtime
        directory_nav.pos <- directory_nav.pos - 1

    Sounds.stopAndDo(fun _ -> Sounds.switchToFile(directory_nav.current(), true))       // Is sure to pause before requesting the track to switch
    setImage(directory_nav.current(), imageControl) |> ignore
    let fileList = (directory_nav.current() |> seperateParentPath)
    if fileList.Length > 1
    then fileList.Item 2                                                                // Returns the current file name for UI reasons
    else directory_nav.current()

// File Dialog Documentation, which I am referencing heavily: 
// https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.filedialog?view=windowsdesktop-9.0

// This function runs the file selection dialog and proccesses the result, ultimately initiating playback
// Must run in the UI thread
// Returns true if audio is playing after excecution, false if not
let getAudioFile(display: ContentControl, imageControl: Image) =
    let dialog = new OpenFileDialog()

    // Extra protection, resets the user's home directory if the previously used directory was deleted
    if not (Directory.Exists default_path)
    then if Directory.Exists userFolder then default_path <- userFolder else default_path <- "c:\\"

    // Set dialog properties
    dialog.InitialDirectory <- default_path                                                     // Starts at the saved default
    dialog.Filter <- "supported audio files (*.wav;*.mp3;.aiff;.wma)|*.wav;*.mp3;*.aiff;*.wma"  // Only allows the selection of supported files

    // Triggers the user selection and saves whether a file was selected or cancelled

    if dialog.ShowDialog() = System.Windows.Forms.DialogResult.OK
        then
        //If selection was a success
        setupAudioFile(dialog.FileName, display, imageControl)
    else 
        // Leaves display the same when the dialog box was closed
        Sounds.play()                                                                           // Tries to restart the audio, if it exists
                                                                                                // We want to return the success of that, so the caller knows if audio is now playing

//Forum that solved some headaches:
//https://stackoverflow.com/questions/9646684/cant-use-system-windows-forms

//Other useful sources:
//https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
//https://learnxbyexample.com/fsharp/regular-expressions/
//https://stackoverflow.com/questions/1143706/getting-the-path-of-the-home-directory-in-c