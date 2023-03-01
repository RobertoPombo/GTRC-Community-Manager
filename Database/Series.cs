using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class Series : DatabaseObject<Series>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<Series> Statics { get; set; }
        public static readonly string DefaultName = "Series #1";

        private string name = DefaultName;

        static Series()
        {
            Statics = new StaticDbField<Series>(true)
            {
                Table = "Series",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "Name" } },
                ToStringPropertiesNames = new List<string>() { "Name" },
                ListSetter = () => ListSetter()
            };
        }

        public Series() { This = this; Initialize(true, true); }
        public Series(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Series(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public string Name
        {
            get { return name; }
            set
            {
                name = Basics.RemoveSpaceStartEnd(value ?? name);
                if (name == null || name == "") { name = DefaultName; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public static void ListSetter() { }

        public override void SetNextAvailable()
        {
            int nr = 1;
            string defName = name;
            if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
            while (!IsUnique())
            {
                name = defName + " #" + nr.ToString();
                nr++; if (nr == int.MaxValue) { break; }
            }
        }
    }
}
