﻿using Core;
using Database;
using Enums;
using Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTRC_Community_Manager
{
    public class SeasonM : ObservableObject
    {
        public SeasonM(Season _season) { Season = _season; }

        private Season season = new(false);
        private int gridSlotsLimit = 0;
        private int carLimitBallast = 1;
        private int gainBallast = 1;
        private int carLimitRestrictor = 1;
        private int gainRestrictor = 1;
        private int carLimitRegisterLimit = 0;
        private DateTime dateRegisterLimit = DateTime.Now;
        private DateTime dateBoPFreeze = DateTime.Now;
        private int noShowLimit = 0;
        private int signOutLimit = 0;
        private int carChangeLimit = 0;
        private DateTime dateCarChangeLimit = DateTime.Now;
        private int daysIgnoreCarLimits = 1;
        private TimeTypeEnum timeTypeEnum = TimeTypeEnum.Days;
        private int timeIgnoreCarLimits = -1;

        public Season Season
        {
            get { return season; }
            set { season = value; RaisePropertyChanged(); }
        }

        public string Name { get { return season.Name; } }

        public bool IsCheckedGridSlotsLimit
        {
            get { return season.GridSlotsLimit < int.MaxValue; }
            set { if (value) { GridSlotsLimit = gridSlotsLimit; } else { GridSlotsLimit = int.MaxValue; } RaisePropertyChanged(); }
        }

        public int GridSlotsLimit
        {
            get { if (IsCheckedGridSlotsLimit) { return season.GridSlotsLimit; } else { return gridSlotsLimit; } }
            set
            {
                if (season.GridSlotsLimit != value)
                {
                    season.GridSlotsLimit = value;
                    if (value < int.MaxValue) { gridSlotsLimit = value; }
                    RaisePropertyChanged();
                    if (PreSeasonVM.Instance is not null) { PreSeasonVM.Instance.SlotsAvailable++; }
                }
            }
        }

        public bool IsCheckedBallast
        {
            get { return season.CarLimitBallast < int.MaxValue && season.GainBallast > 0; }
            set
            {
                if (value) { CarLimitBallast = carLimitBallast; GainBallast = gainBallast; }
                else { CarLimitBallast = int.MaxValue; GainBallast = 0; }
                RaisePropertyChanged();
            }
        }

        public int CarLimitBallast
        {
            get { if (IsCheckedBallast) { return season.CarLimitBallast; } else { return carLimitBallast; } }
            set
            {
                if (season.CarLimitBallast != value)
                {
                    season.CarLimitBallast = value;
                    if (value < int.MaxValue) { carLimitBallast = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public int GainBallast
        {
            get { if (IsCheckedBallast) { return season.GainBallast; } else { return gainBallast; } }
            set
            {
                if (season.GainBallast != value)
                {
                    season.GainBallast = value;
                    if (value > 0) { gainBallast = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedRestrictor
        {
            get { return season.CarLimitRestrictor < int.MaxValue && season.GainRestrictor > 0; }
            set
            {
                if (value) { CarLimitRestrictor = carLimitRestrictor; GainRestrictor = gainRestrictor; }
                else { CarLimitRestrictor = int.MaxValue; GainRestrictor = 0; }
                RaisePropertyChanged();
            }
        }

        public int CarLimitRestrictor
        {
            get { if (IsCheckedRestrictor) { return season.CarLimitRestrictor; } else { return carLimitRestrictor; } }
            set
            {
                if (season.CarLimitRestrictor != value)
                {
                    season.CarLimitRestrictor = value;
                    if (value < int.MaxValue) { carLimitRestrictor = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public int GainRestrictor
        {
            get { if (IsCheckedRestrictor) { return season.GainRestrictor; } else { return gainRestrictor; } }
            set
            {
                if (season.GainRestrictor != value)
                {
                    season.GainRestrictor = value;
                    if (value > 0) { gainRestrictor = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedRegisterLimit
        {
            get { return season.CarLimitRegisterLimit < int.MaxValue && season.DateRegisterLimit < Basics.DateTimeMaxValue; }
            set
            {
                if (value) { CarLimitRegisterLimit = carLimitRegisterLimit; DateRegisterLimit = dateRegisterLimit; } 
                else { CarLimitRegisterLimit = int.MaxValue; DateRegisterLimit = Basics.DateTimeMaxValue; }
                RaisePropertyChanged();
            }
        }

        public int CarLimitRegisterLimit
        {
            get { if (IsCheckedRegisterLimit) { return season.CarLimitRegisterLimit; } else { return carLimitRegisterLimit; } }
            set
            {
                if (season.CarLimitRegisterLimit != value)
                {
                    season.CarLimitRegisterLimit = value;
                    if (value < int.MaxValue) { carLimitRegisterLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public DateTime DateRegisterLimit
        {
            get { if (IsCheckedRegisterLimit) { return season.DateRegisterLimit; } else { return dateRegisterLimit; } }
            set
            {
                if (season.DateRegisterLimit != value)
                {
                    season.DateRegisterLimit = value;
                    if (value < Basics.DateTimeMaxValue) { dateRegisterLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedBoPFreeze
        {
            get { return season.DateBoPFreeze < Basics.DateTimeMaxValue; }
            set
            {
                if (value) { DateBoPFreeze = dateBoPFreeze; }
                else { DateBoPFreeze = Basics.DateTimeMaxValue; }
                RaisePropertyChanged();
            }
        }

        public DateTime DateBoPFreeze
        {
            get { if (IsCheckedBoPFreeze) { return season.DateBoPFreeze; } else { return dateBoPFreeze; } }
            set
            {
                if (season.DateBoPFreeze != value)
                {
                    season.DateBoPFreeze = value;
                    if (value < Basics.DateTimeMaxValue) { dateBoPFreeze = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedSignOutLimit
        {
            get { return season.SignOutLimit < int.MaxValue; }
            set
            {
                if (value) { SignOutLimit = signOutLimit; }
                else { SignOutLimit = int.MaxValue; }
                RaisePropertyChanged();
            }
        }

        public int SignOutLimit
        {
            get { if (IsCheckedSignOutLimit) { return season.SignOutLimit; } else { return signOutLimit; } }
            set
            {
                if (season.SignOutLimit != value)
                {
                    season.SignOutLimit = value;
                    if (value < int.MaxValue) { signOutLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedNoShowLimit
        {
            get { return season.NoShowLimit < int.MaxValue; }
            set
            {
                if (value) { NoShowLimit = noShowLimit; }
                else { NoShowLimit = int.MaxValue; }
                RaisePropertyChanged();
            }
        }

        public int NoShowLimit
        {
            get { if (IsCheckedNoShowLimit) { return season.NoShowLimit; } else { return noShowLimit; } }
            set
            {
                if (season.NoShowLimit != value)
                {
                    season.NoShowLimit = value;
                    if (value < int.MaxValue) { noShowLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedCarChangeLimit
        {
            get { return season.CarChangeLimit < int.MaxValue && season.DateCarChangeLimit < Basics.DateTimeMaxValue; }
            set
            {
                if (value) { CarChangeLimit = carChangeLimit; DateCarChangeLimit = dateCarChangeLimit; }
                else { CarChangeLimit = int.MaxValue; DateCarChangeLimit = Basics.DateTimeMaxValue; }
                RaisePropertyChanged();
            }
        }

        public int CarChangeLimit
        {
            get { if (IsCheckedCarChangeLimit) { return season.CarChangeLimit; } else { return carChangeLimit; } }
            set
            {
                if (season.CarChangeLimit != value)
                {
                    season.CarChangeLimit = value;
                    if (value < int.MaxValue) { carChangeLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public DateTime DateCarChangeLimit
        {
            get { if (IsCheckedCarChangeLimit) { return season.DateCarChangeLimit; } else { return dateCarChangeLimit; } }
            set
            {
                if (season.DateCarChangeLimit != value)
                {
                    season.DateCarChangeLimit = value;
                    if (value < Basics.DateTimeMaxValue) { dateCarChangeLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedGroupCarLimits
        {
            get { return season.GroupCarLimits; }
            set { if (season.GroupCarLimits != value) { season.GroupCarLimits = value; RaisePropertyChanged(); } }
        }

        public bool IsCheckedBopLatestModelOnly
        {
            get { return season.BopLatestModelOnly; }
            set { if (season.BopLatestModelOnly != value) { season.BopLatestModelOnly = value; RaisePropertyChanged(); } }
        }

        public bool IsCheckedIgnoreCarLimits
        {
            get { return season.DaysIgnoreCarLimits > 0; }
            set
            {
                if (value) { DaysIgnoreCarLimits = daysIgnoreCarLimits; }
                else { DaysIgnoreCarLimits = 0; }
                RaisePropertyChanged();
            }
        }

        public IEnumerable<TimeTypeEnum> ListTimeTypeEnums
        {
            get
            {
                IEnumerable<TimeTypeEnum> list = new List<TimeTypeEnum>();
                foreach (var timeType in Enum.GetValues<TimeTypeEnum>()) { if (timeType >= TimeTypeEnum.Days) { list = list.Append(timeType); } }
                return list;
            }
            set { }
        }

        public int DaysIgnoreCarLimits
        {
            get { if (IsCheckedIgnoreCarLimits) { return season.DaysIgnoreCarLimits; } else { return daysIgnoreCarLimits; } }
            set
            {
                if (season.DaysIgnoreCarLimits != value)
                {
                    season.DaysIgnoreCarLimits = value;
                    if (value > 0) { daysIgnoreCarLimits = value; }
                }
                if (DaysIgnoreCarLimits == 0) { timeTypeEnum = TimeTypeEnum.Days; timeIgnoreCarLimits = DaysIgnoreCarLimits; }
                else if (Math.Round((float)DaysIgnoreCarLimits / 365, 0) == (float)DaysIgnoreCarLimits / 365)
                {
                    timeTypeEnum = TimeTypeEnum.Years;
                    timeIgnoreCarLimits = (int)Math.Round((float)DaysIgnoreCarLimits / 365, 0);
                }
                else if (Math.Round((float)DaysIgnoreCarLimits / 31, 0) == (float)DaysIgnoreCarLimits / 31)
                {
                    timeTypeEnum = TimeTypeEnum.Months;
                    timeIgnoreCarLimits = (int)Math.Round((float)DaysIgnoreCarLimits / 31, 0);
                }
                else if (Math.Round((float)DaysIgnoreCarLimits / 7, 0) == (float)DaysIgnoreCarLimits / 7)
                {
                    timeTypeEnum = TimeTypeEnum.Weeks;
                    timeIgnoreCarLimits = (int)Math.Round((float)DaysIgnoreCarLimits / 7, 0);
                }
                else { timeTypeEnum = TimeTypeEnum.Days; timeIgnoreCarLimits = DaysIgnoreCarLimits; }
                RaisePropertyChanged(nameof(TimeTypeEnum));
                RaisePropertyChanged(nameof(TimeIgnoreCarLimits));
            }
        }

        public TimeTypeEnum TimeTypeEnum
        {
            get { return timeTypeEnum; }
            set
            {
                timeTypeEnum = value;
                if (value == TimeTypeEnum.Days) { DaysIgnoreCarLimits = TimeIgnoreCarLimits; }
                else if (value == TimeTypeEnum.Weeks) { DaysIgnoreCarLimits = 7 * TimeIgnoreCarLimits; }
                else if (value == TimeTypeEnum.Months) { DaysIgnoreCarLimits = 31 * TimeIgnoreCarLimits; }
                else if (value == TimeTypeEnum.Years) { DaysIgnoreCarLimits = 365 * TimeIgnoreCarLimits; }
                RaisePropertyChanged();
            }
        }

        public int TimeIgnoreCarLimits
        {
            get { if (timeIgnoreCarLimits == -1) { DaysIgnoreCarLimits = season.DaysIgnoreCarLimits; } return timeIgnoreCarLimits; }
            set
            {
                timeIgnoreCarLimits = value;
                if (TimeTypeEnum == TimeTypeEnum.Days) { DaysIgnoreCarLimits = value; }
                else if (TimeTypeEnum == TimeTypeEnum.Weeks) { DaysIgnoreCarLimits = value * 7; }
                else if (TimeTypeEnum == TimeTypeEnum.Months) { DaysIgnoreCarLimits = value * 31; }
                else if (TimeTypeEnum == TimeTypeEnum.Years) { DaysIgnoreCarLimits = value * 365; }
                RaisePropertyChanged();
            }
        }

        public string ExplanationStintAnalisisSettings { get { return ExplainStintAnalisisSettings(); } set { } }

        public string ExplainStintAnalisisSettings()
        {
            string explanation = "From a stint";
            return explanation;
        }
    }
}
