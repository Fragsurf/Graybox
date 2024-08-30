using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Graybox.FileSystem
{
    public static class FileExtensions
    {
        /// <summary>
        /// Traverses a file path. If the path starts with a /, it will search from the root.
        /// If a path contains . or .., they will be treated as the current and parent directories respectively.
        /// Otherwise, files and folders will be traversed until the end of the path.
        /// </summary>
        /// <param name="file">The file to start the traversal from</param>
        /// <param name="path">The path to traverse</param>
        /// <returns>The file at the end of the path. Returns null if the path was not found.</returns>
        public static IFile TraversePath(this IFile file, string path)
        {
            IFile f = file;
            for (int i = 0; i < path.Split('/', '\\').Length; i++)
            {
                string name = path.Split('/', '\\')[i];
                if (i == 0 && name == "") while (f != null && f.Parent != null) f = f.Parent;
                else if (name == "") throw new FileNotFoundException("Invalid path.");

                if (f == null) return null;

                switch (name)
                {
                    case ".":
                        break;
                    case "..":
                        f = f.Parent;
                        break;
                    default:
                        IFile c = f.GetChild(name);
                        if (c != null)
                        {
                            f = c;
                            break;
                        }
                        c = f.GetFile(name);
                        if (c != null)
                        {
                            f = c;
                            break;
                        }
                        return null;
                }
            }
            return f;
        }

        private static IEnumerable<IFile> CollectChildren(IFile file)
        {
            List<IFile> files = new List<IFile> { file };
            files.AddRange(file.GetChildren().SelectMany(CollectChildren));
            return files;
        }

        public static IEnumerable<IFile> GetFiles(this IFile file, bool recursive)
        {
            return !recursive ? file.GetFiles() : CollectChildren(file).SelectMany(x => x.GetFiles());
        }

        public static IEnumerable<IFile> GetFiles(this IFile file, string regex, bool recursive)
        {
            return !recursive ? file.GetFiles(regex) : CollectChildren(file).SelectMany(x => x.GetFiles(regex));
        }

        public static IEnumerable<IFile> GetChildren(this IFile file, bool recursive)
        {
            return !recursive ? file.GetChildren() : CollectChildren(file);
        }

        public static IEnumerable<IFile> GetChildren(this IFile file, string regex, bool recursive)
        {
            return !recursive ? file.GetChildren(regex) : CollectChildren(file).Where(x => Regex.IsMatch(x.Name, regex, RegexOptions.IgnoreCase));
        }

        public static string GetRelativePath(this IFile file, IFile relative)
        {
            string path = file.Name;
            IFile par = file;
            while (par != null && par.FullPathName != relative.FullPathName)
            {
                if (par.Parent != null) path = par.Parent.Name + "/" + path;
                par = par.Parent;
            }
            if (par == null) return file.FullPathName;
            return path;

        }
    }
}