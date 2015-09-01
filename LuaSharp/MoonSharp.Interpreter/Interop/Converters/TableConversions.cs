﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace MoonSharp.Interpreter.Interop.Converters
{
    internal static class TableConversions
    {
        /// <summary>
        ///     Converts an IList to a Lua table.
        /// </summary>
        internal static Table ConvertIListToTable(Script script, IList list)
        {
            var t = new Table(script);
            for (var i = 0; i < list.Count; i++)
            {
                t[i + 1] = ClrToScriptConversions.ObjectToDynValue(script, list[i]);
            }
            return t;
        }

        /// <summary>
        ///     Converts an IDictionary to a Lua table.
        /// </summary>
        internal static Table ConvertIDictionaryToTable(Script script, IDictionary dict)
        {
            var t = new Table(script);

            foreach (DictionaryEntry kvp in dict)
            {
                var key = ClrToScriptConversions.ObjectToDynValue(script, kvp.Key);
                var val = ClrToScriptConversions.ObjectToDynValue(script, kvp.Value);
                t.Set(key, val);
            }

            return t;
        }

        /// <summary>
        ///     Determines whether the specified table can be converted to the specified type
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="t">The type.</param>
        /// <returns></returns>
        internal static bool CanConvertTableToType(Table table, Type t)
        {
            if (t.IsAssignableFrom(typeof (Dictionary<object, object>)))
                return true;
            if (t.IsAssignableFrom(typeof (Dictionary<DynValue, DynValue>)))
                return true;
            if (t.IsAssignableFrom(typeof (List<object>)))
                return true;
            if (t.IsAssignableFrom(typeof (List<DynValue>)))
                return true;
            if (t.IsAssignableFrom(typeof (object[])))
                return true;
            if (t.IsAssignableFrom(typeof (DynValue[])))
                return true;

            if (t.IsGenericType)
            {
                var generic = t.GetGenericTypeDefinition();

                if ((generic == typeof (List<>))
                    || (generic == typeof (IList<>))
                    || (generic == typeof (ICollection<>))
                    || (generic == typeof (IEnumerable<>)))
                {
                    return true;
                }
                if ((generic == typeof (Dictionary<,>))
                    || (generic == typeof (IDictionary<,>)))
                {
                    return true;
                }
            }

            if (t.IsArray && t.GetArrayRank() == 1)
                return true;

            return false;
        }

        /// <summary>
        ///     Converts a table to a CLR object of a given type
        /// </summary>
        internal static object ConvertTableToType(Table table, Type t)
        {
            if (t.IsAssignableFrom(typeof (Dictionary<object, object>)))
                return TableToDictionary(table, v => v.ToObject(), v => v.ToObject());
            if (t.IsAssignableFrom(typeof (Dictionary<DynValue, DynValue>)))
                return TableToDictionary(table, v => v, v => v);
            if (t.IsAssignableFrom(typeof (List<object>)))
                return TableToList(table, v => v.ToObject());
            if (t.IsAssignableFrom(typeof (List<DynValue>)))
                return TableToList(table, v => v);
            if (t.IsAssignableFrom(typeof (object[])))
                return TableToList(table, v => v.ToObject()).ToArray();
            if (t.IsAssignableFrom(typeof (DynValue[])))
                return TableToList(table, v => v).ToArray();

            if (t.IsGenericType)
            {
                var generic = t.GetGenericTypeDefinition();

                if ((generic == typeof (List<>))
                    || (generic == typeof (IList<>))
                    || (generic == typeof (ICollection<>))
                    || (generic == typeof (IEnumerable<>)))
                {
                    return ConvertTableToListOfGenericType(t, t.GetGenericArguments()[0], table);
                }
                if ((generic == typeof (Dictionary<,>))
                    || (generic == typeof (IDictionary<,>)))
                {
                    return ConvertTableToDictionaryOfGenericType(t, t.GetGenericArguments()[0],
                        t.GetGenericArguments()[1], table);
                }
            }

            if (t.IsArray && t.GetArrayRank() == 1)
                return ConvertTableToArrayOfGenericType(t, t.GetElementType(), table);

            return null;
        }

        /// <summary>
        ///     Converts a table to a <see cref="Dictionary{TKey,TValue}" />
        /// </summary>
        internal static object ConvertTableToDictionaryOfGenericType(Type dictionaryType, Type keyType, Type valueType,
            Table table)
        {
            if (dictionaryType.GetGenericTypeDefinition() != typeof (Dictionary<,>))
            {
                dictionaryType = typeof (Dictionary<,>);
                dictionaryType = dictionaryType.MakeGenericType(keyType, valueType);
            }

            var dic = (IDictionary) Activator.CreateInstance(dictionaryType);

            foreach (var kvp in table.Pairs)
            {
                var key = ScriptToClrConversions.DynValueToObjectOfType(kvp.Key, keyType, null, false);
                var val = ScriptToClrConversions.DynValueToObjectOfType(kvp.Value, valueType, null, false);

                dic.Add(key, val);
            }

            return dic;
        }

        /// <summary>
        ///     Converts a table to a T[]
        /// </summary>
        internal static object ConvertTableToArrayOfGenericType(Type arrayType, Type itemType, Table table)
        {
            var lst = new List<object>();

            for (int i = 1, l = table.Length; i <= l; i++)
            {
                var v = table.Get(i);
                var o = ScriptToClrConversions.DynValueToObjectOfType(v, itemType, null, false);
                lst.Add(o);
            }

            var array = (IList) Activator.CreateInstance(arrayType, lst.Count);

            for (var i = 0; i < lst.Count; i++)
                array[i] = lst[i];

            return array;
        }

        /// <summary>
        ///     Converts a table to a <see cref="List{T}" />
        /// </summary>
        internal static object ConvertTableToListOfGenericType(Type listType, Type itemType, Table table)
        {
            if (listType.GetGenericTypeDefinition() != typeof (List<>))
            {
                listType = typeof (List<>);
                listType = listType.MakeGenericType(itemType);
            }

            var lst = (IList) Activator.CreateInstance(listType);

            for (int i = 1, l = table.Length; i <= l; i++)
            {
                var v = table.Get(i);
                var o = ScriptToClrConversions.DynValueToObjectOfType(v, itemType, null, false);
                lst.Add(o);
            }

            return lst;
        }

        /// <summary>
        ///     Converts a table to a <see cref="List{T}" />, known in advance
        /// </summary>
        internal static List<T> TableToList<T>(Table table, Func<DynValue, T> converter)
        {
            var lst = new List<T>();

            for (int i = 1, l = table.Length; i <= l; i++)
            {
                var v = table.Get(i);
                var o = converter(v);
                lst.Add(o);
            }

            return lst;
        }

        /// <summary>
        ///     Converts a table to a Dictionary, known in advance
        /// </summary>
        internal static Dictionary<TK, TV> TableToDictionary<TK, TV>(Table table, Func<DynValue, TK> keyconverter,
            Func<DynValue, TV> valconverter)
        {
            var dict = new Dictionary<TK, TV>();

            foreach (var kvp in table.Pairs)
            {
                var key = keyconverter(kvp.Key);
                var val = valconverter(kvp.Value);

                dict.Add(key, val);
            }

            return dict;
        }
    }
}