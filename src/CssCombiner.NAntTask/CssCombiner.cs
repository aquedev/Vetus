using System;
using System.IO;
using System.Text.RegularExpressions;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System.Collections.Generic;
using System.Linq;

namespace CssCombiner.NAntTask
{
    [TaskName("CssCombiner")]
    public class CssCombiner : Task
    {
        [BuildElement("fileset", Required = true)]
        public virtual FileSet CssFiles { get; set; }

        [BuildElement("filestokeep", Required = false)]
        public virtual FileSet FilesToKeep { get; set; }

        [TaskAttribute("workingdir", Required = true)]
        public virtual string WorkingDir { get; set; }

        private List<string> m_processedFiles = new List<string>();

        protected override void ExecuteTask()
        {
            PrintInfo();


            foreach (var fileName in CssFiles.FileNames)
            {
                CombineCss(fileName);
            }

            foreach (string file in m_processedFiles)
            {
                if (ShouldKeepFile(file))
                    continue;

                Console.WriteLine("Deleting: " + GetRelativeFilePath(file));
                File.Delete(file);
            }

        }
        private void PrintInfo()
        {
            Echo("Css combiner .......................");
            Echo("Website base path: " + WorkingDir);
            Echo("Files to process:");
            foreach (string fileName in CssFiles.FileNames)
            {
                Echo(GetRelativeFilePath(fileName));
            }

            if (FilesToKeep != null)
            {
                Echo("Files that won't be deleted:");
                foreach (string fileName in FilesToKeep.FileNames)
                {
                    Echo(GetRelativeFilePath(fileName));
                }
            }
                
        }
        private static void Echo(string message)
        {
            Console.WriteLine(message);
        }

        private bool ShouldKeepFile(string file)
        {
            if (FilesToKeep != null)
            {
                foreach (string fileToKeep in FilesToKeep.FileNames)
                {
                    if (string.Equals(file, fileToKeep, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Echo("Keeping file: "+ GetRelativeFilePath(file));
                        return true;
                    }
                        
                }
            }
            return false;
        }
        private string GetRelativeFilePath(string file)
        {
            return file.Replace(WorkingDir, "");
        }

        private void CombineCss(string file)
        {
            if (Verbose)
                Console.WriteLine("Scanning: " + GetRelativeFilePath(file));

            var replacedFile = ReplaceImports(file);

            SaveFile(file, replacedFile);
        }

        private static void SaveFile(string filename, string file)
        {
            try
            {
                using (TextWriter writer = new StreamWriter(filename))
                    writer.Write(file);
            }
            catch (IOException e)
            {
                throw new BuildException("Cannot save file: " + filename, e);
            }
        }

        private string ReplaceImports(string filename)
        {
            var file = LoadFile(filename);

            var r = new Regex("@Import url\\(([^\\)]+)\\);?", RegexOptions.IgnoreCase);

            var matches = r.Matches(file);

            if (matches.Count > 0)
            {
                if (Verbose)
                    Console.WriteLine(matches.Count + " imports found");

                foreach (Match match in matches)
                {
                    string importPath = match.Groups[1].Value.Trim('"');
                    Console.WriteLine("Importing: " + importPath);
                    string fileToInclude = GetFullPath(filename, importPath);
                    file = file.Replace(match.Value, LoadFile(fileToInclude));
                    AddToProcessedFiles(fileToInclude);
                }
            }
            else
            {
                if (Verbose)
                    Console.WriteLine("No imports found");
            }
            return file;
        }

        private void AddToProcessedFiles(string fileToInclude)
        {
            if (m_processedFiles.Contains(fileToInclude) == false)
                m_processedFiles.Add(fileToInclude);
        }

        private string GetFullPath(string containerPath, string importPath)
        {
            if (importPath.StartsWith("/"))
                return WorkingDir.TrimEnd('/') + importPath;
            else
            {
                string pathGetFileName = Path.GetFileName(containerPath);
                return containerPath.Replace(pathGetFileName, string.Empty) + importPath;
            }
        }

        private string LoadFile(string filename)
        {

            if (Verbose)
                Console.WriteLine("Loading: " + filename);

            try
            {
                using (TextReader reader = new StreamReader(filename))
                    return reader.ReadToEnd();
            }
            catch (IOException e)
            {
                throw new BuildException("Cannot open file: " + filename, e);
            }
        }
    }
}