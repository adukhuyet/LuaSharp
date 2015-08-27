using System;
using LeagueSharp;
using LeagueSharp.Common;
using LuaSharp.Classes;
using MoonSharp.Interpreter;
using SharpDX;

namespace LuaSharp.Core.API.Unit
{
    class UnitApi
    {
        public static void AddApi(Script script)
        {
            UserData.RegisterType<GameUnit>();

            #region Spells 'n stuff
            script.Globals["_Q"] = SpellSlot.Q;
            script.Globals["_W"] = SpellSlot.W;
            script.Globals["_E"] = SpellSlot.E;
            script.Globals["_R"] = SpellSlot.R;
            

            script.Globals["SUMMONER_1"] = SpellSlot.Summoner1;
            script.Globals["SUMMONER_2"] = SpellSlot.Summoner2;

            script.Globals["ITEM_1"] = SpellSlot.Item1;
            script.Globals["ITEM_2"] = SpellSlot.Item2;
            script.Globals["ITEM_3"] = SpellSlot.Item3;
            script.Globals["ITEM_4"] = SpellSlot.Item4;
            script.Globals["ITEM_5"] = SpellSlot.Item5;
            script.Globals["ITEM_6"] = SpellSlot.Item6;

            script.Globals["RECALL"] = SpellSlot.Recall;
            #endregion

            script.Globals["LevelSpell"] = (Action<SpellSlot>) LevelSpell;

            script.Globals["CastSpell"] = (Action<SpellSlot>) CastSpell;
            script.Globals["CastSpell"] = (Action<SpellSlot, float, float>) CastSpell;
            script.Globals["CastSpell"] = (Action<SpellSlot, GameUnit>) CastSpell;
        }

        private static void LevelSpell(SpellSlot slot)
        {
            ObjectManager.Player.Spellbook.LevelSpell(slot);
        }

        private static void CastSpell(SpellSlot slot, GameUnit unit)
        {
            ObjectManager.Player.Spellbook.CastSpell(slot, unit.Unit);
        }

        private static void CastSpell(SpellSlot slot, float x, float y)
        {
            ObjectManager.Player.Spellbook.CastSpell(slot, new Vector3(x, y, 0));
        }

        private static void CastSpell(SpellSlot slot)
        {
            ObjectManager.Player.Spellbook.CastSpell(slot);
        }
    }
}
