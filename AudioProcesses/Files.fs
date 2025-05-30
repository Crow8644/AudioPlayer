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

// Type file_tracker is used like a two way enumerator to track the files around that originally selected
module directory_nav = 
    let mutable array: array<string> = [||] 
    let mutable pos: int = 0 
    let current = fun _ ->
        // TODO: Add error handling
        array[pos]

let mutable default_path: string = "c:\\"                                                      // Currently active directory to restore to

// Citation for empty: https://stackoverflow.com/questions/1714351/return-an-empty-ienumerator

let seperateParentPath(path: string) =
    // The triple slash - \\\ - is due to an odd quirk I found with .net regexes
    // It seemingly processes escaping the regex special characters and escaping the string characters on seperate occations
    // This makes a \\ process as \ AND THEN cause a \) or \] to be processed as a literal ) or ]
    // The triple slash is necessary for the regex to match a single slash
    let m = Regex("^(.*\\\)([^\\\]*)(\.[^\\\]*)$").Match(path)      // Regex matches three groups: the parent folder, the file name, and the file extention
    if m.Success
    then [for x in m.Groups -> x.Value]                             // Composes the original string and two groups from the regex into a list
    else ["c:\\"]                                                   // Default parent directory

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
let advanceFile(continuing: bool): string =
    directory_nav.pos <- directory_nav.pos + 1              // We increment the counter at this point, before checking if it fits in the array
    if 0 <= directory_nav.pos && directory_nav.pos < directory_nav.array.Length && continuing
    then 
        Sounds.pauseAndDo(fun _ -> ())
        Sounds.switchToFile(directory_nav.current())
        directory_nav.current()                             // Returns the current directory as well
    else ""

let rewindFile(): string =
    if Sounds.getFileProgress(20) = 0 then
        directory_nav.pos <- directory_nav.pos - 1

    Sounds.pauseAndDo(fun _ -> Sounds.switchToFile(directory_nav.current())) // Is sure to pause before requesting the track to switch

    directory_nav.current()         // Returns the current directory

// File Dialog Documentation, which I am referencing heavily: 
// https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.filedialog?view=windowsdesktop-9.0

// This function will only run multiple times if we take something as input
let getAudioFile(button) =
    let dialog = new System.Windows.Forms.OpenFileDialog()

    // Provided by OpenFile documentation
    dialog.InitialDirectory <- default_path                             // Starts at the highest level
    dialog.Filter <- "wav and mp3 files (*.wav;*.mp3)|*.wav;*.mp3"      // Only allows the selection of .wav files

    // Triggers the user selection and saves whether a file was selected or cancelled

    if dialog.ShowDialog() = System.Windows.Forms.DialogResult.OK
        then
        //Setting up file if selection was a success
        let fileList = seperateParentPath(dialog.FileName)
        default_path <- fileList.Item(1)        // Sets the default path to be used on next open to the parent folder of this selection

        // Gets and enumerator using only approved file types
        //file_enumerator <- Directory.EnumerateFiles(fileList.Item(1), "*.wav;*.mp3").GetEnumerator()

        // TODO: manually combine other file types here
        directory_nav.array <- Directory.EnumerateFiles(fileList.Item(1), "*.wav").ToArray()
        moveNavTo(dialog.FileName)

        // TODO: Save path data and send the file name to audio player
        Sounds.initializeAudio(dialog.FileName, advanceFile) |> ignore

        fileList.Item(2)                        // Returns the name of the file
        else "Unselected"                       // Otherwise, returns "Unselected"

//Forum that solved some headaches:
//https://stackoverflow.com/questions/9646684/cant-use-system-windows-forms

//Other useful sources:
//https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
//https://learnxbyexample.com/fsharp/regular-expressions/