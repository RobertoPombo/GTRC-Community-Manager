using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

namespace Database
{
    public class EntriesDatetimes : DatabaseObject<EntriesDatetimes>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<EntriesDatetimes> Statics { get; set; }
        static EntriesDatetimes()
        {
            Statics = new StaticDbField<EntriesDatetimes>(true)
            {
                Table = "EntriesDatetimes",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(EntryID), nameof(Date) } },
                ToStringPropertiesNames = new List<string>() { nameof(EntryID), nameof(Date) },
                PublishList = () => PublishList()
            };
        }
        public EntriesDatetimes() { This = this; Initialize(true, true); }
        public EntriesDatetimes(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public EntriesDatetimes(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Entry objEntry = new(false);
        private Car objCar = new(false);
        [JsonIgnore][NotMapped] public Entry ObjEntry { get { return objEntry; } }
        [JsonIgnore][NotMapped] public Car ObjCar { get { return objCar; } }

        private int entryID = 0;
        private DateTime date = DateTime.Now;
        private int carID = Basics.ID0;
        private bool scorePoints = true;
        private bool permanent = true;

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } objEntry = Entry.Statics.GetByID(entryID); SetParentProps(); }
        }

        public DateTime Date
        {
            get { return date; }
            set { date = value; if (ReadyForList) { SetNextAvailable(); } SetParentProps(); }
        }

        public int CarID
        {
            get { return carID; }
            set
            {
                if (Car.Statics.IDList.Count == 0) { objCar = new Car() { ID = 1 }; }
                if (!Car.Statics.ExistsID(value)) { objCar = Car.Statics.IDList[0]; carID = objCar.ID; }
                else { carID = value; objCar = Car.Statics.GetByID(carID); }
            }
        }

        public bool ScorePoints
        {
            get { return scorePoints; }
            set { scorePoints = value; }
        }

        public bool Permanent
        {
            get { return permanent; }
            set { permanent = value; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int entryNr = 0;
            List<Entry> _idListEntry = Entry.Statics.IDList;
            if (_idListEntry.Count == 0) { _ = new Entry() { ID = 1 }; _idListEntry = Entry.Statics.IDList; }
            Entry _entry = Entry.Statics.GetByID(entryID);
            if (_entry.ReadyForList) { entryNr = Entry.Statics.IDList.IndexOf(_entry); }
            else { _entry = _idListEntry[0]; entryID = _entry.ID; }
            int startValueEntry = entryNr;

            if (date < Basics.DateTimeMinValue) { date = Basics.DateTimeMinValue; }
            else if (date > Basics.DateTimeMaxValue) { date = Basics.DateTimeMaxValue; }
            DateTime startValue = date;

            while (!IsUnique())
            {
                if (date < Basics.DateTimeMaxValue) { date = date.AddSeconds(1); } else { date = Basics.DateTimeMinValue; }
                if (date == startValue)
                {
                    if (entryNr + 1 < _idListEntry.Count) { entryNr += 1; } else { entryNr = 0; }
                    entryID = _idListEntry[entryNr].ID;
                    if (entryNr == startValueEntry) { break; }
                }
            }

            objEntry = Entry.Statics.GetByID(entryID);
        }

        public void SetParentProps()
        {
            CarID = ObjEntry.CarID;
            ScorePoints = ObjEntry.ScorePoints;
            Permanent = ObjEntry.Permanent;
            List<EntriesDatetimes> _list = SortByDate(Statics.GetBy(nameof(EntryID), EntryID));
            if (_list.Count > 0)
            {
                CarID = _list[^1].CarID;
                ScorePoints = _list[^1].ScorePoints;
                Permanent = _list[^1].Permanent;
            }
            for (int index = 0; index < _list.Count - 1; index++)
            {
                if (_list[index + 1].Date > DateTime.Now)
                {
                    CarID = _list[index].CarID;
                    ScorePoints = _list[index].ScorePoints;
                    Permanent = _list[index].Permanent;
                }
            }
        }

        public static void SortByDate()
        {
            for (int index1 = 0; index1 < Statics.List.Count - 1; index1++)
            {
                for (int index2 = index1; index2 < Statics.List.Count; index2++)
                {
                    EntriesDatetimes obj1 = Statics.List[index1];
                    EntriesDatetimes obj2 = Statics.List[index2];
                    if ((obj1.EntryID > obj2.EntryID) || ((obj1.EntryID == obj2.EntryID) && (obj1.Date > obj2.Date)))
                    {
                        (Statics.List[index2], Statics.List[index1]) = (Statics.List[index1], Statics.List[index2]);
                    }
                }
            }
        }

        public static List<EntriesDatetimes> SortByDate(List<EntriesDatetimes> _list)
        {
            for (int index1 = 0; index1 < _list.Count - 1; index1++)
            {
                for (int index2 = index1; index2 < _list.Count; index2++)
                {
                    EntriesDatetimes obj1 = _list[index1];
                    EntriesDatetimes obj2 = _list[index2];
                    if ((obj1.EntryID > obj2.EntryID) || ((obj1.EntryID == obj2.EntryID) && (obj1.Date > obj2.Date)))
                    {
                        (_list[index2], _list[index1]) = (_list[index1], _list[index2]);
                    }
                }
            }
            return _list;
        }

        public static EntriesDatetimes GetAnyByUniqProp(int _entryID, DateTime _date)
        {
            EntriesDatetimes _entryDate = Statics.GetByUniqProp(new List<dynamic>() { _entryID, _date });
            if (!_entryDate.ReadyForList && _entryID >= Basics.ID0)
            {
                _entryDate.EntryID = _entryID;
                _entryDate.Date = _date;
                _entryDate.ListAdd();
            }
            return _entryDate;
        }
    }
}
