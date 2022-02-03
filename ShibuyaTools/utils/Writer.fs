module Writer

open System
open System.IO
open System.Text

let fromStream (stream: Stream) = new BinaryWriter(stream)
let fromPath path = fromStream(IO.createFile(path))
let fromByteArray (bytes: byte[]) = fromStream(new MemoryStream(bytes))

let tell (writer: BinaryWriter) = writer.BaseStream.Position
let seekFromStart position (writer: BinaryWriter) = writer.BaseStream.Position <- position

let writeInt16 (value: int16) (writer: BinaryWriter) = writer.Write(value)
let writeInt32 (value: int32) (writer: BinaryWriter) = writer.Write(value)
let writeInt64 (value: int64) (writer: BinaryWriter) = writer.Write(value)

let writeByte (value: byte) (writer: BinaryWriter) = writer.Write(value)

let writeByteArray (value: byte[]) (writer: BinaryWriter) = writer.Write(value)
let writeString length (value: string) (writer: BinaryWriter) =
    let bytes = Array.zeroCreate length
    Encoding.ASCII.GetBytes(value.AsSpan(), Span(bytes)) |> ignore
    writer.Write(bytes)

let writePrefixedByteArray (value: byte[]) (writer: BinaryWriter) =
    writer.Write(value.Length)
    writer.Write(value)

let writePrefixedString (value: string) (writer: BinaryWriter) =
    writePrefixedByteArray (Encoding.ASCII.GetBytes(value)) writer

let writeMagick magick writer = writeByteArray magick writer

let writeElementArray writeElement elements (writer: BinaryWriter) =
    writer.Write(Array.length elements)
    elements |> Array.iter
        (fun element -> writeElement element writer)

let writeElementList writeElement elements (writer: BinaryWriter) =
    writer.Write(List.length elements)
    elements |> List.iter
        (fun element -> writeElement element writer)
