using Enums;
using Newtonsoft.Json;
using Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Database
{
    public class Incident : DatabaseObject<Incident>
    {
        public static readonly List<string> Sessions = new() { "Qualifying", "Race 1", "Race 2" };
        public static readonly List<int> TimePenalties = new() { 0, 5, 10, 15, 20, 30, 40, 50, 60 };
        [NotMapped][JsonIgnore] public static StaticDbField<Incident> Statics { get; set; }
        static Incident()
        {
            Statics = new StaticDbField<Incident>(true)
            {
                Table = "Incidents",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(EventID), nameof(SessionID), nameof(RaceNumbers), nameof(TimeStamp) } },
                ToStringPropertiesNames = new List<string>() { nameof(EventID), nameof(SessionID), nameof(RaceNumbers), nameof(TimeStamp) },
                PublishList = () => PublishList()
            };
        }
        public Incident() { This = this; Initialize(true, true); }
        public Incident(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Incident(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Event objEvent = new(false);
        [JsonIgnore][NotMapped] public Event ObjEvent { get { return objEvent; } }

        private int eventID = 0;
        private int sessionID = -1;
        private string raceNumbers = "";
        private int timeStamp = 0;
        private int reportReason = 0;
        private int replayTime = Basics.NoID;
        private int eloRatingPenalty = 0;
        private int safetyRatingPenalty = 0;
        private bool warning = false;
        private int timePenalty = 0;
        private int timeLoss = 0;
        private int status = 0;
        private bool applied = false;

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); } objEvent = Event.Statics.GetByID(EventID); }
        }

        public int SessionID
        {
            get { return sessionID; }
            set { sessionID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public string RaceNumbers
        {
            get { return raceNumbers; }
            set { raceNumbers = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        [JsonIgnore][NotMapped] public List<int> RaceNumbersInt
        {
            get
            {
                List<int> raceNumbersInt = new();
                string[] _raceNumbersStr = raceNumbers.Split(' ', '\n', '-', '_', '.', ',', ':', ';', '/', '&', '|');
                foreach (string _raceNumberStr in _raceNumbersStr)
                {
                    if (int.TryParse(_raceNumberStr.Where(Char.IsNumber).ToArray(), out int _raceNumberInt))
                    {
                        if (!raceNumbersInt.Contains(_raceNumberInt)) { raceNumbersInt.Add(_raceNumberInt); }
                    }
                }
                return raceNumbersInt;
            }
        }

        public int TimeStamp
        {
            get { return timeStamp; }
            set
            {
                if (value < 0) { timeStamp = 0; }
                else { timeStamp = value; }
                if (ReplayTime == Basics.NoID) { ReplayTime = timeStamp; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        [JsonIgnore][NotMapped] public string TimeStampStr
        {
            get { return Basics.Ms2String(TimeStamp, "mm:ss"); }
        }

        public int ReportReason
        {
            get { return reportReason; }
            set { if (Enum.IsDefined(typeof(ReportReasonEnum), value)) { reportReason = value; } }
        }

        public int ReplayTime
        {
            get { return replayTime; }
            set
            {
                if (value < 0) { replayTime = 0; }
                else { replayTime = value; }
            }
        }

        [JsonIgnore] public ReportReasonEnum ReportReasonEnum
        {
            get { return (ReportReasonEnum)reportReason; }
            set { reportReason = (int)value; }
        }

        public int EloRatingPenalty
        {
            get { return eloRatingPenalty; }
            set { eloRatingPenalty = value; }
        }

        public int SafetyRatingPenalty
        {
            get { return safetyRatingPenalty; }
            set { if (value < 0) { value = 0; } safetyRatingPenalty = value; }
        }

        public bool Warning
        {
            get { return warning; }
            set { warning = value; if (warning) { TimePenalty = 0; } }
        }

        public int TimePenalty
        {
            get { return timePenalty; }
            set { timePenalty = value; if (timePenalty > 0) { Warning = false; } }
        }

        [JsonIgnore] public int RCTimePenalty
        {
            get
            {
                int rcTimePenalty = Math.Max(0, TimePenalty - TimeLoss);
                foreach (int _timePenalty in TimePenalties)
                {
                    if (rcTimePenalty <= _timePenalty) { rcTimePenalty = _timePenalty; break; }
                }
                int _maxTimePenalty = TimePenalties[^1];
                if (rcTimePenalty >= _maxTimePenalty) { rcTimePenalty = _maxTimePenalty; }
                return rcTimePenalty;
            }
        }

        public int TimeLoss
        {
            get { return timeLoss; }
            set { timeLoss = value; }
        }

        public int Status
        {
            get { return status; }
            set { if (Enum.IsDefined(typeof(IncidentsStatusEnum), value)) { status = value; if ((IncidentsStatusEnum)status != IncidentsStatusEnum.DoneLive) { Applied = false; } } }
        }

        [JsonIgnore] public IncidentsStatusEnum StatusEnum
        {
            get { return (IncidentsStatusEnum)status; }
            set { status = (int)value; if ((IncidentsStatusEnum)status != IncidentsStatusEnum.DoneLive) { Applied = false; } }
        }

        public bool Applied
        {
            get { return applied; }
            set { if (StatusEnum != IncidentsStatusEnum.DoneLive && value) { applied = false; } else { applied = value; } }
        }

        public static void PublishList()
        {
            //xxx.UpdateListIncidents();
        }

        public override void SetNextAvailable()
        {
            int startValueReplayTime = timeStamp;

            if (sessionID < 0) { sessionID = 0; }
            else if (sessionID >= Sessions.Count) { sessionID = Sessions.Count - 1; }

            List<Event> _idListEvent = Event.Statics.IDList;
            if (_idListEvent.Count == 0) { _ = new Event() { ID = 1 }; _idListEvent = Event.Statics.IDList; }
            Event _event = Event.Statics.GetByID(eventID);
            if (!_event.ReadyForList) { eventID = _idListEvent[0].ID; }

            while (!IsUnique())
            {
                if (timeStamp + 1 < int.MaxValue) { timeStamp += 1; } else { timeStamp = 0; }
                if (timeStamp == startValueReplayTime) { break; }
            }

            objEvent = Event.Statics.GetByID(EventID);
        }
    }
}
