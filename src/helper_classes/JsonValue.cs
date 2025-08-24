using System.Text;
using System;
using System.Collections.Generic;
using Godot;
public class JsonValue
{
    VarType varType;
    string valueStored;
    Dictionary<string, JsonValue>? content = null;
    List<JsonValue>? list = null;

    #region Enums
    enum VarType : byte
    {
        String,
        Int,
        Decimal,
        Bool,
        Array,
        Object,
        Null,
        //Undefined,
    }

    enum ContainerEnum
    {
        SettingKey,
        AddingValue
    }

    #endregion
    public JsonValue this[int index]
    {
        get
        {
            varType = VarType.Array;
            if (list is null)
                list = new List<JsonValue>();
            if (index < 0)
            {
                return new JsonValue(VarType.Null);
            }
            else if (index == list.Count)
            {
                for (int i = list.Count; i <= index; i++)
                    Append(new JsonValue(VarType.Null));
            }
            else if (index > list.Count)
                return new JsonValue(VarType.Null);

            return list[index];
        }
        set
        {
            varType = VarType.Array;
            if (list is null)
                list = new List<JsonValue>();
            if (index < 0 || index > list.Count)
                return;
            list[index] = value;
        }
    }
    public JsonValue this[string key]
    {
        get
        {
            varType = VarType.Object;
            if (content is null)
                content = new Dictionary<string, JsonValue>();
            if (!content.ContainsKey(key))
            {
                Add(key, new JsonValue(VarType.Null));
                return content[key];
            }
            return content[key];
        }
        set
        {
            varType = VarType.Object;
            Add(key, value);
        }
    }


    public bool IsNull { get { return varType == VarType.Null; } }
    public bool IsObject { get { return varType == VarType.Object; } }
    public bool IsArray { get { return varType == VarType.Array; } }
    public bool IsValue { get { return !IsArray && !IsObject && !IsNull; } }
    public bool IsBool { get { return varType == VarType.Bool; } }
    public bool IsString { get { return varType == VarType.String; } }
    public bool IsInt { get { return varType == VarType.Int; } }
    public bool IsUInt { get { return varType == VarType.Int && valueStored[0] != '-'; } }
    public bool IsDecimal { get { return varType == VarType.Decimal; } }
    public bool IsVector2
    {
        get
        {
            if (varType == VarType.Array)
            {
                if (list.Count != 2) return false;
                if (list[0].IsDecimal || list[0].IsInt && list[1].IsDecimal || list[1].IsInt)
                    return true;
            }
            return false;
        }
    }
    public bool IsVector3
    {
        get
        {
            if (varType == VarType.Array)
            {
                if (list.Count != 3) return false;
                if (list[0].IsDecimal || list[0].IsInt && list[1].IsDecimal || list[1].IsInt && list[2].IsDecimal || list[2].IsInt)
                    return true;
            }
            return false;
        }
    }
    public bool IsColor
    {
        get
        {
            if(varType == VarType.Array)
            {
                if (list.Count != 4) return false;
                if (list[0].IsDecimal || list[0].IsInt && list[1].IsDecimal || list[1].IsInt && list[2].IsDecimal || list[2].IsInt && list[3].IsDecimal || list[3].IsInt)
                    return true;
            }
            return false;
        }
    }


    public List<JsonValue> Array
    {
        get
        {
            if (list is not null)
                return list;
            return new List<JsonValue>();
        }
        set
        {
            varType = VarType.Array;
            list = value;
        }
    }

    public Dictionary<string, JsonValue> Object
    {
        get
        {
            if (content is not null)
                return content;
            return new Dictionary<string, JsonValue>();
        }
        set
        {
            varType = VarType.Object;
            content = value;
        }
    }

    /// <summary> Returns number of objects directly contained in this object</summary>
    public int Count
    {
        get
        {
            if (IsNull)
                return 0;
            if (IsArray)
                return list.Count;
            if (IsObject)
                return content.Count;

            return 1;
        }
    }


    #region Constructors

    public JsonValue() { InitializeJson(); }
    JsonValue(VarType type)
    {
        InitializeJson();
        varType = type;
    }

    public JsonValue(string value)
    {
        InitializeJson();
        Set(value);
    }
    public JsonValue(int value)
    {
        InitializeJson();
        Set(value);
    }
    public JsonValue(float value)
    {
        InitializeJson();
        Set(value);
    }
    public JsonValue(double value)
    {
        InitializeJson();
        Set(value);
    }
    public JsonValue(bool value)
    {
        InitializeJson();
        Set(value);
    }
    #endregion

    void InitializeJson()
    {
        valueStored = "";
        varType = VarType.Null;

        content = null;
        list = null;
    }

    /// <summary> Removes all data associated with this object. </summary>
    public void Clear()
    {
        InitializeJson();
    }

    /// <returns> Associated data for this objects as a string. </returns>
    public string AsString()
    {
        if (valueStored.Length == 0) return "";
        return valueStored;
    }
    /// <returns> Associated data for this objects as a int. </returns>
    public int AsInt()
    {
        int result;
        try { result = int.Parse(valueStored); }
        catch { result = 0; }
        return result;
    }
    /// <returns> Associated data for this objects as a uint. </returns>
    public uint AsUInt()
    {
        uint result;
        try { result = uint.Parse(valueStored); }
        catch { result = 0; }
        return result;
    }
    /// <returns> Associated data for this objects as a double. </returns>
    public double AsDouble()
    {
        double result;
        try { result = double.Parse(valueStored); }
        catch { result = 0; }

        return result;
    }
    /// <returns> Associated data for this objects as a float. </returns>
    public float AsFloat()
    {
        float result;
        try { result = float.Parse(valueStored); }
        catch { result = 0; }
        return result;
    }
    /// <returns> Associated data for this objects as a bool. </returns>
    public bool AsBool()
    {
        if (valueStored.Equals("true"))
            return true;
        return false;
    }

    public Vector3 AsVector3()
    {
        if (!IsVector3)
            return new Vector3();
        return new Vector3(list[0].AsFloat(), list[1].AsFloat(), list[2].AsFloat());
    }

    public Vector2 AsVector2()
    {
        if (!IsVector2)
            return new Vector2();
        return new Vector2(list[0].AsFloat(), list[1].AsFloat());
    }
    public Color AsColor()
    {
        if (!IsColor)
            return new Color();
        return new Color(list[0].AsFloat(), list[1].AsFloat(), list[2].AsFloat(), list[3].AsFloat());
    }
    public void Set(JsonValue obj)
    {
        if (obj is null)
            return;

        this.list = obj.list;
        this.content = obj.content;
        this.varType = obj.varType;
        this.valueStored = obj.valueStored;
    }
    public void Set(string value) { Set<string>(value); }
    public void Set(bool value) { Set<bool>(value); }
    public void Set(int value) { Set<int>(value); }
    public void Set(uint value) { Set<uint>(value); }
    public void Set(float value) { Set<float>(value); }
    public void Set(double value) { Set<double>(value); }
    public void Set(decimal value) { Set<decimal>(value); }
    public void Set(Vector3 value) { Set<Vector3>(value); }
    public void Set(Vector2 value) { Set<Vector2>(value); }
    public void Set(Color color) { Set<Color>(color); }
    void Set<T>(T value)
    {
        if (value is null)
            return;
        InitializeJson();
        valueStored = value.ToString();


        if (value is string)
            varType = VarType.String;
        else if (value is int || value is uint)
            varType = VarType.Int;
        else if (value is double || value is float || value is decimal)
            varType = VarType.Decimal;
        else if (value is bool)
        {
            varType = VarType.Bool;
            valueStored = valueStored.ToLower();
        }
        else if (value is Vector3 v3)
        {
            varType = VarType.Array;
            Append(v3.X);
            Append(v3.Y);
            Append(v3.Z);
        }
        else if (value is Vector2 v2)
        {
            varType = VarType.Array;
            Append(v2.X);
            Append(v2.Y);
        }
        else if (value is Color c)
        {
            varType = VarType.Array;
            Append(c.R);
            Append(c.G);
            Append(c.B);
            Append(c.A);
        }
    }

    #region ADD
    public void Add(string key, JsonValue value)
    {
        if (value is null)
            return;
        if (content is null)
            content = new Dictionary<string, JsonValue>();
        if (content.ContainsKey(key))
            content[key] = value;
        else
        {
            content.Add(key, value);
            varType = VarType.Object;
        }
    }

    public void Add(string key, string value) { Add<string>(key, value); }
    public void Add(string key, bool value) { Add<bool>(key, value); }
    public void Add(string key, int value) { Add<int>(key, value); }
    public void Add(string key, uint value) { Add<uint>(key, value); }
    public void Add(string key, float value) { Add<float>(key, value); }
    public void Add(string key, double value) { Add<double>(key, value); }
    public void Add(string key, decimal value) { Add<decimal>(key, value); }
    public void Add(string key, Vector3 value) { Add<Vector3>(key, value); }
    public void Add(string key, Vector2 value) { Add<Vector2>(key, value); }
    void Add<T>(string key, T val)
    {
        if (val is null)
            return;
        JsonValue obj = new JsonValue();
        obj.Set(val);
        Add(key, obj);
    }

    #endregion

    #region REMOVE

    public void Remove(string key)
    {
        if (content is null)
            content = new Dictionary<string, JsonValue>();
        content.Remove(key);
        if (content.Count == 0)
        {
            varType = VarType.Null;
        }
    }
    public void Remove(int index)
    {
        if (list is null)
            list = new List<JsonValue>();
        if (index >= 0 && list.Count > index)
            list.RemoveAt(index);
        if (list.Count == 0)
            varType = VarType.Null;
    }

    public void Insert(int index, JsonValue obj)
    {
        if (obj is null)
            return;
        if (list is null)
            list = new List<JsonValue>();
        if (index >= 0 && list.Count > index)
        {
            varType = VarType.Array;
            list.Insert(index, obj);
        }
    }
    public void Insert(int index, string value) { Insert<string>(index, value); }
    public void Insert(int index, bool value) { Insert<bool>(index, value); }
    public void Insert(int index, int value) { Insert<int>(index, value); }
    public void Insert(int index, uint value) { Insert<uint>(index, value); }
    public void Insert(int index, float value) { Insert<float>(index, value); }
    public void Insert(int index, double value) { Insert<double>(index, value); }
    public void Insert(int index, decimal value) { Insert<decimal>(index, value); }
    public void Insert(int index, Vector3 value) { Insert<Vector3>(index, value); }
    public void Insert(int index, Vector2 value) { Insert<Vector2>(index, value); }
    void Insert<T>(int index, T val)
    {
        if (val is null)
            return;
        JsonValue obj = new JsonValue();
        obj.Set(val);
        Insert(index, obj);
    }


    #endregion

    #region APPEND

    public void Append(JsonValue value)
    {
        if (value is null)
            return;
        if (list is null)
            list = new List<JsonValue>();
        list.Add(value);
        varType = VarType.Array;
    }
    public void Append(string value) { Append<string>(value); }
    public void Append(bool value) { Append<bool>(value); }
    public void Append(int value) { Append<int>(value); }
    public void Append(uint value) { Append<uint>(value); }
    public void Append(float value) { Append<float>(value); }
    public void Append(double value) { Append<double>(value); }
    public void Append(decimal value) { Append<decimal>(value); }
    public void Append(Vector3 value) { Append<Vector3>(value); }
    public void Append(Vector2 value) { Append<Vector2>(value); }
    public void Append(Color value) { Append<Color>(value); }
    void Append<T>(T value)
    {
        if (value is null)
            return;
        JsonValue obj = new JsonValue();
        obj.Set(value);
        Append(obj);
    }

    #endregion


    #region SERIALIZATION

    /// <summary> Converts this JsonValue object into a json string. 
    /// This method should only be called on an Object value, an Array and Value will return "{}".</summary>
    public override string ToString()
    {
        return Serializer();
    }
    public string ToFormattedString()
    {
        if (varType != VarType.Array && varType != VarType.Object)
            return Serializer();

        return AddFormatting(Serializer());
    }
    /// <summary> Helper function for handling creating a json string of all connected JsonValue objects.</summary>
    string Serializer()
    {
        StringBuilder sb = new StringBuilder();
        switch (varType)
        {
            case VarType.Null:
                return "null";
            case VarType.String:
                {
                    // convert escaped characters to text
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append('\"');
                    for (int i = 0; i < valueStored.Length; i++)
                    {
                        switch (valueStored[i])
                        {
                            case '\n':
                                stringBuilder.Append("\\n");
                                continue;
                            case '\t':
                                stringBuilder.Append("\\t");
                                continue;
                            case '\\':
                                stringBuilder.Append("\\");
                                stringBuilder.Append("\\");
                                continue;
                            default:
                                stringBuilder.Append(valueStored[i]);
                                break;
                        }
                    }
                    stringBuilder.Append('\"');
                    return stringBuilder.ToString();
                }
            case VarType.Bool:
                {
                    {
                        if (valueStored.Equals("False") || valueStored.Equals("false"))
                            return "false";
                        else
                            return "true";
                    }
                }
            case VarType.Int:
            case VarType.Decimal:
                if (varType == VarType.Decimal && !valueStored.Contains('.'))
                    return valueStored + ".0";
                return valueStored;
            case VarType.Array:
                {
                    sb.Append('[');
                    foreach (JsonValue item in list)
                    {
                        if (item.IsNull)
                            continue;
                        sb.Append(item.Serializer());
                        sb.Append(',');
                    }
                    if (!sb.Equals("["))
                        sb.Length--;
                    sb.Append(']');

                    return sb.ToString();
                }
            case VarType.Object:
                {
                    sb.Append('{');
                    foreach (KeyValuePair<string, JsonValue> item in content)
                    {
                        if (item.Value.IsNull || (item.Value.IsArray && item.Value.Count == 0))
                            continue;

                        sb.Append('\"');
                        sb.Append(item.Key);
                        sb.Append("\":");
                        sb.Append(content[item.Key].Serializer());
                        sb.Append(',');

                    }
                    if (content.Count > 0 && !sb.Equals("{"))
                    {
                        sb.Length--;
                    }
                    sb.Append('}');

                    return sb.ToString();
                }
        }
        return "null";
    }

    #endregion

    #region DESERIALIZATION

    /// <summary>
    /// This method taked in a string and attempts to create a JsonValue obj to contain the data.
    /// </summary>
    /// <param name="data">Json string. Formatting will be removed within this function.</param>
    /// <returns>True if successful, False if unsuccessful.</returns>
    private bool Parser(string data)
    {
        varType = VarType.Object;
        int index = 1;
        string unformattedData = RemoveFormatting(data);
        try
        {
            Deserializer(unformattedData, ref index, VarType.Object);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static JsonValue Parse(string data)
    {
        JsonValue json = new JsonValue();
        json.Parser(data);
        return json;
    }

    /// <summary>
    /// Helper function for converting a string into a JsonValue object(s).
    /// </summary>
    void Deserializer(string data, ref int index, VarType type)
    {
        bool inString = false;
        switch (type)
        {
            case VarType.Object:
                {
                    ContainerEnum state = ContainerEnum.SettingKey;

                    string key = "";
                    while (data[index] != '}' || inString)
                    {
                        if (state == ContainerEnum.SettingKey)
                        {
                            int startIndex = index;
                            while (data[index] != ':' || inString)
                            {
                                UpdateInString(ref inString, data, index);
                                index++;
                            }
                            // set key, and remove qoutations surrounding it
                            key = data.Substring(startIndex + 1, index - startIndex - 2);
                            state = ContainerEnum.AddingValue;
                        }
                        else
                        {
                            //remove colon
                            index++;

                            JsonValue valToAdd = new JsonValue();
                            if (data[index] == '{')
                            {
                                index++;
                                valToAdd.Deserializer(data, ref index, VarType.Object);
                            }
                            else if (data[index] == '[')
                            {
                                index++;
                                valToAdd.Deserializer(data, ref index, VarType.Array);
                            }
                            else
                                valToAdd.Deserializer(data, ref index, VarType.Null);

                            Add(key, valToAdd);

                            state = ContainerEnum.SettingKey;
                        }

                        if (data[index] == ',')
                            index++;
                    }
                    index++;
                }
                break;
            case VarType.Array:
                {
                    while (data[index] != ']')
                    {
                        while (data[index] != ',' && data[index] != ']')
                        {

                            JsonValue valToAdd = new JsonValue();
                            if (data[index] == '{')
                            {
                                // need to move to creating
                                index++;
                                valToAdd.Deserializer(data, ref index, VarType.Object);
                            }
                            else if (data[index] == '[')
                            {
                                index++;
                                valToAdd.Deserializer(data, ref index, VarType.Array);
                            }
                            else
                                valToAdd.Deserializer(data, ref index, VarType.Null);

                            Append(valToAdd);

                        }
                        if (data[index] == ',')
                            index++;
                    }

                    index++;
                }
                break;
            default:
                {
                    StringBuilder value = new StringBuilder();
                    while (data[index] != ',' && data[index] != '}' && data[index] != ']' || inString)
                    {
                        UpdateInString(ref inString, data, index);
                        if (inString)
                        {
                            if (data[index] == '\\')
                            {
                                switch (data[index + 1])
                                {
                                    case 'n':
                                        index += 2;
                                        value.Append('\n');
                                        continue;
                                    case 't':
                                        index += 2;
                                        value.Append('\t');
                                        continue;
                                    case '\\':
                                        index += 2;
                                        value.Append('\\');
                                        continue;
                                }

                            }
                        }
                        value.Append(data[index]);
                        index++;
                    }

                    valueStored = value.ToString();
                    if (valueStored[0] == '\"')
                    {
                        // remove quotations from string
                        valueStored = valueStored.Substring(1, valueStored.Length - 2);
                        varType = VarType.String;
                    }
                    else if (valueStored == "null" || valueStored.Length == 0)
                        varType = VarType.Null;
                    else if (valueStored.Contains('.'))
                        varType = VarType.Decimal;
                    else if (valueStored.Equals("true") || valueStored.Equals("false"))
                        varType = VarType.Bool;
                    else
                        varType = VarType.Int;

                }
                break;
        }
    }

    #endregion
    /// <summary>
    /// Private function for handling determining when within a string value while
    /// deserializing from a json string.
    /// </summary>
    void UpdateInString(ref bool inString, string data, int index)
    {
        if (data[index] == '\"')
        {
            if (index > 0)
            {
                if (data[index - 1] != '\\')
                    inString = !inString;
            }
            else
                inString = !inString;
        }
    }

    #region FORMATTING
    /// <summary>
    /// Takes in a string and removes all special characters and spaces which are not
    /// within a value's string and returns the new unformatted string.
    /// </summary>
    static string RemoveFormatting(string data)
    {
        bool inString = false;
        var result = new StringBuilder(data.Length);
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == '\"')
            {
                if (data[i - 1] != '\\')
                    inString = !inString;
            }
            if (!inString)
            {
                if (data[i] != ' ' && data[i] != '\t' && data[i] != '\n' && data[i] != '\r')
                    result.Append(data[i]);
            }
            else
                result.Append(data[i]);

        }

        return result.ToString();

    }

    /// <summary>
    /// Takes in a string and adds on appropriate formatting to make a string more legible
    /// in a json file.
    /// </summary>
    static public string AddFormatting(string data)
    {
        StringBuilder result = new StringBuilder(data.Length);
        bool inString = false;
        const string TAB = "    ";
        int tabDepth = 1;
        result.Append(data[0].ToString() + '\n' + TAB);
        int i = 1;

        while (i < data.Length)
        {
            if (data[i] == '"')
            {
                if (data[i - 1] != '\\')
                    inString = !inString;
            }

            if (inString)
            {
                result.Append(data[i]);
                i++;
                continue;
            }

            switch (data[i])
            {
                case '{':
                case '[':
                    if (data[i - 1] != '[' && data[i - 1] != '{' && data[i - 1] != ',')
                    {
                        result.Append('\n');
                        for (int j = 0; j < tabDepth; j++)
                            result.Append(TAB);
                    }
                    result.Append(data[i].ToString() + '\n');
                    tabDepth++;
                    for (int j = 0; j < tabDepth; j++)
                        result.Append(TAB);
                    break;
                case ':':
                    result.Append(": ");
                    break;
                case ',':
                    result.Append(",\n");
                    for (int j = 0; j < tabDepth; j++)
                        result.Append(TAB);
                    break;
                case '}':
                case ']':
                    tabDepth--;
                    result.Append('\n');
                    for (int j = 0; j < tabDepth; j++)
                        result.Append(TAB);
                    result.Append(data[i].ToString());
                    break;
                default:
                    result.Append(data[i].ToString());
                    break;
            }

            i++;
        }
        return result.ToString();
    }

    #endregion

}
