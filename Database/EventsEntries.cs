using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace GTRCLeagueManager.Database
{
    public class EventsEntries : DatabaseObject<EventsEntries>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<EventsEntries> Statics { get; set; }
        static EventsEntries()
        {
            Statics = new StaticDbField<EventsEntries>(true)
            {
                Table = "EventsEntries",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "EntryID", "EventID" } },
                ToStringPropertiesNames = new List<string>() { "EntryID", "EventID" },
                PublishList = () => PublishList(),
            };
        }
        public EventsEntries() { This = this; Initialize(true, true); }
        public EventsEntries(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public EventsEntries(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int entryID = 0;
        private int eventID = 0;
        private DateTime signindate = Event.DateTimeMaxValue;
        private bool isonentrylist = false;
        private bool attended = false;
        private int ballast = new Entry(false).Ballast;
        private int restrictor = new Entry(false).Restrictor;
        private int category = new Entry(false).Category;
        private bool scorepoints = new Entry(false).ScorePoints;
        private int carID = 0;
        private DateTime carchangedate = Event.DateTimeMinValue;

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); SetParentProps(); } }
        }

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); SetParentProps(); } }
        }

        public DateTime SignInDate
        {
            get { return signindate; }
            set { signindate = value; }
        }

        [JsonIgnore] public bool SignInState
        {
            get { return SignInDate < Event.Statics.GetByID(EventID).EventDate; }
        }

        public bool IsOnEntrylist
        {
            get { return isonentrylist; }
            set { isonentrylist = value; }
        }

        public bool Attended
        {
            get { return attended; }
            set { attended = value; }
        }

        public int Ballast
        {
            get { return ballast; }
            set
            {
                if (value < 0) { ballast = 0; }
                else if (value > 30) { ballast = 30; }
                else { ballast = value; }
            }
        }

        public int Restrictor
        {
            get { return restrictor; }
            set
            {
                if (value < 0) { restrictor = 0; }
                else if (value > 20) { restrictor = 20; }
                else { restrictor = value; }
            }
        }

        public int Category
        {
            get { return category; }
            set { if (value >= 0 && value <= 4) { category = value; } }
        }

        public bool ScorePoints
        {
            get { return scorepoints; }
            set { scorepoints = value; }
        }

        public int CarID
        {
            get { return carID; }
            set
            {
                if (Car.Statics.IDList.Count == 0) { _ = new Car() { ID = 1 }; }
                if (!Car.Statics.ExistsID(value)) { value = Car.Statics.IDList[0].ID; }
                carID = value;
            }
        }

        public DateTime CarChangeDate
        {
            get { return carchangedate; }
            set { carchangedate = value; }
        }

        [JsonIgnore] public int EventNr
        {
            get { return Event.Statics.GetByID(EventID).EventNr; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            List<Entry> _idListEntry = Entry.Statics.IDList;
            if (_idListEntry.Count == 0) { Entry _newEntry = new() { ID = 1 }; _idListEntry.Add(_newEntry); }
            Entry _entry = Entry.Statics.GetByID(entryID);
            int entryNr = 0;
            if (_entry.ReadyForList) { entryNr = _idListEntry.IndexOf(_entry); } else { _entry = _idListEntry[entryNr]; entryID = _entry.ID; }
            int startValueEntry = entryNr;

            var linqListEntry = from _lingEntry in Entry.Statics.List
                           where _lingEntry.SeasonID == _entry.SeasonID && _lingEntry.ID != Basics.NoID
                           select _lingEntry;
            _idListEntry = linqListEntry.Cast<Entry>().ToList();
            var linqListEvent = from _lingEvent in Event.Statics.List
                                where _lingEvent.SeasonID == _entry.SeasonID && _lingEvent.ID != Basics.NoID
                                select _lingEvent;
            List<Event> _idListEvent = linqListEvent.Cast<Event>().ToList();

            if (_idListEvent.Count == 0) { Event _newEvent = new() { ID = 1, SeasonID = _entry.SeasonID }; _idListEvent.Add(_newEvent); }
            Event _event = Event.Statics.GetByID(eventID);
            int eventNr = 0;
            if (_event.ReadyForList && _idListEvent.Contains(_event)) { eventNr = _idListEvent.IndexOf(_event); } else { eventID = _idListEvent[eventNr].ID; }
            int startValueEvent = eventNr;

            while (!IsUnique())
            {
                if (eventNr + 1 < _idListEvent.Count) { eventNr += 1; } else { eventNr = 0; }
                eventID = _idListEvent[eventNr].ID;
                if (eventNr == startValueEvent)
                {
                    if (entryNr + 1 < _idListEntry.Count) { entryNr += 1; } else { entryNr = 0; }
                    entryID = _idListEntry[entryNr].ID;
                    if (entryNr == startValueEntry) { break; }
                }
            }
        }

        public void SetParentProps()
        {
            Entry _entry = Entry.Statics.GetByID(EntryID);
            Event _event = Event.Statics.GetByID(EventID);
            if (_entry.RegisterDate < _event.EventDate && _entry.SignOutDate > _event.EventDate && _entry.ScorePoints) { SignInDate = Event.DateTimeMinValue; }
            else { SignInDate = Event.DateTimeMaxValue; }
            IsOnEntrylist = false;
            Attended = false;
            Ballast = _entry.Ballast;
            Restrictor = _entry.Restrictor;
            Category = _entry.Category;
            ScorePoints = _entry.ScorePoints;
            CarID = _entry.CarID;
            CarChangeDate = _entry.RegisterDate;
        }

        public static EventsEntries GetAnyByUniqProp(int _entryID, int _eventID)
        {
            EventsEntries eventEntry = Statics.GetByUniqProp(new List<dynamic>() { _entryID, _eventID });
            if (!eventEntry.ReadyForList && _entryID != Basics.NoID && _eventID != Basics.NoID)
            {
                eventEntry.EntryID = _entryID;
                eventEntry.EventID = _eventID;
                eventEntry.ListAdd();
            }
            return eventEntry;
        }

        public static List<EventsEntries> GetAnyBy(string propName, int id)
        {
            List<EventsEntries> eventsEntries = new();
            if (propName == nameof(EntryID) && id != Basics.NoID)
            {
                List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), Entry.Statics.GetByID(id).SeasonID);
                foreach (Event _event in listEvents)
                {
                    EventsEntries eventEntry = GetAnyByUniqProp(id, _event.ID);
                    if (eventEntry.ReadyForList) { eventsEntries.Add(eventEntry); }
                }
            }
            else if (propName == nameof(EventID) && id != Basics.NoID)
            {
                List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), Event.Statics.GetByID(id).SeasonID);
                foreach (Entry _entry in listEntries)
                {
                    EventsEntries eventEntry = GetAnyByUniqProp(_entry.ID, id);
                    if (eventEntry.ReadyForList) { eventsEntries.Add(eventEntry); }
                }
            }
            return eventsEntries;
        }

        public static List<EventsEntries> SortByDate(List<EventsEntries> _list)
        {
            for (int eventEntryNr1 = 0; eventEntryNr1 < _list.Count - 1; eventEntryNr1++)
            {
                for (int eventEntryNr2 = eventEntryNr1; eventEntryNr2 < _list.Count; eventEntryNr2++)
                {
                    Event event1 = Event.Statics.GetByID(_list[eventEntryNr1].EventID);
                    Event event2 = Event.Statics.GetByID(_list[eventEntryNr2].EventID);
                    if (event1.EventDate > event2.EventDate)
                    {
                        (_list[eventEntryNr2], _list[eventEntryNr1]) = (_list[eventEntryNr1], _list[eventEntryNr2]);
                    }
                }
            }
            return _list;
        }

        public static EventsEntries GetLatestEventsEntries(Entry _entry, DateTime carChangeDateMax)
        {
            List<EventsEntries> eventsEntriesList = Statics.GetBy(nameof(EntryID), _entry.ID);
            var linqList = from _eventsEntries in eventsEntriesList
                           orderby Event.Statics.GetByID(_eventsEntries.ID).EventDate
                           select _eventsEntries;
            eventsEntriesList = linqList.Cast<EventsEntries>().ToList();
            EventsEntries eventsEntries = new(false);
            foreach (EventsEntries _eventsEntries in eventsEntriesList)
            {
                if (_eventsEntries.CarChangeDate < carChangeDateMax)
                {
                    eventsEntries = _eventsEntries;
                    if (Event.Statics.GetByID(_eventsEntries.EventID).EventDate > carChangeDateMax) { break; }
                }
                else { break; }
            }
            return eventsEntries;
        }

        //TEMP: Converter
        [NotMapped] public DateTime EventDate { set { EventID = Event.Statics.GetByUniqProp(new List<dynamic>() { 4, value }).ID; } }
        [NotMapped] public string RaceNumber { set { EntryID = Entry.Statics.GetByUniqProp(new List<dynamic>() { 4, value }).ID; } }
        [NotMapped] public string CarID2 { set { CarID = Car.Statics.GetByUniqProp(value).ID; } }
    }
}
