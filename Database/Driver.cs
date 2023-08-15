using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Scripts;

namespace Database
{
    public class Driver : DatabaseObject<Driver>
    {
        public static readonly long DiscordIDNoValue = 0;
        public static readonly long SteamIDMinValue = 10000000000000000;
        public static readonly long DiscordIDMinValue = 100000000000000000;
        public static readonly long SteamIDMaxValue = SteamIDMinValue * 10 - 1;
        public static readonly long DiscordIDMaxValue = Int64.MaxValue;
        [NotMapped][JsonIgnore] public static StaticDbField<Driver> Statics { get; set; }
        static Driver()
        {
            Statics = new StaticDbField<Driver>(true)
            {
                Table = "Drivers",
                UniquePropertiesNames = new List<List<string>>() { new List<string>() { nameof(SteamID) }, new List<string>() { nameof(DiscordID) } },
                ToStringPropertiesNames = new List<string>() { nameof(FullName) },
                PublishList = () => PublishList()
            };
        }
        public Driver() { This = this; Initialize(true, true); }
        public Driver(bool _readyForList) { This = this; Initialize(_readyForList, _readyForList); }
        public Driver(bool _readyForList, bool inList) { This = this; Initialize(_readyForList, inList); }

        private long steamID = SteamIDMinValue;
        private long discordID = Basics.NoID;
        private string firstName = "";
        private string lastName = "";
        private DateTime registerDate = DateTime.Now;
        private DateTime banDate = Basics.DateTimeMaxValue;
        private string name3Digits = "";
        private int eloRating = 1500;
        private int safetyRating = 50;
        private int warnings = 0;
        /*private string discordName = "";
        private bool isOnDiscordServer = false;
        private string accessToken = "";*/

        [NotMapped][JsonIgnore] public List<string> Name3DigitsOptions = new() { "" };

        public long SteamID
        {
            get { return steamID; }
            set { if (!IsValidSteamID(value)) { steamID = SteamIDMinValue; } else { steamID = value; } if (ReadyForList) { SetNextAvailable(); } }
        }

        public long DiscordID
        {
            get { return discordID; }
            set { if (!IsValidDiscordID(value)) { discordID = DiscordIDNoValue; } else { discordID = value; } if (ReadyForList) { SetNextAvailable(); } }
        }

        public string FirstName
        {
            get { return firstName; }
            set { firstName = Basics.RemoveSpaceStartEnd(value ?? firstName); UpdateName3DigitsOptions(); }
        }

        public string LastName
        {
            get { return lastName; }
            set { lastName = Basics.RemoveSpaceStartEnd(value ?? lastName); UpdateName3DigitsOptions(); }
        }

        [JsonIgnore] public string FullName
        {
            get { return GetFullName(firstName, lastName); }
        }

        [JsonIgnore] public string ShortName
        {
            get { return GetShortName(firstName, lastName); }
        }

        public DateTime RegisterDate
        {
            get { return registerDate; }
            set { if (value >= Basics.DateTimeMinValue && value > DateTime.Now) { registerDate = DateTime.Now; } else { registerDate = value; } }
        }

        public DateTime BanDate
        {
            get { return banDate; }
            set { if (value >= Basics.DateTimeMinValue && value >= RegisterDate) { banDate = value; } }
        }

        public string Name3Digits
        {
            get { return name3Digits; }
            set {
                if (value != null && value.Length == 3)
                {
                    name3Digits = value.ToUpper();
                    UpdateName3DigitsOptions();
                }
            }
        }

        public int EloRating
        {
            get { return eloRating; }
            set { if (value < 0) { eloRating = 0; } else if (value > 9999) { eloRating = 9999; } else { eloRating = value; } }
        }

        public int SafetyRating
        {
            get { return safetyRating; }
            set { if (value > 100) { safetyRating = 100; } else { safetyRating = value; } }
        }

        public int Warnings
        {
            get { return warnings; }
            set
            {
                if (value < 0) { warnings = 0; }
                else { warnings = value; }
            }
        }

        /*public string DiscordName
        {
            get { return discordName; }
            set { discordName = value; }
        }

        public bool IsOnDiscordServer
        {
            get { return isOnDiscordServer; }
            set { isOnDiscordServer = value; }
        }

        public string AccessToken
        {
            get { return accessToken; }
            set { accessToken = value; }
        }*/

        public static void PublishList() { }

        public override void SetNextAvailable()
        {
            long startValue = steamID;
            while (!IsUnique(0))
            {
                if (steamID < SteamIDMaxValue) { steamID += 1; } else { steamID = SteamIDMinValue; }
                if (steamID == startValue) { break; }
            }
            startValue = discordID;
            while (!IsUnique(1) && discordID != DiscordIDNoValue)
            {
                if (discordID < DiscordIDMaxValue) { discordID += 1; } else { discordID = DiscordIDMinValue; }
                if (discordID == startValue) { break; }
            }
        }

        public static bool IsValidSteamID(long _steamID)
        {
            return _steamID >= SteamIDMinValue && _steamID <= SteamIDMaxValue;
        }

        public static bool IsValidDiscordID(long _discordID)
        {
            return _discordID >= DiscordIDMinValue && _discordID <= DiscordIDMaxValue;
        }

        public static long String2LongSteamID(string? _strSteamID)
        {
            long _steamID = Basics.NoID;
            _strSteamID = new string(_strSteamID?.Where(Char.IsNumber).ToArray());
            _ = long.TryParse(_strSteamID, out _steamID);
            if (IsValidSteamID(_steamID)) { return _steamID; }
            else { return Basics.NoID; }
        }

        public static string GetFullName(string _firstName, string _lastName)
        {
            return _firstName + " " + _lastName;
        }

        public static string GetShortName(string _firstName, string _lastName)
        {
            string _shortName = "";
            List<char> cList = new() { ' ', '-' };
            if (_firstName != null)
            {
                for (int index = 0; index < _firstName.Length - 1; index++)
                {
                    if (_shortName.Length == 0 && !cList.Contains(_firstName[index]))
                    {
                        _shortName = _firstName[index].ToString() + ".";
                    }
                    else if (cList.Contains(_firstName[index]) && !cList.Contains(_firstName[index + 1]))
                    {
                        _shortName += _firstName[index].ToString() + _firstName[index + 1].ToString() + ".";
                    }
                }
                _shortName += " " + _lastName;
            }
            else { _shortName = _lastName; }
            return _shortName;
        }

        public static string DriverList2String(List<Driver> drivers, string PropertyName)
        {
            string strDrivers = "";
            foreach (PropertyInfo property in Statics.AllProperties)
            {
                if (property.Name == PropertyName)
                {
                    foreach (Driver obj in drivers) { strDrivers += Basics.GetCastedValue(obj, property).ToString() + ", "; }
                    break;
                }
            }
            strDrivers = strDrivers[..Math.Max(0, strDrivers.Length - 2)];
            return strDrivers;
        }

        public void UpdateName3DigitsOptions()
        {
            List<string> listFirstNames; List<string> listLastNames;
            List<string> tempListN3D = new();
            listFirstNames = FilterLetters4N3D(FirstName);
            listLastNames = FilterLetters4N3D(LastName);
            List<string> listAllNames = new();
            foreach (string _name in listFirstNames) { listAllNames.Add(_name); }
            foreach (string _name in listLastNames) { listAllNames.Add(_name); }
            tempListN3D = AddN3D(tempListN3D, Name3Digits);
            string n3D = "";
            foreach (string _name in listLastNames) { n3D += _name[0]; }
            tempListN3D = AddN3D(tempListN3D, n3D);
            n3D = "";
            foreach (string _name in listAllNames) { n3D += _name[0]; }
            tempListN3D = AddN3D(tempListN3D, n3D);
            foreach (string _name in listLastNames) { tempListN3D = AddN3D(tempListN3D, _name); }
            n3D = "";
            foreach (string _name in listLastNames) { n3D += _name; }
            tempListN3D = AddN3D(tempListN3D, n3D);
            foreach (string _fname in listFirstNames)
            {
                n3D = Basics.SubStr(_fname, 0, 1);
                foreach (string _name in listLastNames) { n3D += _name; }
                tempListN3D = AddN3D(tempListN3D, n3D);
            }
            n3D = "";
            foreach (string _name in listLastNames)
            {
                n3D += Basics.SubStr(_name, 0, 1) + Basics.StrRemoveVocals(Basics.SubStr(_name, 1));
            }
            tempListN3D = AddN3D(tempListN3D, n3D);
            foreach (string _fname in listFirstNames)
            {
                n3D = Basics.SubStr(_fname, 0, 1);
                foreach (string _name in listLastNames)
                {
                    n3D += Basics.SubStr(_name, 0, 1) + Basics.StrRemoveVocals(Basics.SubStr(_name, 1));
                }
                tempListN3D = AddN3D(tempListN3D, n3D);
            }
            foreach (string _fname in listFirstNames) { tempListN3D = AddN3D(tempListN3D, _fname); }
            n3D = "";
            foreach (string _name in listLastNames)
            {
                n3D += Basics.SubStr(_name, 0, 1) + Basics.StrRemoveVocals(Basics.SubStr(_name, 1));
            }
            if (n3D.Length > 2)
            {
                for (int charNr1 = 1; charNr1 < n3D.Length - 1; charNr1++)
                {
                    for (int charNr2 = charNr1 + 1; charNr2 < n3D.Length; charNr2++)
                    {
                        tempListN3D = AddN3D(tempListN3D, Basics.SubStr(n3D, 0, 1) + Basics.SubStr(n3D, charNr1, 1) + Basics.SubStr(n3D, charNr2, 1));
                    }
                }
            }
            foreach (string _fname in listFirstNames)
            {
                n3D = Basics.SubStr(_fname, 0, 1);
                foreach (string _name in listLastNames)
                {
                    n3D += Basics.SubStr(_name, 0, 1) + Basics.StrRemoveVocals(Basics.SubStr(_name, 1));
                }
                if (n3D.Length > 2)
                {
                    for (int charNr = 2; charNr < n3D.Length; charNr++)
                    {
                        tempListN3D = AddN3D(tempListN3D, Basics.SubStr(n3D, 0, 2) + Basics.SubStr(n3D, charNr, 1));
                    }
                }
            }
            n3D = "";
            foreach (string _name in listLastNames) { n3D += Basics.StrRemoveVocals(Basics.SubStr(_name, 1)); }
            if (n3D.Length > 2)
            {
                for (int charNr1 = 1; charNr1 < n3D.Length - 1; charNr1++)
                {
                    for (int charNr2 = charNr1 + 1; charNr2 < n3D.Length; charNr2++)
                    {
                        tempListN3D = AddN3D(tempListN3D, Basics.SubStr(n3D, 0, 1) + Basics.SubStr(n3D, charNr1, 1) + Basics.SubStr(n3D, charNr2, 1));
                    }
                }
            }
            foreach (string _fname in listFirstNames)
            {
                n3D = Basics.SubStr(_fname, 0, 1);
                foreach (string _name in listLastNames) { n3D += _name; }
                if (n3D.Length > 2)
                {
                    for (int charNr = 2; charNr < n3D.Length; charNr++)
                    {
                        tempListN3D = AddN3D(tempListN3D, Basics.SubStr(n3D, 0, 2) + Basics.SubStr(n3D, charNr, 1));
                    }
                }
            }
            n3D = "";
            foreach (string _name in listAllNames) { n3D += _name; }
            n3D += "XXX";
            tempListN3D = AddN3D(tempListN3D, n3D);
            Name3DigitsOptions = tempListN3D;
        }

        public static List<string> AddN3D(List<string> tempListN3D, string n3D)
        {
            if (n3D.Length > 2 && !tempListN3D.Contains(Basics.SubStr(n3D, 0, 3))) { tempListN3D.Add(Basics.SubStr(n3D, 0, 3)); }
            return tempListN3D;
        }

        public static List<string> FilterLetters4N3D(string name)
        {
            name = Basics.StrRemoveSpecialLetters(name);
            name = name.ToUpper();
            name = name.Replace("-", " ");
            List<string> nameList = new();
            foreach (string _name in name.Split(' ')) { if (_name.Length > 0) { nameList.Add(_name); } }
            return nameList;
        }

        public bool IsBanned(Event _event) { if (BanDate > _event.Date) { return false; } else { return true; } }
    }
}
