using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class DriversDatetimes : DatabaseObject<DriversDatetimes>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<DriversDatetimes> Statics { get; set; }
        static DriversDatetimes()
        {
            Statics = new StaticDbField<DriversDatetimes>(true)
            {
                Table = "DriversDatetimes",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(DriverID), nameof(Date) } },
                ToStringPropertiesNames = new List<string>() { nameof(DriverID), nameof(Date) },
                PublishList = () => PublishList()
            };
        }
        public DriversDatetimes() { This = this; Initialize(true, true); }
        public DriversDatetimes(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public DriversDatetimes(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Driver objDriver = new(false);
        [JsonIgnore][NotMapped] public Driver ObjDriver { get { return objDriver; } }

        private int driverID = 0;
        private DateTime date = DateTime.Now;
        private int eloRating = new Driver(false).EloRating;
        private int safetyRating = new Driver(false).SafetyRating;
        private int warnings = new Driver(false).Warnings;

        public int DriverID
        {
            get { return driverID; }
            set { driverID = value; if (ReadyForList) { SetNextAvailable(); } objDriver = Driver.Statics.GetByID(driverID); SetParentProps(); }
        }

        public DateTime Date
        {
            get { return date; }
            set { date = value; if (ReadyForList) { SetNextAvailable(); } SetParentProps(); }
        }

        public int EloRating
        {
            get { return eloRating; }
            set { if (value < 0) { eloRating = 0; } else if (value > 9999) { eloRating = 9999; } else { eloRating = value; } }
        }

        public int SafetyRating
        {
            get { return safetyRating; }
            set { if (value > 100) { safetyRating = 100; } else { safetyRating = value; } }
        }

        public int Warnings
        {
            get { return warnings; }
            set
            {
                if (value < 0) { warnings = 0; }
                else { warnings = value; }
            }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int driverNr = 0;
            List<Driver> _idListDriver = Driver.Statics.IDList;
            if (_idListDriver.Count == 0) { _ = new Driver() { ID = 1 }; _idListDriver = Driver.Statics.IDList; }
            Driver _driver = Driver.Statics.GetByID(driverID);
            if (_driver.ReadyForList) { driverNr = Driver.Statics.IDList.IndexOf(_driver); } else { _driver = _idListDriver[0]; driverID = _driver.ID; }
            int startValueDriver = driverNr;

            if (date < Basics.DateTimeMinValue) { date = Basics.DateTimeMinValue; }
            else if (date > Basics.DateTimeMaxValue) { date = Basics.DateTimeMaxValue; }
            DateTime startValue = date;

            while (!IsUnique())
            {
                if (date < Basics.DateTimeMaxValue) { date = date.AddDays(1); } else { date = Basics.DateTimeMinValue; }
                if (date == startValue)
                {
                    if (driverNr + 1 < _idListDriver.Count) { driverNr += 1; } else { driverNr = 0; }
                    _driver = _idListDriver[driverNr];
                    driverID = _driver.ID;
                    if (driverNr == startValueDriver) { break; }
                }
            }
        }

        public void SetParentProps()
        {
            EloRating = objDriver.EloRating;
            SafetyRating = objDriver.SafetyRating;
            Warnings = objDriver.Warnings;
        }

        public static void SortByDate()
        {
            for (int index1 = 0; index1 < Statics.List.Count - 1; index1++)
            {
                for (int index2 = index1; index2 < Statics.List.Count; index2++)
                {
                    if (Statics.List[index1].Date > Statics.List[index2].Date)
                    {
                        (Statics.List[index2], Statics.List[index1]) = (Statics.List[index1], Statics.List[index2]);
                    }
                }
            }
        }

        public static List<DriversDatetimes> SortByDate(List<DriversDatetimes> _list)
        {
            for (int index1 = 0; index1 < _list.Count - 1; index1++)
            {
                for (int index2 = index1; index2 < _list.Count; index2++)
                {
                    if (_list[index1].Date > _list[index2].Date)
                    {
                        (_list[index2], _list[index1]) = (_list[index1], _list[index2]);
                    }
                }
            }
            return _list;
        }
    }
}
