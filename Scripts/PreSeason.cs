using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Reflection;

namespace Scripts
{
    public static class PreSeason
    {

        public static bool TryReadResultsJson(string path)
        {
            Thread.Sleep(10);
            try
            {
                int tempLBLs = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path, Encoding.Unicode)).sessionResult.leaderBoardLines.Count;
                int tempLaps = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path, Encoding.Unicode)).laps.Count;
            }
            catch { return false; }
            return true;
        }

        public static void AddResultsJson(string path, int seasonID)
        {
            //Sollte dynamisch sein:
            int attemptMax = 1000;
            Dictionary<string, int> QualiTracks = new() { { "nurburgring", 0 }, { "misano", 1 } };

            for (int attemptNr = 0; attemptNr < attemptMax; attemptNr++)
            {
                if (TryReadResultsJson(path)) { break; }
                else { Console.WriteLine("Versuch-Nr " + attemptNr.ToString() + ": Pre-Quali Results nicht einlesbar."); }
            }
            if (TryReadResultsJson(path))
            {
                var resultsJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path, Encoding.Unicode));
                var leaderBoardLines = resultsJson.sessionResult.leaderBoardLines;
                var laps = resultsJson.laps;
                int trackID = new Lap(false).Track;
                if (resultsJson.trackName is JValue)
                {
                    string _trackName = resultsJson.trackName.ToString();
                    if (QualiTracks.ContainsKey(_trackName)) { trackID = QualiTracks[_trackName]; }
                }
                for (int lapNr = 0; lapNr < laps.Count; lapNr++)
                {
                    Lap lap = new() { Track = trackID };
                    var _time = laps[lapNr].laptime;
                    if (_time is JValue) { if (Int32.TryParse(_time.ToString(), out int time)) { lap.Time = time; } }
                    if (laps[lapNr].splits is IList && laps[lapNr].splits.Count > 2)
                    {
                        _time = laps[lapNr].splits[0];
                        if (_time is JValue) { if (Int32.TryParse(_time.ToString(), out int time)) { lap.Sector1 = time; } }
                        _time = laps[lapNr].splits[1];
                        if (_time is JValue) { if (Int32.TryParse(_time.ToString(), out int time)) { lap.Sector2 = time; } }
                        _time = laps[lapNr].splits[2];
                        if (_time is JValue) { if (Int32.TryParse(_time.ToString(), out int time)) { lap.Sector3 = time; } }
                    }
                    var _valid = laps[lapNr].isValidForBest;
                    if (_valid is JValue) { if (Boolean.TryParse(_valid.ToString(), out bool valid)) { lap.Valid = valid; } }
                    int carID = -1;
                    var _carID = laps[lapNr].carId;
                    if (_carID is JValue) { Int32.TryParse(_carID.ToString(), out carID); }
                    int driverNr = -1;
                    var _driverNr = laps[lapNr].driverIndex;
                    if (_driverNr is JValue) { Int32.TryParse(_driverNr.ToString(), out driverNr); }
                    string steamID = Basics.NoID.ToString();
                    if (carID > -1 && driverNr > -1)
                    {
                        for (int pos = 0; pos < leaderBoardLines.Count; pos++)
                        {
                            if (leaderBoardLines[pos].car is JObject)
                            {
                                int carID_lbl = -1;
                                var _carID_lbl = leaderBoardLines[pos].car.carId;
                                if (_carID_lbl is JValue && Int32.TryParse(_carID_lbl.ToString(), out carID_lbl) && carID == carID_lbl)
                                {
                                    if (leaderBoardLines[pos].car.drivers is IList && leaderBoardLines[pos].car.drivers.Count > driverNr)
                                    {
                                        if (leaderBoardLines[pos].car.drivers[driverNr] is JObject && leaderBoardLines[pos].car.drivers[driverNr].playerId is JValue)
                                        {
                                            steamID = leaderBoardLines[pos].car.drivers[driverNr].playerId.ToString();
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    int _entryID = Basics.NoID;
                    Driver driver = Driver.Statics.GetByUniqProp(Driver.String2LongSteamID(steamID));
                    _entryID = DriversEntries.GetByDriverIDSeasonID(driver.ID, seasonID).EntryID;
                    if (_entryID != Basics.NoID) { lap.EntryID = _entryID; }
                }
            }
        }

        public static void ResetPreQResults(int seasonID)
        {
            PreQualiResultLine.Statics.ResetSQL();
            PreQualiResultLine.Statics.LoadSQL();
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), seasonID);
            foreach (Entry _entry in listEntries) { _ = new PreQualiResultLine { EntryID = _entry.RaceNumber }; }
            PreQualiResultLine.Statics.WriteJson();
            PreQualiResultLine.Statics.WriteSQL();
        }

        public static void UpdatePreQResults(int seasonID)
        {
            //Sollte dynamisch sein:
            double timeFactorMax = 1.07;
            int lapsCountStintMin = 10;
            Dictionary<string, int> QualiTracks = new() { { "nurburgring", 0 }, { "misano", 1 } };

            ResetPreQResults(seasonID);
            foreach (PreQualiResultLine preQualiResultLine in PreQualiResultLine.Statics.List)
            {
                List<Lap> lapsEntry = new();
                foreach (Lap _lap in Lap.Statics.List)
                {
                    if (_lap.EntryID == preQualiResultLine.EntryID)
                    {
                        lapsEntry.Add(_lap);
                    }
                }
                foreach (int _trackID in QualiTracks.Values)
                {
                    List<Lap> lapsEntryTrack = new();
                    foreach (Lap _lap in lapsEntry)
                    {
                        if (_lap.Track == _trackID)
                        {
                            lapsEntryTrack.Add(_lap);
                            preQualiResultLine.LapsCount++;
                            if (_lap.Valid) { preQualiResultLine.ValidLapsCount++; }
                            switch (_trackID)
                            {
                                case 0:
                                    preQualiResultLine.LapsCount1++;
                                    if (_lap.Valid) { preQualiResultLine.ValidLapsCount1++; }
                                    break;
                                case 1:
                                    preQualiResultLine.LapsCount2++;
                                    if (_lap.Valid) { preQualiResultLine.ValidLapsCount2++; }
                                    break;
                            }
                        }
                    }
                    for (int lapNr = 0; lapNr < lapsEntryTrack.Count; lapNr++)
                    {
                        if (lapsEntryTrack[lapNr].Valid)
                        {
                            switch (_trackID)
                            {
                                case 0: 
                                    preQualiResultLine.BestLap1 = Math.Min(preQualiResultLine.BestLap1, lapsEntryTrack[lapNr].Time);
                                    preQualiResultLine.BestSector1a = Math.Min(preQualiResultLine.BestSector1a, lapsEntryTrack[lapNr].Sector1);
                                    preQualiResultLine.BestSector3a = Math.Min(preQualiResultLine.BestSector3a, lapsEntryTrack[lapNr].Sector3);
                                    break;
                                case 1:
                                    preQualiResultLine.BestLap2 = Math.Min(preQualiResultLine.BestLap2, lapsEntryTrack[lapNr].Time);
                                    preQualiResultLine.BestSector1b = Math.Min(preQualiResultLine.BestSector1b, lapsEntryTrack[lapNr].Sector1);
                                    preQualiResultLine.BestSector3b = Math.Min(preQualiResultLine.BestSector3b, lapsEntryTrack[lapNr].Sector3);
                                    break;
                            }
                        }
                    }
                    int time107 = int.MaxValue;
                    int s1s3_107 = int.MaxValue;
                    switch (_trackID)
                    {
                        case 0:
                            time107 = (int)Math.Round(preQualiResultLine.BestLap1 * timeFactorMax, 0);
                            s1s3_107 = (int)Math.Round((preQualiResultLine.BestSector1a + preQualiResultLine.BestSector3a) * timeFactorMax, 0);
                            break;
                        case 1:
                            time107 = (int)Math.Round(preQualiResultLine.BestLap2 * timeFactorMax, 0);
                            s1s3_107 = (int)Math.Round((preQualiResultLine.BestSector1b + preQualiResultLine.BestSector3b) * timeFactorMax, 0);
                            break;
                    }
                    bool newStint = true;
                    List<Lap> lapsEntryTrackStint = new();
                    foreach (Lap _lap in lapsEntryTrack)
                    {
                        if (_lap.Time > time107 && _lap.Sector1 + _lap.Sector3 > s1s3_107) { lapsEntryTrackStint = new List<Lap>(); newStint = true; }
                        else if (_lap.Valid) { lapsEntryTrackStint.Add(_lap); }
                        if (lapsEntryTrackStint.Count >= lapsCountStintMin)
                        {
                            List<int> bestTimes = new();
                            foreach (Lap _lapStint in lapsEntryTrackStint) { bestTimes.Add(_lapStint.Time); }
                            for (int lapNr1 = 0; lapNr1 < lapsEntryTrackStint.Count - 1; lapNr1++)
                            {
                                for (int lapNr2 = lapNr1 + 1; lapNr2 < lapsEntryTrackStint.Count; lapNr2++)
                                {
                                    if (bestTimes[lapNr1] > bestTimes[lapNr2])
                                    {
                                        (bestTimes[lapNr2], bestTimes[lapNr1]) = (bestTimes[lapNr1], bestTimes[lapNr2]);
                                    }
                                }
                            }
                            if (newStint)
                            {
                                newStint = false;
                                preQualiResultLine.ValidStintsCount++;
                                switch (_trackID)
                                {
                                    case 0: preQualiResultLine.ValidStintsCount1++; break;
                                    case 1: preQualiResultLine.ValidStintsCount2++; break;
                                }
                            }
                            double totalTime = 0;
                            for (int lapNr = 0; lapNr < lapsCountStintMin; lapNr++) { totalTime += bestTimes[lapNr]; }
                            int newAverage = (int)Math.Round(totalTime / lapsCountStintMin, 0);
                            switch (_trackID)
                            {
                                case 0: preQualiResultLine.Average1 = Math.Min(preQualiResultLine.Average1, newAverage); break;
                                case 1: preQualiResultLine.Average2 = Math.Min(preQualiResultLine.Average2, newAverage); break;
                            }
                            if (preQualiResultLine.Average1 < int.MaxValue && preQualiResultLine.Average2 < int.MaxValue)
                            {
                                double _average = (preQualiResultLine.Average1 + preQualiResultLine.Average2) / 2;
                                preQualiResultLine.Average = (int)Math.Round(_average, 0);
                            }
                        }
                    }
                }
            }
            if (PreQualiResultLine.Statics.List.Count > 1)
            {
                var linqSortList = from _resultsLine in PreQualiResultLine.Statics.List orderby _resultsLine.Average1 select _resultsLine;
                PreQualiResultLine.Statics.List = linqSortList.Cast<PreQualiResultLine>().ToList();
                int OverallBest = PreQualiResultLine.Statics.List[0].Average1;
                if (OverallBest < int.MaxValue)
                {
                    foreach (PreQualiResultLine preQualiResultLine in PreQualiResultLine.Statics.List)
                    {
                        if (preQualiResultLine.Average1 < int.MaxValue)
                        {
                            double quotient = (double)preQualiResultLine.Average1 / (double)OverallBest;
                            quotient *= 100; quotient -= 100; quotient *= 100000;
                            preQualiResultLine.DiffAverage = Math.Min(preQualiResultLine.DiffAverage, (int)Math.Round(quotient));
                        }
                    }
                }
                linqSortList = from _resultsLine in PreQualiResultLine.Statics.List orderby _resultsLine.Average2 select _resultsLine;
                PreQualiResultLine.Statics.List = linqSortList.Cast<PreQualiResultLine>().ToList();
                OverallBest = PreQualiResultLine.Statics.List[0].Average2;
                if (OverallBest < int.MaxValue)
                {
                    foreach (PreQualiResultLine preQualiResultLine in PreQualiResultLine.Statics.List)
                    {
                        if (preQualiResultLine.Average2 < int.MaxValue)
                        {
                            double quotient = (double)preQualiResultLine.Average2 / (double)OverallBest;
                            quotient *= 100; quotient -= 100; quotient *= 100000;
                            preQualiResultLine.DiffAverage = Math.Min(preQualiResultLine.DiffAverage, (int)Math.Round(quotient));
                        }
                    }
                }
                linqSortList = from _resultsLine in PreQualiResultLine.Statics.List orderby _resultsLine.BestLap1 select _resultsLine;
                PreQualiResultLine.Statics.List = linqSortList.Cast<PreQualiResultLine>().ToList();
                OverallBest = PreQualiResultLine.Statics.List[0].BestLap1;
                if (OverallBest < int.MaxValue)
                {
                    foreach (PreQualiResultLine preQualiResultLine in PreQualiResultLine.Statics.List)
                    {
                        if (preQualiResultLine.BestLap1 < int.MaxValue)
                        {
                            double quotient = (double)preQualiResultLine.BestLap1 / (double)OverallBest;
                            quotient *= 100; quotient -= 100; quotient *= 100000;
                            preQualiResultLine.DiffBestLap = Math.Min(preQualiResultLine.DiffBestLap, (int)Math.Round(quotient));
                        }
                    }
                }
                linqSortList = from _resultsLine in PreQualiResultLine.Statics.List orderby _resultsLine.BestLap2 select _resultsLine;
                PreQualiResultLine.Statics.List = linqSortList.Cast<PreQualiResultLine>().ToList();
                OverallBest = PreQualiResultLine.Statics.List[0].BestLap2;
                if (OverallBest < int.MaxValue)
                {
                    foreach (PreQualiResultLine preQualiResultLine in PreQualiResultLine.Statics.List)
                    {
                        if (preQualiResultLine.BestLap2 < int.MaxValue)
                        {
                            double quotient = (double)preQualiResultLine.BestLap2 / (double)OverallBest;
                            quotient *= 100; quotient -= 100; quotient *= 100000;
                            preQualiResultLine.DiffBestLap = Math.Min(preQualiResultLine.DiffBestLap, (int)Math.Round(quotient));
                        }
                    }
                }
            }
            var linqList = from _resultsLine in PreQualiResultLine.Statics.List
                           orderby _resultsLine.Average, _resultsLine.DiffAverage, _resultsLine.DiffBestLap, _resultsLine.ValidStintsCount descending,
                           Entry.Statics.GetByID(_resultsLine.EntryID).RegisterDate
                           select _resultsLine;
            PreQualiResultLine.Statics.List = linqList.Cast<PreQualiResultLine>().ToList();
            PreQualiResultLine.Statics.WriteJson();
            PreQualiResultLine.Statics.WriteSQL();
        }

        public static void EntryAutoSignOut(Event _event, int SignOutLimit, int NoShowLimit)
        {
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), _event.SeasonID);
            List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), _event.SeasonID));
            foreach (Entry _entry in listEntries)
            {
                int SignOutCount = 0;
                int NoShowCount = 0;
                foreach (Event _tempEvent in listEvents)
                {
                    if (_tempEvent.EventDate <= _event.EventDate && _tempEvent.EventDate < DateTime.Now)
                    {
                        EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                        bool candidate = _entry.SignOutDate > _tempEvent.EventDate && _entry.RegisterDate < _tempEvent.EventDate;
                        if (candidate && !_eventsEntries.SignInState && _eventsEntries.ScorePoints) { SignOutCount++; }
                        if (candidate && _eventsEntries.SignInState && _eventsEntries.IsOnEntrylist && !_eventsEntries.Attended) { NoShowCount++; }
                    }
                }
                SignOutCount += NoShowCount;
                //if (_entry.SignOutDate > DateTime.Now && (SignOutCount > SignOutLimit || NoShowCount > NoShowLimit)) { _entry.SignOutDate = DateTime.Now; } Erst wenn results.json der Events eingelesen werden
            }
            //Entry.WriteSQL(); Erst wenn results.json der Events eingelesen werden
        }

        public static void CountCars(Event _event, DateTime DateRegisterLimit, int CarLimitRegisterLimit, DateTime DateBoPFreeze, bool IsCheckedRegisterLimit, bool IsCheckedBoPFreeze)
        {
            if (!IsCheckedRegisterLimit) { DateRegisterLimit = Event.DateTimeMaxValue; }
            if (!IsCheckedBoPFreeze) { DateBoPFreeze = Event.DateTimeMaxValue; }

            var linqList = from _entry in Entry.Statics.List
                           where _entry.SeasonID == _event.SeasonID
                           orderby EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID).CarChangeDate
                           select _entry;
            List<Entry> Entrylist = linqList.Cast<Entry>().ToList();

            List<EventsCars> _eventsCars = EventsCars.Statics.GetBy(nameof(EventsCars.EventID), _event.ID);
            foreach (EventsCars _eventCar in _eventsCars) { _eventCar.Count = 0; _eventCar.CountBoP = 0; }
            foreach (Entry _entry in Entrylist)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                if (_entry.RegisterDate < _event.EventDate && _entry.ScorePoints)
                {
                    DateTime carChangeDateMax = _event.EventDate;
                    if (DateBoPFreeze < _event.EventDate) { carChangeDateMax = DateBoPFreeze; }
                    int carID = _entry.CarID;
                    EventsEntries eventsEntries = EventsEntries.GetLatestEventsEntries(_entry, carChangeDateMax);
                    if (eventsEntries.ReadyForList) { carID = eventsEntries.CarID; }
                    DateTime carChangeDateBeforeFreeze = _entry.RegisterDate;
                    if (eventsEntries.ReadyForList) { carChangeDateBeforeFreeze = eventsEntries.CarChangeDate; }
                    EventsCars _eventCar = EventsCars.GetAnyByUniqProp(_eventsEntries.CarID, _event.ID);
                    EventsCars _eventCarAtFreeze = EventsCars.GetAnyByUniqProp(carID, _event.ID);
                    bool validCar = _eventCar.ReadyForList;
                    bool validCarAtFreeze = _eventCarAtFreeze.ReadyForList;
                    bool respectsRegLimit = _eventsEntries.CarChangeDate < DateRegisterLimit || _eventCar.Count < CarLimitRegisterLimit;
                    bool respectsRegLimitAtFreeze = carChangeDateBeforeFreeze < DateRegisterLimit || _eventCarAtFreeze.CountBoP < CarLimitRegisterLimit;
                    bool isRegistered = _entry.SignOutDate > _event.EventDate;
                    bool isRegisteredAtFreeze = _entry.RegisterDate < DateBoPFreeze && (_entry.SignOutDate > DateBoPFreeze || _entry.SignOutDate > _event.EventDate);
                    if (validCarAtFreeze && respectsRegLimitAtFreeze && isRegisteredAtFreeze) { _eventCarAtFreeze.CountBoP++; }
                    if (validCar && isRegistered)
                    {
                        if (respectsRegLimit) { _eventCarAtFreeze.Count++; _eventsEntries.ScorePoints = true; }
                        else { _eventsEntries.ScorePoints = false; }
                    }
                }
            }
            EventsCars.Statics.WriteSQL();
            EventsEntries.Statics.WriteSQL();
        }

        public static (List<Entry>, List<Entry>) DetermineEntrylist(Event _event, int SlotsAvailable, DateTime DateRegisterLimit)
        {
            List<Entry> EntriesSignedIn = new();
            List<Entry> EntriesSignedOut = new();
            List<Entry> EntriesSortPriority = new();
            List<Entry> EntriesSortSignInDate = new();
            PreQualiResultLine.Statics.LoadSQL();
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), _event.SeasonID);
            foreach (Entry _entry in listEntries)
            {
                if (_entry.RegisterDate < _event.EventDate && _entry.SignOutDate > _event.EventDate) { EntriesSortPriority.Add(_entry); }
            }
            var linqList = from _entry in EntriesSortPriority
                           orderby _entry.Priority
                           select _entry;
            EntriesSortPriority = linqList.Cast<Entry>().ToList();
            linqList = from _entry in EntriesSortPriority
                       orderby EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID).SignInDate
                       select _entry;
            EntriesSortSignInDate = linqList.Cast<Entry>().ToList();

            foreach (Entry _entry in EntriesSortPriority)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                bool candidate = !EntriesSignedIn.Contains(_entry) && EntriesSignedIn.Count < SlotsAvailable && _eventsEntries.SignInState;
                bool regBeforePreQ = _entry.RegisterDate < DateRegisterLimit;
                if (candidate && _eventsEntries.ScorePoints && regBeforePreQ) { EntriesSignedIn.Add(_entry); _eventsEntries.IsOnEntrylist = true; }
            }
            foreach (Entry _entry in EntriesSortPriority)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                bool candidate = !EntriesSignedIn.Contains(_entry) && EntriesSignedIn.Count < SlotsAvailable && _eventsEntries.SignInState;
                if (candidate && _eventsEntries.ScorePoints) { EntriesSignedIn.Add(_entry); _eventsEntries.IsOnEntrylist = true; }
            }
            foreach (Entry _entry in EntriesSortSignInDate)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                bool candidate = !EntriesSignedIn.Contains(_entry) && EntriesSignedIn.Count < SlotsAvailable && _eventsEntries.SignInState;
                if (candidate) { EntriesSignedIn.Add(_entry); _eventsEntries.IsOnEntrylist = true; }
            }
            foreach (Entry _entry in EntriesSortPriority)
            {
                if (!EntriesSignedIn.Contains(_entry) && !EntriesSignedOut.Contains(_entry)) { EntriesSignedOut.Add(_entry); }
            }
            foreach (Entry _entry in listEntries)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                if (!EntriesSignedIn.Contains(_entry)) { _eventsEntries.IsOnEntrylist = false; }
            }
            EventsEntries.Statics.WriteSQL();
            return (EntriesSignedIn, EntriesSignedOut);
        }

        public static (List<Entry>, List<Entry>) FillUpEntrylist(Event _event, int SlotsAvailable, List<Entry> EntriesSignedIn, List<Entry> EntriesSignedOut)
        {
            List<Entry> Entrylist = new();
            foreach (Entry _entry in Entry.Statics.List)
            {
                if (_entry.SeasonID == _event.SeasonID && _entry.RegisterDate < _event.EventDate && _entry.SignOutDate > _event.EventDate)
                {
                    Entrylist.Add(_entry);
                }
            }
            var linqList = from _entry in Entrylist
                           orderby _entry.Priority
                           select _entry;
            Entrylist = linqList.Cast<Entry>().ToList();
            foreach (Entry _entry in Entrylist)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                bool candidate = !EntriesSignedIn.Contains(_entry) && EntriesSignedIn.Count < SlotsAvailable;
                if (candidate) { EntriesSignedIn.Add(_entry); _eventsEntries.IsOnEntrylist = true; if (EntriesSignedOut.Contains(_entry)) { EntriesSignedOut.Remove(_entry); } }
            }
            EventsEntries.Statics.WriteSQL();
            return (EntriesSignedIn, EntriesSignedOut);
        }

        public static void CalcBoP(Event _event, int CarLimitBallast, int CarLimitRestriktor, int GainBallast, int GainRestriktor, bool IsCheckedBallast, bool IsCheckedRestriktor)
        {
            if (!IsCheckedBallast) { GainBallast = 0; }
            if (!IsCheckedRestriktor) { GainRestriktor = 0; }
            List<EventsCars> listEventsCars = EventsCars.GetAnyBy(nameof(EventsCars.EventID), _event.ID);
            foreach (EventsCars _eventCar in listEventsCars)
            {
                _eventCar.Ballast = Math.Max(0, _eventCar.CountBoP - CarLimitBallast) * GainBallast;
                _eventCar.Restrictor = Math.Max(0, _eventCar.CountBoP - CarLimitRestriktor) * GainRestriktor;
            }
            EventsCars.Statics.WriteSQL();
        }

        public static void UpdateName3Digits(int _seasonID)
        {
            bool allUnique = false; int number = 0;
            List<DriversEntries> driverEntryList = new();
            List<Entry> entryList = Entry.Statics.GetBy(nameof(Entry.SeasonID), _seasonID);
            foreach (Entry _entry in entryList)
            {
                List<DriversEntries> tempDriverEntryList = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _entry.ID);
                foreach (DriversEntries _tempDriverEntry in tempDriverEntryList) { driverEntryList.Add(_tempDriverEntry); }
            }
            foreach (DriversEntries _driverEntry in driverEntryList)
            {
                _driverEntry.Name3Digits = Driver.Statics.GetByID(_driverEntry.DriverID).Name3DigitsOptions[0];
            }
            while (!allUnique)
            {
                allUnique = true;
                foreach (DriversEntries _driverEntry in driverEntryList)
                {
                    List<DriversEntries> identicalN3D = new();
                    string currentN3D = _driverEntry.Name3Digits;
                    foreach (DriversEntries _driverEntry2 in driverEntryList)
                    {
                        if (currentN3D == _driverEntry2.Name3Digits) { identicalN3D.Add(_driverEntry2); }
                    }
                    if (identicalN3D.Count > 1)
                    {
                        int lvlsMax = -1;
                        List<DriversEntries> identicalN3D_0 = new();
                        List<DriversEntries> identicalN3D_1 = new();
                        foreach (DriversEntries _driverEntry2 in identicalN3D)
                        {
                            Driver _driver2 = Driver.Statics.GetByID(_driverEntry2.DriverID);
                            if (_driver2.Name3DigitsOptions.IndexOf(currentN3D) == 0) { identicalN3D_0.Add(_driverEntry2); }
                            int lvlsMaxTemp = _driver2.Name3DigitsOptions.Count - _driver2.Name3DigitsOptions.IndexOf(currentN3D);
                            if (lvlsMaxTemp > lvlsMax) { lvlsMax = lvlsMaxTemp; identicalN3D_1 = new List<DriversEntries>() { _driverEntry2 }; }
                        }
                        if (identicalN3D_0.Count > 0) { identicalN3D = identicalN3D_0; } else { identicalN3D = identicalN3D_1; }
                        foreach (DriversEntries _driverEntry2 in identicalN3D)
                        {
                            Driver _driver2 = Driver.Statics.GetByID(_driverEntry2.DriverID);
                            int currentLvl = _driver2.Name3DigitsOptions.IndexOf(currentN3D) + 1;
                            int lvlMax = _driver2.Name3DigitsOptions.Count;
                            if (currentLvl == lvlMax) { _driverEntry2.Name3Digits = number.ToString(); number++; }
                            else { _driverEntry2.Name3Digits = _driver2.Name3DigitsOptions[currentLvl]; }
                        }
                        allUnique = false;
                        break;
                    }
                }
            }
            foreach (DriversEntries _driverEntry in driverEntryList)
            {
                if (Int32.TryParse(_driverEntry.Name3Digits, out number))
                {
                    _driverEntry.Name3Digits = Driver.Statics.GetByID(_driverEntry.DriverID).Name3DigitsOptions[0];
                }
            }
            DriversEntries.Statics.WriteSQL();
        }

        public static void UpdateEntryPriority(int _seasonID)
        {
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), _seasonID);
            var linqList = from _entry in listEntries
                           orderby _entry.ScorePoints descending, _entry.RegisterDate
                           select _entry;
            listEntries = linqList.Cast<Entry>().ToList();
            for (int entryNr = 0; entryNr < listEntries.Count; entryNr++) { listEntries[entryNr].Priority = entryNr + 1; }
            Entry.Statics.WriteSQL();
        }
    }
}