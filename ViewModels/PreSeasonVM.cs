using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using GTRCLeagueManager.Database;

namespace GTRCLeagueManager
{
    public class PreSeasonVM : ObservableObject
    {
        public static PreSeasonVM Instance;
        private static readonly string PathSettings = MainWindow.dataDirectory + "config preseason.json";
        [JsonIgnore] public BackgroundWorker BackgroundWorkerResetEntries = new() { WorkerSupportsCancellation = true };
        public static readonly Random random = new();

        private static ObservableCollection<Series> listSeries = new();
        private static ObservableCollection<Season> listSeasons = new();

        private int currentSeriesID = Basics.NoID;
        private int currentSeasonID = Basics.NoID;
        private Event currentevent;
        private int slotsavailable = 0;
        private int slotstaken = 0;
        private string slotstakentext = "0/0";
        private bool stateautoupdateentries;
        private Brush stateentries;
        private int intervallminrefreshentries = 0;
        private int entriesupdateremtime = 0;
        private bool isrunningentries = false;
        private int waitqueueentries = 0;
        private bool ischeckedballast = false;
        private bool ischeckedrestriktor = false;
        private bool ischeckedregisterlimit = false;
        private bool ischeckedbopfreeze = false;
        private bool ischeckedgridslotslimit = false;
        private bool ischeckedsignoutlimit = false;
        private bool ischeckednoshowlimit = false;
        private bool ischeckedcarchangelimit = false;
        private int carlimitballast = 0;
        private int carlimitrestriktor = 0;
        private int carlimitregisterlimit = 0;
        private int gridslotslimit = 0;
        private int signoutlimit = 0;
        private int noshowlimit = 0;
        private int carchangelimit = 0;
        private int gainballast = 0;
        private int gainrestriktor = 0;
        private DateTime dateregisterlimit = DateTime.Now;
        private DateTime datebopfreeze = DateTime.Now;
        private DateTime datecarchangelimit = DateTime.Now;

        public PreSeasonVM()
        {
            Instance = this;
            RestoreSettingsCmd = new UICmd((o) => RestoreSettings());
            SaveSettingsCmd = new UICmd((o) => SaveSettings());
            ResetEntriesCmd = new UICmd((o) => TriggerResetEntries());
            UpdateEntrylistBoPCmd = new UICmd((o) => UpdateEntrylistBoP());
            if (!File.Exists(PathSettings)) { SaveSettings(); }
            RestoreSettings();
            StateEntries = ServerM.StateOff;
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
            get { return currentevent; }
            set { currentevent = value; SlotsAvailable++; RaisePropertyChanged(); }
        }

        [JsonIgnore] public int SlotsAvailable
        {
            get { return slotsavailable; }
            set
            {
                if (currentevent is not null)
                {
                    slotsavailable = GetSlotsAvalable(Track.Statics.GetByID(CurrentEvent.TrackID));
                    SlotsTakenText = "?";
                }
            }
        }

        [JsonIgnore] public int SlotsTaken
        {
            get { return slotstaken; }
            set { slotstaken = value; SlotsTakenText = "?"; }
        }

        [JsonIgnore] public string SlotsTakenText
        {
            get { return slotstakentext; }
            set { slotstakentext = SlotsTaken.ToString() + "/" + SlotsAvailable.ToString(); RaisePropertyChanged(); }
        }

        public bool StateAutoUpdateEntries
        {
            get { return stateautoupdateentries; }
            set
            {
                stateautoupdateentries = value;
                RaisePropertyChanged();
                if (stateautoupdateentries && StateEntries == ServerM.StateOff) { StateEntries = ServerM.StateOn; }
                else if (!stateautoupdateentries && StateEntries == ServerM.StateOn) { StateEntries = ServerM.StateOff; }
                entriesupdateremtime = intervallminrefreshentries * 60;
                EntriesUpdateRemTime = "?";
            }
        }

        [JsonIgnore] public Brush StateEntries
        {
            get { return stateentries; }
            set { stateentries = value; RaisePropertyChanged(); }
        }

        public int IntervallMinRefreshEntries
        {
            get { return intervallminrefreshentries; }
            set
            {
                if (value < 1) { value = 1; }
                else if (value > 1440) { value = 1440; }
                intervallminrefreshentries = value;
                entriesupdateremtime = intervallminrefreshentries * 60;
                EntriesUpdateRemTime = "?";
                RaisePropertyChanged();
            }
        }

        [JsonIgnore] public string EntriesUpdateRemTime
        {
            get
            {
                if (entriesupdateremtime > 7200) { return ((int)Math.Ceiling((double)entriesupdateremtime / (60 * 60))).ToString() + " h"; }
                else if (entriesupdateremtime > 120) { return ((int)Math.Ceiling((double)entriesupdateremtime / 60)).ToString() + " min"; }
                else { return entriesupdateremtime.ToString() + " sec"; }
            }
            set { RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsRunningEntries
        {
            get { return isrunningentries; }
            set { isrunningentries = value; SetStateEntries(); }
        }

        [JsonIgnore]
        public int WaitQueueEntries
        {
            get { return waitqueueentries; }
            set { if (value >= 0) { waitqueueentries = value; SetStateEntries(); } }
        }

        public bool IsCheckedBallast
        {
            get { return ischeckedballast; }
            set { ischeckedballast = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedRestriktor
        {
            get { return ischeckedrestriktor; }
            set { ischeckedrestriktor = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedRegisterLimit
        {
            get { return ischeckedregisterlimit; }
            set { ischeckedregisterlimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedBoPFreeze
        {
            get { return ischeckedbopfreeze; }
            set { ischeckedbopfreeze = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedGridSlotsLimit
        {
            get { return ischeckedgridslotslimit; }
            set { ischeckedgridslotslimit = value; RaisePropertyChanged(); SlotsAvailable++; }
        }

        public bool IsCheckedSignOutLimit
        {
            get { return ischeckedsignoutlimit; }
            set { ischeckedsignoutlimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedNoShowLimit
        {
            get { return ischeckednoshowlimit; }
            set { ischeckednoshowlimit = value; RaisePropertyChanged(); }
        }

        public bool IsCheckedCarChangeLimit
        {
            get { return ischeckedcarchangelimit; }
            set { ischeckedcarchangelimit = value; RaisePropertyChanged(); }
        }

        public int CarLimitBallast
        {
            get { return carlimitballast; }
            set { carlimitballast = value; RaisePropertyChanged(); }
        }

        public int CarLimitRestriktor
        {
            get { return carlimitrestriktor; }
            set { carlimitrestriktor = value; RaisePropertyChanged(); }
        }

        public int CarLimitRegisterLimit
        {
            get { return carlimitregisterlimit; }
            set { carlimitregisterlimit = value; RaisePropertyChanged(); }
        }

        public int GridSlotsLimit
        {
            get { return gridslotslimit; }
            set { gridslotslimit = value; RaisePropertyChanged(); SlotsAvailable++; }
        }

        public int SignOutLimit
        {
            get { return signoutlimit; }
            set { signoutlimit = value; RaisePropertyChanged(); }
        }

        public int NoShowLimit
        {
            get { return noshowlimit; }
            set { noshowlimit = value; RaisePropertyChanged(); }
        }

        public int CarChangeLimit
        {
            get { return carchangelimit; }
            set { carchangelimit = value; RaisePropertyChanged(); }
        }

        public int GainBallast
        {
            get { return gainballast; }
            set { gainballast = value; RaisePropertyChanged(); }
        }

        public int GainRestriktor
        {
            get { return gainrestriktor; }
            set { gainrestriktor = value; RaisePropertyChanged(); }
        }

        public DateTime DateRegisterLimit
        {
            get { return dateregisterlimit; }
            set { dateregisterlimit = value; RaisePropertyChanged(); }
        }

        public DateTime DateBoPFreeze
        {
            get { return datebopfreeze; }
            set { datebopfreeze = value; RaisePropertyChanged(); }
        }

        public DateTime DateCarChangeLimit
        {
            get { return datecarchangelimit; }
            set { datecarchangelimit = value; RaisePropertyChanged(); }
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
                    entriesupdateremtime -= 1;
                    EntriesUpdateRemTime = "?";
                    if (entriesupdateremtime == 0)
                    {
                        TriggerResetEntries();
                        entriesupdateremtime = intervallminrefreshentries * 60;
                    }
                }
            }
        }

        public void ThreadResetEntries()
        {
            WaitQueueEntries++;
            while (CheckExistingThreads()) { Thread.Sleep(200 + random.Next(100)); }
            IsRunningEntries = true;
            WaitQueueEntries--;
            Lap.Statics.LoadSQL();
            ResetEntries();
            PreSeason.UpdatePreQResults(CurrentSeasonID);
            PreSeason.CountCars(CurrentEvent, DateRegisterLimit, CarLimitRegisterLimit, DateBoPFreeze, IsCheckedRegisterLimit, IsCheckedBoPFreeze);
            PreSeason.CalcBoP(CurrentEvent, CarLimitBallast, CarLimitRestriktor, GainBallast, GainRestriktor, IsCheckedBallast, IsCheckedRestriktor);
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
                if (WaitQueueEntries > 0) { StateEntries = ServerM.StateRunWait; }
                else { StateEntries = ServerM.StateRun; }
            }
            else
            {
                if (WaitQueueEntries > 0) { StateEntries = ServerM.StateWait; }
                else { if (StateAutoUpdateEntries) { StateEntries = ServerM.StateOn; } else { StateEntries = ServerM.StateOff; } }
            }
        }

        public bool CheckExistingThreads()
        {
            if (IsRunningEntries) { return true; }
            foreach (ServerM _server in ServerM.List) { if (_server.IsRunning) { return true; } }
            return false;
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
                int currentCarID = entry.CarID;
                for (int index = 0; index < eventList.Count; index++)
                {
                    Event _event = Event.Statics.GetByID(eventList[index].EventID);
                    if (eventList[index].CarChangeDate > DateCarChangeLimit && _event.EventDate < maxEventDate && eventList[index].CarID != currentCarID)
                    {
                        carChangeCount++;
                    }
                    currentCarID = eventList[index].CarID;
                }
            }
            return carChangeCount;
        }

        public void RestoreSettings()
        {
            try
            {
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(PathSettings, Encoding.Unicode));
                CurrentSeriesID = obj.CurrentSeriesID ?? currentSeriesID;
                CurrentSeasonID = obj.CurrentSeasonID ?? currentSeasonID;
                StateAutoUpdateEntries = obj.StateAutoUpdateEntries ?? stateautoupdateentries;
                IntervallMinRefreshEntries = obj.IntervallMinRefreshEntries ?? intervallminrefreshentries;
                IsCheckedBallast = obj.IsCheckedBallast ?? ischeckedballast;
                IsCheckedRestriktor = obj.IsCheckedRestriktor ?? ischeckedrestriktor;
                IsCheckedRegisterLimit = obj.IsCheckedRegisterLimit ?? ischeckedregisterlimit;
                IsCheckedBoPFreeze = obj.IsCheckedBoPFreeze ?? ischeckedbopfreeze;
                IsCheckedGridSlotsLimit = obj.IsCheckedGridSlotsLimit ?? ischeckedgridslotslimit;
                IsCheckedSignOutLimit = obj.IsCheckedSignOutLimit ?? ischeckedsignoutlimit;
                IsCheckedNoShowLimit = obj.IsCheckedNoShowLimit ?? ischeckednoshowlimit;
                IsCheckedCarChangeLimit = obj.IsCheckedCarChangeLimit ?? ischeckedcarchangelimit;
                CarLimitBallast = obj.CarLimitBallast ?? carlimitballast;
                CarLimitRestriktor = obj.CarLimitRestriktor ?? carlimitrestriktor;
                CarLimitRegisterLimit = obj.CarLimitRegisterLimit ?? carlimitregisterlimit;
                GridSlotsLimit = obj.GridSlotsLimit ?? gridslotslimit;
                SignOutLimit = obj.SignOutLimit ?? signoutlimit;
                NoShowLimit = obj.NoShowLimit ?? noshowlimit;
                CarChangeLimit = obj.CarChangeLimit ?? carchangelimit;
                GainBallast = obj.GainBallast ?? gainballast;
                GainRestriktor = obj.GainRestriktor ?? gainrestriktor;
                DateRegisterLimit = obj.DateRegisterLimit ?? dateregisterlimit;
                DateBoPFreeze = obj.DateBoPFreeze ?? datebopfreeze;
                DateCarChangeLimit = obj.DateCarChangeLimit ?? datecarchangelimit;
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
