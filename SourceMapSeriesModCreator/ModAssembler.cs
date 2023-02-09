using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Linq;

namespace SourceMapSeriesModCreator
{
    public static class ModAssembler
    {
        public struct ScriptToken
        {
            public string Name;
            public string Replace;
        };

        public static List<ScriptToken> ScriptTokens = new List<ScriptToken>();
        public static List<string> TokenFiles = new List<string>();
        public static List<string> TokenFolders = new List<string>();

        public static void InitScriptTokens(string file = "ScriptTokens.xml")
        {
            if (ScriptTokens.Count > 0)
            {
                ScriptTokens.Clear();
                TokenFiles.Clear();
            }

            XmlDocument xml = new XmlDocument();
            xml.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file));

            if (xml.ChildNodes.Count <= 0)
                return;

            XmlNode tokens = xml.GetElementsByTagName("Tokens").Item(0);
            XmlNode files = xml.GetElementsByTagName("Files").Item(0);

            // Parse tokens
            foreach (XmlNode tokenNode in tokens)
            {
                if (tokenNode.Attributes == null)
                    continue;

                ScriptToken token = new ScriptToken
                {
                    Name = tokenNode.Attributes["Name"].Value
                };

                XmlAttribute defaultReplace = tokenNode.Attributes["Default"];
                if (defaultReplace != null)
                    token.Replace = defaultReplace.Value;

                ScriptTokens.Add(token);
            }

            // Parse files
            foreach (XmlNode fileNode in files)
            {
                if (fileNode.Attributes == null)
                    continue;

                if (fileNode.Name == "Folder")
                {
                    TokenFolders.Add( fileNode.Attributes["Name"].Value );
                }
                else
                {
                    TokenFiles.Add( fileNode.Attributes["Name"].Value );
                }
            }
        }

        // ==========================================================================

        public static MainWindow Window;
        public static string ModTitle;

        private static List<string> EntrySoundscripts = new List<string>();
        private static List<string> EntrySoundscapes = new List<string>();

        private static string BonusMapsScript = "";

        public static ScriptToken ProcessToken( ScriptToken token )
        {
            switch (token.Name)
            {
                // Parse simple text
                case "ModTitle":                token.Replace = ModTitle; break;
                case "EventTitle":              token.Replace = Window.EventTitleBox.Text; break;
                case "EventLongPlacement":      token.Replace = Window.EventLongPlacementBox.Text; break;
                case "EventShortPlacement":     token.Replace = Window.EventShortPlacementBox.Text; break;
                case "EventComment":            token.Replace = Window.EventCommentBox.Text; break;
                case "BackgroundMap":           token.Replace = Window.BackgroundMapBox.Text; break;
                case "BackgroundMap2":          token.Replace = Window.BackgroundMap2Box.Text.Length > 0 ? Window.BackgroundMap2Box.Text : Window.BackgroundMapBox.Text; break;
                case "BonusMapInfo":            token.Replace = BonusMapsScript; break;

                case "LocalizationTokens":
                    {
                        // Create tokens for each entry and entry comment
                        token.Replace = "";
                        for (int i = 0; i < Window.EntryData.Count; i++)
                        {
                            MapEntry entry = Window.EntryData[i];
                            if (entry.Placement == CompPlacement.NonEntry)
                                break; // Non-entries are always at the end of the list

                            // Generate a comment
                            string entryComment = "Created by " + entry.EntrantNames;

                            if (entry.Placement != CompPlacement.None)
                            {
                                switch(entry.Placement)
                                {
                                    case CompPlacement.Bonus:    entryComment += " (Bonus entry)"; break;
                                    case CompPlacement.First:    entryComment += " (1st Place winner)"; break;
                                    case CompPlacement.Second:   entryComment += " (2nd Place winner)"; break;
                                    case CompPlacement.Third:    entryComment += " (3rd Place winner)"; break;
                                }
                            }

                            if (entry.EntryComment != "")
                                entryComment += "\\n\\n" + entry.EntryComment;

                            // Escape quotation marks
                            entryComment = entryComment.Replace("\"", "\\\"");

                            // Remove ampersands (Source doesn't seem to like those)
                            entryComment = entryComment.Replace("&", "and");

                            token.Replace += String.Format("	\"{0}_Map{1}_Title\"		\"{2}\"\n",
                                Window.EventShortPlacementBox.Text, i, entry.EntryTitle);

                            token.Replace += String.Format("	\"{0}_Map{1}_Comment\"		\"{2}\"\n",
                                Window.EventShortPlacementBox.Text, i, entryComment);
                        }
                    } break;

                    // TODO
                    /*
                case "CloseCaptionTokens":
                    {
                        // Enumerate unique closed captioning in every entry
                    } break;
                    */

                case "Soundscapes":
                    {
                        // Enumerate unique soundscapes in every entry
                        string soundscapes = "";
                        foreach (string soundscape in EntrySoundscapes)
                        {
                            soundscapes += String.Format("	\"file\"		\"scripts/{0}.txt\"\n", soundscape);
                        }
                    } break;

                case "Soundscripts":
                    {
                        // Enumerate unique soundscripts in every entry
                        string soundscripts = "";
                        foreach (string soundscript in EntrySoundscripts)
                        {
                            soundscripts += String.Format("	\"precache_file\"		\"scripts/{0}.txt\"\n", soundscript);
                        }
                    } break;

            }

            return token;
        }

        // ==========================================================================

        public static void StartBinProcess(string filename, string args)
        {
            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(Window.BinDirectoryBox.Text, filename);
            if (!File.Exists(process.StartInfo.FileName))
            {
                Console.WriteLine("WARNING: Can't find \"{0}\" in \"{1}\"!", filename, Window.BinDirectoryBox.Text);
                return;
            }

            Console.WriteLine("Bin Process: {0} {1}", process.StartInfo.FileName, args);

            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
        }

        // ==========================================================================

        public static void CreateMod()
        {
            InitScriptTokens();

            ConsoleWriter.Setup();

            if (Directory.Exists(Window.TemplateDirectoryBox.Text) == false)
            {
                ConsoleWriter.WriteLine(String.Format("ERROR: Template directory \"{0}\" does not exist", Window.TemplateDirectoryBox.Text));
                ConsoleWriter.MakeClosable();
                return;
            }

            Window.ToggleControls(false);

            ConsoleWriter.WriteLine(String.Format("Creating mod \"{0}\" for event {1}", Window.TemplateDirectoryBox.Text, Window.EventTitleBox.Text));

            // UNDONE: Delete the mod directory if it already exists
            // (we don't do this now because of files potentially still being in use)
            // (an error is now given before CreateMod() is called)
            //if (Directory.Exists(Window.OutputDirectoryBox.Text))
            //    Directory.Delete(Window.OutputDirectoryBox.Text, true);

            // First, create the mod directory
            DirectoryInfo outputDir = Directory.CreateDirectory(Window.OutputDirectoryBox.Text);
            ModTitle = outputDir.Name;

            // Next, copy the template directory's contents to the mod directory
            DirectoryInfo templateDir = Directory.CreateDirectory(Window.TemplateDirectoryBox.Text);
            IEnumerable<FileInfo> templateFiles = templateDir.EnumerateFiles("*", SearchOption.AllDirectories);

            string relativePath;
            string destPath;

            ConsoleWriter.WriteLine("Copying template files to mod directory...");

            foreach (FileInfo file in templateFiles)
            {
                relativePath = Path.GetRelativePath(templateDir.FullName, file.FullName);
                destPath = Path.Combine(outputDir.FullName, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                if (!File.Exists(destPath))
                        file.CopyTo(destPath);
            }

            ConsoleWriter.WriteLine("Done copying template files");

            ConsoleWriter.WriteDivider();

            if (Window.EntryData.Count > 0)
            {
                ConsoleWriter.WriteLine("Identifying entry script files requiring manifest entries...");

                foreach (MapEntry entry in Window.EntryData)
                {
                    if (entry.ContentDir == "" || Directory.Exists(entry.ContentDir) == false)
                    {
                        ConsoleWriter.WriteLine(String.Format("WARNING: Source directory \"{0}\" for {1} does not exist", entry.ContentDir, entry.EntryTitle));
                        continue;
                    }

                    string scriptDir = Path.Combine(entry.ContentDir, "scripts");
                    if (Directory.Exists(scriptDir))
                    {
                        DirectoryInfo scriptDirInfo = Directory.CreateDirectory(scriptDir);
                        EntrySoundscripts.AddRange( FindSoundscriptFilesInDirectory(scriptDirInfo) );
                        EntrySoundscapes.AddRange( FindSoundscapeFilesInDirectory(scriptDirInfo) );
                    }

                    ConsoleWriter.WriteLine(String.Format("Done looking for scripts in entry \"{0}\"", entry.EntryTitle));
                }

                ConsoleWriter.WriteLine("Creating bonus maps script for entries...");

                PopulateBonusMapsScript();

                foreach (FileInfo file in templateFiles)
                {
                    if (file.Name == "placeholder.tga")
                    {
                        relativePath = Path.GetRelativePath(templateDir.FullName, file.FullName);
                        destPath = Path.Combine(outputDir.FullName, relativePath);

                        // The file needs to already exist
                        if (!File.Exists(destPath))
                            break;

                        destPath = destPath.Replace('/', '\\');
                        destPath = destPath.Remove(destPath.LastIndexOf('\\'));

                        // Instead of copying the file directly, copy it for each entry
                        for (int i = 0; i < Window.EntryData.Count; i++)
                        {
                            MapEntry entry = Window.EntryData[i];
                            if (entry.Placement == CompPlacement.NonEntry)
                                break; // Non-entries are always at the end of the list

                            destPath = Path.Combine(destPath, entry.StartingMap) + ".tga";

                            ConsoleWriter.WriteLine(String.Format("Adding placeholder thumbnail \"{0}\"", destPath));

                            if (!File.Exists(destPath))
                                file.CopyTo(destPath);

                            destPath = destPath.Remove(destPath.LastIndexOf('\\'));
                        }

                        break;
                    }
                }

                ConsoleWriter.WriteLine("Done creating bonus maps script");

                ConsoleWriter.WriteDivider();
            }

            // Now process the tokens
            ConsoleWriter.WriteLine("Processing tokens...");

            for (int i = 0; i < ScriptTokens.Count; i++)
            {
                ScriptTokens[i] = ProcessToken(ScriptTokens[i]);
            }

            ConsoleWriter.WriteLine("Inserting tokens into mod files...");

            for (int i = 0; i < TokenFiles.Count; i++)
            {
                string file = TokenFiles[i];

                // Check for token in folder/file name
                bool DirHasToken = false;
                if (file.Contains('$'))
                {
                    file = file.Replace("$", "");
                    DirHasToken = true;
                }

                destPath = Path.Combine(outputDir.FullName, file);

                if (!File.Exists(destPath))
                {
                    ConsoleWriter.WriteLine(String.Format("WARNING: File \"{0}\" does not exist!", destPath));
                    continue;
                }

                string text = File.ReadAllText(destPath);
                Encoding encoding = file.IndexOf("_english") != -1 ? Encoding.Unicode : new UTF8Encoding(false); // HACKHACK
                foreach (ScriptToken token in ScriptTokens)
                {
                    text = text.Replace("$"+token.Name+"$", token.Replace);
                }

                if (DirHasToken)
                {
                    File.Delete(destPath);
                    foreach (ScriptToken token in ScriptTokens)
                    {
                        destPath = destPath.Replace(token.Name, token.Replace);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    File.WriteAllText(destPath, text, encoding);
                }
                else
                {
                    File.WriteAllText(destPath, text, encoding);
                }

                ConsoleWriter.WriteLine(String.Format("Processed file \"{0}\"", file));
            }

            for (int i = 0; i < TokenFolders.Count; i++)
            {
                string folder = TokenFolders[i];

                // Rename folders
                if (folder.Contains('$'))
                {
                    folder = folder.Replace("$", "");
                }
                else
                {
                    // No use for folders without tokens yet
                    continue;
                }

                destPath = relativePath = Path.Combine(outputDir.FullName, folder);
                foreach (ScriptToken token in ScriptTokens)
                {
                    destPath = destPath.Replace(token.Name, token.Replace);
                }

                if (!Directory.Exists(relativePath))
                    continue;

                if (!Directory.Exists(destPath))
                {
                    Directory.Move(relativePath, destPath);
                }
                else
                {
                    // Move all files and delete the original directory
                    DirectoryInfo originalDir = Directory.CreateDirectory(relativePath);
                    IEnumerable<FileInfo> originalFiles = originalDir.EnumerateFiles("*", SearchOption.AllDirectories);

                    foreach (FileInfo file in originalFiles)
                    {
                        string newPath = Path.Combine(destPath, file.Name);

                        if (File.Exists(newPath))
                            File.Delete(newPath);

                        File.Move(file.FullName, newPath);
                    }

                    originalDir.Delete();
                }

                ConsoleWriter.WriteLine(String.Format("Processed folder \"{0}\" into \"{1}\"", folder, destPath));
            }

            ConsoleWriter.WriteDivider();

            if (Window.EntryData.Count > 0)
            {
                ConsoleWriter.WriteLine("Copying entries to mod directory...");

                foreach (MapEntry entry in Window.EntryData)
                {
                    if (entry.ContentDir == "" || Directory.Exists(entry.ContentDir) == false)
                    {
                        ConsoleWriter.WriteLine(String.Format("WARNING: Source directory \"{0}\" for {1} does not exist", entry.ContentDir, entry.EntryTitle));
                        continue;
                    }

                    string quotedTitle = "\"" + entry.EntryTitle + "\"";
                    ConsoleWriter.WriteLine("Checking content for entry " + quotedTitle + "...");

                    DirectoryInfo entryDir = Directory.CreateDirectory(entry.ContentDir);

                    // Search for common sorting mistakes
                    if (Window.SortFilesBox.IsChecked == true)
                    {
                        relativePath = Path.Combine(entryDir.FullName, "maps");
                        if (Directory.Exists(relativePath))
                        {
                            // Look in "maps" folder for VMFs and subfolders
                            DirectoryInfo mapsDir = Directory.CreateDirectory(relativePath);

                            // Create a mapsrc directory/make sure it exists
                            relativePath = Path.Combine(entryDir.FullName, "mapsrc");
                            DirectoryInfo mapSrcDir = Directory.CreateDirectory(relativePath);

                            IEnumerable<FileInfo> mapSrcFiles = mapsDir.EnumerateFiles("*.vmf", SearchOption.TopDirectoryOnly);
                            if (mapSrcFiles.Count() > 0)
                            {
                                // Move files to mapsrc
                                foreach (FileInfo file in mapSrcFiles)
                                {
                                    relativePath = Path.GetRelativePath(mapsDir.FullName, file.FullName);
                                    destPath = Path.Combine(mapSrcDir.FullName, relativePath);

                                    if (File.Exists(destPath))
                                        File.Delete(destPath);

                                    file.MoveTo(destPath);
                                }
                            }

                            // Clean up compilation garbage
                            string[] mapSrcGarbageExts = new[] { ".vmx", ".prt", ".lin", ".log" };
                            IEnumerable<FileInfo> mapSrcGarbage = mapsDir.EnumerateFiles("*", SearchOption.AllDirectories)
                                .Where(file => mapSrcGarbageExts.Any(file.Extension.Equals)).ToList();
                            foreach (FileInfo file in mapSrcGarbage)
                            {
                                // TODO: Delete
                                Console.WriteLine(file.FullName);
                            }

                            // Look in mapsrc too (for entries that already used it)
                            mapSrcGarbage = mapSrcDir.EnumerateFiles("*", SearchOption.AllDirectories)
                                .Where(file => mapSrcGarbageExts.Any(file.Extension.Equals)).ToList();
                            foreach (FileInfo file in mapSrcGarbage)
                            {
                                // TODO: Delete
                                Console.WriteLine(file.FullName);
                            }
                        }

                        // Move any root .txt files to readmes
                        IEnumerable<FileInfo> rootTextFiles = entryDir.EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly);
                        if (rootTextFiles.Count() > 0)
                        {
                            // Create a mapsrc directory/make sure it exists
                            relativePath = Path.Combine(entryDir.FullName, "readmes");
                            DirectoryInfo readmesDir = Directory.CreateDirectory(relativePath);

                            foreach (FileInfo file in rootTextFiles)
                            {
                                file.MoveTo(Path.Combine(readmesDir.FullName, file.Name + file.Extension));
                            }
                        }

                        // Move any root .tga files for thumbnails
                        IEnumerable<FileInfo> rootTGAFiles = entryDir.EnumerateFiles("*.tga", SearchOption.TopDirectoryOnly);
                        if (rootTGAFiles.Count() == 1)
                        {
                            // Put it in the thumbnail folder
                            destPath = Path.Combine(entryDir.FullName, "maps", Window.EventShortPlacementBox.Text, entry.StartingMap) + ".tga";
                            rootTGAFiles.ElementAt(0).MoveTo(destPath);
                        }
                        else if (rootTGAFiles.Count() > 1)
                        {
                            Console.WriteLine("WARNING: Entry has multiple .tga files at root");
                        }
                    }

                    // Don't add non-entries to VPKs or the "entries" folder, as they may contain important files which override regular entries
                    if (entry.Placement == CompPlacement.NonEntry)
                    {
                        IEnumerable<FileInfo> looseFileInfos = entryDir.EnumerateFiles("*", SearchOption.AllDirectories);
                        foreach (FileInfo file in looseFileInfos)
                        {
                            relativePath = Path.GetRelativePath(entryDir.FullName, file.FullName);
                            destPath = Path.Combine(outputDir.FullName, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            if (File.Exists(destPath))
                                File.Delete(destPath);

                            file.CopyTo(destPath);
                        }
                    }
                    else
                    {
                        string entriesDir = Path.Combine(outputDir.FullName, "entries");
                        Directory.CreateDirectory(entriesDir);

                        // Separate the "maps", "mapsrc", "media", "resource", and "custom" directories from this folder
                        string[] LooseDirectories = new string[] { "maps", "mapsrc", "media", "resource", "custom" };
                        DirectoryInfo looseDir = Directory.CreateDirectory(entryDir.FullName + "__loose");
                        foreach (string dir in LooseDirectories)
                        {
                            string fullDir = Path.Combine(entryDir.FullName, dir);
                            if (!Directory.Exists(fullDir))
                                continue;

                            string targetPath = Path.Combine(looseDir.FullName, dir);
                            DirectoryInfo targetDir = Directory.CreateDirectory(fullDir);
                            if (Directory.Exists(targetPath))
                            {
                                // Do a manual file replace
                                foreach (FileInfo file in targetDir.EnumerateFiles())
                                {
                                    string looseFilePath = Path.Combine(targetPath, file.Name);
                                    if (File.Exists(looseFilePath))
                                    {
                                        string backupPath = looseFilePath + ".old";
                                        file.Replace(looseFilePath, backupPath);
                                        if (File.Exists(backupPath))
                                            File.Delete(backupPath);
                                    }
                                    else
                                        file.MoveTo(looseFilePath);
                                }

                                targetDir.Delete();
                            }
                            else
                            {
                                // Just move it
                                targetDir.MoveTo(targetPath);
                            }
                        }

                        // Look for .bik files in other folders (some people put them elsewhere)
                        IEnumerable<FileInfo> bikFileInfos = entryDir.EnumerateFiles("*.bik", SearchOption.AllDirectories);
                        foreach (FileInfo file in bikFileInfos)
                        {
                            relativePath = Path.GetRelativePath(entryDir.FullName, file.FullName);
                            destPath = Path.Combine(looseDir.FullName, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            if (File.Exists(destPath))
                                File.Delete(destPath);

                            file.MoveTo(destPath);
                        }

                        if (Window.PackageIntoVPKBox.IsChecked == true)
                        {
                            // Package all entry content into VPKs and move them to the "entries" folder
                            if (entryDir.GetFiles().Length > 0 || entryDir.GetDirectories().Length > 0)
                            {
                                StartBinProcess("vpk.exe", "-M \"" + entryDir.FullName + "\"");

                                // Move the VPKs to the mod folder
                                List<FileInfo> vpklist = new List<FileInfo>( entryDir.Parent.GetFiles("*.vpk") );
                                foreach (FileInfo file in new List<FileInfo>(vpklist))
                                {
                                    // Ignore VPKs that aren't for this entry
                                    if (!file.Name.StartsWith(entryDir.Name))
                                        continue;

                                    Console.WriteLine(file.FullName);

                                    // File.MoveTo doesn't seem to work for some reason
                                    file.CopyTo(Path.Combine(entriesDir, file.Name));
                                    file.Delete();
                                }
                            }
                        }
                        else
                        {
                            // Copy all entry content into their own folders in the "entries" folder
                            IEnumerable<FileInfo> entryFileInfos = entryDir.EnumerateFiles("*", SearchOption.AllDirectories);
                            foreach (FileInfo file in entryFileInfos)
                            {
                                relativePath = Path.GetRelativePath(entryDir.Parent.FullName, file.FullName);
                                destPath = Path.Combine(entriesDir, relativePath);

                                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                                if (!File.Exists(destPath))
                                    file.CopyTo(destPath);
                            }
                        }

                        // Copy loose files to the main mod directory
                        IEnumerable<FileInfo> looseFileInfos = looseDir.EnumerateFiles("*", SearchOption.AllDirectories);
                        foreach (FileInfo file in looseFileInfos)
                        {
                            relativePath = Path.GetRelativePath(looseDir.FullName, file.FullName);
                            destPath = Path.Combine(outputDir.FullName, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            if (!File.Exists(destPath))
                                file.CopyTo(destPath);
                        }

                        // Move loose file directories back to the original folder and remove the "loose" placeholder folder
                        IEnumerable<DirectoryInfo> looseDirectoryInfos = looseDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
                        foreach (DirectoryInfo dir in looseDirectoryInfos)
                        {
                            relativePath = Path.GetRelativePath(looseDir.FullName, dir.FullName);
                            destPath = Path.Combine(entryDir.FullName, relativePath);

                            dir.MoveTo(destPath);
                        }
                        looseDir.Delete();
                    }

                    ConsoleWriter.WriteLine("Copied entry " + quotedTitle);
                }

                ConsoleWriter.WriteLine("Done copying entries");

                ConsoleWriter.WriteDivider();
            }

            ConsoleWriter.WriteLine("Done packaging mod!");

            Window.ToggleControls(true);
            ConsoleWriter.MakeClosable();
        }

        // ==========================================================================

        public static string[] FindSoundscriptFilesInDirectory(DirectoryInfo directory)
        {
            List<string> soundscriptList = new List<string>();
            IEnumerable<FileInfo> entryFileInfos = directory.EnumerateFiles("*.txt", SearchOption.AllDirectories);
            foreach (FileInfo file in entryFileInfos)
            {
                if (file.Name.StartsWith("level_sounds_") || file.Name.StartsWith("game_sounds_") || file.Name.StartsWith("npc_sounds_"))
                {
                    if (file.Name == "game_sounds_manifest.txt")
                        continue;

                    ConsoleWriter.WriteLine(String.Format("Found soundscript \"{0}\"", file.Name));
                    soundscriptList.Add(file.Name);
                }
            }

            return soundscriptList.ToArray();
        }

        public static string[] FindSoundscapeFilesInDirectory(DirectoryInfo directory)
        {
            List<string> soundscapeList = new List<string>();
            IEnumerable<FileInfo> entryFileInfos = directory.EnumerateFiles("*.txt", SearchOption.AllDirectories);
            foreach (FileInfo file in entryFileInfos)
            {
                if (file.Name.StartsWith("soundscapes_"))
                {
                    if (file.Name == "soundscapes_manifest.txt")
                        continue;

                    ConsoleWriter.WriteLine(String.Format("Found soundscape \"{0}\"", file.Name));
                    soundscapeList.Add(file.Name);
                }
            }

            return soundscapeList.ToArray();
        }

        public static bool MapHasCommentaryFile(string MapName, string directory)
        {
            string CommentaryFileName = MapName + "_commentary.txt";

            if (File.Exists( Path.Combine(directory, CommentaryFileName) ))
            {
                return true;
            }

            return false;
        }

        public static void PopulateBonusMapsScript()
        {
            BonusMapsScript = "";

            for (int i = 0; i < Window.EntryData.Count; i++)
            {
                MapEntry entry = Window.EntryData[i];
                if (entry.Placement == CompPlacement.NonEntry)
                    continue;

                // Generate a comment
                string title = String.Format("{0}_Map{1}_Title", Window.EventShortPlacementBox.Text, i);
                string comment = String.Format("{0}_Map{1}_Comment", Window.EventShortPlacementBox.Text, i);

                BonusMapsScript += String.Format("\"#{0}\"\n{{\n\t\"Map\"\t\"{1}\"\n\t\"Image\"\t\"./{1}.tga\"\n\t\"Comment\"\t\"#{2}\"\n",
                    title, entry.StartingMap, comment);

                // If the map has commentary, include commentary challenge
                if (MapHasCommentaryFile(entry.StartingMap, Path.Combine(entry.ContentDir, "maps")))
                {
                    BonusMapsScript += "\t\"challenges\"\n\t{{\n\t\t\"#Bonus_Map_Challenge_Commentary\"\n\t\t{{\n" +
                        "\t\t\t\"comment\"\t\"#Bonus_Map_Challenge_CommentaryComment\"\n" +
                        "\t\t\t\"type\"\t\"0\"\n" +
                        "\t\t\t\"bronze\"\t\"1\"\n" +
                        "\t\t\t\"silver\"\t\"2\"\n" +
                        "\t\t\t\"gold\"\t\"3\"\n" +
                        "\t\t}}\n\t}}";
                }

                BonusMapsScript += "}\n";
            }
        }
    }
}
