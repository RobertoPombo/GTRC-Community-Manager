using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

namespace Database
{
    public class PointsSystem : DatabaseObject<PointsSystem>
    {
        public static readonly string DefaultName = "Points system #1";
        [NotMapped][JsonIgnore] public static StaticDbField<PointsSystem> Statics { get; set; }
        static PointsSystem()
        {
            Statics = new StaticDbField<PointsSystem>(true)
            {
                Table = "PointsSystems",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(Name) } },
                ToStringPropertiesNames = new List<string>() { nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public PointsSystem() { This = this; Initialize(true, true); }
        public PointsSystem(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public PointsSystem(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private string name = DefaultName;
        private int minPercentageOfP1 = 0;
        private int maxPercentageOfP1 = int.MaxValue;

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

        public int MinPercentageOfP1
        {
            get { return minPercentageOfP1; }
            set { if (value < 0) { minPercentageOfP1 = 0; } else if (value > 100) { minPercentageOfP1 = 100; } else { minPercentageOfP1 = value; } }
        }

        public int MaxPercentageOfP1
        {
            get { return maxPercentageOfP1; }
            set { if (value < 100) { maxPercentageOfP1 = 100; } else { maxPercentageOfP1 = value; } }
        }

        public static void PublishList() { }

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

        public int GetPointsByPosition(int position)
        {
            if (ID != Basics.NoID)
            {
                for (int pos = position; pos > 0; pos--)
                {
                    List<dynamic> uniqValues = new() { ID, pos };
                    if (PointsPositions.Statics.ExistsUniqProp(uniqValues))
                    {
                        return PointsPositions.Statics.GetByUniqProp(uniqValues).Points;
                    }
                }
            }
            return 0;
        }
    }
}
