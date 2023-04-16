using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Scripts;

namespace Database
{
    public class IncidentsEntries : DatabaseObject<IncidentsEntries>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<IncidentsEntries> Statics { get; set; }
        static IncidentsEntries()
        {
            Statics = new StaticDbField<IncidentsEntries>(true)
            {
                Table = "IncidentsEntries",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(IncidentID), nameof(EntryID) } },
                ToStringPropertiesNames = new List<string>() { nameof(IncidentID), nameof(EntryID) },
                PublishList = () => PublishList()
            };
        }
        public IncidentsEntries() { This = this; Initialize(true, true); }
        public IncidentsEntries(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public IncidentsEntries(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Incident objIncident = new(false);
        private Entry objEntry = new(false);
        [JsonIgnore][NotMapped] public Incident ObjIncident { get { return objIncident; } }
        [JsonIgnore][NotMapped] public Entry ObjEntry { get { return objEntry; } }

        private int incidentID = 0;
        private int entryID = 0;
        private int driverID = Basics.ID0;
        private bool isAtFault = false;

        public int IncidentID
        {
            get { return incidentID; }
            set { incidentID = value; if (ReadyForList) { SetNextAvailable(); } objIncident = Incident.Statics.GetByID(incidentID); }
        }

        public int EntryID
        {
            get { return entryID; }
            set { entryID = value; if (ReadyForList) { SetNextAvailable(); } objEntry = Entry.Statics.GetByID(entryID); }
        }

        public int DriverID
        {
            get { return driverID; }
            set
            {
                if (Driver.Statics.IDList.Count == 0) { _ = new Driver() { ID = 1 }; }
                if (!Driver.Statics.ExistsID(value)) { value = Driver.Statics.IDList[0].ID; }
                driverID = value;
            }
        }

        public bool IsAtFault
        {
            get { return isAtFault; }
            set
            {
                if (value)
                {
                    List<IncidentsEntries> incidentsEntries = Statics.GetBy(nameof(IncidentID), incidentID);
                    foreach (IncidentsEntries incidentEntry in incidentsEntries) { incidentEntry.IsAtFault = false; }
                }
                isAtFault = value;
            }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int incidentNr = 0;
            List<Incident> _idListIncident = Incident.Statics.IDList;
            if (_idListIncident.Count == 0) { _ = new Incident() { ID = 1 }; _idListIncident = Incident.Statics.IDList; }
            Incident _incident = Incident.Statics.GetByID(incidentID);
            if (_incident.ReadyForList) { incidentNr = Incident.Statics.IDList.IndexOf(_incident); }
            else { _incident = _idListIncident[incidentNr]; incidentID = _idListIncident[0].ID; }
            int seasonID = Event.Statics.GetByID(_incident.EventID).SeasonID;

            var linqListEntry = from _lingEntry in Entry.Statics.List
                                where _lingEntry.SeasonID == seasonID && _lingEntry.ID != Basics.NoID
                                select _lingEntry;
            List<Entry> _idListEntry = linqListEntry.Cast<Entry>().ToList();

            if (_idListEntry.Count == 0) { Entry _newEntry = new() { ID = 1, SeasonID = seasonID }; _idListEntry.Add(_newEntry); }
            Entry _entry = Entry.Statics.GetByID(entryID);
            int entryNr = 0;
            if (_entry.ReadyForList && _idListEntry.Contains(_entry)) { entryNr = _idListEntry.IndexOf(_entry); } else { _entry = _idListEntry[entryNr]; entryID = _entry.ID; }
            int startValueEntry = entryNr;

            while (!IsUnique())
            {
                if (entryNr + 1 < _idListEntry.Count) { entryNr += 1; } else { entryNr = 0; }
                entryID = _idListEntry[entryNr].ID;
                if (entryNr == startValueEntry) { break; }
            }

            objIncident = Incident.Statics.GetByID(incidentID);
            objEntry = Entry.Statics.GetByID(entryID);
        }
    }
}