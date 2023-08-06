using Core;
using Enums;
using Database;
using Scripts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Data;
using System.Text;

namespace GTRC_Community_Manager
{
    public class ServerM : ObservableObject
    {
        public static ObservableCollection<ServerM> List = new();

        [JsonIgnore] public Process AccServerProcess;
        [JsonIgnore] public int ServerOutputTimeoutSekMax = 600;

        private Server server;
        private FileSystemWatcher resultswatcher;
        private bool setOnline = false;
        private bool detectResults = false;
        private int countOnline = 0;
        private Brush state;
        private bool isRunning = false;
        private int waitQueue = 0;
        private int serverOutputTimeoutSek = 0;

        public ServerM() { }

        public ServerM(int _serverID)
        {
            List.Add(this);
            ResultsWatcher = new FileSystemWatcher() { Filter = "*.json", NotifyFilter = NotifyFilters.FileName };
            ServerID = _serverID;
            ResultsWatcher.Created += new FileSystemEventHandler(EventNewResultsJson);
            State = Basics.StateOff;
        }

        public int ServerID
        {
            get { return Server.ID; }
            set
            {
                Server = Server.Statics.GetByID(value);
                PathExists = true;
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(Path));
                RaisePropertyChanged(nameof(SeriesName));
                RaisePropertyChanged(nameof(SeasonName));
                RaisePropertyChanged(nameof(ServerTypeEnum));
                RaisePropertyChanged(nameof(EntrylistTypeEnum));
                RaisePropertyChanged(nameof(ForceCarModel));
                RaisePropertyChanged(nameof(ForceEntrylist));
                RaisePropertyChanged(nameof(WriteBoP));
            }
        }

        [JsonIgnore] public Server Server
        {
            get { return server; }
            set { server = value; }
        }

        [JsonIgnore] public FileSystemWatcher ResultsWatcher
        {
            get { return resultswatcher; }
            set { resultswatcher = value; }
        }

        [JsonIgnore] public string Name
        {
            get { return Server.Name; }
            set { Server.Name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore] public string Path
        {
            get { PathExists = true; return Server.Path; }
            set { Server.Path = value; PathExists = true; RaisePropertyChanged(); RaisePropertyChanged(nameof(PathIsForbidden)); }
        }

        [JsonIgnore] public bool PathIsForbidden { get { return Server.PathIsForbidden(); } }

        [JsonIgnore] public bool PathExists
        {
            get { return Server.PathExistsExe(); }
            set
            {
                if (!Server.PathExistsExe()) { SetOnline = false; }
                if (Server.PathExists()) { ResultsWatcher.Path = Server.PathResults; if (DetectResults && State == Basics.StateOff) { State = Basics.StateOn; } }
                else { DetectResults = false; if (State == Basics.StateOn) { State = Basics.StateOff; } }
                RaisePropertyChanged();
            }
        }

        [JsonIgnore] public string SeriesName
        {
            get { return Server.ObjSeason.ObjSeries.Name; }
            set
            {
                int oldVal = Server.ObjSeason.SeriesID;
                int newVal = Series.Statics.GetByUniqProp(value).ID;
                List<Season> _seasonList = Season.Statics.GetBy(nameof(Season.SeriesID), newVal);
                int _seasonListCount = _seasonList.Count;
                if (newVal != oldVal && _seasonListCount > 0)
                {
                    Server.SeasonID = _seasonList[_seasonListCount - 1].ID;
                    RaisePropertyChanged(nameof(ListSeasonNames));
                    SeasonName = _seasonList[_seasonListCount - 1].Name;
                    RaisePropertyChanged(nameof(SeasonName));
                    RaisePropertyChanged();
                }
            }
        }

        [JsonIgnore] public string SeasonName
        {
            get { return Server.ObjSeason.Name; }
            set
            {
                int oldVal = Server.SeasonID;
                Server.SeasonID = Season.Statics.GetByUniqProp(value).ID;
                if (Server.SeasonID != oldVal)
                {
                    if (EntrylistTypeEnum == EntrylistTypeEnum.Season) { ServerVM.UpdateEntrylist(Server); }
                    ServerVM.UpdateBoP(Server);
                    RaisePropertyChanged();
                }
            }
        }

        [JsonIgnore] public ServerTypeEnum ServerTypeEnum
        {
            get { return Server.ServerTypeEnum; }
            set { Server.ServerTypeEnum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore] public EntrylistTypeEnum EntrylistTypeEnum
        {
            get { return Server.EntrylistTypeEnum; }
            set
            {
                EntrylistTypeEnum oldVal = Server.EntrylistTypeEnum;
                Server.EntrylistTypeEnum = value;
                if (Server.EntrylistTypeEnum != oldVal)
                {
                    ServerVM.UpdateEntrylist(Server);
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(ForceCarModel));
                    RaisePropertyChanged(nameof(ForceEntrylist));
                }
            }
        }

        [JsonIgnore] public bool ForceCarModel
        {
            get { return Server.ForceCarModel; }
            set { bool oldVal = Server.ForceCarModel; Server.ForceCarModel = value; if (Server.ForceCarModel != oldVal) { RaisePropertyChanged(); ServerVM.UpdateEntrylist(Server); } }
        }

        [JsonIgnore] public bool ForceEntrylist
        {
            get { return Server.ForceEntrylist; }
            set { bool oldVal = Server.ForceEntrylist; Server.ForceEntrylist = value; if (Server.ForceEntrylist != oldVal) { RaisePropertyChanged(); ServerVM.UpdateEntrylist(Server); } }
        }

        [JsonIgnore] public bool WriteBoP
        {
            get { return Server.WriteBoP; }
            set { bool oldVal = Server.WriteBoP; Server.WriteBoP = value; if (Server.WriteBoP != oldVal) { RaisePropertyChanged(); ServerVM.UpdateBoP(Server); } }
        }

        public bool SetOnline
        {
            get { return setOnline; }
            set {
                if (!value || Server.PathExistsExe())
                {
                    if (setOnline != value)
                    {
                        setOnline = value;
                        if (setOnline)
                        {
                            serverOutputTimeoutSek = ServerOutputTimeoutSekMax;
                            new Thread(ThreadCountdownServerTimeout).Start();
                            new Thread(ThreadStartAccServer).Start();
                        }
                        else { StopAccServer(); serverOutputTimeoutSek = -1; }
                        RaisePropertyChanged();
                    }
                }
            }
        }

        [JsonIgnore] public int CountOnline
        {
            get { return countOnline; }
            set { if (countOnline != value) { countOnline = value; RaisePropertyChanged(); } }
        }

        public bool DetectResults
        {
            get { return detectResults; }
            set
            {
                if (!value || Server.PathExists())
                {
                    if (detectResults != value)
                    {
                        detectResults = value;
                        ResultsWatcher.EnableRaisingEvents = detectResults;
                        if (detectResults && State == Basics.StateOff) { State = Basics.StateOn; }
                        else if (!detectResults && State == Basics.StateOn) { State = Basics.StateOff; }
                        RaisePropertyChanged();
                    }
                }
            }
        }

        [JsonIgnore] public Brush State
        {
            get { return state; }
            set { state = value; RaisePropertyChanged(); }
        }

        [JsonIgnore] public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; SetStateResults(); }
        }

        [JsonIgnore] public int WaitQueue
        {
            get { return waitQueue; }
            set { if (value >= 0) { waitQueue = value; SetStateResults(); } }
        }

        [JsonIgnore] public IEnumerable<ServerTypeEnum> ListServerTypeEnums
        {
            get { return Enum.GetValues(typeof(ServerTypeEnum)).Cast<ServerTypeEnum>(); }
            set { }
        }

        [JsonIgnore] public IEnumerable<EntrylistTypeEnum> ListEntrylistTypeEnums
        {
            get { return Enum.GetValues(typeof(EntrylistTypeEnum)).Cast<EntrylistTypeEnum>(); }
            set { }
        }

        [JsonIgnore] public ObservableCollection<string> ListSeriesNames
        {
            get
            {
                ObservableCollection<string> listSeriesNames = new();
                foreach (Series _series in Series.Statics.List) { if (Season.Statics.GetBy(nameof(Season.SeriesID), _series.ID).Count > 0) { listSeriesNames.Add(_series.Name); } }
                return listSeriesNames;
            }
        }

        [JsonIgnore] public ObservableCollection<string> ListSeasonNames
        {
            get
            {
                ObservableCollection<string> listSeasonNames = new();
                foreach (Season _season in Season.Statics.List) { if (_season.SeriesID == Server.ObjSeason.SeriesID) { listSeasonNames.Add(_season.Name); } }
                return listSeasonNames;
            }
        }

        [JsonIgnore] public int ServerOutputTimeoutSek
        {
            get { return serverOutputTimeoutSek; }
            set { if (value <= 0) { CountOnline = 0; } else if (serverOutputTimeoutSek >= 0) { serverOutputTimeoutSek = value; } }
        }

        public void SetStateResults()
        {
            if (IsRunning)
            {
                if (WaitQueue > 1) { State = Basics.StateRunWait; }
                else { State = Basics.StateRun; }
            }
            else
            {
                if (WaitQueue > 1) { State = Basics.StateWait; }
                else { if (DetectResults) { State = Basics.StateOn; } else { State = Basics.StateOff; } }
            }
        }

        public void ThreadStartAccServer()
        {
            AccServerProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = null,
                    WorkingDirectory = Server.PathServer,
                    FileName = Server.PathExe,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                }
            };
            AccServerProcess.OutputDataReceived += (sender, argsx) => ReadServerOutput(argsx.Data);
            AccServerProcess.ErrorDataReceived += (sender, argsx) => ReadServerOutput(argsx.Data);
            AccServerProcess.Start();
            AccServerProcess.BeginOutputReadLine();
            AccServerProcess.BeginErrorReadLine();
            /*string outputStr = "";
            while (true)
            {
                if (SetOnline)
                {
                    int _charInt = AccServerProcess.StandardOutput.Read();
                    if (_charInt > 0)
                    {
                        char _char = (char)_charInt;
                        if (_char == '\n') { ReadServerOutput(outputStr); outputStr = ""; }
                        else { outputStr += _char; }
                    }
                }
                else { ReadServerOutput(outputStr); break; }
            }*/
            AccServerProcess.WaitForExit();
            SetOnline = false;
        }

        public void ReadServerOutput(string? accServerOutput)
        {
            List<string> prefixes = new() { "Udp message count (", "Tcp message count (", "Updated lobby with ", "Updated leaderboard for ", "Alive cars: ",
                "Alive connections: " };
            if (accServerOutput is not null && accServerOutput.Length > 0)
            {
                string[] accServerOutputLines = accServerOutput.Split('\n');
                foreach (string accServerOutputLine in accServerOutputLines)
                {
                    ServerOutputTimeoutSek = ServerOutputTimeoutSekMax;
                    bool prefixFound = false;
                    foreach (string prefix in prefixes)
                    {
                        if (accServerOutputLine.Length > prefix.Length && accServerOutputLine[..prefix.Length] == prefix)
                        {
                            for (int charCount = 1; charCount <= accServerOutputLine.Length - prefix.Length; charCount++)
                            {
                                string strDriverCount = accServerOutputLine.Substring(prefix.Length, charCount);
                                if (int.TryParse(strDriverCount, out int _currentDriverCount)) { CountOnline = _currentDriverCount; prefixFound = true; }
                                else { break; }
                            }
                        }
                        if (prefixFound) { break; }
                    }
                }
            }
        }

        public void StopAccServer()
        {
            try { AccServerProcess?.Kill(); }
            catch { }
        }

        public void EventNewResultsJson(object source, FileSystemEventArgs e)
        {
            new Thread(() => ServerVM.Instance.ThreadReadNewResultsJsons(this, new List<string>() { e.FullPath })).Start();
        }

        public void ThreadCountdownServerTimeout()
        {
            while (ServerOutputTimeoutSek >= 0) { Thread.Sleep(1000); ServerOutputTimeoutSek--; }
        }
    }
}
