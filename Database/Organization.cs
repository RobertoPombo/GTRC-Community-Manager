using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Scripts;

namespace Database
{
    public class Organization : DatabaseObject<Organization>
    {
        public static readonly string DefaultName = "Organization #1";
        [NotMapped][JsonIgnore] public static StaticDbField<Organization> Statics { get; set; }
        static Organization()
        {
            Statics = new StaticDbField<Organization>(true)
            {
                Table = "Organizations",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(Name) } },
                ToStringPropertiesNames = new List<string>() { nameof(Name) },
                PublishList = () => PublishList()
            };
        }
        public Organization() { This = this; Initialize(true, true); }
        public Organization(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Organization(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private string name = DefaultName;

        public string Name
        {
            get { return name; }
            set
            {
                name = Basics.SubStr(Basics.RemoveSpaceStartEnd(value ?? name), 0, 32);
                if (name == null || name == "") { name = DefaultName; }
                if (ReadyForList) { SetNextAvailable(); }
            }
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
    }
}
