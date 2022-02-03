module IO

open System
open System.IO

type Info = private Info of FileInfo
type TimeTracker = private { mutable Time: DateTime }

module TimeTracker =
    let fromPath path =
        { Time = FileInfo(path).LastWriteTimeUtc }

    let addInfo (Info(info)) tracker =
        tracker.Time <- max info.LastWriteTimeUtc tracker.Time

    let addPath = FileInfo >> Info >> addInfo

    let makeInfinity tracker =
        tracker.Time <- DateTime.MaxValue

    let notOlderThanPath path tracker =
        let info = FileInfo(path)
        if info.Exists
        then info.LastWriteTimeUtc >= tracker.Time
        else false

let combine path1 path2 = Path.Combine(path1, path2)

let readBytes (path: string) =
    File.ReadAllBytes(path)

let private ensurePath (path: string) =
    let dir = Path.GetDirectoryName(path)
    if not(Directory.Exists(dir))
    then Directory.CreateDirectory(dir) |> ignore

let createFile path =
    ensurePath path
    File.Create(path)

let writeBytes bytes (path: string) =
    ensurePath path
    File.WriteAllBytes(path, bytes)

let fileInfo path = Info(FileInfo(path))
let fileExists path = File.Exists(path)
let infoExists (Info(info)) = info.Exists
let deleteFile path = if File.Exists(path) then File.Delete(path)
let infoLength (Info(info)) = info.Length

let copyInfoTo (writer: BinaryWriter) (Info(info)) =
    use stream = info.OpenRead()
    stream.CopyTo(writer.BaseStream)

let moveFile fromPath toPath =
    ensurePath toPath
    File.Move(fromPath, toPath, true)

let changeExtension extension path = Path.ChangeExtension(path, extension)
let extensionIs extension (path: string) =
    String.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase)

let newMemoryStream() = new MemoryStream()
