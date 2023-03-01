using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;

namespace GTRCLeagueManager.Database
{
    public class Server : ObservableObject
    {
        public static readonly string NoPath = ServerVM.NoPath;
        public static readonly string ForbiddenPath = ServerVM.ForbiddenPath;
        public static readonly ObservableCollection<string> listEntrylistTypes = new ObservableCollection<string>() { "None", "Race Control", "All Drivers" };
        public static ObservableCollection<Server> List = new ObservableCollection<Server>();
        public static readonly string DefaultName = "Server #1";

        [JsonIgnore] public Process AccServerProcess;

        private FileSystemWatcher resultswatcher;
        private string name;
        private string path;
        private bool pathExists = false;
        private ServerTypeEnum type = ServerTypeEnum.Practice;
        private bool setOnline = false;
        private int countOnline = 0;
        private string entrylistType = listEntrylistTypes[0];
        private bool forceCarModel = false;
        private bool forceEntrylist = false;
        private bool writeBoP = false;
        private bool detectResults = false;
        private Brush state;
        private bool isRunning = false;
        private int waitQueue = 0;

        public Server()
        {
            List.Add(this);
            ResultsWatcher = new FileSystemWatcher() { Filter = "*.json", NotifyFilter = NotifyFilters.FileName };
            Path = NoPath;
            State = ServerVM.StateOff;
            if (name == null) { Name = DefaultName; }
        }

        [JsonIgnore] public IEnumerable<ServerTypeEnum> ListServerTypes
        {
            get { return Enum.GetValues(typeof(ServerTypeEnum)).Cast<ServerTypeEnum>(); }
            set { }
        }

        [JsonIgnore] public ObservableCollection<string> ListEntrylistTypes
        {
            get
            {
                ObservableCollection<string> tempList = new ObservableCollection<string>();
                foreach (string _entrylistType in listEntrylistTypes) { tempList.Add(_entrylistType); }
                foreach (Season _season in Season.Statics.List) { tempList.Add(_season.Name); }
                return tempList;
            }
            set { }
        }

        [JsonIgnore] public FileSystemWatcher ResultsWatcher
        {
            get { return resultswatcher; }
            set { resultswatcher = value; }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (value != null)
                {
                    name = Basics.RemoveSpaceStartEnd(value);
                    if (name == null || name == "") { name = DefaultName; }
                    int nr = 1; string defName = name; bool isUnique = true;
                    if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
                    foreach (Server _server in List) { if (_server != this && _server.Name == name) { isUnique = false; break; } }
                    while (!isUnique)
                    {
                        isUnique = true;
                        name = defName + " #" + nr.ToString();
                        foreach (Server _server in List) { if (_server != this && _server.Name == name) { isUnique = false; break; } }
                        nr++; if (nr == int.MaxValue) { break; }
                    }
                }
            }
        }

        public string Path
        {
            get { return path; }
            set
            {
                if (value != null)
                {
                    value = Basics.ValidatedPath(NoPath, value);
                    if (path != value)
                    {
                        path = value;
                        bool isValid = true;
                        foreach (string currentPath in new List<string>() { AbsolutePath, PathCfg, PathResults })
                        {
                            if (!Directory.Exists(currentPath)) { isValid = false; break; }
                            if (Basics.PathIsParentOf(NoPath, ForbiddenPath, currentPath)) { isValid = false; break; }
                        }
                        foreach (Server _server in List) { if (_server != this && _server.Path == path) { isValid = false; } }
                        if (isValid) { PathExists = true; } else { PathExists = false; }
                        this.RaisePropertyChanged();
                    }
                }
            }
        }

        [JsonIgnore] public string AbsolutePath { get { return Basics.RelativePath2AbsolutePath(NoPath, Basics.ValidatedPath(NoPath, Path)); } }

        [JsonIgnore] public string PathServer { get { return AbsolutePath + "\\server\\"; } }

        [JsonIgnore] public string PathCfg { get { return PathServer + "\\cfg\\"; } }

        [JsonIgnore] public string PathResults { get { return PathServer + "\\results\\"; } }

        [JsonIgnore] public string PathExe { get { return PathServer + "\\accServer.exe"; } }

        [JsonIgnore] public bool PathExists
        {
            get { return pathExists; }
            set
            {
                if (value)
                {
                    foreach (string currentPath in new List<string>() { AbsolutePath, PathCfg, PathResults })
                    {
                        if (!Directory.Exists(currentPath)) { Directory.CreateDirectory(currentPath); }
                        else if (Basics.PathIsParentOf(NoPath, ForbiddenPath, currentPath)) { Path = NoPath; }
                    }
                    ResultsWatcher.Path = PathResults;
                    if (DetectResults && State == ServerVM.StateOff) { State = ServerVM.StateOn; }
                }
                else
                {
                    DetectResults = false;
                    SetOnline = false;
                    if (State == ServerVM.StateOn) { State = ServerVM.StateOff; }
                }
                pathExists = value;
                this.RaisePropertyChanged();
            }
        }

        public ServerTypeEnum Type
        {
            get { return type; }
            set { if (type != value) { type = value; this.RaisePropertyChanged(); } }
        }

        public bool SetOnline
        {
            get { return setOnline; }
            set
            {
                if (setOnline != value)
                {
                    if (!value)
                    {
                        setOnline = value; this.RaisePropertyChanged();
                        CountOnline = 0;
                        StopAccServer();
                    }
                    else if (PathExists && File.Exists(PathExe))
                    {
                        setOnline = value; this.RaisePropertyChanged();
                        new Thread(ThreadStartAccServer).Start();
                    }
                }
            }
        }

        [JsonIgnore] public int CountOnline
        {
            get { return countOnline; }
            set { countOnline = value; this.RaisePropertyChanged(); }
        }

        public string EntrylistType
        {
            get { return entrylistType; }
            set
            {
                if (value != null && entrylistType != value && ListEntrylistTypes.Contains(value))
                {
                    entrylistType = value;
                    if (listEntrylistTypes.Contains(value)) { ForceCarModel = false; }
                    if (value == listEntrylistTypes[0]) { ForceEntrylist = false; }
                    this.RaisePropertyChanged();
                    ServerVM.UpdateEntrylist(this);
                }
            }
        }

        public bool ForceCarModel
        {
            get { return forceCarModel; }
            set { if (forceCarModel != value && (!value || !listEntrylistTypes.Contains(EntrylistType))) { forceCarModel = value; this.RaisePropertyChanged(); ServerVM.UpdateEntrylist(this); } }
        }

        public bool ForceEntrylist
        {
            get { return forceEntrylist; }
            set { if (forceEntrylist != value && (!value || EntrylistType != listEntrylistTypes[0])) { forceEntrylist = value; this.RaisePropertyChanged(); ServerVM.UpdateEntrylist(this); } }
        }

        public bool WriteBoP
        {
            get { return writeBoP; }
            set { if (writeBoP != value) { writeBoP = value; this.RaisePropertyChanged(); ServerVM.UpdateBoP(this); } }
        }

        public bool DetectResults
        {
            get { return detectResults; }
            set
            {
                if (detectResults != value && (!value || PathExists))
                {
                    detectResults = value;
                    ResultsWatcher.EnableRaisingEvents = DetectResults;
                    if (value && State == ServerVM.StateOff) { State = ServerVM.StateOn; }
                    else if (!value && State == ServerVM.StateOn) { State = ServerVM.StateOff; }
                    this.RaisePropertyChanged();
                }
            }
        }

        [JsonIgnore] public Brush State
        {
            get { return state; }
            set { state = value; this.RaisePropertyChanged(); }
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

        public void Delete()
        {
            SetOnline = false;
            DetectResults = false;
            ResultsWatcher.Dispose();
            if (List.Contains(this)) { List.Remove(this); }
        }

        public void SetStateResults()
        {
            if (IsRunning)
            {
                if (WaitQueue > 1) { State = ServerVM.StateRunWait; }
                else { State = ServerVM.StateRun; }
            }
            else
            {
                if (WaitQueue > 1) { State = ServerVM.StateWait; }
                else { if (DetectResults) { State = ServerVM.StateOn; } else { State = ServerVM.StateOff; } }
            }
        }

        public void ThreadStartAccServer()
        {
            AccServerProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = null,
                    WorkingDirectory = PathServer,
                    FileName = PathExe,
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

        public void ReadServerOutput(string accServerOutput)
        {
            List<string> prefixes = new List<string>() { "Udp message count (", "Tcp message count (", "Updated lobby with ", "Updated leaderboard for ", "Alive cars: " };
            //List<string> suffixes = new List<string>() { "driver", "client" };
            List<string> messageBlacklist = new List<string>() { "==ERR:", "	EntryList entry: " };
            if (accServerOutput != null && accServerOutput.Length > 0)
            {
                foreach (string message in messageBlacklist)
                {
                    if (accServerOutput.Length > message.Length && accServerOutput.Substring(0, message.Length) == message) { break; }
                }
                foreach (string prefix in prefixes)
                {
                    if (accServerOutput.Length > prefix.Length && accServerOutput.Substring(0, prefix.Length) == prefix)
                    {
                        for (int charCount = 1; charCount <= accServerOutput.Length - prefix.Length; charCount++)
                        {
                            string strDriverCount = accServerOutput.Substring(prefix.Length, charCount);
                            if (Int32.TryParse(strDriverCount, out int _currentDriverCount)) { CountOnline = _currentDriverCount; }
                            else { break; }
                        }
                    }
                }
            }
        }

        public void StopAccServer()
        {
            try { AccServerProcess.Kill(); }
            catch { }
        }
    }

    public enum ServerTypeEnum
    {
        PreQuali = 0,
        Practice = 1,
        Event = 2
    }
}
