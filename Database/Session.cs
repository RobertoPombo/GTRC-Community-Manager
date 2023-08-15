using Enums;
using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Database
{
    public class Session : DatabaseObject<Session>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<Session> Statics { get; set; }
        static Session()
        {
            Statics = new StaticDbField<Session>(true)
            {
                Table = "Sessions",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(EventID), nameof(ServerID), nameof(StartTime) } },
                ToStringPropertiesNames = new List<string>() { nameof(EventID), nameof(EventID) },
                PublishList = () => PublishList()
            };
        }
        public Session() { This = this; Initialize(true, true); }
        public Session(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Session(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Event objEvent = new(false);
        private Server objServer = new(false);
        private PointsSystem objPointsSystem = new(false);
        [JsonIgnore][NotMapped] public Event ObjEvent { get { return Event.Statics.GetByID(eventID); } }
        [JsonIgnore][NotMapped] public Server ObjServer { get { return Server.Statics.GetByID(serverID); } }
        [JsonIgnore][NotMapped] public PointsSystem ObjPointsSystem { get { return PointsSystem.Statics.GetByID(pointsSystemID); } }
        [JsonIgnore][NotMapped] public Session? ObjGridSession { get { if (gridSessionID == Basics.NoID) { return null; } else { return Statics.GetByID(gridSessionID); } } }

        private int eventID = 0;
        private int serverID = 0;
        private TimeSpan startTime = TimeSpan.Zero;
        private TimeSpan duration = TimeSpan.FromMinutes(1);
        private TimeSpan ingameDuration = TimeSpan.FromMinutes(1);
        private int serverType = 0;
        private int sessionType = 0;
        private int pointsSystemID = Basics.ID0;
        private int gridSessionID = Basics.NoID;
        private int reverseGridFrom = 1;
        private int reverseGridTo = 1;
        private int ingameStartTime = 0;
        private int dayOfWeekend = 1;
        private int timeMultiplier = 1;
        private bool attendanceObligated = false;
        private int entrylistType = 0;
        private bool forceEntrylist = false;
        private bool forceDriverInfo = false;
        private bool forceCarModel = false;
        private bool writeBoP = false;
        private string serverName = "unnamed server";
        private string driverPassword = "123";
        private string spectatorPassword = "123";
        private string adminPassword = "123";

        [JsonIgnore] public int SessionNr
        {
            get
            {
                if (List.Contains(this) && ID != Basics.NoID)
                {
                    List<Session> _list = Event.GetSessions(EventID);
                    int nr = 1;
                    foreach (Session _session in _list) { if (_session == this) { return nr; } nr++; }
                    return Basics.NoID;
                }
                else { return Basics.NoID; }
            }
        }

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); } objEvent = Event.Statics.GetByID(eventID); }
        }

        public int ServerID
        {
            get { return serverID; }
            set { serverID = value; if (ReadyForList) { SetNextAvailable(); } objServer = Server.Statics.GetByID(serverID); }
        }

        public TimeSpan StartTime
        {
            get { return startTime; }
            set { if (value >= TimeSpan.Zero && value <= TimeSpan.FromHours(24)) { startTime = value; } if (ReadyForList) { SetNextAvailable(); } }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set
            {
                if (value != duration && value > TimeSpan.Zero) { duration = value; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public TimeSpan IngameDuration
        {
            get { return ingameDuration; }
            set { if (value != ingameDuration && value > TimeSpan.Zero) { ingameDuration = value; if (SessionTypeEnum == SessionTypeEnum.Race) { Duration = IngameDuration; } } }
        }

        public int ServerType
        {
            get { return serverType; }
            set { if (Enum.IsDefined(typeof(ServerTypeEnum), value)) { serverType = value; } }
        }

        [JsonIgnore] public ServerTypeEnum ServerTypeEnum
        {
            get { return (ServerTypeEnum)serverType; }
            set { serverType = (int)value; }
        }

        public int SessionType
        {
            get { return sessionType; }
            set { if (Enum.IsDefined(typeof(SessionTypeEnum), value)) { sessionType = value; if (SessionTypeEnum == SessionTypeEnum.Race) { Duration = IngameDuration; } } }
        }

        [JsonIgnore] public SessionTypeEnum SessionTypeEnum
        {
            get { return (SessionTypeEnum)sessionType; }
            set { sessionType = (int)value; }
        }

        [JsonIgnore] public int SessionNrOfThisType
        {
            get
            {
                if (List.Contains(this) && ID != Basics.NoID)
                {
                    List<Session> _list = Event.GetSessions(EventID);
                    int nr = 1;
                    foreach (Session _session in _list) { if (_session == this) { return nr; } if (SessionType == _session.SessionType) { nr++; } }
                    return Basics.NoID;
                }
                else { return Basics.NoID; }
            }
        }

        public int PointsSystemID
        {
            get { return pointsSystemID; }
            set
            {
                if (PointsSystem.Statics.IDList.Count == 0) { objPointsSystem = new PointsSystem() { ID = 1 }; }
                if (!PointsSystem.Statics.ExistsID(value)) { objPointsSystem = PointsSystem.Statics.IDList[0]; pointsSystemID = objPointsSystem.ID; }
                else { pointsSystemID = value; objPointsSystem = PointsSystem.Statics.GetByID(pointsSystemID); }
            }
        }

        public int GridSessionID
        {
            get { return gridSessionID; }
            set
            {
                gridSessionID = Basics.NoID;
                if (value != ID && SessionTypeEnum == SessionTypeEnum.Race)
                {
                    Session _gridSession = Statics.GetByID(value);
                    if (_gridSession.ReadyForList && _gridSession.objEvent.SeasonID == ObjEvent.SeasonID && StartTime > _gridSession.StartTime + _gridSession.Duration)
                    {
                        gridSessionID = value;
                    }
                }
            }
        }

        public int ReverseGridFrom
        {
            get { return reverseGridFrom; }
            set { if (value > 0 && value < ObjEvent.ObjTrack.ServerSlotsCount) { reverseGridFrom = value; reverseGridTo = Math.Max(reverseGridFrom, reverseGridTo); } }
        }

        public int ReverseGridTo
        {
            get { return reverseGridTo; }
            set { if (value >= ReverseGridFrom && value <= ObjEvent.ObjTrack.ServerSlotsCount) { reverseGridTo = value; } }
        }

        public int IngameStartTime
        {
            get { return ingameStartTime; }
            set { if (value >= 0 && value < 24) { ingameStartTime = value; } }
        }

        public int DayOfWeekend
        {
            get { return dayOfWeekend; }
            set { if (Enum.IsDefined(typeof(DayOfWeekendEnum), value)) { dayOfWeekend = value; } }
        }

        [JsonIgnore] public DayOfWeekendEnum DayOfWeekendEnum
        {
            get { return (DayOfWeekendEnum)dayOfWeekend; }
            set { dayOfWeekend = (int)value; }
        }

        public int TimeMultiplier
        {
            get { return timeMultiplier; }
            set { if (value >= 0 && value <= 24) { timeMultiplier = value; } }
        }

        public bool AttendanceObligated
        {
            get { return attendanceObligated; }
            set { attendanceObligated = value; }
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
                    else if (EntrylistTypeEnum != EntrylistTypeEnum.Season) { ForceCarModel = false; }
                }
            }
        }

        [JsonIgnore] public EntrylistTypeEnum EntrylistTypeEnum
        {
            get { return (EntrylistTypeEnum)EntrylistType; }
            set { EntrylistType = (int)value; }
        }

        public bool ForceEntrylist
        {
            get { return forceEntrylist; }
            set { if (!value || EntrylistTypeEnum != EntrylistTypeEnum.None) { forceEntrylist = value; } }
        }

        public bool ForceDriverInfo
        {
            get { return forceDriverInfo; }
            set { if (!value || EntrylistTypeEnum != EntrylistTypeEnum.None) { forceDriverInfo = value; } }
        }

        public bool ForceCarModel
        {
            get { return forceCarModel; }
            set { if (!value || EntrylistTypeEnum == EntrylistTypeEnum.Season) { forceCarModel = value; } }
        }

        public bool WriteBoP
        {
            get { return writeBoP; }
            set { writeBoP = value; }
        }

        public string ServerName
        {
            get { return serverName; }
            set { value = Basics.RemoveSpaceStartEnd(value); if (value.Length > 0) { serverName = value; } }
        }

        public string DriverPassword
        {
            get { return driverPassword; }
            set { value = Basics.RemoveSpaceStartEnd(value); if (value.Length >= 3) { driverPassword = value; } }
        }

        public string SpectatorPassword
        {
            get { return spectatorPassword; }
            set { value = Basics.RemoveSpaceStartEnd(value); if (value.Length >= 3) { spectatorPassword = value; } }
        }

        public string AdminPassword
        {
            get { return adminPassword; }
            set { value = Basics.RemoveSpaceStartEnd(value); if (value.Length >= 3) { adminPassword = value; } }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            /*List<Event> _idListEvent = Event.Statics.IDList;
            if (_idListEvent.Count == 0) { _ = new Event() { ID = 1 }; _idListEvent = Event.Statics.IDList; }
            Event _event = Event.Statics.GetByID(eventID);
            if (!_event.ReadyForList) { eventID = _idListEvent[0].ID; }

            List<Server> _idListServer = Server.Statics.IDList;
            if (_idListServer.Count == 0) { _ = new Server() { ID = 1 }; _idListServer = Server.Statics.IDList; }
            Server _server = Server.Statics.GetByID(serverID);
            if (!_server.ReadyForList) { serverID = _idListServer[0].ID; }*/

            bool serverIsFree = false;
            while (!serverIsFree)
            {
                serverIsFree = true;
                if (Basics.DateTimeMaxValue.Subtract(ObjEvent.Date) <= StartTime + Duration) { startTime = TimeSpan.Zero; duration = TimeSpan.FromMinutes(1); break; }
                DateTime startDate0 = ObjEvent.Date.Add(StartTime);
                DateTime endDate0 = startDate0.Add(Duration);
                foreach (Session _session in List)
                {
                    DateTime startDate1 = _session.ObjEvent.Date.Add(_session.StartTime);
                    DateTime endDate1 = startDate1.Add(_session.Duration);
                    if (_session.ID != Basics.NoID && _session.ID != ID && ServerID == _session.ServerID && !(startDate0 > endDate1 || startDate1 > endDate0))
                    {
                        serverIsFree = false;
                        duration = TimeSpan.FromMinutes(1);
                        startTime = StartTime.Add(TimeSpan.FromHours(1));
                        break;
                    }
                }
            }
            if (SessionTypeEnum == SessionTypeEnum.Race) { IngameDuration = Duration; }
            List<Session> list = Event.GetSessions(EventID);
            foreach(Session _session in list) { _session.GridSessionID = _session.GridSessionID; }

            objEvent = Event.Statics.GetByID(eventID);
            objServer = Server.Statics.GetByID(serverID);
        }

        public static List<Session> SortByStartTime(List<Session> _list)
        {
            var linqList = from _session in _list
                           orderby _session.ObjEvent.Date, _session.StartTime
                           select _session;
            return linqList.Cast<Session>().ToList();
        }
    }
}
