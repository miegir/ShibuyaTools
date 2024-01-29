namespace ShibuyaTools.Core;

public sealed class FileSource(string sourcePath)
{
    private readonly string backupPath = sourcePath + ".bak";

    private string ReadPath => File.Exists(backupPath) ? backupPath : sourcePath;
    public string FileName => Path.GetFileName(sourcePath);
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(sourcePath);
    public FileDestination Destination => new(sourcePath);
    public DateTime LastWriteTimeUtc => File.GetLastWriteTimeUtc(ReadPath);

    public FileStream OpenRead() => File.OpenRead(ReadPath);
    public bool CanUnroll() => File.Exists(backupPath);
    public FileTarget CreateTarget() => new(sourcePath, createBackupIfNotExists: true);

    public void Unroll()
    {
        if (File.Exists(backupPath))
        {
            File.Move(backupPath, sourcePath, overwrite: true);
        }
    }

    public static IEnumerable<FileSource> EnumerateFiles(string directory, string searchPattern)
    {
        foreach (var path in Directory.EnumerateFiles(directory, searchPattern))
        {
            yield return new FileSource(path);
        }
    }

    public static IEnumerable<FileSource> EnumerateFiles(string directory, params string[] searchPatterns)
    {
        foreach (var searchPattern in searchPatterns)
        {
            foreach (var source in EnumerateFiles(directory, searchPattern))
            {
                yield return source;
            }
        }
    }
}
