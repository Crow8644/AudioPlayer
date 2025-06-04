module Sounds

// Contains all of the resources for working with audio device drivers:
open NAudio.Wave
open NAudio.Wave.SampleProviders
open System.Threading
open System.Windows.Forms

// NOTE: NAudio documentation says that init should not called more than once on a single object
// I implimented my system here before seeing that, and did not run into any problems
// If I do later I will correct this.

exception SignalError of string

// Examples and Documentation for NAudio:
// General page: https://github.com/naudio/NAudio?tab=readme-ov-file
// https://github.com/naudio/NAudio/blob/master/Docs/PlayAudioFileWinForms.md
// https://github.com/naudio/NAudio/blob/master/Docs/PlaybackStopped.md

// This entire module is dedicated to handling these two objects:
let outputDevice: WaveOutEvent = new WaveOutEvent()         // We just keep one device variable and set its details dynamically
let mutable audioFile: WaveStream = null                    // This object will need to be recreated every time the user changes files

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
        if filePath = "" then                   // This usually indicates the end of the directory or that an error occurred in previous functions
            ()                                  // We do nothing here, letting the player sit in its current state
        else
            try
                audioFile.Dispose()             // Clears audioFile's current resources
                audioFile <- new WaveFileReader(filePath)
                outputDevice.Init audioFile     // Despite protection through pauseAndDo, this still occationally generates errors
                if playAfter then outputDevice.Play()
            with 
                | ex -> 
                MessageBox.Show("There was an error in playing a new file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
        ) // End fun

let changeFilePostitionTo(postion: float) =
    lock switchLock (fun _ ->
        match audioFile with
            | null ->
                ()
            | _ ->
                // The setter for Position has built-in protection
                audioFile.Position <-
                // This calculation finds the exact byte position that needs to be set, understanding that the result must be an integer multiple of BlockAlign
                audioFile.Length / (int64)audioFile.BlockAlign |> float 
                |> (*) postion |> int64 
                |> (*) (int64 audioFile.BlockAlign)
    ) // End locked function

// Returns a value out of paramater range for how through being played the file is
let getFileProgress(resulution: int): int =
    if Monitor.TryEnter switchLock              // This function is called by the GUI thread, so we want no blocking here, just return 0 if unable to attain lock
    then
        try
            match audioFile with
            | null ->
                0
            | _ ->
                let portion: float = float audioFile.Position / float audioFile.Length      // Calculates current progress as a portion
                int (floor(portion * float resulution))                                   // Multiplies to fit specified range and rounds down
        finally
            Monitor.Exit switchLock
    else
        0

let isFileOver(): bool = 
    if Monitor.TryEnter switchLock
    then
        try
            audioFile.Length = audioFile.Position
        finally
            Monitor.Exit switchLock
    else 
        false

let adjustVol(volume) = outputDevice.Volume <- volume

// Sets up all audio objects
let initializeAudio(file: string, nextFinder: bool->string): bool = 
    lock switchLock (fun _ ->
        outputDevice.PlaybackStopped.RemoveHandler advanceHandler
        try
            audioFile <- new WaveFileReader(file)  // Needs to be recreated for every new track
            outputDevice.Init audioFile

            // Composes a function to be called by the PlaybackStopped event
            // This is has the cumulative effect of allowing access to file data from the not-yet-compiled Files module
            let stop(args: StoppedEventArgs) = if isFileOver() then continuing |> nextFinder |> ignore else ()
            advanceHandler <- System.EventHandler<StoppedEventArgs>(fun (a: obj) -> stop)
            outputDevice.PlaybackStopped.AddHandler advanceHandler

            outputDevice.Play()
            true                                    // Return true when the setup proccess was successful
        with
            // Matches any exception and names it ex
            | ex -> false                           // Returns false for failure
    )


// -- Public Use Control Functions -- //
let play(button) =
    outputDevice.Play()

let pause() =
    outputDevice.Pause()

// Ends the audio device playback, using Stop so playbackStopped will be signalled
let stop() =
    outputDevice.Stop()         // A very sudden stop to the audio
    audioFile.Position <- 0     // Resets the file when audio is stopped


// -- More Internal Control Functions -- //

// Allows a calling function to synconously call pause and send a one time follow-up action to it being stopped
//  - nextaction: _->unit; must be a relatively thread safe function
// Throws a Signal Error if the ManuelResetEvent cannot be reset
let stopAndDo(nextAction: _->unit) =
    // Tests if the device is already stopped, so the passed action still happens
    if outputDevice.PlaybackState = PlaybackState.Stopped
    then 
        nextAction()
    // stopSignal may have been set several times before this point, so we need to reset it
    elif stopSignal.Reset() then                  // Reset will return a true if successful and a false if not

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

// -- Cleanup Functions -- //

// This clears up resources at the end of the program,
// Only to be called as part of the window-close handler
let closeObjects(window) =
    stopAndDo(fun _ ->          // Ensure the stream is already stopped before disposing of resources
        outputDevice.Dispose()
        // audioFile might not exist (in which case we have no need of disposing)
        // So we encapsulate it in a try block
        try
            match audioFile with
            | null ->
                ()
            | _ ->              // Anything else
                audioFile.Dispose()
        with | ex -> ()         // An error here is not a problem, we just return unit
    )

// Referenced Documentation:
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values
// https://markheath.net/post/naudio-wavestream-in-depth