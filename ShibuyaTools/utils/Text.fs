﻿module Text

let compact = String.map (function
    | 'А' -> 'A'
    | 'В' -> 'B'
    | 'С' -> 'C'
    | 'Е' -> 'E'
    | 'Н' -> 'H'
    | 'К' -> 'K'
    | 'М' -> 'M'
    | 'О' -> 'O'
    | 'Р' -> 'P'
    | 'Т' -> 'T'
    | 'Х' -> 'X'
    | 'а' -> 'a'
    | 'с' -> 'c'
    | 'е' -> 'e'
    | 'о' -> 'o'
    | 'р' -> 'p'
    | 'х' -> 'x'
    | 'у' -> 'y'
    | c -> c)