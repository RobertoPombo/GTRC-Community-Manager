using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GTRCLeagueManager.Database
{
    public class Lap : DatabaseObject<Lap>
    {
        public static StaticDbField<Lap> Statics = new StaticDbField<Lap>(true)
        {
            Table = "Laps",
            ListSetter = () => ListSetter()
        };

        private int entryID = 0;
        private bool valid = false;
        private int time = int.MaxValue;
        private int sector1 = int.MaxValue;
        private int sector2 = int.MaxValue;
        private int sector3 = int.MaxValue;
        private int track = Basics.NoID;

        public Lap() { This = this; Initialize(true, true); }
        public Lap(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Lap(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public bool Valid
        {
            get { return valid; }
            set { valid = value; }
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

        public int Track
        {
            get { return track; }
            set { if (value == 0 || value == 1) { track = value; } }
        }

        public static void ListSetter() { }

        public override void SetNextAvailable()
        {
            int entryNr = 0;
            List<Entry> _idList = Entry.Statics.IDList;
            if (_idList.Count == 0) { new Entry() { ID = 1 }; _idList = Entry.Statics.IDList; }
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
