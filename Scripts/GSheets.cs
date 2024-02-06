using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Database;
using System;
using System.Collections.Generic;
using System.IO;

using GTRC_Community_Manager;
using System.Linq;
using Discord;

namespace Scripts
{
    public static class GSheets
    {
        private static string PathCrendentials = MainWindow.dataDirectory + "googlesheets_projectinfo.json";
        private static readonly string appName = "GTRC Community Manager";
        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static GoogleCredential Crendentials;
        private static SheetsService GSheetService;

        public static readonly List<string> VarListEntries = new() { "Startnummer", "SteamID64", "DiscordID", "Vorname", "Nachname", "Teamname", "Fahrzeug", "Stammfahrer oder Gaststarter", "Zeitstempel" };

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

        public static void UpdateFontColor(string docID, string sheetID, List<GSheetRange> ranges, int colorID)
        {
            try
            {
                Initialize();
                Spreadsheet spreadsheet = GSheetService.Spreadsheets.Get(docID).Execute();
                Sheet sheet = spreadsheet.Sheets.Where(s => s.Properties.Title == sheetID).FirstOrDefault();
                int sheetId = (int)sheet.Properties.SheetId;
                ThemeColor color = ThemeColor.Statics.GetByID(colorID);
                var userEnteredFormat = new CellFormat()
                {
                    TextFormat = new TextFormat()
                    {
                        ForegroundColor = new Google.Apis.Sheets.v4.Data.Color()
                        {
                            Blue = (float)color.Blue / 255,
                            Red = (float)color.Red / 255,
                            Green = (float)color.Green / 255,
                            Alpha = (float)color.Alpha / 255
                        }
                    }
                };
                BatchUpdateSpreadsheetRequest bussr = new()
                {
                    Requests = new List<Request>()
                };
                foreach (GSheetRange range in ranges)
                {
                    var updateCellsRequest = new Request()
                    {
                        RepeatCell = new RepeatCellRequest()
                        {
                            Range = new GridRange()
                            {
                                SheetId = sheetId,
                                StartColumnIndex = range.Col0,
                                StartRowIndex = range.Row0,
                                EndColumnIndex = range.Col1,
                                EndRowIndex = range.Row1
                            },
                            Cell = new CellData()
                            {
                                UserEnteredFormat = userEnteredFormat
                            },
                            Fields = "userEnteredFormat.textFormat.foregroundColor"
                        }
                    };
                    bussr.Requests.Add(updateCellsRequest);
                }
                var response = GSheetService.Spreadsheets.BatchUpdate(bussr, docID).Execute();
            }
            catch { MainVM.List[0].LogCurrentText = "Update Google-Sheets font color failed! [sheetID: " + sheetID + "]"; }
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
                ["Permanent"] = new Entry(false).Permanent,
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
            string? permanent = ReadValueFromColumn(VarMap, row, "Stammfahrer oder Gaststarter");
            if (permanent == "Stammfahrer") { values["Permanent"] = true; } else { values["Permanent"] = false; }
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
            if (Driver.IsValidSteamID(SteamID) && FirstName.Length > 0 && LastName.Length > 0)
            {
                Driver driver = Driver.Statics.GetByUniqProp(SteamID);
                if (driver.ID == Basics.NoID) { driver = new() { SteamID = SteamID }; }
                if (driver.SteamID == Driver.SteamIDMinValue) { driver.SteamID = SteamID; }
                if (driver.DiscordID == Basics.NoID) { driver.DiscordID = DiscordID; }
                if (driver.FirstName == "") { driver.FirstName = FirstName; }
                if (driver.LastName == "") { driver.LastName = LastName; }
                if (RegisterDate < driver.RegisterDate) { driver.RegisterDate = RegisterDate; }
                driver = Driver.Statics.WriteSQL(driver);
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
            bool Permanent = values["Permanent"];
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
                if (newEntry) { entry.Permanent = Permanent; entry.Permanent = Permanent; }
                if (RegisterDate < entry.RegisterDate) { entry.RegisterDate = RegisterDate; }
            }
            return newEntry || _newEntry;
        }

        public static int SyncEntryByDriver(Dictionary<string, dynamic> values, int seasonID)
        {
            Driver driver = SyncDriver(values);
            int RaceNumber = values["RaceNumber"];
            string TeamName = values["TeamName"];
            int CarID = values["CarID"];
            DateTime RegisterDate = values["RegisterDate"];
            bool Permanent = values["Permanent"];
            if (driver.ID != Basics.NoID && RegisterDate < driver.BanDate && RaceNumber != Basics.NoID && TeamName.Length > 0 && Car.Statics.ExistsID(CarID) &&
                DriversEntries.GetListByDriverIDSeasonID(driver.ID, seasonID).Count == 0 && !Entry.Statics.ExistsUniqProp(new List<dynamic>() { seasonID, RaceNumber }))
            {
                Team team = Team.Statics.GetByUniqProp(new List<dynamic>() { seasonID, TeamName });
                if (team.ID == Basics.NoID) { team = Team.Statics.WriteSQL(new Team { SeasonID = seasonID, Name = TeamName }); }
                DriversTeams driverTeam = DriversTeams.Statics.GetByUniqProp(new List<dynamic>() { driver.ID, team.ID });
                if (driverTeam.ID == Basics.NoID) { driverTeam = DriversTeams.Statics.WriteSQL(new DriversTeams { DriverID = driver.ID, TeamID = team.ID }); }
                Entry entry = new() { SeasonID = seasonID, RaceNumber = RaceNumber, TeamID = driverTeam.TeamID, CarID = CarID, RegisterDate = RegisterDate, Permanent = Permanent };
                entry = Entry.Statics.WriteSQL(entry);
                _ = DriversEntries.Statics.WriteSQL(new DriversEntries { DriverID = driverTeam.DriverID, EntryID = entry.ID });
                return entry.RaceNumber;
            }
            return Basics.NoID;
        }

        public static void SyncFormsEntries(int seasonID, string docID, string sheetID, string range)
        {
            dynamic rows = LoadRange(docID, sheetID, range);
            if (rows?.Count > 1)
            {
                List<int> newEntries = new();
                Dictionary<string, int> VarMap = CreateVarMap(rows[0], VarListEntries);
                for (int rowNr = 1; rowNr < rows.Count; rowNr++)
                {
                    Dictionary<string, dynamic> values = ReadValuesFromRow(VarMap, rows[rowNr]);
                    int newEntry = SyncEntryByDriver(values, seasonID);
                    if (newEntry > Basics.NoID) { newEntries.Add(newEntry); }
                }
                if (newEntries.Count > 0)
                {
                    string message = "Neue Anmeldung";
                    if (newEntries.Count > 1) { message += "en"; }
                    message += ":";
                    foreach (int _raceNumber in newEntries) { message += "\n- #" + _raceNumber.ToString(); }
                    _ = Commands.NotifyAdmins(message);
                    _ = Commands.CreateStartingGridMessage(Event.GetNextEvent(seasonID, DateTime.Now).ID, false, false);
                }
            }
        }

        public static void UpdatePreQStandings(string docID, string sheetID)
        {
            List<List<object>> rows = new();
            List<object> values = new() { "Pos", "Fahrer", "Nr", "Team", "Fahrzeug", "Schnitt", "Abstand", "Intervall", "Schnitt", "Schnitt", "Schnitt",
                "Anzahl Runden", "Anzahl Runden", "Anzahl Runden", "Anzahl gültige Runden", "Anzahl gültige Runden", "Anzahl gültige Runden", "Bestzeit", "Bestzeit",
                "Bestzeit", "Anzahl Stints", "Anzahl Stints", "Anzahl Stints" };
            rows.Add(values);
            values = new List<object>() { "Pos", "Fahrer", "Nr", "Team", "Fahrzeug", "Schnitt", "Abstand", "Intervall", "Kyalami", "Laguna Seca", "Differenz", "Gesamt",
                "Kyalami", "Laguna Seca", "Gesamt", "Kyalami", "Laguna Seca", "Kyalami", "Laguna Seca", "Differenz", "Gesamt", "Kyalami", "Laguna Seca" };
            rows.Add(values);
            List<GSheetRange> rangesCol3 = new();
            List<GSheetRange> rangesCol5 = new();
            List<long> steamIDsFixPreQ = PreQualiResultLine.SteamIDsFixPreQ;
            if (PreQualiResultLine.Statics.List.Count > 0)
            {
                int seasonID = Entry.Statics.GetByID(PreQualiResultLine.Statics.List[0].EntryID).SeasonID;
                int minEntrySlots = Season.Statics.GetByID(seasonID).GridSlotsLimit;
                List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), seasonID);
                foreach (Event _event in listEvents) { minEntrySlots = Math.Min(minEntrySlots, Track.Statics.GetByID(_event.TrackID).ServerSlotsCount); }
                values = new List<object>() { "FIX QUALIFIZIERT - Top 10 Fahrerwertung letzter Saison + Einladungen", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                    "", "", "", "", "" };
                rows.Add(values);
                int pos = 0;
                int average0 = PreQualiResultLine.Statics.List[0].Average;
                foreach (long _steamIDfixPreQ in steamIDsFixPreQ)
                {
                    pos++;
                    string fullName = Driver.Statics.GetByUniqProp(_steamIDfixPreQ).FullName;
                    values = new List<object>() { pos.ToString() + ".", fullName, "-", "-", "-", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                    rows.Add(values);
                }
                values = new List<object>() { "QUALIFIZIERT NACH PRE-QUALIFYING - Anmeldungen dieser Saison", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                    "", "", "", "", "", "" };
                rows.Add(values);
                pos = 0;
                int priorityPos = 0;
                for (int rowNr = 0; rowNr < PreQualiResultLine.Statics.List.Count; rowNr++)
                {
                    pos++;
                    values = new List<object>();
                    PreQualiResultLine _resultsLine = PreQualiResultLine.Statics.List[rowNr];
                    int _id = _resultsLine.EntryID;
                    Entry _entry = Entry.Statics.GetByID(_id);
                    List<DriversEntries> _driverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entry.ID);
                    string driverText = "";
                    foreach (DriversEntries _driverEntry in _driverEntries) { driverText += Driver.Statics.GetByID(_driverEntry.DriverID).FullName + ", "; }
                    driverText = driverText[..Math.Max(0, driverText.Length - 2)];
                    int average = _resultsLine.Average;
                    int average1 = PreQualiResultLine.Statics.List[Math.Max(0, rowNr - 1)].Average;
                    values.Add(pos.ToString() + ".");
                    values.Add(driverText);
                    values.Add(_entry.RaceNumber.ToString());
                    Team _team = Team.Statics.GetByID(_entry.TeamID);
                    if (_team.ReadyForList) { values.Add(_team.Name); } else { values.Add(""); }
                    values.Add(Car.Statics.GetByID(_entry.GetEntriesDatetimesByDate(DateTime.Now).CarID).Name);
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
                    bool isFixPreQ = false;
                    List<DriversEntries> _driversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), PreQualiResultLine.Statics.List[rowNr].EntryID);
                    int lineGSheet = 1;
                    foreach (DriversEntries _driverEntry in _driversEntries)
                    {
                        for (int _fixPreQNr = 0; _fixPreQNr < steamIDsFixPreQ.Count; _fixPreQNr++)
                        {
                            if (_driverEntry.ReadyForList && _driverEntry.ObjDriver.SteamID == steamIDsFixPreQ[_fixPreQNr])
                            {
                                for (int valueNr = 1; valueNr < values.Count; valueNr++) { rows[_fixPreQNr + 3][valueNr] = values[valueNr]; }
                                isFixPreQ = true; lineGSheet = _fixPreQNr + 4; break;
                            }
                        }
                        if (isFixPreQ) { break; }
                    }
                    if (isFixPreQ) { pos--; } else { rows.Add(values); lineGSheet = rows.Count; }
                    if (_entry.Permanent && _entry.ScorePoints && _entry.SignOutDate > DateTime.Now)
                    {
                        priorityPos++;
                        if (priorityPos <= minEntrySlots) { rangesCol3.Add(new GSheetRange() { Col1 = 1, Row1 = lineGSheet }); }
                    }
                    bool isBest = true;
                    foreach (PreQualiResultLine _preQLine in PreQualiResultLine.Statics.List) { if (_preQLine.Average < _resultsLine.Average) { isBest = false; break; } }
                    if (isBest) { rangesCol5.Add(new GSheetRange() { Col1 = 6, Row1 = lineGSheet }); } else { isBest = true; }
                    foreach (PreQualiResultLine _preQLine in PreQualiResultLine.Statics.List) { if (_preQLine.Average1 < _resultsLine.Average1) { isBest = false; break; } }
                    if (isBest) { rangesCol5.Add(new GSheetRange() { Col1 = 9, Row1 = lineGSheet }); } else { isBest = true; }
                    foreach (PreQualiResultLine _preQLine in PreQualiResultLine.Statics.List) { if (_preQLine.Average2 < _resultsLine.Average2) { isBest = false; break; } }
                    if (isBest) { rangesCol5.Add(new GSheetRange() { Col1 = 10, Row1 = lineGSheet }); } else { isBest = true; }
                    foreach (PreQualiResultLine _preQLine in PreQualiResultLine.Statics.List) { if (_preQLine.BestLap1 < _resultsLine.BestLap1) { isBest = false; break; } }
                    if (isBest) { rangesCol5.Add(new GSheetRange() { Col1 = 18, Row1 = lineGSheet }); } else { isBest = true; }
                    foreach (PreQualiResultLine _preQLine in PreQualiResultLine.Statics.List) { if (_preQLine.BestLap2 < _resultsLine.BestLap2) { isBest = false; break; } }
                    if (isBest) { rangesCol5.Add(new GSheetRange() { Col1 = 19, Row1 = lineGSheet }); } else { isBest = true; }
                }
            }
            string range = "A1:W";
            ClearRange(docID, sheetID, range);
            range += rows.Count.ToString();
            UpdateRange(docID, sheetID, range, rows);
            List<GSheetRange> rangesCol4 = new() { new GSheetRange() { Col1 = 1, Row0 = 2, Row1 = rows.Count } };
            UpdateFontColor(docID, sheetID, rangesCol4, 4);
            rangesCol3.Add(new GSheetRange() { Col0 = 1, Col1 = 23, Row0 = 2, Row1 = rows.Count });
            if (rangesCol3.Count > 0) { UpdateFontColor(docID, sheetID, rangesCol3, 3); }
            if (rangesCol5.Count > 0) { UpdateFontColor(docID, sheetID, rangesCol5, 4); }
        }

        public static void UpdateBoPStandings(Event currentEvent, string docID, string sheetID)
        {
            List<EventsCars> eventsCars = EventsCars.SortByCount(EventsCars.GetAnyBy(nameof(EventsCars.EventID), currentEvent.ID));
            List<List<object>> rows = new();
            List<object> values = new() { "Pos", "Fahrzeug", "Jahr", "Anz.", "Ballast", "Restr." };
            rows.Add(values);
            values = new List<object>() { "", "", "", "", "", "" };
            rows.Add(values);
            int pos = 1;
            int count0 = int.MaxValue;
            foreach (EventsCars eventCar in eventsCars)
            {
                if (eventCar.ObjCar.Category == "GT3" && eventCar.ObjCar.IsLatestModel)
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
                range += rows.Count.ToString();
                UpdateRange(docID, sheetID, range, rows);
            }
        }

        public static void UpdateEntriesCurrentEvent(string docID, string sheetID, Event _event)
        {
            List<List<object>> rows = new();
            List<object> values = new() { "Zeitstempel", "Vorname", "Nachname", "SteamID64", "Fahrzeug", "Teamname", "Startnummer", "Stammfahrer oder Gaststarter", "Angemeldet" };
            rows.Add(values);
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), _event.SeasonID);
            List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), _event.SeasonID));
            foreach (Entry _entry in listEntries)
            {
                if (listEvents.Count > 0 && _entry.SignOutDate > listEvents[0].Date)
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
                        values.Add(_entry.GetEntriesDatetimesByDate(_event.Date).ObjCar.Name_GTRC);
                        values.Add(_entry.ObjTeam?.Name ?? "");
                        values.Add(_entry.RaceNumber.ToString());
                        if (_eventsEntries.ScorePoints) { values.Add("Stammfahrer"); } else { values.Add("Gaststarter"); }
                        values.Add("TRUE");
                        rows.Add(values);
                    }
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

        public static void UpdatePointsResets(string docID, string sheetID, int seasonID)
        {
            Season season = Season.Statics.GetByID(seasonID);
            bool groupCarLimits = season.GroupCarLimits;
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
                        if (_entry.GetLatestCarChangeDate(_event1.Date) > _event0.Date)
                        {
                            values = new List<object> { _driverEntries[0].ObjDriver.FullName, _event0.ObjTrack.Name_GTRC };
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



    public class GSheetRange
    {
        private int col0 = Basics.NoID;
        private int row0 = Basics.NoID;
        private int col1 = Basics.NoID;
        private int row1 = Basics.NoID;

        public int Col0 { get { return col0; } set { if (value > 0) { col0 = value; } } }
        public int Row0 { get { return row0; } set { if (value > 0) { row0 = value; } } }
        public int Col1 { get { return col1; } set { if (value > 0) { col1 = value; if (col0 == Basics.NoID) { col0 = col1 - 1; } } } }
        public int Row1 { get { return row1; } set { if (value > 0) { row1 = value; if (row0 == Basics.NoID) { row0 = row1 - 1; } } } }
    }
}
