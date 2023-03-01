using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class RaceControl : DatabaseObject<RaceControl>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<RaceControl> Statics { get; set; }

        private int driverID = 0;
        private string firstName = "";
        private string lastName = "";

        static RaceControl()
        {
            Statics = new StaticDbField<RaceControl>(true)
            {
                Table = "RaceControl",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "DriverID" } },
                ToStringPropertiesNames = new List<string>() { "FullName" },
                ListSetter = () => ListSetter()
            };
        }

        public RaceControl() { This = this; Initialize(true, true); }
        public RaceControl(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public RaceControl(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public int DriverID
        {
            get { return driverID; }
            set { driverID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public string FirstName
        {
            get { return firstName; }
            set { firstName = Basics.RemoveSpaceStartEnd(value ?? firstName); }
        }

        public string LastName
        {
            get { return lastName; }
            set { lastName = Basics.RemoveSpaceStartEnd(value ?? lastName); }
        }

        [JsonIgnore] public string FullName
        {
            get { return Driver.GetFullName(firstName, lastName); }
        }

        [JsonIgnore] public string ShortName
        {
            get { return Driver.GetShortName(firstName, lastName); }
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
    }
}
