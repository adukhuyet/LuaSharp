using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace LuaSharp.Classes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ConvertPropertyToExpressionBody")]
    internal class GameUnit
    {
        public GameUnit(Obj_AI_Base unit)
        {
            Unit = unit;
        }

        public GameUnit(GameObject unit)
        {
            foreach (var player in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValid && !h.IsMe).Where(players => players.Name == unit.Name))
            {
                Unit = player;
            }
        }

        public Obj_AI_Base Unit { get; private set; }

        #region Members
        public string name
        {
            get { return Unit.CharData.BaseSkinName; }
        }

        public string charName
        {
            get { return Unit is Obj_AI_Hero ? ((Obj_AI_Hero) Unit).ChampionName : Unit.CharData.BaseSkinName; }
        }

        public float level
        {
            get { return Unit is Obj_AI_Hero ? ((Obj_AI_Hero) Unit).Level : 1; }
        }

        public bool visible
        {
            get { return Unit.IsVisible; }
        }

        public GameObjectType type
        {
            get { return Unit.Type; }
        }

        public float x
        {
            get { return Unit.ServerPosition.X; }
        }

        public float y
        {
            get { return Unit.ServerPosition.Y; }
        }

        public float z
        {
            get { return Unit.ServerPosition.Z; }
        }

        public bool isAI
        {
            get { return !Unit.PlayerControlled; }
        }

        public bool isMe
        {
            get { return Unit.IsMe; }
        }

        public float buffCount
        {
            get { return Unit.Buffs.Count(); }
        }

        public float totalDamage
        {
            get { return Unit.FlatMagicDamageMod + Unit.FlatPhysicalDamageMod; }
        }

        public bool dead
        {
            get { return Unit.IsDead; }
        }

        public GameObjectTeam team
        {
            get { return Unit.Team; }
        }

        public float networkID
        {
            get { return Unit.NetworkId; }
        }

        public float health
        {
            get { return Unit.Health; }
        }

        public float maxHealth
        {
            get { return Unit.MaxHealth; }
        }

        public float mana
        {
            get { return Unit.Mana; }
        }

        public float maxMana
        {
            get { return Unit.MaxMana; }
        }

        public bool bInvulnerable
        {
            get { return Unit.IsInvulnerable; }
        }

        public bool bPhysImune
        {
            get { return Unit.PhysicalImmune; }
        }

        public bool bMagicImune
        {
            get { return Unit.MagicImmune; }
        }

        public bool bTargetable
        {
            get { return Unit.IsTargetable; }
        }

        public bool bTargetableToTeam
        {
            get { return Unit.IsValidTarget(float.MaxValue, false) && Unit.IsAlly; }
        }

        public bool controlled
        {
            get { return Unit.PlayerControlled; }
        }

        public float cdr
        {
            get { return Unit.FlatCooldownMod; }
        }

        public float critChance
        {
            get { return Unit.FlatCritChanceMod; }
        }

        public float critDmg
        {
            get { return Unit.FlatCritDamageMod; }
        }

        public float hpPool
        {
            get { return maxHealth; }
        }

        public float hpRegen
        {
            get { return Unit.HPRegenRate; }
        }

        public float mpRegen
        {
            get { return Unit.PARRegenRate; }
        }

        public float attackSpeed
        {
            get { return 1 / Unit.AttackDelay; }
        }

        public float expBonus
        {
            get { return Unit.PercentEXPBonus; }
        }

        public float hardness
        {
            get { return 0; }
        }

        public float lifeSteal
        {
            get { return Unit.PercentLifeStealMod; }
        }

        public float spellVamp
        {
            get { return Unit.PercentSpellVampMod; }
        }

        public float physReduction
        {
            get { return Unit.FlatPhysicalReduction; }
        }

        public float magicReduction
        {
            get { return Unit.FlatMagicReduction; }
        }

        public float armorPen
        {
            get { return Unit.FlatArmorPenetrationMod; }
        }

        public float magicPen
        {
            get { return Unit.FlatMagicPenetrationMod; }
        }

        public float armorPenPercent
        {
            get { return Unit.PercentArmorPenetrationMod; }
        }

        public float magicPenPerecent
        {
            get { return Unit.PercentMagicPenetrationMod; }
        }

        public float addDamage
        {
            get { return Unit.FlatPhysicalDamageMod; }
        }

        public float ap
        {
            get { return Unit.FlatMagicDamageMod; }
        }

        public float damage
        {
            get { return Unit.FlatPhysicalDamageMod; }
        }

        public float armor
        {
            get { return Unit.Armor; }
        }

        public float magicArmor
        {
            get { return Unit.SpellBlock; }
        }

        public float ms
        {
            get { return Unit.MoveSpeed; }
        }

        public float range
        {
            get { return Unit.AttackRange; }
        }

        public float gold
        {
            get { return Unit.Gold; }
        }

        public Position pos
        {
            get { return new Position(Unit.Position.X, Unit.Position.Y, Unit.Position.Z); }
        }

        public Position minBBox
        {
            get { return new Position(Unit.BBox.Minimum.X, Unit.BBox.Minimum.Y, Unit.BBox.Minimum.Z); }
        }

        public Position maxBBox
        {
            get { return new Position(Unit.BBox.Maximum.X, Unit.BBox.Maximum.Y, Unit.BBox.Maximum.Z); }
        }

        public string armorMaterial
        {
            get { return Unit.ArmorMaterial; }
        }

        public string weaponMaterial
        {
            get { return ""; }
        } // Can't find API in L#

        public float deathTimer
        {
            get { return Unit.DeathDuration; }
        }

        public bool canAttack
        {
            get { return Unit.CanAttack; }
        }

        public bool canMove
        {
            get { return Unit.CanMove; }
        }

        public bool isStealthed
        {
            get { return Unit.HasBuffOfType(BuffType.Invisibility); }
        }

        public bool isRevealSpecificUnit
        {
            get { return Unit.CharacterState == GameObjectCharacterState.RevealSpecificUnit; }
        }

        public bool isTaunted
        {
            get { return Unit.HasBuffOfType(BuffType.Taunt); }
        }

        public bool isCharmed
        {
            get { return Unit.HasBuffOfType(BuffType.Charm); }
        }

        public bool isFeared
        {
            get { return Unit.HasBuffOfType(BuffType.Fear); }
        }

        public bool isAsleep
        {
            get { return Unit.HasBuffOfType(BuffType.Sleep); }
        }

        public bool isNearSight
        {
            get { return Unit.HasBuffOfType(BuffType.NearSight); }
        }

        public bool isGhosted
        {
            get { return Unit.CharacterState == GameObjectCharacterState.Ghosted; }
        }

        public bool isNoRender
        {
            get { return Unit.CharacterState == GameObjectCharacterState.NoRender; }
        }

        public bool isFleeing
        {
            get { return Unit.CharacterState == GameObjectCharacterState.Fleeing; }
        }

        public bool isForceRenderParticles
        {
            get { return Unit.CharacterState == GameObjectCharacterState.ForceRenderParticles; }
        }
        #endregion

        #region methods
        public void HoldPosition()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, ObjectManager.Player.Position);
        }

        public void MoveTo(float x, float y)
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, new Vector2(x, y).To3D(), false);
        }

        public void Attack(GameUnit unit)
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, unit.Unit);
        }

        public float GetDistance(GameUnit unit)
        {
            return unit.Unit.Position.Distance(Unit.Position);
        }

        public string getBuff(int index)
        {
            return index >= Unit.Buffs.Count() ? Unit.Buffs[index - 1].Name : "";
        }

        public int getInventorySlot(SpellSlot slot)
        {
            return Unit.InventoryItems.Where(item => item.SpellSlot == slot).Select(item => (int) item.Id).FirstOrDefault();
        }

        //Can someone tell me what these things do?
        /*
        public float CalcDamage(GameUnit unit)
        {
            return unit.damage;
        }
        public float CalcMagicDamage(GameUnit unit)
        {
            return unit.damage;
        }
        //*/


        #endregion
    }
}