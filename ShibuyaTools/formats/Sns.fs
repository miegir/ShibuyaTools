module Sns

let key = "CSD-CSCNV:01.82\000"B
let keyLength = key.Length

let decodeBuffer (buffer: byte[]) =
    for i = 0 to buffer.Length-1 do
        buffer[i] <- buffer[i] - key[i % keyLength]

let encodeBuffer (buffer: byte[]) =
    for i = 0 to buffer.Length-1 do
        buffer[i] <- buffer[i] + key[i % keyLength]

type File =
    { Offset: int32
      Size: int32 }

type Header =
    { Zero1: string
      Dummy1: int32
      Zero2: int32
      Dummy2: int32
      Zero3: string
      Offset1: int32
      Offset2: int32
      Zero4: string
      Offset3: int32
      Zero5: string
      Offset4: int32
      Zero6: string
      Zero7: string
      Dummy3: int32
      Zero8: string
      Name: string
      Files: File[] }

open Reader

let readFile reader =
    { Offset = readInt32 reader
      Size = readInt32 reader }

let readHeader reader =
    { Zero1 = readString 16 reader
      Dummy1 = readInt32 reader
      Zero2 = readInt32 reader
      Dummy2 = readInt32 reader
      Zero3 = readString 12 reader
      Offset1 = readInt32 reader
      Offset2 = readInt32 reader
      Zero4 = readString 0x30 reader
      Offset3 = readInt32 reader
      Zero5 = readString 12 reader
      Offset4 = readInt32 reader
      Zero6 = readString 12 reader
      Zero7 = readString 0x50 reader
      Dummy3 = readInt32 reader
      Zero8 = readString 12 reader
      Name = readString 0x20 reader
      Files = readElementArray readFile reader }

let extract buffer resourceDir =
    use reader = fromByteArray buffer
    let header = readHeader reader
    header.Files |> Seq.iter (fun file ->
        seekFromStart file.Offset reader
        let bytes = readByteArray file.Size reader
        let name = System.Text.Encoding.ASCII.GetString(bytes, 0, 0x30).TrimEnd('\000')
        let xmlPath = IO.combine resourceDir name
        let binPath = xmlPath + ".bin"
        IO.writeBytes bytes binPath
        let txtPath = xmlPath + ".txt"
        Xml.extract txtPath bytes)

open Writer

let writeFile (e: File) writer =
    writeInt32 e.Offset writer
    writeInt32 e.Size writer

let writeHeader (e: Header) writer =
    writeString 16 e.Zero1 writer
    writeInt32 e.Dummy1 writer
    writeInt32 e.Zero2 writer
    writeInt32 e.Dummy2 writer
    writeString 12 e.Zero3 writer
    writeInt32 e.Offset1 writer
    writeInt32 e.Offset2 writer
    writeString 0x30 e.Zero4 writer
    writeInt32 e.Offset3 writer
    writeString 12 e.Zero5 writer
    writeInt32 e.Offset4 writer
    writeString 12 e.Zero6 writer
    writeString 0x50 e.Zero7 writer
    writeInt32 e.Dummy3 writer
    writeString 12 e.Zero8 writer
    writeString 0x20 e.Name writer
    writeElementArray writeFile e.Files writer

type private FileSource =
    { Name: string
      Body: byte[] }

let patch timeTracker buffer resourceDir =
    use reader = Reader.fromByteArray buffer
    let header = readHeader reader
    let mutable offset = int32(Reader.tell reader)
    Reader.seekFromStart header.Offset1 reader
    let tail1 = Reader.readByteArrayToOffset header.Offset2 reader
    let tail2 = Reader.readByteArrayToOffset header.Offset3 reader
    let tail3 = Reader.readByteArrayToOffset header.Offset4 reader
    let tail4 = Reader.readByteArrayToEnd reader

    let files =
        header.Files |> Array.map (fun file ->
            Reader.seekFromStart file.Offset reader
            let bytes = Reader.readByteArray file.Size reader
            let name = System.Text.Encoding.ASCII.GetString(bytes, 0, 0x30).TrimEnd('\000')
            let jsonName = name + ".txt"
            let jsonPath = IO.combine resourceDir jsonName
            IO.TimeTracker.addPath jsonPath timeTracker
            let body = Xml.translate jsonPath bytes
            { Name = name; Body = body })

    let targetFiles =
        files |> Array.map (fun file ->
            let pos = offset
            let length = file.Body.Length
            offset <- offset + length
            { Offset = pos
              Size = length } )

    // offset is now the tail offset
    let tail1Offset = offset
    let tail2Offset = tail1Offset + tail1.Length
    let tail3Offset = tail2Offset + tail2.Length
    let tail4Offset = tail3Offset + tail3.Length
    let offsetDiff = tail1Offset - header.Offset1

    let targetHeader =
        { header with
            Offset1 = tail1Offset
            Offset2 = tail2Offset
            Offset3 = tail3Offset
            Offset4 = tail4Offset
            Files = targetFiles }

    use writerStream = IO.newMemoryStream()
    use writer = Writer.fromStream writerStream

    writeHeader targetHeader writer

    files |> Array.iter (fun file ->
        Writer.writeByteArray file.Body writer)

    Writer.writeByteArray tail1 writer
    Writer.writeByteArray tail2 writer

    // fix offsets in the tail3
    use offsetReader = Reader.fromByteArray tail3
    let offsetCount = header.Files.Length * 2
    for _ in 1..offsetCount do
        let originalOffset = Reader.readInt32 offsetReader
        let updatedOffset = if originalOffset = 0 then 0 else originalOffset + offsetDiff
        Writer.writeInt32 updatedOffset writer

    let tail3Content = Reader.readByteArrayToEnd offsetReader
    Writer.writeByteArray tail3Content writer
    Writer.writeByteArray tail4 writer
    writerStream.ToArray()
