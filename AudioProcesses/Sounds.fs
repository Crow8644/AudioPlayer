module Sounds

// Contains all of the resources for working with audio device drivers:
open NAudio.Wave
open NAudio.Wave.SampleProviders
open System.Threading

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

// Threading objects:
let stopSignal: ManualResetEvent = new ManualResetEvent(false)          // This works as a mutex and allows us to synchronize events with the playback stopping
let switchLock: obj = new obj()                                         // .NET uses the moniter object with this to lock a code block, allowing only one thread

outputDevice.PlaybackStopped.Add(fun _ -> stopSignal.Set() |> ignore)

// Called when output device ends with a file path already calculated and also by rewind and foward buttons
let switchToFile(filePath: string): unit = 
    // Makes sure multiple threads cannot do this code block at once
    lock switchLock (fun _ ->
        if filePath = "" then           // This usually indicates the end of the directory or that an error occurred in previous functions
            ()                          // We do nothing here, letting the player sit in its current state
        else
            audioFile.Dispose()         // Clears audioFile's current resources
            audioFile <- new AudioFileReader(filePath)
            outputDevice.Init(audioFile)
            outputDevice.Play())

// Returns a value out of paramater range for how through being played the file is
let getFileProgress(resulution): int =
    match audioFile with
    | null ->               // Matches with null
        -1
    | _ ->                  // Matches with anything but null
        let portion: float = (float)audioFile.Position / (float)audioFile.Length    // Calculates current progress as a portion
        (int)(floor(portion * resulution))                                          // Multiplies to fit specified range and rounds down

    
let initializeAudio(file, nextFinder: bool->string): bool = 
    outputDevice.PlaybackStopped.RemoveHandler(lastHandler)

    try
        audioFile <- new AudioFileReader(file)
        outputDevice.Init audioFile

        // Composes a function to be called by the PlaybackStopped event
        // This is has the cumulative effect of allowing access to file data from the not-yet-compiled Files module
        let stop(args: StoppedEventArgs) = if (audioFile.Length = audioFile.Position) then continuing |> nextFinder |> ignore else ()
        lastHandler <- System.EventHandler<StoppedEventArgs>(fun (a: obj) -> stop)
        outputDevice.PlaybackStopped.AddHandler(lastHandler)

        outputDevice.Play()
        true                                    // Return true when the setup proccess was successful
    with
        // Matches any exception and names it ex
        | ex -> false                           // Returns false for failure

let pause() =
    outputDevice.Stop()

// Allows a calling function to synconously call pause and send a one time follow-up action to it being stopped
//  - nextaction: _->unit; must be a relatively thread safe function
let pauseAndDo(nextAction: _->unit) =
    // stopSignal may have been set several times before this point, so we need to reset it
    if stopSignal.Reset() then              // Reset will return a true if successful and a false if not
        // IMPORTANT: Because the event handlers for PlaybackStopped are ran in the thread stop was called from,
        // The program freezes if we call stop and wait in the same thread
        // Moreover, for reasons that I don't understand, the pausing part must be done in the main thread
        let intern() =
            stopSignal.WaitOne() |> ignore      // Wait for the playbackStopped signal
            nextAction()                        // Excecute the requested action
            
        let t: Thread = new Thread(intern)
        t.Name <- "follow_action"
        t.Start()

        pause()                                 // Pause the audio, causing our other thread to run its actions
    else
        //TODO: Throw some kind of error here
        ()

let play(button) =
    outputDevice.Play()

let closeObjects(window) =
    outputDevice.Dispose()
    audioFile.Dispose()

// Referenced Documentation:
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values