using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class Team : DatabaseObject<Team>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<Team> Statics { get; set; }
        public static readonly string DefaultName = "Team #1";

        private string name = DefaultName;

        static Team()
        {
            Statics = new StaticDbField<Team>(true)
            {
                Table = "Teams",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "Name" } },
                ToStringPropertiesNames = new List<string>() { "Name" },
                ListSetter = () => ListSetter()
            };
        }

        public Team() { This = this; Initialize(true, true); }
        public Team(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Team(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

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

        //TEMP: Converter
        [NotMapped] public string TeamID { set { Name = value; } }
    }
}
