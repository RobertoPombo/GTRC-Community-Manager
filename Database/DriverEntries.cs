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
                UniquePropertiesNames = new List<List<string>>() { },
                ToStringPropertiesNames = new List<string>() { nameof(DriverID), nameof(EntryID) },
                PublishList = () => PublishList()
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
            set
            {
                if (Driver.Statics.IDList.Count == 0) { _ = new Driver() { ID = 1 }; }
                if (!Driver.Statics.ExistsID(value)) { value = Driver.Statics.IDList[0].ID; }
                driverID = value;
                Name3Digits = Driver.Statics.GetByID(driverID).Name3DigitsOptions[0];
            }
        }

        public int EntryID
        {
            get { return entryID; }
            set
            {
                if (Entry.Statics.IDList.Count == 0) { _ = new Entry() { ID = 1 }; }
                if (!Entry.Statics.ExistsID(value)) { value = Entry.Statics.IDList[0].ID; }
                entryID = value;
            }
        }

        public string Name3Digits
        {
            get { return name3Digits; }
            set { if (value != null && value.Length == 3) { name3Digits = value.ToUpper(); } }
        }

        public static void PublishList() { }

        public override void SetNextAvailable() { }

        //TEMP: Converter
        [NotMapped] public string SteamID { set { DriverID = Driver.Statics.GetByUniqProp(value).ID; } }
        [NotMapped] public string RaceNumber { set { EntryID = Entry.Statics.GetByUniqProp(new List<dynamic>() { 4, value }).ID; } }
    }
}
