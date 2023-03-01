using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class DriverEntries : DatabaseObject<DriverEntries>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<DriverEntries> Statics { get; set; }
        static DriverEntries()
        {
            Statics = new StaticDbField<DriverEntries>(true)
            {
                Table = "DriverEntries",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "DriverID" } },
                ToStringPropertiesNames = new List<string>() { "DriverID" },
                ListSetter = () => ListSetter()
            };
        }
        public DriverEntries() { This = this; Initialize(true, true); }
        public DriverEntries(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public DriverEntries(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

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
            set
            {
                if (Entry.Statics.IDList.Count == 0) { new Entry() { ID = 1 }; }
                if (!Entry.Statics.ExistsID(value)) { value = Entry.Statics.IDList[0].ID; }
                entryID = value;
            }
        }

        public string Name3Digits
        {
            get { return name3Digits; }
            set { if (value != null && value.Length == 3) { name3Digits = value.ToUpper(); } }
        }

        public static void ListSetter() { }

        public override void SetNextAvailable()
        {
            int driverNr = 0;
            List<Driver> _idList = Driver.Statics.IDList;
            if (_idList.Count == 0) { new Driver() { ID = 1 }; _idList = Driver.Statics.IDList; }
            Driver _driver = Driver.Statics.GetByID(driverID);
            if (_driver.ReadyForList) { driverNr = Driver.Statics.IDList.IndexOf(_driver); } else { driverID = _idList[0].ID; }
            int startValue = driverNr;
            while (!IsUnique())
            {
                if (driverNr + 1 < _idList.Count) { driverNr += 1; } else { driverNr = 0; }
                driverID = _idList[driverNr].ID;
                if (driverNr == startValue) { break; }
            }
        }

        //TEMP: Converter
        [NotMapped] public string SteamID { set { DriverID = Driver.Statics.GetByUniqueProp(value).ID; } }
        [NotMapped] public string RaceNumber { set { EntryID = Entry.Statics.GetByUniqueProp(value).ID; } }
    }
}
