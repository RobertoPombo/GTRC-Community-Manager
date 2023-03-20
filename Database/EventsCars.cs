using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GTRCLeagueManager.Database
{
    public class EventsCars : DatabaseObject<EventsCars>
    {
        [NotMapped][JsonIgnore] public static StaticDbField<EventsCars> Statics { get; set; }
        static EventsCars()
        {
            Statics = new StaticDbField<EventsCars>(true)
            {
                Table = "EventsCars",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(CarID), nameof(EventID) } },
                ToStringPropertiesNames = new List<string>() { nameof(CarID), nameof(EventID) },
                PublishList = () => PublishList()
            };
        }
        public EventsCars() { This = this; Initialize(true, true); }
        public EventsCars(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public EventsCars(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private int carID = 0;
        private int eventID = 0;
        private int count = 0;
        private int countbop = 0;
        private int ballast = 0;
        private int restrictor = 0;

        public int CarID
        {
            get { return carID; }
            set { carID = value; if (ReadyForList) { SetNextAvailable(); } }
        }

        public int EventID
        {
            get { return eventID; }
            set { eventID = value; if (ReadyForList) { SetNextAvailable(); } }
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

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int eventNr = 0;
            List<Event> _idListEvent = Event.Statics.IDList;
            if (_idListEvent.Count == 0) { _ = new Event() { ID = 1 }; _idListEvent = Event.Statics.IDList; }
            Event _event = Event.Statics.GetByID(eventID);
            if (_event.ReadyForList) { eventNr = Event.Statics.IDList.IndexOf(_event); } else { eventID = _idListEvent[0].ID; }
            int startValueEvent = eventNr;

            int carNr = 0;
            List<Car> _idListCar = Car.Statics.IDList;
            if (_idListCar.Count == 0) { _ = new Car() { ID = 1 }; _idListCar = Car.Statics.IDList; }
            Car _car = Car.Statics.GetByID(carID);
            if (_car.ReadyForList) { carNr = Car.Statics.IDList.IndexOf(_car); } else { carID = _idListCar[0].ID; }
            int startValueCar = carNr;

            while (!IsUnique())
            {
                if (eventNr + 1 < _idListEvent.Count) { eventNr += 1; } else { eventNr = 0; }
                eventID = _idListEvent[eventNr].ID;
                if (eventNr == startValueEvent)
                {
                    if (carNr + 1 < _idListCar.Count) { carNr += 1; } else { carNr = 0; }
                    carID = _idListCar[carNr].ID;
                    if (carNr == startValueCar) { break; }
                }
            }
        }

        public static EventsCars GetAnyByUniqProp(int _carID, int _eventID)
        {
            EventsCars eventCar = Statics.GetByUniqProp(new List<dynamic>() { _carID, _eventID });
            if (!eventCar.ReadyForList && _carID != Basics.NoID && _eventID != Basics.NoID)
            {
                eventCar.CarID = _carID;
                eventCar.EventID = _eventID;
                eventCar.ListAdd();
            }
            return eventCar;
        }

        public static List<EventsCars> GetAnyBy(string propName, int id, int seasonID = 0)
        {
            List<EventsCars> eventsCars = new();
            if (propName == nameof(CarID) && id != Basics.NoID && seasonID != Basics.NoID)
            {
                List<Event> listEvents = Event.Statics.GetBy(nameof(Event.SeasonID), seasonID);
                foreach (Event _event in listEvents)
                {
                    EventsCars eventCar = GetAnyByUniqProp(id, _event.ID);
                    if (eventCar.ReadyForList) { eventsCars.Add(eventCar); }
                }
            }
            else if (propName == nameof(EventID) && id != Basics.NoID)
            {
                foreach (Car _car in Car.Statics.List)
                {
                    EventsCars eventCar = GetAnyByUniqProp(_car.ID, id);
                    if (eventCar.ReadyForList) { eventsCars.Add(eventCar); }
                }
            }
            return eventsCars;
        }

        public static List<EventsCars> SortByCount(List<EventsCars> _list)
        {
            var linqList = from _eventCar in _list
                           where _eventCar.CountBoP > 0
                           orderby _eventCar.CountBoP descending, _eventCar.Count descending, Car.Statics.GetByID(_eventCar.CarID).Name
                           select _eventCar;
            return linqList.Cast<EventsCars>().ToList();
        }
    }
}
