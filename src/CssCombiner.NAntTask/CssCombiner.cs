using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System.Collections.Generic;

namespace CssCombiner.NAntTask
{
    [TaskName("CssCombiner")]
    public class CssCombiner : Task
    {
        [BuildElement("fileset", Required = true)]
        public virtual FileSet CssFiles { get; set; }

        [TaskAttribute("assemblyVersion", Required = false)]
        public virtual string AssemblyVersion { get; set; }

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
                if (FilesToKeep.FileNames.Cast<string>().Any(fileToKeep => string.Equals(file, fileToKeep, StringComparison.CurrentCultureIgnoreCase)))
                {
                    Echo("Keeping file: "+ GetRelativeFilePath(file));
                    return true;
                }
            }
            return false;
        }

        private string GetRelativeFilePath(string file)
        {
            return file.Replace(WorkingDir, "");
        }

        private void CombineCss(string filename)
        {
            if (Verbose)
                Console.WriteLine("Scanning: " + GetRelativeFilePath(filename));

            var replacedFile = ReplaceImports(filename);
            replacedFile = AppendBuildVersionToImagesUrls(replacedFile);

            SaveFile(filename, replacedFile);
        }

        private string AppendBuildVersionToImagesUrlsMatch(Match m)
        {
            var url = m.Groups[2].ToString().Trim();

            var version = GetRevisionNumber();

            var closeQuote = string.Empty;
            if (!url.Contains("?v=") && !url.Contains("&v=") && !url.Contains("//"))
            {
                if (url.EndsWith("'") || url.EndsWith("\""))
                {
                    closeQuote = url[url.Length - 1].ToString();
                    url = url.Substring(0, url.Length - 1);
                }
                url += url.Contains("?") ? "&v=" + version : "?v=" + version;
            }
            
            return m.Groups[1] + url + closeQuote + m.Groups[3];
        }

        private string GetRevisionNumber()
        {
            string revision;
            if (!AssemblyVersion.Contains("."))
            {
                revision = AssemblyVersion;
            }
            else
            {
                revision = HttpUtility.HtmlEncode(AssemblyVersion.Substring(AssemblyVersion.LastIndexOf(".") + 1));
            }

            return revision;
        }

        protected string AppendBuildVersionToImagesUrls(string file)
        {
            var r = new Regex(@"(url[\s]*\([\s]*)(.+)(\).*)", RegexOptions.IgnoreCase);

            var result = r.Replace(file, new MatchEvaluator(AppendBuildVersionToImagesUrlsMatch));

            if (Verbose)
                Console.WriteLine("the result of the URL replace is: " + result);

            return result;
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

        private void AddToProcessedFiles(string fileToInclude)
        {
            if (m_processedFiles.Contains(fileToInclude) == false)
                m_processedFiles.Add(fileToInclude);
        }

        private string GetFullPath(string containerPath, string importPath)
        {
            string path;

            if (importPath.StartsWith("/"))
                path = WorkingDir.TrimEnd('/') + importPath;
            else
            {
                string pathGetFileName = Path.GetFileName(containerPath);
                path = containerPath.Replace(pathGetFileName, string.Empty) + importPath;
            }

            return path;
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