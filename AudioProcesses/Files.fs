// A module to handle all of the file system requirements for the audio player
// By: Caleb Ausema
// Created 5/24/2025

module Files            // Because F Sharp doesn't set namespaces for files be default, we have to be intentional about it

open System
open System.Windows
open System.IO
open System.Threading.Tasks
open System.Text.RegularExpressions
open System.Collections
open System.Linq
open System.Windows.Forms
open System.Windows.Controls

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

let regexSeperate(regex: string, default_ret: string, toParse: string) =
    let m = Regex(regex).Match toParse       // Regex matches three groups: the parent folder, the file name, and the file extention
    if m.Success
    then [for x in m.Groups -> x.Value]                             // Composes the original string into the groups from the regex
    else [default_ret]                                              // Default parent directory

// Partial application syntax really isn't working as expected at the moment
let seperateParentPath = fun path -> regexSeperate("^(.*\\\)([^\\\]*)(\.[^\\\]*)$", "c:\\", path)
    // The triple slash - \\\ - is due to an odd quirk I found with .net regexes
    // It seemingly processes escaping the regex special characters and escaping the string characters on seperate occations
    // This makes a \\ process as \ AND THEN cause a \) or \] to be processed as a literal ) or ]
    // The triple slash is necessary for the regex to match a single slash

let matchesExtention(path: string, extentions: array<string>): bool =
    let ext: string = Path.GetExtension(path)
    extentions |> Array.map(fun s -> (s = ext)) |> Array.reduce(||)

let isValidAudioFile = fun path -> matchesExtention(path, [|".wav"; ".mp3"; ".aiff"|])
    

let setImage(filename: string, control: Image) =
    match Metadata.getFilePhoto(filename) with
    | Some(bitmap) ->
        control.Source <- bitmap
    | None ->
        ()

// Starts the enumerator at a specified file
// Used to start at the current playing song when a file is selected from the middle of a directory
let moveNavTo(path: string) =
    directory_nav.pos <- Array.BinarySearch(directory_nav.array, path)
    (*
    while not (path = (string)file_enumerator.Current) && file_enumerator.MoveNext() do
        true |> ignore      // No need to do anything here
    *)

// Calls the file switch in Sounds for the next file in the directory
// continuing may be false if the user has chosen not to automatically advance file
let advanceFile(imageControl: Image, continuing: bool): string =
    directory_nav.pos <- directory_nav.pos + 1              // We increment the counter at this point, before checking if it fits in the array
    if 0 <= directory_nav.pos && directory_nav.pos < directory_nav.array.Length && continuing
    then 
        Sounds.stopAndDo(fun _ -> Sounds.switchToFile(directory_nav.current(), true))   // We do this because we have to wait until playback stops
        setImage(directory_nav.current(), imageControl) |> ignore
        let fileList = (directory_nav.current() |> seperateParentPath)
        if fileList.Length > 1
        then fileList.Item 2                                                            // Returns the current file name for UI reasons
        else directory_nav.current()
    else ""

// Switches to the alphabetically previous file in the directory
let rewindFile(imageControl: Image): string =
    if Sounds.getFileProgress 20 = 0 then           // Goes backwards if the file is in the first 20th of its runtime
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
// 
let getAudioFile(display: ContentControl, imageControl: Image) =
    let dialog = new OpenFileDialog()

    // Extra protection, resets the user's home directory if the previously used directory was deleted
    if not (Directory.Exists default_path)
    then if Directory.Exists userFolder then default_path <- userFolder else default_path <- "c:\\"

    // Set dialog properties
    dialog.InitialDirectory <- default_path                             // Starts at the highest level
    dialog.Filter <- "wav and mp3 files (*.wav;*.mp3)|*.wav;*.mp3"      // Only allows the selection of .wav and .mp3 files

    // Triggers the user selection and saves whether a file was selected or cancelled

    if dialog.ShowDialog() = System.Windows.Forms.DialogResult.OK
        then
        //Setting up file if selection was a success
        let fileList = seperateParentPath dialog.FileName
        default_path <- fileList.Item 1             // Sets the default path to be used on next open to the parent folder of this selection

        // TODO: manually combine other file types here
        directory_nav.array <- Directory.EnumerateFiles(fileList.Item 1, "*").Where(isValidAudioFile).ToArray()
        moveNavTo dialog.FileName

        // TODO: Save path data and send the file name to audio player

        Sounds.stopAndDo(fun _ -> ())                                   // Waits for any currently playing audio to stop
        if Sounds.initializeAudio(dialog.FileName, (fun b -> advanceFile(imageControl, b)), 
            fun name -> display.Content <- name)
        then 
            display.Content <- fileList.Item 2                          // Displays the name of the file
            setImage(directory_nav.current(), imageControl) |> ignore
            true
        else 
            MessageBox.Show("There was an error in playing the selected file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
            display.Content <- "Error"
            false
    else 
        display.Content <- "Unselected"                                 // Displays "Unselected" when the dialog box was closed
        false

//Forum that solved some headaches:
//https://stackoverflow.com/questions/9646684/cant-use-system-windows-forms

//Other useful sources:
//https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
//https://learnxbyexample.com/fsharp/regular-expressions/
//https://stackoverflow.com/questions/1143706/getting-the-path-of-the-home-directory-in-c