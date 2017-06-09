using System.IO;
using System.Linq;

namespace SpyStudio.Extensions
{
    public static class DirectoryInfoExtensions
    {
        public static long GetSize(this DirectoryInfo aDirectory)
        {
            return aDirectory.GetFiles().Sum(file => file.Length)
                + aDirectory.GetDirectories().Sum(directory => directory.GetSize());
        }
    }
}
