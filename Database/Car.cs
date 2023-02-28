using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GTRCLeagueManager.Database
{
    public class Car : DatabaseObject<Car>
    {
        public static StaticDbField<Car> Statics = new StaticDbField<Car>(true)
        {
            Table = "Cars",
            UniquePropertiesNames = new List<List<string>>() { new List<string>() { "AccCarID" } },
            ToStringPropertiesNames = new List<string>() { "Name" },
            ListSetter = () => ListSetter()
        };
        public static readonly string PathLogos = MainWindow.dataDirectory + "logos\\";

        private int accCarID = 0;
        private string name = "";
        private string manufacturer = "";
        private string model = "";
        private string category = "";
        private int year = DateTime.Now.Year;
        private string name_GTRC = "";

        public Car() { This = this; Initialize(true, true); }
        public Car(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Car(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public int AccCarID
        {
            get { return accCarID; }
            set { if (value < 0) { accCarID = 0; } else { accCarID = value; } if (ReadyForList) { SetNextAvailable(); } }
        }

        public string Name
        {
            get { return name; }
            set { name = Basics.RemoveSpaceStartEnd(value ?? name); }
        }

        public string Manufacturer
        {
            get { return manufacturer; }
            set { manufacturer = Basics.RemoveSpaceStartEnd(value ?? manufacturer); }
        }

        public string Model
        {
            get { return model; }
            set { model = Basics.RemoveSpaceStartEnd(value ?? model); }
        }

        public string Category
        {
            get { return category; }
            set { category = Basics.RemoveSpaceStartEnd(value ?? category); }
        }

        public int Year
        {
            get { return year; }
            set { year = value; }
        }

        public string Name_GTRC
        {
            get { return name_GTRC; }
            set { name_GTRC = Basics.RemoveSpaceStartEnd(value ?? name_GTRC); }
        }

        [JsonIgnore]
        public string Logo
        {
            get { return "/Logos/" + manufacturer + ".png"; }
        }

        [JsonIgnore]
        public bool IsLatestVersion
        {
            get { foreach (Car _car in Statics.List) { if (_car.Manufacturer == Manufacturer && _car.Category == Category && _car.Year > Year) { return false; } } return true; }
        }

        public static void ListSetter()
        {
            CarBoP.List.Clear();
            foreach (Car _car in Statics.List) { CarBoP.List.Add(new CarBoP() { Car = _car }); }
        }

        public override void SetNextAvailable()
        {
            int startValue = accCarID;
            while (!IsUnique())
            {
                if (accCarID < int.MaxValue) { accCarID += 1; } else { accCarID = 0; }
                if (accCarID == startValue) { break; }
            }
        }



        //später löschen
        public static List<string> ReturnPropsAsList()
        {
            List<string> list = new List<string>();
            foreach (PropertyInfo property in typeof(Car).GetProperties()) { list.Add(property.Name); }
            return list;
        }

        public Dictionary<string, dynamic> ReturnAsDict()
        {
            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
            foreach (PropertyInfo property in typeof(Car).GetProperties()) { dict[property.Name] = property.GetValue(this); }
            return dict;
        }
    }
}
