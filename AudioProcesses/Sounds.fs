module Sounds

// Contains all of the resources for working with audio device drivers:
open NAudio.Wave
open NAudio.Wave.SampleProviders

// Examples and Documentation for NAudio:
// General page: https://github.com/naudio/NAudio?tab=readme-ov-file
// https://github.com/naudio/NAudio/blob/master/Docs/PlayAudioFileWinForms.md
// https://github.com/naudio/NAudio/blob/master/Docs/PlaybackStopped.md

// This entire module is dedicated to handling these two objects:
let outputDevice: WaveOutEvent = new WaveOutEvent()         // We just keep one device variable and set its details dynamically
let mutable audioFile: AudioFileReader = null               // This object will need to be recreated every time the user changes files

let mutable continuing: bool = true                         // Whether or not to automatically advance the file

// A mutable record of the current event handler used to find the next file, so it can be cleared when the folder is changed
let mutable lastHandler: System.EventHandler<StoppedEventArgs> = null

// TODO: Create semaphore for threaded signalling

// Called when output device ends with a file path already calculated and also by rewind and foward buttons
let switchToFile(filePath: string): unit = 
    if filePath = "" then
        ()
    else
        audioFile.Dispose()
        audioFile <- new AudioFileReader(filePath)
        outputDevice.Init(audioFile)
        outputDevice.Play()

// Returns a value out of paramater range for how through being played the file is
let getFileProgress(resulution): int =
    match audioFile with
    | null ->               // Matches with null
        -1
    | _ ->                  // Matches with anything but null
        let portion: float = (float)audioFile.Position / (float)audioFile.Length    // Calculates current progress as a portion
        (int)(floor(portion * resulution))                                               // Multiplies to fit specified range and rounds down

    
let initializeAudio(file, nextFinder: bool->string) = 
    outputDevice.PlaybackStopped.RemoveHandler(lastHandler)

    // TODO: Add error handling here
    audioFile <- new AudioFileReader(file)
    outputDevice.Init audioFile

    // Composes a function to be called by the PlaybackStopped event
    // This is has the cumulative effect of allowing access to file data from the not-yet-compiled Files module
    let stop(args: StoppedEventArgs) = if (audioFile.Length = audioFile.Position) then continuing |> nextFinder |> ignore else ()
    lastHandler <- System.EventHandler<StoppedEventArgs>(fun (a: obj) -> stop)
    outputDevice.PlaybackStopped.AddHandler(lastHandler)

    outputDevice.Play()
    true                                    // Return true when the setup proccess was successful

let pause(button) =
    outputDevice.Stop()

// Allows a calling function to synconously call pause and send a one time follow-up action to it being stopped
let pauseAndDo(button, nextAction: _->unit) =
    // Wrapping the function in a handler lets us remove it
    let handler = System.EventHandler<StoppedEventArgs>(fun _ -> nextAction)

    // Add the function to run next to the handler and subsequently a function to remove the previous one
    // This leaves that second function to stay in the handler, building up every time pauseAndDo() is invoked
    // I tried some copy restore style things, but this was the main solution that worked
    // A better solution would be to create another event that can be waited for in a thread and permantly associate it with PlaybackStopped
    outputDevice.PlaybackStopped.AddHandler(handler)
    outputDevice.PlaybackStopped.Add(fun _ -> outputDevice.PlaybackStopped.RemoveHandler(handler))

    outputDevice.Stop()

let play(button) =
    outputDevice.Play()

let closeObjects(window) =
    outputDevice.Dispose()
    audioFile.Dispose()

// Referenced Documentation:
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values