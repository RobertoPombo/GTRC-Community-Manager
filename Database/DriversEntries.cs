using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

namespace Database
{
    public class DriversEntries : DatabaseObject<DriversEntries>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<DriversEntries> Statics { get; set; }
        static DriversEntries()
        {
            Statics = new StaticDbField<DriversEntries>(true)
            {
                Table = "DriversEntries",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(DriverID), nameof(EntryID) } },
                ToStringPropertiesNames = new List<string>() { nameof(DriverID), nameof(EntryID) },
                PublishList = () => PublishList()
            };
        }
        public DriversEntries() { This = this; Initialize(true, true); }
        public DriversEntries(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public DriversEntries(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Driver objDriver = new(false);
        private Entry objEntry = new(false);
        [JsonIgnore][NotMapped] public Driver ObjDriver { get { return objDriver; } }
        [JsonIgnore][NotMapped] public Entry ObjEntry { get { return objEntry; } }

        private int driverID = 0;
        private int entryID = 0;
        private string name3Digits = "";

        public int DriverID
        {
            get { return driverID; }
            set
            {
                driverID = value;
                if (ReadyForList) { SetNextAvailable(); }
                objDriver = Driver.Statics.GetByID(driverID);
                Name3Digits = ObjDriver.Name3DigitsOptions[0];
            }
        }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } objEntry = Entry.Statics.GetByID(entryID); }
        }

        public string Name3Digits
        {
            get { return name3Digits; }
            set { if (value != null && value.Length == 3) { name3Digits = value.ToUpper(); } }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int entryNr = 0;
            List<Entry> _idListEntry = Entry.Statics.IDList;
            if (_idListEntry.Count == 0) { _ = new Entry() { ID = 1 }; _idListEntry = Entry.Statics.IDList; }
            Entry _entry = Entry.Statics.GetByID(entryID);
            if (_entry.ReadyForList) { entryNr = Entry.Statics.IDList.IndexOf(_entry); } else { entryID = _idListEntry[0].ID; }
            int startValueEntry = entryNr;

            int driverNr = 0;
            List<Driver> _idListDriver = Driver.Statics.IDList;
            if (_idListDriver.Count == 0) { _ = new Driver() { ID = 1 }; _idListDriver = Driver.Statics.IDList; }
            Driver _driver = Driver.Statics.GetByID(driverID);
            if (_driver.ReadyForList) { driverNr = Driver.Statics.IDList.IndexOf(_driver); } else { driverID = _idListDriver[0].ID; }
            int startValueDriver = driverNr;

            while (!IsUnique() || GetListByDriverIDSeasonID(driverID, Entry.Statics.GetByID(entryID).SeasonID).Count > 1)
            {
                if (entryNr + 1 < _idListEntry.Count) { entryNr += 1; } else { entryNr = 0; }
                entryID = _idListEntry[entryNr].ID;
                if (entryNr == startValueEntry)
                {
                    if (driverNr + 1 < _idListDriver.Count) { driverNr += 1; } else { driverNr = 0; }
                    driverID = _idListDriver[driverNr].ID;
                    if (driverNr == startValueDriver) { break; }
                }
            }

            objDriver = Driver.Statics.GetByID(driverID);
            objEntry = Entry.Statics.GetByID(entryID);
        }

        public static List<DriversEntries> GetListByDriverIDSeasonID(int driverID, int seasonID)
        {
            List<DriversEntries> driversEntries = new();
            List<DriversEntries> _driversEntries = Statics.GetBy(nameof(DriverID), driverID);
            foreach (DriversEntries _driverEntry in _driversEntries)
            {
                if (_driverEntry.ObjEntry.SeasonID == seasonID) { driversEntries.Add(_driverEntry); }
            }
            return driversEntries;
        }

        public static DriversEntries GetByDriverIDSeasonID(int driverID, int seasonID)
        {
            List<DriversEntries> driversEntries = Statics.GetBy(nameof(DriverID), driverID);
            foreach (DriversEntries _driverEntry in driversEntries)
            {
                if (_driverEntry.ObjEntry.ID != Basics.NoID && _driverEntry.ObjEntry.SeasonID == seasonID) { return _driverEntry; }
            }
            return new DriversEntries(false);
        }
    }
}
