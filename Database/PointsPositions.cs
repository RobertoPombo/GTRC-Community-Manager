using GTRC_Community_Manager;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class PointsPositions : DatabaseObject<PointsPositions>
    {
        public static readonly string DefaultName = "Points system #1";
        [NotMapped][JsonIgnore] public static StaticDbField<PointsPositions> Statics { get; set; }
        static PointsPositions()
        {
            Statics = new StaticDbField<PointsPositions>(true)
            {
                Table = "PointsPositions",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(PointsSystemID), nameof(Position) } },
                ToStringPropertiesNames = new List<string>() { nameof(PointsSystemID), nameof(Position) },
                PublishList = () => PublishList()
            };
        }
        public PointsPositions() { This = this; Initialize(true, true); }
        public PointsPositions(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public PointsPositions(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private PointsSystem objPointsSystem = new(false);
        [JsonIgnore][NotMapped] public PointsSystem ObjPointsSystem { get { return PointsSystem.Statics.GetByID(pointsSystemID); } }

        private int pointsSystemID = 0;
        private int position = 1;
        private int points = 0;

        public int PointsSystemID
        {
            get { return pointsSystemID; }
            set { pointsSystemID = value; if (ReadyForList) { SetNextAvailable(); } objPointsSystem = PointsSystem.Statics.GetByID(pointsSystemID); }
        }

        public int Position
        {
            get { return position; }
            set { if (value <= 0) { position = 1; } else { position = value; } if (ReadyForList) { SetNextAvailable(); } }
        }

        public int Points
        {
            get { return points; }
            set { points = value; }
        }

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            int pointsSystemNr = 0;
            List<PointsSystem> _idListPointsSystem = PointsSystem.Statics.IDList;
            if (_idListPointsSystem.Count == 0) { _ = new PointsSystem() { ID = 1 }; _idListPointsSystem = PointsSystem.Statics.IDList; }
            PointsSystem _pointsSystem = PointsSystem.Statics.GetByID(pointsSystemID);
            if (_pointsSystem.ReadyForList) { pointsSystemNr = PointsSystem.Statics.IDList.IndexOf(_pointsSystem); }
            else { pointsSystemID = _idListPointsSystem[0].ID; }
            int startValuePointsSystem = pointsSystemNr;
            int startValuePosition = position;

            while (!IsUnique())
            {
                if (position < int.MaxValue) { position += 1; } else { position = 1; }
                if (position == startValuePosition)
                {
                    if (pointsSystemNr + 1 < _idListPointsSystem.Count) { pointsSystemNr += 1; } else { pointsSystemNr = 0; }
                    pointsSystemID = _idListPointsSystem[pointsSystemNr].ID;
                    if (pointsSystemNr == startValuePointsSystem) { break; }
                }
            }

            objPointsSystem = PointsSystem.Statics.GetByID(pointsSystemID);
        }
    }
}
