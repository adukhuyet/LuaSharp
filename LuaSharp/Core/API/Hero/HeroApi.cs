using System;
using LeagueSharp;
using MoonSharp.Interpreter;
using SharpDX;

/*
unit:HoldPosition()
unit:MoveTo(x,z)
unit:Attack(target)
unit:GetDistance(target)
unit:CalcDamage(target,fDmg)
unit:CalcMagicDamage(target,fDmg)
unit:getBuff(iIndex) --returns buff object
unit:getInventorySlot(iSlot) --from ITEM_1 to ITEM_6, return item ID
unit:getItem(iSlot) --from ITEM_1 to ITEM_6, return LoLItem
unit:GetSpellData(iSpell) --Returns Spell
unit:CanUseSpell(iSpell) --Returns SpellState
//*/
namespace LuaSharp.Core.API.Hero
{

    class HeroApi
    {
        public static void AddApi(Script script)
        {
            script.Globals["unit:HoldPosition"] = (Action) HoldPos;
            script.Globals["unit:MoveTo"] = (Action<float, float>) MoveTo;
        }

        private static void HoldPos()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, ObjectManager.Player.Position);
        }

        private static void MoveTo(float x, float y)
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, new Vector3(x, y, ObjectManager.Player.Position.Z));
        }

        private static void Attack()
        {
            
        }
    }
}
