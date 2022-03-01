module Xml

type Translation =
    { Key: string
      Val: string }

type Bookmark =
    { Location: int32
      LocationOffset: int64 }

let private readBookmarkList scriptOffset reader =
    let bookmarkList = ResizeArray()

    while Reader.tell reader < scriptOffset do
        let _ = Reader.readInt32 reader // kind
        let locationOffset = Reader.tell reader
        let location = Reader.readInt32 reader
        let _ = Reader.readNullTerminatedString reader // name
        Reader.align reader
        bookmarkList.Add({ Location = location; LocationOffset = locationOffset })

    bookmarkList.Sort(fun x y -> x.Location - y.Location)
    bookmarkList

let extract path body =
    use reader = Reader.fromByteArray body
    Reader.seekFromStart 1072 reader
    let scriptOffset = Reader.readInt32 reader
    let bookmarkList = readBookmarkList scriptOffset reader
    let mutable sourcePtr = scriptOffset
    let translations = ResizeArray()

    let registerBlock body =
        let mutable ptr = 0
        for (_, b, key) in Phrases.detect body do
            translations.Add({ Key = key; Val = key })
            ptr <- b
        sourcePtr <- sourcePtr + body.Length

    for bookmark in bookmarkList do
        let offset = scriptOffset + bookmark.Location
        registerBlock body[sourcePtr..offset-1]

    let updated = List.ofSeq translations
    let source = Json.read [] path
    if updated = source then () else
    Json.write updated path

let translate path body =
    let mutable index = 0
    let mutable existing = Json.read [||] path
    let mutable showError = true

    let popval key =
        if index >= existing.Length then key else
        let i = index
        index <- i + 1
        let e = existing[i]
        if e.Key = key then e.Val else
        if showError then eprintfn "%s: wrong key [%d] '%s'; expected '%s'" path i e.Key key
        showError <- false
        key

    use reader = Reader.fromByteArray body
    Reader.seekFromStart 1072 reader
    let scriptOffset = Reader.readInt32 reader
    let bookmarkList = readBookmarkList scriptOffset reader
    let mutable sourcePtr = scriptOffset
    let mutable targetPtr = scriptOffset
    let blocks = ResizeArray()
    let bookmarkBlock = body[..scriptOffset-1]
    use bookmarkWriter = Writer.fromByteArray bookmarkBlock
    blocks.Add(bookmarkBlock)

    let registerBlock body =
        let chunks = ResizeArray()
        let mutable ptr = 0
        for (a, b, phrase) in Phrases.detect body do
            let translation = popval phrase
            chunks.Add(body[ptr..a-1])
            chunks.Add(Phrases.encode translation)
            ptr <- b
        chunks.Add(body[ptr..])
        let block = Array.concat chunks
        blocks.Add(block)
        sourcePtr <- sourcePtr + body.Length
        targetPtr <- targetPtr + block.Length

    for bookmark in bookmarkList do
        let offset = scriptOffset + bookmark.Location
        registerBlock body[sourcePtr..offset-1]
        Writer.seekFromStart bookmark.LocationOffset bookmarkWriter
        Writer.writeInt32 (targetPtr - scriptOffset) bookmarkWriter

    let index = Array.findIndexBack ((=) 0xACuy) body
    registerBlock body[sourcePtr..index+1]
    let totalLength = blocks |> Seq.sumBy (fun x -> x.Length)
    let alignOffset = totalLength % 4
    if alignOffset <> 0 then blocks.Add(Array.zeroCreate (4 - alignOffset))
    Array.concat blocks
