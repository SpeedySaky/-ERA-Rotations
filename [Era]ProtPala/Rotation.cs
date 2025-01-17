using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;
using wShadow.WowBots;
using wShadow.WowBots.PartyInfo;

public class ERAProtPala : Rotation
{
    private List<string> npcConditions = new List<string>
    {
        "Innkeeper", "Auctioneer", "Banker", "FlightMaster", "GuildBanker",
        "PlayerVehicle", "StableMaster", "Repair", "Trainer", "TrainerClass",
        "TrainerProfession", "Vendor", "VendorAmmo", "VendorFood", "VendorPoison",
        "VendorReagent", "WildBattlePet", "GarrisonMissionNPC", "GarrisonTalentNPC",
        "QuestGiver"
    };
    public bool IsValid(WowUnit unit)
    {
        if (unit == null || unit.Address == null)
        {
            return false;
        }
        return true;
    }
    private bool HasEnchantment(EquipmentSlot slot, string enchantmentName)
    {
        return Api.Equipment.HasEnchantment(slot, enchantmentName);
    }
    private CreatureType GetCreatureType(WowUnit unit)
    {
        return unit.Info.GetCreatureType();
    }
    private bool HasItem(object item) => Api.Inventory.HasItem(item);
    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;

    public override void Initialize()
    {
        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        SlowTick = 750;
        FastTick = 500;

        PassiveActions.Add((true, () => false));
        CombatActions.Add((true, () => false));
    }
    public override bool PassivePulse()
    {
        var me = Api.Player;
        var healthPercentage = me.HealthPercent;
        var mana = me.ManaPercent;
        var target = Api.Target;
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }

        if (me.IsValid())
        {
            if (Api.Spellbook.CanCast("Holy Light") && healthPercentage <= 50 && mana > 20)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Holy Light");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Holy Light"))
                {
                    return true;
                }
            }

            if (Api.Spellbook.CanCast("Blessing of Wisdom") && !me.Auras.Contains("Blessing of Wisdom") && mana > 15)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Blessing of Wisdom");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Blessing of Wisdom"))
                {
                    return true;
                }
            }

            if (Api.Spellbook.CanCast("Retribution Aura") && !me.Auras.Contains("Retribution Aura", false))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Retribution Aura");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Retribution Aura"))
                {
                    return true;
                }
            }
        }

        return base.PassivePulse();
    }

    public override bool CombatPulse()
    {
        var me = Api.Player;
        var healthPercentage = me.HealthPercent;
        var mana = me.ManaPercent;
        var target = Api.Target;
        var targethp = target.HealthPercent;
        var targetDistance = target.Position.Distance2D(me.Position);

        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        if (Api.Spellbook.CanCast("Divine Protection") && Api.Player.HealthPercent < 45 && !me.IsCasting() &&
            !Api.Player.Auras.Contains("Forbearance", false) && !Api.Spellbook.OnCooldown("Divine Protection"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Divine Protection");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Divine Protection"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Holy Shield") && !Api.Spellbook.OnCooldown("Holy Shield") && mana > 15)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Holy Shield");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Holy Shield"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Avenger's Shield") && !Api.Spellbook.OnCooldown("Avenger's Shield") && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Avenger's Shield");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Avenger's Shield"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Hammer of Justice") && !Api.Spellbook.OnCooldown("Hammer of Justice") && mana > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Hammer of Justice");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Hammer of Justice"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Exorcism") && !Api.Spellbook.OnCooldown("Exorcism") && (GetCreatureType(target) == CreatureType.Undead || GetCreatureType(target) == CreatureType.Demon) && mana > 15)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Exorcism");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Exorcism"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Lay on Hands") && healthPercentage <= 10 && !Api.Spellbook.OnCooldown("Lay on Hands"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Lay on Hands");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Lay on Hands"))
            {
                return true;
            }
        }

       
        if (!target.Auras.Contains("Judgement of Wisdom") && Api.Spellbook.CanCast("Seal of Wisdom") && !me.Auras.Contains("Seal of Wisdom") && mana > 5)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Seal of Wisdom");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Seal of Wisdom"))
                return true;
        }

        if (!me.Auras.Contains("Seal of Wisdom") && Api.Spellbook.CanCast("Seal of Righteousness") && !me.Auras.Contains("Seal of Righteousness") && mana > 30)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Seal of Righteousness");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Seal of Righteousness"))
                return true;
        }

        if (Api.Spellbook.CanCast("Judgement") && mana > 15 && !Api.Spellbook.OnCooldown("Judgement") && (me.Auras.Contains("Seal of Righteousness") || me.Auras.Contains("Seal of Wisdom") || me.Auras.Contains("Seal of Command")))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Judgement");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Judgement"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Consecration") && !Api.Spellbook.OnCooldown("Consecration") && targethp >= 30 && mana > 30 && Api.UnitsTargetingMe(5, true).Length >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Consecration");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Consecration"))
                return true;
        }

        if (Api.Spellbook.CanCast("Attack") && !me.IsAutoAttacking())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Attack");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Attack"))
            {
                return true;
            }
        }

        return base.CombatPulse();
    }

    private bool IsNPC(WowUnit unit)
    {
        if (!IsValid(unit))
        {
            return false;
        }

        foreach (var condition in npcConditions)
        {
            switch (condition)
            {
                case "Innkeeper" when unit.IsInnkeeper():
                case "Auctioneer" when unit.IsAuctioneer():
                case "Banker" when unit.IsBanker():
                case "FlightMaster" when unit.IsFlightMaster():
                case "GuildBanker" when unit.IsGuildBanker():
                case "StableMaster" when unit.IsStableMaster():
                case "Trainer" when unit.IsTrainer():
                case "Vendor" when unit.IsVendor():
                case "QuestGiver" when unit.IsQuestGiver():
                    return true;
            }
        }

        return false;
    }

    private void LogPlayerStats()
    {
        var me = Api.Player;

        var mana = me.Mana;
        var healthPercentage = me.HealthPercent;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana} Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");

        var hasPoisonDebuff = me.Auras.Contains("Poison");

        if (hasPoisonDebuff)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Have poison debuff");
            Console.ResetColor();
        }
    }
}