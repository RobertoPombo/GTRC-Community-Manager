using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

using GTRC_Community_Manager;
using System.Windows.Controls;

namespace Database
{
    public class Event : DatabaseObject<Event>
    {
        public static readonly string DefaultName = "Event #1";
        [NotMapped][JsonIgnore] public static StaticDbField<Event> Statics { get; set; }
        static Event()
        {
            Statics = new StaticDbField<Event>(true)
            {
                Table = "Events",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(SeasonID), nameof(Date) },
                    new List<string>() { nameof(SeasonID), nameof(Name) } },
                ToStringPropertiesNames = new List<string>() { nameof(SeasonID), nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Event() { This = this; Initialize(true, true); }
        public Event(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Event(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Season objSeason = new(false);
        private Track objTrack = new(false);
        [JsonIgnore][NotMapped] public Season ObjSeason { get { return Season.Statics.GetByID(seasonID); } }
        [JsonIgnore][NotMapped] public Track ObjTrack { get { return Track.Statics.GetByID(trackID); } }

        private int seasonID = 0;
        private DateTime date = DateTime.Now;
        private int trackID = 0;
        private string name = DefaultName;
        private int ambientTemp = 20;
        private int cloudLevel = 0;
        private int rainLevel = 0;
        private int weatherRandomness = 0;
        private bool fixedConditions = false;

        [JsonIgnore] public int EventNr
        {
            get
            {
                if (List.Contains(this) && ID != Basics.NoID)
                {
                    SortByDate();
                    int nr = 1;
                    foreach (Event _event in Statics.List)
                    {
                        if (_event == this) { return nr; }
                        if (_event.SeasonID == SeasonID) { nr++; }
                    }
                    return Basics.NoID;
                }
                else { return Basics.NoID; }
            }
        }

        public int SeasonID
        {
            get { return seasonID; }
            set { seasonID = value; if (ReadyForList) { SetNextAvailable(); } objSeason = Season.Statics.GetByID(seasonID); }
        }

        public DateTime Date
        {
            get { return date; }
            set { date = value; if (ReadyForList) { SetNextAvailable(); } if (Statics.IDList.Contains(this)) { PublishList(); } }
        }

        public int TrackID
        {
            get { return trackID; }
            set
            {
                if (Track.Statics.IDList.Count == 0) { objTrack = new Track() { ID = 1 }; }
                if (!Track.Statics.ExistsID(value)) { objTrack = Track.Statics.IDList[0]; trackID = objTrack.ID; }
                else { trackID = value; objTrack = Track.Statics.GetByID(trackID); }
            }
        }

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

        public int AmbientTemp
        {
            get { return ambientTemp; }
            set { ambientTemp = value; }
        }

        public int CloudLevel
        {
            get { return cloudLevel; }
            set { if (value < 0) { cloudLevel = 0; } else if (value > 100) { cloudLevel = 100; } else { cloudLevel = value; } }
        }

        public int RainLevel
        {
            get { return rainLevel; }
            set { if (value < 0) { rainLevel = 0; } else if (value > 100) { rainLevel = 100; } else { rainLevel = value; } }
        }

        public int WeatherRandomness
        {
            get { return weatherRandomness; }
            set { if (value < 0) { weatherRandomness = 0; } else if (value > 7) { weatherRandomness = 7; } else { weatherRandomness = value; } }
        }

        public bool FixedConditions
        {
            get { return fixedConditions; }
            set { fixedConditions = value; }
        }

        public static void PublishList()
        {
            SortByDate();
            PreSeasonVM.UpdateListEvents();
            SessionsVM.UpdateListEvents();
        }

        public override void SetNextAvailable()
        {
            int seasonNr = 0;
            List<Season> _idListSeason = Season.Statics.IDList;
            if (_idListSeason.Count == 0) { _ = new Season() { ID = 1 }; _idListSeason = Season.Statics.IDList; }
            Season _season = Season.Statics.GetByID(seasonID);
            if (_season.ReadyForList) { seasonNr = Season.Statics.IDList.IndexOf(_season); } else { seasonID = _idListSeason[0].ID; }
            int startValueSeason = seasonNr;

            if (date < Basics.DateTimeMinValue) { date = Basics.DateTimeMinValue; }
            else if (date > Basics.DateTimeMaxValue.AddDays(-1)) { date = Basics.DateTimeMaxValue; }
            DateTime startValue = date;

            while (!IsUnique(0))
            {
                if (date < Basics.DateTimeMaxValue.AddDays(-1)) { date = date.AddDays(1); } else { date = Basics.DateTimeMinValue.AddDays(-1); }
                if (date == startValue)
                {
                    if (seasonNr + 1 < _idListSeason.Count) { seasonNr += 1; } else { seasonNr = 0; }
                    seasonID = _idListSeason[seasonNr].ID;
                    if (seasonNr == startValueSeason) { break; }
                }
            }

            int nr = 1;
            string defName = name;
            if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
            while (!IsUnique(1))
            {
                name = defName + " #" + nr.ToString();
                nr++; if (nr == int.MaxValue)
                {
                    if (seasonNr + 1 < _idListSeason.Count) { seasonNr += 1; } else { seasonNr = 0; }
                    seasonID = _idListSeason[seasonNr].ID;
                    if (seasonNr == startValueSeason) { break; }
                }
            }

            objSeason = Season.Statics.GetByID(seasonID);
        }

        public static void SortByDate()
        {
            for (int _eventID1 = 0; _eventID1 < Statics.List.Count - 1; _eventID1++)
            {
                for (int _eventID2 = _eventID1; _eventID2 < Statics.List.Count; _eventID2++)
                {
                    Event _event1 = Statics.List[_eventID1];
                    Event _event2 = Statics.List[_eventID2];
                    if ((_event1.SeasonID > _event2.SeasonID) || ((_event1.SeasonID == _event2.SeasonID) && (_event1.Date > _event2.Date)))
                    {
                        (Statics.List[_eventID2], Statics.List[_eventID1]) = (Statics.List[_eventID1], Statics.List[_eventID2]);
                    }
                }
            }
        }

        public static List<Event> SortByDate(List<Event> listEvents)
        {
            for (int _eventID1 = 0; _eventID1 < listEvents.Count - 1; _eventID1++)
            {
                for (int _eventID2 = _eventID1; _eventID2 < listEvents.Count; _eventID2++)
                {
                    Event _event1 = listEvents[_eventID1];
                    Event _event2 = listEvents[_eventID2];
                    if ((_event1.SeasonID > _event2.SeasonID) || ((_event1.SeasonID == _event2.SeasonID) && (_event1.Date > _event2.Date)))
                    {
                        (listEvents[_eventID2], listEvents[_eventID1]) = (listEvents[_eventID1], listEvents[_eventID2]);
                    }
                }
            }
            return listEvents;
        }

        public static Event GetNextEvent(int _seasonID, DateTime _date)
        {
            Event nextEvent = new(false);
            List<Event> eventList = SortByDate(Statics.GetBy(nameof(SeasonID), _seasonID));
            foreach (Event _event in eventList) { nextEvent = _event; if (_event.Date > _date) { return nextEvent; } }
            return nextEvent;
        }

        public List<Session> GetSessions() { return GetSessions(ID); }
        public static List<Session> GetSessions(int _eventID) { return Session.SortByStartTime(Session.Statics.GetBy(nameof(Session.EventID), _eventID)); }
    }
}
