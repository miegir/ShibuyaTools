module Phrases

let private isValid (phrase: string) =
    phrase.Length > 0 && not(Seq.exists (fun c -> int c < 20 || int c > 65532) phrase)

let private encoding = System.Text.UTF8Encoding(false)

let detect bytes = seq {
    let mutable f = false
    let mutable s = 0

    for i = 0 to Array.length bytes - 1 do
        match bytes.[i] with
        | 1uy when not f ->
            f <- true
            s <- i + 1
        | 1uy when f ->
            s <- i + 1
        | 2uy when f ->
            f <- false
            let phrase = encoding.GetString(bytes[s..i-1])
            if isValid phrase then yield (s, i, phrase)
        | _ -> ()
    }

let encode (s: string) = encoding.GetBytes(s)
