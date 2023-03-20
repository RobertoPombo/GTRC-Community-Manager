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

namespace GTRCLeagueManager.Database
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
        void SetNextAvailable();
        Dictionary<PropertyInfo, dynamic> ReturnAsDict(bool retID, bool retJsonIgnore, bool retCanNotWrite, bool retNotUnique);
        bool IsUnique();
        bool IsUnique(int index);
        bool IsChild();
        void RemoveAllChilds();
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
        public List<PropertyInfo> AllProperties = new();
        public List<List<PropertyInfo>> UniqueProperties = new();
        public List<PropertyInfo> ToStringProperties = new();
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

        public void ListRemoveAt(int index, bool forceDel = false)
        {
            if (List.Count > index)
            {
                if (forceDel) { List[index].RemoveAllChilds(); }
                if (!List[index].IsChild()) { List.RemoveAt(index); if (!DelayPL) { PublishList(); } }
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
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
        }

        public void ReadJson(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            ListClear(forceDel);
            JsonConvert.DeserializeObject<DbType[]>(File.ReadAllText(Path, Encoding.Unicode));
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
        }

        public void WriteJson()
        {
            bool _delayPL = DelayPL; DelayPL = true;
            string text = JsonConvert.SerializeObject(List, Formatting.Indented);
            File.WriteAllText(Path, text, Encoding.Unicode);
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
        }

        public void LoadSQL(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            ListClear(forceDel);
            string SqlQry = "SELECT * FROM " + Table + ";";
            try { SQL.Connection.Query<DbType>(SqlQry); }
            catch { MainVM.List[0].LogCurrentText = "Loading SQL table '" + Table + "' failed!"; }
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
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
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
        }

        public void ResetSQL(bool forceDel = false)
        {
            bool _delayPL = DelayPL; DelayPL = true;
            ListClear(forceDel);
            WriteSQL();
            if (List.Count == 0) { SQL.ReseedSQL(Table, 0); }
            DelayPL = _delayPL; if (!DelayPL) { PublishList(); }
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
            if (UniqueProperties[index].Count == 1)
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
            if (GetByID(id).ID == Basics.NoID) { return false; } else { return true; }
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
                DbType oldObj = StaticFields.GetByID(value);
                int listIndex = StaticFields.List.IndexOf(oldObj);
                if (oldObj.ID != Basics.NoID && StaticFields.List.Contains(This)) { This.ListRemove(); StaticFields.List[listIndex] = This; }
                id = value;
                if (!StaticFields.DelayPL) { StaticFields.PublishList(); }
            }
        }

        [JsonIgnore] public int Nr
        {
            get { if (StaticFields.List.Contains(This)) { return StaticFields.List.IndexOf(This) + 1; } else { return Basics.NoID; } }
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
                        if (property.Name == "ID") { StaticFields.AllProperties.Insert(0, property); }
                        else { StaticFields.AllProperties.Add(property); }
                    }
                }
                foreach (List<string> propertyNameList in StaticFields.UniquePropertiesNames)
                {
                    List<PropertyInfo> propertyList = new List<PropertyInfo>();
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

        public void ListAdd() { ReadyForList = true; if (ReadyForList) { StaticFields.List.Add(This); } }
        public void ListInsert(int index)
        {
            if (StaticFields.List.Count > index)
            {
                ReadyForList = true;
                StaticFields.List.Insert(index, This);
                if (!StaticFields.DelayPL) { StaticFields.PublishList(); }
            }
        }
        public void ListRemove(bool forceDel = false)
        {
            if (StaticFields.List.Contains(This))
            {
                if (forceDel) { RemoveAllChilds(); }
                if (!IsChild()) { StaticFields.List.Remove(This); if (!StaticFields.DelayPL) { StaticFields.PublishList(); } }
            }
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

        public void RemoveAllChilds()
        {
            if (ID != Basics.NoID)
            {
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
                                foreach (dynamic _parent in listParents) { _parent.ListRemove(true); }
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
}
