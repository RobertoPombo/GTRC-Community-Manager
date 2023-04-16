using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

using GTRC_Community_Manager;
using Enums;
using System.Windows.Controls.Primitives;

namespace Database
{
    public class Season : DatabaseObject<Season>
    {
        public static readonly string DefaultName = "Season #1";
        [NotMapped][JsonIgnore] public static StaticDbField<Season> Statics { get; set; }
        static Season()
        {
            Statics = new StaticDbField<Season>(true)
            {
                Table = "Seasons",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(Name) } },
                ToStringPropertiesNames = new List<string>() { nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Season() { This = this; Initialize(true, true); }
        public Season(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Season(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private Series objSeries = new(false);
        [JsonIgnore][NotMapped] public Series ObjSeries { get { return objSeries; } }

        private string name = DefaultName;
        private int seriesID = 0;
        private int gridSlotsLimit = 0;
        private int carLimitBallast = 0;
        private int gainBallast = 0;
        private int carLimitRestriktor = 0;
        private int gainRestriktor = 0;
        private int carLimitRegisterLimit = 0;
        private DateTime dateRegisterLimit = DateTime.Now;
        private DateTime dateBoPFreeze = DateTime.Now;
        private int noShowLimit = 0;
        private int signOutLimit = 0;
        private int carChangeLimit = 0;
        private DateTime dateCarChangeLimit = DateTime.Now;
        private bool unlimitedCarVersionChanges = false;
        private int formationLapType = 0;

        public string Name
        {
            get { return name; }
            set
            {
                name = Basics.RemoveSpaceStartEnd(value ?? name);
                if (name == null || name == "") { name = DefaultName; }
                if (ReadyForList) { SetNextAvailable(); }
            }
        }

        public int SeriesID
        {
            get { return seriesID; }
            set
            {
                if (Series.Statics.IDList.Count == 0) { objSeries = new Series() { ID = 1 }; }
                if (!Series.Statics.ExistsID(value)) { objSeries = Series.Statics.IDList[0]; seriesID = objSeries.ID; }
                else { seriesID = value; objSeries = Series.Statics.GetByID(seriesID); }
            }
        }

        public int GridSlotsLimit
        {
            get { return gridSlotsLimit; }
            set { if (value >= 0) { gridSlotsLimit = value; } }
        }

        public int CarLimitBallast
        {
            get { return carLimitBallast; }
            set { if (value >= 0) { carLimitBallast = value; } }
        }

        public int GainBallast
        {
            get { return gainBallast; }
            set { if (value >= 0) { gainBallast = value; } }
        }

        public int CarLimitRestriktor
        {
            get { return carLimitRestriktor; }
            set { if (value >= 0) { carLimitRestriktor = value; } }
        }

        public int GainRestriktor
        {
            get { return gainRestriktor; }
            set { if (value >= 0) { gainRestriktor = value; } }
        }

        public int CarLimitRegisterLimit
        {
            get { return carLimitRegisterLimit; }
            set { if (value >= 0) { carLimitRegisterLimit = value; } }
        }

        public DateTime DateRegisterLimit
        {
            get { return dateRegisterLimit; }
            set { if (value >= Event.DateTimeMinValue) { dateRegisterLimit = value; } }
        }

        public DateTime DateBoPFreeze
        {
            get { return dateBoPFreeze; }
            set { if (value >= Event.DateTimeMinValue) { dateBoPFreeze = value; } }
        }

        public int NoShowLimit
        {
            get { return noShowLimit; }
            set { if (value >= 0) { noShowLimit = value; } }
        }

        public int SignOutLimit
        {
            get { return signOutLimit; }
            set { if (value >= 0) { signOutLimit = value; } }
        }

        public int CarChangeLimit
        {
            get { return carChangeLimit; }
            set
            {
                if (value >= 0 && carChangeLimit != value)
                {
                    carChangeLimit = value;
                    if (carChangeLimit == int.MaxValue) { unlimitedCarVersionChanges = true; }
                }
            }
        }

        public DateTime DateCarChangeLimit
        {
            get { return dateCarChangeLimit; }
            set
            {
                if (value >= Event.DateTimeMinValue && dateCarChangeLimit != value)
                {
                    dateCarChangeLimit = value;
                    if (dateCarChangeLimit >= Event.DateTimeMaxValue) { unlimitedCarVersionChanges = true; }
                }
            }
        }

        public bool UnlimitedCarVersionChanges
        {
            get { return unlimitedCarVersionChanges; }
            set
            {
                if (unlimitedCarVersionChanges != value)
                {
                    unlimitedCarVersionChanges = value;
                    if (!unlimitedCarVersionChanges)
                    {
                        if (carChangeLimit == int.MaxValue) { carChangeLimit = 0; }
                        if (dateCarChangeLimit >= Event.DateTimeMaxValue) { dateCarChangeLimit = DateTime.Now; }
                    }
                }
            }
        }

        public int FormationLapType
        {
            get { return formationLapType; }
            set { if (Enum.IsDefined(typeof(FormationLapTypeEnum), value)) { formationLapType = value; } }
        }

        [JsonIgnore] public FormationLapTypeEnum FormationLapTypeEnum
        {
            get { return (FormationLapTypeEnum)formationLapType; }
            set { formationLapType = (int)value; }
        }

        public static void PublishList()
        {
            PreSeasonVM.UpdateListSeasons();
        }

        public override void SetNextAvailable()
        {
            int nr = 1;
            string defName = name;
            if (Basics.SubStr(defName, -3, 2) == " #") { defName = Basics.SubStr(defName, 0, defName.Length - 3); }
            while (!IsUnique())
            {
                name = defName + " #" + nr.ToString();
                nr++; if (nr == int.MaxValue) { break; }
            }
        }
    }
}
