module Sounds

// Contains all of the resources for working with audio device drivers:
open NAudio.Wave
open NAudio.Wave.SampleProviders
open System.Threading

exception SignalError of string

// Examples and Documentation for NAudio:
// General page: https://github.com/naudio/NAudio?tab=readme-ov-file
// https://github.com/naudio/NAudio/blob/master/Docs/PlayAudioFileWinForms.md
// https://github.com/naudio/NAudio/blob/master/Docs/PlaybackStopped.md

// This entire module is dedicated to handling these two objects:
let outputDevice: WaveOutEvent = new WaveOutEvent()         // We just keep one device variable and set its details dynamically
let mutable audioFile: AudioFileReader = null               // This object will need to be recreated every time the user changes files

let mutable continuing: bool = true                         // Whether or not to automatically advance the file

// A mutable record of the current event handler used to find the next file, so it can be cleared when the folder is changed
let mutable advanceHandler: System.EventHandler<StoppedEventArgs> = null

// Threading objects:
let stopSignal: ManualResetEvent = new ManualResetEvent(false)          // This works as a mutex and allows us to synchronize events with the playback stopping
let switchLock: obj = new obj()                                         // .NET uses the moniter object with this to lock a code block, allowing only one thread

outputDevice.PlaybackStopped.Add(fun _ -> stopSignal.Set() |> ignore)   // Links the PlaybackStopped event to a signal that can be waited on

// Called when output device ends with a file path already calculated and also by rewind and foward buttons
let switchToFile(filePath: string, playAfter: bool): unit = 
    // Makes sure multiple threads cannot do this code block at once
    lock switchLock (fun _ ->
        if filePath = "" then           // This usually indicates the end of the directory or that an error occurred in previous functions
            ()                          // We do nothing here, letting the player sit in its current state
        else
            audioFile.Dispose()         // Clears audioFile's current resources
            audioFile <- new AudioFileReader(filePath)
            outputDevice.Init(audioFile)
            if playAfter then outputDevice.Play())

// Returns a value out of paramater range for how through being played the file is
let getFileProgress(resulution): int =
    match audioFile with
    | null ->               // Matches with null
        -1
    | _ ->                  // Matches with anything but null
        let portion: float = (float)audioFile.Position / (float)audioFile.Length    // Calculates current progress as a portion
        (int)(floor(portion * resulution))                                          // Multiplies to fit specified range and rounds down

    
let initializeAudio(file, nextFinder: bool->string): bool = 
    outputDevice.PlaybackStopped.RemoveHandler(advanceHandler)

    try
        audioFile <- new AudioFileReader(file)  // Needs to be recreated for every new track
        outputDevice.Init audioFile

        // Composes a function to be called by the PlaybackStopped event
        // This is has the cumulative effect of allowing access to file data from the not-yet-compiled Files module
        let stop(args: StoppedEventArgs) = if (audioFile.Length = audioFile.Position) then continuing |> nextFinder |> ignore else ()
        advanceHandler <- System.EventHandler<StoppedEventArgs>(fun (a: obj) -> stop)
        outputDevice.PlaybackStopped.AddHandler(advanceHandler)

        outputDevice.Play()
        true                                    // Return true when the setup proccess was successful
    with
        // Matches any exception and names it ex
        | ex -> false                           // Returns false for failure

// Pauses the audio device, using Stop so playbackStopped will be signalled
let pause() =
    outputDevice.Stop()

// Allows a calling function to synconously call pause and send a one time follow-up action to it being stopped
//  - nextaction: _->unit; must be a relatively thread safe function
// Throws a Signal Error if the ManuelResetEvent cannot be reset
let pauseAndDo(nextAction: _->unit) =
    // stopSignal may have been set several times before this point, so we need to reset it
    if stopSignal.Reset() then                  // Reset will return a true if successful and a false if not

        // IMPORTANT: Because the event handlers for PlaybackStopped are ran in the thread stop was called from,
        // The program freezes if we call stop and wait in the same thread
        // Moreover, for reasons that I don't understand, pausing the device must be done in the main thread
        let parellel() =
            stopSignal.WaitOne() |> ignore      // Wait for the playbackStopped signal
            nextAction()                        // Excecute the requested action
            
        let t: Thread = new Thread(parellel)    // Sets the thread with internal function
        t.Name <- "follow_action"
        t.Start()                               // Begins the thread

        outputDevice.Stop()                     // Pause the audio and signal playbackStopped, causing our other thread to run its actions
    else
        raise (SignalError("Signals for sound pausing encountered an error"))
        ()

let play(button) =
    outputDevice.Play()

// This clears up resources at the end of the program,
// Only to be called as part of the window-close handler
let closeObjects(window) =
    outputDevice.Dispose()
    // audioFile might not exist (in which case we have no need of disposing)
    // So we encapsulate it in a try block
    try
        audioFile.Dispose()
    with | ex -> ()         // An error here is not a problem, we just return unit

// Referenced Documentation:
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values