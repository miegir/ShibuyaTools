module Reader

open System.IO
open System.Text

exception InvalidMagickException

let fromStream (stream: Stream) = new BinaryReader(stream)
let fromPath path = fromStream(File.OpenRead(path))
let fromByteArray (bytes: byte[]) = fromStream(new MemoryStream(bytes))

let tell (reader: BinaryReader) = reader.BaseStream.Position
let seekFromStart position (reader: BinaryReader) = reader.BaseStream.Position <- position

let alignBy alignment (reader: BinaryReader) =
    let stream = reader.BaseStream
    let offset = stream.Position % alignment
    if offset <> 0L then
        stream.Position <- stream.Position + (alignment - offset)

let align reader = alignBy 4 reader

let readInt16 (reader: BinaryReader) = reader.ReadInt16()
let readInt32 (reader: BinaryReader) = reader.ReadInt32()
let readInt64 (reader: BinaryReader) = reader.ReadInt64()

let readByte (reader: BinaryReader) = reader.ReadByte()
let readByteArray length (reader: BinaryReader) = reader.ReadBytes(length)
let readString length reader = Encoding.ASCII.GetString(readByteArray length reader).TrimEnd('\000')
let readPrefixedByteArray reader = readByteArray (readInt32 reader) reader
let readPrefixedString reader = Encoding.ASCII.GetString(readPrefixedByteArray reader)

let readMagick magick reader =
    let bytes = readByteArray (Array.length magick) reader
    if bytes <> magick then raise(InvalidMagickException)

let readElementArray readElement reader =
    Array.init
        (readInt32 reader)
        (fun _ -> readElement reader)

let readElementList readElement reader =
    List.init
        (readInt32 reader)
        (fun _ -> readElement reader)

let readByteArrayToOffset offset reader =
    let len = offset - tell reader
    readByteArray (int32 len) reader

let readByteArrayToEnd (reader: BinaryReader) =
    readByteArrayToOffset reader.BaseStream.Length reader

let readNullTerminatedString (reader: BinaryReader) =
    let rec loop list =
        match reader.Read() with
        | 0 -> Encoding.ASCII.GetString(List.rev list |> List.toArray)
        | b -> loop (byte b :: list)
    loop []
