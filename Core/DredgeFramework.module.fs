module DredgeFramework

open System.Collections.Generic
open System.Globalization
open System
open System.Drawing
open System.IO
open System.Linq
open System.Xml;
open System.Xml.XPath;
open System.Xml.Xsl
open FSharp.Reflection

open Thoth.Json.Net

let (|?) lhs rhs = if lhs = null then rhs else lhs

let joinWithNewLine (arr: string[]) = arr |> String.concat "\n"

let getCurrentVenue () = 1

let map list = list |> Map.ofList

let JoinArray (char: string) (array: 'a[]) = String.Join(char, array)

let StringReplace (search:string) (replace:string) (string:string) = (search, replace) |> string.Replace

let StringTrim (string: string) = string.Trim()

let StringSplit (separator: string) (string: string) =
    string.Split separator
            |> Array.map(fun s -> s.Trim())
let EnumerableToArray (enumerable: IEnumerable<'T>) =  enumerable.ToArray()

let getFileExtension (file: string) =  Path.GetExtension file

let GetFileContents = File.ReadAllText
let GetFileName (file: string) = Path.GetFileName file
let FileExists = File.Exists

let length (variable: 'T[]) = variable.Length
let first (array: 'a[]) = array[0]

let last (array: 'a[]) = array[array.Length-1]

let filterFirst (array:'a[]) = if array.Length > 0 then [|array[0]|] else [||]

let removeFalseValues (variable: bool[]) = variable |> Array.filter id

let jsonEncode variable = Encode.Auto.toString(4, variable)

let isOk result =
    match result with
    | Ok _ -> true
    | Error _ -> false

let isError result = result |> isOk |> not

let getOk result =
    match result with
    | Ok response -> response
    | Error message -> failwith message

let applyXSLTransform xmlString (xslFile: string) =
    let processor = XslCompiledTransform()
    processor.Load(xslFile)
    use xmlReader = XmlReader.Create(new StringReader(xmlString))
    use resultWriter = new StringWriter()
    processor.Transform(xmlReader, null, resultWriter)
    resultWriter |> string

let RunSynchronously task =
    task
    |> Async.AwaitTask
    |> Async.RunSynchronously

let AppendToArray (element: 'T) (array : 'T[]) = Array.append [|element|] array

let ToLowerCase (string: string) = string.ToLower()
let ToUpperCase (string: string) = string.ToUpper()
let ToTitleCase (string: string) = CultureInfo.CurrentCulture.TextInfo.ToTitleCase <| string

let recordToMap (record: 'T) =
    seq {
        for prop in FSharpType.GetRecordFields(typeof<'T>) -> prop.Name, prop.GetValue(record) |> string
    }
    |> Map.ofSeq

let status (status: string) result =
    map [
        "status", status
        "data", (jsonEncode result)
    ]

let ajaxFail data = status "fail" data
let ajaxSuccess data = status "success" data

let loadImage image = Image.FromFile image
let GetImageSize image =
    let loadedImage = loadImage image
    loadedImage.Width, loadedImage.Height

let CurrentTime() = DateTimeOffset.Now.ToUnixTimeSeconds() |> int