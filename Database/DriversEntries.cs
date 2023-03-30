using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
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

        private int driverID = 0;
        private int entryID = 0;
        private string name3Digits = "";

        public int DriverID
        {
            get { return driverID; }
            set { driverID = value; if (ReadyForList) { SetNextAvailable(); } Name3Digits = Driver.Statics.GetByID(driverID).Name3DigitsOptions[0]; }
        }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } }
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
        }

        private static List<DriversEntries> GetListByDriverIDSeasonID(int driverID, int seasonID)
        {
            List<DriversEntries> driversEntries = new();
            List<DriversEntries> _driversEntries = Statics.GetBy(nameof(DriverID), driverID);
            foreach (DriversEntries _driverEntry in _driversEntries)
            {
                if (Entry.Statics.GetByID(_driverEntry.EntryID).SeasonID == seasonID) { driversEntries.Add(_driverEntry); }
            }
            return driversEntries;
        }

        public static DriversEntries GetByDriverIDSeasonID(int driverID, int seasonID)
        {
            List<DriversEntries> driversEntries = Statics.GetBy(nameof(DriverID), driverID);
            foreach (DriversEntries _driverEntry in driversEntries)
            {
                Entry entry = Entry.Statics.GetByID(_driverEntry.EntryID);
                if (entry.ID != Basics.NoID && entry.SeasonID == seasonID) { return _driverEntry; }
            }
            return new DriversEntries(false);
        }

        //TEMP: Converter
        [NotMapped] public string SteamID { set { DriverID = Driver.Statics.GetByUniqProp(value).ID; } }
        [NotMapped] public string RaceNumber { set { EntryID = Entry.Statics.GetByUniqProp(new List<dynamic>() { 4, value }).ID; } }
    }
}
