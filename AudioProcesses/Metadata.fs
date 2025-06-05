module Metadata
open ATL
open ATL.AudioData
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
    