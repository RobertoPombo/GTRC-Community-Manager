using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Discord;
using GTRC_Community_Manager;
using GTRCLeagueManager.Database;
using Newtonsoft.Json;

namespace GTRCLeagueManager
{
    public class ServerVM : ObservableObject
    {
        public static ServerVM Instance;
        private static readonly string PathSettings = MainWindow.dataDirectory + "config server.json";
        [JsonIgnore] public BackgroundWorker BackgroundWorkerRestartServer = new() { WorkerSupportsCancellation = true };
        public static readonly Random random = new();

        private bool stateAutoServerRestart = false;
        private int serverRestartTime = 0;
        private int serverRestartRemTime = 0;

        public ServerVM()
        {
            Instance = this;
            AddServerCmd = new UICmd((o) => AddServer());
            DelServerCmd = new UICmd((o) => DelServer(o));
            RestoreSettingsCmd = new UICmd((o) => RestoreSettingsTrigger());
            SaveSettingsCmd = new UICmd((o) => SaveSettings());
            ReloadAllResultsJsonCmd = new UICmd((o) => ReloadAllResultsJson(o));
            ReloadAllServerAllResultsJsonCmd = new UICmd((o) => ReloadAllServerAllResultsJson());
            if (!File.Exists(PathSettings)) { SaveSettings(); }
            RestoreSettingsTrigger();
            BackgroundWorkerRestartServer.DoWork += InfiniteLoopRestartServer;
            BackgroundWorkerRestartServer.RunWorkerAsync();
        }

        public bool StateAutoServerRestart
        {
            get { return stateAutoServerRestart; }
            set { stateAutoServerRestart = value; UpdateServerRestartRemTime(); RaisePropertyChanged(); }
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
                RaisePropertyChanged();
            }
        }

        [JsonIgnore] public string ServerRestartRemTime
        {
            get
            {
                if (!StateAutoServerRestart) { return "∞"; }
                else if (serverRestartRemTime > 7200) { return ((int)Math.Ceiling((double)serverRestartRemTime / (60 * 60))).ToString() + " h"; }
                else if (serverRestartRemTime > 120) { return ((int)Math.Ceiling((double)serverRestartRemTime / 60)).ToString() + " min"; }
                else { return serverRestartRemTime.ToString() + " sec"; }
            }
            set { RaisePropertyChanged(); }
        }

        public ObservableCollection<ServerM> ListServer
        {
            get { return ServerM.List; }
            set { }
        }

        public static void UpdateListSeasons()
        {
            if (Instance != null)
            {
                foreach (ServerM _serverM in ServerM.List) { _serverM.RaisePropertyChanged(nameof(ServerM.ListSeasonNames)); }
                Instance.RestoreSettings();
            }
        }

        public static void UpdateListServers()
        {
            if (Instance != null) { Instance.RestoreSettings(); }
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
            foreach (ServerM _server in ServerM.List)
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
            foreach (ServerM _server in ServerM.List)
            {
                if (_server.Server.ServerTypeEnum == ServerTypeEnum.PreQuali && Basics.PathIsParentOf(Server.NoPath, _server.Server.PathResults, e.FullPath))
                {
                    new Thread(() => ThreadReadNewResultsJsons(_server, new List<string>() { e.FullPath })).Start();
                    break;
                }
            }
        }

        public void ThreadReadNewResultsJsons(ServerM _server, List<string> paths)
        {
            _server.WaitQueue++;
            _server.WaitQueue += paths.Count;
            while (PreSeasonVM.Instance.CheckExistingThreads()) { Thread.Sleep(200 + random.Next(100)); }
            _server.IsRunning = true;
            _server.WaitQueue--;
            Lap.Statics.LoadSQL();
            for (int pathNr = 0; pathNr < paths.Count; pathNr++) { PreSeason.AddResultsJson(paths[pathNr]); _server.WaitQueue--; }
            Lap.Statics.WriteSQL();
            if (PreSeasonVM.Instance is not null)
            {
                PreSeason.UpdatePreQResults(PreSeasonVM.Instance.CurrentSeasonID);
                GSheets.UpdatePreQStandings(GSheet.ListIDs[1].DocID, GSheet.ListIDs[1].SheetID);
            }
            _server.IsRunning = false;
        }

        public void ReloadAllServerAllResultsJson()
        {

        }

        public void ReloadAllResultsJson(object obj)
        {
            if (obj.GetType() == typeof(ServerM)) { ServerM _server = (ServerM)obj; new Thread(() => ThreadReloadAllResultsJson(_server)).Start(); }
        }

        public void ThreadReloadAllResultsJson(ServerM _server)
        {
            Lap.Statics.ResetSQL();
            if (_server.Server.ServerTypeEnum == ServerTypeEnum.PreQuali)
            {
                DirectoryInfo dirInfo = new(_server.Server.PathResults);
                FileInfo[] listResultsJsonFiles = dirInfo.GetFiles(_server.ResultsWatcher.Filter);
                List<string> listPaths = new();
                foreach (FileInfo resultsJsonFile in listResultsJsonFiles)
                {
                    listPaths.Add(_server.Server.PathResults + resultsJsonFile.Name);
                }
                new Thread(() => ThreadReadNewResultsJsons(_server, listPaths)).Start();
            }
        }

        public static void UpdateEntrylists()
        {
            foreach (ServerM _server in ServerM.List) { UpdateEntrylist(_server.Server); }
        }

        public static void UpdateEntrylist(Server _server)
        {
            if (_server.PathExists())
            {
                ACC_Entrylist Entrylist = new();
                if (_server.EntrylistTypeEnum == EntrylistTypeEnum.None)
                {
                    Entrylist.CreateEmpty();
                    Entrylist.WriteJson(_server.PathCfg);
                }
                else if (_server.EntrylistTypeEnum == EntrylistTypeEnum.RaceControl)
                {
                    Entrylist.CreateRaceControl(_server.ForceEntrylist);
                    Entrylist.WriteJson(_server.PathCfg);
                }
                else if (_server.EntrylistTypeEnum == EntrylistTypeEnum.AllDrivers)
                {
                    Entrylist.CreateDrivers(_server.ForceEntrylist);
                    Entrylist.WriteJson(_server.PathCfg);
                }
                else if (_server.EntrylistTypeEnum == EntrylistTypeEnum.Season && PreSeasonVM.Instance is not null)
                {
                    Entrylist.Create(_server.ForceCarModel, _server.ForceEntrylist, PreSeasonVM.Instance.CurrentEvent);
                    Entrylist.WriteJson(_server.PathCfg);
                }
            }
        }

        public static void UpdateBoPs()
        {
            foreach (ServerM _server in ServerM.List) { UpdateBoP(_server.Server); }
        }

        public static void UpdateBoP(Server _server)
        {
            if (_server.PathExists())
            {
                ACC_BoP BoP = new();
                if (_server.WriteBoP && PreSeasonVM.Instance is not null) { BoP.Create(PreSeasonVM.Instance.CurrentEvent); BoP.WriteJson(_server.PathCfg); }
                else if (!_server.WriteBoP) { BoP.CreateEmpty(); BoP.WriteJson(_server.PathCfg); }
            }
        }

        public void AddServer()
        {
            Server.Statics.LoadSQL();
            _ = new Server();
            Server.Statics.WriteSQL();
            Server.Statics.LoadSQL();
        }

        public void DelServer(object obj)
        {
            if (obj.GetType() == typeof(ServerM)) { ServerM _serverM = (ServerM)obj; _serverM.Server.ListRemove(); this.RaisePropertyChanged(nameof(ListServer)); }
        }

        public void RestoreSettingsTrigger() { Server.Statics.LoadSQL(); }

        public void RestoreSettings()
        {
            try
            {
                int countServerM = ServerM.List.Count;
                for (int _serverMNr = countServerM - 1; _serverMNr >= 0; _serverMNr--) { ServerM.List.RemoveAt(_serverMNr); }
                dynamic? obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(PathSettings, Encoding.Unicode));
                StateAutoServerRestart = obj?.StateAutoServerRestart ?? stateAutoServerRestart;
                ServerRestartTime = obj?.ServerRestartTime ?? serverRestartTime;
                Dictionary<int, Tuple<bool, bool>> listServerSettings = new();
                if (obj?.ListServer is IList)
                {
                    foreach (var item in obj.ListServer)
                    {
                        int serverID = item.ServerID ?? Basics.NoID;
                        bool setOnline = item.SetOnline ?? new ServerM().SetOnline;
                        bool detectResults = item.DetectResults ?? new ServerM().DetectResults;
                        if (Server.Statics.ExistsID(serverID)) { listServerSettings[serverID] = new Tuple<bool, bool> (setOnline, detectResults); }
                    }
                }
                foreach (Server _server in Server.Statics.List)
                {
                    ServerM serverM = new(_server.ID);
                    if (listServerSettings.ContainsKey(_server.ID))
                    {
                        serverM.SetOnline = listServerSettings[_server.ID].Item1;
                        serverM.DetectResults = listServerSettings[_server.ID].Item2;
                    }
                }
                UpdateEntrylists();
                UpdateBoPs();
                MainVM.List[0].LogCurrentText = "Server settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore server settings failed!"; }
        }

        public void SaveSettings()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(PathSettings, text, Encoding.Unicode);
            Server.Statics.WriteSQL();
            MainVM.List[0].LogCurrentText = "Server settings saved.";
        }

        [JsonIgnore] public UICmd AddServerCmd { get; set; }
        [JsonIgnore] public UICmd DelServerCmd { get; set; }
        [JsonIgnore] public UICmd RestoreSettingsCmd { get; set; }
        [JsonIgnore] public UICmd SaveSettingsCmd { get; set; }
        [JsonIgnore] public UICmd ReloadAllResultsJsonCmd { get; set; }
        [JsonIgnore] public UICmd ReloadAllServerAllResultsJsonCmd { get; set; }
    }
}
