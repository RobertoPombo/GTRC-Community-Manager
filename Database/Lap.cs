using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class Lap : DatabaseObject<Lap>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<Lap> Statics { get; set; }
        static Lap()
        {
            Statics = new StaticDbField<Lap>(true)
            {
                Table = "Laps",
                UniquePropertiesNames = new List<List<string>>() { },
                ToStringPropertiesNames = new List<string>() { nameof(ResultsFileID), nameof(RaceNumber), nameof(LastName), nameof(AccCarID),
                    nameof(Time), nameof(IsValid) },
                PublishList = () => PublishList()
            };
        }
        public Lap() { This = this; Initialize(true, true); }
        public Lap(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Lap(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int resultsFileID = Basics.ID0;
        private long steamID = Driver.SteamIDMinValue;
        private bool isValid = false;
        private int time = int.MaxValue;
        private int sector1 = int.MaxValue;
        private int sector2 = int.MaxValue;
        private int sector3 = int.MaxValue;
        private int raceNumber = Basics.NoID;
        private string firstName = "";
        private string lastName = "";
        private int accCarID = Basics.NoID;
        private int ballast = 0;
        private int restrictor = 0;
        private int category = Basics.NoID;

        public int ResultsFileID
        {
            get { return resultsFileID; }
            set
            {
                if (ResultsFile.Statics.IDList.Count == 0) { _ = new ResultsFile() { ID = 1 }; }
                if (!ResultsFile.Statics.ExistsID(value)) { value = ResultsFile.Statics.IDList[0].ID; }
                resultsFileID = value;
            }
        }

        public long SteamID
        {
            get { return steamID; }
            set { if (!Driver.IsValidSteamID(value)) { steamID = Driver.SteamIDMinValue; } else { steamID = value; } }
        }

        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public int Time
        {
            get { return time; }
            set { if (value > 0) { time = value; } }
        }

        public int Sector1
        {
            get { return sector1; }
            set { if (value > 0) { sector1 = value; } }
        }

        public int Sector2
        {
            get { return sector2; }
            set { if (value > 0) { sector2 = value; } }
        }

        public int Sector3
        {
            get { return sector3; }
            set { if (value > 0) { sector3 = value; } }
        }

        public int RaceNumber
        {
            get { return raceNumber; }
            set { raceNumber = value; }
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

        public int AccCarID
        {
            get { return accCarID; }
            set { if (value >= 0) { accCarID = value; } }
        }

        public int Ballast
        {
            get { return ballast; }
            set { ballast = value; }
        }

        public int Restrictor
        {
            get { return restrictor; }
            set { restrictor = value; }
        }

        public int Category
        {
            get { return category; }
            set { category = value; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable() { }
    }
}
