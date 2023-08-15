using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Scripts;
using System.Windows.Controls;

namespace Database
{
    public class EventsEntries : DatabaseObject<EventsEntries>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<EventsEntries> Statics { get; set; }
        static EventsEntries()
        {
            Statics = new StaticDbField<EventsEntries>(true)
            {
                Table = "EventsEntries",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(EntryID), nameof(EventID) } },
                ToStringPropertiesNames = new List<string>() { nameof(EntryID), nameof(EventID) },
                PublishList = () => PublishList(),
            };
        }
        public EventsEntries() { This = this; Initialize(true, true); }
        public EventsEntries(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public EventsEntries(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Entry objEntry = new(false);
        private Event objEvent = new(false);
        [JsonIgnore][NotMapped] public Entry ObjEntry { get { return objEntry; } }
        [JsonIgnore][NotMapped] public Event ObjEvent { get { return objEvent; } }

        private int entryID = 0;
        private int eventID = 0;
        private DateTime signInDate = Basics.DateTimeMaxValue;
        private bool isOnEntrylist = false;
        private bool attended = false;
        private int ballast = new Entry(false).Ballast;
        private int restrictor = new Entry(false).Restrictor;
        private int category = new Entry(false).Category;
        private bool scorePoints = new Entry(false).ScorePoints;
        private int priority = int.MaxValue;

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } objEntry = Entry.Statics.GetByID(entryID); SetParentProps(); }
        }

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); } objEvent = Event.Statics.GetByID(EventID); SetParentProps(); }
        }

        public DateTime SignInDate
        {
            get { return signInDate; }
            set
            {
                if (RegisterState) { if (value >= Basics.DateTimeMinValue) { signInDate = value; } else { signInDate = Basics.DateTimeMinValue; } }
                else { signInDate = Basics.DateTimeMaxValue; }
            }
        }

        [JsonIgnore] public bool RegisterState
        {
            get { return objEntry.RegisterDate < objEvent.Date && objEntry.SignOutDate > objEvent.Date; }
        }

        [JsonIgnore] public bool SignInState
        {
            get { return SignInDate < ObjEvent.Date; }
        }

        public bool IsOnEntrylist
        {
            get { return isOnEntrylist; }
            set { isOnEntrylist = value; }
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
            set
            {
                if (value >= 0 && value <= 4 && category != value)
                {
                    category = value;
                    if (category == 3) { ScorePoints = true; } else if (category == 1) { ScorePoints = false; }
                }
            }
        }

        public bool ScorePoints
        {
            get { return scorePoints; }
            set { if (scorePoints != value) { scorePoints = value; if (scorePoints) { Category = 3; } else { Category = 1; } } }
        }

        public int Priority
        {
            get { return priority; }
            set
            {
                if (value < 0) { priority = 0; }
                else { priority = value; }
            }
        }

        [JsonIgnore] public bool IsBanned
        {
            get
            {
                List<DriversEntries> _driversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), EntryID);
                foreach (DriversEntries _driverEntry in _driversEntries) { if (!_driverEntry.ObjDriver.IsBanned(ObjEvent)) { return false; } }
                return true;
            }
        }

        [JsonIgnore] public int EventNr
        {
            get { return ObjEvent.EventNr; }
        }

        public static void PublishList()
        {
            //if (!Statics.DelayPL) { Statics.DeleteNotUnique(); }
        }

        public override void SetNextAvailable()
        {
            if (Statics.DelayPL) { return; }

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

            objEntry = Entry.Statics.GetByID(entryID);
            objEvent = Event.Statics.GetByID(EventID);
        }

        public void SetParentProps()
        {
            if (RegisterState && ObjEntry.Permanent) { SignInDate = Basics.DateTimeMinValue; }
            else { SignInDate = Basics.DateTimeMaxValue; }
            IsOnEntrylist = false;
            Attended = false;
            Ballast = ObjEntry.Ballast;
            Restrictor = ObjEntry.Restrictor;
            ScorePoints = ObjEntry.GetEntriesDatetimesByDate(ObjEvent.Date).ScorePoints;
        }

        public static EventsEntries GetAnyByUniqProp(int _entryID, int _eventID)
        {
            EventsEntries eventEntry = Statics.GetByUniqProp(new List<dynamic>() { _entryID, _eventID });
            if (!eventEntry.ReadyForList && _entryID >= Basics.ID0 && _eventID >= Basics.ID0)
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
            if (propName == nameof(EntryID) && id >= Basics.ID0)
            {
                List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), Entry.Statics.GetByID(id).SeasonID);
                foreach (Event _event in listEvents)
                {
                    EventsEntries eventEntry = GetAnyByUniqProp(id, _event.ID);
                    if (eventEntry.ReadyForList) { eventsEntries.Add(eventEntry); }
                }
            }
            else if (propName == nameof(EventID) && id >= Basics.ID0)
            {
                List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), Event.Statics.GetByID(id).SeasonID);
                foreach (Entry _entry in listEntries)
                {
                    EventsEntries eventEntry = GetAnyByUniqProp(_entry.ID, id);
                    if (eventEntry.ReadyForList) { eventsEntries.Add(eventEntry); }
                }
            }
            else if ((propName == nameof(Event.SeasonID) || propName == nameof(Entry.SeasonID)) && id >= Basics.ID0)
            {
                List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), id);
                List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), id);
                foreach (Event _event in listEvents)
                {
                    foreach (Entry _entry in listEntries)
                    {
                        EventsEntries eventEntry = GetAnyByUniqProp(_entry.ID, _event.ID);
                        if (eventEntry.ReadyForList) { eventsEntries.Add(eventEntry); }
                    }
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
                    if (_list[eventEntryNr1].ObjEvent.Date > _list[eventEntryNr2].ObjEvent.Date)
                    {
                        (_list[eventEntryNr2], _list[eventEntryNr1]) = (_list[eventEntryNr1], _list[eventEntryNr2]);
                    }
                }
            }
            return _list;
        }
    }
}
