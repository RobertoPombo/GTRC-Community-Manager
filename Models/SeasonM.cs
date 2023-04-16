using Core;
using Database;
using System;

namespace GTRC_Community_Manager
{
    public class SeasonM : ObservableObject
    {
        public SeasonM(Season _season) { Season = _season; }

        private Season season = new(false);
        private int gridSlotsLimit = 0;
        private int carLimitBallast = 1;
        private int gainBallast = 1;
        private int carLimitRestriktor = 1;
        private int gainRestriktor = 1;
        private int carLimitRegisterLimit = 0;
        private DateTime dateRegisterLimit = DateTime.Now;
        private DateTime dateBoPFreeze = DateTime.Now;
        private int noShowLimit = 0;
        private int signOutLimit = 0;
        private int carChangeLimit = 0;
        private DateTime dateCarChangeLimit = DateTime.Now;

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

        public bool IsCheckedRestriktor
        {
            get { return season.CarLimitRestriktor < int.MaxValue && season.GainRestriktor > 0; }
            set
            {
                if (value) { CarLimitRestriktor = carLimitRestriktor; GainRestriktor = gainRestriktor; }
                else { CarLimitRestriktor = int.MaxValue; GainRestriktor = 0; }
                RaisePropertyChanged();
            }
        }

        public int CarLimitRestriktor
        {
            get { if (IsCheckedRestriktor) { return season.CarLimitRestriktor; } else { return carLimitRestriktor; } }
            set
            {
                if (season.CarLimitRestriktor != value)
                {
                    season.CarLimitRestriktor = value;
                    if (value < int.MaxValue) { carLimitRestriktor = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public int GainRestriktor
        {
            get { if (IsCheckedRestriktor) { return season.GainRestriktor; } else { return gainRestriktor; } }
            set
            {
                if (season.GainRestriktor != value)
                {
                    season.GainRestriktor = value;
                    if (value > 0) { gainRestriktor = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedRegisterLimit
        {
            get { return season.CarLimitRegisterLimit < int.MaxValue && season.DateRegisterLimit < Event.DateTimeMaxValue; }
            set
            {
                if (value) { CarLimitRegisterLimit = carLimitRegisterLimit; DateRegisterLimit = dateRegisterLimit; } 
                else { CarLimitRegisterLimit = int.MaxValue; DateRegisterLimit = Event.DateTimeMaxValue; }
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
                    if (value < Event.DateTimeMaxValue) { dateRegisterLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedBoPFreeze
        {
            get { return season.DateBoPFreeze < Event.DateTimeMaxValue; }
            set
            {
                if (value) { DateBoPFreeze = dateBoPFreeze; }
                else { DateBoPFreeze = Event.DateTimeMaxValue; }
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
                    if (value < Event.DateTimeMaxValue) { dateBoPFreeze = value; }
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
            get { return season.CarChangeLimit < int.MaxValue && season.DateCarChangeLimit < Event.DateTimeMaxValue; }
            set
            {
                if (value) { CarChangeLimit = carChangeLimit; DateCarChangeLimit = dateCarChangeLimit; }
                else { CarChangeLimit = int.MaxValue; DateCarChangeLimit = Event.DateTimeMaxValue; }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsCheckedUnlimitedCarVersionChanges));
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
                    if (value < Event.DateTimeMaxValue) { dateCarChangeLimit = value; }
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCheckedUnlimitedCarVersionChanges
        {
            get { return season.UnlimitedCarVersionChanges; }
            set
            {
                if (season.UnlimitedCarVersionChanges != value)
                {
                    season.UnlimitedCarVersionChanges = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsCheckedCarChangeLimit));
                    RaisePropertyChanged(nameof(CarChangeLimit));
                    RaisePropertyChanged(nameof(DateCarChangeLimit));
                }
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
