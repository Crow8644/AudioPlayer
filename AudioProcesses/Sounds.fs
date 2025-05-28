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

// Triggers from the output device:

let initializeAudio(file, nextFinder: bool->string) = 
    // TODO: Add error handling here
    audioFile <- new AudioFileReader(file)
    outputDevice.Init(audioFile)

    outputDevice.Play()
    true                                    // Return true when the setup proccess was successful

let pause(button) =
    outputDevice.Stop()

let play(button) =
    outputDevice.Play()

let closeObjects(window) =
    outputDevice.Dispose()
    audioFile.Dispose()