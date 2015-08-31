//Thank you ChewyMoon for making my life a lot easier
using LeagueSharp;
using LeagueSharp.Common;
using LuaSharp.Core;
using LuaSharp.Core.API;
using System;

namespace LuaSharp
{
    class Program
    {
        private static Menu _menu;
        static void Main(string[] args)
        {
            Game.OnStart += GameOnOnGameLoad;

            // Setup load event for the Lua API
            Game.OnStart += ApiHandler.OnGameLoad;
        }
        private static void GameOnOnGameLoad(EventArgs args)
        {
            // Load all the scripts
            ScriptInitializer.Init();

            // API Events
            Game.OnUpdate += ApiHandler.OnGameUpdate;
            Drawing.OnDraw += ApiHandler.OnDraw;
            GameObject.OnCreate += ApiHandler.OnCreateObj;
            GameObject.OnDelete += ApiHandler.OnDeleteObj;
            Game.OnWndProc += ApiHandler.OnWndMsg;
            Obj_AI_Base.OnProcessSpellCast += ApiHandler.OnProcessSpellCast;
            Game.OnInput += ApiHandler.OnSendChat;
            Game.OnSendPacket += ApiHandler.OnSendPacket;
            (_menu = new Menu("LuaSharp", "luasharp", true)).AddToMainMenu();
            _menu.AddItem(new MenuItem("enabled", "Enabled").SetValue(false));

            Game.PrintChat("LuaSharp based off bridge of london is loaded");
        }
    }
}
