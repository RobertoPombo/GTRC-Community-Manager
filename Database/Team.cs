using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class Team : DatabaseObject<Team>
    {
        public static readonly string DefaultName = "Team #1";
        [NotMapped][JsonIgnore] public static StaticDbField<Team> Statics { get; set; }
        static Team()
        {
            Statics = new StaticDbField<Team>(true)
            {
                Table = "Teams",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "SeasonID", "Name" } },
                ToStringPropertiesNames = new List<string>() { "SeasonID", "Name" },
                PublishList = () => PublishList()
            };
        }
        public Team() { This = this; Initialize(true, true); }
        public Team(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Team(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int seasonID = 0;
        private string name = DefaultName;

        public int SeasonID
        {
            get { return seasonID; }
            set { seasonID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = Basics.SubStr(Basics.RemoveSpaceStartEnd(value ?? name), 0, 32);
                if (name == null || name == "") { name = DefaultName; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int seasonNr = 0;
            List<Season> _idListSeason = Season.Statics.IDList;
            if (_idListSeason.Count == 0) { _ = new Season() { ID = 1 }; _idListSeason = Season.Statics.IDList; }
            Season _season = Season.Statics.GetByID(seasonID);
            if (_season.ReadyForList) { seasonNr = Season.Statics.IDList.IndexOf(_season); } else { seasonID = _idListSeason[0].ID; }
            int startValueSeason = seasonNr;

            int nr = 1;
            string defName = name;
            if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
            while (!IsUnique())
            {
                name = defName + " #" + nr.ToString();
                nr++; if (nr == int.MaxValue)
                {
                    if (seasonNr + 1 < _idListSeason.Count) { seasonNr += 1; } else { seasonNr = 0; }
                    seasonID = _idListSeason[seasonNr].ID;
                    if (seasonNr == startValueSeason) { break; }
                }
            }
        }

        //TEMP: Converter
        [NotMapped] public string TeamID { set { Name = value; } }
    }
}
