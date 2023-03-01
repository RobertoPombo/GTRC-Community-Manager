using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GTRCLeagueManager.Database
{
    public class DriversTeams : DatabaseObject<DriversTeams>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<DriversTeams> Statics { get; set; }

        private int driverID = 0;
        private int teamID = 0;

        static DriversTeams()
        {
            Statics = new StaticDbField<DriversTeams>(true)
            {
                Table = "DriversTeams",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "DriverID", "TeamID" } },
                ToStringPropertiesNames = new List<string>() { "DriverID", "TeamID" },
                ListSetter = () => ListSetter()
            };
        }

        public DriversTeams() { This = this; Initialize(true, true); }
        public DriversTeams(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public DriversTeams(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public int DriverID
        {
            get { return driverID; }
            set { driverID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public int TeamID
        {
            get { return teamID; }
            set { teamID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public static void ListSetter() { }

        public override void SetNextAvailable()
        {
            int teamNr = 0;
            List<Team> _idListTeam = Team.Statics.IDList;
            if (_idListTeam.Count == 0) { new Team() { ID = 1 }; _idListTeam = Team.Statics.IDList; }
            Team _team = Team.Statics.GetByID(teamID);
            if (_team.ReadyForList) { teamNr = Team.Statics.IDList.IndexOf(_team); } else { teamID = _idListTeam[0].ID; }
            int startValueTeam = teamNr;

            int driverNr = 0;
            List<Driver> _idListDriver = Driver.Statics.IDList;
            if (_idListDriver.Count == 0) { new Driver() { ID = 1 }; _idListDriver = Driver.Statics.IDList; }
            Driver _driver = Driver.Statics.GetByID(driverID);
            if (_driver.ReadyForList) { driverNr = Driver.Statics.IDList.IndexOf(_driver); } else { driverID = _idListDriver[0].ID; }
            int startValueDriver = driverNr;

            while (!IsUnique())
            {
                if (teamNr + 1 < _idListTeam.Count) { teamNr += 1; } else { teamNr = 0; }
                teamID = _idListTeam[teamNr].ID;
                if (teamNr == startValueTeam)
                {
                    if (driverNr + 1 < _idListDriver.Count) { driverNr += 1; } else { driverNr = 0; }
                    driverID = _idListDriver[driverNr].ID;
                    if (driverNr == startValueDriver) { break; }
                }
            }
        }

        //TEMP: Converter
        [NotMapped] public string SteamID { set { DriverID = Driver.Statics.GetByUniqueProp(value).ID; } }
        [NotMapped] public string TeamID2 { set { TeamID = Team.Statics.GetByUniqueProp(value).ID; } }
    }
}
