using System.Security.Cryptography;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace SegaAMFileTests;

public class FileContentComparer : IEqualityComparer<FileInfo> {
    private static readonly byte[] SALT = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

    public bool Equals(FileInfo f1, FileInfo f2) {
        if (f1 == null || f2 == null) {
            return false;
        }

        using (HMACSHA256 hash1 = new HMACSHA256(SALT))
        using (HMACSHA256 hash2 = new HMACSHA256(SALT))
        using (FileStream fs1 = f1.OpenRead())
        using (FileStream fs2 = f2.OpenRead()) {
            return hash1.ComputeHash(fs1).SequenceEqual(hash2.ComputeHash(fs2));
        }
    }

    public int GetHashCode(FileInfo fi) {
        return $"{fi.Name}{fi.Length}".GetHashCode();
    }
}

public static class Util {
    public static List<string> GatherFilesRecursive(string path) {
        List<string> files = new List<string>();
        foreach (string directory in Directory.EnumerateDirectories(path)) {
            files.AddRange(GatherFilesRecursive(directory));
        }

        files.AddRange(Directory.EnumerateFiles(path));

        return files;
    }

    public static byte[] RandomBytes(int count) {
        Random rand = new Random();
        byte[] arr = new byte[count];
        rand.NextBytes(arr);
        return arr;
    }

    public static void AssertTwoDirectoriesContentEqual(string dir1, string dir2) {
        List<FileInfo> files1 = GatherFilesRecursive(dir1).Select(x => new FileInfo(x)).ToList();
        List<FileInfo> files2 = GatherFilesRecursive(dir2).Select(x => new FileInfo(x)).ToList();
        FileContentComparer comparer = new FileContentComparer();
        IEnumerable<FileInfo> newFiles = files2.Except(files1, comparer);
        Log.Main.LogInformation("New Files: " + newFiles.Count());
        CollectionAssert.IsEmpty(newFiles);
        IEnumerable<FileInfo> deletedFiles = files1.Except(files2, comparer);
        Log.Main.LogInformation("Deleted Files: " + deletedFiles.Count());
        CollectionAssert.IsEmpty(deletedFiles);
        IEnumerable<FileInfo> changedFiles = files1.Except(files2, comparer);
        Log.Main.LogInformation("Changed Files: " + changedFiles.Count());
        CollectionAssert.IsEmpty(changedFiles);
    }
}