// This module holds functions to access and work with the metadata for the audio file
// Uses ATL: https://github.com/Zeugma440/atldotnet
// By: Caleb Ausema
// Created 6/05/2025

module Metadata
open ATL                                // 3rd-party library for accessing metadata
open System.Windows.Media.Imaging
open System.IO

// Returns an object that can be used as the source for the displayed image in xaml
// ATL gives us a stream, which we must convert into a bitmap image
// Citation: https://stackoverflow.com/questions/16857325/io-stream-to-image-in-wpf
let getFilePhoto(filePath: string): Option<BitmapFrame> =
    let track: Track = new Track(filePath)
    let embeddedImages = track.EmbeddedPictures
    if embeddedImages.Count > 0                                                                 // Tests if there even is a cover image for this file
    then
        use stream = new MemoryStream(embeddedImages.Item(0).PictureData)
        Some (BitmapFrame.Create(stream, BitmapCreateOptions.None,BitmapCacheOption.OnLoad))    // Might pass data back in a form that can be used as an image source in wpf
    else
        None
    
let getArtist(filePath: string): Option<string> =
    let track: Track = new Track(filePath)
    let artist: string = track.Artist
    if not (artist = "")                // Tests if there really was an artist embedded in the file
    then Some artist
    else None

let getDuration(filePath: string): Option<System.TimeSpan> =
    let track: Track = new Track(filePath)
    let seconds: int = track.Duration
    if not (seconds = 0)                // Tests if there really was an artist embedded in the file
    then Some (new System.TimeSpan(0, 0, seconds))
    else None
