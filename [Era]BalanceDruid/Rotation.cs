using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;


public class EraBalanceDruid : Rotation
{
    private bool HasEnchantment(EquipmentSlot slot, string enchantmentName)
    {
        return Api.Equipment.HasEnchantment(slot, enchantmentName);
    }
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
    private bool HasItem(object item) => Api.Inventory.HasItem(item);

    private int debugInterval = 20; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;
    private DateTime Starsurge = DateTime.MinValue;
    private TimeSpan StarsurgeCD = TimeSpan.FromSeconds(6);

    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.

        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 600;
        FastTick = 150;

        // You can also use this method to add to various action lists.

        // This will add an action to the internal passive tick.
        // bool: needTarget -> If true action will not fire if player does not have a target
        // Func<bool>: function -> Action to attempt, must return true or false.
        PassiveActions.Add((true, () => false));

        // This will add an action to the internal combat tick.
        // bool: needTarget -> If true action will not fire if player does not have a target
        // Func<bool>: function -> Action to attempt, must return true or false.
        CombatActions.Add((true, () => false));



    }
    public override bool PassivePulse()
    {


        var me = Api.Player;
        var healthPercentage = me.HealthPercent;
        var mana = me.ManaPercent;
        var target = Api.Target;
        var reaction = me.GetReaction(target);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }

        if (me.IsValid())
        {

            if (Api.Spellbook.CanCast("Mark of the Wild") && !me.Auras.Contains("Mark of the Wild"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Mark of the Wild");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Mark of the Wild"))

                    return true;
            }
            if (Api.Spellbook.CanCast("Thorns") && !me.Auras.Contains("Thorns"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Thorns");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Thorns"))

                    return true;
            }

            if (Api.Spellbook.CanCast("Omen of Clarity") && !me.Auras.Contains("Omen of Clarity"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Omen of Clarity");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Omen of Clarity"))

                    return true;
            }
            if (Api.Spellbook.CanCast("Rejuvenation") && healthPercentage <= 60 && !me.Auras.Contains("Rejuvenation"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Rejuvenation");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Rejuvenation"))
                    return true;
            }

            if (Api.Spellbook.CanCast("Regrowth") && healthPercentage <= 40 && !me.Auras.Contains("Regrowth"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Regrowth");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Regrowth"))
                    return true;
            }
            if (Api.Spellbook.CanCast("Healing Touch") && healthPercentage <= 30)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Healing Touch");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Healing Touch"))
                    return true;
            }
            if (Api.Spellbook.CanCast("Moonkin Form") && !me.Auras.Contains("Moonkin Form", false))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Moonkin Form");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Moonkin Form"))

                    return true;
            }
        }
        if (target.IsValid())
        {
            if (!target.IsDead() && (reaction != UnitReaction.Friendly &&
                 reaction != UnitReaction.Honored &&
                 reaction != UnitReaction.Revered &&
                 reaction != UnitReaction.Exalted) &&
                mana > 20 && !IsNPC(target))
            {

                if (Api.Spellbook.CanCast("Entangling Roots") && !target.Auras.Contains("Entangling Roots"))
                {


                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Entangling Roots");
                    Console.ResetColor();

                    if (Api.Spellbook.Cast("Entangling Roots"))
                        return true; // Successful cast of Wrath
                                     // If unable to cast Moonfire, proceed to the next spell
                }
                else
                if (Api.Spellbook.CanCast("Moonfire") && !target.Auras.Contains("Moonfire"))
                {


                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Moonfire");
                    Console.ResetColor();

                    if (Api.Spellbook.Cast("Moonfire"))
                        return true; // Successful cast of Wrath
                                     // If unable to cast Moonfire, proceed to the next spell
                }
                else
                 if (Api.Spellbook.CanCast("Wrath"))
                {

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Wrath");
                    Console.ResetColor();

                    if (Api.Spellbook.Cast("Wrath"))
                    {
                        return true; // Successful cast of Wrath
                    }
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
        var targethealth = target.HealthPercent;
        var energy = me.Energy;
        var points = me.ComboPoints;
        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };
        if (!target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting()  || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;


        if (me.HealthPercent <= 70 && (!Api.Inventory.OnCooldown(MP) || !Api.Inventory.OnCooldown(HP)))
        {
            foreach (string hpot in HP)
            {
                if (HasItem(hpot))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Using Healing potion");
                    Console.ResetColor();
                    if (Api.Inventory.Use(hpot))
                    {
                        return true;
                    }
                }
            }
        }

        if (me.ManaPercent <= 50 && (!Api.Inventory.OnCooldown(MP) || !Api.Inventory.OnCooldown(HP)))
        {
            foreach (string manapot in MP)
            {
                if (HasItem(manapot))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Using mana potion");
                    Console.ResetColor();
                    if (Api.Inventory.Use(manapot))
                    {
                        return true;
                    }
                }
            }
        }

        if (Api.Spellbook.CanCast("Rejuvenation") && !me.Auras.Contains("Rejuvenation") && healthPercentage <= 70 && mana >= 15)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Rejuvenation");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Rejuvenation"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Healing Touch") && healthPercentage <= 45 && mana >= 20 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Healing Touch");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Healing Touch"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Healing Touch") && healthPercentage <= 45 && mana >= 20 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Healing Touch");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Healing Touch"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Moonkin Form") && !me.Auras.Contains("Moonkin Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Moonkin Form");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Moonkin Form"))

                return true;
        }
        if (Api.Spellbook.CanCast("Starfire") && me.Auras.Contains(417157) && mana > 10 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Starfire");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Starfire"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Moonfire") && !target.Auras.Contains("Moonfire") && targethealth > 30 && mana >= 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Moonfire");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Moonfire"))
            {
                return true;
            }
        }


        if (Api.Spellbook.CanCast("Wrath") && me.Auras.Contains(408248) && mana > 10 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Wrath with Eclipse");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Wrath"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Wrath") && mana > 10 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Wrath");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Wrath"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Attack") && !me.IsAutoAttacking())
        {
            Api.Spellbook.Cast("Attack");
            Console.WriteLine("Attacking");
            if (Api.Spellbook.Cast("Judgement"))

                return true;
        }

        return base.CombatPulse();
    }
    private bool IsNPC(WowUnit unit)
    {
        if (!IsValid(unit))
        {
            // If the unit is not valid, consider it not an NPC
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

        var mana = me.ManaPercent;
        var healthPercentage = me.HealthPercent;
        var target = Api.Target;
        var targethealth = target.HealthPercent;
        var reaction = me.GetReaction(target);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana} Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();
        Console.ResetColor();

        if (me.Auras.Contains("Fury of Stormrage"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Fury of Stormrage");
            Console.ResetColor();
        }

        if (target.IsValid() && !target.IsDead())
        {
            Console.WriteLine("Target is valid and not dead");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{targethealth}% Target Health");
            Console.WriteLine($"Target Reaction: {reaction}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("Target is not valid or is dead");
        }



        if (me.Auras.Contains("Mark of the Wild"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Have Mark of the Wild");
            Console.ResetColor();
        }
        if (me.Auras.Contains("Rejuvenation"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Have Rejuvenation");
            Console.ResetColor();
        }
        if (me.Auras.Contains(5234))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Have 5234");
            Console.ResetColor();
        }
        if (Api.Spellbook.CanCast("Mark of the Wild"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Can cast Mark of the Wild");
            Console.ResetColor();

        }

    }
}