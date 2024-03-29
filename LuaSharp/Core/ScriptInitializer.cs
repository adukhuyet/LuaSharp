﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp.Common;
using LuaSharp.Core.API;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.REPL;

namespace LuaSharp.Core
{
    public class ScriptInitializer
    {
        public static List<Script> Scripts;

        public static void Init()
        {
            // Create directories
            if (!Directory.Exists(RootScriptDir))
                Directory.CreateDirectory(RootScriptDir);

            if (!Directory.Exists(RootLibScriptDir))
                Directory.CreateDirectory(RootLibScriptDir);

            // Initialize list
            Scripts = new List<Script>();

            Console.WriteLine(Directory.GetFiles(RootScriptDir).Count());
            Script.DefaultOptions.ScriptLoader = new ReplInterpreterScriptLoader();
            foreach (var scriptFile in Directory.GetFiles(RootScriptDir))
            {
                var script = new Script();
                ((ScriptLoaderBase)script.Options.ScriptLoader).ModulePaths = new[] { Path.Combine(RootLibScriptDir, "?"), Path.Combine(RootLibScriptDir, "?.lua") };
                ApiHandler.AddApi(script);

                //Get each lib file, and load it to the script
                /*
                foreach (var libFile in Directory.GetFiles(RootLibScriptDir))
                {
                    script.LoadFile(libFile);
                }
                //*/

                // Run the script :D
                script.DoFile(scriptFile);

                // Add script to the list
                Scripts.Add(script);
            }
        }

        public static string RootScriptDir = Path.Combine(Config.AppDataDirectory, "Lua");
        public static string RootLibScriptDir = Path.Combine(RootScriptDir, "Lib");
    }
}
