using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Enums;
using Scripts;

namespace Database
{
    public class ResultsFile : DatabaseObject<ResultsFile>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<ResultsFile> Statics { get; set; }
        static ResultsFile()
        {
            Statics = new StaticDbField<ResultsFile>(true)
            {
                Table = "ResultsFiles",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(ServerID), nameof(Date) } },
                ToStringPropertiesNames = new List<string>() { nameof(ServerID), nameof(Date), nameof(TrackID) },
                PublishList = () => PublishList()
            };
        }
        public ResultsFile() { This = this; Initialize(true, true); }
        public ResultsFile(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public ResultsFile(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int serverID = 0;
        private DateTime date = Event.DateTimeMinValue;
        private int sessionType = 0;
        private int trackID = Basics.ID0;
        private int seasonID = 0;
        private int serverType = 0;

        public int ServerID
        {
            get { return serverID; }
            set { serverID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public DateTime Date
        {
            get { return date; }
            set { date = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public int SessionType
        {
            get { return sessionType; }
            set { if (Enum.IsDefined(typeof(SessionTypeEnum), value)) { sessionType = value; } }
        }

        [JsonIgnore] public SessionTypeEnum SessionTypeEnum
        {
            get { return (SessionTypeEnum)sessionType; }
            set { sessionType = (int)value; }
        }

        public int TrackID
        {
            get { return trackID; }
            set
            {
                if (Track.Statics.IDList.Count == 0) { _ = new Track() { ID = 1 }; }
                if (!Track.Statics.ExistsID(value)) { value = Track.Statics.IDList[0].ID; }
                trackID = value;
            }
        }

        public int SeasonID
        {
            get { return seasonID; }
            set
            {
                if (Season.Statics.IDList.Count == 0) { _ = new Season() { ID = 1 }; }
                if (!Season.Statics.ExistsID(value)) { value = Season.Statics.IDList[0].ID; }
                seasonID = value;
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

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int serverNr = 0;
            List<Server> _idListServer = Server.Statics.IDList;
            if (_idListServer.Count == 0) { _ = new Server() { ID = 1 }; _idListServer = Server.Statics.IDList; }
            Server _server = Server.Statics.GetByID(serverID);
            if (_server.ReadyForList) { serverNr = Server.Statics.IDList.IndexOf(_server); } else { serverID = _idListServer[0].ID; }
            int startValueServer = serverNr;

            if (date < Event.DateTimeMinValue) { date = Event.DateTimeMinValue; }
            else if (date > Event.DateTimeMaxValue) { date = Event.DateTimeMaxValue; }
            DateTime startValue = date;

            while (!IsUnique())
            {
                if (date < Event.DateTimeMaxValue) { date = date.AddDays(1); } else { date = Event.DateTimeMinValue; }
                if (date == startValue)
                {
                    if (serverNr + 1 < _idListServer.Count) { serverNr += 1; } else { serverNr = 0; }
                    serverID = _idListServer[serverNr].ID;
                    if (serverNr == startValueServer) { break; }
                }
            }
        }
    }
}
