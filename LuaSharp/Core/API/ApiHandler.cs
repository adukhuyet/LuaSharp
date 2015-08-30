#region

using System;
using LeagueSharp;
using MoonSharp.Interpreter;
using LuaSharp.Core;
using LuaSharp.Core.API.Drawing;
using LuaSharp.Core.API.Hero;
using LuaSharp.Core.API.Menu;
using LuaSharp.Core.API.Packets;
using LuaSharp.Core.API.Util;
using LuaSharp.Core.API.Unit;

#endregion

namespace LuaSharp.Core.API
{
    internal class ApiHandler
    {
        public static void AddApi(Script script)
        {
            script.Globals["PrintChat"] = (Action<string>) Game.PrintChat;
            script.Globals["SendChat"] = (Action<string>) Game.Say;

            //Add API's
            MenuApi.AddApi(script);
            PacketApi.AddApi(script);
            HeroApi.AddApi(script);
            DrawingApi.AddApi(script);
            UtilApi.AddApi(script);
            UnitApi.AddApi(script);
        }

        private static void CallFunc(string funcName)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                script.Call(script.Globals[funcName]);
            }
        }

        #region Events

        public static void OnGameUpdate(EventArgs eventArgs)
        {
            CallFunc("OnTick");
        }

        public static void OnGameLoad(EventArgs eventArgs)
        {
            CallFunc("OnLoad");
        }

        public static void OnDraw(EventArgs args)
        {
            CallFunc("OnDraw");
        }

        public static void OnCreateObj(GameObject sender, EventArgs args)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                script.Call(script.Globals["OnCreateObj"], sender);
            }
        }

        public static void OnDeleteObj(GameObject sender, EventArgs args)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                script.Call(script.Globals["OnDeleteObj"], sender);
            }
        }

        public static void OnWndMsg(WndEventArgs args)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                script.Call(script.Globals["OnWndMsg"], args.Msg, args.WParam);
            }
        }

        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                script.Call(script.Globals["OnProcessSpell"], sender, args);
            }
        }

        public static void OnSendChat(GameInputEventArgs args)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                script.Call(script.Globals["OnSendChat"], args.Input);
            }
        }

        public static void OnSendPacket(GamePacketEventArgs args)
        {
            foreach (var script in ScriptInitializer.Scripts)
            {
                //Disabled until CLoLPacket is implemented
                script.Call(script.Globals["OnSendPacket"], args.PacketData);
            }
        }

        #endregion
    }
}