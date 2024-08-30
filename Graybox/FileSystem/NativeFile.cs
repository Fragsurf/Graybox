﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Graybox.FileSystem
{
    /// <summary>
    /// An implementation of the native windows file system using the IFile interface.
    /// </summary>
    public class NativeFile : IFile
    {
        protected FileInfo FileInfo { get; set; }
        protected DirectoryInfo DirectoryInfo { get; set; }

        public NativeFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        public NativeFile(DirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
        }

        public NativeFile(string filePath)
        {
            if (Directory.Exists(filePath)) DirectoryInfo = new DirectoryInfo(filePath);
            else FileInfo = new FileInfo(filePath);
        }

        public FileSystemType Type
        {
            get { return FileSystemType.Native; }
        }

        public IFile Parent
        {
            get { return IsContainer ? (DirectoryInfo.Parent == null ? null : new NativeFile(DirectoryInfo.Parent)) : new NativeFile(FileInfo.Directory); }
        }

        public string FullPathName
        {
            get { return IsContainer ? DirectoryInfo.FullName : FileInfo.FullName; }
        }

        public string Name
        {
            get { return IsContainer ? DirectoryInfo.Name : FileInfo.Name; }
        }

        public string NameWithoutExtension
        {
            get { return IsContainer ? "" : Path.GetFileNameWithoutExtension(FileInfo.FullName); }
        }

        public string Extension
        {
            get { return IsContainer ? "" : FileInfo.Extension.Any() ? FileInfo.Extension.Substring(1) : ""; }
        }

        public bool Exists
        {
            get { return IsContainer ? DirectoryInfo.Exists : FileInfo.Exists; }
        }

        public long Size
        {
            get { return IsContainer ? 0 : FileInfo.Length; }
        }

        public bool IsContainer
        {
            get { return DirectoryInfo != null; }
        }

        public int NumChildren
        {
            get { return IsContainer ? GetChildren().Count() : 0; }
        }

        public int NumFiles
        {
            get { return IsContainer ? GetFiles().Count() : 0; }
        }

        public Stream Open()
        {
            if (IsContainer) throw new FileNotFoundException("Unable to open a container.");
            return FileInfo.Open(FileMode.Open, FileAccess.Read);
        }

        public byte[] ReadAll()
        {
            return File.ReadAllBytes(FileInfo.FullName);
        }

        public byte[] Read(long offset, long count)
        {
            // Just read the whole damn file because I'm lazy
            byte[] barr = new byte[count];
            Array.Copy(ReadAll(), offset, barr, 0, count);
            return barr;
        }

        public IEnumerable<IFile> GetRelatedFiles()
        {
            if (IsContainer || Parent == null) return new List<IFile>();
            string thisName = NameWithoutExtension.ToLower();
            return Parent.GetFiles().Where(file => file.Name.Split('.')[0].ToLower() == thisName);
        }

        public IFile GetRelatedFile(string extension)
        {
            return GetRelatedFiles().FirstOrDefault(x => String.Equals(x.Extension, extension, StringComparison.CurrentCultureIgnoreCase));
        }

        private void LoadPackages()
        {
			throw new System.NotImplementedException();
            //if (_packages != null) return;
            //List<InlinePackageFile> paks = DirectoryInfo.GetFiles("*.pak").Select(x => new InlinePackageFile(x.FullName)).ToList();
            //List<InlinePackageFile> vpks = DirectoryInfo.GetFiles("*_dir.vpk").Select(x => new InlinePackageFile(x.FullName)).ToList();
            //_packages = paks.Union(vpks).ToList();
        }

        public IFile GetChild(string name)
        {
            return GetChildren().FirstOrDefault(x => String.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }

        public IEnumerable<IFile> GetChildren()
        {
			yield break;
            //if (!IsContainer) return new List<IFile>();
            //LoadPackages();
            //List<IFile> children = _packages.SelectMany(x => x.GetChildren()).ToList();
            //List<IFile> dirs = DirectoryInfo.GetDirectories().Select<DirectoryInfo, IFile>(x =>
            //{
            //    NativeFile nf = new NativeFile(x);
            //    List<IFile> paks = children.Where(p => String.Equals(x.Name, p.Name, StringComparison.CurrentCultureIgnoreCase)).ToList();
            //    if (paks.Any())
            //    {
            //        paks.Insert(0, nf);
            //        return new CompositeFile(this, paks);
            //    }
            //    return nf;
            //}).ToList();
            //foreach (IFile d in children)
            //{
            //    if (!dirs.Any(x => String.Equals(x.Name, d.Name, StringComparison.CurrentCultureIgnoreCase)))
            //    {
            //        dirs.Add(d);
            //    }
            //}
            //return dirs;
        }

        public IEnumerable<IFile> GetChildren(string regex)
        {
            return GetChildren().Where(x => Regex.IsMatch(x.Name, regex, RegexOptions.IgnoreCase));
        }

        public IFile GetFile(string name)
        {
            return GetFiles().FirstOrDefault(x => String.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }

        public IEnumerable<IFile> GetFiles()
        {
			yield break;
            //if (!IsContainer) return new List<IFile>();
            //LoadPackages();
            //List<IFile> files = DirectoryInfo.GetFiles().Select(fileInfo => new NativeFile(fileInfo)).ToList<IFile>();
            //foreach (IFile f in _packages.SelectMany(x => x.GetFiles()))
            //{
            //    if (!files.Any(x => String.Equals(x.Name, f.Name, StringComparison.CurrentCultureIgnoreCase)))
            //    {
            //        files.Add(f);
            //    }
            //}
            //return files;
        }

        public IEnumerable<IFile> GetFiles(string regex)
        {
            return GetFiles().Where(x => Regex.IsMatch(x.Name, regex, RegexOptions.IgnoreCase));
        }

        public IEnumerable<IFile> GetFilesWithExtension(string extension)
        {
            return GetFiles().Where(x => String.Equals(x.Extension, extension, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
