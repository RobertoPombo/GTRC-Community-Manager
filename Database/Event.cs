using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class Event : DatabaseObject<Event>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<Event> Statics { get; set; }
        public static readonly DateTime DateTimeMinValue = DateTime.MinValue.AddYears(1800);
        public static readonly DateTime DateTimeMaxValue = DateTime.MaxValue.AddDays(-1);
        public static readonly string DefaultName = "Event #1";

        private DateTime eventDate = DateTime.Now;
        private int trackID = 0;
        private string name = DefaultName;

        static Event()
        {
            Statics = new StaticDbField<Event>(true)
            {
                Table = "Events",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "EventDate" }, new List<string>() { "Name" } },
                ToStringPropertiesNames = new List<string>() { "Name" },
                ListSetter = () => ListSetter()
            };
        }

        public Event() { This = this; Initialize(true, true); }
        public Event(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Event(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        [JsonIgnore] public int EventNr
        {
            get { if (List.Contains(this)) { SortByDate(); return List.IndexOf(this); } else return -1; }
        }

        public DateTime EventDate
        {
            get { return eventDate; }
            set
            {
                if (value < DateTimeMinValue) { eventDate = DateTimeMinValue; }
                else if (value > DateTimeMaxValue) { eventDate = DateTimeMaxValue; }
                else { eventDate = value; }
                if (ReadyForList) { SetNextAvailable(); }
                if (Statics.IDList.Contains(this)) { ListSetter(); }
            }
        }

        public int TrackID
        {
            get { return trackID; }
            set
            {
                if (Track.Statics.IDList.Count == 0) { new Track() { ID = 1 }; }
                if (!Track.Statics.ExistsID(value)) { value = Track.Statics.IDList[0].ID; }
                trackID = value;
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

        public static void ListSetter()
        {
            PreSeasonVM.UpdateListEvents();
            EventsEntries.Statics.PendingSync = true;
            SortByDate();
        }

        public override void SetNextAvailable()
        {
            DateTime startValue = eventDate;
            while (!IsUnique(0))
            {
                if (eventDate < DateTimeMaxValue) { eventDate.AddDays(1); } else { eventDate = DateTimeMinValue; }
                if (eventDate == startValue) { break; }
            }
            int nr = 1;
            string defName = name;
            if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
            while (!IsUnique(1))
            {
                name = defName + " #" + nr.ToString();
                nr++; if (nr == int.MaxValue) { break; }
            }
        }

        public static void SortByDate()
        {
            for (int _eventID1 = 0; _eventID1 < Statics.List.Count - 1; _eventID1++)
            {
                for (int _eventID2 = _eventID1; _eventID2 < Statics.List.Count; _eventID2++)
                {
                    if (Statics.List[_eventID1].EventDate > Statics.List[_eventID2].EventDate)
                    {
                        (Statics.List[_eventID2], Statics.List[_eventID1]) = (Statics.List[_eventID1], Statics.List[_eventID2]);
                    }
                }
            }
        }

        public static Event GetEventByCurrentDate()
        {
            SortByDate();
            DateTime _now = DateTime.Now;
            Event nextEvent;
            foreach (Event _event in Statics.List) { nextEvent = _event; if (_event.EventDate > _now) { return nextEvent; } }
            return new Event(false);
        }

        //TEMP: Converter
        [NotMapped] public string TrackID2 { set { TrackID = Track.Statics.GetByUniqueProp(value).ID; } }
    }
}
