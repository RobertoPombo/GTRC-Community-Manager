using Core;
using Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Controls;

namespace GTRC_Community_Manager
{
    public class DatabaseVM : ObservableObject
    {
        public static DatabaseVM? Instance;
        private static readonly string PathFilter = MainWindow.dataDirectory + "config filter.json";
        private static Type dataType = typeof(ThemeColor);
        private static List<KeyValuePair<string, Type>> listDataTypes = new();
        public static bool IsRunning = false;
        public static readonly Random random = new();

        private bool forceDel = false;
        private ObservableCollection<DataRowVM> list = new();
        private DataRowVM? current;
        private DataRowVM? selected;
        private int SelectedID = Basics.NoID;
        private dynamic? Statics;
        private ObservableCollection<StaticDbFilter> filter = new();

        public DatabaseVM()
        {
            Instance = this;
            ListDataTypes.Add(new KeyValuePair<string, Type>(ThemeColor.Statics.Table, typeof(ThemeColor)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Car.Statics.Table, typeof(Car)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Track.Statics.Table, typeof(Track)));
            //ListDataTypes.Add(new KeyValuePair<string, Type>(Organization.Statics.Table, typeof(Organization)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Driver.Statics.Table, typeof(Driver)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(RaceControl.Statics.Table, typeof(RaceControl)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Series.Statics.Table, typeof(Series)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Season.Statics.Table, typeof(Season)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(PointsSystem.Statics.Table, typeof(PointsSystem)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(PointsPositions.Statics.Table, typeof(PointsPositions)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Server.Statics.Table, typeof(Server)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Team.Statics.Table, typeof(Team)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Entry.Statics.Table, typeof(Entry)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Event.Statics.Table, typeof(Event)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Session.Statics.Table, typeof(Session)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(DriversEntries.Statics.Table, typeof(DriversEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(DriversTeams.Statics.Table, typeof(DriversTeams)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(DriversDatetimes.Statics.Table, typeof(DriversDatetimes)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(EntriesDatetimes.Statics.Table, typeof(EntriesDatetimes)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(EventsEntries.Statics.Table, typeof(EventsEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(EventsCars.Statics.Table, typeof(EventsCars)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(ResultsFile.Statics.Table, typeof(ResultsFile)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Lap.Statics.Table, typeof(Lap)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(LeaderboardLinePractice.Statics.Table, typeof(LeaderboardLinePractice)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Incident.Statics.Table, typeof(Incident)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(IncidentsEntries.Statics.Table, typeof(IncidentsEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(PreQualiResultLine.Statics.Table, typeof(PreQualiResultLine)));
            if (!File.Exists(PathFilter)) { File.WriteAllText(PathFilter, "", Encoding.Unicode); }
            RestoreFilter();
            DataType = ListDataTypes[0].Value;
            AddCmd = new UICmd((o) => Add());
            DelCmd = new UICmd((o) => Del());
            ClearCurrentCmd = new UICmd((o) => ClearCurrent());
            UpdateCmd = new UICmd((o) => Update());
            LoadJsonCmd = new UICmd((o) => ReadJson());
            LoadSQLCmd = new UICmd((o) => LoadSQL());
            WriteJsonCmd = new UICmd((o) => WriteJson());
            WriteSQLCmd = new UICmd((o) => WriteSQL());
            ClearListCmd = new UICmd((o) => ClearList());
            ClearSQLCmd = new UICmd((o) => ClearSQL());
            ClearJsonCmd = new UICmd((o) => ClearJson());
            CleanUpListCmd = new UICmd((o) => CleanUpList());
            ClearFilterCmd = new UICmd((o) => ClearFilter());
        }

        public void InitializeDatabase()
        {
            Type backupDataType = DataType;
            foreach (KeyValuePair<string, Type> _dataType in ListDataTypes) { DataType = _dataType.Value; forceDel = true; ClearList(); }
            foreach (KeyValuePair<string, Type> _dataType in ListDataTypes) { DataType = _dataType.Value; LoadSQL(); }
            DataType = backupDataType;

            /*TEMP Löschen Conv
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); }
            IsRunning = true; ForceDel = true;
            Organization.Statics.ResetSQL(UseForceDel());
            foreach (Team _team in Team.Statics.List)
            {
                if (!Organization.Statics.ExistsUniqProp(_team.OrganizationName)) { _ = Organization.Statics.WriteSQL(new Organization() { Name = _team.OrganizationName }); }
            }
            IsRunning = false;*/
        }

        public bool ForceDel
        {
            get { return forceDel; }
            set { forceDel = value; RaisePropertyChanged(); }
        }

        public bool UseForceDel() { if (ForceDel) { ForceDel = false; return true; } else { return false; } }

        public List<KeyValuePair<string, Type>> ListDataTypes
        {
            get { return listDataTypes; }
            set { listDataTypes = value; RaisePropertyChanged(); }
        }

        public Type DataType
        {
            get { return dataType; }
            set
            {
                dataType = value;
                Statics = StaticFieldList.GetByType(dataType);
                ClearCurrent();
                filter = new();
                if (Statics is not null) { foreach (StaticDbFilter _staticDbFilter in Statics.Filter) { filter.Add(_staticDbFilter); } }
                RaisePropertyChanged(nameof(Filter));
                ResetList();
                ClearCurrent();
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<DataRowVM> List
        {
            get { return list; }
            set { list = value; RaisePropertyChanged(); }
        }

        public DataRowVM? Current
        {
            get { return current; }
            set { current = value; RaisePropertyChanged(); }
        }

        public DataRowVM? Selected
        {
            get { return selected; }
            set
            {
                if (value != null)
                {
                    selected = value;
                    SelectedID = selected.Object.ID;
                    Current = new DataRowVM(selected.Object, false, false);
                    RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<StaticDbFilter> Filter { get { return filter; } set { filter = value; RaisePropertyChanged(); } }

        public void RaisePropertyChanged_Filter(int index = 0)
        {
            if (Filter?.Count > index && Statics?.Filter?.Count > index)
            {
                Filter.RemoveAt(index);
                Filter.Insert(index, Statics.Filter[index]);
                RaisePropertyChanged(nameof(Filter));
            }
        }

        public void ResetList(int index = 0)
        {
            List.Clear();
            if (Statics is not null) { foreach (var _obj in Statics.FilteredList) { List.Add(new DataRowVM(_obj, true, true)); } SetSelected(index); }
        }

        public static void DataRow2Object(dynamic _obj, DataRowVM _dataRow)
        {
            foreach (DataFieldVM _dataField in _dataRow.List) { _dataField.Property.SetValue(_obj, Basics.CastValue(_dataField.Property, _dataField.Value)); }
        }

        public static bool CompareLists(List<dynamic> list1, List<dynamic> list2)
        {
            bool identical = true;
            if (list1.Count == list2.Count)
            {
                for (int index = 0; index < list1.Count; index++)
                {
                    if (!list1[index].Equals(list2[index])) { identical = false; break; }
                }
            }
            else { identical = false; }
            return identical;
        }

        public void SetSelected(int index = 0)
        {
            if (Statics?.FilteredList?.Count > 0)
            {
                index = Math.Min(Math.Max(0, index), Statics.FilteredList.Count - 1);
                if (index == 0 && SelectedID != Basics.NoID)
                {
                    var _obj = Statics.GetByID(SelectedID);
                    if (Statics.FilteredList.Contains(_obj)) { index = Statics.FilteredList.IndexOf(_obj); }
                }
                foreach (DataRowVM _dataRow in List) { if (_dataRow.Object.ID == Statics.FilteredList[index].ID) { Selected = _dataRow; break; } }
            }
        }

        public void ClearCurrent()
        {
            Current = new DataRowVM(Activator.CreateInstance(DataType, true, false)!, false, false);
        }

        public void Add()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            List<dynamic> backupValues = Current.Values;
            if (Current.Object.List.Contains(Current.Object))
            {
                Current.Object = Activator.CreateInstance(DataType, true, false)!;
            }
            DataRow2Object(Current.Object, Current);
            Current.Object.ID = Basics.NoID;
            Current = new DataRowVM(Current.Object, false, false);
            if (Enumerable.SequenceEqual(backupValues, Current.Values)) { Current.Object.ListAdd(); ResetList(Current.Object.ID); }
            IsRunning = false;
        }

        public void Del()
        {
            if (Selected is not null && Statics?.FilteredList.Contains(Selected.Object))
            {
                while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
                int index = Statics.FilteredList.IndexOf(Selected.Object);
                Selected.Object.ListRemove(UseForceDel());
                ResetList(index);
                IsRunning = false;
            }
        }

        public void Update()
        {
            if (Current is not null && Selected is not null)
            {
                DataRowVM backupSelected = new(Selected.Object, false, false);
                List<dynamic> backupValues = Current.Values;
                DataRow2Object(Selected.Object, Current);
                Current = new DataRowVM(Selected.Object, false, false);
                if (CompareLists(backupValues, Current.Values)) { Selected = new DataRowVM(Selected.Object, true, true); ResetList(); }
                else { DataRow2Object(Selected.Object, backupSelected); }
            }
        }

        public void ReadJson()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Statics.ReadJson(UseForceDel());
            ResetList();
            IsRunning = false;
        }

        public void WriteJson()
        {
            Statics.WriteJson();
        }

        public void LoadSQL()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Statics.LoadSQL(UseForceDel());
            ResetList();
            IsRunning = false;
        }

        public void WriteSQL()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Statics.WriteSQL();
            ResetList();
            IsRunning = false;
        }

        public void ClearList()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Statics.ListClear(UseForceDel());
            ResetList();
            IsRunning = false;
        }

        public void ClearSQL()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Statics.ResetSQL(UseForceDel());
            ResetList();
            IsRunning = false;
        }

        public void ClearJson()
        {
            ClearList();
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            WriteJson();
            IsRunning = false;
        }

        public void CleanUpList()
        {
            while (MainWindow.CheckExistingSqlThreads()) { Thread.Sleep(200 + random.Next(100)); } IsRunning = true;
            Statics.DeleteNotUnique();
            ResetList();
            IsRunning = false;
        }

        public void ClearFilter()
        {
            for (int filterNr = Filter.Count - 1; filterNr >= 0; filterNr--) { Filter[filterNr].Filter = ""; RaisePropertyChanged_Filter(filterNr); }
        }

        public void RestoreFilter()
        {
            try
            {
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(PathFilter, Encoding.Unicode));
                foreach (KeyValuePair<string, Type> _dataType in ListDataTypes)
                {
                    dynamic? _tempStatics = StaticFieldList.GetByType(_dataType.Value);
                    var _filter = obj?[_dataType.Key] ?? null;
                    if (_tempStatics is not null && _filter is not null && _filter is IList)
                    {
                        DataType = _dataType.Value;
                        for (int filterNr = 0; filterNr < _tempStatics.Filter.Count; filterNr++)
                        {
                            foreach (var _property in _filter)
                            {
                                string? _name = _property.PropertyName?.ToString() ?? null;
                                string? _value = _property.Filter?.ToString() ?? null;
                                if (_name == _tempStatics.Filter[filterNr].PropertyName && _value is not null)
                                {
                                    _tempStatics.Filter[filterNr].Filter = _value;
                                    RaisePropertyChanged_Filter(filterNr);
                                    break;
                                }
                            }
                        }
                    }
                }
                MainVM.List[0].LogCurrentText = "Filter settings restored.";
            }
            catch { MainVM.List[0].LogCurrentText = "Restore filter settings failed!"; }
        }

        public void SaveFilter()
        {
            Dictionary<string, List<StaticDbFilter>> filterList = new();
            foreach (KeyValuePair<string, Type> _dataType in ListDataTypes)
            {
                List<StaticDbFilter> tempFilterList = new();
                dynamic? _tempStatics = StaticFieldList.GetByType(_dataType.Value);
                if (_tempStatics is not null) { foreach (StaticDbFilter _staticDbFilter in _tempStatics.Filter) { tempFilterList.Add(_staticDbFilter); } }
                filterList[_dataType.Key] = tempFilterList;
            }
            string text = JsonConvert.SerializeObject(filterList, Formatting.Indented);
            File.WriteAllText(PathFilter, text, Encoding.Unicode);
            MainVM.List[0].LogCurrentText = "Filter settings saved.";
        }

        public UICmd AddCmd { get; set; }
        public UICmd DelCmd { get; set; }
        public UICmd ClearCurrentCmd { get; set; }
        public UICmd UpdateCmd { get; set; }
        public UICmd LoadJsonCmd { get; set; }
        public UICmd WriteJsonCmd { get; set; }
        public UICmd LoadSQLCmd { get; set; }
        public UICmd WriteSQLCmd { get; set; }
        public UICmd ClearListCmd { get; set; }
        public UICmd ClearSQLCmd { get; set; }
        public UICmd ClearJsonCmd { get; set; }
        public UICmd CleanUpListCmd { get; set; }
        public UICmd ClearFilterCmd { get; set; }
    }



    public class DataRowVM : ObservableObject
    {
        private ObservableCollection<DataFieldVM> list = new();

        public dynamic Object;

        public DataRowVM(dynamic _obj, bool retID, bool retJsonIgnore)
        {
            Object = _obj;
            Dictionary<PropertyInfo, dynamic> dict = _obj.ReturnAsDict(retID, retJsonIgnore, true, true);
            foreach (KeyValuePair<PropertyInfo, dynamic> item in dict) { List.Add(new DataFieldVM(this, item)); }
            RaisePropertyChanged(nameof(List));
        }

        public ObservableCollection<DataFieldVM> List { get { return list; } set { list = value; RaisePropertyChanged(); } }

        public List<string> Names
        {
            get
            {
                List<string> _list = new();
                foreach (DataFieldVM _dataField in List) { _list.Add(_dataField.Name); }
                return _list;
            }
        }

        public List<dynamic> Values
        {
            get
            {
                List<dynamic> _list = new();
                foreach (DataFieldVM _dataField in List) { _list.Add(Basics.CastValue(_dataField.Property, _dataField.Value)); }
                return _list;
            }
        }

        public DataFieldVM GetDataFieldByPropertyName(string _name)
        {
            foreach (DataFieldVM _dataField in List)
            {
                if (_dataField.Name == _name) { return _dataField; }
            }
            return new DataFieldVM(null, new KeyValuePair<PropertyInfo, dynamic>());
        }
    }



    public class DataFieldVM : ObservableObject
    {
        private string name = "";
        private dynamic val = Basics.NoID;
        private List<KeyValuePair<string, int>> idList = new();
        private string? path;

        public DataRowVM? DataRow;
        public PropertyInfo Property;

        public DataFieldVM(DataRowVM? _dataRow, KeyValuePair<PropertyInfo, dynamic> item)
        {
            DataRow = _dataRow;
            Property = item.Key;
            Name = Property.Name;
            Value = item.Value;
            idList.Clear();
            dynamic? _statics = StaticFieldList.GetByIDProperty(Name);
            if (_statics is not null)
            {
                foreach (var _obj in _statics.IDList)
                {
                    idList.Add(new KeyValuePair<string, int>(_obj.ToString(), _obj.ID));
                }
            }
            if (Name == "Logo") { Path = Value.ToString(); } else { Path = null; }
        }

        public string Name { get { return name; } set { name = value; RaisePropertyChanged(); } }

        public dynamic Value { get { return val; } set { val = value; RaisePropertyChanged(); } }

        public List<KeyValuePair<string, int>> IDList { get { return idList; } set { idList = value; RaisePropertyChanged(); } }

        public dynamic? Path { get { return path; } set { path = value; RaisePropertyChanged(); } }
    }
}
