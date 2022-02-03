module Wad

type File =
    { Name: string
      Size: int64
      Offset: int64 }

type EntryType =
    | File = 0uy
    | Directory = 1uy

type Entry =
    { Name: string
      Type: EntryType }

type Directory =
    { Name: string
      Entries: Entry[] }

type Header =
    { Major: int32
      Minor: int32
      Extra: byte[]
      Files: File[]
      Directories: Directory[] }

open Reader
      
let readFile reader =
    { Name = readPrefixedString reader
      Size = readInt64 reader &&& 0x7FFFFFFFFFFFFFFFL
      Offset = readInt64 reader &&& 0x7FFFFFFFFFFFFFFFL }

let readEntry reader =
    { Name = readPrefixedString reader
      Type = readByte reader |> LanguagePrimitives.EnumOfValue }

let readDirectory reader =
    { Name = readPrefixedString reader
      Entries = readElementArray readEntry reader }

let readHeader reader =
    readMagick "AGAR"B reader
    { Major = readInt32 reader
      Minor = readInt32 reader
      Extra = readPrefixedByteArray reader
      Files = readElementArray readFile reader
      Directories = readElementArray readDirectory reader }

let extract sourcePath resourceDir =
    printfn "extracting %s..." sourcePath
    use reader = Reader.fromPath sourcePath
    let header = readHeader reader
    let dataOffset = tell reader
    let readFile file =
        seekFromStart (dataOffset + file.Offset) reader
        readByteArray (int32 file.Size) reader
    header.Files |> Seq.iter (fun file ->
        let path = IO.combine resourceDir file.Name
        if IO.extensionIs ".sns" file.Name then
            let bytes = readFile file
            Sns.decodeBuffer bytes
            Sns.extract bytes path
        else
            if IO.fileExists path then () else
            let bytes = readFile file
            IO.writeBytes bytes path)

open Writer

let writeFile (e: File) writer =
    writePrefixedString e.Name writer
    writeInt64 e.Size writer
    writeInt64 e.Offset writer

let writeEntry (e: Entry) writer =
    writePrefixedString e.Name writer
    writeByte (byte e.Type) writer

let writeDirectory (e: Directory) writer =
    writePrefixedString e.Name writer
    writeElementArray writeEntry e.Entries writer

let writeHeader (e: Header) writer =
    writeMagick "AGAR"B writer
    writeInt32 e.Major writer
    writeInt32 e.Minor writer
    writePrefixedByteArray e.Extra writer
    writeElementArray writeFile e.Files writer
    writeElementArray writeDirectory e.Directories writer

type private FileContent =
    | InternalContent of offset: int64 * length: int64
    | ExternalContent of info: IO.Info
    | BufferContent of byte[]
    member x.Length =
        match x with
        | InternalContent(_, length) -> length
        | ExternalContent(info) -> IO.infoLength info
        | BufferContent(buffer) -> buffer.LongLength

type private FileSource =
    { Name: string
      Body: FileContent }

let private formatLength length =
    if length < 1024L then $"{length}B" else
    let length = float length / 1024.
    if length < 1024. then $"%.2f{length}KB" else
    let length = length / 1024.
    if length < 1024. then $"%.2f{length}MB" else
    let length = length / 1024.
    $"%.2f{length}GB"

let patch sourcePath resourceDir targetPath =
    printfn "patching %s..." targetPath
    let timeTracker = IO.TimeTracker.fromPath sourcePath
    use reader = Reader.fromPath sourcePath
    let header = readHeader reader
    let dataOffset = Reader.tell reader
    let mutable offset = 0L

    let existingFileNames =
        System.Collections.Generic.HashSet(
            header.Files |> Seq.map (fun file -> file.Name))
    let addedFiles = ResizeArray()

    let files =
        header.Files
            |> Array.map (fun file ->
                if IO.extensionIs ".ivf" file.Name then
                    let jsubtName = IO.changeExtension ".jsubt" file.Name
                    if existingFileNames.Contains(jsubtName) then () else
                    let jsubtPath = IO.combine resourceDir jsubtName
                    let jsubtInfo = IO.fileInfo jsubtPath
                    if IO.infoExists jsubtInfo then
                        IO.TimeTracker.addInfo jsubtInfo timeTracker
                        addedFiles.Add({ Name = jsubtName; Body = ExternalContent(jsubtInfo) })

                let body =
                    let path = IO.combine resourceDir file.Name
                    if IO.extensionIs ".sns" file.Name then
                        Reader.seekFromStart (dataOffset + file.Offset) reader
                        let buffer = Reader.readByteArray (int32 file.Size) reader
                        Sns.decodeBuffer buffer
                        let compressed = Sns.patch timeTracker buffer path
                        Sns.encodeBuffer compressed
                        BufferContent(compressed)
                    else
                        let info = IO.fileInfo path
                        if IO.infoExists info then
                            IO.TimeTracker.addInfo info timeTracker
                            ExternalContent(info)
                        else
                            InternalContent(dataOffset + file.Offset, file.Size)
                { Name = file.Name; Body = body })

    let files = if addedFiles.Count > 0 then Array.append (addedFiles.ToArray()) files else files

    let targetHeader =
        { header with
            Files = files |> Array.map (fun file ->
                let pos = offset
                let length = file.Body.Length
                offset <- offset + length
                { Name = file.Name
                  Offset = pos
                  Size = length }) }

    if IO.fileExists targetPath then
        use reader = Reader.fromPath targetPath
        let existingHeader = readHeader reader
        if existingHeader.Files <> targetHeader.Files then
            IO.TimeTracker.makeInfinity timeTracker

    if IO.TimeTracker.notOlderThanPath targetPath timeTracker then () else
    let targetPathTmp = targetPath + "~tmp"
    using (Writer.fromPath targetPathTmp) (fun writer ->
    writeHeader targetHeader writer
    let buffer = Array.zeroCreate 65536
    let totalLength = formatLength(Writer.tell writer + Seq.sumBy (fun f -> f.Body.Length) files)
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()

    files |> Array.iter (fun file ->
        match file.Body with
        | InternalContent (offset, length) ->
            Reader.seekFromStart offset reader
            let rec loop (remains: int64) =
                let count = min remains buffer.Length
                match reader.BaseStream.Read(buffer, 0, int32 count) with
                | 0 -> ()
                | length ->
                    writer.BaseStream.Write(buffer, 0, length)
                    loop (remains - int64 length)
            loop length
        | ExternalContent (info) -> IO.copyInfoTo writer info
        | BufferContent (buffer) -> Writer.writeByteArray buffer writer
        if stopwatch.Elapsed.TotalSeconds >= 1 then
            printfn "written %s of %s" (formatLength(Writer.tell writer)) totalLength
            stopwatch.Restart()))

    IO.moveFile targetPathTmp targetPath
