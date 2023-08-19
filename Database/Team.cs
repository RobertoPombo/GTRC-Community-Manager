using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;
using Newtonsoft.Json.Linq;

namespace Database
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
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(SeasonID), nameof(Name) } },
                ToStringPropertiesNames = new List<string>() { nameof(SeasonID), nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Team() { This = this; Initialize(true, true); }
        public Team(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Team(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Season objSeason = new(false);
        [JsonIgnore][NotMapped] public Season ObjSeason { get { return Season.Statics.GetByID(seasonID); } }

        private int seasonID = 0;
        private string name = DefaultName;

        public int SeasonID
        {
            get { return seasonID; }
            set { seasonID = value; if (ReadyForList) { SetNextAvailable(); } objSeason = Season.Statics.GetByID(seasonID); }
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
        /*[JsonIgnore] public string OrganizationName
        {
            get
            {
                List<string> del = new() { " - ", " #", " | ", " (" };
                List<string> apx = new() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
                List<string> rep = new() { " ", "-", "_", ".", "team", "simracing", "eracing", "racing", "motorsports", "motorsport", "esports", "esport", "sport", "performance", "junior" };
                string orgaName = "";
                string _name0 = Name;
                foreach (string _del in del)
                {
                    string[] _names = _name0.Split(_del);
                    if (_names.Length > 1)
                    {
                        string _name1 = _names[0];
                        if (_name1.Length > orgaName.Length) { orgaName = _name1; }
                    }
                }
                if (orgaName.Length == 0) { orgaName = _name0; }
                foreach (string _apx in apx)
                {
                    string _name1 = Basics.SubStr(_name0, 0, _name0.Length - _apx.Length);
                    string _name2 = Basics.SubStr(_name0, -_apx.Length);
                    if (_name2 == _apx && _name1.Length > 0 && _name1.Length < orgaName.Length) { orgaName = _name1; }
                }
                orgaName = orgaName.ToLower();
                foreach (string _rep in rep) { orgaName = orgaName.Replace(_rep.ToLower(), ""); }
                return orgaName;
            }
        }*/

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

            objSeason = Season.Statics.GetByID(seasonID);
        }
    }
}
