using Core;
using Enums;
using Database;
using Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Documents;

namespace GTRC_Community_Manager
{
    public class ServerVM : ObservableObject
    {
        public static ServerVM Instance;
        private static readonly string PathSettings = MainWindow.dataDirectory + "config server.json";
        [JsonIgnore] public BackgroundWorker BackgroundWorkerRestartServer = new() { WorkerSupportsCancellation = true };
        public static readonly Random random = new();
        public static bool IsRunning = false;

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

        public static void ThreadRestartAllAccServers(int delayMS = 0)
        {
            foreach (ServerM _server in ServerM.List)
            {
                if (_server.SetOnline) { _server.SetOnline = false; Thread.Sleep(delayMS); _server.SetOnline = true; Thread.Sleep(delayMS); }
            }
        }

        public static void ThreadStopAllAccServers(int delayMS = 0)
        {
            foreach (ServerM _server in ServerM.List) { if (_server.SetOnline) { _server.SetOnline = false; Thread.Sleep(delayMS); } }
            foreach (ServerM _server in ServerM.List) { _server.StopAccServer(); }
        }

        public void TriggerRestartServer()
        {
            UpdateEntrylists();
            UpdateBoPs();
            new Thread(() => ThreadRestartAllAccServers(500)).Start();
        }

        public void ReloadAllServerAllResultsJson()
        {
            foreach (ServerM _server in ServerM.List) { new Thread(() => ThreadReloadAllResultsJson(_server)).Start(); }
        }

        public void ReloadAllResultsJson(object obj)
        {
            if (obj.GetType() == typeof(ServerM)) { ServerM _server = (ServerM)obj; new Thread(() => ThreadReloadAllResultsJson(_server)).Start(); }
        }

        public void ThreadReloadAllResultsJson(ServerM _server)
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

        public void ThreadReadNewResultsJsons(ServerM _server, List<string> paths)
        {
            _server.WaitQueue++;
            _server.WaitQueue += paths.Count;
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); }
            _server.IsRunning = true;
            _server.WaitQueue--;
            for (int pathNr = 0; pathNr < paths.Count; pathNr++) { ImportResultsJson(paths[pathNr], _server); _server.WaitQueue--; }
            Lap.Statics.LoadSQL();
            if (PreSeasonVM.Instance is not null)
            {
                PreSeason.UpdateLeaderboard(_server);
                //PreSeason.UpdatePreQResults(PreSeasonVM.Instance.CurrentSeasonID);
                //GSheets.UpdatePreQStandings(GSheet.ListIDs[1].DocID, GSheet.ListIDs[1].SheetID);
            }
            _server.IsRunning = false;
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
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Server.Statics.LoadSQL();
            _ = new Server();
            Server.Statics.WriteSQL();
            Server.Statics.LoadSQL();
            IsRunning = false;
        }

        public void DelServer(object obj)
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            if (obj.GetType() == typeof(ServerM)) { ServerM _serverM = (ServerM)obj; _serverM.Server.ListRemove(); this.RaisePropertyChanged(nameof(ListServer)); }
            IsRunning = false;
        }

        public void RestoreSettingsTrigger()
        {
            ThreadStopAllAccServers();
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Server.Statics.LoadSQL();
            IsRunning = false;
        }

        public void RestoreSettings()
        {
            try
            {
                ThreadStopAllAccServers();
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
            ThreadStopAllAccServers();
            File.WriteAllText(PathSettings, text, Encoding.Unicode);
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Server.Statics.WriteSQL();
            IsRunning = false;
            MainVM.List[0].LogCurrentText = "Server settings saved.";
        }

        public static bool TryReadResultsJson(string path)
        {
            Thread.Sleep(10);
            try
            {
                var jsonFile = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path, Encoding.Unicode));
                if (jsonFile?.sessionResult?.leaderBoardLines is IList && jsonFile?.laps is IList &&
                    jsonFile?.sessionType?.ToString() != "" && jsonFile?.trackName?.ToString() != "") { return true; }
            }
            catch { return false; }
            return false;
        }

        public static void ImportResultsJson(string _path, ServerM _server)
        {
            //Sollte dynamisch sein:
            int attemptMax = 1000;

            DateTime dateTime = ResultsPath2DateTime(_path);
            if (dateTime < Event.DateTimeMaxValue)
            {
                for (int attemptNr = 0; attemptNr < attemptMax; attemptNr++)
                {
                    if (TryReadResultsJson(_path)) { break; }
                    else { MainVM.List[0].LogCurrentText = "Versuch-Nr " + attemptNr.ToString() + ": Results nicht einlesbar."; }
                }
                if (!TryReadResultsJson(_path)) { return; }
                var resultsJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(_path, Encoding.Unicode));
                var leaderBoardLines = resultsJson!.sessionResult!.leaderBoardLines;
                var laps = resultsJson!.laps;
                string _sessionType = resultsJson!.sessionType!.ToString();
                string _trackName = resultsJson!.trackName!.ToString();
                SessionTypeEnum? sessionType = null;
                if (_sessionType.Length > 0)
                {
                    if (_sessionType[..1] == "R") { sessionType = SessionTypeEnum.Race; }
                    else if (_sessionType[..1] == "Q") { sessionType = SessionTypeEnum.Qualifying; }
                    else if (_sessionType.Length > 1) { if (_sessionType[..2] == "FP") { sessionType = SessionTypeEnum.Practice; } }
                }
                if (_trackName.Length > 5 && _trackName.AsSpan(_trackName.Length - 5, 1) == "_" &&
                    int.TryParse(_trackName.AsSpan(_trackName.Length - 4, 4), out int bopYear))
                {
                    _trackName = _trackName[..^5];
                }
                int trackID = Track.Statics.GetByUniqProp(_trackName).ID;
                if (sessionType is null || trackID == Basics.NoID ||
                laps is not IList || leaderBoardLines is not IList || laps.Count <= 0 || leaderBoardLines.Count <= 0) { return; }
                ResultsFile resultsFile = ResultsFile.Statics.GetByUniqProp(new List<dynamic>() { _server.ServerID, dateTime });
                if (resultsFile.ID != Basics.NoID && resultsFile.TrackID == trackID) { return; }
                resultsFile = new() { ServerID = _server.ServerID, Date = dateTime, SessionTypeEnum = sessionType ?? 0, TrackID = trackID,
                    SeasonID = _server.Server.SeasonID, ServerType = _server.Server.ServerType };
                resultsFile = ResultsFile.Statics.WriteSQL(resultsFile);
                if (resultsFile.ID == Basics.NoID) { return; }
                foreach (var _lap in laps)
                {
                    Lap lap = new() { ResultsFileID = resultsFile.ID };
                    var _time = _lap.laptime;
                    if (_time is JValue) { if (int.TryParse(_time.ToString(), out int time)) { lap.Time = time; } }
                    if (_lap.splits is IList && _lap.splits.Count > 2)
                    {
                        _time = _lap.splits[0];
                        if (_time is JValue) { if (int.TryParse(_time.ToString(), out int time)) { lap.Sector1 = time; } }
                        _time = _lap.splits[1];
                        if (_time is JValue) { if (int.TryParse(_time.ToString(), out int time)) { lap.Sector2 = time; } }
                        _time = _lap.splits[2];
                        if (_time is JValue) { if (int.TryParse(_time.ToString(), out int time)) { lap.Sector3 = time; } }
                    }
                    var _valid = _lap.isValidForBest;
                    if (_valid is JValue) { if (bool.TryParse(_valid.ToString(), out bool valid)) { lap.IsValid = valid; } }
                    int carID = Basics.NoID;
                    var _carID = _lap.carId;
                    if (_carID is JValue) { int.TryParse(_carID.ToString(), out carID); }
                    int driverNr = Basics.NoID;
                    var _driverNr = _lap.driverIndex;
                    if (_driverNr is JValue) { int.TryParse(_driverNr.ToString(), out driverNr); }
                    string steamID = Basics.NoID.ToString();
                    if (carID > Basics.NoID && driverNr > Basics.NoID)
                    {
                        for (int pos = 0; pos < leaderBoardLines.Count; pos++)
                        {
                            if (leaderBoardLines[pos].car is JObject)
                            {
                                int carID_lbl = Basics.NoID;
                                var _carID_lbl = leaderBoardLines[pos].car.carId;
                                if (_carID_lbl is JValue && int.TryParse(_carID_lbl.ToString(), out carID_lbl) && carID == carID_lbl)
                                {
                                    if (leaderBoardLines[pos].car.drivers is IList && leaderBoardLines[pos].car.drivers.Count > driverNr)
                                    {
                                        if (leaderBoardLines[pos].car.drivers[driverNr] is JObject &&
                                            leaderBoardLines[pos].car.drivers[driverNr].playerId is JValue)
                                        {
                                            steamID = leaderBoardLines[pos].car.drivers[driverNr].playerId.ToString();
                                            var strRN = leaderBoardLines[pos].car.raceNumber;
                                            if (strRN is JValue) { if (int.TryParse(strRN.ToString(), out int _raceNumber)) { lap.RaceNumber = _raceNumber; } }
                                            var strFN = leaderBoardLines[pos].car.drivers[driverNr].firstName;
                                            if (strFN is JValue) { lap.FirstName = strFN.ToString(); }
                                            var strLN = leaderBoardLines[pos].car.drivers[driverNr].lastName;
                                            if (strLN is JValue) { lap.LastName = strLN.ToString(); }
                                            var strACI = leaderBoardLines[pos].car.carModel;
                                            if (strACI is JValue) { if (int.TryParse(strACI.ToString(), out int _accCarID)) { lap.AccCarID = _accCarID; } }
                                            var strB = leaderBoardLines[pos].car.ballastKg;
                                            if (strB is JValue) { if (int.TryParse(strB.ToString(), out int _ballast)) { lap.Ballast = _ballast; } }
                                            var strR = leaderBoardLines[pos].car.restrictor;
                                            if (strR is JValue) { if (int.TryParse(strR.ToString(), out int _restrictor)) { lap.Restrictor = _restrictor; } }
                                            var strC = leaderBoardLines[pos].car.cupCategory;
                                            if (strC is JValue)
                                            {
                                                if (int.TryParse(strC.ToString(), out int _category))
                                                {
                                                    if (_category == 0) { lap.Category = 3; }
                                                    else if (_category == 3) { lap.Category = 1; }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    lap.SteamID = Driver.String2LongSteamID(steamID);
                    if (Driver.IsValidSteamID(lap.SteamID)) { _ = Lap.Statics.WriteSQL(lap); }
                }
            }
        }

        public static DateTime ResultsPath2DateTime(string path)
        {
            DateTime dateTime = Event.DateTimeMaxValue;
            string[] pathRoot = path.Split('\\');
            if (pathRoot.Length > 0)
            {
                string[] fileName = pathRoot[^1].Split('_');
                if (fileName.Length > 1 && fileName[0].Length == 6 && fileName[1].Length == 6)
                {
                    string dateStr = fileName[0];
                    string timeStr = fileName[1];
                    if (int.TryParse(dateStr[..2], out int yearInt) && int.TryParse(dateStr.AsSpan(2, 2), out int monthInt) &&
                        int.TryParse(dateStr.AsSpan(4, 2), out int dayInt) && int.TryParse(timeStr[..2], out int hourInt) &&
                        int.TryParse(timeStr.AsSpan(2, 2), out int minInt) && int.TryParse(timeStr.AsSpan(4, 2), out int secInt))
                    {
                        yearInt += 2000;
                        dateTime = new DateTime(yearInt, monthInt, dayInt, hourInt, minInt, secInt, 0, DateTimeKind.Local);
                    }
                }
            }
            return dateTime;
        }

        [JsonIgnore] public UICmd AddServerCmd { get; set; }
        [JsonIgnore] public UICmd DelServerCmd { get; set; }
        [JsonIgnore] public UICmd RestoreSettingsCmd { get; set; }
        [JsonIgnore] public UICmd SaveSettingsCmd { get; set; }
        [JsonIgnore] public UICmd ReloadAllResultsJsonCmd { get; set; }
        [JsonIgnore] public UICmd ReloadAllServerAllResultsJsonCmd { get; set; }
    }
}
