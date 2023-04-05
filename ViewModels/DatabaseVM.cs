using Core;
using Database;
using Scripts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace GTRC_Community_Manager
{
    public class DatabaseVM : ObservableObject
    {
        public static DatabaseVM? Instance;

        private static Type dataType = typeof(ThemeColor);
        private static List<KeyValuePair<string, Type>> listDataTypes = new();

        private bool forceDel = false;
        private ObservableCollection<DataRowVM> list = new();
        private DataRowVM? current;
        private DataRowVM? selected;
        private int SelectedID = Basics.NoID;
        private dynamic? Statics;
        private ObservableCollection<StaticDbFilter> filter;

        public DatabaseVM()
        {
            Instance = this;
            ListDataTypes.Add(new KeyValuePair<string, Type>(ThemeColor.Statics.Table, typeof(ThemeColor)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Car.Statics.Table, typeof(Car)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Track.Statics.Table, typeof(Track)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Driver.Statics.Table, typeof(Driver)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(RaceControl.Statics.Table, typeof(RaceControl)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Series.Statics.Table, typeof(Series)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Season.Statics.Table, typeof(Season)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Server.Statics.Table, typeof(Server)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Team.Statics.Table, typeof(Team)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Entry.Statics.Table, typeof(Entry)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Event.Statics.Table, typeof(Event)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(DriversEntries.Statics.Table, typeof(DriversEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(DriversTeams.Statics.Table, typeof(DriversTeams)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(EventsEntries.Statics.Table, typeof(EventsEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(EventsCars.Statics.Table, typeof(EventsCars)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(PreQualiResultLine.Statics.Table, typeof(PreQualiResultLine)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(Incident.Statics.Table, typeof(Incident)));
            ListDataTypes.Add(new KeyValuePair<string, Type>(IncidentsEntries.Statics.Table, typeof(IncidentsEntries)));
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
            foreach (KeyValuePair<string, Type> _dataType in ListDataTypes) { DataType = _dataType.Value; if (_dataType.Key == "EventsEntries") { } LoadSQL(); }
            DataType = backupDataType;
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
            List<dynamic> backupValues = Current.Values;
            if (Current.Object.List.Contains(Current.Object))
            {
                Current.Object = Activator.CreateInstance(DataType, true, false)!;
            }
            DataRow2Object(Current.Object, Current);
            Current.Object.ID = Basics.NoID;
            Current = new DataRowVM(Current.Object, false, false);
            if (Enumerable.SequenceEqual(backupValues, Current.Values)) { Current.Object.ListAdd(); ResetList(Current.Object.ID); }
        }

        public void Del()
        {
            if (Selected is not null && Statics?.FilteredList.Contains(Selected.Object))
            {
                int index = Statics.FilteredList.IndexOf(Selected.Object);
                Selected.Object.ListRemove(UseForceDel());
                ResetList(index);
            }
        }

        public void Update()
        {
            DataRowVM backupSelected = new(Selected.Object, false, false);
            List<dynamic> backupValues = Current.Values;
            DataRow2Object(Selected.Object, Current);
            Current = new DataRowVM(Selected.Object, false, false);
            if (CompareLists(backupValues, Current.Values)) { Selected = new DataRowVM(Selected.Object, true, true); ResetList(); }
            else { DataRow2Object(Selected.Object, backupSelected); }
        }

        public void ReadJson()
        {
            Statics.ReadJson(UseForceDel());
            ResetList();
        }

        public void WriteJson()
        {
            Statics.WriteJson();
        }

        public void LoadSQL()
        {
            Statics.LoadSQL(UseForceDel());
            ResetList();
        }

        public void WriteSQL()
        {
            Statics.WriteSQL();
            ResetList();
        }

        public void ClearList()
        {
            Statics.ListClear(UseForceDel());
            ResetList();
        }

        public void ClearSQL()
        {
            Statics.ResetSQL(UseForceDel());
            ResetList();
        }

        public void ClearJson()
        {
            ClearList();
            WriteJson();
        }

        public void CleanUpList()
        {
            Statics.DeleteNotUnique();
            ResetList();
        }

        public void ClearFilter()
        {
            for (int filterNr = Filter.Count - 1; filterNr >= 0; filterNr--) { Filter[filterNr].Filter = ""; RaisePropertyChanged_Filter(filterNr); }
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
