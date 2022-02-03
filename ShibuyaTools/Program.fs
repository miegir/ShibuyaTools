open System
open Argu

type ExtractArgs =
    | [<Mandatory; AltCommandLine("-s")>] Source_Dir of string
    | [<Mandatory; AltCommandLine("-r")>] Resource_Dir of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Source_Dir _ -> "source directory with .wad files."
            | Resource_Dir _ -> "directory to which the files will be extracted."

type PatchArgs =
    | [<Mandatory; AltCommandLine("-s")>] Source_Dir of string
    | [<Mandatory; AltCommandLine("-r")>] Resource_Dir of string
    | [<Mandatory; AltCommandLine("-t")>] Target_Dir of string
    | Launch

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Source_Dir _ -> "source directory with original .wad files."
            | Resource_Dir _ -> "directory with the resources that are to be replaced."
            | Target_Dir _ -> "target directory where to place the patched .wad files."
            | Launch -> "launch the game .exe from target directory after patching."

type ProgramArgs =
    | [<CliPrefix(CliPrefix.None)>] X of ParseResults<ExtractArgs>
    | [<CliPrefix(CliPrefix.None)>] P of ParseResults<PatchArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | X _ -> "extract .wad files."
            | P _ -> "patch .wad files and optionally launches the game."

type CliError =
    | ArgumentsNotSpecified

let getExitCode = function
    | Ok _ -> 0
    | Error ArgumentsNotSpecified -> 1

[<EntryPoint>]
let main args =
    let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<ProgramArgs>(errorHandler = errorHandler)

    match parser.Parse(args) with
    | p when p.Contains(X) ->
        let c = p.GetResult(X)
        let sourceDir = c.GetResult(ExtractArgs.Source_Dir)
        let resourceDir = c.GetResult(ExtractArgs.Resource_Dir)

        let run name =
            let sourcePath = IO.combine sourceDir name
            let resourcePath = IO.combine resourceDir name
            Wad.extract sourcePath resourcePath

        run "shibuya_desktop_data_main.wad"
        run "shibuya_desktop_data_main_patch.wad"
        run "shibuya_desktop_data_core.wad"
        run "shibuya_desktop_data_core_patch.wad"

        Ok ()

    | p when p.Contains(P) ->
        let c = p.GetResult(P)
        let sourceDir = c.GetResult(PatchArgs.Source_Dir)
        let resourceDir = c.GetResult(PatchArgs.Resource_Dir)
        let targetDir = c.GetResult(PatchArgs.Target_Dir)

        let run name =
            let sourcePath = IO.combine sourceDir name
            let resourcePath = IO.combine resourceDir name
            let targetPath = IO.combine targetDir name
            Wad.patch sourcePath resourcePath targetPath

        run "shibuya_desktop_data_main.wad"
        run "shibuya_desktop_data_main_patch.wad"
        run "shibuya_desktop_data_core.wad"
        run "shibuya_desktop_data_core_patch.wad"

        if c.Contains(PatchArgs.Launch) then
            let exePath = IO.combine targetDir "428 Shibuya Scramble.exe"
            System.Diagnostics.Process.Start(exePath) |> ignore

        Ok ()

    | _ ->
        parser.PrintUsage() |> eprintfn "%s"
        Error ArgumentsNotSpecified

    |> getExitCode
