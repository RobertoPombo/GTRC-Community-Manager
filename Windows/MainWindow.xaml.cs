using Scripts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Threading;
using System.Windows.Documents;
using System.Globalization;

namespace GTRC_Community_Manager
{
    public partial class MainWindow : Window
    {
        public static string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string dataDirectory = AppDomain.CurrentDomain.BaseDirectory + "data\\";
        public static double screenWidth = SystemParameters.PrimaryScreenWidth;
        public static double screenHeight = SystemParameters.FullPrimaryScreenHeight + SystemParameters.WindowCaptionHeight;
        public static string DefText = "#!#";
        public ListWindow instanceListWindow;
        public ListWindow instanceListHiddenWindow;
        //private Storyboard AnimStoryBoard;

        public MainWindow()
        {
            if (!Directory.Exists(dataDirectory)) { Directory.CreateDirectory(dataDirectory); }
            SetCultureInfo();
            InitializeComponent();
            Width = screenWidth * 0.6;
            Height = screenHeight * 0.6;
            Left = ((screenWidth / 2) - (Width / 2)) * 1.9;
            Top = ((screenHeight / 2) - (Height / 2)) * 1.8;
            MinimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
            CloseButton.Click += (s, e) => CloseWindow();
        }

        public void CloseWindow()
        {
            if (instanceListWindow is not null) { instanceListWindow.Close(); }
            if (DatabaseVM.Instance is not null) { DatabaseVM.Instance.SaveFilter(); }
            ServerVM.ThreadStopAllAccServers();
            try { SQL.Connection.Close(); } catch { }
            if (SignInOutBot.Instance?._client is not null)
            {
                try { SignInOutBot.Instance.StopBot(); }
                catch { }
            }
            this.Close();
        }

        public static bool CheckExistingSqlThreads()
        {
            if (DatabaseVM.IsRunning) { return true; }
            if (Commands.IsRunning) { return true; }
            if (PreSeasonVM.IsRunningExportEntrylist) { return true; }
            if (PreSeasonVM.Instance?.IsRunningEntries ?? false) { return true; }
            if (ServerVM.IsRunning) { return true; }
            foreach (ServerM _server in ServerM.List) { if (_server.IsRunning) { return true; } }
            return false;
        }

        private void SetCultureInfo()
        {
            var newCulture = new CultureInfo(Thread.CurrentThread.CurrentUICulture.Name);
            newCulture.DateTimeFormat.FullDateTimePattern = "dd MM yyyy HH mm ss";

            CultureInfo.DefaultThreadCurrentCulture = newCulture;
            CultureInfo.DefaultThreadCurrentUICulture = newCulture;

            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        /*
        private void ValidatePathEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox Sender = sender as TextBox;
                ValidatePath(Sender.Text, Sender);
            }
        }

        private void LoadFile(object sender, RoutedEventArgs e)
        {
            ValidatePath(TextBoxPath.Text, TextBoxPath);
            CreatePath(TextBoxPath.Text, TextBoxPath);
            FileInfo[] jsonFiles = ListJsons(TextBoxPath.Text);

            ComboBoxPath.Items.Clear();

            foreach (FileInfo jsonFile in jsonFiles)
            {
                ComboBoxPath.Items.Add(jsonFile.Name);
            }

            if (jsonFiles.Length > 0)
            {
                ComboBoxPath.SelectedIndex = 0;
                ComboBoxPath.Focus();
            }
        }

        private void LiveCheckPath(object sender, RoutedEventArgs e)
        {
            TextBox Sender = sender as TextBox;
            CheckPath(Sender);
        }

        private void ValidatePath(string text, TextBox Sender)
        {
            Sender.Text = Basics.ValidatedPath(text);
            CheckPath(Sender);
        }

        private void CreatePath(string path, TextBox Sender)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            CheckPath(Sender);
        }

        private void CheckPath(TextBox Sender)
        {
            if (Sender.Text.Length == 0)
            {
                Sender.BorderBrush = (Brush)Application.Current.FindResource("color2");
            }
            else if (Directory.Exists(Sender.Text))
            {
                Sender.BorderBrush = (Brush)Application.Current.FindResource("color5");
            }
            else
            {
                Sender.BorderBrush = (Brush)Application.Current.FindResource("color6");
            }
        }

        private FileInfo[] ListJsons(string path)
        {
            FileInfo[] jsonFiles = new FileInfo[0];
            if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                jsonFiles = dirInfo.GetFiles("*.json");
            }

            return jsonFiles;
        }

        private void ShowList(object sender, RoutedEventArgs e)
        {
            if (instanceListWindow != null)
            {
                ButtonShow.Content = "Einblenden";
                instanceListWindow.Close();
                DestroyList();
                instanceListWindow = null;
            }
            else if (TextBoxPath.Text.Length > 0 && ComboBoxPath.Text.Length > 0)
            {
                instanceListWindow = new ListWindow();
                BuildList();
                instanceListWindow.AddTable();
                if (instanceListWindow != null)
                {
                    ButtonShow.Content = "Ausblenden";

                    foreach (var rowData in ResultsList.DataLabels)
                    {
                        foreach (var columnData in rowData)
                        {
                            columnData.Opacity = 0;
                        }
                    }

                    instanceListWindow.ShowWindow();

                    DoubleAnimation RectAnimDouble;
                    AnimStoryBoard = new Storyboard();
                    foreach (var columnData in ResultsList.DataLabels[0])
                    {
                        RectAnimDouble = new DoubleAnimation();
                        RectAnimDouble.From = 0;
                        RectAnimDouble.To = 1;
                        RectAnimDouble.Duration = new Duration(TimeSpan.FromSeconds(1));
                        RectAnimDouble.AutoReverse = false;
                        Storyboard.SetTarget(RectAnimDouble, columnData);
                        Storyboard.SetTargetProperty(RectAnimDouble, new PropertyPath(Rectangle.OpacityProperty));
                        AnimStoryBoard.Children.Add(RectAnimDouble);
                    }
                    AnimStoryBoard.Begin(this);
                }
            }
            ComboBoxPath.Focus();
        }

        private void DestroyList()
        {
            for (int resultsLineNr = 0; resultsLineNr < ResultsLine.ResultsLineList.Count; resultsLineNr++)
            {
                ResultsLine.ResultsLineList[resultsLineNr] = null;
            }
            ResultsLine.ResultsLineList.Clear();
        }

        private void BuildList()
        {
            string path = TextBoxPath.Text + ComboBoxPath.Text;
            try
            {
                int temp = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path, Encoding.Unicode)).sessionResult.leaderBoardLines.Count;
            }
            catch { ComboBoxPath.Items.Remove(ComboBoxPath.SelectedItem); ComboBoxPath.SelectedIndex = 0; return; }

            var resultsJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path, Encoding.Unicode));
            var leaderBoardLines = resultsJson.sessionResult.leaderBoardLines;
            for (int pos = 0; pos < leaderBoardLines.Count; pos++)
            {
                ResultsLine resultsLine = new ResultsLine();
                resultsLine.Position = pos + 1;
                if (leaderBoardLines[pos].timing is JObject)
                {
                    var _lapCount = leaderBoardLines[pos].timing.lapCount;
                    if (_lapCount is JValue) { if (Int32.TryParse(_lapCount.ToString(), out int lapCount)) { resultsLine.Laps = lapCount; } }
                    if (leaderBoardLines[pos].timing.totalTime is JValue) { resultsLine.Time = leaderBoardLines[pos].timing.totalTime.ToString(); }
                    if (leaderBoardLines[pos].timing.bestLap is JValue) { resultsLine.BestLap = leaderBoardLines[pos].timing.bestLap.ToString(); }
                }
                if (leaderBoardLines[pos].car is JObject)
                {
                    List<string> steamIDs = new List<string>();
                    int countDrivers = 0;
                    if (leaderBoardLines[pos].car.drivers is IList) { countDrivers = leaderBoardLines[pos].car.drivers.Count; }
                    List<int> driverIDs = new List<int>();
                    for (int driverNr = 0; driverNr < countDrivers; driverNr++)
                    {
                        string steamID = "";
                        if (leaderBoardLines[pos].car.drivers[driverNr] is JObject && leaderBoardLines[pos].car.drivers[driverNr].playerId is JValue) { steamID = leaderBoardLines[pos].car.drivers[driverNr].playerId; }
                        steamIDs.Add(steamID);
                        Driver driver = Driver.getDriverBySteamId(steamID);
                        if (driver == null)
                        {
                            driver = new Driver { SteamId = steamID };
                            if (leaderBoardLines[pos].car.drivers[driverNr] is JObject)
                            {
                                if (leaderBoardLines[pos].car.drivers[driverNr].firstName is JValue) { driver.FirstName = leaderBoardLines[pos].car.drivers[driverNr].firstName; }
                                if (leaderBoardLines[pos].car.drivers[driverNr].lastName is JValue) { driver.LastName = leaderBoardLines[pos].car.drivers[driverNr].lastName; }
                                if (leaderBoardLines[pos].car.drivers[driverNr].shortName is JValue) { driver.Name3Digits = leaderBoardLines[pos].car.drivers[driverNr].shortName; }
                            }
                        }
                        steamIDs[driverNr] = driver.SteamId;
                        driverIDs.Add(driver.DriverID);
                    }
                    Entry entry = Entry.getEntryBySteamIDs(steamIDs);
                    if (entry == null)
                    {
                        entry = new Entry { DriverIDs = driverIDs };
                        var _raceNumber = leaderBoardLines[pos].car.raceNumber;
                        if (_raceNumber is JValue) { if (Int32.TryParse(_raceNumber.ToString(), out int raceNumber)) { entry.RaceNumber = raceNumber; } }
                        var _accID = leaderBoardLines[pos].car.carModel;
                        if (_accID is JValue)
                        {
                            if (Int32.TryParse(_accID.ToString(), out int accID))
                            {
                                if (Car.getCarByAccId(accID) == null)
                                {
                                    string category = null;
                                    if (leaderBoardLines[pos].car.carGroup is JValue) { category = leaderBoardLines[pos].car.carGroup; };
                                    Car car = new Car { AccId = accID, Category = category };
                                    entry.CarID = car.CarID;
                                }
                                else { entry.CarID = Car.getCarByAccId(accID).CarID; }
                            }
                        }
                        var _cupCategory = leaderBoardLines[pos].car.cupCategory;
                        if (_cupCategory is JValue) { if (Int32.TryParse(_cupCategory.ToString(), out int cupCategory)) { entry.CupCategory = cupCategory; } }
                        var _ballast = leaderBoardLines[pos].car.ballastKg;
                        if (_ballast is JValue) { if (Int32.TryParse(_ballast.ToString(), out int ballast)) { entry.Ballast = ballast; } }
                        var _restrictor = leaderBoardLines[pos].car.restrictor;
                        if (_restrictor is JValue) { if (Int32.TryParse(_restrictor.ToString(), out int restrictor)) { entry.Restrictor = restrictor; } }
                    }
                    ResultsLine.ResultsLineList[pos].EntryID = entry.EntryID;
                    var _accIDRes = leaderBoardLines[pos].car.carModel;
                    if (_accIDRes is JValue)
                    {
                        if (Int32.TryParse(_accIDRes.ToString(), out int accID))
                        {
                            if (Car.getCarByAccId(accID) == null)
                            {
                                string category = null;
                                if (leaderBoardLines[pos].car.carGroup is JValue) { category = leaderBoardLines[pos].car.carGroup; };
                                Car car = new Car { AccId = accID, Category = category };
                                ResultsLine.ResultsLineList[pos].CarID = car.CarID;
                            }
                            else { ResultsLine.ResultsLineList[pos].CarID = Car.getCarByAccId(accID).CarID; }
                        }
                    }
                    var _ballastRes = leaderBoardLines[pos].car.ballastKg;
                    if (_ballastRes is JValue) { if (Int32.TryParse(_ballastRes.ToString(), out int ballast)) { ResultsLine.ResultsLineList[pos].Ballast = ballast; } }
                    var _restrictorRes = leaderBoardLines[pos].car.restrictor;
                    if (_restrictorRes is JValue) { if (Int32.TryParse(_restrictorRes.ToString(), out int restrictor)) { ResultsLine.ResultsLineList[pos].Restrictor = restrictor; } }
                }
                if (leaderBoardLines[pos].currentDriver is IList)
                {
                    string currentSteamID = "";
                    if (leaderBoardLines[pos].currentDriver.playerId is JValue) { currentSteamID = leaderBoardLines[pos].currentDriver.playerId; }
                    List<int> _driverIDs = Entry.EntryListTemp[ResultsLine.ResultsLineList[pos].EntryID].DriverIDs;
                    for (int driverNr = 0; driverNr < _driverIDs.Count; driverNr++)
                    {
                        if (Driver.DriverListTemp[_driverIDs[driverNr]].SteamId == currentSteamID)
                        {
                            ResultsLine.ResultsLineList[pos].CurrentDriverID = driverNr; break;
                        }
                    }
                }
            }
        }

        private void PrintList(object sender, RoutedEventArgs e)
        {
            ValidatePath(TextBoxPrintPath.Text, TextBoxPrintPath);
            CreatePath(TextBoxPrintPath.Text, TextBoxPrintPath);

            if (TextBoxPath.Text.Length > 0 && ComboBoxPath.Text.Length > 0)
            {
                DestroyList();
                instanceListHiddenWindow = new ListWindow();
                BuildList();
                instanceListHiddenWindow.AddTable();
                instanceListHiddenWindow.ShowHiddenWindow();

                if (instanceListHiddenWindow != null)
                {
                    Visual target = instanceListHiddenWindow.ResultsGrid;
                    Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
                    Visual target2 = instanceListHiddenWindow.ResultsScrollArea;
                    Rect bounds2 = VisualTreeHelper.GetDescendantBounds(target2);
                    if ((Int32)bounds2.Width > 0)
                    {
                        RenderTargetBitmap renderTarget = new RenderTargetBitmap((Int32)bounds2.Width, (Int32)bounds.Height, 96, 96, PixelFormats.Pbgra32);
                        DrawingVisual visual = new DrawingVisual();
                        using (DrawingContext context = visual.RenderOpen())
                        {
                            VisualBrush visualBrush = new VisualBrush(target);
                            context.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
                        }
                        renderTarget.Render(visual);
                        PngBitmapEncoder bitmapEncoder = new PngBitmapEncoder();
                        bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
                        using (Stream stm = File.Create(TextBoxPrintPath.Text + "/Results.png"))
                        {
                            bitmapEncoder.Save(stm);
                        }
                    }
                }

                instanceListHiddenWindow.Close();
                instanceListHiddenWindow = null;
            }
        }
        */
    }
}
