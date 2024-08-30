using System.Collections.Generic;

namespace Graybox.FileSystem
{
    public class FileSource
    {
        private readonly List<IFile> _roots;

        public FileSource()
        {
            _roots = new List<IFile>();
        }

        public void AddRoot(IFile source)
        {
            _roots.Add(source);
        }

        public IFile GetRoot()
        {
            return new CompositeFile(null, _roots);
        }
    }
}
