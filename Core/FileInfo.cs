using System;
using System.Collections.Generic;
using System.Text;

namespace UniBridge.Core
{
    public struct FileInfo
    {
        public string Id;
        public string Name;
        public string Path;
        public string MimeType;
        public readonly bool IsFolder => MimeType == "application/vnd.google-apps.folder";

        public override readonly string ToString()
        {
            return new StringBuilder()
                .Append("ID: ")
                .Append(Id)
                .AppendLine("")
                .Append("Name: ")
                .Append(Name)
                .AppendLine("")
                .Append("Path: ")
                .Append(Path)
                .AppendLine("")
                .Append("Type: ")
                .Append(IsFolder ? "Directory" : "File")
                .ToString();
        }

        public static bool Compare(FileInfo a, FileInfo b, bool isExact = false)
        {
            return (a.Id == b.Id || !isExact) && a.Name == b.Name && a.Path == b.Path && (a.MimeType == b.MimeType || !isExact);
        }
    }

    public class FileInfoPathComparer : IEqualityComparer<FileInfo>
    {
        public bool Equals(FileInfo a, FileInfo b)
        {
            if (Object.ReferenceEquals(a, b)) return true;
            return a.Path.Equals(b.Path);
        }

        public int GetHashCode(FileInfo obj) => obj.Path.GetHashCode();
    }
}