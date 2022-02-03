module Json

open System.IO
open System.Text.Json
open System.Text.Encodings.Web

let options =
    JsonSerializerOptions(
        JsonSerializerDefaults.Web,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip)

let read defaultValue path =
    let result =
        if IO.fileExists path then
            use stream = File.OpenRead(path)
            JsonSerializer.Deserialize(stream, options = options)
        else defaultValue
    if isNull(box result) then defaultValue else result

let write value path =
    use stream = IO.createFile path
    JsonSerializer.Serialize(stream, value, options = options)
