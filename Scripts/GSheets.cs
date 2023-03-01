using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using GTRCLeagueManager.Database;

namespace GTRCLeagueManager
{
    public static class GSheets
    {
        private static string PathCrendentials = MainWindow.dataDirectory + "googlesheets_projectinfo.json";
        private static readonly string appName = "GTRC Community Manager";
        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static GoogleCredential Crendentials;
        private static SheetsService GSheetService;

        public static readonly List<string> VarListEntries = new List<string> { "Startnummer", "SteamID64", "DiscordID", "Vorname", "Nachname", "Teamname", "Fahrzeug", "Stammfahrer oder Gaststarter", "Zeitstempel" };

        public static void Initialize()
        {
            using (var stream = new FileStream(PathCrendentials, FileMode.Open, FileAccess.ReadWrite)) { Crendentials = GoogleCredential.FromStream(stream).CreateScoped(Scopes); }
            GSheetService = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer() { HttpClientInitializer = Crendentials, ApplicationName = appName, });
        }

        public static dynamic LoadRange(string docID, string sheetID, string range)
        {
            try
            {
                Initialize();
                var request = GSheetService.Spreadsheets.Values.Get(docID, $"{sheetID}!" + range);
                var response = request.Execute();
                dynamic rows = response.Values;
                return rows;
            }
            catch { return new List<List<string>>(); }
        }

        public static void ClearRange(string docID, string sheetID, string range)
        {
            try
            {
                Initialize();
                var requestBody = new ClearValuesRequest();
                var deleteRequest = GSheetService.Spreadsheets.Values.Clear(requestBody, docID, $"{sheetID}!" + range);
                var deleteResponse = deleteRequest.Execute();
            }
            catch { }
        }

        public static void UpdateRange(string docID, string sheetID, string range, List<List<object>> rows)
        {
            try
            {
                Initialize();
                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>>();
                foreach (List<object> row in rows) { valueRange.Values.Add(row); }
                var updateRequest = GSheetService.Spreadsheets.Values.Update(valueRange, docID, $"{sheetID}!" + range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var updateResponse = updateRequest.Execute();
            }
            catch { }
        }

        public static Dictionary<string, int> CreateVarMap(dynamic firstRow, List<string> VarList)
        {
            Dictionary<string, int> VarMap = new Dictionary<string, int>();
            if (firstRow.Count > 0)
            {
                foreach (string preQualiVar in VarList)
                {
                    if (firstRow.Contains(preQualiVar)) { VarMap[preQualiVar] = firstRow.IndexOf(preQualiVar); }
                }
            }
            return VarMap;
        }

        public static Dictionary<string, dynamic> ReadValuesFromRow(Dictionary<string, int> VarMap, List<object> row)
        {
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
            values["RaceNumber"] = Basics.NoID;
            values["SteamID"] = Basics.NoID;
            values["DiscordID"] = Driver.DiscordIDNoValue;
            values["FirstName"] = new Driver(false).FirstName;
            values["LastName"] = new Driver(false).LastName;
            values["TeamName"] = Team.DefaultName;
            values["CarID"] = Basics.NoID;
            values["Category"] = new Entry(false).Category;
            values["ScorePoints"] = new Entry(false).ScorePoints;
            values["RegisterDate"] = DateTime.Now;
            DateTime RegisterDate = DateTime.Now;
            if (VarMap.ContainsKey("Startnummer"))
            {
                string _raceNumber = row[VarMap["Startnummer"]].ToString();
                int raceNumber = values["RaceNumber"];
                if (Int32.TryParse(_raceNumber, out raceNumber)) { values["RaceNumber"] = raceNumber; }
            }
            if (VarMap.ContainsKey("SteamID64"))
            {
                long steamID = Driver.String2LongSteamID(row[VarMap["SteamID64"]].ToString());
                if (Driver.IsValidSteamID(steamID)) { values["SteamID"] = steamID; }
            }
            if (VarMap.ContainsKey("DiscordID"))
            {
                if (Int64.TryParse(row[VarMap["DiscordID"]].ToString(), out long discordID) && Driver.IsValidDiscordID(discordID)) { values["DiscordID"] = discordID; }
            }
            if (VarMap.ContainsKey("Vorname")) { values["FirstName"] = row[VarMap["Vorname"]].ToString(); }
            if (VarMap.ContainsKey("Nachname")) { values["LastName"] = row[VarMap["Nachname"]].ToString(); }
            if (VarMap.ContainsKey("Teamname")) { values["TeamName"] = row[VarMap["Teamname"]].ToString(); }
            if (VarMap.ContainsKey("Fahrzeug")) { values["CarID"] = Car.Statics.GetBy("Name_GTRC", row[VarMap["Fahrzeug"]].ToString()).AccCarID; }
            if (VarMap.ContainsKey("Stammfahrer oder Gaststarter"))
            {
                if (row[VarMap["Stammfahrer oder Gaststarter"]].ToString() == "Stammfahrer") { values["Category"] = 3; values["ScorePoints"] = true; }
                else { values["Category"] = 1; values["ScorePoints"] = false; values["TeamName"] = Team.DefaultName; }
            }
            if (VarMap.ContainsKey("Zeitstempel"))
            {
                DateTime registerDate = values["RegisterDate"];
                if (DateTime.TryParse(row[VarMap["Zeitstempel"]].ToString(), out registerDate)) { values["RegisterDate"] = registerDate; }
            }
            return values;
        }

        public static void SyncDriver(Dictionary<string, dynamic> values)
        {
            long SteamID = values["SteamID"];
            long DiscordID = values["DiscordID"];
            string FirstName = values["FirstName"];
            string LastName = values["LastName"];
            DateTime RegisterDate = values["RegisterDate"];
            if (Driver.IsValidSteamID(SteamID))
            {
                Driver driver = Driver.Statics.GetByUniqueProp(SteamID);
                if (!driver.ReadyForList) { driver = new Driver { SteamID = SteamID }; }
                if (driver.FirstName == "") { driver.FirstName = FirstName; }
                if (driver.LastName == "") { driver.LastName = LastName; }
                if (driver.DiscordID == Driver.DiscordIDNoValue) { driver.DiscordID = DiscordID; }
                if (RegisterDate < driver.RegisterDate) { driver.RegisterDate = RegisterDate; }
            }
        }

        public static void SyncTeam(Dictionary<string, dynamic> values)
        {
            long SteamID = values["SteamID"];
            string TeamName = values["TeamName"];
            SyncDriver(values);
            Driver driver = Driver.Statics.GetByUniqueProp(SteamID);
            SteamID = driver.SteamID;
            if (TeamName != Team.DefaultName && Driver.IsValidSteamID(SteamID))
            {
                Team team = Team.Statics.GetByUniqueProp(TeamName);
                if (!Team.Statics.ExistsUniqueProp(TeamName)) { team = new Team { Name = TeamName }; }
                TeamName = team.Name;
                //if (!DriversTeams.Statics.ExistsUniqueProp(new List<dynamic>() { _entry.ID, _event.ID } { DriverID = SteamID, TeamID = TeamName })) { new DriversTeams() { DriverID = SteamID, TeamID = TeamName }; }
            }
        }

        public static void SyncEntry(Dictionary<string, dynamic> values)
        {
            int RaceNumber = values["RaceNumber"];
            long SteamID = values["SteamID"];
            string TeamName = values["TeamName"];
            int CarID = values["CarID"];
            int Category = values["Category"];
            bool ScorePoints = values["ScorePoints"];
            DateTime RegisterDate = values["RegisterDate"];
            SyncTeam(values);
            Driver driver = Driver.Statics.GetByUniqueProp(SteamID);
            SteamID = driver.SteamID;
            Team team = Team.Statics.GetByUniqueProp(TeamName);
            if(Team.Statics.ExistsUniqueProp(TeamName)) { TeamName = team.Name; } else { TeamName = Team.DefaultName; }/*
            if (RaceNumber != Entry.NoID && driver.ReadyForList)
            {
                Entry entry = Entry.getEntryByRaceNumber(RaceNumber);
                DriverEntries driverEntries = DriverEntries.getDriverEntriesBySteamID(SteamID);
                bool initEventsEntries = false;
                if (entry.RaceNumber == Entry.NoID) { entry = new Entry { RaceNumber = RaceNumber }; initEventsEntries = true; }
                if (driverEntries.DriverID == DriverEntries.NoID) { driverEntries = new DriverEntries { DriverID = SteamID }; }
                driverEntries.EntryID = RaceNumber;
                if (TeamName != Team.NoTeamName) { entry.TeamID = TeamName; }
                if (entry.CarID == Basics.NoID && CarID != Basics.NoID) { entry.CarID = CarID; }
                entry.Category = Category;
                entry.ScorePoints = ScorePoints;
                if (RegisterDate < entry.RegisterDate) { entry.RegisterDate = RegisterDate; }
                if (initEventsEntries)
                {
                    List<EventsEntries> listEventsEntries = EventsEntries.GetEventsEntriesByRaceNumber(RaceNumber);
                    foreach (EventsEntries _eventsEntries in listEventsEntries) { _eventsEntries.InitializeProperties(); }
                    _ = Commands.CreateStartingGridMessage(entry.RaceNumber, Event.GetEventByCurrentDate().EventDate, false, false);
                }
            }*/
        }

        public static void SyncFormsEntries(string docID, string sheetID, string range)
        {
            dynamic rows = LoadRange(docID, sheetID, range);
            if (rows != null && rows.Count > 1)
            {
                Dictionary<string, int> VarMap = CreateVarMap(rows[0], new List<string> { "Startnummer" });
                List<Entry> iterateListEntry = new List<Entry>(); foreach (Entry entry in Entry.Statics.List) { iterateListEntry.Add(entry); }
                foreach (Entry entry in iterateListEntry)
                {
                    for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                    {
                        if (ReadValuesFromRow(VarMap, rows[rowNr])["RaceNumber"] == entry.RaceNumber) { break; }
                        if (rowNr == rows.Count - 1) { RemoveEntry(entry.RaceNumber); }
                    }
                }
                VarMap = CreateVarMap(rows[0], new List<string> { "Teamname" });
                List<Team> iterateListTeam = new List<Team>(); foreach (Team team in Team.Statics.List) { iterateListTeam.Add(team); }
                foreach (Team team in iterateListTeam)
                {
                    for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                    {
                        if (ReadValuesFromRow(VarMap, rows[rowNr])["TeamName"] == team.Name) { break; }
                        if (rowNr == rows.Count - 1) { RemoveTeam(team.ID); }
                    }
                }
                VarMap = CreateVarMap(rows[0], VarListEntries);
                for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                {
                    Dictionary<string, dynamic> values = ReadValuesFromRow(VarMap, rows[rowNr]);
                    SyncEntry(values);
                }
            }
        }

        public static void RemoveEntry(int _entryID)
        {
            Entry entry = Entry.Statics.GetByID(_entryID);
            if (entry.ID != Basics.NoID)
            {
                List<DriverEntries> ListDriverEntries = DriverEntries.Statics.GetBy("EntryID", entry.ID);
                List<Driver> ListDrivers = new List<Driver>();
                foreach (DriverEntries driverEntry in ListDriverEntries)
                {
                    ListDrivers.Add(Driver.Statics.GetByID(driverEntry.DriverID));
                    driverEntry.ListRemove();
                }
                Team team = Team.Statics.GetByID(entry.TeamID);
                if (team.ID != Basics.NoID)
                {
                    foreach (Driver driver in ListDrivers)
                    {
                        DriversTeams.Statics.GetByUniqueProp(new List<dynamic>() { driver.ID, team.ID }).ListRemove();
                    }
                    if (DriversTeams.Statics.GetBy("TeamID", team.ID).Count == 0) { team.ListRemove(); }
                }
                entry.ListRemove();
            }
        }

        public static void RemoveTeam(int _teamID)
        {
            if (Team.Statics.ExistsID(_teamID))
            {
                Team team = Team.Statics.GetByID(_teamID);
                List<DriversTeams> ListDriversTeams = DriversTeams.Statics.GetBy("TeamID", team.ID);
                foreach (DriversTeams driversTeams in ListDriversTeams) { driversTeams.ListRemove(); }
                team.ListRemove();
            }
        }

        public static void UpdatePreQStandings(string docID, string sheetID)
        {
            List<List<object>> rows = new List<List<object>>();
            List<object> values = new List<object>() { "Pos", "Fahrer", "Nr", "Team", "Fahrzeug", "Schnitt", "Abstand", "Intervall", "Schnitt", "Schnitt", "Schnitt",
                "Anzahl Runden", "Anzahl Runden", "Anzahl Runden", "Anzahl gültige Runden", "Anzahl gültige Runden", "Anzahl gültige Runden", "Bestzeit", "Bestzeit",
                "Bestzeit", "Anzahl Stints", "Anzahl Stints", "Anzahl Stints" };
            rows.Add(values);
            values = new List<object>() { "Pos", "Fahrer", "Nr", "Team", "Fahrzeug", "Schnitt", "Abstand", "Intervall", "Nürburgring", "Misano", "Differenz", "Gesamt",
                "Nürburgring", "Misano", "Gesamt", "Nürburgring", "Misano", "Nürburgring", "Misano", "Differenz", "Gesamt", "Nürburgring", "Misano" };
            rows.Add(values);
            if (PreQualiResultLine.Statics.List.Count > 0)
            {
                int average0 = PreQualiResultLine.Statics.List[0].Average;
                for (int rowNr = 0; rowNr < PreQualiResultLine.Statics.List.Count; rowNr++)
                {
                    values = new List<object>();
                    PreQualiResultLine _resultsLine = PreQualiResultLine.Statics.List[rowNr];
                    int _id = _resultsLine.EntryID;
                    Entry _entry = Entry.Statics.GetByID(_id);
                    List<DriverEntries> _driverEntries = DriverEntries.Statics.GetBy("EntryID", _entry.ID);
                    string driverText = "";
                    foreach (DriverEntries _driverEntry in _driverEntries) { driverText += Driver.Statics.GetByID(_driverEntry.EntryID).FullName + ", "; }
                    driverText = driverText.Substring(0, Math.Max(0, driverText.Length - 2));
                    int average = _resultsLine.Average;
                    int average1 = PreQualiResultLine.Statics.List[Math.Max(0, rowNr - 1)].Average;
                    values.Add((rowNr + 1).ToString() + ".");
                    values.Add(driverText);
                    values.Add(_entry.RaceNumber.ToString());
                    values.Add(_entry.TeamID);
                    values.Add(Car.Statics.GetByUniqueProp(_entry.CarID).Name);
                    if (average < int.MaxValue)
                    {
                        values.Add(Basics.ms2laptime(average));
                        if (average != average0 && average0 < Int32.MaxValue) { values.Add("'+" + Basics.ms2laptime(average - average0)); }
                        else { values.Add(""); }
                        if (average != average1 && average1 < Int32.MaxValue) { values.Add("'+" + Basics.ms2laptime(average - average1)); }
                        else { values.Add(""); }
                    }
                    else { values.Add(""); values.Add(""); values.Add(""); }
                    if (_resultsLine.Average1 < int.MaxValue) { values.Add(Basics.ms2laptime(_resultsLine.Average1)); }
                    else { values.Add(""); }
                    if (_resultsLine.Average2 < int.MaxValue) { values.Add(Basics.ms2laptime(_resultsLine.Average2)); }
                    else { values.Add(""); }
                    if (_resultsLine.DiffAverage > 0 && _resultsLine.DiffAverage < int.MaxValue) { values.Add(Math.Round((double)_resultsLine.DiffAverage / 100000, 3).ToString() + "%"); }
                    else { values.Add(""); }
                    values.Add(_resultsLine.LapsCount.ToString() + "L");
                    values.Add(_resultsLine.LapsCount1.ToString() + "L");
                    values.Add(_resultsLine.LapsCount2.ToString() + "L");
                    values.Add(_resultsLine.ValidLapsCount.ToString() + "L");
                    values.Add(_resultsLine.ValidLapsCount1.ToString() + "L");
                    values.Add(_resultsLine.ValidLapsCount2.ToString() + "L");
                    if (_resultsLine.BestLap1 < int.MaxValue) { values.Add(Basics.ms2laptime(_resultsLine.BestLap1)); }
                    else { values.Add(""); }
                    if (_resultsLine.BestLap2 < int.MaxValue) { values.Add(Basics.ms2laptime(_resultsLine.BestLap2)); }
                    else { values.Add(""); }
                    if (_resultsLine.DiffBestLap > 0 && _resultsLine.DiffBestLap < int.MaxValue) { values.Add(Math.Round((double)_resultsLine.DiffBestLap / 100000, 3).ToString() + "%"); }
                    else { values.Add(""); }
                    if (_resultsLine.ValidStintsCount > 0) { values.Add(_resultsLine.ValidStintsCount.ToString() + "x"); }
                    else { values.Add(""); }
                    if (_resultsLine.ValidStintsCount1 > 0) { values.Add(_resultsLine.ValidStintsCount1.ToString() + "x"); }
                    else { values.Add(""); }
                    if (_resultsLine.ValidStintsCount2 > 0) { values.Add(_resultsLine.ValidStintsCount2.ToString() + "x"); }
                    else { values.Add(""); }
                    rows.Add(values);
                }
                string range = "A1:W";
                ClearRange(docID, sheetID, range);
                range += (PreQualiResultLine.Statics.List.Count + 2).ToString();
                UpdateRange(docID, sheetID, range, rows);
            }
        }

        public static void UpdateBoPStandings(string docID, string sheetID)
        {
            CarBoP.SortByCount();
            List<List<object>> rows = new List<List<object>>();
            List<object> values = new List<object>() { "Pos", "Fahrzeug", "Jahr", "Anz.", "Ballast", "Restr." };
            rows.Add(values);
            values = new List<object>() { "", "", "", "", "", "" };
            rows.Add(values);
            int pos = 1;
            int count0 = Int32.MaxValue;
            foreach (CarBoP _carBoP in CarBoP.List)
            {
                if (_carBoP.Car.Category == "GT3" && _carBoP.Car.IsLatestVersion)
                {
                    values = new List<object>();
                    if (_carBoP.CountBoP == count0) { values.Add("'="); }
                    else { values.Add(pos.ToString() + "."); }
                    values.Add(_carBoP.Car.Name);
                    values.Add(_carBoP.Car.Year.ToString());
                    if (_carBoP.CountBoP == 0) { values.Add(""); }
                    else { values.Add(_carBoP.CountBoP.ToString() + "x"); }
                    if (_carBoP.Ballast == 0) { values.Add("'-"); }
                    else { values.Add(_carBoP.Ballast.ToString() + " kg"); }
                    if (_carBoP.Restrictor == 0) { values.Add("'-"); }
                    else { values.Add(_carBoP.Restrictor.ToString() + "%"); }
                    rows.Add(values);
                    count0 = _carBoP.CountBoP;
                    pos++;
                }
            }
            if (rows.Count > 0)
            {
                string range = "A1:F";
                ClearRange(docID, sheetID, range);
                range += (rows.Count).ToString();
                UpdateRange(docID, sheetID, range, rows);
            }
        }
    }
}
