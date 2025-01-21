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
    private readonly TimeSpan overpowerCooldown = TimeSpan.FromSeconds(5); // 10-second cooldown
    private Dictionary<string, DateTime> potionCooldowns = new Dictionary<string, DateTime>();

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
        SlowTick = 850;
        FastTick = 500;

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



        if (me.IsDead() || me.IsGhost() || me.IsCasting()  || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        var targetDistance = target.Position.Distance2D(me.Position);
        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now;
        }
        if (!me.Auras.Contains("Battle Stance",false) && Api.Spellbook.CanCast("Battle Stance") )
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Battle Stance");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Battle Stance"))
                return true;
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
                if (Api.Spellbook.CanCast("Charge") && targetDistance > 8 && targetDistance < 30 && !Api.Spellbook.OnCooldown("Charge") && me.Auras.Contains("Battle Stance", false))
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
        var unitsTargetingMe = Api.UnitsTargetingMe(5, true).Length;

        // Check for the DODGE event
        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        if (UsePotions())
        {
            return true; // Exit early if a potion was used
        }
        if (Api.Spellbook.CanCast("Attack") && !me.IsAutoAttacking())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Attack");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Attack"))
                return true;
        }
        if (Api.Spellbook.CanCast("Bloodrage") && me.HealthPercent >= 70 && !Api.Spellbook.OnCooldown("Bloodrage"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Bloodrage");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Bloodrage"))
                return true;
        }

        // Apply Sunder Armor until the target has at least 2 stacks (only in single-target fights)
        if (unitsTargetingMe == 1)
        {
            if (!me.Auras.Contains("Defensive Stance", false) && Api.Spellbook.CanCast("Defensive Stance") && !target.Auras.Contains("Sunder Armor"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Defensive Stance");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Defensive Stance"))
                    return true;
            }

            if (me.Auras.Contains("Defensive Stance", false) && Api.Spellbook.CanCast("Sunder Armor") && !target.Auras.Contains("Sunder Armor")  && rage > 15)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Sunder Armor");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Sunder Armor"))
                    return true;
            }
        }

        // Continue with the rest of the combat rotation
        if (target.Auras.Contains("Sunder Armor") || unitsTargetingMe > 1)
        {
            if (!me.Auras.Contains("Battle Stance", false) && Api.Spellbook.CanCast("Battle Stance"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Switching to Battle Stance");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Battle Stance"))
                    return true;
            }

            // Switch to Battle Stance if Overpower is available
            if (DateTime.Now - lastOverpowerAttempt >= overpowerCooldown && Api.Spellbook.CanCast("Overpower") && rage > 5 && !Api.Spellbook.OnCooldown("Overpower") && !me.Auras.Contains("Battle Stance", false))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Switching to Battle Stance for Overpower");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Battle Stance"))
                    return true;
            }

            // Cast Overpower in Battle Stance
            if (me.Auras.Contains("Battle Stance", false) && Api.Spellbook.CanCast("Overpower") && rage > 5 && !Api.Spellbook.OnCooldown("Overpower") && DateTime.Now - lastOverpowerAttempt >= overpowerCooldown)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Attempting Overpower at {DateTime.Now}. Conditions: CanCast={Api.Spellbook.CanCast("Overpower")}, Rage={rage}, OnCooldown={Api.Spellbook.OnCooldown("Overpower")}");
                Console.ResetColor();

                bool castResult = Api.Spellbook.Cast("Overpower"); // Store the result for clarity

                if (castResult)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Overpower cast successfully!");
                    Console.ResetColor();

                    lastOverpowerAttempt = DateTime.Now; // Set cooldown timer after successful cast
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Overpower attempt failed, starting cooldown.");
                    Console.ResetColor();

                    lastOverpowerAttempt = DateTime.Now; // Start cooldown even if the cast fails
                }
            }

            // Switch to Berserker Stance for DPS abilities
            if (!me.Auras.Contains("Berserker Stance", false) && (Api.Spellbook.CanCast("Whirlwind") || Api.Spellbook.CanCast("Berserker Rage")))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Switching to Berserker Stance for DPS");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Berserker Stance"))
                    return true;
            }

            // Cast Execute in Battle Stance or Berserker Stance
            if ((me.Auras.Contains("Battle Stance", false) || me.Auras.Contains("Berserker Stance", false)) && Api.Spellbook.CanCast("Execute") && targethealth <= 20 && !Api.Spellbook.OnCooldown("Execute") && rage > 15)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Execute");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Execute"))
                    return true;
            }

            // Cast Whirlwind in Berserker Stance
            if (me.Auras.Contains("Berserker Stance", false) && Api.Spellbook.CanCast("Whirlwind") && !Api.Spellbook.OnCooldown("Whirlwind") && rage > 25)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Whirlwind");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Whirlwind"))
                    return true;
            }

            // Cast Berserker Rage in Berserker Stance
            if (me.Auras.Contains("Berserker Stance", false) && Api.Spellbook.CanCast("Berserker Rage") && !Api.Spellbook.OnCooldown("Berserker Rage"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Berserker Rage");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Berserker Rage"))
                    return true;
            }

            // Switch back to Battle Stance if not already in it
            if (!me.Auras.Contains("Battle Stance", false) && !Api.Spellbook.OnCooldown("Battle Stance"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Switching to Battle Stance");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Battle Stance"))
                    return true;
            }

            // Cast other abilities in Battle Stance
            if (me.Auras.Contains("Battle Stance", false))
            {
                if (Api.Spellbook.CanCast("Retaliation") && !Api.Spellbook.OnCooldown("Retaliation") && unitsTargetingMe >= 2)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Retaliation");
                    Console.ResetColor();
                    if (Api.Spellbook.Cast("Retaliation"))
                        return true;
                }

                if (Api.Spellbook.CanCast("Recklessness") && me.HealthPercent >= 60 && !Api.Spellbook.OnCooldown("Recklessness") && unitsTargetingMe >= 2)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Recklessness");
                    Console.ResetColor();
                    if (Api.Spellbook.Cast("Recklessness"))
                        return true;
                }

                if (Api.Spellbook.CanCast("Hamstring") && targethealth <= 30 && !target.Auras.Contains("Hamstring") && rage > 10)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Hamstring");
                    Console.ResetColor();
                    if (Api.Spellbook.Cast("Hamstring"))
                        return true;
                }

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

                if (Api.Spellbook.CanCast("Sweeping Strikes") && unitsTargetingMe >= 2 && !Api.Spellbook.OnCooldown("Sweeping Strikes") && rage > 30)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Casting Sweeping Strikes");
                    Console.ResetColor();

                    if (Api.Spellbook.Cast("Sweeping Strikes"))
                        return true;
                }

                if (Api.Spellbook.CanCast("Mortal Strike") && rage > 30 && !target.Auras.Contains("Mortal Strike"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Mortal Strike");
                    Console.ResetColor();
                    if (Api.Spellbook.Cast("Mortal Strike"))
                        return true;
                }

                if (Api.Spellbook.CanCast("Thunder Clap") && !target.Auras.Contains("Thunder Clap", true) && rage > 20 && targethealth >= 30 && unitsTargetingMe >= 2)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Thunder Clap");
                    Console.ResetColor();
                    if (Api.Spellbook.Cast("Thunder Clap"))
                        return true;
                }
            }

            // Cast Cleave if appropriate
            if (Api.Spellbook.CanCast("Cleave") && rage > 20 && unitsTargetingMe >= 2)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Cleave");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Cleave"))
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
        }

        return base.CombatPulse();
    }





    public bool UsePotions()
    {
        // Check for health potions if health is low
        if (Api.Player.HealthPercent <= 60)
        {
            if (UsePotion("Major Healing Potion")) return true;
            if (UsePotion("Superior Healing Potion")) return true;
            if (UsePotion("Greater Healing Potion")) return true;
            if (UsePotion("Healing Potion")) return true;
            if (UsePotion("Lesser Healing Potion")) return true;
            if (UsePotion("Minor Healing Potion")) return true;
        }

        // Check for mana potions if mana is low
        if (Api.Player.ManaPercent < 30)
        {
            if (UsePotion("Major Mana Potion")) return true;
            if (UsePotion("Superior Mana Potion")) return true;
            if (UsePotion("Greater Mana Potion")) return true;
            if (UsePotion("Mana Potion")) return true;
            if (UsePotion("Lesser Mana Potion")) return true;
            if (UsePotion("Minor Mana Potion")) return true;
        }

        return false; // No potions were used
    }

    private bool UsePotion(string potionName)
    {
        int potionCount = Api.Inventory.ItemCount(potionName);

        // Check cooldown for potions
        bool isOnCooldown = potionCooldowns.ContainsKey("Potion") && (DateTime.Now - potionCooldowns["Potion"]).TotalSeconds < 130;

        if (potionCount > 0 && !isOnCooldown)
        {
            Console.ForegroundColor = potionName.Contains("Mana") ? ConsoleColor.Cyan : ConsoleColor.Green;
            Console.WriteLine($"Using {potionName}.");
            Console.ResetColor();

            if (Api.Inventory.Use(potionName))
            {
                potionCooldowns["Potion"] = DateTime.Now; // Update the cooldown
                return true; // Exit early after using the potion
            }
        }

        return false; // Potion was not used
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
        var target = Api.Target;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{rage} Rage available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();
        // Log available health potions
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Available Health Potions:");
        LogPotionCount("Major Healing Potion");
        LogPotionCount("Superior Healing Potion");
        LogPotionCount("Greater Healing Potion");
        LogPotionCount("Healing Potion");
        LogPotionCount("Lesser Healing Potion");
        LogPotionCount("Minor Healing Potion");
        Console.ResetColor();

        // Log available mana potions
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Available Mana Potions:");
        LogPotionCount("Major Mana Potion");
        LogPotionCount("Superior Mana Potion");
        LogPotionCount("Greater Mana Potion");
        LogPotionCount("Mana Potion");
        LogPotionCount("Lesser Mana Potion");
        LogPotionCount("Minor Mana Potion");
        Console.ResetColor();
        // Log Sunder Armor stacks
        if (target != null && target.Auras.Contains("Sunder Armor"))
        {
            int sunderArmorStacks = target.Auras.GetStacks("Sunder Armor");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Sunder Armor stacks on target: {sunderArmorStacks}");
            Console.ResetColor();
        }
        // Log potion cooldown timer
        if (potionCooldowns.ContainsKey("Potion"))
        {
            var cooldownRemaining = 130 - (DateTime.Now - potionCooldowns["Potion"]).TotalSeconds;
            if (cooldownRemaining > 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Potion cooldown remaining: {Math.Ceiling(cooldownRemaining)} seconds");
                Console.ResetColor();
            }
        }
    }
    private void LogPotionCount(string potionName)
    {
        int count = Api.Inventory.ItemCount(potionName);
        Console.WriteLine($"Checking {potionName}: {count}");
        if (count > 0)
        {
            Console.WriteLine($"{potionName}: {count}");
        }
    }
}