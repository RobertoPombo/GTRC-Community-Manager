using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class PreQualiResultLine : DatabaseObject<PreQualiResultLine>
    {
        [NotMapped][JsonIgnore] public static readonly List<long> SteamIDsFixPreQ = new()
        {
            76561197974380992,
            76561198203881699,
            76561198404497438,
            76561198073693971,
            76561199261458988,
            76561198007124321,
            76561198282429343,
            76561199102253061,
            76561199239196375,
            76561198011698379
        };
        [NotMapped][JsonIgnore] public static StaticDbField<PreQualiResultLine> Statics { get; set; }
        static PreQualiResultLine()
        {
            Statics = new StaticDbField<PreQualiResultLine>(true)
            {
                Table = "PreQualiResultLines",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(EntryID) } },
                ToStringPropertiesNames = new List<string>() { nameof(Position), nameof(EntryID) },
                PublishList = () => PublishList()
            };
        }
        public PreQualiResultLine() { This = this; Initialize(true, true); }
        public PreQualiResultLine(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public PreQualiResultLine(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int position = 0;
        private int entryID = 0;
        private int average = int.MaxValue;
        private int average1 = int.MaxValue;
        private int average2 = int.MaxValue;
        private int diffAverage = int.MaxValue;
        private int bestLap1 = int.MaxValue;
        private int bestLap2 = int.MaxValue;
        private int diffBestLap = int.MaxValue;
        private int lapsCount = 0;
        private int lapsCount1 = 0;
        private int lapsCount2 = 0;
        private int validLapsCount = 0;
        private int validLapsCount1 = 0;
        private int validLapsCount2 = 0;
        private int validStintsCount = 0;
        private int validStintsCount1 = 0;
        private int validStintsCount2 = 0;

        [JsonIgnore] public int BestSector1a = int.MaxValue;
        [JsonIgnore] public int BestSector3a = int.MaxValue;
        [JsonIgnore] public int BestSector1b = int.MaxValue;
        [JsonIgnore] public int BestSector3b = int.MaxValue;

        public int Position
        {
            get { if (List.Contains(this)) { return position; } else { return Basics.NoID; } }
            set { if (value > 0) { position = value; } }
        }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public int Average
        {
            get { return average; }
            set { average = value; }
        }

        public int Average1
        {
            get { return average1; }
            set { average1 = value; }
        }

        public int Average2
        {
            get { return average2; }
            set { average2 = value; }
        }

        public int DiffAverage
        {
            get { return diffAverage; }
            set { diffAverage = value; }
        }

        public int BestLap1
        {
            get { return bestLap1; }
            set { bestLap1 = value; }
        }

        public int BestLap2
        {
            get { return bestLap2; }
            set { bestLap2 = value; }
        }

        public int DiffBestLap
        {
            get { return diffBestLap; }
            set { diffBestLap = value; }
        }

        public int LapsCount
        {
            get { return lapsCount; }
            set { lapsCount = value; }
        }

        public int LapsCount1
        {
            get { return lapsCount1; }
            set { lapsCount1 = value; }
        }

        public int LapsCount2
        {
            get { return lapsCount2; }
            set { lapsCount2 = value; }
        }

        public int ValidLapsCount
        {
            get { return validLapsCount; }
            set { validLapsCount = value; }
        }

        public int ValidLapsCount1
        {
            get { return validLapsCount1; }
            set { validLapsCount1 = value; }
        }

        public int ValidLapsCount2
        {
            get { return validLapsCount2; }
            set { validLapsCount2 = value; }
        }

        public int ValidStintsCount
        {
            get { return validStintsCount; }
            set { validStintsCount = value; }
        }

        public int ValidStintsCount1
        {
            get { return validStintsCount1; }
            set { validStintsCount1 = value; }
        }

        public int ValidStintsCount2
        {
            get { return validStintsCount2; }
            set { validStintsCount2 = value; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int entryNr = 0;
            List<Entry> _idList = Entry.Statics.IDList;
            if (_idList.Count == 0) { _ = new Entry() { ID = 1 }; _idList = Entry.Statics.IDList; }
            Entry _entry = Entry.Statics.GetByID(entryID);
            if (_entry.ReadyForList) { entryNr = Entry.Statics.IDList.IndexOf(_entry); } else { entryID = _idList[0].ID; }
            int startValue = entryNr;
            while (!IsUnique(1))
            {
                if (entryNr + 1 < _idList.Count) { entryNr += 1; } else { entryNr = 0; }
                entryID = _idList[entryNr].ID;
                if (entryNr == startValue) { break; }
            }
        }
    }
}
