using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using GTRCLeagueManager.Database;
using Newtonsoft.Json;

namespace GTRCLeagueManager
{
    public class ServerVM : ObservableObject
    {
        public static ServerVM Instance;
        private static readonly string PathSettings = MainWindow.dataDirectory + "config server.json";
        public static readonly Brush StateOff = (Brush)Application.Current.FindResource("color1");
        public static readonly Brush StateOn = (Brush)Application.Current.FindResource("color3");
        public static readonly Brush StateWait = (Brush)Application.Current.FindResource("color6");
        public static readonly Brush StateRun = (Brush)Application.Current.FindResource("color5");
        public static readonly Brush StateRunWait = (Brush)Application.Current.FindResource("color4");
        public static readonly string NoPath = MainWindow.currentDirectory;
        public static readonly string ForbiddenPath = MainWindow.dataDirectory;
        [JsonIgnore] public BackgroundWorker BackgroundWorkerRestartServer = new BackgroundWorker() { WorkerSupportsCancellation = true };
        public static readonly Random random = new Random();

        private bool stateAutoServerRestart = false;
        private int serverRestartTime = 0;
        private int serverRestartRemTime = 0;

        public ServerVM()
        {
            Instance = this;
            AddServerCmd = new UICmd((o) => AddDefServer());
            DelServerCmd = new UICmd((o) => DelServer(o));
            RestoreSettingsCmd = new UICmd((o) => RestoreSettings());
            SaveSettingsCmd = new UICmd((o) => SaveSettings());
            ReloadAllResultsJsonCmd = new UICmd((o) => ReloadAllResultsJson(o));
            if (!File.Exists(PathSettings)) { SaveSettings(); }
            RestoreSettings();
            BackgroundWorkerRestartServer.DoWork += InfiniteLoopRestartServer;
            BackgroundWorkerRestartServer.RunWorkerAsync();
        }

        public bool StateAutoServerRestart
        {
            get { return stateAutoServerRestart; }
            set { stateAutoServerRestart = value; UpdateServerRestartRemTime(); this.RaisePropertyChanged(); }
        }

        public int ServerRestartTime
        {
            get { return serverRestartTime; }
            set
            {
                if (value < 0) { value = 0; }
                else if (value > 23) { value = 23; }
                serverRestartTime = value;
                UpdateServerRestartRemTime();
                this.RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public string ServerRestartRemTime
        {
            get
            {
                if (!StateAutoServerRestart) { return "∞"; }
                else if (serverRestartRemTime > 7200) { return ((int)Math.Ceiling((double)serverRestartRemTime / (60 * 60))).ToString() + " h"; }
                else if (serverRestartRemTime > 120) { return ((int)Math.Ceiling((double)serverRestartRemTime / 60)).ToString() + " min"; }
                else { return serverRestartRemTime.ToString() + " sec"; }
            }
            set { this.RaisePropertyChanged(); }
        }

        public ObservableCollection<Server> ListServer
        {
            get { return Server.List; }
            set { }
        }

        public void UpdateServerRestartRemTime()
        {
            int currentTime = ((DateTime.Now.Hour * 60) + DateTime.Now.Minute) * 60 + DateTime.Now.Second;
            int restartTime = serverRestartTime * 60 * 60;
            if (restartTime < currentTime) { restartTime += 24 * 60 * 60; }
            serverRestartRemTime = restartTime - currentTime;
            ServerRestartRemTime = "?";
        }

        public void InfiniteLoopRestartServer(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (StateAutoServerRestart)
                {
                    UpdateServerRestartRemTime();
                    if (serverRestartRemTime < 2)
                    {
                        TriggerRestartServer();
                        Thread.Sleep(2000);
                    }
                }
            }
        }

        public void ThreadRestartServer()
        {
            foreach (Server _server in Server.List)
            {
                if (_server.SetOnline) { _server.SetOnline = false; Thread.Sleep(500); _server.SetOnline = true; Thread.Sleep(500); }
            }
        }

        public void TriggerRestartServer()
        {
            UpdateEntrylists();
            UpdateBoPs();
            new Thread(ThreadRestartServer).Start();
        }

        public void EventNewResultsJson(object source, FileSystemEventArgs e)
        {
            foreach (Server _server in Server.List)
            {
                if (_server.Type == ServerTypeEnum.PreQuali && Basics.PathIsParentOf(NoPath, _server.PathResults, e.FullPath))
                {
                    new Thread(() => ThreadReadNewResultsJsons(_server, new List<string>() { e.FullPath })).Start();
                    break;
                }
            }
        }

        public void ThreadReadNewResultsJsons(Server _server, List<string> paths)
        {
            _server.WaitQueue++;
            _server.WaitQueue += paths.Count;
            while (PreSeasonVM.Instance.CheckExistingThreads()) { Thread.Sleep(200 + random.Next(100)); }
            _server.IsRunning = true;
            _server.WaitQueue--;
            Lap.Statics.LoadSQL();
            for (int pathNr = 0; pathNr < paths.Count; pathNr++) { PreSeason.AddResultsJson(paths[pathNr]); _server.WaitQueue--; }
            Lap.Statics.WriteJson(); Lap.Statics.ResetSQL(); Lap.Statics.WriteSQL();
            PreSeason.UpdatePreQResults();
            GSheets.UpdatePreQStandings(GSheet.ListIDs[0].DocID, GSheet.ListIDs[0].SheetID);
            _server.IsRunning = false;
        }

        public void ReloadAllResultsJson(object obj)
        {
            if (obj.GetType() == typeof(Server)) { Server _server = (Server)obj; new Thread(() => ThreadReloadAllResultsJson(_server)).Start(); }
        }

        public void ThreadReloadAllResultsJson(Server _server)
        {
            Lap.Statics.ResetSQL();
            if (_server.Type == ServerTypeEnum.PreQuali)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(_server.PathResults);
                FileInfo[] listResultsJsonFiles = dirInfo.GetFiles(_server.ResultsWatcher.Filter);
                List<string> listPaths = new List<string>();
                foreach (FileInfo resultsJsonFile in listResultsJsonFiles)
                {
                    listPaths.Add(_server.PathResults + resultsJsonFile.Name);
                }
                new Thread(() => ThreadReadNewResultsJsons(_server, listPaths)).Start();
            }
        }

        public static void UpdateEntrylists()
        {
            foreach (Server _server in Server.List) { UpdateEntrylist(_server); }
        }

        public static void UpdateEntrylist(Server _server)
        {
            if (_server.PathExists)
            {
                ACC_Entrylist Entrylist = new ACC_Entrylist();
                if (_server.EntrylistType == Server.listEntrylistTypes[0])
                {
                    Entrylist.CreateEmpty();
                    Entrylist.WriteJson(_server.PathCfg);
                }
                else if (_server.EntrylistType == Server.listEntrylistTypes[1])
                {
                    Entrylist.CreateRaceControl(_server.ForceEntrylist);
                    Entrylist.WriteJson(_server.PathCfg);
                }
                else if (_server.EntrylistType == Server.listEntrylistTypes[2])
                {
                    Entrylist.CreateDrivers(_server.ForceEntrylist);
                    Entrylist.WriteJson(_server.PathCfg);
                }
                else
                {
                    Entrylist.Create(_server.ForceCarModel, _server.ForceEntrylist, PreSeasonVM.Instance.CurrentEvent);
                    Entrylist.WriteJson(_server.PathCfg);
                }
            }
        }

        public static void UpdateBoPs()
        {
            foreach (Server _server in Server.List) { UpdateBoP(_server); }
        }

        public static void UpdateBoP(Server _server)
        {
            if (_server.PathExists)
            {
                ACC_BoP BoP = new ACC_BoP();
                if (_server.WriteBoP) { BoP.Create(); } else { BoP.CreateEmpty(); }
                BoP.WriteJson(_server.PathCfg);
            }
        }

        public void AddServer(string name, string path, ServerTypeEnum type, bool setOnline, string entrylistType, bool forceCarModel, bool forceEntrylist, bool writeBoP, bool detectResults)
        {
            Server _server = new Server()
            {
                Name = name,
                Path = path,
                Type = type,
                SetOnline = setOnline,
                EntrylistType = entrylistType,
                ForceCarModel = forceCarModel,
                ForceEntrylist = forceEntrylist,
                WriteBoP = writeBoP,
                DetectResults = detectResults
            };
            _server.ResultsWatcher.Created += new FileSystemEventHandler(EventNewResultsJson);
            this.RaisePropertyChanged("ListServer");
        }

        public void AddDefServer()
        {
            Server _server = new Server();
            _server.ResultsWatcher.Created += new FileSystemEventHandler(EventNewResultsJson);
            this.RaisePropertyChanged("ListServer");
        }

        public void DelServer(object obj)
        {
            if (obj.GetType() == typeof(Server)) { Server _server = (Server)obj; _server.Delete(); this.RaisePropertyChanged("ListServer"); }
        }

        public void RestoreSettings()
        {
            try
            {
                int ServerCount = Server.List.Count;
                for (int serverNr = ServerCount - 1; serverNr >= 0; serverNr--) { DelServer(Server.List[serverNr]); }
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(PathSettings, Encoding.Unicode));
                StateAutoServerRestart = obj.StateAutoServerRestart ?? stateAutoServerRestart;
                ServerRestartTime = obj.ServerRestartTime ?? serverRestartTime;
                if (obj.ListServer is IList)
                {
                    foreach (var item in obj.ListServer)
                    {
                        AddServer(
                            (string)item.Name,
                            (string)item.Path,
                            (ServerTypeEnum)item.Type,
                            (bool)item.SetOnline,
                            (string)item.EntrylistType,
                            (bool)item.ForceCarModel,
                            (bool)item.ForceEntrylist,
                            (bool)item.WriteBoP,
                            (bool)item.DetectResults
                            );
                    }
                }
                if (Server.List.Count == 0) { AddDefServer(); }
                MainVM.List[0].LogCurrentText = "Server settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore server settings failed!"; }
        }

        public void SaveSettings()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(PathSettings, text, Encoding.Unicode);
            MainVM.List[0].LogCurrentText = "Server settings saved.";
        }

        [JsonIgnore] public UICmd AddServerCmd { get; set; }
        [JsonIgnore] public UICmd DelServerCmd { get; set; }
        [JsonIgnore] public UICmd RestoreSettingsCmd { get; set; }
        [JsonIgnore] public UICmd SaveSettingsCmd { get; set; }
        [JsonIgnore] public UICmd ReloadAllResultsJsonCmd { get; set; }
    }
}
