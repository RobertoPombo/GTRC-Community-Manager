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
using GTRC_Community_Manager;
using Enums;
using System.Runtime.ConstrainedExecution;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Scripts
{
    public static class PreSeason
    {
        public static void UpdateLeaderboard(ServerM _server)
        {
            int serverID = _server.ServerID;
            int seasonID = _server.Server.SeasonID;
            if (_server.ServerTypeEnum == ServerTypeEnum.Practice || _server.ServerTypeEnum == ServerTypeEnum.PreQuali)
            {
                List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), seasonID));
                for (int eventNr = 0; eventNr < listEvents.Count; eventNr++)
                {
                    Event _event = listEvents[eventNr];
                    int eventID = _event.ID;
                    List<string> propNames = new() { nameof(LeaderboardLinePractice.ServerID), nameof(LeaderboardLinePractice.EventID) };
                    List<dynamic> propValues = new() { serverID, eventID };
                    List<LeaderboardLinePractice> listLBL = LeaderboardLinePractice.Statics.GetBy(propNames, propValues);
                    for (int lblNr = listLBL.Count - 1; lblNr >= 0; lblNr--) { listLBL[lblNr].DeleteSQL(); }
                    propNames = new() { nameof(ResultsFile.ServerID), nameof(ResultsFile.TrackID), nameof(ResultsFile.SeasonID), nameof(ResultsFile.ServerType) };
                    propValues = new() { serverID, _event.TrackID, seasonID, _server.Server.ServerType };
                    List<ResultsFile> listResultsFiles = ResultsFile.Statics.GetBy(propNames, propValues);
                    foreach (ResultsFile _resultsFile in listResultsFiles)
                    {
                        DateTime previousEventDate = Basics.DateTimeMinValue;
                        if (eventNr > 0) { previousEventDate = listEvents[eventNr - 1].Date; }
                        if (_resultsFile.Date > previousEventDate && _resultsFile.Date < _event.Date)
                        {
                            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), seasonID);
                            foreach (Entry _entry in listEntries)
                            {
                                int entryID = _entry.ID;
                                List<DriversEntries> listDriversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), entryID);
                                foreach (DriversEntries _driverEntry in listDriversEntries)
                                {
                                    int driverID = _driverEntry.DriverID;
                                    long steamID = _driverEntry.ObjDriver.SteamID;
                                    foreach(Car _car in Car.Statics.List)
                                    {
                                        int carID = _car.ID;
                                        propNames = new() { nameof(Lap.ResultsFileID), nameof(Lap.SteamID), nameof(Lap.AccCarID) };
                                        propValues = new() { _resultsFile.ID, steamID, _car.AccCarID };
                                        List<Lap> listLaps = Lap.Statics.GetBy(propNames, propValues);
                                        if (listLaps.Count > 0)
                                        {
                                            List<dynamic> uniqProps = new() { serverID, eventID, entryID, driverID };
                                            LeaderboardLinePractice newLBL = LeaderboardLinePractice.Statics.GetByUniqProp(uniqProps);
                                            if (!newLBL.ReadyForList)
                                            {
                                                newLBL = new() { ServerID = serverID, EventID = eventID, EntryID = entryID, DriverID = driverID, CarID = carID };
                                            }
                                            foreach (Lap _lap in listLaps)
                                            {
                                                if (_lap.IsValid)
                                                {
                                                    newLBL.BestLap = Math.Min(_lap.Time, newLBL.BestLap);
                                                    newLBL.BestSector1 = Math.Min(_lap.Sector1, newLBL.BestSector1);
                                                    newLBL.BestSector2 = Math.Min(_lap.Sector2, newLBL.BestSector2);
                                                    newLBL.BestSector3 = Math.Min(_lap.Sector3, newLBL.BestSector3);
                                                    newLBL.ValidLapsCount += 1;
                                                }
                                                newLBL.LapsCount += 1;
                                            }
                                            _ = LeaderboardLinePractice.Statics.WriteSQL(newLBL);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ResetPreQResults(int seasonID)
        {
            PreQualiResultLine.Statics.ResetSQL(true);
            PreQualiResultLine.Statics.LoadSQL();
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), seasonID);
            List<Event> listEvents = Event.SortByDate(Event.Statics.GetBy(nameof(Event.SeasonID), seasonID));
            foreach (Entry _entry in listEntries) { if (listEvents.Count > 0 && _entry.SignOutDate > listEvents[0].Date) { _ = new PreQualiResultLine { EntryID = _entry.ID }; } }
            PreQualiResultLine.Statics.WriteJson();
            PreQualiResultLine.Statics.WriteSQL();
        }

        public static void UpdatePreQResults(int seasonID)
        {
            //Sollte dynamisch sein:
            double timeFactorMax = 1.07;
            int lapsCountStintMin = 10;
            List<int> listTracks = new() { 8, 9 };

            ResetPreQResults(seasonID);

            List<string> propNames = new() { nameof(ResultsFile.TrackID), nameof(ResultsFile.SeasonID), nameof(ResultsFile.ServerType) };
            List<dynamic> propValues = new() { Basics.NoID, seasonID, ServerTypeEnum.PreQuali };
            List<List<ResultsFile>> listResultsFiles = new ();
            foreach (int _track in listTracks) { propValues[0] = _track; listResultsFiles.Add(ResultsFile.Statics.GetBy(propNames, propValues)); }

            foreach (PreQualiResultLine preQualiResultLine in PreQualiResultLine.Statics.List)
            {
                List <DriversEntries> _driversEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), preQualiResultLine.EntryID);
                List<long> listSteamIDs = new();
                foreach (DriversEntries _driverEntry in _driversEntries) { if (_driverEntry.ObjDriver.ReadyForList) { listSteamIDs.Add(_driverEntry.ObjDriver.SteamID); } }
                for (int trackNr = 0; trackNr < listTracks.Count; trackNr++)
                {
                    List<Lap> lapsEntryTrack = new();
                    foreach (ResultsFile preQualiResultFile in listResultsFiles[trackNr])
                    {
                        foreach (long _steamID in listSteamIDs)
                        {
                            propNames = new() { nameof(Lap.ResultsFileID), nameof(Lap.SteamID) };
                            propValues = new() { preQualiResultFile.ID, _steamID };
                            List<Lap> lapsEntryTrackResultsFile = Lap.Statics.GetBy(propNames, propValues);
                            foreach (Lap lap in lapsEntryTrackResultsFile)
                            {
                                lapsEntryTrack.Add(lap);
                            }
                        }
                    }
                    foreach (Lap lap in lapsEntryTrack)
                    {
                        preQualiResultLine.LapsCount++;
                        if (lap.IsValid) { preQualiResultLine.ValidLapsCount++; }
                        switch (trackNr)
                        {
                            case 0:
                                preQualiResultLine.LapsCount1++;
                                if (lap.IsValid) { preQualiResultLine.ValidLapsCount1++; }
                                break;
                            case 1:
                                preQualiResultLine.LapsCount2++;
                                if (lap.IsValid) { preQualiResultLine.ValidLapsCount2++; }
                                break;
                        }
                        if (lap.IsValid)
                        {
                            switch (trackNr)
                            {
                                case 0:
                                    preQualiResultLine.BestLap1 = Math.Min(preQualiResultLine.BestLap1, lap.Time);
                                    preQualiResultLine.BestSector1a = Math.Min(preQualiResultLine.BestSector1a, lap.Sector1);
                                    preQualiResultLine.BestSector3a = Math.Min(preQualiResultLine.BestSector3a, lap.Sector3);
                                    break;
                                case 1:
                                    preQualiResultLine.BestLap2 = Math.Min(preQualiResultLine.BestLap2, lap.Time);
                                    preQualiResultLine.BestSector1b = Math.Min(preQualiResultLine.BestSector1b, lap.Sector1);
                                    preQualiResultLine.BestSector3b = Math.Min(preQualiResultLine.BestSector3b, lap.Sector3);
                                    break;
                            }
                        }
                    }
                    int time107 = int.MaxValue;
                    int s1s3_107 = int.MaxValue;
                    switch (trackNr)
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
                    foreach (Lap lap in lapsEntryTrack)
                    {
                        if (lap.Time > time107 && lap.Sector1 + lap.Sector3 > s1s3_107) { lapsEntryTrackStint = new List<Lap>(); newStint = true; }
                        else if (lap.IsValid) { lapsEntryTrackStint.Add(lap); }
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
                                switch (trackNr)
                                {
                                    case 0: preQualiResultLine.ValidStintsCount1++; break;
                                    case 1: preQualiResultLine.ValidStintsCount2++; break;
                                }
                            }
                            double totalTime = 0;
                            for (int lapNr = 0; lapNr < lapsCountStintMin; lapNr++) { totalTime += bestTimes[lapNr]; }
                            int newAverage = (int)Math.Round(totalTime / lapsCountStintMin, 0);
                            switch (trackNr)
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
            for (int lineNr = 0; lineNr < PreQualiResultLine.Statics.List.Count; lineNr++) { PreQualiResultLine.Statics.List[lineNr].Position = lineNr + 1; }
            PreQualiResultLine.Statics.WriteJson();
            PreQualiResultLine.Statics.WriteSQL();
            linqList = from _resultsLine in PreQualiResultLine.Statics.List
                           orderby _resultsLine.Position
                           select _resultsLine;
            PreQualiResultLine.Statics.List = linqList.Cast<PreQualiResultLine>().ToList();
        }

        public static void SetEntry_NotScorePoints_NotPermanent(Event nextEvent)
        {
            string delimiter = "#!#";
            List<Entry> listEntries = Entry.Statics.GetBy(nameof(Entry.SeasonID), nextEvent.SeasonID);
            foreach (Entry _entry in listEntries)
            {
                int signOutCount = _entry.CountSignOut(nextEvent);
                int noShowCount = _entry.CountNoShow(nextEvent, false);
                signOutCount += noShowCount;
                if (_entry.SignOutDate > DateTime.Now)
                {
                    EntriesDatetimes _entryDate = _entry.GetEntriesDatetimesByDate(nextEvent.Date);
                    if (noShowCount > nextEvent.ObjSeason.NoShowLimit)
                    {
                        if (_entryDate.ScorePoints || _entryDate.Permanent)
                        {
                            string message = delimiter + "Da du das Limit von " + nextEvent.ObjSeason.NoShowLimit.ToString() +
                                " Nichtteilnahmen trotz Anmeldung pro Saison überschritten hast, ";
                            if (_entryDate.ScorePoints && _entryDate.Permanent)
                            {
                                message += "sammelst du ab jetzt keine Meisterschaftspunkte mehr und musst dich zu jedem Event einzeln anmelden." +
                                    " Startplätze werden bevorzugt an Teilnehmer vergeben, die nicht außerhalb der Wertung fahren" +
                                    " und für jedes Rennen automatisch angemeldet sind.";
                            }
                            else if (_entryDate.ScorePoints)
                            {
                                message += "sammelst du ab jetzt keine Meisterschaftspunkte mehr." +
                                    " Startplätze werden bevorzugt an Teilnehmer vergeben, die nicht außerhalb der Wertung fahren.";
                            }
                            else
                            {
                                message += "musst du dich ab jetzt zu jedem Event einzeln anmelden." +
                                    " Startplätze werden bevorzugt an Teilnehmer vergeben, die für jedes Rennen automatisch angemeldet sind.";
                            }
                            _ = Commands.NotifyEntry(_entry, message, delimiter);
                            EntriesDatetimes newEntryDate = EntriesDatetimes.GetAnyByUniqProp(_entry.ID, DateTime.Now);
                            newEntryDate.ScorePoints = false;
                            newEntryDate.Permanent = false;
                            _ = EntriesDatetimes.Statics.WriteSQL(newEntryDate);
                        }
                    }
                    else if (signOutCount > nextEvent.ObjSeason.SignOutLimit)
                    {
                        if (_entryDate.Permanent)
                        {
                            string message = delimiter + "Da du das Limit von " + nextEvent.ObjSeason.SignOutLimit.ToString() +
                                " Abmeldungen/Nichtteilnahmen pro Saison überschritten hast, musst du dich ab jetzt zu jedem Event einzeln anmelden." +
                                " Startplätze werden bevorzugt an Teilnehmer vergeben, die für jedes Rennen automatisch angemeldet sind.";
                            _ = Commands.NotifyEntry(_entry, message, delimiter);
                            EntriesDatetimes newEntryDate = EntriesDatetimes.GetAnyByUniqProp(_entry.ID, DateTime.Now);
                            newEntryDate.Permanent = false;
                        }
                    }
                }
            }
        }

        public static void CountCars(Event _event)
        {
            string delimiter = "#!#";
            Season _season = _event.ObjSeason;
            var linqList = from _entry in Entry.Statics.List
                           where _entry.SeasonID == _event.SeasonID
                           orderby _entry.GetLatestCarChangeDate(_event.Date)
                           select _entry;
            List<Entry> Entrylist = linqList.Cast<Entry>().ToList();

            List<EventsCars> _eventsCars = EventsCars.Statics.GetBy(nameof(EventsCars.EventID), _event.ID);
            foreach (EventsCars _eventCar in _eventsCars) { _eventCar.Count = 0; _eventCar.CountBoP = 0; }
            foreach (Entry _entry in Entrylist)
            {
                EventsEntries _eventsEntries = EventsEntries.GetAnyByUniqProp(_entry.ID, _event.ID);
                if (_entry.RegisterDate < _event.Date && _entry.ScorePoints)
                {
                    DateTime carChangeDateMax = _event.Date;
                    if (_season.DateBoPFreeze < _event.Date) { carChangeDateMax = _season.DateBoPFreeze; }
                    int carID = _entry.CarID;
                    int carIDBeforeFreze = _entry.CarID;
                    EntriesDatetimes entryDatetime = _entry.GetEntriesDatetimesByDate(_event.Date);
                    EntriesDatetimes entryDatetimeBeforeFreze = _entry.GetEntriesDatetimesByDate(carChangeDateMax);
                    if (entryDatetime.ReadyForList) { carID = entryDatetime.CarID; }
                    if (entryDatetimeBeforeFreze.ReadyForList) { carIDBeforeFreze = entryDatetimeBeforeFreze.CarID; }
                    DateTime carChangeDate = _entry.RegisterDate;
                    DateTime carChangeDateBeforeFreze = _entry.RegisterDate;
                    carChangeDate = _entry.GetLatestCarChangeDate(_event.Date);
                    carChangeDateBeforeFreze = _entry.GetLatestCarChangeDate(carChangeDateMax);
                    EventsCars _eventCar = EventsCars.GetAnyByUniqProp(carID, _event.ID);
                    EventsCars _eventCarAtFreeze = EventsCars.GetAnyByUniqProp(carIDBeforeFreze, _event.ID);
                    int carCount = _eventCar.Count;
                    int carCountBoP = _eventCarAtFreeze.CountBoP;
                    if (_season.GroupCarLimits)
                    {
                        carCount = 0;
                        carCountBoP = 0;
                        foreach (EventsCars _eventCar2 in _eventsCars)
                        {
                            Car _car2 = _eventCar2.ObjCar;
                            if (_eventCar.ObjCar.Manufacturer == _car2.Manufacturer && _eventCar.ObjCar.Category == _car2.Category)
                            {
                                carCount += _eventCar2.CountBoP;
                            }
                            if (_eventCarAtFreeze.ObjCar.Manufacturer == _car2.Manufacturer && _eventCarAtFreeze.ObjCar.Category == _car2.Category)
                            {
                                carCountBoP += _eventCar2.CountBoP;
                            }
                        }
                    }
                    bool validCar = _eventCar.ReadyForList;
                    bool validCarAtFreeze = _eventCarAtFreeze.ReadyForList;
                    bool respectsRegLimit = carChangeDate < _season.DateRegisterLimit || carCount < _season.CarLimitRegisterLimit;
                    bool respectsRegLimitAtFreeze0 = carChangeDateBeforeFreze < _season.DateRegisterLimit;
                    bool respectsRegLimitAtFreeze = respectsRegLimitAtFreeze0 || carCountBoP < _season.CarLimitRegisterLimit;
                    bool isRegistered = _entry.SignOutDate > _event.Date;
                    bool isRegisteredAtFreeze0 = _entry.RegisterDate < _season.DateBoPFreeze;
                    bool isRegisteredAtFreeze = isRegisteredAtFreeze0 && (_entry.SignOutDate > _season.DateBoPFreeze || _entry.SignOutDate > _event.Date);
                    if (validCarAtFreeze && respectsRegLimitAtFreeze && isRegisteredAtFreeze) { _eventCarAtFreeze.CountBoP++; }
                    if (validCar && isRegistered)
                    {
                        if (respectsRegLimit)
                        {
                            _eventCarAtFreeze.Count++;
                            if (!_eventsEntries.ScorePoints)
                            {
                                string message = delimiter + "Du fährst ab jetzt nicht mehr außerhalb der Wertung mit und sammelst Meisterschaftspunkte.";
                                _ = Commands.NotifyEntry(_entry, message, delimiter);
                                _eventsEntries.ScorePoints = true;
                            }
                        }
                        else
                        {
                            if (_eventsEntries.ScorePoints)
                            {
                                string message = delimiter + CreateNoScorePointsNotification(_season, _event, _eventCar.ObjCar);
                                _ = Commands.NotifyEntry(_entry, message, delimiter);
                                _eventsEntries.ScorePoints = false;
                            }
                        }
                    }
                }
            }
            EventsCars.Statics.WriteSQL();
            EventsEntries.Statics.WriteSQL();
        }

        public static string CreateNoScorePointsNotification(Season _season, Event _event, Car _car)
        {
            string message = "Aufgrund der Obergrenze von " + _season.CarLimitRegisterLimit.ToString() +
                " Fahrzeugen fährst du das kommende Rennen mit dem " + _car.Name +
                " außerhalb der Wertung mit und sammelst keine Meisterschaftspunkte.";
            if (_season.DateBoPFreeze > DateTime.Now)
            {
                message += " Sobald dieses Fahrzeug wieder weniger als " + _event.ObjSeason.CarLimitRegisterLimit.ToString() +
                    "x in der Meisterschaft vertreten ist, nimmst du an der Meisterschaftswertung teil.";
            }
            if (_season.CarChangeLimit > 0)
            {
                message += " Solltest du beim kommenden Rennen gerne um Punkte fahren wollen," +
                " kannst du dir die `!fahrzeugliste` anzeigen lassen und einen `!fahrzeugwechsel` versuchen.";
            }
            return message;
        }

        public static (List<EventsEntries>, List<EventsEntries>) DetermineEntrylist(Event _event, int SlotsAvailable)
        {
            UpdateEventEntriesPriority(_event);
            List<EventsEntries> SignedIn = new();
            List<EventsEntries> SignedOut = new();
            List<EventsEntries> listSortPriority = EventsEntries.GetAnyBy(nameof(EventsEntries.EventID), _event.ID);
            var linqList = from _eventEntry in listSortPriority orderby _eventEntry.Priority select _eventEntry;
            listSortPriority = linqList.Cast<EventsEntries>().ToList();

            foreach (EventsEntries _eventEntry in listSortPriority)
            {
                if (!_eventEntry.IsBanned && _eventEntry.SignInState && SignedIn.Count < SlotsAvailable) { _eventEntry.IsOnEntrylist = true; SignedIn.Add(_eventEntry); }
                else { _eventEntry.IsOnEntrylist = false; SignedOut.Add(_eventEntry); }
            }
            EventsEntries.Statics.WriteSQL();
            return (SignedIn, SignedOut);
        }

        public static (List<EventsEntries>, List<EventsEntries>) FillUpEntrylist(int SlotsAvailable, List<EventsEntries> SignedIn, List<EventsEntries> SignedOut)
        {
            List<EventsEntries> _iterateList = new(); foreach(EventsEntries _eventEntry in SignedOut) { _iterateList.Add(_eventEntry); }
            foreach (EventsEntries _eventEntry in _iterateList)
            {
                if (!_eventEntry.IsBanned && !SignedIn.Contains(_eventEntry) && SignedIn.Count < SlotsAvailable)
                {
                    SignedOut.Remove(_eventEntry);
                    SignedIn.Add(_eventEntry);
                    _eventEntry.IsOnEntrylist = true;
                }
            }
            EventsEntries.Statics.WriteSQL();
            return (SignedIn, SignedOut);
        }

        public static void CalcBoP(Event _event)
        {
            List<EventsCars> listEventsCars = EventsCars.GetAnyBy(nameof(EventsCars.EventID), _event.ID);
            foreach (EventsCars _eventCar in listEventsCars)
            {
                int carCount = _eventCar.CountBoP;
                if (_event.ObjSeason.GroupCarLimits)
                {
                    carCount = 0;
                    foreach (EventsCars _eventCar2 in listEventsCars)
                    {
                        if (_eventCar.ObjCar.Manufacturer == _eventCar2.ObjCar.Manufacturer && _eventCar.ObjCar.Category == _eventCar2.ObjCar.Category)
                        {
                            carCount += _eventCar2.CountBoP;
                        }
                    }
                }
                if (!_event.ObjSeason.BopLatestModelOnly || _eventCar.ObjCar.IsLatestModel)
                {
                    _eventCar.Ballast = Math.Max(0, carCount - _event.ObjSeason.CarLimitBallast) * _event.ObjSeason.GainBallast;
                    _eventCar.Restrictor = Math.Max(0, carCount - _event.ObjSeason.CarLimitRestrictor) * _event.ObjSeason.GainRestrictor;
                }
                else
                {
                    _eventCar.Ballast = new Entry(false).Ballast;
                    _eventCar.Restrictor = new Entry(false).Restrictor;
                }
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
                _driverEntry.Name3Digits = _driverEntry.ObjDriver.Name3DigitsOptions[0];
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
                            Driver _driver2 = _driverEntry2.ObjDriver;
                            if (_driver2.Name3DigitsOptions.IndexOf(currentN3D) == 0) { identicalN3D_0.Add(_driverEntry2); }
                            int lvlsMaxTemp = _driver2.Name3DigitsOptions.Count - _driver2.Name3DigitsOptions.IndexOf(currentN3D);
                            if (lvlsMaxTemp > lvlsMax) { lvlsMax = lvlsMaxTemp; identicalN3D_1 = new List<DriversEntries>() { _driverEntry2 }; }
                        }
                        if (identicalN3D_0.Count > 0) { identicalN3D = identicalN3D_0; } else { identicalN3D = identicalN3D_1; }
                        foreach (DriversEntries _driverEntry2 in identicalN3D)
                        {
                            Driver _driver2 = _driverEntry2.ObjDriver;
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
                if (int.TryParse(_driverEntry.Name3Digits, out number))
                {
                    _driverEntry.Name3Digits = _driverEntry.ObjDriver.Name3DigitsOptions[0];
                }
            }
            DriversEntries.Statics.WriteSQL();
        }

        public static void UpdateEntryPriority(int _seasonID)
        {
            List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), _seasonID);
            foreach (Event _event in listEvents) { UpdateEventEntriesPriority(_event); }
        }

        public static void UpdateEventEntriesPriority(Event _event)
        {
            List<long> steamIDsFixPreQ = PreQualiResultLine.SteamIDsFixPreQ;
            List<EventsEntries> listEventsEntries = EventsEntries.GetAnyBy(nameof(EventsEntries.EventID), _event.ID);
            for (int index1 = 0; index1 < listEventsEntries.Count - 1; index1++)
            {
                for (int index2 = index1 + 1; index2 < listEventsEntries.Count; index2++)
                {
                    EventsEntries _eventEntry1 = listEventsEntries[index1];
                    EventsEntries _eventEntry2 = listEventsEntries[index2];
                    List<DriversEntries> _driversEntries1 = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _eventEntry1.EntryID);
                    List<DriversEntries> _driversEntries2 = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _eventEntry2.EntryID);
                    int posPreQ1 = PreQualiResultLine.Statics.GetByUniqProp(_eventEntry1.EntryID).Position;
                    int posPreQ2 = PreQualiResultLine.Statics.GetByUniqProp(_eventEntry2.EntryID).Position;
                    int fixPosPreQ1 = steamIDsFixPreQ.Count;
                    int fixPosPreQ2 = steamIDsFixPreQ.Count;
                    for (int fixPosPreQ = 0; fixPosPreQ < steamIDsFixPreQ.Count; fixPosPreQ++)
                    {
                        foreach (DriversEntries _driverEntry in _driversEntries1)
                        {
                            if (_driverEntry.ReadyForList && _driverEntry.ObjDriver.SteamID == steamIDsFixPreQ[fixPosPreQ])
                            {
                                fixPosPreQ1 = Math.Min(fixPosPreQ1, fixPosPreQ);
                            }
                        }
                        foreach (DriversEntries _driverEntry in _driversEntries2)
                        {
                            if (_driverEntry.ReadyForList && _driverEntry.ObjDriver.SteamID == steamIDsFixPreQ[fixPosPreQ])
                            {
                                fixPosPreQ2 = Math.Min(fixPosPreQ2, fixPosPreQ);
                            }
                        }
                    }
                    if (_eventEntry1.RegisterState == _eventEntry2.RegisterState)
                    {
                        if (_eventEntry1.IsBanned == _eventEntry2.IsBanned)
                        {
                            if (_eventEntry1.ScorePoints == _eventEntry2.ScorePoints)
                            {
                                if (_eventEntry1.ObjEntry.Permanent == _eventEntry2.ObjEntry.Permanent)
                                {
                                    if (_eventEntry1.SignInDate == _eventEntry2.SignInDate || _eventEntry1.ObjEntry.Permanent)
                                    {
                                        if (fixPosPreQ1 == fixPosPreQ2)
                                        {
                                            if (posPreQ1 == posPreQ2)
                                            {
                                                if (_eventEntry1.ObjEntry.RegisterDate == _eventEntry2.ObjEntry.RegisterDate)
                                                {
                                                    if (_eventEntry1.ObjEntry.RaceNumber > _eventEntry2.ObjEntry.RaceNumber)
                                                    {
                                                        (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                                                    }
                                                }
                                                else if (_eventEntry1.ObjEntry.RegisterDate > _eventEntry2.ObjEntry.RegisterDate)
                                                {
                                                    (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                                                }
                                            }
                                            else if (posPreQ1 > posPreQ2)
                                            {
                                                (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                                            }
                                        }
                                        else if (fixPosPreQ1 > fixPosPreQ2)
                                        {
                                            (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                                        }
                                    }
                                    else if (_eventEntry1.SignInDate > _eventEntry2.SignInDate && !_eventEntry1.ObjEntry.Permanent)
                                    {
                                        (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                                    }
                                }
                                else if (_eventEntry2.ObjEntry.Permanent)
                                {
                                    (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                                }
                            }
                            else if (_eventEntry2.ScorePoints)
                            {
                                (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                            }
                        }
                        else if (_eventEntry1.IsBanned)
                        {
                            (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                        }
                    }
                    else if (_eventEntry2.RegisterState)
                    {
                        (listEventsEntries[index1], listEventsEntries[index2]) = (listEventsEntries[index2], listEventsEntries[index1]);
                    }
                }
            }
            for (int index = 0; index < listEventsEntries.Count; index++) { listEventsEntries[index].Priority = index + 1; }
        }
    }
}