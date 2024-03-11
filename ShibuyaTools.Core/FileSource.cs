namespace ShibuyaTools.Core;

public sealed class FileSource(string sourcePath, string backupPath)
{
    public string FileName => Path.GetFileName(sourcePath);
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(sourcePath);
    public FileDestination Destination => new(sourcePath);
    public DateTime LastWriteTimeUtc => File.GetLastWriteTimeUtc(ReadPath);

    private string ReadPath => File.Exists(backupPath) ? backupPath : sourcePath;

    public FileStream OpenRead() => File.OpenRead(ReadPath);
    public bool CanUnroll() => File.Exists(backupPath);

    public FileTarget CreateTarget(ProgressCallback<long> callback, CancellationToken cancellationToken)
    {
        var createBackupIfNotExists = !Path.Exists(backupPath);

        if (createBackupIfNotExists)
        {
            // Backup source if backup directory is different
            var sourceDir = Path.GetDirectoryName(sourcePath);
            var backupDir = Path.GetDirectoryName(backupPath);

            if (sourceDir != backupDir)
            {
                Copy(sourcePath, backupPath, callback, cancellationToken);
                createBackupIfNotExists = false; // already backed up
            }
        }

        return new(sourcePath, createBackupIfNotExists);
    }

    public void Unroll(ProgressCallback<long> callback, CancellationToken cancellationToken)
    {
        if (File.Exists(backupPath))
        {
            var sourceDir = Path.GetDirectoryName(sourcePath);
            var backupDir = Path.GetDirectoryName(backupPath);

            // Only copy backup if the directory is different
            if (sourceDir == backupDir)
            {
                File.Move(backupPath, sourcePath, overwrite: true);
            }
            else
            {
                Copy(backupPath, sourcePath, callback, cancellationToken);
            }
        }
    }

    private static void Copy(string sourcePath, string targetPath, ProgressCallback<long> callback, CancellationToken cancellationToken)
    {
        using var target = new FileTarget(targetPath);
        using var source = File.OpenRead(sourcePath);
        source.CopyTo(target.Stream, callback, cancellationToken);
        target.CopyFileInfo(sourcePath);
        target.Commit();
    }
}
