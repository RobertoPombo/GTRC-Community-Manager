using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class EventsEntries : DatabaseObject<EventsEntries>
    {
        public static StaticDbField<EventsEntries> Statics = new StaticDbField<EventsEntries>(true)
        {
            Table = "EventsEntries",
            UniquePropertiesNames = new List<List<string>>() { new List<string>() { "EntryID", "EventID" } },
            ToStringPropertiesNames = new List<string>() { "EntryID", "EventID" },
            ListSetter = () => ListSetter(),
            DoSync = () => SyncDelete()
        };

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

        public EventsEntries() { This = this; Initialize(true, true); }
        public EventsEntries(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public EventsEntries(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); InitializeProperties(); } }
        }

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); InitializeProperties(); } }
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
                if (Car.Statics.IDList.Count == 0) { new Car() { ID = 1 }; }
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

        public static void ListSetter() { }

        public override void SetNextAvailable()
        {
            int eventNr = 0;
            List<Event> _idListEvent = Event.Statics.IDList;
            if (_idListEvent.Count == 0) { new Event() { ID = 1 }; _idListEvent = Event.Statics.IDList; }
            Event _event = Event.Statics.GetByID(eventID);
            if (_event.ReadyForList) { eventNr = Event.Statics.IDList.IndexOf(_event); } else { eventID = _idListEvent[0].ID; }
            int startValueEvent = eventNr;

            int entryNr = 0;
            List<Entry> _idListEntry = Entry.Statics.IDList;
            if (_idListEntry.Count == 0) { new Entry() { ID = 1 }; _idListEntry = Entry.Statics.IDList; }
            Entry _entry = Entry.Statics.GetByID(entryID);
            if (_entry.ReadyForList) { entryNr = Entry.Statics.IDList.IndexOf(_entry); } else { entryID = _idListEntry[0].ID; }
            int startValueEntry = entryNr;

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

        public void InitializeProperties()
        {
            Event _event = Event.Statics.GetByID(EventID);
            Entry _entry = Entry.Statics.GetByID(EntryID);
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

        public static void Sync()
        {
            foreach (Event _event in Event.Statics.List)
            {
                foreach (Entry _entry in Entry.Statics.List)
                {
                    if ( _entry.ID != Basics.NoID && _event.ID != Basics.NoID && !Statics.ExistsUniqueProp(new List<dynamic>() { _entry.ID, _event.ID }))
                    {
                        new EventsEntries() { EntryID = _entry.ID, EventID = _event.ID };
                    }
                }
            }
        }

        public static void SyncDelete()
        {
            List<EventsEntries> iterateEventsEntries = new List<EventsEntries>();
            foreach (EventsEntries _eventsEntries in Statics.List) { iterateEventsEntries.Add(_eventsEntries); }
            foreach (EventsEntries _eventsEntries in iterateEventsEntries)
            {
                bool delete = true;
                foreach (Entry _entry in Entry.Statics.List) { if (_entry.ID == _eventsEntries.EntryID) { delete = false; break; } }
                if (delete) { _eventsEntries.ListRemove(); }
            }
            iterateEventsEntries.Clear();
            foreach (EventsEntries _eventsEntries in Statics.List) { iterateEventsEntries.Add(_eventsEntries); }
            foreach (EventsEntries _eventsEntries in iterateEventsEntries)
            {
                bool delete = true;
                foreach (Event _event in Event.Statics.List) { if (_event.ID == _eventsEntries.EventID) { delete = false; break; } }
                if (delete) { _eventsEntries.ListRemove(); }
            }
            iterateEventsEntries.Clear();
            Sync();
        }

        public static void SortByDate()
        {
            for (int eventEntryNr1 = 0; eventEntryNr1 < Statics.List.Count - 1; eventEntryNr1++)
            {
                for (int eventEntryNr2 = eventEntryNr1; eventEntryNr2 < Statics.List.Count; eventEntryNr2++)
                {
                    Event event1 = Event.Statics.GetByID(Statics.List[eventEntryNr1].EventID);
                    Event event2 = Event.Statics.GetByID(Statics.List[eventEntryNr2].EventID);
                    Entry entry1 = Entry.Statics.GetByID(Statics.List[eventEntryNr1].EntryID);
                    Entry entry2 = Entry.Statics.GetByID(Statics.List[eventEntryNr2].EntryID);
                    if (event1.EventDate > event2.EventDate || (event1.EventDate == event2.EventDate && entry1.RaceNumber > entry2.RaceNumber))
                    {
                        (Statics.List[eventEntryNr2], Statics.List[eventEntryNr1]) = (Statics.List[eventEntryNr1], Statics.List[eventEntryNr2]);
                    }
                }
            }
        }

        //TEMP: Converter
        [NotMapped] public DateTime EventDate { set { EventID = Event.Statics.GetByUniqueProp(value).ID; } }
        [NotMapped] public string RaceNumber { set { EntryID = Entry.Statics.GetByUniqueProp(value).ID; } }
        [NotMapped] public string CarID2 { set { CarID = Car.Statics.GetByUniqueProp(value).ID; } }
    }
}
