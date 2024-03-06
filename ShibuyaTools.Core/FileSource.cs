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

    public FileTarget CreateTarget(ProgressCallback<long> callback)
    {
        var createBackupIfNotExists = !Path.Exists(backupPath);

        if (createBackupIfNotExists)
        {
            // Backup source if backup directory is different
            var sourceDir = Path.GetDirectoryName(sourcePath);
            var backupDir = Path.GetDirectoryName(backupPath);

            if (sourceDir != backupDir)
            {
                using var target = new FileTarget(backupPath);
                using var source = File.OpenRead(sourcePath);
                source.CopyTo(target.Stream, callback);
                target.CopyFileInfo(sourcePath);
                target.Commit();

                createBackupIfNotExists = false; // already backed up
            }
        }

        return new(sourcePath, createBackupIfNotExists);
    }

    public void Unroll()
    {
        if (File.Exists(backupPath))
        {
            File.Move(backupPath, sourcePath, overwrite: true);
        }
    }
}
