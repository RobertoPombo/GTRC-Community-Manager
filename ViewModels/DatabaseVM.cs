using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using GTRCLeagueManager.Database;

namespace GTRCLeagueManager
{
    public class DatabaseVM : ObservableObject
    {
        public static DatabaseVM? Instance;
        public static SortState CurrentSortState = new();

        private static Type dataType = typeof(ThemeColor);
        private static List<KeyValuePair<string, Type>> listDataTypes = new List<KeyValuePair<string, Type>>();

        private ObservableCollection<DataRowVM> list = new ObservableCollection<DataRowVM>();
        private DataRowVM current;
        private DataRowVM selected;

        public DatabaseVM()
        {
            Instance = this;
            ListDataTypes.Add(new KeyValuePair<string, Type>("Colors", typeof(ThemeColor)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Cars", typeof(Car)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Tracks", typeof(Track)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Drivers", typeof(Driver)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("RaceControl", typeof(RaceControl)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Series", typeof(Series)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Seasons", typeof(Season)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Teams", typeof(Team)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Entries", typeof(Entry)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("Events", typeof(Event)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("DriverEntries", typeof(DriverEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("DriversTeams", typeof(DriversTeams)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("EventsEntries", typeof(EventsEntries)));
            ListDataTypes.Add(new KeyValuePair<string, Type>("PreQualiResultLines", typeof(PreQualiResultLine)));
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
        }

        public void InitializeDatabase()
        {
            Type backupDataType = DataType;
            foreach (KeyValuePair<string, Type> _dataType in ListDataTypes) { DataType = _dataType.Value; LoadSQL(); }
            DataType = backupDataType;
        }

        public static void UpdateDatabase(bool saveSQL)
        {
            Type backupDataType = dataType;
            foreach (KeyValuePair<string, Type> _dataType in listDataTypes) { dataType = _dataType.Value; if (saveSQL) { WriteSQL(); } }
            dataType = backupDataType ?? dataType;
        }

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
                ClearCurrent();
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

        public DataRowVM Current
        {
            get { return current; }
            set { current = value; RaisePropertyChanged(); }
        }

        public DataRowVM Selected
        {
            get { return selected; }
            set { if (value != null) { selected = value; Current = new DataRowVM(Selected.Object, false, false); RaisePropertyChanged(); } }
        }

        public void ResetList(int index = 0)
        {
            List.Clear();
            List<dynamic> iterateObj = new List<dynamic>();
            foreach (var _obj in Current.Object.List) { iterateObj.Add(_obj); }
            foreach (var _obj in iterateObj) { List.Add(new DataRowVM(_obj, true, true)); }
            SetSelected(index);
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
            if (List.Count > index && index >= 0) { Selected = List[index]; }
            else if (List.Count > 0) { Selected = List[0]; }
        }

        public void ClearCurrent()
        {
            Current = new DataRowVM(Activator.CreateInstance(DataType, true, false)!, false, false);
        }

        public void Add()
        {
            if (Current.Object.List.Contains(Current.Object))
            {
                Current.Object = Activator.CreateInstance(DataType, true, false)!;
                DataRow2Object(Current.Object, Current);
                Current = new DataRowVM(Current.Object, false, false);
            }
            else
            {
                List<dynamic> backupValues = Current.Values;
                DataRow2Object(Current.Object, Current);
                Current.Object.ID = Basics.NoID;
                Current = new DataRowVM(Current.Object, false, false);
                if (Enumerable.SequenceEqual(backupValues, Current.Values)) { Current.Object.ListAdd(); ResetList(List.Count); }
            }
        }

        public void Del()
        {
            if (Selected != null && Current.Object.List.Contains(Selected.Object))
            {
                int index = Selected.Object.List.IndexOf(Selected.Object);
                Selected.Object.ListRemove();
                if (index == Selected.Object.List.Count) { index -= 1; }
                ResetList(index);
            }
        }

        public void Update()
        {
            DataRowVM backupSelected = new DataRowVM(Selected.Object, false, false);
            List<dynamic> backupValues = Current.Values;
            DataRow2Object(Selected.Object, Current);
            Current = new DataRowVM(Selected.Object, false, false);
            if (CompareLists(backupValues, Current.Values))
            {
                Selected = new DataRowVM(Selected.Object, true, true);
                if (Selected.Object.List.Contains(Selected.Object)) { ResetList(Selected.Object.List.IndexOf(Selected.Object)); }
            }
            else { DataRow2Object(Selected.Object, backupSelected); }
        }

        public void Sort(PropertyInfo property)
        {
            List<string> numericalTypes = new List<string>() { "System.Int16", "System.Int32", "System.Int64", "System.UInt16", "System.UInt32", "System.UInt64", "System.Single", "System.Double", "System.Decimal", "System.DateTime" };
            bool stringCompare = true;
            if (numericalTypes.Contains(property.PropertyType.ToString())) { stringCompare = false; }
            if (CurrentSortState.Property == property) { CurrentSortState.SortAscending = !CurrentSortState.SortAscending; }
            else { CurrentSortState = new SortState { Property = property }; }
            for (int rowNr1 = 0; rowNr1 < Current.Object.List.Count - 1; rowNr1++)
            {
                for (int rowNr2 = rowNr1 + 1; rowNr2 < Current.Object.List.Count; rowNr2++)
                {
                    bool smallerValueIsFirst;
                    dynamic val1 = Basics.GetCastedValue(Current.Object.List[rowNr1], property);
                    dynamic val2 = Basics.GetCastedValue(Current.Object.List[rowNr2], property);
                    if (stringCompare) { smallerValueIsFirst = String.Compare(val1.ToString(), val2.ToString()) < 0; }
                    else { smallerValueIsFirst = val1 < val2; }
                    if ((CurrentSortState.SortAscending && !smallerValueIsFirst) || (!CurrentSortState.SortAscending && smallerValueIsFirst))
                    {
                        (Current.Object.List[rowNr1], Current.Object.List[rowNr2]) = (Current.Object.List[rowNr2], Current.Object.List[rowNr1]);
                    }
                }
            }
            ResetList(Selected.Object.List.IndexOf(Selected.Object));
        }

        public void ReadJson()
        {
            StaticFieldList.GetByType(DataType).ReadJson();
            ResetList();
        }

        public void WriteJson()
        {
            StaticFieldList.GetByType(DataType).WriteJson();
        }

        public void LoadSQL()
        {
            StaticFieldList.GetByType(DataType).LoadSQL();
            ResetList();
        }

        public static void WriteSQL()
        {
            StaticFieldList.GetByType(dataType).WriteSQL();
        }

        public void ClearList()
        {
            StaticFieldList.GetByType(DataType).ListClear();
            ResetList();
        }

        public void ClearSQL()
        {
            StaticFieldList.GetByType(dataType).ResetSQL();
            ResetList();
        }

        public void ClearJson()
        {
            ClearList();
            WriteJson();
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
    }



    public class DataRowVM : ObservableObject
    {
        private ObservableCollection<DataFieldVM> list = new ObservableCollection<DataFieldVM>();

        public dynamic Object;

        public DataRowVM(dynamic _obj, bool retID, bool retJsonIgnore)
        {
            Object = _obj;
            Dictionary<PropertyInfo, dynamic> dict = _obj.ReturnAsDict(retID, retJsonIgnore, true, true);
            foreach (KeyValuePair<PropertyInfo, dynamic> item in dict) { List.Add(new DataFieldVM(this, item)); }
            RaisePropertyChanged("List");
        }

        public ObservableCollection<DataFieldVM> List { get { return list; } set { list = value; RaisePropertyChanged(); } }

        public List<string> Names
        {
            get
            {
                List<string> _list = new List<string>();
                foreach (DataFieldVM _dataField in List) { _list.Add(_dataField.Name); }
                return _list;
            }
        }

        public List<dynamic> Values
        {
            get
            {
                List<dynamic> _list = new List<dynamic>();
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
        private string name;
        private dynamic val;
        private List<KeyValuePair<string, int>> idList = new List<KeyValuePair<string, int>>();
        private string path;

        public DataRowVM DataRow;
        public PropertyInfo Property;

        public DataFieldVM(DataRowVM _dataRow, KeyValuePair<PropertyInfo, dynamic> item)
        {
            DataRow = _dataRow;
            Property = item.Key;
            Name = Property.Name;
            Value = item.Value;
            idList.Clear();
            dynamic _statics = StaticFieldList.GetByIDProperty(Name);
            if (_statics != null)
            {
                foreach (var _obj in _statics.IDList)
                {
                    idList.Add(new KeyValuePair<string, int>(_obj.ToString(), _obj.ID));
                }
            }
            /*
            if (Basics.SubStr(Name, -2, 2) == "ID")
            {
                string _dataTypeName = Basics.SubStr(Name, 0, Name.Length - 2);
                if (_dataTypeName.Length > 0)
                {
                    foreach (var _obj in StaticFieldList.GetByType(_dataTypeName).IDList)
                    {
                        idList.Add(new KeyValuePair<string, int>(_obj.ToString(), _obj.ID));
                    }
                    /*
                    foreach (KeyValuePair<string, Type> _dataType in DatabaseVM.Instance.ListDataTypes)
                    {
                        if (_dataType.Value.Name == _dataTypeName)
                        {
                            foreach (var _obj in StaticFieldList.GetByType(_dataType.Value).IDList)
                            {
                                idList.Add(new KeyValuePair<string, int>(_obj.ToString(), _obj.ID));
                            }
                            break;
                        }
                    }
                }
            }
            */
            if (Name == "Logo") { Path = Value.ToString(); } else { Path = null; }
            SortCmd = new UICmd((o) => DatabaseVM.Instance.Sort(Property));
        }

        public string Name { get { return name; } set { name = value; RaisePropertyChanged(); } }

        public dynamic Value { get { return val; } set { val = value; RaisePropertyChanged(); } }

        public List<KeyValuePair<string, int>> IDList { get { return idList; } set { idList = value; RaisePropertyChanged(); } }

        public dynamic Path { get { return path; } set { path = value; RaisePropertyChanged(); } }

        public UICmd SortCmd { get; set; }
    }

    public class SortState
    {
        public SortState() { }

        public bool SortAscending = true;
        public PropertyInfo Property = null;

    }
}
