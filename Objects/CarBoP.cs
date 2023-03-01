using System;
using System.Collections.Generic;
using System.Linq;

namespace GTRCLeagueManager.Database
{
    public class CarBoP
    {
        public static List<CarBoP> List = new List<CarBoP>();

        private Car car = new Car(false);
        private int count = 0;
        private int countbop = 0;
        private int ballast = 0;
        private int restrictor = 0;

        public Car Car
        {
            get { return car; }
            set { if (value != car) { if (Car.Statics.ExistsID(value.ID)) { car = value; } } }
        }

        public int Count
        {
            get { return count; }
            set { if (value >= 0) { count = value; } }
        }

        public int CountBoP
        {
            get { return countbop; }
            set { if (value >= 0) { countbop = value; } }
        }

        public int Ballast
        {
            get { return ballast; }
            set
            {
                if (value < 0) { ballast = 0; }
                else if (value > 30) { ballast = 30; }
                else { ballast = value; }
            }
        }

        public int Restrictor
        {
            get { return restrictor; }
            set
            {
                if (value < 0) { restrictor = 0; }
                else if (value > 20) { restrictor = 20; }
                else { restrictor = value; }
            }
        }



        public CarBoP() { }

        public static CarBoP GetCarByCarID(int _carID)
        {
            foreach (CarBoP _carBoP in List) { if (_carBoP.Car.AccCarID == _carID) { return _carBoP; } }
            return new CarBoP();
        }

        public static void SortByCount()
        {
            var linqList = from _carBoP in List
                           orderby _carBoP.CountBoP descending, _carBoP.Count descending, _carBoP.Car.Name
                           select _carBoP;
            List = linqList.Cast<CarBoP>().ToList();
        }
    }
}
