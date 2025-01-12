using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.CombatLog;

public class Warrior : Rotation
{

    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;
    private bool overpowerAttempted = false;
    private DateTime lastOverpowerAttempt = DateTime.MinValue; // Tracks the last time Overpower was attempted
    private readonly TimeSpan overpowerCooldown = TimeSpan.FromSeconds(3); // 10-second cooldown

    private CreatureType GetCreatureType(WowUnit unit)

    {
        return unit.Info.GetCreatureType();
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


    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.

        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 550;
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
        var target = Api.Target;
        var rage = me.Rage / 10;



        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        var targetDistance = target.Position.Distance2D(me.Position);
        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now;
        }
        var reaction = me.GetReaction(target);
        if (target.IsValid())
        {

            if (!target.IsDead() &&
    (reaction != UnitReaction.Friendly &&
     reaction != UnitReaction.Honored &&
     reaction != UnitReaction.Revered &&
     reaction != UnitReaction.Exalted) &&
     !IsNPC(target) && healthPercentage > 80)
            {
                if (Api.Spellbook.CanCast("Charge") && targetDistance > 8 && targetDistance < 30 && !Api.Spellbook.OnCooldown("Charge"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Charge");
                    Console.ResetColor();

                    if (Api.Spellbook.Cast("Charge"))
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
        var rage = me.Rage / 10;
        var target = Api.Target;
        var targethealth = target.HealthPercent;

        // Check for the DODGE event
        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };

        if (me.HealthPercent <= 70 && !Api.Inventory.OnCooldown(HP))
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

        // Cast Bloodrage if appropriate
        if (Api.Spellbook.CanCast("Bloodrage") && me.HealthPercent >= 85 && !Api.Spellbook.OnCooldown("Bloodrage"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Bloodrage");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Bloodrage"))
                return true;
        }

        // Cast Hamstring if appropriate
        if (Api.Spellbook.CanCast("Hamstring") && targethealth <= 30 && !target.Auras.Contains("Hamstring") && rage > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Hamstring");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Hamstring"))
                return true;
        }

        // Cast Battle Shout if appropriate
        if (!me.Auras.Contains("Battle Shout") && Api.Spellbook.CanCast("Battle Shout") && rage > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Battle Shout");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Battle Shout"))
                return true;
        }
        if (!target.Auras.Contains("Demoralizing Shout") && Api.Spellbook.CanCast("Demoralizing Shout") && rage > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Demoralizing Shout");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Demoralizing Shout"))
                return true;
        }
        if (DateTime.Now - lastOverpowerAttempt >= overpowerCooldown && Api.Spellbook.CanCast("Overpower") && rage > 5 && !Api.Spellbook.OnCooldown("Overpower"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Attempting Overpower");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Overpower"))
            {
                lastOverpowerAttempt = DateTime.Now; // Set cooldown timer after successful cast
                return true;
            }
            else
            {
                lastOverpowerAttempt = DateTime.Now; // Set cooldown even if the cast fails
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Overpower attempt failed, starting cooldown.");
                Console.ResetColor();
            }
        }



        // Cast Execute if appropriate
        if (Api.Spellbook.CanCast("Execute") && targethealth <= 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Execute");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Execute"))
                return true;
        }

        // Cast Rend if appropriate
        CreatureType targetCreatureType = GetCreatureType(target);
        if (Api.Spellbook.CanCast("Rend") && targethealth >= 30 && !target.Auras.Contains("Rend") && rage > 10 && targetCreatureType != CreatureType.Mechanical)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Rend");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Rend"))
                return true;
        }

        // Cast Thunder Clap if appropriate
        if (Api.Spellbook.CanCast("Thunder Clap") && !target.Auras.Contains("Thunder Clap", true) && rage > 20 && targethealth >= 30 && Api.UnfriendlyUnitsNearby(5, true) >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Thunder Clap");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Thunder Clap"))
                return true;
        }

        // Cast Sunder Armor if appropriate
        if (Api.Spellbook.CanCast("Sunder Armor") && !target.Auras.Contains("Sunder Armor", true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Sunder Armor");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Sunder Armor"))
                return true;
        }

        // Cast Heroic Strike if appropriate
        if (Api.Spellbook.CanCast("Heroic Strike") && rage > 15)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Heroic Strike");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Heroic Strike"))
                return true;
        }

        // Cast Attack if appropriate
        if (Api.Spellbook.CanCast("Attack") && !me.IsAutoAttacking())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Attack");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Attack"))
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

        var rage = me.Rage / 10;
        var healthPercentage = me.HealthPercent;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{rage} Rage available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();



    }

}