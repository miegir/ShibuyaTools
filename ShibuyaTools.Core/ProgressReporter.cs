using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ShibuyaTools.Core;

public class ProgressReporter(ILogger logger)
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public void Restart() => stopwatch.Restart();

    public void ReportProgress(ProgressPayload<long> progress)
    {
        if (stopwatch.Elapsed.TotalSeconds >= 1)
        {
            logger.LogDebug(
                "written {count} of {total} ({progress:0.00}%)",
                FormatLength(progress.Position),
                FormatLength(progress.Total),
                progress.Position * 100.0 / progress.Total);

            stopwatch.Restart();
        }
    }

    private static string FormatLength(float length)
    {
        if (length < 1024) return $"{length:0.00}B";
        length /= 1024;
        if (length < 1024) return $"{length:0.00}KB";
        length /= 1024;
        if (length < 1024) return $"{length:0.00}MB";
        return $"{length:0.00}GB";
    }
}
