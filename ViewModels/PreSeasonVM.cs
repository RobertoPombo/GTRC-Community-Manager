using Newtonsoft.Json;
using Core;
using Database;
using Scripts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Media;

namespace GTRC_Community_Manager
{
    public class PreSeasonVM : ObservableObject
    {
        public static PreSeasonVM Instance;
        private static readonly string PathSettings = MainWindow.dataDirectory + "config preseason.json";
        [JsonIgnore] public BackgroundWorker BackgroundWorkerResetEntries = new() { WorkerSupportsCancellation = true };
        public static readonly Random random = new();
        public static bool IsRunningExportEntrylist = false;

        private static ObservableCollection<Series> listSeries = new();
        private static ObservableCollection<Season> listSeasons = new();

        private int currentSeriesID = Basics.NoID;
        private int currentSeasonID = Basics.NoID;
        private Event currentEvent;
        private int slotsAvailable = 0;
        private int slotsTaken = 0;
        private string slotsTakenText = "0/0";
        private bool stateAutoUpdateEntries;
        private Brush stateEntries = Basics.StateOff;
        private int intervallMinRefreshEntries = 0;
        private int entriesUpdateRemTime = 0;
        private bool isRunningEntries = false;
        private int waitQueueEntries = 0;
        private bool isCheckedBallast = false;
        private bool isCheckedRestriktor = false;
        private bool isCheckedRegisterLimit = false;
        private bool isCheckedBoPFreeze = false;
        private bool isCheckedGridSlotsLimit = false;
        private bool isCheckedSignOutLimit = false;
        private bool isCheckedNoShowLimit = false;
        private bool isCheckedCarChangeLimit = false;
        private bool isCheckedUnlimitedCarVersionChanges = false;
        private int carLimitBallast = 0;
        private int carLimitRestriktor = 0;
        private int carLimitRegisterLimit = 0;
        private int gridSlotsLimit = 0;
        private int signOutLimit = 0;
        private int noShowLimit = 0;
        private int carChangeLimit = 0;
        private int gainBallast = 0;
        private int gainRestriktor = 0;
        private DateTime dateRegisterLimit = DateTime.Now;
        private DateTime dateBoPFreeze = DateTime.Now;
        private DateTime dateCarChangeLimit = DateTime.Now;

        public PreSeasonVM()
        {
            Instance = this;
            RestoreSettingsCmd = new UICmd((o) => RestoreSettings());
            SaveSettingsCmd = new UICmd((o) => SaveSettings());
            ResetEntriesCmd = new UICmd((o) => TriggerResetEntries());
            UpdateEntrylistBoPCmd = new UICmd((o) => UpdateEntrylistBoP());
            if (!File.Exists(PathSettings)) { SaveSettings(); }
            RestoreSettings();
            BackgroundWorkerResetEntries.DoWork += InfiniteLoopResetEntries;
            BackgroundWorkerResetEntries.RunWorkerAsync();
        }

        [JsonIgnore] public ObservableCollection<Series> ListSeries { get { return listSeries; } set { listSeries = value; } }
        [JsonIgnore] public ObservableCollection<Season> ListSeasons { get { return listSeasons; } set { listSeasons = value; } }
        [JsonIgnore] public ObservableCollection<Event> ListEvents { get; set; }

        public int CurrentSeriesID
        {
            get { return currentSeriesID; }
            set { CurrentSeries = Series.Statics.GetByID(value); }
        }

        [JsonIgnore]
        public Series CurrentSeries
        {
            get { Series _series = Series.Statics.GetByID(currentSeriesID); return _series; }
            set { if (value != null && value != CurrentSeries) { currentSeriesID = value.ID; RaisePropertyChanged(); UpdateListSeasons(); UpdateListEvents(); } }
        }

        public int CurrentSeasonID
        {
            get { return currentSeasonID; }
            set
            {
                CurrentSeason = Season.Statics.GetByID(value);
                int _listSeasonsCount = ListSeasons.Count;
                if (CurrentSeason.SeriesID != CurrentSeriesID && _listSeasonsCount > 0) { CurrentSeason = ListSeasons[_listSeasonsCount - 1]; }
            }
        }

        [JsonIgnore] public Season CurrentSeason
        {
            get { return Season.Statics.GetByID(currentSeasonID); }
            set { if (value != null && value != CurrentSeason) { currentSeasonID = value.ID; RaisePropertyChanged(); UpdateListEvents(); } }
        }

        [JsonIgnore] public Event CurrentEvent
        {
            get { return currentEvent; }
            set { currentEvent = value; SlotsAvailable++; RaisePropertyChanged(); }
        }

        [JsonIgnore] public int SlotsAvailable
        {
            get { return slotsAvailable; }
            set
            {
                if (currentEvent is not null)
                {
                    slotsAvailable = GetSlotsAvalable(Track.Statics.GetByID(CurrentEvent.TrackID));
                    SlotsTakenText = "?";
                }
            }
        }

        [JsonIgnore] public int SlotsTaken
        {
            get { return slotsTaken; }
            set { slotsTaken = value; SlotsTakenText = "?"; }
        }

        [JsonIgnore] public string SlotsTakenText
        {
            get { return slotsTakenText; }
            set { slotsTakenText = SlotsTaken.ToString() + "/" + SlotsAvailable.ToString(); RaisePropertyChanged(); }
        }

        public bool StateAutoUpdateEntries
        {
            get { return stateAutoUpdateEntries; }
            set
            {
                stateAutoUpdateEntries = value;
                RaisePropertyChanged();
                if (stateAutoUpdateEntries && StateEntries == Basics.StateOff) { StateEntries = Basics.StateOn; }
                else if (!stateAutoUpdateEntries && StateEntries == Basics.StateOn) { StateEntries = Basics.StateOff; }
                entriesUpdateRemTime = intervallMinRefreshEntries * 60;
                EntriesUpdateRemTime = "?";
            }
        }

        [JsonIgnore] public Brush StateEntries
        {
            get { return stateEntries; }
            set { stateEntries = value; RaisePropertyChanged(); }
        }

        public int IntervallMinRefreshEntries
        {
            get { return intervallMinRefreshEntries; }
            set
            {
                if (value < 1) { value = 1; }
                else if (value > 1440) { value = 1440; }
                intervallMinRefreshEntries = value;
                entriesUpdateRemTime = intervallMinRefreshEntries * 60;
                EntriesUpdateRemTime = "?";
                RaisePropertyChanged();
            }
        }

        [JsonIgnore] public string EntriesUpdateRemTime
        {
            get
            {
                if (entriesUpdateRemTime > 7200) { return ((int)Math.Ceiling((double)entriesUpdateRemTime / (60 * 60))).ToString() + " h"; }
                else if (entriesUpdateRemTime > 120) { return ((int)Math.Ceiling((double)entriesUpdateRemTime / 60)).ToString() + " min"; }
                else { return entriesUpdateRemTime.ToString() + " sec"; }
            }
            set { RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsRunningEntries
        {
            get { return isRunningEntries; }
            set { isRunningEntries = value; SetStateEntries(); }
        }

        [JsonIgnore]
        public int WaitQueueEntries
        {
            get { return waitQueueEntries; }
            set { if (value >= 0) { waitQueueEntries = value; SetStateEntries(); } }
        }

        public bool IsCheckedBallast
        {
            get { return isCheckedBallast; }
            set { isCheckedBallast = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedRestriktor
        {
            get { return isCheckedRestriktor; }
            set { isCheckedRestriktor = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedRegisterLimit
        {
            get { return isCheckedRegisterLimit; }
            set { isCheckedRegisterLimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedBoPFreeze
        {
            get { return isCheckedBoPFreeze; }
            set { isCheckedBoPFreeze = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedGridSlotsLimit
        {
            get { return isCheckedGridSlotsLimit; }
            set { isCheckedGridSlotsLimit = value; RaisePropertyChanged(); SlotsAvailable++; }
        }

        public bool IsCheckedSignOutLimit
        {
            get { return isCheckedSignOutLimit; }
            set { isCheckedSignOutLimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedNoShowLimit
        {
            get { return isCheckedNoShowLimit; }
            set { isCheckedNoShowLimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedCarChangeLimit
        {
            get { return isCheckedCarChangeLimit; }
            set { isCheckedCarChangeLimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedUnlimitedCarVersionChanges
        {
            get { return isCheckedUnlimitedCarVersionChanges; }
            set { isCheckedUnlimitedCarVersionChanges = value; RaisePropertyChanged(); }
        }

        public int CarLimitBallast
        {
            get { return carLimitBallast; }
            set { carLimitBallast = value; RaisePropertyChanged(); }
        }

        public int CarLimitRestriktor
        {
            get { return carLimitRestriktor; }
            set { carLimitRestriktor = value; RaisePropertyChanged(); }
        }

        public int CarLimitRegisterLimit
        {
            get { return carLimitRegisterLimit; }
            set { carLimitRegisterLimit = value; RaisePropertyChanged(); }
        }

        public int GridSlotsLimit
        {
            get { return gridSlotsLimit; }
            set { gridSlotsLimit = value; RaisePropertyChanged(); SlotsAvailable++; }
        }

        public int SignOutLimit
        {
            get { return signOutLimit; }
            set { signOutLimit = value; RaisePropertyChanged(); }
        }

        public int NoShowLimit
        {
            get { return noShowLimit; }
            set { noShowLimit = value; RaisePropertyChanged(); }
        }

        public int CarChangeLimit
        {
            get { return carChangeLimit; }
            set { carChangeLimit = value; RaisePropertyChanged(); }
        }

        public int GainBallast
        {
            get { return gainBallast; }
            set { gainBallast = value; RaisePropertyChanged(); }
        }

        public int GainRestriktor
        {
            get { return gainRestriktor; }
            set { gainRestriktor = value; RaisePropertyChanged(); }
        }

        public DateTime DateRegisterLimit
        {
            get { return dateRegisterLimit; }
            set { dateRegisterLimit = value; RaisePropertyChanged(); }
        }

        public DateTime DateBoPFreeze
        {
            get { return dateBoPFreeze; }
            set { dateBoPFreeze = value; RaisePropertyChanged(); }
        }

        public DateTime DateCarChangeLimit
        {
            get { return dateCarChangeLimit; }
            set { dateCarChangeLimit = value; RaisePropertyChanged(); }
        }

        public int GetSlotsAvalable(Track track)
        {
            if (IsCheckedGridSlotsLimit) { return Math.Min(GridSlotsLimit, track.ServerSlotsCount); }
            else { return track.ServerSlotsCount; }
        }

        public static void UpdateListSeries()
        {
            listSeries = new ObservableCollection<Series>();
            foreach (Series _series in Series.Statics.List) { listSeries.Add(_series); }
            if (Instance is not null)
            {
                int backupCurrentSeriesID = Instance.CurrentSeriesID;
                Instance.RaisePropertyChanged(nameof(ListSeries));
                Instance.CurrentSeriesID = Basics.NoID;
                Instance.CurrentSeriesID = backupCurrentSeriesID;
            }
        }

        public static void UpdateListSeasons()
        {
            listSeasons = new ObservableCollection<Season>();
            if (Instance is null) { foreach (Season _season in Season.Statics.List) { listSeasons.Add(_season); } }
            else
            {
                foreach (Season _season in Season.Statics.List) { if (_season.SeriesID == Instance.CurrentSeriesID) { listSeasons.Add(_season); } }
                int backupCurrentSeasonID = Instance.CurrentSeasonID;
                Instance.RaisePropertyChanged(nameof(ListSeasons));
                Instance.CurrentSeasonID = Basics.NoID;
                Instance.CurrentSeasonID = backupCurrentSeasonID;
            }
        }

        public static void UpdateListEvents()
        {
            if (Instance is not null)
            {
                Event.SortByDate();
                Instance.ListEvents = new ObservableCollection<Event>();
                List<Event> eventList = Event.Statics.GetBy(nameof(Event.SeasonID), Instance.CurrentSeasonID);
                foreach (Event _event in eventList) { Instance.ListEvents.Add(_event); }
                Instance.RaisePropertyChanged(nameof(ListEvents));
                Instance.SetCurrentEvent();
            }
        }

        public void SetCurrentEvent()
        {
            CurrentEvent = Event.GetNextEvent(CurrentSeasonID, DateTime.Now);
        }

        public void InfiniteLoopResetEntries(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (StateAutoUpdateEntries)
                {
                    entriesUpdateRemTime -= 1;
                    EntriesUpdateRemTime = "?";
                    if (entriesUpdateRemTime == 0)
                    {
                        TriggerResetEntries();
                        entriesUpdateRemTime = intervallMinRefreshEntries * 60;
                    }
                }
            }
        }

        public void ThreadResetEntries()
        {
            WaitQueueEntries++;
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunningEntries = true;
            WaitQueueEntries--;
            //Lap.Statics.LoadSQL();
            ResetEntries();
            //PreSeason.UpdatePreQResults(CurrentSeasonID);
            UpdateBoPForEvent(CurrentEvent);
            GSheets.UpdateBoPStandings(CurrentEvent, GSheet.ListIDs[2].DocID, GSheet.ListIDs[2].SheetID);
            //GSheets.UpdatePreQStandings(GSheet.ListIDs[1].DocID, GSheet.ListIDs[1].SheetID);
            IsRunningEntries = false;
        }

        public void TriggerResetEntries()
        {
            new Thread(ThreadResetEntries).Start();
        }

        public void SetStateEntries()
        {
            if (IsRunningEntries)
            {
                if (WaitQueueEntries > 0) { StateEntries = Basics.StateRunWait; }
                else { StateEntries = Basics.StateRun; }
            }
            else
            {
                if (WaitQueueEntries > 0) { StateEntries = Basics.StateWait; }
                else { if (StateAutoUpdateEntries) { StateEntries = Basics.StateOn; } else { StateEntries = Basics.StateOff; } }
            }
        }

        public void ResetEntries()
        {
            GSheets.SyncFormsEntries(CurrentSeasonID, GSheet.ListIDs[4].DocID, GSheet.ListIDs[4].SheetID, "A:I");
            GSheets.UpdateEntriesCurrentEvent(GSheet.ListIDs[6].DocID, GSheet.ListIDs[6].SheetID, CurrentEvent);
        }

        public void UpdateEntrylistBoP()
        {
            new Thread(() => ThreadUpdateEntrylistBoP(CurrentEvent)).Start();
            MainVM.List[0].LogCurrentText = "Export entrylists and BoPs...";
        }

        public void ThreadUpdateEntrylistBoP(Event _event)
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunningExportEntrylist = true;
            _ = ThreadUpdateEntrylistBoP_Int(_event);
            IsRunningExportEntrylist = false;
        }

        public int ThreadUpdateEntrylistBoP_Int(Event _event)
        {
            PreSeason.UpdateEntryPriority(_event.SeasonID);
            PreSeason.UpdateName3Digits(_event.SeasonID);
            PreSeason.EntryAutoSignOut(_event, SignOutLimit, NoShowLimit);
            UpdateBoPForEvent(_event);
            (List<Entry> EntriesSignedIn, List<Entry> EntriesSignedOut) = PreSeason.DetermineEntrylist(_event, SlotsAvailable, DateRegisterLimit);
            int tempSlotsTaken = EntriesSignedIn.Count;
            (EntriesSignedIn, EntriesSignedOut) = PreSeason.FillUpEntrylist(_event, SlotsAvailable, EntriesSignedIn, EntriesSignedOut);
            GSheets.UpdateEntriesCurrentEvent(GSheet.ListIDs[6].DocID, GSheet.ListIDs[6].SheetID, CurrentEvent);
            GSheets.UpdateCarChanges(GSheet.ListIDs[7].DocID, GSheet.ListIDs[7].SheetID, _event.SeasonID);
            if (_event.ID == CurrentEvent.ID)
            {
                SlotsTaken = tempSlotsTaken;
                GSheets.UpdateBoPStandings(CurrentEvent, GSheet.ListIDs[2].DocID, GSheet.ListIDs[2].SheetID);
                ServerVM.UpdateEntrylists();
                ServerVM.UpdateBoPs();
                MainVM.List[0].LogCurrentText = "Entrylist exported.";
            }
            return tempSlotsTaken;
        }

        public void UpdateBoPForEvent(Event _event)
        {
            PreSeason.CountCars(_event, DateRegisterLimit, CarLimitRegisterLimit, DateBoPFreeze, IsCheckedRegisterLimit, IsCheckedBoPFreeze);
            PreSeason.CalcBoP(_event, CarLimitBallast, CarLimitRestriktor, GainBallast, GainRestriktor, IsCheckedBallast, IsCheckedRestriktor);
        }

        public int CarChangeCount(Entry entry, DateTime maxEventDate)
        {
            int carChangeCount = 0;
            if (IsCheckedCarChangeLimit && entry.ScorePoints)
            {
                List<EventsEntries> eventList = EventsEntries.GetAnyBy(nameof(EventsEntries.EntryID), entry.ID);
                eventList = EventsEntries.SortByDate(eventList);
                Car currentCar = Car.Statics.GetByID(entry.CarID);
                for (int index = 0; index < eventList.Count; index++)
                {
                    Event _event = Event.Statics.GetByID(eventList[index].EventID);
                    Car _eventCar = Car.Statics.GetByID(eventList[index].CarID);
                    bool carChange = _eventCar.ID != currentCar.ID;
                    bool isVersionChange = _eventCar.Manufacturer == currentCar.Manufacturer && _eventCar.Category == currentCar.Category;
                    carChange = carChange && (!IsCheckedUnlimitedCarVersionChanges || !isVersionChange);
                    if (eventList[index].CarChangeDate > DateCarChangeLimit && _event.EventDate < maxEventDate && carChange) { carChangeCount++; }
                    currentCar = Car.Statics.GetByID(eventList[index].CarID);
                }
            }
            return carChangeCount;
        }

        public void RestoreSettings()
        {
            try
            {
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(PathSettings, Encoding.Unicode));
                CurrentSeriesID = obj?.CurrentSeriesID ?? currentSeriesID;
                CurrentSeasonID = obj?.CurrentSeasonID ?? currentSeasonID;
                StateAutoUpdateEntries = obj?.StateAutoUpdateEntries ?? stateAutoUpdateEntries;
                IntervallMinRefreshEntries = obj?.IntervallMinRefreshEntries ?? intervallMinRefreshEntries;
                IsCheckedBallast = obj?.IsCheckedBallast ?? isCheckedBallast;
                IsCheckedRestriktor = obj?.IsCheckedRestriktor ?? isCheckedRestriktor;
                IsCheckedRegisterLimit = obj?.IsCheckedRegisterLimit ?? isCheckedRegisterLimit;
                IsCheckedBoPFreeze = obj?.IsCheckedBoPFreeze ?? isCheckedBoPFreeze;
                IsCheckedGridSlotsLimit = obj?.IsCheckedGridSlotsLimit ?? isCheckedGridSlotsLimit;
                IsCheckedSignOutLimit = obj?.IsCheckedSignOutLimit ?? isCheckedSignOutLimit;
                IsCheckedNoShowLimit = obj?.IsCheckedNoShowLimit ?? isCheckedNoShowLimit;
                IsCheckedCarChangeLimit = obj?.IsCheckedCarChangeLimit ?? isCheckedCarChangeLimit;
                IsCheckedUnlimitedCarVersionChanges = obj?.IsCheckedUnlimitedCarVersionChanges ?? isCheckedUnlimitedCarVersionChanges;
                CarLimitBallast = obj?.CarLimitBallast ?? carLimitBallast;
                CarLimitRestriktor =    obj?.CarLimitRestriktor ?? carLimitRestriktor;
                CarLimitRegisterLimit = obj?.CarLimitRegisterLimit ?? carLimitRegisterLimit;
                GridSlotsLimit = obj?.GridSlotsLimit ?? gridSlotsLimit;
                SignOutLimit = obj?.SignOutLimit ?? signOutLimit;
                NoShowLimit = obj?.NoShowLimit ?? noShowLimit;
                CarChangeLimit = obj?.CarChangeLimit ?? carChangeLimit;
                GainBallast = obj?.GainBallast ?? gainBallast;
                GainRestriktor = obj?.GainRestriktor ?? gainRestriktor;
                DateRegisterLimit = obj?.DateRegisterLimit ?? dateRegisterLimit;
                DateBoPFreeze = obj?.DateBoPFreeze ?? dateBoPFreeze;
                DateCarChangeLimit = obj?.DateCarChangeLimit ?? dateCarChangeLimit;
                MainVM.List[0].LogCurrentText = "Pre-Season settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore pre-Season settings failed!"; }
        }

        public void SaveSettings()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(PathSettings, text, Encoding.Unicode);
            MainVM.List[0].LogCurrentText = "Pre-Season settings saved.";
        }

        [JsonIgnore] public UICmd RestoreSettingsCmd { get; set; }
        [JsonIgnore] public UICmd SaveSettingsCmd { get; set; }
        [JsonIgnore] public UICmd ResetEntriesCmd { get; set; }
        [JsonIgnore] public UICmd UpdateEntrylistBoPCmd { get; set; }
    }
}
