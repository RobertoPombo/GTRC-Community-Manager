using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Database;
using System;
using System.Collections.Generic;
using System.IO;

using GTRC_Community_Manager;

namespace Scripts
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
            catch
            {
                MainVM.List[0].LogCurrentText = "Load Google-Sheets range failed! [sheetID: " + sheetID + " | range: " + range + "]";
                return new List<List<string>>();
            }
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
            catch { MainVM.List[0].LogCurrentText = "Clear Google-Sheets range failed! [sheetID: " + sheetID + " | range: " + range + "]"; }
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
            catch { MainVM.List[0].LogCurrentText = "Update Google-Sheets range failed! [sheetID: " + sheetID + " | range: " + range + "]"; }
        }

        public static Dictionary<string, int> CreateVarMap(dynamic? firstRow, List<string> VarList)
        {
            Dictionary<string, int> VarMap = new();
            if (firstRow?.Count > 0)
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
            Dictionary<string, dynamic> values = new()
            {
                ["RaceNumber"] = Basics.NoID,
                ["SteamID"] = Basics.NoID,
                ["DiscordID"] = Driver.DiscordIDNoValue,
                ["FirstName"] = new Driver(false).FirstName,
                ["LastName"] = new Driver(false).LastName,
                ["TeamName"] = Team.DefaultName,
                ["CarID"] = Basics.NoID,
                ["ScorePoints"] = new Entry(false).ScorePoints,
                ["RegisterDate"] = DateTime.Now
            };
            if (int.TryParse(ReadValueFromColumn(VarMap, row, "Startnummer"), out int raceNumber)) { values["RaceNumber"] = raceNumber; }
            long steamID = Driver.String2LongSteamID(ReadValueFromColumn(VarMap, row, "SteamID64"));
            if (Driver.IsValidSteamID(steamID)) { values["SteamID"] = steamID; }
            if (Int64.TryParse(ReadValueFromColumn(VarMap, row, "DiscordID"), out long discordID) && Driver.IsValidDiscordID(discordID)) { values["DiscordID"] = discordID; }
            values["FirstName"] = ReadValueFromColumn(VarMap, row, "Vorname") ?? values["FirstName"];
            values["LastName"] = ReadValueFromColumn(VarMap, row, "Nachname") ?? values["LastName"];
            values["TeamName"] = ReadValueFromColumn(VarMap, row, "Teamname") ?? values["TeamName"];
            List<Car> cars = Car.Statics.GetBy(nameof(Car.Name_GTRC), ReadValueFromColumn(VarMap, row, "Fahrzeug"));
            if (cars.Count > 0) { values["CarID"] = cars[0].ID; }
            string? scorePoints = ReadValueFromColumn(VarMap, row, "Stammfahrer oder Gaststarter");
            if (scorePoints == "Stammfahrer") { values["ScorePoints"] = true; } else { values["ScorePoints"] = false; }
            if (DateTime.TryParse(ReadValueFromColumn(VarMap, row, "Zeitstempel"), out DateTime registerDate)) { values["RegisterDate"] = registerDate; }
            return values;
        }

        public static string? ReadValueFromColumn(Dictionary<string, int> VarMap, List<object> row, string strKey)
        {
            if (VarMap.TryGetValue(strKey, out int intKey) && row.Count > intKey && row[intKey] is not null)
            {
                string? strVal = row[intKey].ToString();
                if (strVal is null) { return null; }
                else { return Basics.RemoveSpaceStartEnd(strVal); }
            }
            else { return null; }
        }

        public static Driver SyncDriver(Dictionary<string, dynamic> values)
        {
            long SteamID = values["SteamID"];
            long DiscordID = values["DiscordID"];
            string FirstName = values["FirstName"];
            string LastName = values["LastName"];
            DateTime RegisterDate = values["RegisterDate"];
            if (Driver.IsValidSteamID(SteamID))
            {
                Driver driver = Driver.Statics.GetByUniqProp(SteamID);
                if (driver.ID == Basics.NoID) { driver = Driver.Statics.WriteSQL(new Driver { SteamID = SteamID }); }
                if (driver.SteamID == Driver.SteamIDMinValue) { driver.SteamID = SteamID; }
                if (driver.DiscordID == Basics.NoID) { driver.DiscordID = DiscordID; }
                if (driver.FirstName == "") { driver.FirstName = FirstName; }
                if (driver.LastName == "") { driver.LastName = LastName; }
                if (RegisterDate < driver.RegisterDate) { driver.RegisterDate = RegisterDate; }
                return driver;
            }
            else { return new Driver(false); }
        }

        public static DriversTeams SyncTeam(Dictionary<string, dynamic> values, int seasonID)
        {
            Driver driver = SyncDriver(values);
            string TeamName = values["TeamName"];
            if (driver.ID != Basics.NoID && TeamName != Team.DefaultName)
            {
                Team team = Team.Statics.GetByUniqProp(new List<dynamic>() { seasonID, TeamName });
                if (team.ID == Basics.NoID) { team = Team.Statics.WriteSQL(new Team { SeasonID = seasonID, Name = TeamName }); }
                DriversTeams driverTeam = DriversTeams.Statics.GetByUniqProp(new List<dynamic>() { driver.ID, team.ID });
                if (driverTeam.ID == Basics.NoID) { driverTeam = DriversTeams.Statics.WriteSQL(new DriversTeams { DriverID = driver.ID, TeamID = team.ID }); }
                return driverTeam;
            }
            else { return new DriversTeams(false); }
        }

        public static bool SyncEntry(Dictionary<string, dynamic> values, int seasonID, bool _newEntry)
        {
            bool newEntry = false;
            DriversTeams driverTeam = SyncTeam(values, seasonID);
            int RaceNumber = values["RaceNumber"];
            int CarID = values["CarID"];
            bool ScorePoints = values["ScorePoints"];
            DateTime RegisterDate = values["RegisterDate"];
            if (driverTeam.ID != Basics.NoID && RaceNumber != Basics.NoID)
            {
                Entry entry = Entry.Statics.GetByUniqProp(new List<dynamic>() { seasonID, RaceNumber });
                if (entry.ID == Basics.NoID) { entry = Entry.Statics.WriteSQL(new Entry { SeasonID = seasonID, RaceNumber = RaceNumber }); newEntry = true; }
                DriversEntries driverEntry = DriversEntries.GetByDriverIDSeasonID(driverTeam.DriverID, entry.SeasonID);
                if (driverEntry.ID == Basics.NoID) { driverEntry = DriversEntries.Statics.WriteSQL(new DriversEntries { DriverID = driverTeam.DriverID }); }
                driverEntry.EntryID = entry.ID;
                entry.TeamID = driverTeam.TeamID;
                if ((newEntry || entry.CarID == Basics.NoID) && CarID != Basics.NoID) { entry.CarID = CarID; }
                if (newEntry) { entry.ScorePoints = ScorePoints; entry.Permanent = ScorePoints; }
                if (RegisterDate < entry.RegisterDate) { entry.RegisterDate = RegisterDate; }
            }
            return newEntry || _newEntry;
        }

        public static void SyncFormsEntries(int seasonID, string docID, string sheetID, string range)
        {
            dynamic rows = LoadRange(docID, sheetID, range);
            if (rows?.Count > 1)
            {
                Dictionary<string, int> VarMap = CreateVarMap(rows[0], new List<string> { "Startnummer" });
                List<Entry> iterateListEntry = Entry.Statics.GetBy(nameof(Entry.SeasonID), seasonID);
                foreach (Entry entry in iterateListEntry)
                {
                    for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                    {
                        if (ReadValuesFromRow(VarMap, rows[rowNr])["RaceNumber"] == entry.RaceNumber) { break; }
                        if (rowNr == rows.Count - 1) { entry.ListRemove(true); }
                    }
                }
                VarMap = CreateVarMap(rows[0], new List<string> { "Teamname" });
                List<Team> iterateListTeam = Team.Statics.GetBy(nameof(Team.SeasonID), seasonID);
                foreach (Team team in iterateListTeam)
                {
                    for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                    {
                        if (ReadValuesFromRow(VarMap, rows[rowNr])["TeamName"] == team.Name) { break; }
                        if (rowNr == rows.Count - 1) { team.ListRemove(true); }
                    }
                }
                bool newEntry = false;
                VarMap = CreateVarMap(rows[0], VarListEntries);
                for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                {
                    Dictionary<string, dynamic> values = ReadValuesFromRow(VarMap, rows[rowNr]);
                    newEntry = SyncEntry(values, seasonID, newEntry);
                }
                EventsEntries.Statics.WriteSQL();
                DriversTeams.Statics.WriteSQL();
                DriversEntries.Statics.WriteSQL();
                Entry.Statics.WriteSQL();
                Team.Statics.WriteSQL();
                Driver.Statics.WriteSQL();
                if (newEntry) { _ = Commands.CreateStartingGridMessage(Event.GetNextEvent(seasonID, DateTime.Now).ID, false, false); }
            }
        }

        public static void UpdatePreQStandings(string docID, string sheetID)
        {
            List<List<object>> rows = new();
            List<object> values = new() { "Pos", "Fahrer", "Nr", "Team", "Fahrzeug", "Schnitt", "Abstand", "Intervall", "Schnitt", "Schnitt", "Schnitt",
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
                    List<DriversEntries> _driverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entry.ID);
                    string driverText = "";
                    foreach (DriversEntries _driverEntry in _driverEntries) { driverText += Driver.Statics.GetByID(_driverEntry.DriverID).FullName + ", "; }
                    driverText = driverText.Substring(0, Math.Max(0, driverText.Length - 2));
                    int average = _resultsLine.Average;
                    int average1 = PreQualiResultLine.Statics.List[Math.Max(0, rowNr - 1)].Average;
                    values.Add((rowNr + 1).ToString() + ".");
                    values.Add(driverText);
                    values.Add(_entry.RaceNumber.ToString());
                    Team _team = Team.Statics.GetByID(_entry.TeamID);
                    if (_team.ReadyForList) { values.Add(_team.Name); } else { values.Add("Gaststarter"); }
                    values.Add(Car.Statics.GetByID(EventsEntries.GetLatestEventsEntries(_entry, DateTime.Now).CarID).Name);
                    if (average < int.MaxValue)
                    {
                        values.Add(Basics.Ms2Laptime(average));
                        if (average != average0 && average0 < int.MaxValue) { values.Add("'+" + Basics.Ms2Laptime(average - average0)); }
                        else { values.Add(""); }
                        if (average != average1 && average1 < int.MaxValue) { values.Add("'+" + Basics.Ms2Laptime(average - average1)); }
                        else { values.Add(""); }
                    }
                    else { values.Add(""); values.Add(""); values.Add(""); }
                    if (_resultsLine.Average1 < int.MaxValue) { values.Add(Basics.Ms2Laptime(_resultsLine.Average1)); }
                    else { values.Add(""); }
                    if (_resultsLine.Average2 < int.MaxValue) { values.Add(Basics.Ms2Laptime(_resultsLine.Average2)); }
                    else { values.Add(""); }
                    if (_resultsLine.DiffAverage > 0 && _resultsLine.DiffAverage < int.MaxValue) { values.Add(Math.Round((double)_resultsLine.DiffAverage / 100000, 3).ToString() + "%"); }
                    else { values.Add(""); }
                    values.Add(_resultsLine.LapsCount.ToString() + "L");
                    values.Add(_resultsLine.LapsCount1.ToString() + "L");
                    values.Add(_resultsLine.LapsCount2.ToString() + "L");
                    values.Add(_resultsLine.ValidLapsCount.ToString() + "L");
                    values.Add(_resultsLine.ValidLapsCount1.ToString() + "L");
                    values.Add(_resultsLine.ValidLapsCount2.ToString() + "L");
                    if (_resultsLine.BestLap1 < int.MaxValue) { values.Add(Basics.Ms2Laptime(_resultsLine.BestLap1)); }
                    else { values.Add(""); }
                    if (_resultsLine.BestLap2 < int.MaxValue) { values.Add(Basics.Ms2Laptime(_resultsLine.BestLap2)); }
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

        public static void UpdateBoPStandings(Event currentEvent, string docID, string sheetID)
        {
            List<EventsCars> eventsCars = EventsCars.GetAnyBy(nameof(EventsCars.EventID), currentEvent.ID);
            List<List<object>> rows = new();
            List<object> values = new() { "Pos", "Fahrzeug", "Jahr", "Anz.", "Ballast", "Restr." };
            rows.Add(values);
            values = new List<object>() { "", "", "", "", "", "" };
            rows.Add(values);
            int pos = 1;
            int count0 = int.MaxValue;
            foreach (EventsCars eventCar in eventsCars)
            {
                if (eventCar.ObjCar.Category == "GT3" && eventCar.ObjCar.IsLatestVersion)
                {
                    values = new List<object>();
                    if (eventCar.CountBoP == count0) { values.Add("'="); }
                    else { values.Add(pos.ToString() + "."); }
                    values.Add(eventCar.ObjCar.Name);
                    values.Add(eventCar.ObjCar.Year.ToString());
                    if (eventCar.CountBoP == 0) { values.Add(""); }
                    else { values.Add(eventCar.CountBoP.ToString() + "x"); }
                    if (eventCar.Ballast == 0) { values.Add("'-"); }
                    else { values.Add(eventCar.Ballast.ToString() + " kg"); }
                    if (eventCar.Restrictor == 0) { values.Add("'-"); }
                    else { values.Add(eventCar.Restrictor.ToString() + "%"); }
                    rows.Add(values);
                    count0 = eventCar.CountBoP;
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

        public static void UpdateEntriesCurrentEvent(string docID, string sheetID, Event _event)
        {
            List<List<object>> rows = new();
            List<object> values = new() { "Zeitstempel", "Vorname", "Nachname", "SteamID64", "Fahrzeug", "Teamname", "Startnummer", "Stammfahrer oder Gaststarter", "Angemeldet" };
            rows.Add(values);
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), _event.SeasonID);
            foreach (Entry _entry in listEntries)
            {
                values = new List<object>();
                List<DriversEntries> _driverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entry.ID);
                if (_driverEntries.Count > 0)
                {
                    EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                    values.Add(_entry.RegisterDate.ToString());
                    values.Add(_driverEntries[0].ObjDriver.FirstName);
                    values.Add(_driverEntries[0].ObjDriver.LastName);
                    values.Add("S" + _driverEntries[0].ObjDriver.SteamID.ToString());
                    values.Add(_eventsEntries.ObjCar.Name_GTRC);
                    values.Add(_entry.ObjTeam?.Name ?? "");
                    values.Add(_entry.RaceNumber.ToString());
                    if (_eventsEntries.ScorePoints) { values.Add("Stammfahrer"); } else { values.Add("Gaststarter"); }
                    values.Add("TRUE");
                    rows.Add(values);
                }
            }
            if (rows.Count > 0)
            {
                string range = "A1:I";
                ClearRange(docID, sheetID, range);
                range += (rows.Count).ToString();
                UpdateRange(docID, sheetID, range, rows);
            }
        }

        public static void UpdateCarChanges(string docID, string sheetID, int seasonID)
        {
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), seasonID);
            List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), seasonID));
            List<List<object>> rows = new();
            List<object> values = new() { "Fahrzeugwechsel", "Fahrzeugwechsel" };
            rows.Add(values);
            values = new List<object>() { "Fahrer", "Nach Rennen" };
            rows.Add(values);
            foreach (Entry _entry in listEntries)
            {
                List<DriversEntries> _driverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entry.ID);
                if (_driverEntries.Count > 0)
                {
                    for (int eventNr = 1; eventNr < listEvents.Count; eventNr++)
                    {
                        Event _event0 = listEvents[eventNr - 1];
                        Event _event1 = listEvents[eventNr];
                        EventsEntries _eventsEntries0 = EventsEntries.GetAnyByUniqProp(_entry.ID, _event0.ID);
                        EventsEntries _eventsEntries1 = EventsEntries.GetAnyByUniqProp(_entry.ID, _event1.ID);
                        if (_eventsEntries1.CarID != _eventsEntries0.CarID)
                        {
                            values = new List<object>();
                            values.Add(_driverEntries[0].ObjDriver.FullName);
                            values.Add(_event0.ObjTrack.Name_GTRC);
                            rows.Add(values);
                        }
                    }
                }
            }
            if (rows.Count > 0)
            {
                string range = "AC2:AD";
                ClearRange(docID, sheetID, range);
                range += (rows.Count + 1).ToString();
                UpdateRange(docID, sheetID, range, rows);
            }
        }
    }
}
