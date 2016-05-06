using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hastoop
{
    public class NameNode
    {
        List<string> Data = new List<string>();
        List<string> ExtractLines = new List<string>();
        Dictionary<string, int> ExtractUniqueLines = new Dictionary<string, int>();
        public string NameNodeDirectory;
        public string SearchDirectory;
        public string SearchString;
        private Regex searchRegex;
        private bool isRegex;
        private Regex uniqueRegex;
        private Regex notUnqiueRegex = null;
        private string uniqueMatchGroup = "";

        int counter = 0;

        public NameNode(string NameNodeDirectory, string SearchDirectory, string SearchString)
        {
            this.NameNodeDirectory = NameNodeDirectory;
            this.SearchDirectory = SearchDirectory;
            this.SearchString = SearchString;
        }

        public void MakeRegex()
        {
            //promote the string to a regex
            searchRegex = new Regex(SearchString, RegexOptions.Compiled);
            isRegex = true;
        }

        public void BuildDataFile()
        {
            Data.Clear();

            counter = 0;

            Console.WriteLine(string.Format("Searching {0} for '{1}'", SearchDirectory, SearchString));

            ScanDirectory(SearchDirectory);

            //save to name node file all paths
            PrepareNameNodeDataDirectory(NameNodeDirectory);

            SaveNameNodeData(NameNodeDirectory);
        }

        void ScanDirectory(string drpath)
        {
            foreach (string d in Directory.GetDirectories(drpath))
            {
                if (SearchCompare(d, SearchString))
                    AddData(d);
                ScanDirectory(d);
            }

            foreach (string file in Directory.GetFiles(drpath))
            {
                IncrementCounter();

                if (SearchCompare(file, SearchString))
                    AddData(file);

                if (file.Contains(".zip"))
                    GetZipFilePaths(file);
            }
        }

        void GetZipFilePaths(string zippath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zippath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    IncrementCounter();
                    if (SearchCompare(entry.FullName, SearchString))
                        AddData(zippath + ":" + entry.FullName);
                }
            }
        }

        void IncrementCounter()
        {
            counter++;
            if (counter % 1000 == 0)
                Console.WriteLine(string.Format("Progress => {0}", counter));
        }

        void AddData(string d)
        {
            Console.WriteLine(d);
            Data.Add(d);
        }

        bool SearchCompare(string s, string has)
        {
            if (isRegex)
            {
                return searchRegex.IsMatch(s);
            }
            else
            {
                return s.ToUpper().Contains(has.ToUpper());
            }
        }

        void SaveNameNodeData(string directory)
        {
            var namenode_filepath = GetNameNodeDirectoryPath(directory) + "_namenode.dat";

            File.WriteAllLines(namenode_filepath, Data);
        }

        void PrepareNameNodeDataDirectory(string directory)
        {
            var namenode_root = GetNameNodeDirectoryPath(directory);
            if (!Directory.Exists(namenode_root))
            {
                Directory.CreateDirectory(namenode_root);
            }
        }

        string GetNameNodeDirectoryPath(string directory)
        {
            var namenode_root = ConfigurationManager.AppSettings["NameNodeRoot"];
            return namenode_root + directory + "\\";
        }

        public void Scan()
        {
            var lines = File.ReadAllLines(ConfigurationManager.AppSettings["LineExtractPath"]);

            Regex lineMatch = new Regex(ConfigurationManager.AppSettings["ScanQualifierRegex"]);
            Regex lineextract = new Regex(ConfigurationManager.AppSettings["ScanExtractRegexGroup"]);

            var scanlines = new List<string>();

            foreach (string line in lines)
            {
                if (lineMatch.IsMatch(line))
                {
                    var sl = "";
                    var m = lineextract.Match(line);

                    var names = lineextract.GetGroupNames();

                    int i;

                    foreach (string g in names)
                    {
                        if (!int.TryParse(g, out i))
                        {
                            sl += m.Groups[g].Value + "\t";
                        }
                    }

                    sl = sl.TrimEnd('\t');
                    scanlines.Add(sl);
                }
            }

            File.WriteAllLines(ConfigurationManager.AppSettings["ScanLinesResults"], scanlines);
        }

        public void ExtractUnique()
        {
            uniqueRegex = new Regex(ConfigurationManager.AppSettings["uniqueExtractRegEx"], RegexOptions.Compiled);

            if (ConfigurationManager.AppSettings["negateUniqueExtractRegEx"] != "")
                notUnqiueRegex = new Regex(ConfigurationManager.AppSettings["negateUniqueExtractRegEx"], RegexOptions.Compiled);

            uniqueMatchGroup = ConfigurationManager.AppSettings["uniquematchgroup"];

            Extract(true);
        }

        public void Extract(bool isUnique = false)
        {
            var namenode_filepath = GetNameNodeDirectoryPath(NameNodeDirectory) + "_namenode.dat";
            var extract_dir = GetNameNodeDirectoryPath(NameNodeDirectory) + "Extract\\";

            if (!Directory.Exists(extract_dir))
                Directory.CreateDirectory(extract_dir);

            string[] files = File.ReadAllLines(namenode_filepath);
            
            int i = 1;
            int t = files.Length;

            files.ToList().ForEach(f =>
            {
                if (f.Contains("."))
                {
                    Console.WriteLine(string.Format("Extracting [{0} of {1}] : {2}", i, t, f));

                    if (f.Contains(".zip"))
                    {
                        var parts = f.Split(':');

                        using (ZipArchive archive = ZipFile.OpenRead(parts[0] + ":" + parts[1]))
                        {
                            var archivefile = parts[2];
                            var extracttofile = extract_dir + "ZIPFILE_" + parts[2].Replace("\\", "_DIR_").Replace("/", "_DIR_");

                            archive.GetEntry(archivefile).ExtractToFile(extracttofile, true);

                            getMerchantLines(extracttofile, isUnique);
                        }
                    }
                    else
                    {
                        //var parts = f.Split('\\');
                        var newfilename = extract_dir + f.Replace(SearchDirectory, "").Replace("\\", "_DIR_");
                        File.Copy(f, newfilename, true);
                    }

                    i++;
                }
            });

            if (isUnique)
            {
                var data = (from j in ExtractUniqueLines.Keys select $"{j}\t{ExtractUniqueLines[j] + ""}").ToList();

                File.WriteAllLines(ConfigurationManager.AppSettings["LineExtractPath"], data);
            }
            else
            {
                File.WriteAllLines(ConfigurationManager.AppSettings["LineExtractPath"], ExtractLines);
            }

        }

        void getMerchantLines(string path, bool isunique)
        {
            Console.WriteLine($"Processing file {path}");

            foreach (string line in File.ReadAllLines(path))
            {
                if (isunique)
                {
                    var m = uniqueRegex.Match(line);

                    if (m.Success)
                    {
                        if (notUnqiueRegex != null)
                        {
                            var m2 = notUnqiueRegex.Match(line);
                            if (m2.Success)
                                continue;
                        }

                        var val = m.Groups[uniqueMatchGroup].Value;

                        if (ExtractUniqueLines.ContainsKey(val))
                            ExtractUniqueLines[val]++;
                        else
                            ExtractUniqueLines.Add(val, 1);
                    }
                }
                else
                {
                    if (line.Contains(ConfigurationManager.AppSettings["lineSearchString"]))
                    {
                        ExtractLines.Add(line);
                    }
                }
            }

            if (isunique)
            {
                Console.WriteLine($"{ExtractUniqueLines.Count} unqiue extracted...");
            }
            else
            {
                Console.WriteLine($"{ExtractLines.Count} extracted...");
            }

            File.Delete(path);
        }
    }
}
