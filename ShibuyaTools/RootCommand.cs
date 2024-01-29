using McMaster.Extensions.CommandLineUtils;

namespace ShibuyaTools;

[Command]
[Subcommand(
    typeof(ExportCommand),
    typeof(ImportCommand),
    typeof(CreateCommand),
    typeof(UnpackCommand),
    typeof(UnrollCommand))]
internal class RootCommand(CommandLineApplication application)
{
    public void OnExecute()
    {
        application.ShowHelp();
    }
}
