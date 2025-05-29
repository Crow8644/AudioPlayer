module Sounds

// Contains all of the resources for working with audio device drivers:
open NAudio.Wave
open NAudio.Wave.SampleProviders

// Examples and Documentation for NAudio:
// General page: https://github.com/naudio/NAudio?tab=readme-ov-file
// https://github.com/naudio/NAudio/blob/master/Docs/PlayAudioFileWinForms.md
// https://github.com/naudio/NAudio/blob/master/Docs/PlaybackStopped.md

let outputDevice: WaveOutEvent = new WaveOutEvent()         // We just keep one device variable and set its details dynamically
let mutable audioFile: AudioFileReader = null               // This object will need to be recreated every time the user changes files

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
let getFileProgress(range): int =
    match audioFile with
    | null ->               // Matches with null
        -1
    | _ ->                  // Matches with anything but null
        let portion: float = (float)audioFile.Position / (float)audioFile.Length    // Calculates current progress as a portion
        (int)(floor(portion * range))                                               // Multiplies to fit specified range and rounds down

    
let initializeAudio(file, nextFinder: bool->string) = 
    // TODO: Add error handling here
    audioFile <- new AudioFileReader(file)
    outputDevice.Init(audioFile)

    // Composes a function to be called by the PlaybackStopped event
    // This is has the cumulative effect of allowing access to file data from the not-yet-compiled Files module
    outputDevice.PlaybackStopped.Add(fun _ -> (audioFile.Length = audioFile.Position) |> nextFinder |> switchToFile)
    
    outputDevice.Play()
    true                                    // Return true when the setup proccess was successful

let pause(button) =
    outputDevice.Stop()

let play(button) =
    outputDevice.Play()

let closeObjects(window) =
    outputDevice.Dispose()
    audioFile.Dispose()

// Referenced Documentation:
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values