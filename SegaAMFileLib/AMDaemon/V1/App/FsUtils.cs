using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using DiscUtils;
using DiscUtils.Ntfs;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public class FsUtils {
    public delegate void ProgressCallback(string file, int num, int total, long currentSize, long processedSize, long totalSize);

    private delegate object SpanPattern<T>(ReadOnlySpan<T> s);

    public static String DumpNtfsFileSystemProperties(Stream data) {
        byte[] buf = new byte[512];
        data.ReadExactly(buf);
        return DumpNtfsFileSystemProperties(buf);
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public static String DumpNtfsFileSystemProperties(byte[] data) {
        // what the fuck
        ParameterExpression param = Expression.Parameter(typeof(ReadOnlySpan<byte>));
        Type type = typeof(NtfsFileSystem).Assembly.GetType("DiscUtils.Ntfs.BiosParameterBlock");
        MethodInfo methodInfo = type.GetMethod("FromBytes", BindingFlags.Static | BindingFlags.NonPublic);
        MethodCallExpression ctorCall = Expression.Call(methodInfo, param);
        SpanPattern<byte> delegateSpan = Expression.Lambda<SpanPattern<byte>>(ctorCall, param).Compile();
        object biosBlock = delegateSpan(data);

        TextWriter tw = new StringWriter();
        type.GetMethod("Dump", BindingFlags.Instance | BindingFlags.Public).Invoke(biosBlock, new object[] { tw, "\n" });

        return tw.ToString();
    }


    public static void ExtractRecursive(ILogger log, DiscDirectoryInfo directory, String targetDirectory, ProgressCallback callback) {
        log.LogDebug("Scanning directory: " + directory.FullName);
        List<DiscFileInfo> extractFileList = GatherFilesRecursive(directory);
        long totalSize = extractFileList.Sum(f => f.Length);
        int totalCount = extractFileList.Count;
        int num = 0;
        long processedSize = 0;
        foreach (DiscFileInfo file in extractFileList) {
            log.LogDebug("Extracting " + file.FullName);
            try {
                String targetFile = Path.Combine(targetDirectory, file.FullName);
                DirectoryInfo parentPath = Directory.GetParent(targetFile);
                Debug.Assert(parentPath != null, nameof(parentPath) + " != null");
                if (!parentPath.Exists) {
                    log.LogDebug("Creating directory: " + parentPath);
                    parentPath.Create();
                }

                callback?.Invoke(file.FullName, ++num, totalCount, file.Length, processedSize, totalSize);
                using (FileStream target = File.Create(Path.Combine(targetDirectory, file.FullName))) {
                    file.OpenRead().CopyTo(target);
                }

                processedSize += file.Length;
            } catch (Exception ex) {
                throw new IOException("Failed to extract " + file.FullName + " from " + directory.FullName + " to " + targetDirectory, ex);
            }
        }
    }

    private static List<DiscFileInfo> GatherFilesRecursive(DiscDirectoryInfo directory) {
        List<DiscFileInfo> list = directory.GetFiles().ToList();

        foreach (DiscDirectoryInfo dir in directory.GetDirectories()) {
            list.AddRange(GatherFilesRecursive(dir));
        }

        return list;
    }
}