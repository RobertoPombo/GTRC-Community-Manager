using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        private Season objSeason = new(false);
        private Team? objTeam = new(false);
        private Car objCar = new(false);
        [JsonIgnore][NotMapped] public Season ObjSeason { get { return Season.Statics.GetByID(seasonID); } }
        [JsonIgnore][NotMapped] public Team? ObjTeam { get { return Team.Statics.GetByID(teamID); } }
        [JsonIgnore][NotMapped] public Car ObjCar { get { return Car.Statics.GetByID(carID); } }

        private int seasonID = 0;
        private int raceNumber = DefaultRaceNumber;
        private int teamID = Basics.NoID;
        private int carID = Basics.ID0;
        private DateTime registerDate = DateTime.Now;
        private DateTime signOutDate = Basics.DateTimeMaxValue;
        private int ballast = 0;
        private int restrictor = 0;
        private int category = 3;
        private bool scorePoints = true;
        private bool permanent = true;

        public int SeasonID
        {
            get { return seasonID; }
            set { seasonID = value; if (ReadyForList) { SetNextAvailable(); } objSeason = Season.Statics.GetByID(seasonID); }
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
            set
            {
                objTeam = Team.Statics.GetByID(value);
                if (objTeam.ID != value || objTeam.SeasonID != seasonID) { value = Basics.NoID; objTeam = null; }
                teamID = value;
            }
        }

        public int CarID
        {
            get { return carID; }
            set
            {
                if (Car.Statics.IDList.Count == 0) { objCar = new Car() { ID = 1 }; }
                if (!Car.Statics.ExistsID(value)) { objCar = Car.Statics.IDList[0]; carID = objCar.ID; }
                else { carID = value; objCar = Car.Statics.GetByID(carID); }
            }
        }

        public DateTime RegisterDate
        {
            get { return registerDate; }
            set { if (value >= Basics.DateTimeMinValue) { registerDate = value; } }
        }

        public DateTime SignOutDate
        {
            get { return signOutDate; }
            set
            {
                if (value >= Basics.DateTimeMinValue && value >= RegisterDate)
                {
                    DateTime previousSignOutDate = signOutDate;
                    signOutDate = value;
                    if (!Statics.DelayPL)
                    {
                        List<EventsEntries> listEventsEntries = EventsEntries.Statics.GetBy(nameof(EventsEntries.EntryID), ID);
                        foreach (EventsEntries _eventEntry in listEventsEntries)
                        {
                            Event _event = _eventEntry.ObjEvent;
                            if (_event.Date > signOutDate) { _eventEntry.SignInDate = Basics.DateTimeMaxValue; }
                            else if (_event.Date > DateTime.Now && _event.Date > previousSignOutDate && _event.Date < signOutDate && Permanent)
                            {
                                _eventEntry.SignInDate = Basics.DateTimeMinValue;
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
                if (value < -40) { ballast = 0; }
                else if (value > 40) { ballast = 40; }
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
            set { if (value >= 0 && value <= 4 && category != value) { category = value; if (category == 3) { ScorePoints = true; } else if (category == 1) { ScorePoints = false; } } }
        }

        public bool ScorePoints
        {
            get { return scorePoints; }
            set { if (scorePoints != value) { scorePoints = value; if (scorePoints) { Category = 3; } else { Category = 1; } } }
        }

        public bool Permanent
        {
            get { return permanent; }
            set { if (permanent != value) { permanent = value; } }
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

            objSeason = Season.Statics.GetByID(seasonID);
            objTeam = Team.Statics.GetByID(teamID);
            if (objTeam.ID != teamID || objTeam.SeasonID != seasonID) { teamID = Basics.NoID; objTeam = null; }
        }

        public int CountSignOut(Event nextEvent)
        {
            int signOutCount = 0;
            if (nextEvent.SeasonID != SeasonID) { return signOutCount; }
            List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), SeasonID));
            foreach (Event _event in listEvents)
            {
                if (RegisterDate < _event.Date)
                {
                    if (_event.Date > nextEvent.Date) { return signOutCount; }
                    if (_event.Date > SignOutDate) { return signOutCount; }
                    if (!EventsEntries.GetAnyByUniqProp(ID, _event.ID).SignInState) { signOutCount++; }
                }
            }
            return signOutCount;
        }

        public int CountNoShow(Event nextEvent, bool includeNextEvent)
        {
            int noShowCount = 0;
            if (nextEvent.SeasonID != SeasonID) { return noShowCount; }
            List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), SeasonID));
            for (int eventNr = 0; eventNr < listEvents.Count; eventNr++)
            {
                if (RegisterDate < listEvents[eventNr].Date)
                {
                    bool reachedNextEvent1 = includeNextEvent && listEvents[eventNr].Date > nextEvent.Date;
                    bool reachedNextEvent2 = !includeNextEvent && listEvents[eventNr].Date >= nextEvent.Date;
                    if (reachedNextEvent1 || reachedNextEvent2) { return noShowCount; }
                    if (listEvents[eventNr].Date > SignOutDate || listEvents[eventNr].Date > DateTime.Now) { return noShowCount; } //checken ob das event vllt gerade läuft!
                    EventsEntries _eventEntry = EventsEntries.GetAnyByUniqProp(ID, listEvents[eventNr].ID);
                    if (_eventEntry.SignInState && _eventEntry.IsOnEntrylist && !_eventEntry.Attended) { noShowCount++; }
                }
            }
            return noShowCount;
        }

        public List<EntriesDatetimes> GetAnyEntriesDatetimes()
        {
            List<EntriesDatetimes> _list = EntriesDatetimes.SortByDate(EntriesDatetimes.Statics.GetBy(nameof(EntriesDatetimes.EntryID), ID));
            if (_list.Count == 0) { _list.Add(new(false) { EntryID = ID, Date = RegisterDate, CarID = CarID, ScorePoints = ScorePoints, Permanent = Permanent }); }
            return _list;
        }

        public EntriesDatetimes GetEntriesDatetimesByDate(DateTime _limit)
        {
            List<EntriesDatetimes> _list = GetAnyEntriesDatetimes();
            for (int index = 0; index < _list.Count - 1; index++) { if (_list[index + 1].Date > _limit) { return _list[index]; } }
            return _list[^1];
        }

        public int CarChangeCount(DateTime limitDate)
        {
            int carChangeCount = 0;
            DateTime minDate = ObjSeason.DateCarChangeLimit;
            List<EntriesDatetimes> _list = GetAnyEntriesDatetimes();
            Car car0 = ObjCar;
            for (int index = 0; index < _list.Count; index++)
            {
                if (_list[index].Date > limitDate) { return carChangeCount; }
                Car car1 = _list[index].ObjCar;
                if (car1.ID != car0.ID && _list[index].Date > minDate)
                {
                    if ((!ObjSeason.GroupCarLimits && ObjSeason.DaysIgnoreCarLimits < (_list[index].Date - RegisterDate).Days) ||
                        car1.Category != car0.Category || car1.Manufacturer != car0.Manufacturer)
                    {
                        carChangeCount++;
                        minDate = Event.GetNextEvent(SeasonID, _list[index].Date).Date;
                    }
                }
                car0 = car1;
            }
            return carChangeCount;
        }

        public DateTime GetLatestCarChangeDate(DateTime limitDate, bool IgnoreGroupCarLimits=false)
        {
            bool GroupCarLimits = ObjSeason.GroupCarLimits && !IgnoreGroupCarLimits;
            DateTime latestCarChangeDate = RegisterDate;
            List<EntriesDatetimes> _list = GetAnyEntriesDatetimes();
            Car car0 = ObjCar;
            for (int index = 0; index < _list.Count; index++)
            {
                if (_list[index].Date > limitDate) { return latestCarChangeDate; }
                Car car1 = _list[index].ObjCar;
                bool IgnoreCarChange = ObjSeason.DaysIgnoreCarLimits > (_list[index].Date - car1.ReleaseDate).Days && !IgnoreGroupCarLimits;
                if (car1.ID != car0.ID && ((!GroupCarLimits && !IgnoreCarChange) || car1.Category != car0.Category || car1.Manufacturer != car0.Manufacturer))
                {
                    latestCarChangeDate = _list[index].Date;
                }
                car0 = car1;
            }
            return latestCarChangeDate;
        }
    }
}
