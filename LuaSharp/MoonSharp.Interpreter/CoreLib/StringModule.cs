﻿// Disable warnings about XML documentation

#pragma warning disable 1591

using System;
using System.IO;
using System.Text;
using MoonSharp.Interpreter.CoreLib.StringLib;

namespace MoonSharp.Interpreter.CoreLib
{
    /// <summary>
    ///     Class implementing string Lua functions
    /// </summary>
    [MoonSharpModule(Namespace = "string")]
    public class StringModule
    {
        public const string BASE64_DUMP_HEADER = "MoonSharp_dump_b64::";

        public static void MoonSharpInit(Table globalTable, Table stringTable)
        {
            var stringMetatable = new Table(globalTable.OwnerScript);
            stringMetatable.Set("__index", DynValue.NewTable(stringTable));
            globalTable.OwnerScript.SetTypeMetatable(DataType.String, stringMetatable);
        }

        [MoonSharpModuleMethod]
        public static DynValue dump(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var fn = args.AsType(0, "dump", DataType.Function, false);

            try
            {
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    executionContext.GetScript().Dump(fn, ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    bytes = ms.ToArray();
                }
                var base64 = Convert.ToBase64String(bytes);
                return DynValue.NewString(BASE64_DUMP_HEADER + base64);
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }
        }

        [MoonSharpModuleMethod]
        public static DynValue @char(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var sb = new StringBuilder(args.Count);

            for (var i = 0; i < args.Count; i++)
            {
                var v = args[i];
                var d = 0d;

                if (v.Type == DataType.String)
                {
                    var nd = v.CastToNumber();
                    if (nd == null)
                        args.AsType(i, "char", DataType.Number, false);
                    else
                        d = nd.Value;
                }
                else
                {
                    args.AsType(i, "char", DataType.Number, false);
                    d = v.Number;
                }

                sb.Append((char) (d));
            }

            return DynValue.NewString(sb.ToString());
        }

        [MoonSharpModuleMethod]
        public static DynValue @byte(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var vs = args.AsType(0, "byte", DataType.String, false);
            var vi = args.AsType(1, "byte", DataType.Number, true);
            var vj = args.AsType(2, "byte", DataType.Number, true);

            return PerformByteLike(vs, vi, vj,
                i => Unicode2Ascii(i));
        }

        [MoonSharpModuleMethod]
        public static DynValue unicode(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var vs = args.AsType(0, "unicode", DataType.String, false);
            var vi = args.AsType(1, "unicode", DataType.Number, true);
            var vj = args.AsType(2, "unicode", DataType.Number, true);

            return PerformByteLike(vs, vi, vj, i => i);
        }

        private static int Unicode2Ascii(int i)
        {
            if (i >= 0 && i < 255)
                return i;

            return '?';
        }

        private static DynValue PerformByteLike(DynValue vs, DynValue vi, DynValue vj, Func<int, int> filter)
        {
            var range = StringRange.FromLuaRange(vi, vj, null);
            var s = range.ApplyToString(vs.String);

            var length = s.Length;
            var rets = new DynValue[length];

            for (var i = 0; i < length; ++i)
            {
                rets[i] = DynValue.NewNumber(filter(s[i]));
            }

            return DynValue.NewTuple(rets);
        }

        private static int? AdjustIndex(string s, DynValue vi, int defval)
        {
            if (vi.IsNil())
                return defval;

            var i = (int) Math.Round(vi.Number, 0);

            if (i == 0)
                return null;

            if (i > 0)
                return i - 1;

            return s.Length - i;
        }

        [MoonSharpModuleMethod]
        public static DynValue len(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var vs = args.AsType(0, "len", DataType.String, false);
            return DynValue.NewNumber(vs.String.Length);
        }

        [MoonSharpModuleMethod]
        public static DynValue match(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "match", KopiLua_StringLib.str_match);
        }

        [MoonSharpModuleMethod]
        public static DynValue gmatch(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "gmatch", KopiLua_StringLib.str_gmatch);
        }

        [MoonSharpModuleMethod]
        public static DynValue gsub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "gsub", KopiLua_StringLib.str_gsub);
        }

        [MoonSharpModuleMethod]
        public static DynValue find(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "find",
                KopiLua_StringLib.str_find);
        }

        [MoonSharpModuleMethod]
        public static DynValue lower(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var arg_s = args.AsType(0, "lower", DataType.String, false);
            return DynValue.NewString(arg_s.String.ToLower());
        }

        [MoonSharpModuleMethod]
        public static DynValue upper(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var arg_s = args.AsType(0, "upper", DataType.String, false);
            return DynValue.NewString(arg_s.String.ToUpper());
        }

        [MoonSharpModuleMethod]
        public static DynValue rep(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var arg_s = args.AsType(0, "rep", DataType.String, false);
            var arg_n = args.AsType(1, "rep", DataType.Number, false);
            var arg_sep = args.AsType(2, "rep", DataType.String, true);

            if (string.IsNullOrEmpty(arg_s.String) || (arg_n.Number < 1))
            {
                return DynValue.NewString("");
            }

            var sep = (arg_sep.IsNotNil()) ? arg_sep.String : null;

            var count = (int) arg_n.Number;
            var result = new StringBuilder(arg_s.String.Length*count);

            for (var i = 0; i < count; ++i)
            {
                if (i != 0 && sep != null)
                    result.Append(sep);

                result.Append(arg_s.String);
            }

            return DynValue.NewString(result.ToString());
        }

        [MoonSharpModuleMethod]
        public static DynValue format(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return executionContext.EmulateClassicCall(args, "format", KopiLua_StringLib.str_format);
        }

        [MoonSharpModuleMethod]
        public static DynValue reverse(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var arg_s = args.AsType(0, "reverse", DataType.String, false);

            if (string.IsNullOrEmpty(arg_s.String))
            {
                return DynValue.NewString("");
            }

            var elements = arg_s.String.ToCharArray();
            Array.Reverse(elements);

            return DynValue.NewString(new string(elements));
        }

        [MoonSharpModuleMethod]
        public static DynValue sub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var arg_s = args.AsType(0, "sub", DataType.String, false);
            var arg_i = args.AsType(1, "sub", DataType.Number, true);
            var arg_j = args.AsType(2, "sub", DataType.Number, true);

            var range = StringRange.FromLuaRange(arg_i, arg_j, -1);
            var s = range.ApplyToString(arg_s.String);

            return DynValue.NewString(s);
        }
    }
}