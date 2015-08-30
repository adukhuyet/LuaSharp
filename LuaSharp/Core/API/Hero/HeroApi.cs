using System;
using LeagueSharp;
using LuaSharp.Classes;
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
            UserData.RegisterType<GameUnit>();
            var unit = UserData.Create(new GameUnit(ObjectManager.Player));
            script.Globals.Set("myHero", unit);
        }
    }
}
