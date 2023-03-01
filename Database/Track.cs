using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace GTRCLeagueManager.Database
{
    public class Track : DatabaseObject<Track>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<Track> Statics { get; set; }
        public static readonly string DefaultAccTrackID = "TrackID";

        private string accTrackID = DefaultAccTrackID;
        private string name = "";
        private int pitBoxesCount = 0;
        private int serverSlotsCount = 0;
        private string name_GTRC = "";

        static Track()
        {
            Statics = new StaticDbField<Track>(true)
            {
                Table = "Tracks",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { "AccTrackID" } },
                ToStringPropertiesNames = new List<string>() { "Name" },
                ListSetter = () => ListSetter()
            };
        }

        public Track() { This = this; Initialize(true, true); }
        public Track(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Track(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        public string AccTrackID
        {
            get { return accTrackID; }
            set
            {
                accTrackID = Basics.RemoveSpaceStartEnd(value ?? accTrackID);
                if (accTrackID == null || accTrackID == "") { accTrackID = DefaultAccTrackID; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public string Name
        {
            get { return name; }
            set { name = Basics.RemoveSpaceStartEnd(value ?? name); }
        }
        
        public int PitBoxesCount
        {
            get { return pitBoxesCount; }
            set { if (value >= 0) { pitBoxesCount = value; } }
        }

        public int ServerSlotsCount
        {
            get { return serverSlotsCount; }
            set { if (value >= 0) { serverSlotsCount = value; } }
        }

        public string Name_GTRC
        {
            get { return name_GTRC; }
            set { name_GTRC = Basics.RemoveSpaceStartEnd(value ?? name_GTRC); }
        }

        public static void ListSetter() { }

        public override void SetNextAvailable()
        {
            int nr = 1;
            string defID = accTrackID;
            if (Basics.SubStr(defID, -2, 1) == "_") { defID = Basics.SubStr(defID, 0, defID.Length - 2); }
            while (!IsUnique())
            {
                accTrackID = defID + "_" + nr.ToString();
                nr++; if (nr == int.MaxValue) { break; }
            }
        }



        //später löschen
        public static List<string> ReturnPropsAsList()
        {
            List<string> list = new List<string>();
            foreach (PropertyInfo property in typeof(Track).GetProperties()) { list.Add(property.Name); }
            return list;
        }

        public Dictionary<string, dynamic> ReturnAsDict()
        {
            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
            foreach (PropertyInfo property in typeof(Track).GetProperties()) { dict[property.Name] = property.GetValue(this); }
            return dict;
        }
    }
}
