using Core;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Documents;
using Scripts;

using GTRC_Community_Manager;

namespace Database
{
    public interface IDatabaseObject
    {
        bool ReadyForList { get; set; }
        int ID { get; set; }
        int Nr { get; }
        void Initialize(bool _readyForList, bool inList);
        void ListAdd();
        void ListInsert(int index);
        void ListRemove(bool forceDel = false);
        void DeleteSQL();
        void SetNextAvailable();
        Dictionary<PropertyInfo, dynamic> ReturnAsDict(bool retID, bool retJsonIgnore, bool retCanNotWrite, bool retNotUnique);
        bool IsUnique();
        bool IsUnique(int index);
        bool IsChild();
        void RemoveAllChilds(int newID = -1);
        string ToString();
    }



    public static class StaticFieldList
    {
        public static List<dynamic> List = new();

        public static dynamic? GetByType(Type _varDbType)
        {
            foreach (dynamic _obj in List) { if (_obj.VarDbType == _varDbType) { return _obj; } }
            return null;
        }

        public static dynamic? GetByType(string _varDbType)
        {
            foreach (dynamic _obj in List) { if (_obj.VarDbType.Name == _varDbType) { return _obj; } }
            return null;
        }

        public static dynamic? GetByIDProperty(string _idPropertyName)
        {
            if (Basics.SubStr(_idPropertyName, -2, 2) == "ID")
            {
                _idPropertyName = Basics.SubStr(_idPropertyName, 0, _idPropertyName.Length - 2);
                if (_idPropertyName.Length > 0) { return GetByType(_idPropertyName); }
            }
            return null;
        }
    }



    public class StaticDbField<DbType> where DbType : IDatabaseObject, new()
    {
        public StaticDbField(bool inList) { if (inList) { StaticFieldList.List.Add(this); } }

        public string Table = "";
        public List<List<string>> UniquePropertiesNames = new();
        public List<string> ToStringPropertiesNames = new();
        public Action PublishList = () => Console.WriteLine("Error");

        public Type VarDbType = typeof(DbType);
        public List<DbType> List = new();
        public List<DbType> FilteredList = new();
        public static SortState CurrentSortState = new();
        public List<PropertyInfo> AllProperties = new();
        public List<List<PropertyInfo>> UniqueProperties = new();
        public List<PropertyInfo> ToStringProperties = new();
        public List<StaticDbFilter> Filter = new();
        public bool DelayPL = false;

        public string Path { get { return MainWindow.dataDirectory + Table.ToLower() + ".json"; } }

        [NotMapped][JsonIgnore] public List<DbType> IDList
        {
            get
            {
                List<DbType> _idList = new();
                foreach (DbType _obj in List) { if (_obj.ID > Basics.NoID) { _idList.Add(_obj); } }
                return _idList;
            }
        }

        public void DeleteNotUnique()
        {
            bool _delayPL = DelayPL; DelayPL = true;
            for (int objIndex0 = List.Count - 1; objIndex0 >= 0; objIndex0--)
            {
                int newID = Basics.NoID;
                for (int index = 0; index < UniqueProperties.Count; index++)
                {
                    if (UniqueProperties.Count > index && UniqueProperties[index].Count > 0)
                    {
                        for (int objIndex = 0; objIndex < List.Count; objIndex++)
                        {
                            if (objIndex != objIndex0)
                            {
                                bool identical = true;
                                foreach (PropertyInfo property in UniqueProperties[index])
                                {
                                    if (Basics.GetCastedValue(List[objIndex0], property) != Basics.GetCastedValue(List[objIndex], property))
                                    {
                                        identical = false;
                                        break;
                                    }
                                }
                                if (identical) { newID = List[objIndex].ID; break; }
                            }
                        }
                    }
                    if (newID != Basics.NoID) { break; }
                }
                if (newID != Basics.NoID)
                {
                    List[objIndex0].RemoveAllChilds(newID);
                    if (!List[objIndex0].IsChild()) { List.Remove(List[objIndex0]); }
                }
            }
            if (!_delayPL) { FilterList(); PublishList(); } DelayPL = _delayPL;
        }

        public void ListRemoveAt(int index, bool forceDel = false)
        {
            if (List.Count > index)
            {
                if (forceDel) { List[index].RemoveAllChilds(); }
                if (!List[index].IsChild()) { List.RemoveAt(index); if (!DelayPL) { FilterList(); PublishList(); } }
            }
        }

        public void ListClear(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            List<DbType> iterateList = new();
            foreach (DbType _obj in List) { iterateList.Add(_obj); }
            foreach (DbType _obj in iterateList)
            {
                if (forceDel) { _obj.RemoveAllChilds(); }
                if (!_obj.IsChild()) { List.Remove(_obj); }
            }
            DelayPL = _delayPL; if (!DelayPL) { FilterList(); PublishList(); }
        }

        public void ReadJson(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            ListClear(forceDel);
            JsonConvert.DeserializeObject<DbType[]>(File.ReadAllText(Path, Encoding.Unicode));
            DelayPL = _delayPL; if (!DelayPL) { SortList(); FilterList(); PublishList(); }
        }

        public void WriteJson()
        {
            bool _delayPL = DelayPL; DelayPL = true;
            SortList();
            string text = JsonConvert.SerializeObject(List, Formatting.Indented);
            File.WriteAllText(Path, text, Encoding.Unicode);
            DelayPL = _delayPL; if (!DelayPL) { FilterList(); PublishList(); }
        }

        public void LoadSQL(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            ListClear(forceDel);
            string SqlQry = "SELECT * FROM " + Table + ";";
            try { SQL.Connection.Query<DbType>(SqlQry); }
            catch { MainVM.List[0].LogCurrentText = "Loading SQL table '" + Table + "' failed!"; }
            SortList(); DelayPL = _delayPL; if (!DelayPL) { FilterList(); PublishList(); }
        }

        public List<DbType> GetBySQL(List<string> propertyNames, List<dynamic> values)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            List<DbType> listObj = new();
            string SqlQry = "SELECT * FROM " + Table + " WHERE ";
            if (propertyNames.Count == values.Count && propertyNames.Count > 0)
            {
                for (int index = 0; index < propertyNames.Count; index++)
                {
                    if (values[index] is DateTime)
                    {
                        values[index] = Basics.Date2String(values[index], "YYYY-MM-DD hh:mm:ss");
                        SqlQry += "DATEADD(ms, -DATEPART(ms, " + propertyNames[index] + "), " + propertyNames[index] + ")" + " = '" + values[index] + "' AND ";
                    }
                    else { SqlQry += propertyNames[index] + " = '" + values[index] + "' AND "; }
                }
                SqlQry = SqlQry[..^5] + ";";
                try { listObj = SQL.Connection.Query<DbType>(SqlQry).ToList(); }
                catch { MainVM.List[0].LogCurrentText = "Loading object from SQL table '" + Table + "' failed!"; }
            }
            else { MainVM.List[0].LogCurrentText = "Loading object from SQL table '" + Table + "' failed!"; }
            DelayPL = _delayPL; if (!DelayPL) { FilterList(); PublishList(); }
            return listObj;
        }

        public void WriteSQL()
        {
            bool _delayPL = DelayPL; DelayPL = true;
            List<DbType> currentListSQL = new();
            List<dynamic> objList = SQL.LoadSQL(Table);
            for (int objNr = 0; objNr < objList.Count; objNr++)
            {
                DbType newObj = (DbType)Activator.CreateInstance(VarDbType, false, false)!;
                currentListSQL.Add(newObj);
                foreach (PropertyInfo property in AllProperties)
                {
                    foreach (var prop in objList[objNr])
                    {
                        if (property.Name == prop.Key.ToString())
                        {
                            var value = prop.Value;
                            property.SetValue(newObj, value);
                            break;
                        }
                    }
                }
            }
            foreach (DbType oldObj in currentListSQL)
            {
                bool breakLoop = false;
                foreach (DbType newObj in List) { if (newObj.ID == oldObj.ID) { breakLoop = true; break; } }
                if (!breakLoop) { SQL.DelSQL(Table, oldObj); }
            }
            foreach (DbType newObj in List)
            {
                bool breakLoop = false;
                foreach (DbType oldObj in currentListSQL) { if (newObj.ID == oldObj.ID) { breakLoop = true; break; } }
                if (breakLoop) { SQL.UpdateSQL(Table, newObj); }
                else { SQL.AddSQL(Table, newObj); }
            }
            LoadSQL();
            DelayPL = _delayPL; if (!DelayPL) { FilterList(); PublishList(); }
        }

        public DbType WriteSQL(DbType _obj)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            if (List.Contains(_obj)) { List.Remove(_obj); }
            if (_obj.ID == Basics.NoID) { SQL.AddSQL(Table, _obj); } else { SQL.UpdateSQL(Table, _obj); }
            Dictionary<PropertyInfo, dynamic> _dict = _obj.ReturnAsDict(false, false, false, false);
            List<string> propertyNames = new(); List<dynamic> values = new();
            foreach (PropertyInfo prop in _dict.Keys)
            {
                propertyNames.Add(prop.Name);
                values.Add(_dict[prop]);
            }
            List<DbType> listObj = new();
            if (_obj.ID == Basics.NoID && _dict.Count > 0) { listObj = GetBySQL(propertyNames, values); } else { listObj.Add(GetByIdSQL(_obj.ID)); }
            DelayPL = _delayPL; if (!DelayPL) { SortList(); FilterList(); PublishList(); }
            if (listObj.Count > 0) { return listObj[0]; } else { return _obj; }
        }

        public void ResetSQL(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            ListClear(forceDel);
            WriteSQL();
            if (List.Count == 0) { SQL.ReseedSQL(Table, 0); }
            DelayPL = _delayPL; if (!DelayPL) { FilterList(); PublishList(); }
        }

        public DbType GetByIdSQL(int id)
        {
            List<DbType> listObj = GetBySQL(new List<string> { "ID" }, new List <dynamic> { id });
            if (listObj.Count > 0) { return listObj[0]; } else { return GetByID(id); }
        }

        public DbType GetByID(int id)
        {
            if (id > Basics.NoID) { foreach (DbType _obj in List) { if (_obj.ID == id) { return _obj; } } }
            return (DbType)Activator.CreateInstance(VarDbType, false, false)!;
        }

        public DbType GetByUniqProp(List<dynamic> values, int index = 0)
        {
            if (UniqueProperties.Count > index && UniqueProperties[index].Count > 0 && UniqueProperties[index].Count == values.Count)
            {
                foreach (DbType _obj in List)
                {
                    bool found = true;
                    for (int propertyNr = 0; propertyNr < UniqueProperties[index].Count; propertyNr++)
                    {
                        if (Basics.GetCastedValue(_obj, UniqueProperties[index][propertyNr]) != Basics.CastValue(UniqueProperties[index][propertyNr], values[propertyNr]))
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found) { return _obj; }
                }
            }
            return (DbType)Activator.CreateInstance(VarDbType, false, false)!;
        }

        public DbType GetByUniqProp(dynamic _value, int index = 0)
        {
            if (UniqueProperties.Count > index && UniqueProperties[index].Count == 1)
            {
                return GetByUniqProp(new List<dynamic>() { _value }, index);
            }
            return (DbType)Activator.CreateInstance(VarDbType, false, false)!;
        }

        public List<DbType> GetBy(List<PropertyInfo> properties, List<dynamic> values)
        {
            List<DbType> _list = new();
            if (properties.Count > 0 && properties.Count == values.Count)
            {
                foreach (DbType _obj in List)
                {
                    bool found = true;
                    for (int propertyNr = 0; propertyNr < properties.Count; propertyNr++)
                    {
                        if (Basics.GetCastedValue(_obj, properties[propertyNr]) != Basics.CastValue(properties[propertyNr], values[propertyNr])) { found = false; break; }
                    }
                    if (found) { _list.Add(_obj); }
                }
            }
            return _list;
        }

        public List<DbType> GetBy(List<string> propertyNames, List<dynamic> values)
        {
            List<DbType> _list = new();
            if (propertyNames.Count > 0 && propertyNames.Count == values.Count)
            {
                List<PropertyInfo> properties = new();
                List<dynamic> newValues = new();
                foreach (PropertyInfo property in AllProperties)
                {
                    if (propertyNames.Contains(property.Name))
                    {
                        properties.Add(property);
                        newValues.Add(values[propertyNames.IndexOf(property.Name)]);
                    }
                }
                return GetBy(properties, newValues);
            }
            return _list;
        }

        public dynamic GetBy(PropertyInfo property, dynamic _value)
        {
            return GetBy(new List<PropertyInfo>() { property }, new List<dynamic>() { _value });
        }

        public dynamic GetBy(string propertyName, dynamic? _value)
        {
            return GetBy(new List<string>() { propertyName }, new List<dynamic>() { _value });
        }

        public bool ExistsID(int id)
        {
            if (id != Basics.NoID && GetByID(id).ID == id) { return true; } else { return false; }
        }

        public bool ExistsUniqProp(List<dynamic> values, int index = 0)
        {
            if (GetByUniqProp(values, index).ReadyForList) { return true; } else { return false; }
        }

        public bool ExistsUniqProp(dynamic _value, int index = 0)
        {
            if (GetByUniqProp(_value, index).ReadyForList) { return true; } else { return false; }
        }

        public bool Exists(List<PropertyInfo> properties, List<dynamic> values)
        {
            if (GetBy(properties, values).Count == 0) { return false; } else { return true; }
        }

        public bool Exists(List<string> propertyNames, List<dynamic> values)
        {
            if (GetBy(propertyNames, values).Count == 0) { return false; } else { return true; }
        }

        public bool Exists(PropertyInfo property, dynamic _value)
        {
            if (GetBy(property, _value).Count == 0) { return false; } else { return true; }
        }

        public bool Exists(string propertyName, dynamic _value)
        {
            if (GetBy(propertyName, _value).Count == 0) { return false; } else { return true; }
        }

        public bool IsUniqProperty(string propertyName)
        {
            foreach (List<PropertyInfo> propertyList in UniqueProperties)
            {
                foreach (PropertyInfo property in propertyList)
                {
                    if (property.Name == propertyName) { return true; }
                }
            }
            return false;
        }

        public void FilterList()
        {
            FilteredList = new();
            foreach (var _obj in List)
            {
                bool notFiltered = true;
                foreach (PropertyInfo property in AllProperties)
                {
                    foreach (StaticDbFilter _filter in Filter)
                    {
                        if (_filter.PropertyName == property.Name)
                        {
                            if (_filter.Filter != "")
                            {
                                string[] limits = _filter.Filter.Split(':');
                                if (limits.Length == 2 && int.TryParse(limits[0], out int _minValue) && int.TryParse(limits[1], out int _maxValue) && int.TryParse(property.GetValue(_obj)?.ToString(), out int _value))
                                {
                                    if (_value > _maxValue || _value < _minValue) { notFiltered = false; }
                                }
                                else if (!Basics.GetCastedValue(_obj, property).ToString().ToLower().Contains(_filter.Filter.ToLower())) { notFiltered = false; }
                            }
                            break;
                        }
                    }
                    if (!notFiltered) { break; }
                }
                if (notFiltered) { FilteredList.Add(_obj); }
            }
        }

        public void SortFilteredList(PropertyInfo property)
        {
            List<string> numericalTypes = new() { "System.Int16", "System.Int32", "System.Int64", "System.UInt16", "System.UInt32", "System.UInt64", "System.Single", "System.Double", "System.Decimal", "System.DateTime" };
            bool stringCompare = true;
            if (numericalTypes.Contains(property.PropertyType.ToString())) { stringCompare = false; }
            if (CurrentSortState.Property == property) { CurrentSortState.SortAscending = !CurrentSortState.SortAscending; }
            else { CurrentSortState = new SortState { Property = property }; }
            for (int rowNr1 = 0; rowNr1 < FilteredList.Count - 1; rowNr1++)
            {
                for (int rowNr2 = rowNr1 + 1; rowNr2 < FilteredList.Count; rowNr2++)
                {
                    bool smallerValueIsFirst;
                    dynamic val1 = Basics.GetCastedValue(FilteredList[rowNr1], property);
                    dynamic val2 = Basics.GetCastedValue(FilteredList[rowNr2], property);
                    if (stringCompare) { smallerValueIsFirst = String.Compare(val1.ToString(), val2.ToString()) < 0; }
                    else { smallerValueIsFirst = val1 < val2; }
                    if ((CurrentSortState.SortAscending && !smallerValueIsFirst) || (!CurrentSortState.SortAscending && smallerValueIsFirst))
                    {
                        (FilteredList[rowNr1], FilteredList[rowNr2]) = (FilteredList[rowNr2], FilteredList[rowNr1]);
                    }
                }
            }
        }

        public void SortList()
        {
            bool _delayPL = DelayPL; DelayPL = true;
            for (int rowNr1 = 0; rowNr1 < List.Count - 1; rowNr1++)
            {
                for (int rowNr2 = rowNr1 + 1; rowNr2 < List.Count; rowNr2++)
                {
                    if (List[rowNr1].ID > List[rowNr2].ID)
                    {
                        (List[rowNr1], List[rowNr2]) = (List[rowNr2], List[rowNr1]);
                    }
                }
            }
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
        }
    }



    public abstract class DatabaseObject<DbType> : IDatabaseObject where DbType : IDatabaseObject, new()
    {
        private readonly StaticDbField<DbType> StaticFields = StaticFieldList.GetByType(typeof(DbType));
        private DbType _this;
        private bool readyForList = false;
        private int id = Basics.NoID;

        [NotMapped][JsonIgnore] public DbType This
        {
            get { return _this; }
            set { _this = value; }
        }

        [NotMapped][JsonIgnore] public List<DbType> List { get { return StaticFields.List; } }

        [NotMapped][JsonIgnore] public bool ReadyForList
        {
            get { return readyForList; }
            set { if (value != readyForList) { if (value) { SetNextAvailable(); } if (!value || IsUnique()) { readyForList = value; } } }
        }

        [JsonProperty(Order = int.MinValue)] public int ID
        {
            get { return id; }
            set
            {
                bool _delayPL = StaticFields.DelayPL; StaticFields.DelayPL = true;
                DbType oldObj = StaticFields.GetByID(value);
                int listIndex = StaticFields.List.IndexOf(oldObj);
                if (oldObj.ID != Basics.NoID && StaticFields.List.Contains(This)) { This.ListRemove(); StaticFields.List[listIndex] = This; }
                id = value;
                StaticFields.DelayPL = _delayPL; if (!StaticFields.DelayPL) { StaticFields.PublishList(); }
            }
        }

        [JsonIgnore] public int Nr
        {
            get
            {
                if (StaticFields.FilteredList.Contains(This)) { return StaticFields.FilteredList.IndexOf(This) + 1; }
                else if (StaticFields.List.Contains(This)) { return StaticFields.List.IndexOf(This) + 1; }
                else { return Basics.NoID; }
            }
        }

        public void Initialize(bool _readyForList, bool inList)
        {
            if (!File.Exists(StaticFields.Path)) { StaticFields.WriteJson(); }
            if (StaticFields.AllProperties.Count == 0)
            {
                foreach (PropertyInfo property in GetType().GetProperties())
                {
                    if (property.GetCustomAttributes(typeof(NotMappedAttribute), true).Count() == 0)
                    {
                        if (property.Name == nameof(DatabaseObject<DbType>.ID))
                        {
                            StaticFields.AllProperties.Insert(0, property);
                            StaticFields.Filter.Insert(0, new StaticDbFilter(typeof(DbType), property) { Filter = Basics.NoID.ToString() });
                        }
                        else
                        {
                            StaticFields.AllProperties.Add(property);
                            StaticFields.Filter.Add(new StaticDbFilter(typeof(DbType), property));
                        }
                    }
                }
                foreach (List<string> propertyNameList in StaticFields.UniquePropertiesNames)
                {
                    List<PropertyInfo> propertyList = new();
                    foreach (string propertyName in propertyNameList)
                    {
                        foreach (PropertyInfo property in StaticFields.AllProperties)
                        {
                            if (property.Name == propertyName) { propertyList.Add(property); }
                        }
                    }
                    if (propertyList.Count > 0) { StaticFields.UniqueProperties.Add(propertyList); }
                }
                foreach (string propertyName in StaticFields.ToStringPropertiesNames)
                {
                    foreach (PropertyInfo property in StaticFields.AllProperties)
                    {
                        if (property.Name == propertyName) { StaticFields.ToStringProperties.Add(property); }
                    }
                }
            }
            if (inList) { ListAdd(); }
            else if (_readyForList) { ReadyForList = true; } else { ReadyForList = false; }
        }

        public void ListAdd()
        {
            ReadyForList = true;
            if (ReadyForList && !StaticFields.List.Contains(This))
            {
                StaticFields.List.Add(This);
                if (!StaticFields.DelayPL) { StaticFields.FilterList(); StaticFields.PublishList(); }
            }
        }

        public void ListInsert(int index)
        {
            if (StaticFields.List.Count > index)
            {
                ReadyForList = true;
                if (ReadyForList && !StaticFields.List.Contains(This))
                {
                    StaticFields.List.Insert(index, This);
                    if (!StaticFields.DelayPL) { StaticFields.FilterList(); StaticFields.PublishList(); }
                }
            }
        }

        public void ListRemove(bool forceDel = false)
        {
            if (StaticFields.List.Contains(This))
            {
                if (forceDel) { RemoveAllChilds(); }
                if (!IsChild()) { StaticFields.List.Remove(This); StaticFields.FilterList(); if (!StaticFields.DelayPL) { StaticFields.PublishList(); } }
            }
        }

        public void DeleteSQL()
        {
            bool _delayPL = StaticFields.DelayPL; StaticFields.DelayPL = true;
            SQL.DelSQL(StaticFields.Table, this);
            ListRemove(true);
            StaticFields.DelayPL = _delayPL; StaticFields.FilterList();if (!StaticFields.DelayPL) { StaticFields.PublishList(); }
        }

        public abstract void SetNextAvailable();

        public Dictionary<PropertyInfo, dynamic> ReturnAsDict(bool retID, bool retJsonIgnore, bool retCanNotWrite, bool retNotUnique)
        {
            Dictionary<PropertyInfo, dynamic> dict = new();
            foreach (PropertyInfo property in StaticFields.AllProperties)
            {
                bool ret = true;
                if (!property.CanRead) { ret = false; }
                else if (!retID && property.Name == "ID") { ret = false; }
                else if (!retJsonIgnore && property.GetCustomAttributes(false).OfType<JsonIgnoreAttribute>().Any()) { ret = false; }
                else if (!retCanNotWrite && !property.CanWrite) { ret = false; }
                else if (!retNotUnique && !StaticFields.IsUniqProperty(property.Name)) { ret = false; }
                if (ret) { dict[property] = property.GetValue(this)!; }
            }
            return dict;
        }

        public bool IsUnique()
        {
            for (int index = 0; index < StaticFields.UniqueProperties.Count; index++)
            {
                if (!IsUnique(index)) { return false; }
            }
            return true;
        }

        public bool IsUnique(int index)
        {
            if (StaticFields.UniqueProperties.Count > index && StaticFields.UniqueProperties[index].Count > 0)
            {
                int objIndex0 = -1;
                if (StaticFields.List.Contains(This)) { objIndex0 = StaticFields.List.IndexOf(This); }
                for (int objIndex = 0; objIndex < StaticFields.List.Count; objIndex++)
                {
                    if (objIndex != objIndex0)
                    {
                        bool identical = true;
                        foreach (PropertyInfo property in StaticFields.UniqueProperties[index])
                        {
                            if (Basics.GetCastedValue(this, property) != Basics.GetCastedValue(StaticFields.List[objIndex], property)) { identical = false; break; }
                        }
                        if (identical) { return false; }
                    }
                }
            }
            return true;
        }

        public bool IsChild()
        {
            if (ID == Basics.NoID) { return false; }
            string idPropertyName = StaticFields.VarDbType.Name + "ID";
            List<dynamic> listTempParents = new();
            foreach (dynamic _type in StaticFieldList.List)
            {
                if (_type.VarDbType != StaticFields.VarDbType)
                {
                    foreach (PropertyInfo property in _type.AllProperties)
                    {
                        if (property.Name == idPropertyName)
                        {
                            var listParents = _type.GetBy(idPropertyName, ID);
                            foreach (dynamic _parent in listParents) { if (_parent.ID == Basics.NoID) { listTempParents.Add(_parent); } else { return true; } }
                            break;
                        }
                    }
                }
            }
            foreach (dynamic _parent in listTempParents) { _parent.ListRemove(); }
            return false;
        }

        public void RemoveAllChilds(int newID = -1)
        {
            if (ID != Basics.NoID)
            {
                bool delete;
                if (newID == -1) { delete = true; }
                else { if (StaticFields.ExistsID(newID)) { delete = false; } else { delete = true; } }
                string idPropertyName = StaticFields.VarDbType.Name + "ID";
                foreach (dynamic _type in StaticFieldList.List)
                {
                    if (_type.VarDbType != StaticFields.VarDbType)
                    {
                        foreach (PropertyInfo property in _type.AllProperties)
                        {
                            if (property.Name == idPropertyName)
                            {
                                var listParents = _type.GetBy(idPropertyName, ID);
                                foreach (dynamic _parent in listParents)
                                {
                                    if (delete) { _parent.ListRemove(true); }
                                    else { property.SetValue(_parent, newID); }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            string str = ID.ToString() + ".";
            if (StaticFields.ToStringProperties.Count == 0) { return str; }
            foreach (PropertyInfo property in StaticFields.ToStringProperties)
            {
                string? strAdd = null;
                if (property.PropertyType.ToString() == "System.Int32")
                {
                    int _id = Basics.GetCastedValue(This, property);
                    dynamic _statics = StaticFieldList.GetByIDProperty(property.Name);
                    if (_statics != null)
                    {
                        var _obj = StaticFieldList.GetByIDProperty(property.Name).GetByID(_id);
                        if (_obj.ID != Basics.NoID) { strAdd = _obj.ToString(); }
                    }
                }
                strAdd ??= property.GetValue(This).ToString();
                str += " " + strAdd + " |";
            }
            return Basics.SubStr(str, 0, str.Length - 2);
        }
    }



    public class StaticDbFilter
    {
        private Type dbType;
        private PropertyInfo property;
        private string filter = "";

        public StaticDbFilter(Type _type, PropertyInfo _property) { dbType = _type; property = _property; SortCmd = new UICmd((o) => Sort()); }

        public string PropertyName { get { if (property is not null) { return property.Name; } else { return ""; } } }

        public string Filter
        {
            get { return filter; }
            set
            {
                if (filter != value)
                {
                    filter = value;
                    var _statics = StaticFieldList.GetByType(dbType);
                    if (_statics is not null && _statics.Filter[0].PropertyName != PropertyName)
                    {
                        if (filter == "")
                        {
                            bool noFilter = true;
                            foreach (StaticDbFilter _staticDbFilter in _statics.Filter) { if (_staticDbFilter.Filter != filter) { noFilter = false; break; } }
                            if (noFilter) { _statics.Filter[0].Filter = Basics.NoID.ToString(); RaisePropertyChanged_Filter0(); }
                        }
                        else { if (_statics.Filter[0].Filter == Basics.NoID.ToString()) { _statics.Filter[0].Filter = ""; RaisePropertyChanged_Filter0(); } }
                    }
                    PublishFilter();
                }
            }
        }

        public void PublishFilter()
        {
            StaticFieldList.GetByType(dbType)?.FilterList();
            if (DatabaseVM.Instance is not null) { DatabaseVM.Instance.ResetList(); }
        }

        public void Sort()
        {
            var _statics = StaticFieldList.GetByType(dbType);
            if (_statics is not null)
            {
                _statics.SortFilteredList(property);
                if (DatabaseVM.Instance is not null) { DatabaseVM.Instance.ResetList(); }
            }
        }

        public void RaisePropertyChanged_Filter0()
        {
            if (DatabaseVM.Instance is not null) { DatabaseVM.Instance.RaisePropertyChanged_Filter(0); }
        }

        [JsonIgnore] public UICmd SortCmd { get; set; }
    }



    public class SortState
    {
        public SortState() { }

        public bool SortAscending = true;
        public PropertyInfo? Property = null;

    }
}
