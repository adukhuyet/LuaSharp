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
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;

            // Setup load event for the Lua API
            CustomEvents.Game.OnGameLoad += ApiHandler.OnGameLoad;
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

            Game.PrintChat("LuaSharp based off bridge of london is loaded");
        }
    }
}
