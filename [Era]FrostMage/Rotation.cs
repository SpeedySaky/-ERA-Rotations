using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;

public class EraFrostMage : Rotation
{
    private List<string> npcConditions = new List<string>
    {
        "Innkeeper", "Auctioneer", "Banker", "FlightMaster", "GuildBanker",
        "PlayerVehicle", "StableMaster", "Repair", "Trainer", "TrainerClass",
        "TrainerProfession", "Vendor", "VendorAmmo", "VendorFood", "VendorPoison",
        "VendorReagent", "WildBattlePet", "GarrisonMissionNPC", "GarrisonTalentNPC",
        "QuestGiver"
    };
    private Dictionary<string, DateTime> potionCooldowns = new Dictionary<string, DateTime>();

    public bool IsValid(WowUnit unit)
    {
        if (unit == null || unit.Address == null)
        {
            return false;
        }
        return true;
    }
    private CreatureType GetCreatureType(WowUnit unit)
    {
        return unit.Info.GetCreatureType();
    }
    private bool HasEnchantment(EquipmentSlot slot, string enchantmentName)
    {
        return Api.Equipment.HasEnchantment(slot, enchantmentName);
    }

    private bool HasItem(object item) => Api.Inventory.HasItem(item);
    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastPyroblastTime = DateTime.MinValue; // Track the last time Pyroblast was cast
    private DateTime lastDebugTime = DateTime.MinValue;
    private DateTime lastPyro = DateTime.MinValue;
    public override void Initialize()
    {
        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        SlowTick = 700;
        FastTick = 15;

        PassiveActions.Add((true, () => false));
        CombatActions.Add((true, () => false));
    }

    public override bool PassivePulse()
    {
        var me = Api.Player;
        var target = Api.Target;
        var pet = me.Pet();

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now;
        }

        var healthPercentage = me.HealthPercent;
        var mana = me.ManaPercent;
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsLooting() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        var hasaura = me.Auras.Contains("Curse of Stalvan") || me.Auras.Contains("Curse of Blood");
        if (me.IsValid())
        {
            if (hasaura && Api.Spellbook.CanCast("Remove Lesser Curse"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Decursing");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Remove Lesser Curse"))
                {
                    return true;
                }
            }

            if (Api.Spellbook.CanCast("Ice Armor") && !me.Auras.Contains("Ice Armor", true))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Ice Armor");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Ice Armor"))
                    return true;
            }

            if (Api.Spellbook.CanCast("Frost Armor") && !me.Auras.Contains("Frost Armor", true) && !me.Auras.Contains("Ice Armor", true))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Frost Armor");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Frost Armor"))
                {
                    return true;
                }
            }

            if (Api.Spellbook.CanCast("Arcane Intellect") && !me.Auras.Contains("Arcane Intellect", true))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Arcane Intellect");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Arcane Intellect"))
                {
                    return true;
                }
            }



            string[] waterTypes = { "Conjured Mana Strudel", "Conjured Mountain Spring Water", "Conjured Crystal Water", "Conjured Sparkling Water", "Conjured Mineral Water", "Conjured Spring Water", "Conjured Purified Water", "Conjured Fresh Water", "Conjured Water" };
            string[] foodTypes = { "Conjured Mana Strudel", "Conjured Cinnamon Roll", "Conjured Sweet Roll", "Conjured Sourdough", "Conjured Pumpernickel", "Conjured Rye", "Conjured Bread", "Conjured Muffin" };
            bool needsWater = false;
            bool needsFood = false;

            foreach (string waterType in waterTypes)
            {
                if (Api.Inventory.HasItem(waterType))
                {
                    needsWater = true;
                    break;
                }
            }

            foreach (string foodType in foodTypes)
            {
                if (Api.Inventory.HasItem(foodType))
                {
                    needsFood = true;
                    break;
                }
            }

            if (!needsWater)
            {
                if (Api.Spellbook.CanCast("Conjure Water"))
                {
                    if (Api.Spellbook.Cast("Conjure Water"))
                    {
                        Console.WriteLine("Conjured water.");
                        return true;
                    }
                }
            }

            if (!needsFood)
            {
                if (Api.Spellbook.CanCast("Conjure Food"))
                {
                    if (Api.Spellbook.Cast("Conjure Food"))
                    {
                        Console.WriteLine("Conjured Food.");
                        return true;
                    }
                }
            }
        }

        var reaction = me.GetReaction(target);
        if (target.IsValid())
        {
            if (!target.IsDead() &&
                (reaction != UnitReaction.Friendly &&
                 reaction != UnitReaction.Honored &&
                 reaction != UnitReaction.Revered &&
                 reaction != UnitReaction.Exalted) &&
                mana > 20 && !IsNPC(target))
            {
                Console.WriteLine("Trying to cast Frostbolt");

                if (Api.Spellbook.CanCast("Frostbolt"))
                {
                    Api.Spellbook.Cast("Frostbolt");
                    Console.WriteLine("Casting Frostbolt");
                    return true;
                }
            }
        }

        return base.PassivePulse();
    }

    public override bool CombatPulse()
    {
        var me = Api.Player;
        var target = Api.Target;
        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;
        var mana = me.ManaPercent;
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        var hasaura = me.Auras.Contains("Curse of Stalvan") || me.Auras.Contains("Curse of Blood");

        if (UsePotions())
        {
            return true;
        }

        string[] GemTypes = { "Mana Jade", "Mana Citrine", "Mana Ruby", "Mana Emerald", "Mana Sapphire", "Mana Agate" };

        if (me.Mana <= 30 && !Api.Inventory.OnCooldown(GemTypes))
        {
            foreach (string gem in GemTypes)
            {
                if (HasItem(gem))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Using {gem}");
                    Console.ResetColor();

                    if (Api.Inventory.Use(gem))
                    {
                        return true;
                    }
                }
            }
        }

        if (Api.Spellbook.CanCast("Frost Nova") && !Api.Spellbook.OnCooldown("Frost Nova") && targetDistance >= 6 && targetDistance <= 12)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Frost Nova");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Frost Nova"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Ice Lance") && mana > 15 && !Api.Spellbook.OnCooldown("Ice Lance") && targetDistance < 25)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Ice Lance");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Ice Lance"))
            {
                return true;
            }
        }

        if (hasaura && Api.Spellbook.CanCast("Remove Lesser Curse"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Decursing");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Remove Lesser Curse"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Counterspell") && !Api.Spellbook.OnCooldown("Counterspell") && (target.IsCasting() || target.IsChanneling()) && targetDistance < 28)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Counterspell");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Counterspell"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Evocation") && !Api.Spellbook.OnCooldown("Evocation") && mana <= 10 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Evocation");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Evocation"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Ice Block") && healthPercentage < 20 && !Api.Spellbook.OnCooldown("Ice Block"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Ice Block");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Ice Block"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Ice Barrier") && !me.Auras.Contains("Ice Barrier", true) && !Api.Spellbook.OnCooldown("Ice Barrier"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Ice Barrier");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Ice Barrier"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Mana Shield") && healthPercentage < 50 && mana > 20 && !me.Auras.Contains("Mana Shield", true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Mana Shield");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mana Shield"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Frostbolt") && mana > 20 && targethealth > 20 && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Frostbolt");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Frostbolt"))
            {
                return true;
            }
        }

        if (Api.Equipment.HasItem(EquipmentSlot.Extra) && Api.Spellbook.CanCast("Shoot") && !me.IsShooting() && !me.IsMoving())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ranged weapon is equipped. Attempting to cast Shoot.");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Shoot"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Attack") && !me.IsAutoAttacking() && !me.IsShooting() && !me.IsMoving())
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

    public bool UsePotions()
    {
        // Check for health potions if health is low
        if (Api.Player.HealthPercent <= 70)
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



    private void LogPlayerStats()
    {
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        var mana = me.Mana;

        // Health percentage of the player
        var healthPercentage = me.HealthPercent;

        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana} Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();

        if (me.Auras.Contains("Frost Armor")) // Replace "Thorns" with the actual aura name
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.ResetColor();
            var remainingTimeSeconds = me.Auras.TimeRemaining("Frost Armor");
            var remainingTimeMinutes = remainingTimeSeconds / 60; // Convert seconds to minutes
            var roundedMinutes = Math.Round(remainingTimeMinutes / 1000, 1); // Round to one decimal place

            Console.WriteLine($"Remaining time for Frost Armor: {roundedMinutes} minutes");
            Console.ResetColor();
        }

        // Define food and water types
        string[] waterTypes = { "Conjured Mana Strudel", "Conjured Mountain Spring Water", "Conjured Crystal Water", "Conjured Sparkling Water", "Conjured Mineral Water", "Conjured Spring Water", "Conjured Purified Water", "Conjured Fresh Water", "Conjured Water" };
        string[] foodTypes = { "Conjured Mana Strudel", "Conjured Cinnamon Roll", "Conjured Sweet Roll", "Conjured Sourdough", "Conjured Pumpernickel", "Conjured Rye", "Conjured Bread", "Conjured Muffin" };
        string[] healthPotions = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] manaPotions = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };

        // Count food items in the inventory
        int foodCount = 0;
        foreach (string foodType in foodTypes)
        {
            int count = Api.Inventory.ItemCount(foodType);
            foodCount += count;
        }

        // Count water items in the inventory
        int waterCount = 0;
        foreach (string waterType in waterTypes)
        {
            int count = Api.Inventory.ItemCount(waterType);
            waterCount += count;
        }

        // Display the counts of food and water items
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Current Food Count: " + foodCount);
        Console.WriteLine("Current Water Count: " + waterCount);
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

        var hasaura = me.Auras.Contains("Curse of Stalvan");
        Console.ResetColor();
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
