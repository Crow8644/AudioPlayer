// Compiled first

module Utilities

open System.Text.RegularExpressions
open System.IO

let regexSeperate(regex: string, default_ret: string, toParse: string) =
    let m = Regex(regex).Match toParse       // Regex matches three groups: the parent folder, the file name, and the file extention
    if m.Success
    then [for x in m.Groups -> x.Value]                             // Composes the original string into the groups from the regex
    else [default_ret]                                              // Default parent directory

let matchesExtention(path: string, extentions: array<string>): bool =
    let ext: string = Path.GetExtension(path)
    extentions |> Array.map(fun s -> (s = ext)) |> Array.reduce(||)
