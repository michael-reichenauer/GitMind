using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace GitMind.Utils.GlobPatterns
{
    public static class GlobExtensions
    {
        public static IEnumerable<DirectoryInfo> GlobDirectories(this DirectoryInfo di, string pattern)
        {
            var glob = new GitMind.Utils.GlobPatterns.Glob(pattern);
            var truncateLength = di.FullName.Length + 1;

            return di.EnumerateDirectories("*", SearchOption.AllDirectories).Where(info => glob.IsMatch(info.FullName.Remove(0, truncateLength)));
        }

        public static IEnumerable<FileInfo> GlobFiles(this DirectoryInfo di, string pattern)
        {
            var glob = new GitMind.Utils.GlobPatterns.Glob(pattern);
            var truncateLength = di.FullName.Length + 1;

            return di.EnumerateFiles("*", SearchOption.AllDirectories).Where(info => glob.IsMatch(info.FullName.Remove(0, truncateLength)));
        }

        public static IEnumerable<FileSystemInfo> GlobFileSystemInfos(this DirectoryInfo di, string pattern)
        {
            var glob = new GitMind.Utils.GlobPatterns.Glob(pattern);
            var truncateLength = di.FullName.Length + 1;

            return di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).Where(info => glob.IsMatch(info.FullName.Remove(0, truncateLength)));
        }
    }
}
