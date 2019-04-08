using System.IO;
using System.Text;

namespace Extensions
{
    public static class FileInfoExtensions
    {
        public static StreamReader OpenSharedRead(this FileInfo file)
        {
            return new StreamReader(OpenSharedReadStream(file), Encoding.UTF8, false, 1024, true);
        }

        public static FileStream OpenSharedReadStream(this FileInfo file)
        {
            return File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

        public static StreamWriter OpenSharedWrite(this FileInfo file)
        {
            return new StreamWriter(OpenSharedWriteStream(file), Encoding.UTF8, 1024, true);
        }

        public static FileStream OpenSharedWriteStream(this FileInfo file)
        {
            return File.Open(file.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        }
    }
}