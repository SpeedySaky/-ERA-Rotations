using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;


public class EraBearDruid : Rotation
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
        SlowTick = 750;
        FastTick = 300;

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

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }



        if (Api.Spellbook.CanCast("Mark of the Wild") && !me.Auras.Contains("Mark of the Wild") && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Mark of the Wild");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mark of the Wild"))

                return true;
        }
        if (Api.Spellbook.CanCast("Thorns") && !me.Auras.Contains("Thorns") && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Thorns");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Thorns"))

                return true;
        }

        if (Api.Spellbook.CanCast("Omen of Clarity") && !me.Auras.Contains("Omen of Clarity") && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Omen of Clarity");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Omen of Clarity"))

                return true;
        }
        if (Api.Spellbook.CanCast("Rejuvenation") && healthPercentage <= 60 && !me.Auras.Contains("Rejuvenation") && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Rejuvenation");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Rejuvenation"))
                return true;
        }

        if (Api.Spellbook.CanCast("Regrowth") && healthPercentage <= 40 && !me.Auras.Contains("Regrowth") && !me.IsMoving() && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Regrowth");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Regrowth"))
                return true;
        }
        if (Api.Spellbook.CanCast("Healing Touch") && healthPercentage <= 30 && !me.IsMoving() && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Healing Touch");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Healing Touch"))
                return true;
        }


        if (Api.Spellbook.CanCast("Bear Form") && !me.Auras.Contains("Bear Form", false) && mana > 55)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Bear Form");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Bear Form"))
            {
                return true;
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



        return base.PassivePulse();
    }

    public override bool CombatPulse()
    {
        var me = Api.Player;
        var healthPercentage = me.HealthPercent;
        var mana = me.ManaPercent;
        var target = Api.Target;
        var targethealth = target.HealthPercent;
        var rage = me.Rage;

        if (!target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting()  || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        // Use a healing potion if health is low
        if (me.HealthPercent <= 30 && !Api.Inventory.OnCooldown("Healing Potion"))
        {
            if (HasItem("Healing Potion"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Using Healing Potion");
                Console.ResetColor();
                if (Api.Inventory.Use("Healing Potion"))
                {
                    return true;
                }
            }
        }
        // Cast Rejuvenation if health is low and mana is sufficient
        if (Api.Spellbook.CanCast("Rejuvenation") && !me.Auras.Contains("Rejuvenation") && healthPercentage <= 40 && mana >= 60)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Rejuvenation");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Rejuvenation"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Bear Form") && !me.Auras.Contains("Bear Form", false) && mana >= 55)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Bear Form");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Bear Form"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Skull Bash") && (target.IsCasting() || target.IsChanneling()) && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Skull Bash");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Skull Bash"))
            {
                return true;
            }
        }
        if (rage >= 10 && Api.Spellbook.CanCast("Frenzied Regeneration") && healthPercentage <= 40 && !Api.Spellbook.OnCooldown("Frenzied Regeneration") && Api.UnitsTargetingMe(5, true).Length >= 2 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Frenzied Regeneration");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Frenzied Regeneration"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Berserk") && !Api.Spellbook.OnCooldown("Berserk") && Api.UnitsTargetingMe(5, true).Length >= 2 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Berserk");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Berserk"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Survival Instincts") && !Api.Spellbook.OnCooldown("Survival Instincts") && Api.UnitsTargetingMe(5, true).Length >= 2 && healthPercentage <= 40 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Survival Instincts");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Survival Instincts"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Barkskin") && !Api.Spellbook.OnCooldown("Barkskin") && Api.UnitsTargetingMe(5, true).Length >= 2 && healthPercentage <= 40 && mana > 20 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Barkskin");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Barkskin"))
            {
                return true;
            }
        }
        // Use Enrage to generate rage if needed
        if (rage < 20 && Api.Spellbook.CanCast("Enrage") && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Enrage");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Enrage"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Faerie Fire (Feral)") && !target.Auras.Contains("Faerie Fire (Feral)") && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Faerie Fire (Feral)");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Faerie Fire (Feral)"))
            {
                return true;
            }
        }
        

        // Ensure we are in Bear Form

        if (Api.Spellbook.CanCast("Mangle (Bear)") && !target.Auras.Contains("Mangle") && rage > 20 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Mangle (Bear)");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mangle (Bear)"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Lacerate") && !target.Auras.Contains("Lacerate") && rage > 10 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Lacerate");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Lacerate"))
            {
                return true;
            }
        }
        // Use tanking abilities
        if (rage >= 15 && Api.Spellbook.CanCast("Maul") && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Maul");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Maul"))
            {
                return true;
            }
        }

        if (rage >= 10 && Api.Spellbook.CanCast("Swipe") && Api.UnitsTargetingMe(5, true).Length >= 2 && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Swipe");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Swipe"))
            {
                return true;
            }
        }

        if (rage >= 5 && Api.Spellbook.CanCast("Demoralizing Roar") && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Demoralizing Roar");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Demoralizing Roar"))
            {
                return true;
            }
        }

        if (rage >= 15 && Api.Spellbook.CanCast("Bash") && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Bash");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Bash"))
            {
                return true;
            }
        }

        if (rage >= 10 && Api.Spellbook.CanCast("Growl") && me.Auras.Contains("Bear Form", false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Growl");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Growl"))
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