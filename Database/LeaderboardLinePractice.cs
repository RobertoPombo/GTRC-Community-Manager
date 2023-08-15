using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Database
{
    public class LeaderboardLinePractice : DatabaseObject<LeaderboardLinePractice>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<LeaderboardLinePractice> Statics { get; set; }
        static LeaderboardLinePractice()
        {
            Statics = new StaticDbField<LeaderboardLinePractice>(true)
            {
                Table = "LeaderboardLinesPractice",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(ServerID), nameof(EventID), nameof(EntryID), nameof(DriverID), nameof(CarID) } },
                ToStringPropertiesNames = new List<string>() { nameof(Position), nameof(ServerID), nameof(EventID), nameof(EntryID), nameof(DriverID), nameof(CarID) },
                PublishList = () => PublishList()
            };
        }
        public LeaderboardLinePractice() { This = this; Initialize(true, true); }
        public LeaderboardLinePractice(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public LeaderboardLinePractice(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Server? objServer = new(false);
        private Event objEvent = new(false);
        private Entry objEntry = new(false);
        private Driver? objDriver = new(false);
        private Car? objCar = new(false);
        [JsonIgnore][NotMapped] public Server? ObjServer { get { return Server.Statics.GetByID(serverID); } }
        [JsonIgnore][NotMapped] public Event ObjEvent { get { return Event.Statics.GetByID(eventID); } }
        [JsonIgnore][NotMapped] public Entry ObjEntry { get { return Entry.Statics.GetByID(entryID); } }
        [JsonIgnore][NotMapped] public Driver? ObjDriver { get { return Driver.Statics.GetByID(driverID); } }
        [JsonIgnore][NotMapped] public Car? ObjCar { get { return Car.Statics.GetByID(carID); } }

        private int serverID = 0;
        private int eventID = 0;
        private int entryID = 0;
        private int driverID = 0;
        private int carID = 0;
        private int position = Basics.NoID;
        private int stintAverage = int.MaxValue;
        private int bestLap = int.MaxValue;
        private int bestSector1 = int.MaxValue;
        private int bestSector2 = int.MaxValue;
        private int bestSector3 = int.MaxValue;
        private int lapsCount = 0;
        private int validLapsCount = 0;
        private int validStintsCount = 0;

        public int ServerID
        {
            get { return serverID; }
            set
            {
                serverID = value;
                if (ReadyForList) { SetNextAvailable(); }
                if (serverID == 0) { objServer = null; }
                else { objServer = Server.Statics.GetByID(serverID); }
            }
        }

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); } objEvent = Event.Statics.GetByID(eventID); }
        }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } objEntry = Entry.Statics.GetByID(entryID); }
        }

        public int DriverID
        {
            get { return driverID; }
            set
            {
                driverID = value;
                if (ReadyForList) { SetNextAvailable(); }
                if (driverID == 0) { objDriver = null; }
                else { objDriver = Driver.Statics.GetByID(driverID); }
            }
        }

        public int CarID
        {
            get { return carID; }
            set
            {
                carID = value;
                if (ReadyForList) { SetNextAvailable(); }
                if (carID == 0) { objCar = null; }
                else { objCar = Car.Statics.GetByID(carID); }
            }
        }

        public int Position
        {
            get { return position; }
            set { if (value > 0) { position = value; } }
        }

        public int StintAverage
        {
            get { return stintAverage; }
            set { stintAverage = value; }
        }

        public int BestLap
        {
            get { return bestLap; }
            set { bestLap = value; }
        }

        public int BestSector1
        {
            get { return bestSector1; }
            set { bestSector1 = value; }
        }

        public int BestSector2
        {
            get { return bestSector2; }
            set { bestSector2 = value; }
        }

        public int BestSector3
        {
            get { return bestSector3; }
            set { bestSector3 = value; }
        }

        public int LapsCount
        {
            get { return lapsCount; }
            set { lapsCount = value; }
        }

        public int ValidLapsCount
        {
            get { return validLapsCount; }
            set { validLapsCount = value; }
        }

        public int ValidStintsCount
        {
            get { return validStintsCount; }
            set { validStintsCount = value; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            if (Statics.DelayPL) { return; }

            int serverNr = -1;
            List<Server> _idListServer = Server.Statics.IDList;
            Server _server = Server.Statics.GetByID(serverID);
            if (_server.ReadyForList) { serverNr = Server.Statics.IDList.IndexOf(_server); } else { serverID = 0; }
            int startValueServer = serverNr;

            List<Event> _idListEvent = Event.Statics.IDList;
            if (_idListEvent.Count == 0) { Event _newEvent = new() { ID = 1 }; _idListEvent.Add(_newEvent); }
            Event _event = Event.Statics.GetByID(eventID);
            int eventNr = 0;
            if (_event.ReadyForList) { eventNr = _idListEvent.IndexOf(_event); } else { _event = _idListEvent[eventNr]; eventID = _event.ID; }
            int startValueEvent = eventNr;

            var linqListEvent = from _lingEvent in Event.Statics.List
                                where _lingEvent.SeasonID == _event.SeasonID && _lingEvent.ID != Basics.NoID
                                select _lingEvent;
            _idListEvent = linqListEvent.Cast<Event>().ToList();
            var linqListEntry = from _lingEntry in Entry.Statics.List
                                where _lingEntry.SeasonID == _event.SeasonID && _lingEntry.ID != Basics.NoID
                                select _lingEntry;
            List<Entry> _idListEntry = linqListEntry.Cast<Entry>().ToList();

            if (_idListEntry.Count == 0) { Entry _newEntry = new() { ID = 1, SeasonID = _event.SeasonID }; _idListEntry.Add(_newEntry); }
            Entry _entry = Entry.Statics.GetByID(entryID);
            int entryNr = 0;
            if (_entry.ReadyForList && _idListEntry.Contains(_entry)) { entryNr = _idListEntry.IndexOf(_entry); } else { entryID = _idListEntry[entryNr].ID; }
            int startValueEntry = entryNr;

            int driverNr = 0;
            List<int> _idListDriverID = new() { 0 };
            List<DriversEntries> _idListDriversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), entryID);
            foreach (DriversEntries _driverEntry in _idListDriversEntries) { _idListDriverID.Add(_driverEntry.DriverID); }
            if (_idListDriverID.Contains(driverID)) { driverNr  = _idListDriverID.IndexOf(driverID); } else { driverID = _idListDriverID[driverNr]; }
            int startValueDriver = driverNr;

            int carNr = 0;
            List<int> _idListCarID = new() { 0 };
            foreach (Car _car in Car.Statics.IDList) { _idListCarID.Add(_car.ID); }
            if (_idListCarID.Contains(carID)) { carNr = _idListCarID.IndexOf(carID); } else { carID = _idListCarID[carNr]; }
            int startValueCar = carNr;

            while (!IsUnique())
            {
                if (carNr + 1 < _idListCarID.Count) { carNr += 1; } else { carNr = 0; }
                carID = _idListCarID[carNr];
                if (carNr == startValueCar)
                {
                    if (driverNr + 1 < _idListDriverID.Count) { driverNr += 1; } else { driverNr = 0; }
                    driverID = _idListDriverID[driverNr];
                    if (driverNr == startValueDriver)
                    {
                        if (entryNr + 1 < _idListEntry.Count) { entryNr += 1; } else { entryNr = 0; }
                        entryID = _idListEntry[entryNr].ID;

                        driverNr = 0;
                        _idListDriverID = new() { 0 };
                        _idListDriversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), entryID);
                        foreach (DriversEntries _driverEntry in _idListDriversEntries) { _idListDriverID.Add(_driverEntry.DriverID); }
                        if (_idListDriverID.Contains(driverID)) { driverNr = _idListDriverID.IndexOf(driverID); } else { driverID = _idListDriverID[driverNr]; }
                        startValueDriver = driverNr;

                        if (entryNr == startValueEntry)
                        {
                            if (eventNr + 1 < _idListEvent.Count) { eventNr += 1; } else { eventNr = 0; }
                            eventID = _idListEvent[eventNr].ID;
                            if (eventNr == startValueEvent)
                            {
                                if (serverNr + 1 < _idListServer.Count) { serverNr += 1; } else { serverNr = -1; }
                                if (serverNr == -1) { serverID = 0; } else { serverID = _idListServer[serverNr].ID; }
                                if (serverNr == startValueServer) { break; }
                            }
                        }
                    }
                }
            }

            if (serverID == 0) { objServer = null; } else { objServer = Server.Statics.GetByID(serverID); }
            objEvent = Event.Statics.GetByID(eventID);
            objEntry = Entry.Statics.GetByID(entryID);
            if (driverID == 0) { objDriver = null; } else { objDriver = Driver.Statics.GetByID(driverID); }
            if (carID == 0) { objCar = null; } else { objCar = Car.Statics.GetByID(carID); }
        }
    }
}