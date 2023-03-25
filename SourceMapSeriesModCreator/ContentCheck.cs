using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SourceMapSeriesModCreator
{
    public static class ContentCheck
    {
        public static MainWindow Window;

        public static void GenerateFileList()
        {
            ConsoleWriter.Setup();

            if (Directory.Exists(Window.SourceContentDirectoryBox.Text) == false)
            {
                ConsoleWriter.WriteLine(String.Format("ERROR: Source directory \"{0}\" does not exist", Window.SourceContentDirectoryBox.Text));
                ConsoleWriter.MakeClosable();
                return;
            }

            Window.ToggleControls(false);

            string AllFiles = "";

            // First, get the files in the source content directory
            ConsoleWriter.WriteLine("Enumerating files for source content directory \"" + Window.SourceContentDirectoryBox.Text + "\"...");
            DirectoryInfo sourceFilesDir = Directory.CreateDirectory(Window.SourceContentDirectoryBox.Text);

            IEnumerable<FileInfo> sourceFileInfos = sourceFilesDir.EnumerateFiles("*", SearchOption.AllDirectories);

            // Now get the relative source file paths and write them to the stream
            ConsoleWriter.WriteLine("Converting source files to file list (this may take a while)...");
            foreach (FileInfo file in sourceFileInfos)
            {
                string path = Path.GetRelativePath(Window.SourceContentDirectoryBox.Text, file.FullName) + '\n';
                ConsoleWriter.Write(path);
                AllFiles += path;
            }

            // Write the final file
            string outputFile = Path.Combine(Window.SourceContentDirectoryBox.Text, "outputfilelist.txt");
            File.WriteAllText( outputFile, AllFiles );

            ConsoleWriter.WriteLine("Wrote file list to \"" + outputFile + "\"");

            Window.ToggleControls(true);
            ConsoleWriter.MakeClosable();
        }

        public static void RunContentCheck()
        {
            ConsoleWriter.Setup();

            // First, check what kind of source file structure we're using
            bool SourceContentIsFile = false;
            if (Path.HasExtension(Window.SourceContentDirectoryBox.Text))
            {
                if (File.Exists(Window.SourceContentDirectoryBox.Text) == false)
                {
                    ConsoleWriter.WriteLine(String.Format("ERROR: Source file list \"{0}\" does not exist", Window.SourceContentDirectoryBox.Text));
                    ConsoleWriter.MakeClosable();
                    return;
                }

                SourceContentIsFile = true;
            }
            else if (Directory.Exists(Window.SourceContentDirectoryBox.Text) == false)
            {
                ConsoleWriter.WriteLine( String.Format("ERROR: Source file list directory \"{0}\" does not exist", Window.SourceContentDirectoryBox.Text) );
                ConsoleWriter.MakeClosable();
                return;
            }

            Window.ToggleControls(false);
            DiscoveredManifests = new Dictionary<string, List<string>>();

            // Next, get the files in the source content directory
            List<string> sourceFiles = new List<string>();
            IEnumerable<FileInfo> sourceFileInfos = null;
            if (SourceContentIsFile)
            {
                sourceFileInfos = new List<FileInfo>() { new FileInfo( Window.SourceContentDirectoryBox.Text ) };
            }
            else
            {
                DirectoryInfo sourceFilesDir = Directory.CreateDirectory(Window.SourceContentDirectoryBox.Text);
                sourceFileInfos = sourceFilesDir.EnumerateFiles("*.txt", SearchOption.AllDirectories);
            }

            foreach (FileInfo file in sourceFileInfos)
            {
                ConsoleWriter.WriteLine("Gathering files from source content file list \"" + file.FullName + "\"...");

                StreamReader reader = file.OpenText();
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    sourceFiles.Add(line);
                }
                reader.Close();
            }

            // Next, get the cumulative files of all entry directories
            Dictionary<string, List<string>> entryFileLists = new Dictionary<string, List<string>>();

            // Get the relative source file paths
            foreach (FileInfo file in sourceFileInfos)
            {
                sourceFiles.Add( Path.GetRelativePath(Window.SourceContentDirectoryBox.Text, file.FullName) );
            }

            ConsoleWriter.WriteLine("Source content directory has " + sourceFiles.Count() + " files");

            // Finally, get all files in the mod template
            DirectoryInfo templateDir = Directory.CreateDirectory(Window.TemplateDirectoryBox.Text);
            IEnumerable<FileInfo> templateFileInfos = templateDir.EnumerateFiles("*", SearchOption.AllDirectories);
            List<string> templateFiles = new List<string>();

            // Get the relative entry file paths
            foreach (FileInfo file in templateFileInfos)
            {
                templateFiles.Add(Path.GetRelativePath(Window.TemplateDirectoryBox.Text, file.FullName));
            }

            // Now compare each entry
            foreach (MapEntry entry in Window.EntryData)
            {
                ConsoleWriter.WriteDivider();

                if (entry.ContentDir == "" || Directory.Exists(entry.ContentDir) == false)
                {
                    ConsoleWriter.WriteLine(String.Format("WARNING: Source directory \"{0}\" for {1} does not exist", entry.ContentDir, entry.EntryTitle));
                    continue;
                }

                string quotedTitle = "\"" + entry.EntryTitle + "\"";
                ConsoleWriter.WriteLine("Checking content for entry " + quotedTitle + "...");

                DirectoryInfo entryDir = Directory.CreateDirectory(entry.ContentDir);
                IEnumerable<FileInfo> entryFileInfos = entryDir.EnumerateFiles("*", SearchOption.AllDirectories);
                List<string> entryFiles = new List<string>();

                // Get the relative entry file paths
                foreach (FileInfo file in entryFileInfos)
                {
                    entryFiles.Add(Path.GetRelativePath(entry.ContentDir, file.FullName));
                }

                ConsoleWriter.WriteLine("Entry has " + entryFiles.Count() + " files");

                // Check for conflicts in each other entry
                foreach (KeyValuePair<string, List<string>> kvpair in entryFileLists)
                {
                    if (CheckConflicts(kvpair.Key, quotedTitle, kvpair.Value.Intersect(entryFiles)))
                    {
                        return;
                    }
                }

                // Check for template content conflicts
                if (CheckConflicts("Mod Template", quotedTitle, templateFiles.Intersect(entryFiles)))
                {
                    return;
                }

                // Now check for main content conflicts
                if (CheckConflicts("Main Source Content", quotedTitle, sourceFiles.Intersect(entryFiles)))
                {
                    return;
                }

                entryFileLists.Add(quotedTitle, entryFiles );
            }

            ConsoleWriter.WriteDivider();

            ConsoleWriter.WriteLine("No conflicts detected!");

            if (DiscoveredManifests.Count > 0)
            {
                ConsoleWriter.WriteLine("Manifests detected:");
                foreach (KeyValuePair<string, List<string>> kvpair in DiscoveredManifests)
                {
                    string msg = "- " + kvpair.Key + "\n";
                    foreach (string manifest in kvpair.Value)
                    {
                        msg += "   - " + manifest + "\n";
                    }
                    ConsoleWriter.Write(msg);
                }

                ConsoleWriter.WriteLine("\nManifests and global script files like these should be avoided when possible.\nPlease convert them to map-specific files and potentially advise the entrant to do the same in the future.\n(Automatic conversion not implemented)");
            }

            DiscoveredManifests.Clear();
            DiscoveredManifests = null;

            Window.ToggleControls(true);
            ConsoleWriter.MakeClosable();
        }

        // Just note these files at the end instead of cancelling everything
        private static readonly string[] Manifests = new string[] {
            @"scripts\game_sounds_manifest",
            @"scripts\soundscapes_manifest",
            @"resource\closecaption_",
            @"particles\particles_manifest",
        };

        private static Dictionary<string, List<string>> DiscoveredManifests;

        private static void AddDiscoveredManifest( string entry, string file )
        {
            if (DiscoveredManifests.ContainsKey(entry))
            {
                List<string> list = DiscoveredManifests[entry];
                list.Add(file);
            }
            else
            {
                List<string> list = new List<string>();
                list.Add(file);
                DiscoveredManifests.Add(entry, list);
            }
        }

        public static bool CheckConflicts( string list1, string list2, IEnumerable<string> fileInfos )
        {
            if (fileInfos.Count() <= 0)
            {
                ConsoleWriter.WriteLine(String.Format("No conflicts detected between {0} and {1}", list1, list2));
                return false;
            }

            // Only cancel if we have non-manifests
            bool ShouldCancel = false;

            foreach (string file in fileInfos)
            {
                bool ManifestFound = false;
                foreach (string manifest in Manifests)
                {
                    if (file.StartsWith(manifest))
                    {
                        AddDiscoveredManifest(list2, file);
                        ManifestFound = true;
                        break;
                    }
                }

                if (ManifestFound)
                    continue;

                ConsoleWriter.WriteLine(String.Format("CONFLICT: \"{0}\" in {1} already exists in {2}!!!", file, list2, list1));
                ShouldCancel = true;
            }

            if (!ShouldCancel)
                return false;

            ConsoleWriter.WriteLine("Canceling due to conflicts");

            Window.ToggleControls(true);
            ConsoleWriter.MakeClosable();
            return true;
        }
    }
}
