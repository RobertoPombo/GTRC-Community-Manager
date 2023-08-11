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
        private static ObservableCollection<SeasonM> listSeasons = new();

        private int currentSeriesID = Basics.NoID;
        private SeasonM currentSeasonM = new (new Season(false));
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
        private bool isCheckedLapRange = false;
        private int lapRange = 0;
        private bool isCheckedMaxInvalidInRange = false;
        private int maxInvalidInRange = 0;
        private bool isCheckedMaxDeltaInRange = false;
        private float maxDeltaInRange = 100;
        private bool isCheckedOnlySectors13 = false;
        private bool isCheckedMinConsecutiveValidLaps = false;
        private int minConsecutiveValidLaps = -1;
        private int minLapsCount = 1;
        private int maxLapsCount = 1;
        private bool forceLapsCountConsecutive = false;
        private int lapsCountCombined = -1;
        private int minLapsRequired = -1;

        public PreSeasonVM()
        {
            Instance = this;
            RestoreSettingsCmd = new UICmd((o) => RestoreSettings());
            SaveSettingsCmd = new UICmd((o) => SaveSettings());
            ResetEntriesCmd = new UICmd((o) => TriggerResetEntries());
            UpdateEntrylistBoPCmd = new UICmd((o) => UpdateEntrylistBoP());
            PublishTrackReportCmd = new UICmd((o) => PublishTrackReport());
            SeasonSaveSQLCmd = new UICmd((o) => SeasonSaveSQL());
            SeasonLoadSQLCmd = new UICmd((o) => SeasonLoadSQL());
            if (!File.Exists(PathSettings)) { SaveSettings(); }
            RestoreSettings();
            BackgroundWorkerResetEntries.DoWork += InfiniteLoopResetEntries;
            BackgroundWorkerResetEntries.RunWorkerAsync();
        }

        [JsonIgnore] public ObservableCollection<Series> ListSeries { get { return listSeries; } set { listSeries = value; } }
        [JsonIgnore] public ObservableCollection<SeasonM> ListSeasons { get { return listSeasons; } set { listSeasons = value; } }
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
            get { return currentSeasonM.Season.ID; }
            set
            {
                if (value != CurrentSeasonID)
                {
                    for (int _seasonNr = 0; _seasonNr < listSeasons.Count; _seasonNr++)
                    {
                        if (listSeasons[_seasonNr].Season.ID == value || _seasonNr == listSeasons.Count - 1)
                        {
                            CurrentSeasonM = listSeasons[_seasonNr];
                            break;
                        }
                    }
                }
            }
        }

        [JsonIgnore] public SeasonM CurrentSeasonM
        {
            get { return currentSeasonM; }
            set { if (value != null) { currentSeasonM = value; RaisePropertyChanged(); UpdateListEvents(); } }
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
                    slotsAvailable = GetSlotsAvalable(CurrentEvent.ObjTrack, CurrentSeasonID);
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

        public bool IsCheckedLapRange
        {
            get { return lapRange == int.MaxValue; }
            set { lapRange = int.MaxValue; RaisePropertyChanged(); }
        }

        public int LapRange
        {
            get { if (lapRange == int.MaxValue) { return -1; } else { return lapRange; } }
            set { if (value >= 0) { lapRange = value; RaisePropertyChanged(); } }
        }

        public bool IsCheckedMaxInvalidInRange
        {
            get { return maxInvalidInRange == int.MaxValue; }
            set { maxInvalidInRange = int.MaxValue; RaisePropertyChanged(); }
        }

        public int MaxInvalidInRange
        {
            get { if (maxInvalidInRange == int.MaxValue) { return -1; } else { return maxInvalidInRange; } }
            set { if (value >= 0) { maxInvalidInRange = value; RaisePropertyChanged(); } }
        }

        public bool IsCheckedMaxDeltaInRange
        {
            get { return maxDeltaInRange == int.MaxValue; }
            set { if (value) { maxDeltaInRange = int.MaxValue; } else { } RaisePropertyChanged(); }
        }

        public float MaxDeltaInRange
        {
            get { if (maxDeltaInRange == int.MaxValue) { return -1; } else { return maxDeltaInRange; } }
            set { if (value >= 100) { maxDeltaInRange = value; RaisePropertyChanged(); } }
        }

        public bool IsCheckedOnlySectors13
        {
            get { return isCheckedOnlySectors13; }
            set { isCheckedOnlySectors13 = value; }
        }

        public bool IsCheckedConsecutiveValidLapsMin
        {
            get { return minConsecutiveValidLaps == 0; }
            set { minConsecutiveValidLaps = 0; }
        }

        public int MinConsecutiveValidLaps
        {
            get { return minConsecutiveValidLaps; }
            set { minConsecutiveValidLaps = value; }
        }

        public int MinLapsCount
        {
            get { return minLapsCount; }
            set { minLapsCount = value; }
        }

        public int MaxLapsCount
        {
            get { return maxLapsCount; }
            set { maxLapsCount = value; }
        }

        public bool ForceLapsCountConsecutive
        {
            get { return forceLapsCountConsecutive; }
            set { forceLapsCountConsecutive = value; }
        }

        public int LapsCountCombined
        {
            get { return lapsCountCombined; }
            set { lapsCountCombined = value; }
        }

        public string ExplanationLeaderboardSettings { get; set; }

        public int GetSlotsAvalable(Track track, int _seasonID)
        {
            return Math.Min(Season.Statics.GetByID(_seasonID).GridSlotsLimit, track.ServerSlotsCount);
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
            if (Instance is null)
            {
                listSeasons = new ObservableCollection<SeasonM>();
                foreach (Season _season in Season.Statics.List) { listSeasons.Add(new SeasonM(_season)); }
            }
            else
            {
                int backupCurrentSeasonID = Instance.CurrentSeasonID;
                listSeasons = new ObservableCollection<SeasonM>();
                foreach (Season _season in Season.Statics.List) { if (_season.SeriesID == Instance.CurrentSeriesID) { listSeasons.Add(new SeasonM(_season)); } }
                Instance.RaisePropertyChanged(nameof(ListSeasons));
                Instance.CurrentSeasonM = new (new Season(false));
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
            PreSeason.UpdatePreQResults(CurrentSeasonID);
            UpdateBoPForEvent(CurrentEvent);
            GSheets.UpdateBoPStandings(CurrentEvent, GSheet.ListIDs[2].DocID, GSheet.ListIDs[2].SheetID);
            GSheets.UpdatePreQStandings(GSheet.ListIDs[1].DocID, GSheet.ListIDs[1].SheetID);
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
            int _slotsAvailable = GetSlotsAvalable(_event.ObjTrack, _event.SeasonID);
            PreSeason.UpdateName3Digits(_event.SeasonID);
            PreSeason.SetEntry_NotScorePoints_NotPermanent(_event);
            UpdateBoPForEvent(_event);
            (List<EventsEntries> SignedIn, List<EventsEntries> SignedOut) = PreSeason.DetermineEntrylist(_event, _slotsAvailable);
            int tempSlotsTaken = SignedIn.Count;
            (SignedIn, SignedOut) = PreSeason.FillUpEntrylist(_slotsAvailable, SignedIn, SignedOut);
            GSheets.UpdateEntriesCurrentEvent(GSheet.ListIDs[6].DocID, GSheet.ListIDs[6].SheetID, CurrentEvent);
            GSheets.UpdatePointsResets(GSheet.ListIDs[7].DocID, GSheet.ListIDs[7].SheetID, _event.SeasonID);
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
            PreSeason.CountCars(_event);
            PreSeason.CalcBoP(_event);
        }

        public void PublishTrackReport()
        {

        }

        public void RestoreSettings()
        {
            try
            {
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(PathSettings, Encoding.Unicode));
                CurrentSeriesID = obj?.CurrentSeriesID ?? currentSeriesID;
                CurrentSeasonID = obj?.CurrentSeasonID ?? CurrentSeasonID;
                StateAutoUpdateEntries = obj?.StateAutoUpdateEntries ?? stateAutoUpdateEntries;
                IntervallMinRefreshEntries = obj?.IntervallMinRefreshEntries ?? intervallMinRefreshEntries;
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

        public void SeasonSaveSQL()
        {
            CurrentSeasonM.Season = Season.Statics.WriteSQL(CurrentSeasonM.Season);
            MainVM.List[0].LogCurrentText = "Season settings saved to SQL Database.";
        }

        public void SeasonLoadSQL()
        {
            CurrentSeasonM.Season = Season.Statics.GetByIdSQL(CurrentSeasonM.Season.ID);
            MainVM.List[0].LogCurrentText = "Season settings restored from SQL Database.";
        }

        [JsonIgnore] public UICmd RestoreSettingsCmd { get; set; }
        [JsonIgnore] public UICmd SaveSettingsCmd { get; set; }
        [JsonIgnore] public UICmd ResetEntriesCmd { get; set; }
        [JsonIgnore] public UICmd UpdateEntrylistBoPCmd { get; set; }
        [JsonIgnore] public UICmd PublishTrackReportCmd { get; set; }
        [JsonIgnore] public UICmd SeasonSaveSQLCmd { get; set; }
        [JsonIgnore] public UICmd SeasonLoadSQLCmd { get; set; }
    }
}
