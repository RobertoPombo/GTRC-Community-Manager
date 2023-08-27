using Core;
using Database;
using Enums;
using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Media3D;

namespace GTRC_Community_Manager
{
    public class SessionsVM : ObservableObject
    {
        public static SessionsVM? Instance;
        private static readonly string PathPreset = MainWindow.dataDirectory + "config session preset.json";
        private static ObservableCollection<Series> listSeries = new();
        private static ObservableCollection<Season> listSeasons = new();
        private static ObservableCollection<Event> listEvents = new();
        private static ObservableCollection<Session> listSessions = new();

        private Series? selSeries;
        private Season? selSeason;
        private Event? selEvent;
        private int chanceOfRain = 101;

        public SessionsVM()
        {
            Instance = this;
            SessionSaveSQLCmd = new UICmd((o) => SessionSaveSQL());
            SessionLoadSQLCmd = new UICmd((o) => SessionLoadSQL());
            PresetWriteJsonCmd = new UICmd((o) => PresetWriteJson());
            PresetReadJsonCmd = new UICmd((o) => PresetReadJson());
            PublishTrackReportCmd = new UICmd((o) => PublishTrackReport());
            if (SettingsVM.Instance?.ActiveDisBotPreset?.CurrentSeasonID > Basics.NoID) { SelSeason = Season.Statics.GetByID(SettingsVM.Instance.ActiveDisBotPreset.CurrentSeasonID); }
            SessionLoadSQL();
        }
        public ObservableCollection<Series> ListSeries { get { return listSeries; } set { listSeries = value; } }
        public ObservableCollection<Season> ListSeasons { get { return listSeasons; } set { listSeasons = value; } }
        public ObservableCollection<Event> ListEvents { get { return listEvents; } set { listEvents = value; } }
        public ObservableCollection<Session> ListSessions { get { return listSessions; } set { listSessions = value; } }

        public IEnumerable<DayOfWeekendEnum> ListDayOfWeekendEnums
        {
            get { return Enum.GetValues(typeof(DayOfWeekendEnum)).Cast<DayOfWeekendEnum>(); }
            set { }
        }

        public Series? SelSeries
        {
            get { return selSeries; }
            set
            {
                if (value?.ID != selSeries?.ID)
                {
                    selSeries = value;
                    UpdateListSeasons(SettingsVM.Instance?.ActiveDisBotPreset?.CurrentSeasonID ?? Basics.NoID);
                }
                RaisePropertyChanged();
            }
        }

        public Season? SelSeason
        {
            get { return selSeason; }
            set
            {
                if (value?.ID != selSeason?.ID)
                {
                    if (value is not null && value.SeriesID > Basics.NoID && value.SeriesID != selSeries?.ID) { SelSeries = Series.Statics.GetByID(value.SeriesID); }
                    selSeason = value;
                    UpdateListEvents(Event.GetNextEvent(selSeason?.ID ?? Basics.NoID, DateTime.Now).ID);
                }
                RaisePropertyChanged();
            }
        }

        public Event? SelEvent
        {
            get { return selEvent; }
            set
            {
                if (value is not null && value.SeasonID > Basics.NoID && value.SeasonID != selSeason?.ID) { SelSeason = Season.Statics.GetByID(value.SeasonID); }
                if (value?.ID != selEvent?.ID) { selEvent = value; UpdateListSessions(); }
                RaisePropertyChanged();
            }
        }

        public int ChanceOfRain { get { return chanceOfRain; } set { if (value >= 0 && value <= 100) { chanceOfRain = value; } } }

        public static void UpdateListSeries(int defID = -1)
        {
            if (defID == -1 && Instance is not null && Instance.selSeries is not null) { defID = Instance.selSeries.ID; }
            listSeries = new ObservableCollection<Series>();
            foreach (Series _series in Series.Statics.List) { listSeries.Add(_series); }
            Instance?.RaisePropertyChanged(nameof(ListSeries));
            Series defSeries = Series.Statics.GetByID(defID);
            if (Instance is not null && listSeries.Count > 0) { Instance.SelSeries = listSeries[0]; }
            if (Instance is not null) { Instance.SelSeries = defSeries; }
        }

        public static void UpdateListSeasons(int defID = -1)
        {
            if (defID == -1 && Instance is not null && Instance.selSeason is not null) { defID = Instance.selSeason.ID; }
            listSeasons = new ObservableCollection<Season>();
            foreach (Season _season in Season.Statics.List) { if (Instance?.selSeries?.ID == _season.SeriesID) { listSeasons.Add(_season); } }
            Instance?.RaisePropertyChanged(nameof(ListSeasons));
            Season defSeason = Season.Statics.GetByID(defID);
            if (Instance is not null && listSeasons.Count > 0) { Instance.SelSeason = listSeasons[0]; }
            if (Instance is not null && Instance.selSeries?.ID == defSeason.SeriesID) { Instance.SelSeason = defSeason; }
        }

        public static void UpdateListEvents(int defID = -1)
        {
            if (defID == -1 && Instance is not null && Instance.selEvent is not null) { defID = Instance.selEvent.ID; }
            listEvents = new ObservableCollection<Event>();
            foreach (Event _event in Event.Statics.List) { if (Instance?.selSeason?.ID == _event.SeasonID) { listEvents.Add(_event); } }
            Instance?.RaisePropertyChanged(nameof(ListEvents));
            Event defEvent = Event.Statics.GetByID(defID);
            if (Instance is not null && listEvents.Count > 0) { Instance.SelEvent = listEvents[0]; }
            if (Instance is not null && Instance.selSeason?.ID == defEvent.SeasonID) { Instance.SelEvent = defEvent; }
        }

        public static void UpdateListSessions()
        {
            listSessions = new ObservableCollection<Session>();
            if (Instance?.selEvent?.ID > Basics.NoID)
            {
                List<Session> _listSessions = Instance.selEvent.GetSessions();
                foreach (Session _session in _listSessions) { listSessions.Add(_session); }
            }
            Instance?.RaisePropertyChanged(nameof(ListSessions));
        }

        public void SessionSaveSQL()
        {
            if (SelEvent?.ID > Basics.NoID)
            {
                Event.Statics.WriteSQL(SelEvent);
                foreach (Session _session in listSessions) { Session.Statics.WriteSQL(_session); }
            }
        }

        public void SessionLoadSQL()
        {
            Session.Statics.LoadSQL();
            if (SelEvent?.ID > Basics.NoID)
            {
                SelEvent = Event.Statics.GetByIdSQL(SelEvent.ID); UpdateListSessions();
                for (int sessionNr = 0; sessionNr < listSessions.Count; sessionNr++) { listSessions[sessionNr] = Session.Statics.GetByIdSQL(listSessions[sessionNr].ID); }
                RaisePropertyChanged(nameof(ListSessions));
            }
        }

        public void PresetReadJson()
        {
            if (Instance?.SelEvent?.ID > Basics.NoID)
            {
                try
                {
                    List<Session> listPresets = JsonConvert.DeserializeObject<List<Session>>(File.ReadAllText(PathPreset, Encoding.Unicode)) ?? new();
                    Session.Statics.LoadSQL();
                    for (int presetNr = 0; presetNr < listPresets.Count; presetNr++)
                    {
                        Dictionary<PropertyInfo, dynamic> dict = listPresets[presetNr].ReturnAsDict(false, false, true, true);
                        if (presetNr < listSessions.Count)
                        {
                            foreach (KeyValuePair<PropertyInfo, dynamic> item in dict)
                            {
                                if (item.Key.Name != nameof(Session.EventID)) { item.Key.SetValue(listSessions[presetNr], Basics.CastValue(item.Key, item.Value)); }
                            }
                        }
                        else
                        {
                            Session newSession = new() { EventID = Instance.SelEvent.ID };
                            foreach (KeyValuePair<PropertyInfo, dynamic> item in dict)
                            {
                                if (item.Key.Name != nameof(Session.EventID)) { item.Key.SetValue(newSession, Basics.CastValue(item.Key, item.Value)); }
                            }
                            newSession.ListAdd();
                            UpdateListSessions();
                        }
                    }
                    for (int presetNr = listSessions.Count - 1; presetNr >= listPresets.Count; presetNr--)
                    {
                        listSessions[presetNr].ListRemove(); UpdateListSessions();
                    }
                }
                catch { MainVM.List[0].LogCurrentText = "Loading sessions preset failed!"; }
            }
            else { MainVM.List[0].LogCurrentText = "Loading sessions preset failed!"; }
        }

        public void PresetWriteJson()
        {
            string text = JsonConvert.SerializeObject(listSessions, Formatting.Indented);
            File.WriteAllText(PathPreset, text, Encoding.Unicode);
        }

        public void PublishTrackReport()
        {
            if (chanceOfRain >= 0 && chanceOfRain <= 100)
            {
                if (SelEvent?.ID > Basics.NoID) { _ = Commands.CreateTrackReportMessage(SelEvent.ID, ChanceOfRain); }
                chanceOfRain = 101;
            }
            else { chanceOfRain += 1; }
            RaisePropertyChanged(nameof(ChanceOfRain));
        }

        public UICmd SessionSaveSQLCmd { get; set; }
        public UICmd SessionLoadSQLCmd { get; set; }
        public UICmd PresetWriteJsonCmd { get; set; }
        public UICmd PresetReadJsonCmd { get; set; }
        [JsonIgnore] public UICmd PublishTrackReportCmd { get; set; }
    }
}
