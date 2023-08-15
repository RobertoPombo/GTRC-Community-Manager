using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Scripts;
using Enums;

using GTRC_Community_Manager;
using System.Windows.Controls.Primitives;

namespace Database
{
    public class Server : DatabaseObject<Server>
    {
        public static readonly string NoPath = MainWindow.currentDirectory;
        public static readonly string ForbiddenPath = MainWindow.dataDirectory;
        public static readonly string DefaultName = "Server #1";
        public static readonly string FileNameAccServerExe = "accServer.exe";
        [NotMapped][JsonIgnore] public static StaticDbField<Server> Statics { get; set; }
        static Server()
        {
            Statics = new StaticDbField<Server>(true)
            {
                Table = "Servers",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(Name) }, new List<string>() { nameof(Path) } },
                ToStringPropertiesNames = new List<string>() { nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Server() { This = this; Initialize(true, true); }
        public Server(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Server(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Season objSeason = new(false);
        [JsonIgnore][NotMapped] public Season ObjSeason { get { return Season.Statics.GetByID(seasonID); } }

        private string name = DefaultName;
        private string path = NoPath;
        private int seasonID = 0;
        private int serverType = 0;
        private int entrylistType = 0;
        private bool forceCarModel = false;
        private bool forceEntrylist = false;
        private bool writeBoP = false;

        public string Name
        {
            get { return name; }
            set
            {
                name = Basics.RemoveSpaceStartEnd(value ?? name);
                if (name == null || name == "") { name = DefaultName; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public string Path
        {
            get { return path; }
            set
            {
                path = Basics.ValidatedPath(NoPath, value ?? path);
                if (path == null || path == "") { path = NoPath; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        [NotMapped][JsonIgnore] public string AbsolutePath { get { return Basics.RelativePath2AbsolutePath(NoPath, Basics.ValidatedPath(NoPath, Path)); } set { } }

        [NotMapped][JsonIgnore] public string PathServer { get { return AbsolutePath + "server\\"; } set { } }

        [NotMapped][JsonIgnore] public string PathCfg { get { return PathServer + "cfg\\"; } set { } }

        [NotMapped][JsonIgnore] public string PathResults { get { return PathServer + "results\\"; } set { } }

        public string PathExe { get { return PathServer + FileNameAccServerExe; } set { } }

        public int SeasonID
        {
            get { return seasonID; }
            set
            {
                if (Season.Statics.IDList.Count == 0) { objSeason = new Season() { ID = 1 }; }
                if (!Season.Statics.ExistsID(value)) { objSeason = Season.Statics.IDList[0]; seasonID = objSeason.ID; }
                else { seasonID = value; objSeason = Season.Statics.GetByID(seasonID); }
            }
        }

        public int ServerType
        {
            get { return serverType; }
            set { if (Enum.IsDefined(typeof(ServerTypeEnum), value)) { serverType = value; } }
        }

        [JsonIgnore] public ServerTypeEnum ServerTypeEnum
        {
            get { return (ServerTypeEnum)serverType; }
            set { ServerType = (int)value; }
        }

        public int EntrylistType
        {
            get { return entrylistType; }
            set
            {
                if (Enum.IsDefined(typeof(EntrylistTypeEnum), value))
                {
                    entrylistType = value;
                    if (EntrylistTypeEnum == EntrylistTypeEnum.None) { ForceEntrylist = false; }
                    else if(EntrylistTypeEnum != EntrylistTypeEnum.Season) { ForceCarModel = false; }
                }
            }
        }

        [JsonIgnore] public EntrylistTypeEnum EntrylistTypeEnum
        {
            get { return (EntrylistTypeEnum)EntrylistType; }
            set { EntrylistType = (int)value; }
        }

        public bool ForceCarModel
        {
            get { return forceCarModel; }
            set { if (!value || EntrylistTypeEnum == EntrylistTypeEnum.Season) { forceCarModel = value; } }
        }

        public bool ForceEntrylist
        {
            get { return forceEntrylist; }
            set { if (!value || EntrylistTypeEnum != EntrylistTypeEnum.None) { forceEntrylist = value; } }
        }

        public bool WriteBoP
        {
            get { return writeBoP; }
            set { writeBoP = value; }
        }

        public static void PublishList()
        {
            ServerVM.UpdateListServers();
        }

        public override void SetNextAvailable()
        {
            int nr = 1;
            string defName = name;
            if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
            while (!IsUnique(0))
            {
                name = defName + " #" + nr.ToString();
                nr++; if (nr == int.MaxValue) { break; }
            }
            nr = 1;
            string defPath = path[..^1];
            if (Basics.SubStr(defPath, -3, 2) == " #") { defPath = Basics.SubStr(defPath, 0, defPath.Length - 3); }
            while (!IsUnique(1))
            {
                path = Basics.ValidatedPath(NoPath, defPath + " #" + nr.ToString());
                nr++; if (nr == int.MaxValue) { break; }
            }
        }

        public bool PathIsForbidden()
        {
            foreach (string currentPath in new List<string>() { AbsolutePath, PathServer, PathCfg, PathResults, PathExe })
            {
                if (Basics.PathIsParentOf(NoPath, ForbiddenPath, currentPath)) { return true; }
            }
            return false;
        }

        public bool PathExists()
        {
            foreach (string currentPath in new List<string>() { PathCfg, PathResults }) { if (!Directory.Exists(currentPath)) { return false; } }
            foreach (Server _server in List) { if (_server != this && _server.Path == path) { return false; } }
            return !PathIsForbidden();
        }

        public bool PathExistsExe()
        {
            if (!File.Exists(PathExe)) { return false; }
            return PathExists();
        }
    }
}
