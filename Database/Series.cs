using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class Series : DatabaseObject<Series>
    {
        public static readonly string DefaultName = "Series #1";
        [NotMapped][JsonIgnore] public static StaticDbField<Series> Statics { get; set; }
        static Series()
        {
            Statics = new StaticDbField<Series>(true)
            {
                Table = "Series",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "Name" } },
                ToStringPropertiesNames = new List<string>() { "Name" },
                PublishList = () => PublishList()
            };
        }
        public Series() { This = this; Initialize(true, true); }
        public Series(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Series(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private string name = DefaultName;

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

        public static void PublishList()
        {
            PreSeasonVM.UpdateListSeries();
        }

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
