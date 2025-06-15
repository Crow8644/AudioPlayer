// This module contains some utility functions used elsewhere
// These are mostly partially applied in other places
// By: Caleb Ausema
// 6/06/2025

module Utilities // Compiled first

open System.Text.RegularExpressions
open System.IO

let regexSeperate(regex: string, default_ret: string, toParse: string) =
    let m = Regex(regex).Match toParse       // Regex matches three groups: the parent folder, the file name, and the file extention
    if m.Success
    then [for x in m.Groups -> x.Value]                             // Composes the original string into the groups from the regex
    else [default_ret]                                              // Default parent directory

// Uses map-reduce to test if a file fits an abritrarily long list of file extentions
let matchesExtention(path: string, extentions: array<string>): bool =
    let ext: string = Path.GetExtension(path)
    extentions |> Array.map(fun s -> (s = ext)) |> Array.reduce(||)

let standardTimeDisplay(span: System.TimeSpan): string =
    if span.Hours = 0 then
         (string span.Minutes) + ":" + (sprintf "%02d" span.Seconds)
    else
         (string span.Hours) + ":" + (sprintf "%02d" span.Minutes) + ":" + (sprintf "%02d" span.Seconds)