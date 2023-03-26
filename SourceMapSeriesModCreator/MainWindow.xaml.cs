using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Xml;
using System.Globalization;
using System.ComponentModel;
using Microsoft.Win32;

namespace SourceMapSeriesModCreator
{
    public enum CompPlacement
    {
        // Copied folders which aren't actually entries.
        [Description("Non-Entry")]
        NonEntry = -2,

        Bonus = -1,

        None,

        [Description("1st")]
        First,
        [Description("2nd")]
        Second,
        [Description("3rd")]
        Third
    };

    public class MapEntry
    {
        public MapEntry(string title = "", string names = "", CompPlacement compplacement = CompPlacement.None, string comment = "", string dir = "", string startmap = "")
        {
            EntryTitle = title; EntrantNames = names; Placement = compplacement; EntryComment = comment; ContentDir = dir; StartingMap = startmap;
        }

        public string EntryTitle { get; set; }
        public string EntrantNames { get; set; }
        public CompPlacement Placement { get; set; }
        public string EntryComment { get; set; }
        public string ContentDir { get; set; }
        public string StartingMap { get; set; }
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<MapEntry> EntryData = new ObservableCollection<MapEntry>();

        public MainWindow()
        {
            InitializeComponent();

            ModAssembler.Window = this;
            ContentCheck.Window = this;

            // Set default values
            BinDirectoryBox.Text = @"C:\Program Files (x86)\Steam\SteamApps\common\Source SDK Base 2013 Singleplayer\bin";
            SourceContentDirectoryBox.Text = @"E:\Map Labs\Mod Template\File Lists";

            TemplateDirectoryBox.Text = @"E:\Map Labs\Mod Template\tool-tokenized-current";
            OutputDirectoryBox.Text = @"C:\Program Files (x86)\Steam\SteamApps\sourcemods\maplabscompetition";

            MiscDirectoryBox.Text = @"E:\Map Labs\Mod Template\ml01_misc";
            BGMapDirectoryBox.Text = @"E:\Map Labs\Mod Template\ml01_bg";

            EventTitleBox.Text = "maplabscompetition";
            EventCommentBox.Text = "For this competition, we asked entrants to...";
            BackgroundMapBox.Text = "template_background01";

            EventLongPlacementBox.Text = "Map Labs #1";
            EventShortPlacementBox.Text = "ml01";

            foreach (DataGridColumn column in DG1.Columns)
            {
                if (column.Header.ToString() == "Placement")
                {
                    ((DataGridComboBoxColumn)column).ItemsSource = Enum.GetValues(typeof(CompPlacement));
                }
            }

            DG1.DataContext = EntryData;

            //EntryData.Add( new MapEntry("Hot Soft Iron", "chickensushi", CompPlacement.None, "Example") );
            //EntryData.Add( new MapEntry("Light Runner 4", "2upE", CompPlacement.None, "Example") );
            //EntryData.Add( new MapEntry("Average Title", "casstle", CompPlacement.None, "Example") );
            //EntryData.Add( new MapEntry("Night of Blue Mail", "Axolossil", CompPlacement.None, "Example") );

            //EntryData.Add( new MapEntry("Test Entry 1", "Someone") );
            //EntryData.Add( new MapEntry("test entry 2", "Somebody") );

            // ------------------------------

            BinDirectoryBox.TextChanged += Generic_TextChanged;
            SourceContentDirectoryBox.TextChanged += Generic_TextChanged;

            TemplateDirectoryBox.TextChanged += Generic_TextChanged;
            OutputDirectoryBox.TextChanged += Generic_TextChanged;

            MiscDirectoryBox.TextChanged += Generic_TextChanged;
            BGMapDirectoryBox.TextChanged += Generic_TextChanged;

            EventTitleBox.TextChanged += Generic_TextChanged;
            EventCommentBox.TextChanged += Generic_TextChanged;
            BackgroundMapBox.TextChanged += Generic_TextChanged;
            BackgroundMap2Box.TextChanged += Generic_TextChanged;

            EventLongPlacementBox.TextChanged += Generic_TextChanged;
            EventShortPlacementBox.TextChanged += Generic_TextChanged;

            PackageIntoVPKBox.Checked += Generic_CheckBoxChanged;
            SortFilesBox.Checked += Generic_CheckBoxChanged;

            DG1.CurrentCellChanged += new EventHandler<EventArgs>(Generic_DataGridChanged);
        }

        private void EntryListAdd_Click(object sender, RoutedEventArgs e)
        {
            EntryData.Add(new MapEntry());
            SpoilSave();
        }

        private void EntryListPrint_Click(object sender, RoutedEventArgs e)
        {
            ConsoleWriter.Setup();

            ToggleControls(false);

            foreach (MapEntry entry in EntryData)
            {
                ConsoleWriter.WriteLine(String.Format("ENTRY {0}: Entrant names {1} - Comment {2} - Content Dir {3} - Starting Map {4}", entry.EntryTitle, entry.EntrantNames, entry.EntryComment, entry.ContentDir, entry.StartingMap));
            }

            ToggleControls(true);
            ConsoleWriter.MakeClosable();
        }

        private void EntryListShuffle_Click(object sender, RoutedEventArgs e)
        {
            Random rng = new Random();
            int n = EntryData.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                MapEntry value = EntryData[k];
                EntryData[k] = EntryData[n];
                EntryData[n] = value;
            }
        }

        public void ToggleControls(bool toggle)
        {
            // false = Now doing something, disable buttons
            // true = Finished doing something
            BinDirectoryBox.IsEnabled = toggle;
            SourceContentDirectoryBox.IsEnabled = toggle;

            TemplateDirectoryBox.IsEnabled = toggle;
            OutputDirectoryBox.IsEnabled = toggle;

            EventTitleBox.IsEnabled = toggle;
            EventCommentBox.IsEnabled = toggle;
            BackgroundMapBox.IsEnabled = toggle;
            BackgroundMap2Box.IsEnabled = toggle;

            // ------------------------------

            DG1.IsEnabled = toggle;

            // ------------------------------

            EntryListAdd.IsEnabled = toggle;
            //EntryListPrint.IsEnabled = toggle;
            EntryListShuffle.IsEnabled = toggle;
            EntryListSave.IsEnabled = toggle;
            EntryListLoad.IsEnabled = toggle;

            ContentCheckButton.IsEnabled = toggle;
            SourceContentMakeFileList.IsEnabled = toggle;
            CreateModButton.IsEnabled = toggle;
        }

        // -----------------------------------------------------------------------------

        private string LastFileName = null;
        private Button LastSaveButton = null;

        private bool SaveSpoiled = false;

        private void SaveEntries(string fileName)
        {
            // Save XML containing the data
            XmlDocument xml = new XmlDocument();
            XmlElement xmlRoot = xml.CreateElement("MapSeriesModCreatorManifest");
            xml.AppendChild(xmlRoot);

            XmlNode inputdata = xml.CreateElement("InputData");

            List<Control> SaveBoxes = new List<Control>
            {
                BinDirectoryBox,
                SourceContentDirectoryBox,
                TemplateDirectoryBox,
                OutputDirectoryBox,
                EventTitleBox,
                EventCommentBox,
                BackgroundMapBox,
                BackgroundMap2Box,
                EventLongPlacementBox,
                EventShortPlacementBox,
                PackageIntoVPKBox,
                SortFilesBox,
                MiscDirectoryBox,
                BGMapDirectoryBox
            };

            foreach (Control box in SaveBoxes)
            {
                XmlNode boxNode = xml.CreateElement(box.Name);

                XmlAttribute boxParam = xml.CreateAttribute("Param");

                if (box is TextBox)
                    boxParam.Value = (box as TextBox).Text;
                else if (box is CheckBox)
                    boxParam.Value = (box as CheckBox).IsChecked == true ? "1" : "0";

                boxNode.Attributes.Append( boxParam );

                inputdata.AppendChild(boxNode);
            }

            // Append the column widths
            XmlNode dataNode = xml.CreateElement("DataGrid");

            for (int i = 0; i < DG1.Columns.Count; i++)
            {
                XmlAttribute widthParam = xml.CreateAttribute("Column" + i);
                widthParam.Value = DG1.Columns[i].ActualWidth.ToString();
                dataNode.Attributes.Append(widthParam);
            }

            inputdata.AppendChild(dataNode);

            xmlRoot.AppendChild(inputdata);

            // Add entries
            XmlNode entries = xml.CreateElement("Entries");

            foreach (MapEntry entry in EntryData)
            {
                XmlNode entryNode = xml.CreateElement("Entry");

                List<KeyValuePair<string, string>> entryParams = new List<KeyValuePair<string, string>>();
                entryParams.Add( new KeyValuePair<string, string>( "EntryTitle", entry.EntryTitle ) );
                entryParams.Add( new KeyValuePair<string, string>( "EntrantNames", entry.EntrantNames ) );
                //entryParams.Add( new KeyValuePair<string, string>( "Bonus", entry.Bonus.ToString() ) );
                entryParams.Add( new KeyValuePair<string, string>( "Placement", entry.Placement.ToString() ) );
                entryParams.Add( new KeyValuePair<string, string>( "EntryComment", entry.EntryComment ) );
                entryParams.Add( new KeyValuePair<string, string>( "ContentDir", entry.ContentDir ) );
                entryParams.Add( new KeyValuePair<string, string>( "StartingMap", entry.StartingMap ) );

                foreach (KeyValuePair<string, string> kvpair in entryParams)
                {
                    XmlAttribute boxParam = xml.CreateAttribute(kvpair.Key);
                    boxParam.Value = kvpair.Value;
                    entryNode.Attributes.Append(boxParam);
                }

                entries.AppendChild(entryNode);
            }

            xmlRoot.AppendChild(entries);

            xml.Save(fileName);
        }

        private /*async*/ void ButtonSaveAnimation(Button button)
        {
            //object buttonContent = button.Content;

            // Disable both buttons, change the one that was used to "Saved!"
            EntryListSave.IsEnabled = false;
            EntryListSaveAs.IsEnabled = false;
            button.Content = "Saved!";
            button.FontStyle = FontStyles.Italic;

            LastSaveButton = button;

            /*await Task.Delay(TimeSpan.FromSeconds(3));

            EntryListSave.IsEnabled = true;
            EntryListSaveAs.IsEnabled = true;
            button.Content = buttonContent;
            button.FontStyle = FontStyles.Normal;*/

            // UNDONE: Green flash

            //object buttonContent = button.Content;
            //Brush buttonBG = button.Background;
            //
            //button.Content = "Saved!";
            //button.Background = Brushes.LightGreen;
            //
            //await Task.Delay(TimeSpan.FromSeconds(0.25));
            //button.Background = buttonBG;
            //await Task.Delay(TimeSpan.FromSeconds(0.25));
            //button.Background = Brushes.LightGreen;
            //await Task.Delay(TimeSpan.FromSeconds(0.25));
            //button.Background = buttonBG;
            //await Task.Delay(TimeSpan.FromSeconds(0.25));
            //
            //button.Content = buttonContent;
        }

        private void EntryListSave_Click(object sender, RoutedEventArgs e)
        {
            if (LastFileName == null)
            {
                EntryListSaveAs_Click(sender, e);
                return;
            }

            SaveEntries(LastFileName);

            ButtonSaveAnimation(EntryListSave);
        }

        private void EntryListSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML files (*.xml)|*.xml";
            if (saveFileDialog.ShowDialog() == false)
                return;

            SaveEntries(saveFileDialog.FileName);
            LastFileName = saveFileDialog.FileName;

            ButtonSaveAnimation(EntryListSaveAs);
        }

        private string GetXMLAttribute(XmlNode node, string name, string defaultVal = "")
        {
            XmlAttribute attribute = node.Attributes[name];
            return attribute == null ? defaultVal : attribute.Value;
        }

        private void EntryListLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML files (*.xml)|*.xml";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == false)
                return;

            // Clear the current entries
            EntryData.Clear();

            XmlDocument xml = new XmlDocument();
            xml.Load(openFileDialog.FileName);

            XmlNode inputdata = xml.GetElementsByTagName("InputData").Item(0);

            // Parse base input data
            for (int i = 0; i < inputdata.ChildNodes.Count; i++)
            {
                XmlNode dataNode = inputdata.ChildNodes[i];

                switch (dataNode.Name)
                {
                    case "BinDirectoryBox":             BinDirectoryBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "SourceContentDirectoryBox":   SourceContentDirectoryBox.Text = GetXMLAttribute(dataNode, "Param"); break;

                    case "TemplateDirectoryBox":    TemplateDirectoryBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "OutputDirectoryBox":      OutputDirectoryBox.Text = GetXMLAttribute(dataNode, "Param"); break;

                    case "EventTitleBox":           EventTitleBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "EventCommentBox":         EventCommentBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "BackgroundMapBox":        BackgroundMapBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "BackgroundMap2Box":       BackgroundMap2Box.Text = GetXMLAttribute(dataNode, "Param"); break;

                    case "EventLongPlacementBox":       EventLongPlacementBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "EventShortPlacementBox":      EventShortPlacementBox.Text = GetXMLAttribute(dataNode, "Param"); break;

                    case "PackageIntoVPKBox":       PackageIntoVPKBox.IsChecked = GetXMLAttribute(dataNode, "Param") == "1"; break;
                    case "SortFilesBox":       SortFilesBox.IsChecked = GetXMLAttribute(dataNode, "Param") == "1"; break;

                    case "MiscDirectoryBox":    MiscDirectoryBox.Text = GetXMLAttribute(dataNode, "Param"); break;
                    case "BGMapDirectoryBox":   BGMapDirectoryBox.Text = GetXMLAttribute(dataNode, "Param"); break;

                    case "DataGrid":
                        {
                            for (int i2 = 0; i2 < DG1.Columns.Count; i2++)
                            {
                                XmlAttribute attribute = dataNode.Attributes["Column" + i2];
                                if (attribute != null)
                                    DG1.Columns[i2].Width = double.Parse(attribute.Value);
                            }
                        } break;
                }
            }

            XmlNode entries = xml.GetElementsByTagName("Entries").Item(0);

            // A list of entries which aren't actually entries (for background maps and other content which should get merged)
            List<MapEntry> nonEntries = new List<MapEntry>();

            // Parse entries
            for (int i = 0; i < entries.ChildNodes.Count; i++)
            {
                XmlNode entryNode = entries.ChildNodes[i];

                MapEntry entry = new MapEntry
                {
                    EntryTitle = GetXMLAttribute(entryNode, "EntryTitle"),
                    EntrantNames = GetXMLAttribute(entryNode, "EntrantNames"),
                    //Bonus = GetXMLAttribute(entryNode, "Bonus") == "True",
                    Placement = (CompPlacement)Enum.Parse( typeof(CompPlacement), GetXMLAttribute(entryNode, "Placement", "None") ),
                    EntryComment = GetXMLAttribute(entryNode, "EntryComment"),
                    ContentDir = GetXMLAttribute(entryNode, "ContentDir"),
                    StartingMap = GetXMLAttribute(entryNode, "StartingMap")
                };

                // Legacy support
                if (GetXMLAttribute(entryNode, "Bonus") == "True")
                    entry.Placement = CompPlacement.Bonus;

                if (entry.Placement == CompPlacement.NonEntry)
                    nonEntries.Add(entry);
                else
                    EntryData.Add(entry);
            }

            // Make sure all non-entries are at the end
            // (preserves correct entry ordering)
            foreach (MapEntry entry in nonEntries)
            {
                EntryData.Add(entry);
            }

            LastFileName = openFileDialog.FileName;
            SaveSpoiled = false;
        }

        // -----------------------------------------------------------------------------

        private void SpoilSave()
        {
            SaveSpoiled = true;

            EntryListSave.IsEnabled = true;
            EntryListSaveAs.IsEnabled = true;

            if (LastSaveButton != null)
            {
                LastSaveButton.FontStyle = FontStyles.Normal;
                if (LastSaveButton == EntryListSave)
                    LastSaveButton.Content = "Save";
                else
                    LastSaveButton.Content = "Save As";
                LastSaveButton = null;
            }
        }

        private void Generic_TextChanged(object sender, TextChangedEventArgs e)
        {
            SpoilSave();
        }

        private void Generic_CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            SpoilSave();
        }

        private void Generic_DataGridChanged(object sender, EventArgs e)
        {
            SpoilSave();
        }

        private void ConfirmExitProgram(object sender, CancelEventArgs e)
        {
            if (SaveSpoiled)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to save your changes before exiting?",
                    "Save", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    if (LastFileName == null)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "XML files (*.xml)|*.xml";
                        if (saveFileDialog.ShowDialog() == false)
                        {
                            e.Cancel = true;
                            return;
                        }

                        LastFileName = saveFileDialog.FileName;
                    }

                    SaveEntries(LastFileName);
                }
            }
        }

        // -----------------------------------------------------------------------------

        private bool RanContentCheck = false;

        private void ContentCheck_Click(object sender, RoutedEventArgs e)
        {
            RanContentCheck = true;

            // Add misc directory and BG map directory to the entry list
            MapEntry miscDir = null;
            if (!String.IsNullOrEmpty(MiscDirectoryBox.Text))
            {
                miscDir = new MapEntry(EventShortPlacementBox.Text + " Misc", "", CompPlacement.NonEntry, "", MiscDirectoryBox.Text);
                EntryData.Add(miscDir);
            }

            MapEntry bgDir = null;
            if (!String.IsNullOrEmpty(BGMapDirectoryBox.Text))
            {
                bgDir = new MapEntry(EventShortPlacementBox.Text + " BG", "", CompPlacement.NonEntry, "", BGMapDirectoryBox.Text);
                EntryData.Add(bgDir);
            }

            ContentCheck.RunContentCheck();

            if (miscDir != null)
                EntryData.Remove(miscDir);
            if (bgDir != null)
                EntryData.Remove(bgDir);
        }

        private void SourceContentMakeFileList_Click(object sender, RoutedEventArgs e)
        {
            ContentCheck.GenerateFileList();
        }

        private void CreateMod_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSpoiled)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to save your changes before proceeding?",
                    "Save", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    EntryListSave_Click(sender, e);
                }
            }

            if (!RanContentCheck)
            {
                MessageBoxResult result = MessageBox.Show("You have not yet checked for conflicting content using the \"Check for conflicting content\" button. Are you sure you want to continue?",
                    "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                    return;
            }

            if (PackageIntoVPKBox.IsChecked == true && System.IO.Directory.Exists(BinDirectoryBox.Text) == false)
            {
                MessageBox.Show(String.Format("\"{0}\" is not a valid bin directory. A valid bin directory is needed to package entries into VPKs.", BinDirectoryBox.Text),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            if (System.IO.Directory.Exists(OutputDirectoryBox.Text))
            {
                MessageBox.Show(String.Format("\"{0}\" already exists. If this is the correct directory, please delete it before proceeding.", OutputDirectoryBox.Text),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;

                //MessageBoxResult result = MessageBox.Show(String.Format("\"{0}\" already exists. Would you like to delete it and replace it with the mod?", OutputDirectoryBox.Text),
                //    "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.OK);
                //if (result == MessageBoxResult.No)
                //    return;
            }

            // Add misc directory and BG map directory to the entry list
            MapEntry miscDir = null;
            if (!String.IsNullOrEmpty(MiscDirectoryBox.Text))
            {
                miscDir = new MapEntry(EventShortPlacementBox.Text + " Misc", "", CompPlacement.NonEntry, "", MiscDirectoryBox.Text);
                EntryData.Add(miscDir);
            }

            MapEntry bgDir = null;
            if (!String.IsNullOrEmpty(BGMapDirectoryBox.Text))
            {
                bgDir = new MapEntry(EventShortPlacementBox.Text + " BG", "", CompPlacement.NonEntry, "", BGMapDirectoryBox.Text);
                EntryData.Add(bgDir);
            }

            ModAssembler.CreateMod();

            if (miscDir != null)
                EntryData.Remove(miscDir);
            if (bgDir != null)
                EntryData.Remove(bgDir);
        }

        private void MakePlaceholderThumbnails_Click(object sender, RoutedEventArgs e)
        {
        }
    }



    // ConsoleWriter - A simple wrapper originally created for the Mapbase Multi-Tool
    public static class ConsoleWriter
    {
        [DllImport("kernel32")]
        static extern bool AllocConsole();

        [DllImport("kernel32")]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleTitle(string lpConsoleTitle);

        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // Used for when we're waiting for the console to close
        //private static Task CloseTask;
        //private static CancellationTokenSource CancelTokenSrc;

        private static bool ConsoleAlloc = false;

        public static bool Setup(string windowName = "Console Output")
        {
            if (!ConsoleAlloc)
                ConsoleAlloc = AllocConsole();

            if (!ConsoleAlloc)
            {
                ConsoleAlloc = AllocConsole();

                // Reset the I/O
                //TextReader reader = new StreamReader(Console.OpenStandardInput()) { };
                //Console.SetIn(reader);
                //TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                //Console.SetOut(writer);

                if (!ConsoleAlloc)
                    return false;
            }

            SetConsoleTitle(windowName);

            IntPtr ConsoleWindow = GetConsoleWindow();
            DeleteMenu(GetSystemMenu(ConsoleWindow, false), SC_CLOSE, MF_BYCOMMAND);
            ShowWindow(ConsoleWindow, 5);
            SetForegroundWindow(ConsoleWindow);

            return true;
        }

        public static bool Close()
        {
            //if (CloseTask != null && CloseTask.Status == TaskStatus.Running)
            //    CancelTokenSrc.Cancel();

            //Console.In.Close();
            //Console.Out.Close();

            if (ConsoleAlloc)
            {
                Console.Clear();
                return ShowWindow(GetConsoleWindow(), 0);
            }

            return true;
            //return FreeConsole();
        }

        public /*async*/ static void MakeClosable()
        {
            ConsoleKeyInfo cki;
            Console.Write("\nPress any key to close this console...");
            cki = Console.ReadKey(true);

            /*
            Console.Write("\nPress any key to close this console...");

            //CloseTask = Task.Run(() => Console.ReadKey());

            CancelTokenSrc = new CancellationTokenSource();
            CancellationToken CancelToken = CancelTokenSrc.Token;
            CloseTask = Task.Run(() =>
            {
                while (Console.KeyAvailable == false)
                {
                    Thread.Sleep(50);
                    CancelToken.ThrowIfCancellationRequested();
                }

            }, CancelToken);

            try { await CloseTask; }
            catch (OperationCanceledException)
            {
                return;
            }

            CancelTokenSrc.Dispose();
            CloseTask.Dispose();
            */

            Close();
        }

        public static void Write(string text)
        {
            Console.Write(text);
        }

        public static void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        public static void WriteDivider()
        {
            Console.WriteLine("===============================================");
        }
    }
}
