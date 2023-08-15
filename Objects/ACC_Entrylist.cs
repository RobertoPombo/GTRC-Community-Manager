using Newtonsoft.Json;
using Database;
using Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GTRC_Community_Manager
{
    public class ACC_Driver
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string shortName { get; set; }
        public int driverCategory { get; set; }
        public string playerID { get; set; }
    }

    public class ACC_Entry
    {
        public List<ACC_Driver> drivers { get; set; }
        public int raceNumber { get; set; }
        public int forcedCarModel { get; set; }
        public int overrideDriverInfo { get; set; }
        public int defaultGridPosition { get; set; }
        public int ballastKg { get; set; }
        public int restrictor { get; set; }
        public int isServerAdmin { get; set; }
    }

    public class ACC_Entrylist
    {
        public List<ACC_Entry> entries { get; set; }
        public int forceEntryList { get; set; }

        public void WriteJson(string Path)
        {
            string relativePath = Basics.ValidatedPath(MainWindow.currentDirectory, Path);
            string absolutePath = Basics.RelativePath2AbsolutePath(MainWindow.currentDirectory, relativePath);
            if (!Directory.Exists(absolutePath)) { Directory.CreateDirectory(absolutePath); }
            absolutePath += "entrylist.json";
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(absolutePath, text, Encoding.Unicode);
        }

        public void Create(bool _forceCarModel, bool _forceEntrylist, Event _event)
        {
            entries = new List<ACC_Entry>();
            List<EventsEntries> ListEntries = EventsEntries.Statics.GetBy(nameof(EventsEntries.EventID), _event.ID);
            for (int i = 0; i < ListEntries.Count - 1; i++)
            {
                for (int i2 = i; i2 < ListEntries.Count; i2++)
                {
                    if (ListEntries[i].ObjEntry.RaceNumber > ListEntries[i2].ObjEntry.RaceNumber)
                    {
                        (ListEntries[i], ListEntries[i2]) = (ListEntries[i2], ListEntries[i]);
                    }
                }
            }
            foreach (EventsEntries _eventsEntries in ListEntries)
            {
                if (_eventsEntries.IsOnEntrylist)
                {
                    bool isAdmin = false;
                    ACC_Entry accEntry = new();
                    entries.Add(accEntry);
                    EntriesDatetimes entryDatetime = _eventsEntries.ObjEntry.GetEntriesDatetimesByDate(_event.Date);
                    accEntry.drivers = new List<ACC_Driver>();
                    List<DriversEntries> ListDriverEntries = DriversEntries.Statics.GetBy(nameof(DriversEntries.EntryID), _eventsEntries.ObjEntry.ID);
                    foreach (DriversEntries _driverEntries in ListDriverEntries)
                    {
                        if (!_driverEntries.ObjDriver.IsBanned(_event))
                        {
                            ACC_Driver accDriver = new();
                            accEntry.drivers.Add(accDriver);
                            if (RaceControl.Statics.ExistsUniqProp(_driverEntries.ObjDriver.ID)) { isAdmin = true; }
                            accDriver.firstName = _driverEntries.ObjDriver.FirstName;
                            accDriver.lastName = _driverEntries.ObjDriver.LastName;
                            accDriver.shortName = _driverEntries.Name3Digits;
                            accDriver.driverCategory = _eventsEntries.Category;
                            accDriver.playerID = "S" + _driverEntries.ObjDriver.SteamID.ToString();
                        }
                    }
                    accEntry.raceNumber = _eventsEntries.ObjEntry.RaceNumber;
                    if (_forceCarModel && entryDatetime.ObjCar.ID != Basics.NoID) { accEntry.forcedCarModel = entryDatetime.ObjCar.AccCarID; }
                    else { accEntry.forcedCarModel = -1; }
                    accEntry.overrideDriverInfo = 1;
                    accEntry.defaultGridPosition = -1;
                    accEntry.ballastKg = _eventsEntries.Ballast;
                    accEntry.restrictor = _eventsEntries.Restrictor;
                    if (isAdmin) { accEntry.isServerAdmin = 1; } else { accEntry.isServerAdmin = 0; }
                }
            }
            int adminNr = 0;
            int raceNumber = 1;
            foreach (RaceControl admin in RaceControl.Statics.List)
            {
                DriversEntries _driverEntriesAdmin = DriversEntries.GetByDriverIDSeasonID(admin.ObjDriver.ID, _event.SeasonID);
                EventsEntries _eventsEntriesAdmin = EventsEntries.GetAnyByUniqProp(_driverEntriesAdmin.EntryID, _event.ID);
                if (!admin.ObjDriver.IsBanned(_event) && !_eventsEntriesAdmin.IsOnEntrylist)
                {
                    adminNr++;
                    raceNumber++;
                    bool usedRaceNumber = true;
                    while (usedRaceNumber)
                    {
                        usedRaceNumber = false;
                        foreach (EventsEntries _eventsEntries in ListEntries)
                        {
                            if (_eventsEntries.IsOnEntrylist && _eventsEntries.ObjEntry.RaceNumber == raceNumber) { raceNumber++; usedRaceNumber = true; break; }
                        }
                    }
                    ACC_Entry accEntry = new();
                    entries.Add(accEntry);
                    accEntry.drivers = new List<ACC_Driver>();
                    ACC_Driver accDriver = new();
                    accEntry.drivers.Add(accDriver);
                    accDriver.firstName = admin.FirstName;
                    accDriver.lastName = admin.LastName;
                    accDriver.shortName = "SC" + adminNr.ToString();
                    accDriver.driverCategory = new Entry(false).Category;
                    accDriver.playerID = "S" + admin.ObjDriver.SteamID.ToString();
                    accEntry.raceNumber = raceNumber;
                    accEntry.forcedCarModel = -1;
                    accEntry.overrideDriverInfo = 1;
                    accEntry.defaultGridPosition = -1;
                    accEntry.ballastKg = 0;
                    accEntry.restrictor = 0;
                    accEntry.isServerAdmin = 1;
                }
            }
            if (_forceEntrylist) { forceEntryList = 1; } else { forceEntryList = 0; }
        }

        public void CreateRaceControl(bool _forceEntrylist, Event _event)
        {
            entries = new List<ACC_Entry>();
            int adminNr = 0;
            foreach (RaceControl admin in RaceControl.Statics.List)
            {
                if (!admin.ObjDriver.IsBanned(_event))
                {
                    adminNr++;
                    ACC_Entry accEntry = new();
                    entries.Add(accEntry);
                    accEntry.drivers = new List<ACC_Driver>();
                    ACC_Driver accDriver = new();
                    accEntry.drivers.Add(accDriver);
                    accDriver.firstName = admin.FirstName;
                    accDriver.lastName = admin.LastName;
                    accDriver.shortName = "SC" + adminNr.ToString();
                    accDriver.driverCategory = new Entry(false).Category;
                    accDriver.playerID = "S" + admin.ObjDriver.SteamID.ToString();
                    accEntry.raceNumber = -1;
                    accEntry.forcedCarModel = -1;
                    accEntry.overrideDriverInfo = 1;
                    accEntry.defaultGridPosition = -1;
                    accEntry.ballastKg = 0;
                    accEntry.restrictor = 0;
                    accEntry.isServerAdmin = 1;
                }
            }
            if (_forceEntrylist) { forceEntryList = 1; } else { forceEntryList = 0; }
        }

        public void CreateDrivers(bool _forceEntrylist, Event _event)
        {
            entries = new List<ACC_Entry>();
            foreach (Driver driver in Driver.Statics.List)
            {
                if (!driver.IsBanned(_event))
                {
                    ACC_Entry accEntry = new();
                    entries.Add(accEntry);
                    accEntry.drivers = new List<ACC_Driver>();
                    ACC_Driver accDriver = new();
                    accEntry.drivers.Add(accDriver);
                    accDriver.firstName = driver.FirstName;
                    accDriver.lastName = driver.LastName;
                    accDriver.shortName = driver.Name3Digits;
                    accDriver.driverCategory = new Entry(false).Category;
                    accDriver.playerID = "S" + driver.SteamID.ToString();
                    accEntry.raceNumber = -1;
                    accEntry.forcedCarModel = -1;
                    accEntry.overrideDriverInfo = 1;
                    accEntry.defaultGridPosition = -1;
                    accEntry.ballastKg = 0;
                    accEntry.restrictor = 0;
                    if (RaceControl.Statics.ExistsUniqProp(driver.ID)) { accEntry.isServerAdmin = 1; } else { accEntry.isServerAdmin = 0; }
                }
            }
            if (_forceEntrylist) { forceEntryList = 1; } else { forceEntryList = 0; }
        }

        public void CreateEmpty()
        {
            entries = new List<ACC_Entry>();
        }
    }
}
