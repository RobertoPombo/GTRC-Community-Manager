using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace GTRCLeagueManager.Database
{
    public class Entry : DatabaseObject<Entry>
    {
        public static StaticDbField<Entry> Statics = new StaticDbField<Entry>(true)
        {
            Table = "Entries",
            UniquePropertiesNames = new List<List<string>>() { new List<string>() { "RaceNumber" } },
            ToStringPropertiesNames = new List<string>() { "RaceNumber", "TeamID" },
            ListSetter = () => ListSetter()
        };
        public static readonly int DefaultRaceNumber = 2;
        public static readonly int RaceNumberMinValue = 1;
        public static readonly int RaceNumberMaxValue = 999;

        private int raceNumber = DefaultRaceNumber;
        private int teamID = Basics.NoID;
        private int carID = 0;
        private DateTime registerdate = DateTime.Now;
        private DateTime signoutdate = Event.DateTimeMaxValue;
        private int ballast = 0;
        private int restrictor = 0;
        private int category = 3;
        private bool scorepoints = true;

        public Entry() { This = this; Initialize(true, true); }
        public Entry(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Entry(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public int RaceNumber
        {
            get { return raceNumber; }
            set
            {
                if (value < RaceNumberMinValue) { raceNumber = RaceNumberMinValue; }
                else if (value > RaceNumberMaxValue) { raceNumber = RaceNumberMaxValue; }
                else { raceNumber = value; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public int TeamID
        {
            get { return teamID; }
            set { if (!Team.Statics.ExistsID(value)) { value = Basics.NoID; } teamID = value; }
        }

        public int CarID
        {
            get { return carID; }
            set
            {
                if (Car.Statics.IDList.Count == 0) { new Car() { ID = 1 }; }
                if (!Car.Statics.ExistsID(value)) { value = Car.Statics.IDList[0].ID; }
                List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy("EntryID", ID);
                foreach (EventsEntries _eventsEntries in listEventsEntries) { if (_eventsEntries.CarID == carID) { _eventsEntries.CarID = value; } }
                carID = value;
            }
        }

        public DateTime RegisterDate
        {
            get { return registerdate; }
            set
            {
                List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy("EntryID", ID);
                foreach (EventsEntries _eventsEntries in listEventsEntries)
                {
                    if (_eventsEntries.CarChangeDate == registerdate) { _eventsEntries.CarChangeDate = value; }
                }
                registerdate = value;
            }
        }

        public DateTime SignOutDate
        {
            get { return signoutdate; }
            set
            {
                if (value >= RegisterDate)
                {
                    signoutdate = value;
                    if (ScorePoints)
                    {
                        List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy("EntryID", ID);
                        foreach (EventsEntries _eventsEntries in listEventsEntries)
                        {
                            Event _event = Event.Statics.GetByID(_eventsEntries.EventID);
                            if (_event.EventDate > signoutdate) { _eventsEntries.SignInDate = Event.DateTimeMaxValue; }
                            else if (RegisterDate < _event.EventDate && _event.EventDate > DateTime.Now)
                            {
                                _eventsEntries.SignInDate = Event.DateTimeMinValue;
                            }
                        }
                    }
                }
            }
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
                if (value >= 0 && value <= 4)
                {
                    List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy("EntryID", ID);
                    foreach (EventsEntries _eventsEntries in listEventsEntries)
                    {
                        if (_eventsEntries.Category == category) { _eventsEntries.Category = value; }
                    }
                    category = value;
                }
            }
        }

        public bool ScorePoints
        {
            get { return scorepoints; }
            set
            {
                List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy("EntryID", ID);
                foreach (EventsEntries _eventsEntries in listEventsEntries)
                {
                    if (_eventsEntries.ScorePoints == scorepoints) { _eventsEntries.ScorePoints = value; }
                }
                scorepoints = value;
            }
        }

        public static void ListSetter()
        {
            EventsEntries.Statics.PendingSync = true;
        }

        public override void SetNextAvailable()
        {
            if (!IsUnique()) { raceNumber = DefaultRaceNumber; }
            int startValue = raceNumber;
            while (!IsUnique())
            {
                if (raceNumber < RaceNumberMaxValue) { raceNumber += 1; } else { raceNumber = RaceNumberMinValue; }
                if (raceNumber == startValue) { break; }
            }
        }



        //später löschen
        public static List<string> ReturnPropsAsList()
        {
            List<string> list = new List<string>();
            List<string> blackListProperties = new List<string> { "CarID", "DriverIDs" };
            foreach (PropertyInfo property in typeof(Entry).GetProperties()) { if (!blackListProperties.Contains(property.Name)) { list.Add(property.Name); } }
            foreach (string key in Driver.ReturnPropsAsList()) { list.Add("Driver " + key); }
            foreach (string key in Car.ReturnPropsAsList()) { list.Add("Car " + key); }
            return list;
        }

        public Dictionary<string, dynamic> ReturnAsDict()
        {
            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
            List<string> blackListProperties = new List<string> { "CarID", "DriverIDs" };
            foreach (PropertyInfo property in typeof(Entry).GetProperties())
            {
                if (!blackListProperties.Contains(property.Name))
                {
                    if (property.GetValue(this) != null) { dict[property.Name] = property.GetValue(this); }
                    else { dict[property.Name] = ""; }
                }
            }
            foreach (string key in Car.Statics.GetByUniqueProp(carID).ReturnAsDict().Keys)
            {
                dict["Car " + key] = Car.Statics.GetByUniqueProp(carID).ReturnAsDict()[key];
            }
            foreach (string key in Driver.ReturnPropsAsList())
            {
                dict["Driver " + key] = new List<dynamic>();
            }
            return dict;
        }

        //TEMP: Converter
        [NotMapped] public string TeamID2 { set { TeamID = Team.Statics.GetByUniqueProp(value).ID; } }
        [NotMapped] public string CarID2 { set { CarID = Car.Statics.GetByUniqueProp(value).ID; } }
    }
}
