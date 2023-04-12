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
using System.Text;
using System.Data;

namespace GTRC_Community_Manager
{
    public class ServerM : ObservableObject
    {
        public static ObservableCollection<ServerM> List = new();

        [JsonIgnore] public Process AccServerProcess;

        private Server server;
        private FileSystemWatcher resultswatcher;
        private bool setOnline = false;
        private bool detectResults = false;
        private int countOnline = 0;
        private Brush state;
        private bool isRunning = false;
        private int waitQueue = 0;

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
            set { Server.Path = value; PathExists = true; RaisePropertyChanged(); }
        }

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
            get { return Series.Statics.GetByID(Season.Statics.GetByID(Server.SeasonID).SeriesID).Name; }
            set
            {
                int oldVal = Season.Statics.GetByID(Server.SeasonID).SeriesID;
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
            get { return Season.Statics.GetByID(Server.SeasonID).Name; }
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
                        if (setOnline) { new Thread(ThreadStartAccServer).Start(); }
                        else { CountOnline = 0; StopAccServer(); }
                        RaisePropertyChanged();
                    }
                }
            }
        }

        [JsonIgnore] public int CountOnline
        {
            get { return countOnline; }
            set { countOnline = value; RaisePropertyChanged(); }
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
                foreach (Season _season in Season.Statics.List) { if (_season.SeriesID == Season.Statics.GetByID(Server.SeasonID).SeriesID) { listSeasonNames.Add(_season.Name); } }
                return listSeasonNames;
            }
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
                    WindowStyle = ProcessWindowStyle.Minimized
                }
            };
            AccServerProcess.OutputDataReceived += (sender, argsx) => ReadServerOutput(argsx.Data);
            AccServerProcess.Start();
            AccServerProcess.BeginOutputReadLine();
            AccServerProcess.WaitForExit();
            SetOnline = false;
        }

        public void ReadServerOutput(string? accServerOutput)
        {
            List<string> prefixes = new() { "Udp message count (", "Tcp message count (", "Updated lobby with ", "Updated leaderboard for ", "Alive cars: " };
            //List<string> suffixes = new List<string>() { "driver", "client" };
            List<string> messageBlacklist = new() { "==ERR:", "	EntryList entry: " };
            if (accServerOutput is not null && accServerOutput.Length > 0)
            {
                foreach (string message in messageBlacklist)
                {
                    if (accServerOutput.Length > message.Length && accServerOutput[..message.Length] == message) { break; }
                }
                foreach (string prefix in prefixes)
                {
                    if (accServerOutput.Length > prefix.Length && accServerOutput[..prefix.Length] == prefix)
                    {
                        for (int charCount = 1; charCount <= accServerOutput.Length - prefix.Length; charCount++)
                        {
                            string strDriverCount = accServerOutput.Substring(prefix.Length, charCount);
                            if (int.TryParse(strDriverCount, out int _currentDriverCount)) { CountOnline = _currentDriverCount; }
                            else { break; }
                        }
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
    }
}
