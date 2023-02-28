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
        [JsonIgnore] public BackgroundWorker BackgroundWorkerResetEntries = new BackgroundWorker() { WorkerSupportsCancellation = true };
        public static readonly Random random = new Random();

        private static ObservableCollection<Season> listSeasons = new ObservableCollection<Season>();

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
            StateEntries = ServerVM.StateOff;
            BackgroundWorkerResetEntries.DoWork += InfiniteLoopResetEntries;
            BackgroundWorkerResetEntries.RunWorkerAsync();
        }

        [JsonIgnore] public ObservableCollection<Season> ListSeasons { get { return listSeasons; } set { listSeasons = value; } }
        [JsonIgnore] public ObservableCollection<Event> ListEvents { get; set; }

        public int CurrentSeasonID
        {
            get { return currentSeasonID; }
            set { CurrentSeason = Season.Statics.GetByID(value); }
        }

        [JsonIgnore]
        public Season CurrentSeason
        {
            get { Season _season = Season.Statics.GetByID(currentSeasonID); return _season; }
            set { if (value != null && value != CurrentSeason) { UpdateListEvents(); currentSeasonID = value.ID; this.RaisePropertyChanged(); } }
        }

        [JsonIgnore]
        public Event CurrentEvent
        {
            get { return currentevent; }
            set { currentevent = value; SlotsAvailable++; this.RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public int SlotsAvailable
        {
            get { return slotsavailable; }
            set
            {
                if (currentevent != null)
                {
                    slotsavailable = GetSlotsAvalable(Track.Statics.GetByUniqueProp(CurrentEvent.TrackID));
                    SlotsTakenText = "?";
                }
            }
        }

        [JsonIgnore]
        public int SlotsTaken
        {
            get { return slotstaken; }
            set { slotstaken = value; SlotsTakenText = "?"; }
        }

        [JsonIgnore]
        public string SlotsTakenText
        {
            get { return slotstakentext; }
            set { slotstakentext = SlotsTaken.ToString() + "/" + SlotsAvailable.ToString(); this.RaisePropertyChanged(); }
        }

        public bool StateAutoUpdateEntries
        {
            get { return stateautoupdateentries; }
            set
            {
                stateautoupdateentries = value;
                this.RaisePropertyChanged();
                if (stateautoupdateentries && StateEntries == ServerVM.StateOff) { StateEntries = ServerVM.StateOn; }
                else if (!stateautoupdateentries && StateEntries == ServerVM.StateOn) { StateEntries = ServerVM.StateOff; }
                entriesupdateremtime = intervallminrefreshentries * 60;
                EntriesUpdateRemTime = "?";
            }
        }

        [JsonIgnore]
        public Brush StateEntries
        {
            get { return stateentries; }
            set { stateentries = value; this.RaisePropertyChanged(); }
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
                this.RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public string EntriesUpdateRemTime
        {
            get
            {
                if (entriesupdateremtime > 7200) { return ((int)Math.Ceiling((double)entriesupdateremtime / (60 * 60))).ToString() + " h"; }
                else if (entriesupdateremtime > 120) { return ((int)Math.Ceiling((double)entriesupdateremtime / 60)).ToString() + " min"; }
                else { return entriesupdateremtime.ToString() + " sec"; }
            }
            set { this.RaisePropertyChanged(); }
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
            set { ischeckedballast = value; this.RaisePropertyChanged(); }
        }

        public bool IsCheckedRestriktor
        {
            get { return ischeckedrestriktor; }
            set { ischeckedrestriktor = value; this.RaisePropertyChanged(); }
        }

        public bool IsCheckedRegisterLimit
        {
            get { return ischeckedregisterlimit; }
            set { ischeckedregisterlimit = value; this.RaisePropertyChanged(); }
        }

        public bool IsCheckedBoPFreeze
        {
            get { return ischeckedbopfreeze; }
            set { ischeckedbopfreeze = value; this.RaisePropertyChanged(); }
        }

        public bool IsCheckedGridSlotsLimit
        {
            get { return ischeckedgridslotslimit; }
            set { ischeckedgridslotslimit = value; this.RaisePropertyChanged(); SlotsAvailable++; }
        }

        public bool IsCheckedSignOutLimit
        {
            get { return ischeckedsignoutlimit; }
            set { ischeckedsignoutlimit = value; this.RaisePropertyChanged(); }
        }

        public bool IsCheckedNoShowLimit
        {
            get { return ischeckednoshowlimit; }
            set { ischeckednoshowlimit = value; this.RaisePropertyChanged(); }
        }

        public bool IsCheckedCarChangeLimit
        {
            get { return ischeckedcarchangelimit; }
            set { ischeckedcarchangelimit = value; this.RaisePropertyChanged(); }
        }

        public int CarLimitBallast
        {
            get { return carlimitballast; }
            set { carlimitballast = value; this.RaisePropertyChanged(); }
        }

        public int CarLimitRestriktor
        {
            get { return carlimitrestriktor; }
            set { carlimitrestriktor = value; this.RaisePropertyChanged(); }
        }

        public int CarLimitRegisterLimit
        {
            get { return carlimitregisterlimit; }
            set { carlimitregisterlimit = value; this.RaisePropertyChanged(); }
        }

        public int GridSlotsLimit
        {
            get { return gridslotslimit; }
            set { gridslotslimit = value; this.RaisePropertyChanged(); SlotsAvailable++; }
        }

        public int SignOutLimit
        {
            get { return signoutlimit; }
            set { signoutlimit = value; this.RaisePropertyChanged(); }
        }

        public int NoShowLimit
        {
            get { return noshowlimit; }
            set { noshowlimit = value; this.RaisePropertyChanged(); }
        }

        public int CarChangeLimit
        {
            get { return carchangelimit; }
            set { carchangelimit = value; this.RaisePropertyChanged(); }
        }

        public int GainBallast
        {
            get { return gainballast; }
            set { gainballast = value; this.RaisePropertyChanged(); }
        }

        public int GainRestriktor
        {
            get { return gainrestriktor; }
            set { gainrestriktor = value; this.RaisePropertyChanged(); }
        }

        public DateTime DateRegisterLimit
        {
            get { return dateregisterlimit; }
            set { dateregisterlimit = value; this.RaisePropertyChanged(); }
        }

        public DateTime DateBoPFreeze
        {
            get { return datebopfreeze; }
            set { datebopfreeze = value; this.RaisePropertyChanged(); }
        }

        public DateTime DateCarChangeLimit
        {
            get { return datecarchangelimit; }
            set { datecarchangelimit = value; this.RaisePropertyChanged(); }
        }

        public int GetSlotsAvalable(Track track)
        {
            if (IsCheckedGridSlotsLimit) { return Math.Min(GridSlotsLimit, track.ServerSlotsCount); }
            else { return track.ServerSlotsCount; }
        }

        public static void UpdateListSeasons()
        {
            listSeasons = new ObservableCollection<Season>();
            foreach (Season _season in Season.Statics.List) { listSeasons.Add(_season); }
            if (Instance != null)
            {
                int backupCurrentSeasonID = Instance.CurrentSeasonID;
                Instance.RaisePropertyChanged("ListSeasons");
                Instance.CurrentSeasonID = Basics.NoID;
                Instance.CurrentSeasonID = backupCurrentSeasonID;
            }
        }

        public static void UpdateListEvents()
        {
            if (Instance != null)
            {
                Instance.ListEvents = new ObservableCollection<Event>();
                foreach (Event _event in Event.Statics.List) { Instance.ListEvents.Add(_event); Instance.RaisePropertyChanged("ListEvents"); }
                Instance.SetCurrentEvent();
            }
        }

        public void SetCurrentEvent()
        {
            foreach (Event _event in Event.Statics.List) { CurrentEvent = _event; if (_event.EventDate > DateTime.Now) { return; } }
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
            PreSeason.UpdatePreQResults();
            PreSeason.CountCars(CurrentEvent.EventDate, DateRegisterLimit, CarLimitRegisterLimit, DateBoPFreeze, IsCheckedRegisterLimit, IsCheckedBoPFreeze);
            PreSeason.CalcBoP(CarLimitBallast, CarLimitRestriktor, GainBallast, GainRestriktor, IsCheckedBallast, IsCheckedRestriktor);
            GSheets.UpdateBoPStandings(GSheet.ListIDs[1].DocID, GSheet.ListIDs[1].SheetID);
            GSheets.UpdatePreQStandings(GSheet.ListIDs[0].DocID, GSheet.ListIDs[0].SheetID);
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
                if (WaitQueueEntries > 0) { StateEntries = ServerVM.StateRunWait; }
                else { StateEntries = ServerVM.StateRun; }
            }
            else
            {
                if (WaitQueueEntries > 0) { StateEntries = ServerVM.StateWait; }
                else { if (StateAutoUpdateEntries) { StateEntries = ServerVM.StateOn; } else { StateEntries = ServerVM.StateOff; } }
            }
        }

        public bool CheckExistingThreads()
        {
            if (IsRunningEntries) { return true; }
            foreach (Server _server in Server.List) { if (_server.IsRunning) { return true; } }
            return false;
        }

        public void ResetEntries()
        {
            GSheets.SyncFormsEntries(GSheet.ListIDs[3].DocID, GSheet.ListIDs[3].SheetID, "A:I");
            DatabaseVM.UpdateDatabase(true);
        }

        public void UpdateEntrylistBoP()
        {
            new Thread(() => ThreadUpdateEntrylistBoP(CurrentEvent.EventDate)).Start();
            MainVM.List[0].LogCurrentText = "Export entrylists and BoPs...";
        }

        public int ThreadUpdateEntrylistBoP(DateTime _eventDate)
        {
            PreSeason.UpdateName3Digits();
            PreSeason.EntryAutoSignOut(_eventDate, SignOutLimit, NoShowLimit);
            UpdateBoPForEvent(_eventDate);
            (List<Entry> EntriesSignedIn, List<Entry> EntriesSignedOut) = PreSeason.DetermineEntrylist(_eventDate, SlotsAvailable, DateRegisterLimit);
            int tempSlotsTaken = EntriesSignedIn.Count;
            (EntriesSignedIn, EntriesSignedOut) = PreSeason.FillUpEntrylist(_eventDate, SlotsAvailable, EntriesSignedIn, EntriesSignedOut);
            if (_eventDate == CurrentEvent.EventDate)
            {
                SlotsTaken = tempSlotsTaken;
                GSheets.UpdateBoPStandings(GSheet.ListIDs[1].DocID, GSheet.ListIDs[1].SheetID);
                MainVM.List[0].LogCurrentText = "Entrylist exported.";
                ServerVM.UpdateEntrylists();
                ServerVM.UpdateBoPs();
            }
            return tempSlotsTaken;
        }

        public void UpdateBoPForEvent(DateTime _eventDate)
        {
            PreSeason.CountCars(_eventDate, DateRegisterLimit, CarLimitRegisterLimit, DateBoPFreeze, IsCheckedRegisterLimit, IsCheckedBoPFreeze);
            PreSeason.CalcBoP(CarLimitBallast, CarLimitRestriktor, GainBallast, GainRestriktor, IsCheckedBallast, IsCheckedRestriktor);
            CarBoP.SortByCount();
        }

        public int CarChangeCount(int entryID, DateTime maxEventDate)
        {
            int carChangeCount = 0;
            Entry entry = Entry.Statics.GetByID(entryID);
            if (IsCheckedCarChangeLimit && entry.ScorePoints)
            {
                EventsEntries.SortByDate();
                List<EventsEntries> eventList = EventsEntries.Statics.GetBy("EntryID", entry.ID);
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
