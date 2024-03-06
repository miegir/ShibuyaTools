﻿using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("unroll")]
internal class UnrollCommand(ILogger<UnrollCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path")]
    public string GamePath { get; }

    [Option("-b|--backup-directory")]
    public string BackupDirectory { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        var game = new ShibuyaGame(
            logger: logger,
            gamePath: GamePath,
            backupDirectory: BackupDirectory);

        game.Unroll();

        logger.LogInformation("executed.");
    }
}
