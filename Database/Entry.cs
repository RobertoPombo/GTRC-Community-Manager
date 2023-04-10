using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Scripts;

namespace Database
{
    public class Entry : DatabaseObject<Entry>
    {
        public static readonly int DefaultRaceNumber = 2;
        public static readonly int RaceNumberMinValue = 1;
        public static readonly int RaceNumberMaxValue = 999;
        [NotMapped][JsonIgnore] public static StaticDbField<Entry> Statics { get; set; }
        static Entry()
        {
            Statics = new StaticDbField<Entry>(true)
            {
                Table = "Entries",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(SeasonID), nameof(RaceNumber) } },
                ToStringPropertiesNames = new List<string>() { nameof(SeasonID), nameof(RaceNumber), nameof(TeamID) },
                PublishList = () => PublishList()
            };
        }
        public Entry() { This = this; Initialize(true, true); }
        public Entry(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Entry(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int seasonID = 0;
        private int raceNumber = DefaultRaceNumber;
        private int teamID = Basics.NoID;
        private int carID = 1;
        private DateTime registerDate = DateTime.Now;
        private DateTime signOutDate = Event.DateTimeMaxValue;
        private int ballast = 0;
        private int restrictor = 0;
        private int category = 3;
        private bool scorePoints = true;
        private int priority = int.MaxValue;

        public int SeasonID
        {
            get { return seasonID; }
            set { seasonID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

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
                if (Car.Statics.IDList.Count == 0) { _ = new Car() { ID = 1 }; }
                if (!Car.Statics.ExistsID(value)) { value = Car.Statics.IDList[0].ID; }
                if (!Statics.DelayPL)
                {
                    List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy(nameof(EventsEntries.EntryID), ID);
                    foreach (EventsEntries _eventEntry in listEventsEntries) { if (_eventEntry.CarChangeDate == registerDate) { _eventEntry.CarID = value; } }
                }
                carID = value;
            }
        }

        public DateTime RegisterDate
        {
            get { return registerDate; }
            set
            {
                registerDate = value;
                if (!Statics.DelayPL)
                {
                    List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy(nameof(EventsEntries.EntryID), ID);
                    foreach (EventsEntries _eventEntry in listEventsEntries)
                    {
                        if (_eventEntry.CarChangeDate <= registerDate) { _eventEntry.CarChangeDate = value; _eventEntry.CarID = CarID; }
                    }
                }
            }
        }

        public DateTime SignOutDate
        {
            get { return signOutDate; }
            set
            {
                if (value >= RegisterDate)
                {
                    DateTime previousSignOutDate = signOutDate;
                    signOutDate = value;
                    if (!Statics.DelayPL && ScorePoints)
                    {
                        List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy(nameof(EventsEntries.EntryID), ID);
                        foreach (EventsEntries _eventEntry in listEventsEntries)
                        {
                            Event _event = Event.Statics.GetByID(_eventEntry.EventID);
                            if (_event.EventDate > signOutDate) { _eventEntry.SignInDate = Event.DateTimeMaxValue; }
                            else if (_event.EventDate > DateTime.Now && _event.EventDate > previousSignOutDate && _event.EventDate < signOutDate)
                            {
                                _eventEntry.SignInDate = DateTime.Now;
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
            set
            {
                if (scorePoints != value)
                {
                    if (!Statics.DelayPL)
                    {
                        List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy(nameof(EventsEntries.EntryID), ID);
                        foreach (EventsEntries _eventEntry in listEventsEntries)
                        {
                            if (_eventEntry.ScorePoints == scorePoints) { _eventEntry.ScorePoints = value; }
                        }
                    }
                    scorePoints = value;
                    if (scorePoints) { Category = 3; } else { Category = 1; }
                }
            }
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

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int seasonNr = 0;
            List<Season> _idListSeason = Season.Statics.IDList;
            if (_idListSeason.Count == 0) { _ = new Season() { ID = 1 }; _idListSeason = Season.Statics.IDList; }
            Season _season = Season.Statics.GetByID(seasonID);
            if (_season.ReadyForList) { seasonNr = Season.Statics.IDList.IndexOf(_season); } else { seasonID = _idListSeason[0].ID; }
            int startValueSeason = seasonNr;

            if (!IsUnique()) { raceNumber = DefaultRaceNumber; }
            int startValueRaceNumber = raceNumber;
            while (!IsUnique())
            {
                if (raceNumber < RaceNumberMaxValue) { raceNumber += 1; } else { raceNumber = RaceNumberMinValue; }
                if (raceNumber == startValueRaceNumber)
                {
                    if (seasonNr + 1 < _idListSeason.Count) { seasonNr += 1; } else { seasonNr = 0; }
                    seasonID = _idListSeason[seasonNr].ID;
                    if (seasonNr == startValueSeason) { break; }
                }
            }
        }
    }
}
