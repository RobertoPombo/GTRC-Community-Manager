using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class Season : DatabaseObject<Season>
    {
        public static readonly string DefaultName = "Season #1";
        [NotMapped][JsonIgnore] public static StaticDbField<Season> Statics { get; set; }
        static Season()
        {
            Statics = new StaticDbField<Season>(true)
            {
                Table = "Seasons",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "Name" } },
                ToStringPropertiesNames = new List<string>() { "Name" },
                ListSetter = () => ListSetter()
            };
        }
        public Season() { This = this; Initialize(true, true); }
        public Season(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Season(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private string name = DefaultName;
        private int seriesID = 0;

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

        public int SeriesID
        {
            get { return seriesID; }
            set
            {
                if (Series.Statics.IDList.Count == 0) { _ = new Series() { ID = 1 }; }
                if (!Series.Statics.ExistsID(value)) { value = Series.Statics.IDList[0].ID; }
                seriesID = value;
            }
        }

        public static void ListSetter()
        {
            PreSeasonVM.UpdateListSeasons();
            ServerVM.UpdateListSeasons();
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
