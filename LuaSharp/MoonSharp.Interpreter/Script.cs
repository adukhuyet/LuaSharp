﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonSharp.Interpreter.CoreLib;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Diagnostics;
using MoonSharp.Interpreter.Execution.VM;
using MoonSharp.Interpreter.IO;
using MoonSharp.Interpreter.Platforms;
using MoonSharp.Interpreter.Tree.Fast_Interface;

namespace MoonSharp.Interpreter
{
    /// <summary>
    ///     This class implements a MoonSharp scripting session. Multiple Script objects can coexist in the same program but
    ///     cannot share
    ///     data among themselves unless some mechanism is put in place.
    /// </summary>
    public class Script : IScriptPrivateResource
    {
        /// <summary>
        ///     The version of the MoonSharp engine
        /// </summary>
        public const string VERSION = "0.9.8.0";

        /// <summary>
        ///     The Lua version being supported
        /// </summary>
        public const string LUA_VERSION = "5.2";

        private readonly ByteCode m_ByteCode;
        private readonly Processor m_MainProcessor;
        private readonly List<SourceCode> m_Sources = new List<SourceCode>();
        private readonly Table[] m_TypeMetatables = new Table[(int) LuaTypeExtensions.MaxMetaTypes];
        private IDebugger m_Debugger;

        /// <summary>
        ///     Initializes the <see cref="Script" /> class.
        /// </summary>
        static Script()
        {
            GlobalOptions = new ScriptGlobalOptions();

            DefaultOptions = new ScriptOptions
            {
                DebugPrint = s => { GlobalOptions.Platform.DefaultPrint(s); },
                DebugInput = s => { return GlobalOptions.Platform.DefaultInput(s); },
                CheckThreadAccess = true,
                ScriptLoader = PlatformAutoDetector.GetDefaultScriptLoader(),
                TailCallOptimizationThreshold = 65536
            };
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Script" /> clas.s
        /// </summary>
        public Script()
            : this(CoreModules.Preset_Default)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Script" /> class.
        /// </summary>
        /// <param name="coreModules">The core modules to be pre-registered in the default global table.</param>
        public Script(CoreModules coreModules)
        {
            Options = new ScriptOptions(DefaultOptions);
            PerformanceStats = new PerformanceStatistics();
            Registry = new Table(this);

            m_ByteCode = new ByteCode(this);
            Globals = new Table(this).RegisterCoreModules(coreModules);
            m_MainProcessor = new Processor(this, Globals, m_ByteCode);
        }

        /// <summary>
        ///     Gets or sets the script loader which will be used as the value of the
        ///     ScriptLoader property for all newly created scripts.
        /// </summary>
        public static ScriptOptions DefaultOptions { get; }

        /// <summary>
        ///     Gets access to the script options.
        /// </summary>
        public ScriptOptions Options { get; }

        /// <summary>
        ///     Gets the global options, that is options which cannot be customized per-script.
        /// </summary>
        public static ScriptGlobalOptions GlobalOptions { get; }

        /// <summary>
        ///     Gets access to performance statistics.
        /// </summary>
        public PerformanceStatistics PerformanceStats { get; private set; }

        /// <summary>
        ///     Gets the default global table for this script. Unless a different table is intentionally passed (or setfenv has
        ///     been used)
        ///     execution uses this table.
        /// </summary>
        public Table Globals { get; }

        /// <summary>
        ///     Gets the source code count.
        /// </summary>
        /// <value>
        ///     The source code count.
        /// </value>
        public int SourceCodeCount
        {
            get { return m_Sources.Count; }
        }

        /// <summary>
        ///     MoonSharp (like Lua itself) provides a registry, a predefined table that can be used by any CLR code to
        ///     store whatever Lua values it needs to store.
        ///     Any CLR code can store data into this table, but it should take care to choose keys
        ///     that are different from those used by other libraries, to avoid collisions.
        ///     Typically, you should use as key a string GUID, a string containing your library name, or a
        ///     userdata with the address of a CLR object in your code.
        /// </summary>
        public Table Registry { get; private set; }

        Script IScriptPrivateResource.OwnerScript
        {
            get { return this; }
        }

        /// <summary>
        ///     Loads a string containing a Lua/MoonSharp function.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="funcFriendlyName">Name of the function used to report errors, etc.</param>
        /// <returns>
        ///     A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadFunction(string code, Table globalTable = null, string funcFriendlyName = null)
        {
            this.CheckScriptOwnership(globalTable);

            var chunkName = string.Format("libfunc_{0}", funcFriendlyName ?? m_Sources.Count.ToString());

            var source = new SourceCode(chunkName, code, m_Sources.Count, this);

            m_Sources.Add(source);

            var address = Loader_Fast.LoadFunction(this, source, m_ByteCode, globalTable ?? Globals);

            SignalSourceCodeChange(source);
            SignalByteCodeChange();

            return MakeClosure(address);
        }

        private void SignalByteCodeChange()
        {
            if (m_Debugger != null)
            {
                m_Debugger.SetByteCode(m_ByteCode.Code.Select(s => s.ToString()).ToArray());
            }
        }

        private void SignalSourceCodeChange(SourceCode source)
        {
            if (m_Debugger != null)
            {
                m_Debugger.SetSourceCode(source);
            }
        }

        /// <summary>
        ///     Loads a string containing a Lua/MoonSharp script.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc.</param>
        /// <returns>
        ///     A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadString(string code, Table globalTable = null, string codeFriendlyName = null)
        {
            this.CheckScriptOwnership(globalTable);

            if (code.StartsWith(StringModule.BASE64_DUMP_HEADER))
            {
                code = code.Substring(StringModule.BASE64_DUMP_HEADER.Length);
                var data = Convert.FromBase64String(code);
                using (var ms = new MemoryStream(data))
                    return LoadStream(ms, globalTable, codeFriendlyName);
            }

            var chunkName = string.Format("{0}", codeFriendlyName ?? "chunk_" + m_Sources.Count);

            var source = new SourceCode(codeFriendlyName ?? chunkName, code, m_Sources.Count, this);

            m_Sources.Add(source);

            var address = Loader_Fast.LoadChunk(this,
                source,
                m_ByteCode,
                globalTable ?? Globals);

            SignalSourceCodeChange(source);
            SignalByteCodeChange();

            return MakeClosure(address);
        }

        /// <summary>
        ///     Loads a Lua/MoonSharp script from a System.IO.Stream. NOTE: This will *NOT* close the stream!
        /// </summary>
        /// <param name="stream">The stream containing code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc.</param>
        /// <returns>
        ///     A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadStream(Stream stream, Table globalTable = null, string codeFriendlyName = null)
        {
            this.CheckScriptOwnership(globalTable);

            Stream codeStream = new UndisposableStream(stream);

            if (!Processor.IsDumpStream(codeStream))
            {
                using (var sr = new StreamReader(codeStream))
                {
                    var scriptCode = sr.ReadToEnd();
                    return LoadString(scriptCode, globalTable, codeFriendlyName);
                }
            }
            var chunkName = string.Format("{0}", codeFriendlyName ?? "dump_" + m_Sources.Count);

            var source = new SourceCode(codeFriendlyName ?? chunkName,
                string.Format("-- This script was decoded from a binary dump - dump_{0}", m_Sources.Count),
                m_Sources.Count, this);

            m_Sources.Add(source);

            bool hasUpvalues;
            var address = m_MainProcessor.Undump(codeStream, m_Sources.Count - 1, globalTable ?? Globals,
                out hasUpvalues);

            SignalSourceCodeChange(source);
            SignalByteCodeChange();

            if (hasUpvalues)
                return MakeClosure(address, globalTable ?? Globals);
            return MakeClosure(address);
        }

        /// <summary>
        ///     Dumps on the specified stream.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentException">
        ///     function arg is not a function!
        ///     or
        ///     stream is readonly!
        ///     or
        ///     function arg has upvalues other than _ENV
        /// </exception>
        public void Dump(DynValue function, Stream stream)
        {
            this.CheckScriptOwnership(function);

            if (function.Type != DataType.Function)
                throw new ArgumentException("function arg is not a function!");

            if (!stream.CanWrite)
                throw new ArgumentException("stream is readonly!");

            var upvaluesType = function.Function.GetUpvaluesType();

            if (upvaluesType == Closure.UpvaluesType.Closure)
                throw new ArgumentException("function arg has upvalues other than _ENV");

            var outStream = new UndisposableStream(stream);
            m_MainProcessor.Dump(outStream, function.Function.EntryPointByteCodeLocation,
                upvaluesType == Closure.UpvaluesType.Environment);
        }

        /// <summary>
        ///     Loads a string containing a Lua/MoonSharp script.
        /// </summary>
        /// <param name="filename">The code.</param>
        /// <param name="globalContext">The global table to bind to this chunk.</param>
        /// <param name="friendlyFilename">The filename to be used in error messages.</param>
        /// <returns>
        ///     A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadFile(string filename, Table globalContext = null, string friendlyFilename = null)
        {
            this.CheckScriptOwnership(globalContext);

#pragma warning disable 618
            filename = Options.ScriptLoader.ResolveFileName(filename, globalContext ?? Globals);
#pragma warning restore 618

            var code = Options.ScriptLoader.LoadFile(filename, globalContext ?? Globals);

            if (code is string)
            {
                return LoadString((string) code, globalContext, friendlyFilename ?? filename);
            }
            if (code is byte[])
            {
                using (var ms = new MemoryStream((byte[]) code))
                    return LoadStream(ms, globalContext, friendlyFilename ?? filename);
            }
            if (code is Stream)
            {
                try
                {
                    return LoadStream((Stream) code, globalContext, friendlyFilename ?? filename);
                }
                finally
                {
                    ((Stream) code).Dispose();
                }
            }
            if (code == null)
                throw new InvalidCastException("Unexpected null from IScriptLoader.LoadFile");
            throw new InvalidCastException(string.Format("Unsupported return type from IScriptLoader.LoadFile : {0}",
                code.GetType()));
        }

        /// <summary>
        ///     Loads and executes a string containing a Lua/MoonSharp script.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        ///     A DynValue containing the result of the processing of the loaded chunk.
        /// </returns>
        public DynValue DoString(string code, Table globalContext = null)
        {
            var func = LoadString(code, globalContext);
            return Call(func);
        }

        /// <summary>
        ///     Loads and executes a stream containing a Lua/MoonSharp script.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        ///     A DynValue containing the result of the processing of the loaded chunk.
        /// </returns>
        public DynValue DoStream(Stream stream, Table globalContext = null)
        {
            var func = LoadStream(stream, globalContext);
            return Call(func);
        }

        /// <summary>
        ///     Loads and executes a file containing a Lua/MoonSharp script.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        ///     A DynValue containing the result of the processing of the loaded chunk.
        /// </returns>
        public DynValue DoFile(string filename, Table globalContext = null)
        {
            var func = LoadFile(filename, globalContext);
            return Call(func);
        }

        /// <summary>
        ///     Runs the specified file with all possible defaults for quick experimenting.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// A DynValue containing the result of the processing of the executed script.
        public static DynValue RunFile(string filename)
        {
            var S = new Script();
            return S.DoFile(filename);
        }

        /// <summary>
        ///     Runs the specified code with all possible defaults for quick experimenting.
        /// </summary>
        /// <param name="code">The Lua/MoonSharp code.</param>
        /// A DynValue containing the result of the processing of the executed script.
        public static DynValue RunString(string code)
        {
            var S = new Script();
            return S.DoString(code);
        }

        /// <summary>
        ///     Creates a closure from a bytecode address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="envTable">The env table to create a 0-upvalue</param>
        /// <returns></returns>
        private DynValue MakeClosure(int address, Table envTable = null)
        {
            this.CheckScriptOwnership(envTable);
            Closure c;

            if (envTable == null)
                c = new Closure(this, address, new SymbolRef[0], new DynValue[0]);
            else
            {
                var syms = new SymbolRef[1]
                {
                    new SymbolRef
                    {
                        i_Env = null,
                        i_Index = 0,
                        i_Name = WellKnownSymbols.ENV,
                        i_Type = SymbolRefType.DefaultEnv
                    }
                };

                var vals = new DynValue[1]
                {
                    DynValue.NewTable(envTable)
                };

                c = new Closure(this, address, syms, vals);
            }

            return DynValue.NewClosure(c);
        }

        /// <summary>
        ///     Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/MoonSharp function to be called</param>
        /// <returns>
        ///     The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function)
        {
            return Call(function, new DynValue[0]);
        }

        /// <summary>
        ///     Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/MoonSharp function to be called</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        ///     The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, params DynValue[] args)
        {
            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(args);

            if (function.Type != DataType.Function && function.Type != DataType.ClrFunction)
            {
                var metafunction = m_MainProcessor.GetMetamethod(function, "__call");

                if (metafunction != null)
                {
                    var metaargs = new DynValue[args.Length + 1];
                    metaargs[0] = function;
                    for (var i = 0; i < args.Length; i++)
                        metaargs[i + 1] = args[i];

                    function = metafunction;
                    args = metaargs;
                }
                else
                {
                    throw new ArgumentException("function is not a function and has no __call metamethod.");
                }
            }
            else if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.ClrCallback(CreateDynamicExecutionContext(function.Callback),
                    new CallbackArguments(args, false));
            }

            return m_MainProcessor.Call(function, args);
        }

        /// <summary>
        ///     Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/MoonSharp function to be called</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        ///     The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, params object[] args)
        {
            var dargs = new DynValue[args.Length];

            for (var i = 0; i < dargs.Length; i++)
                dargs[i] = DynValue.FromObject(this, args[i]);

            return Call(function, dargs);
        }

        /// <summary>
        ///     Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/MoonSharp function to be called</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function)
        {
            return Call(DynValue.FromObject(this, function));
        }

        /// <summary>
        ///     Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/MoonSharp function to be called </param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, params object[] args)
        {
            return Call(DynValue.FromObject(this, function), args);
        }

        /// <summary>
        ///     Creates a coroutine pointing at the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>
        ///     The coroutine handle.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function or DataType.ClrFunction</exception>
        public DynValue CreateCoroutine(DynValue function)
        {
            this.CheckScriptOwnership(function);

            if (function.Type == DataType.Function)
                return m_MainProcessor.Coroutine_Create(function.Function);
            if (function.Type == DataType.ClrFunction)
                return DynValue.NewCoroutine(new Coroutine(function.Callback));
            throw new ArgumentException("function is not of DataType.Function or DataType.ClrFunction");
        }

        /// <summary>
        ///     Creates a coroutine pointing at the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>
        ///     The coroutine handle.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function or DataType.ClrFunction</exception>
        public DynValue CreateCoroutine(object function)
        {
            return CreateCoroutine(DynValue.FromObject(this, function));
        }

        /// <summary>
        ///     Gets the main chunk function.
        /// </summary>
        /// <returns>A DynValue containing a function which executes the first chunk that has been loaded.</returns>
        public DynValue GetMainChunk()
        {
            return MakeClosure(0);
        }

        /// <summary>
        ///     Attaches a debugger. This usually should be called by the debugger itself and not by user code.
        /// </summary>
        /// <param name="debugger">The debugger object.</param>
        public void AttachDebugger(IDebugger debugger)
        {
            m_Debugger = debugger;
            m_MainProcessor.AttachDebugger(debugger);

            foreach (var src in m_Sources)
                SignalSourceCodeChange(src);

            SignalByteCodeChange();
        }

        /// <summary>
        ///     Gets the source code.
        /// </summary>
        /// <param name="sourceCodeID">The source code identifier.</param>
        /// <returns></returns>
        public SourceCode GetSourceCode(int sourceCodeID)
        {
            return m_Sources[sourceCodeID];
        }

        /// <summary>
        ///     Loads a module as per the "require" Lua function. http://www.lua.org/pil/8.1.html
        /// </summary>
        /// <param name="modname">The module name</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">Raised if module is not found</exception>
        public DynValue RequireModule(string modname, Table globalContext = null)
        {
            this.CheckScriptOwnership(globalContext);

            var globals = globalContext ?? Globals;
            var filename = Options.ScriptLoader.ResolveModuleName(modname, globals);

            if (filename == null)
                throw new ScriptRuntimeException("module '{0}' not found", modname);

            var func = LoadFile(filename, globalContext, filename);
            return func;
        }

        /// <summary>
        ///     Gets a type metatable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Table GetTypeMetatable(DataType type)
        {
            var t = (int) type;

            if (t >= 0 && t < m_TypeMetatables.Length)
                return m_TypeMetatables[t];

            return null;
        }

        /// <summary>
        ///     Sets a type metatable.
        /// </summary>
        /// <param name="type">The type. Must be Nil, Boolean, Number, String or Function</param>
        /// <param name="metatable">The metatable.</param>
        /// <exception cref="System.ArgumentException">Specified type not supported :  + type.ToString()</exception>
        public void SetTypeMetatable(DataType type, Table metatable)
        {
            this.CheckScriptOwnership(metatable);

            var t = (int) type;

            if (t >= 0 && t < m_TypeMetatables.Length)
                m_TypeMetatables[t] = metatable;
            else
                throw new ArgumentException("Specified type not supported : " + type);
        }

        /// <summary>
        ///     Warms up the parser/lexer structures so that MoonSharp operations start faster.
        /// </summary>
        public static void WarmUp()
        {
            var s = new Script(CoreModules.Basic);
            s.LoadString("return 1;");
        }

        /// <summary>
        ///     Creates a new dynamic expression.
        /// </summary>
        /// <param name="code">The code of the expression.</param>
        /// <returns></returns>
        public DynamicExpression CreateDynamicExpression(string code)
        {
            var dee = Loader_Fast.LoadDynamicExpr(this, new SourceCode("__dynamic", code, -1, this));
            return new DynamicExpression(this, code, dee);
        }

        /// <summary>
        ///     Creates a new dynamic expression which is actually quite static, returning always the same constant value.
        /// </summary>
        /// <param name="code">The code of the not-so-dynamic expression.</param>
        /// <param name="constant">The constant to return.</param>
        /// <returns></returns>
        public DynamicExpression CreateConstantDynamicExpression(string code, DynValue constant)
        {
            this.CheckScriptOwnership(constant);

            return new DynamicExpression(this, code, constant);
        }

        /// <summary>
        ///     Gets an execution context exposing only partial functionality, which should be used for
        ///     those cases where the execution engine is not really running - for example for dynamic expression
        ///     or calls from CLR to CLR callbacks
        /// </summary>
        internal ScriptExecutionContext CreateDynamicExecutionContext(CallbackFunction func = null)
        {
            return new ScriptExecutionContext(m_MainProcessor, func, null, true);
        }
    }
}