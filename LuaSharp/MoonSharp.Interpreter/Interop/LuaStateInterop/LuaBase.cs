﻿// Disable warnings about XML documentation

#pragma warning disable 1591

using System;
using ptrdiff_t = System.Int32;
using lua_Integer = System.Int32;
using LUA_INTFRM_T = System.Int64;
using UNSIGNED_LUA_INTFRM_T = System.UInt64;

namespace MoonSharp.Interpreter.Interop.LuaStateInterop
{
    /// <summary>
    ///     Classes using the classic interface should inherit from this class.
    ///     This class defines only static methods and is really meant to be used only
    ///     from C# and not other .NET languages.
    ///     For easier operation they should also define:
    ///     using ptrdiff_t = System.Int32;
    ///     using lua_Integer = System.Int32;
    ///     using LUA_INTFRM_T = System.Int64;
    ///     using UNSIGNED_LUA_INTFRM_T = System.UInt64;
    /// </summary>
    public partial class LuaBase
    {
        protected const int LUA_TNONE = -1;
        protected const int LUA_TNIL = 0;
        protected const int LUA_TBOOLEAN = 1;
        protected const int LUA_TLIGHTUSERDATA = 2;
        protected const int LUA_TNUMBER = 3;
        protected const int LUA_TSTRING = 4;
        protected const int LUA_TTABLE = 5;
        protected const int LUA_TFUNCTION = 6;
        protected const int LUA_TUSERDATA = 7;
        protected const int LUA_TTHREAD = 8;
        protected const int LUA_MULTRET = -1;
        protected const string LUA_INTFRMLEN = "l";

        protected static DynValue GetArgument(LuaState L, int pos)
        {
            return L.At(pos);
        }

        protected static DynValue ArgAsType(LuaState L, int pos, DataType type, bool allowNil = false)
        {
            return GetArgument(L, pos)
                .CheckType(L.FunctionName, type, pos - 1,
                    allowNil
                        ? TypeValidationFlags.AllowNil | TypeValidationFlags.AutoConvert
                        : TypeValidationFlags.AutoConvert);
        }

        protected static int LuaType(LuaState L, int p)
        {
            switch (GetArgument(L, p).Type)
            {
                case DataType.Void:
                    return LUA_TNONE;
                case DataType.Nil:
                    return LUA_TNIL;
                case DataType.Boolean:
                    return LUA_TNIL;
                case DataType.Number:
                    return LUA_TNUMBER;
                case DataType.String:
                    return LUA_TSTRING;
                case DataType.Function:
                    return LUA_TFUNCTION;
                case DataType.Table:
                    return LUA_TTABLE;
                case DataType.UserData:
                    return LUA_TUSERDATA;
                case DataType.Thread:
                    return LUA_TTHREAD;
                case DataType.ClrFunction:
                    return LUA_TFUNCTION;
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                case DataType.Tuple:
                default:
                    throw new ScriptRuntimeException("Can't call LuaType on any type");
            }
        }

        protected static string LuaLCheckLString(LuaState L, int argNum, out uint l)
        {
            var str = ArgAsType(L, argNum, DataType.String, false).String;
            l = (uint) str.Length;
            return str;
        }

        protected static void LuaPushInteger(LuaState L, int val)
        {
            L.Push(DynValue.NewNumber(val));
        }

        protected static int LuaToBoolean(LuaState L, int p)
        {
            return GetArgument(L, p).CastToBool() ? 1 : 0;
        }

        protected static string LuaToLString(LuaState luaState, int p, out uint l)
        {
            return LuaLCheckLString(luaState, p, out l);
        }

        protected static string LuaToString(LuaState luaState, int p)
        {
            uint l;
            return LuaLCheckLString(luaState, p, out l);
        }

        protected static void LuaLAddValue(LuaLBuffer b)
        {
            b.StringBuilder.Append(b.LuaState.Pop().ToPrintString());
        }

        protected static void LuaLAddLString(LuaLBuffer b, CharPtr s, uint p)
        {
            b.StringBuilder.Append(s.ToString((int) p));
        }

        protected static void LuaLAddString(LuaLBuffer b, string s)
        {
            b.StringBuilder.Append(s);
        }

        protected static int LuaLOptInteger(LuaState L, int pos, int def)
        {
            var v = ArgAsType(L, pos, DataType.Number, true);

            if (v.IsNil())
                return def;
            return (int) v.Number;
        }

        protected static int LuaLCheckInteger(LuaState L, int pos)
        {
            var v = ArgAsType(L, pos, DataType.Number, false);
            return (int) v.Number;
        }

        protected static void LuaLArgCheck(LuaState L, bool condition, int argNum, string message)
        {
            if (!condition)
                LuaLArgError(L, argNum, message);
        }

        protected static int LuaLCheckInt(LuaState L, int argNum)
        {
            return LuaLCheckInteger(L, argNum);
        }

        protected static int LuaGetTop(LuaState L)
        {
            return L.Count;
        }

        protected static int LuaLError(LuaState luaState, string message, params object[] args)
        {
            throw new ScriptRuntimeException(message, args);
        }

        protected static void LuaLAddChar(LuaLBuffer b, char p)
        {
            b.StringBuilder.Append(p);
        }

        protected static void LuaLBuffInit(LuaState L, LuaLBuffer b)
        {
        }

        protected static void LuaPushLiteral(LuaState L, string literalString)
        {
            L.Push(DynValue.NewString(literalString));
        }

        protected static void LuaLPushResult(LuaLBuffer b)
        {
            LuaPushLiteral(b.LuaState, b.StringBuilder.ToString());
        }

        protected static void LuaPushLString(LuaState L, CharPtr s, uint len)
        {
            var ss = s.ToString((int) len);
            L.Push(DynValue.NewString(ss));
        }

        protected static void LuaLCheckStack(LuaState L, int n, string message)
        {
            // nop ?
        }

        protected static string LUA_QL(string p)
        {
            return "'" + p + "'";
        }

        protected static void LuaPushNil(LuaState L)
        {
            L.Push(DynValue.Nil);
        }

        protected static void LuaAssert(bool p)
        {
            // ??! 
            // A lot of KopiLua methods fall here in valid state!

            //if (!p)
            //	throw new InternalErrorException("LuaAssert failed!");
        }

        protected static string LuaLTypeName(LuaState L, int p)
        {
            return L.At(p).Type.ToErrorTypeString();
        }

        protected static int LuaIsString(LuaState L, int p)
        {
            var v = L.At(p);
            return (v.Type == DataType.String || v.Type == DataType.Number) ? 1 : 0;
        }

        protected static void LuaPop(LuaState L, int p)
        {
            for (var i = 0; i < p; i++)
                L.Pop();
        }

        protected static void LuaGetTable(LuaState L, int p)
        {
            // DEBT: this should call metamethods, now it performs raw access
            var key = L.Pop();
            var table = L.At(p);

            if (table.Type != DataType.Table)
                throw new NotImplementedException();

            var v = table.Table.Get(key);
            L.Push(v);
        }

        protected static int LuaLOptInt(LuaState L, int pos, int def)
        {
            return LuaLOptInteger(L, pos, def);
        }

        protected static CharPtr LuaLCheckString(LuaState L, int p)
        {
            uint dummy;
            return LuaLCheckLString(L, p, out dummy);
        }

        protected static string LuaLCheckStringStr(LuaState L, int p)
        {
            uint dummy;
            return LuaLCheckLString(L, p, out dummy);
        }

        protected static void LuaLArgError(LuaState L, int arg, string p)
        {
            throw ScriptRuntimeException.BadArgument(arg - 1, L.FunctionName, p);
        }

        protected static double LuaLCheckNumber(LuaState L, int pos)
        {
            var v = ArgAsType(L, pos, DataType.Number, false);
            return v.Number;
        }

        protected static void LuaPushValue(LuaState L, int arg)
        {
            var v = L.At(arg);
            L.Push(v);
        }

        /// <summary>
        ///     Calls a function.
        ///     To call a function you must use the following protocol: first, the function to be called is pushed onto the stack;
        ///     then,
        ///     the arguments to the function are pushed in direct order; that is, the first argument is pushed first. Finally you
        ///     call
        ///     lua_call; nargs is the number of arguments that you pushed onto the stack. All arguments and the function value are
        ///     popped from the stack when the function is called. The function results are pushed onto the stack when the function
        ///     returns. The number of results is adjusted to nresults, unless nresults is LUA_MULTRET. In this case, all results
        ///     from
        ///     the function are pushed. Lua takes care that the returned values fit into the stack space. The function results are
        ///     pushed onto the stack in direct order (the first result is pushed first), so that after the call the last result is
        ///     on
        ///     the top of the stack.
        /// </summary>
        /// <param name="L">The LuaState</param>
        /// <param name="nargs">The number of arguments.</param>
        /// <param name="nresults">The number of expected results.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected static void LuaCall(LuaState L, int nargs, int nresults = LUA_MULTRET)
        {
            var args = L.GetTopArray(nargs);

            L.Discard(nargs);

            var func = L.Pop();

            var ret = L.ExecutionContext.Call(func, args);

            if (nresults != 0)
            {
                if (nresults == -1)
                {
                    nresults = (ret.Type == DataType.Tuple) ? ret.Tuple.Length : 1;
                }

                var vals = (ret.Type == DataType.Tuple) ? ret.Tuple : new DynValue[1] {ret};

                var copied = 0;

                for (var i = 0; i < vals.Length && copied < nresults; i++, copied++)
                {
                    L.Push(vals[i]);
                }

                while (copied < nresults)
                {
                    L.Push(DynValue.Nil);
                }
            }
        }
    }
}