using System;
using System.Diagnostics;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LuaSharp.Classes;
using MoonSharp.Interpreter;
using SharpDX;

namespace LuaSharp.Core.API.Util
{
    class UtilApi
    {
        public static void AddApi(Script script)
        {
            UserData.RegisterType<Position>();
            #region Pings
            script.Globals["PING_ASSISTME"] = PingCategory.AssistMe;
            script.Globals["PING_DANGER"] = PingCategory.Danger;
            script.Globals["PING_ENEMYMISSING"] = PingCategory.EnemyMissing;
            script.Globals["PING_FALLBACK"] = PingCategory.Fallback;
            script.Globals["PING_NORMAL"] = PingCategory.Normal;
            script.Globals["PING_ONMYWAY"] = PingCategory.OnMyWay;
            #endregion
            script.Globals["GetTickCount"] = (Func<int>) GetTickCount;
            script.Globals["GetLatency"] = (Func<int>) GetPing;
            script.Globals["GetCursorPos"] = (Func<Position>) GetCursorPosition;
            script.Globals["GetGameTimer"] = (Func<float>) GetGameTime;
            script.Globals["WorldToScreen"] = (Func<GameUnit, Position>) WorldToScreen;
            script.Globals["GetTarget"] = (Func<GameUnit>) GetTarget;
            script.Globals["SetTarget"] = (Action<GameUnit>) SetTarget;
            script.Globals["BuyItem"] = (Action<int>) BuyItem;
            script.Globals["SellItem"] = (Action<SpellSlot>) SellItem;
            script.Globals["IsWallOfGrass"] = (Func<Position, bool>) IsWallOfGrass;
            script.Globals["KillProcess"] = (Action<string>) TaskKill;
            script.Globals["PingSignal"] = (Action<object, float, float, float, PingCategory>) Ping;
            //script.Globals["IsItemPurchasable"] = (Func<int, bool>) IsItemPurchasable;
            script.Globals["GetGameTimer"] = (Func<float>) GetGameTimer;
            script.Globals["GetMyHero"] = (Func<GameUnit>) GetMyHero;
        }

        private static void Ping(object iMode, float x, float y, float z, PingCategory bPing)
        {
            Game.SendPing(bPing, new Vector3(x, y , z));
        }

        private static float GetGameTimer()
        {
            return Game.Time;
        }

        private static void TaskKill(string proc)
        {
            foreach(var process in Process.GetProcessesByName(proc))
                process.Kill();
        }

        private static bool IsWallOfGrass(Position pos)
        {
            return NavMesh.IsWallOfGrass(pos.x, pos.y, 10);
        }

        private static bool IsItemPurchasable(int itemId)
        {
            //TODO: Have some logic behind this
            return true;
        }

        private static void SellItem(SpellSlot itemSlot)
        {
            foreach (var item in ObjectManager.Player.InventoryItems.Where(item => item.SpellSlot == itemSlot))
            {
                ObjectManager.Player.SellItem(item.Slot);
            }
        }

        private static void BuyItem(int itemId)
        {
            ObjectManager.Player.BuyItem((ItemId)itemId);
        }

        private static void SetTarget(GameUnit unit)
        {
            TargetSelector.SetTarget((Obj_AI_Hero)unit.Unit);
        }

        private static GameUnit GetTarget()
        {
            return new GameUnit(ObjectManager.Player.Target);
        }

        private static Position WorldToScreen(GameUnit unit)
        {
            return unit.pos;
        }


        private static GameUnit GetMyHero()
        {
            return new GameUnit(ObjectManager.Player);
        }

        private static float GetGameTime()
        {
            return Game.Time;
        }

        private static Position GetCursorPosition()
        {
            return new Position(Game.CursorPos.X, Game.CursorPos.Y, Game.CursorPos.Z);
        }

        private static int GetPing()
        {
            return Game.Ping;
        }

        private static int GetTickCount()
        {
            return Environment.TickCount;
        }
    }
}
