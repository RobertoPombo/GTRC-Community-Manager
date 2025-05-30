﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

namespace Database
{
    public class Track : DatabaseObject<Track>
    {
        public static readonly string DefaultAccTrackID = "TrackID";
        [NotMapped][JsonIgnore] public static StaticDbField<Track> Statics { get; set; }
        static Track()
        {
            Statics = new StaticDbField<Track>(true)
            {
                Table = "Tracks",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(AccTrackID) } },
                ToStringPropertiesNames = new List<string>() { nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Track() { This = this; Initialize(true, true); }
        public Track(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Track(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private string accTrackID = DefaultAccTrackID;
        private string name = "";
        private int pitBoxesCount = 0;
        private int serverSlotsCount = 0;
        private int accTimePenDT = 30;
        private string name_GTRC = "";

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

        public int AccTimePenDT
        {
            get { return accTimePenDT; }
            set { if (value >= 0) { accTimePenDT = value; } }
        }

        public string Name_GTRC
        {
            get { return name_GTRC; }
            set { name_GTRC = Basics.RemoveSpaceStartEnd(value ?? name_GTRC); }
        }

        public static void PublishList() { }

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
    }
}
