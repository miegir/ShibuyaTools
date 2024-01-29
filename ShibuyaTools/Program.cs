using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShibuyaTools;

await Host
    .CreateDefaultBuilder()
    .ConfigureLogging(builder =>
    {
        builder.AddFilter(nameof(ShibuyaTools), LogLevel.Debug);
        builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
        });
    })
    .RunCommandLineApplicationAsync<RootCommand>(args);
