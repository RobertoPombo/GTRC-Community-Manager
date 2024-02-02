using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

using GTRC_Community_Manager;

namespace Database
{
    public class Car : DatabaseObject<Car>
    {
        public static readonly string PathLogos = MainWindow.dataDirectory + "Logos\\";
        [NotMapped][JsonIgnore] public static StaticDbField<Car> Statics { get; set; }
        static Car()
        {
            Statics = new StaticDbField<Car>(true)
            {
                Table = "Cars",
                UniquePropertiesNames = new List<List<string>>() { new() { nameof(AccCarID) } },
                ToStringPropertiesNames = new List<string>() { nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Car() { This = this; Initialize(true, true); }
        public Car(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Car(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int accCarID = 0;
        private string name = "";
        private string manufacturer = "";
        private string model = "";
        private string category = "";
        private int year = DateTime.Now.Year;
        private DateTime releaseDate = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue);
        private string name_GTRC = "";

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

        public DateTime ReleaseDate
        {
            get { return releaseDate; }
            set { releaseDate = DateOnly.FromDateTime(value).ToDateTime(TimeOnly.MinValue); }
        }

        public string Name_GTRC
        {
            get { return name_GTRC; }
            set { name_GTRC = Basics.RemoveSpaceStartEnd(value ?? name_GTRC); }
        }

        [JsonIgnore] public string Logo
        {
            get { return "\\Logos\\" + manufacturer + ".png"; }
        }

        [JsonIgnore] public bool IsLatestModel
        {
            get { foreach (Car _car in Statics.List) { if (_car.Manufacturer == Manufacturer && _car.Category == Category && _car.Year > Year) { return false; } } return true; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int startValue = accCarID;
            while (!IsUnique())
            {
                if (accCarID < int.MaxValue) { accCarID += 1; } else { accCarID = 0; }
                if (accCarID == startValue) { break; }
            }
        }
    }
}
