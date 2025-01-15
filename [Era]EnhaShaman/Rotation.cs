using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;


public class EnhaShaman : Rotation
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
    private bool HasItem(object item) => Api.Inventory.HasItem(item);
    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;
    private DateTime searingTotemLastCast = DateTime.MinValue; // Tracks when Searing Totem was last cast
    private readonly TimeSpan searingTotemDuration = TimeSpan.FromSeconds(30); // Duration of Searing Totem

    private bool HasEnchantment(EquipmentSlot slot, string enchantmentName)
    {
        return Api.Equipment.HasEnchantment(slot, enchantmentName);
    }

    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.

        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 800;
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
        var Level = me.Level;
        var target = Api.Target;

        var targetDistance = target.Position.Distance2D(me.Position);

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling()) return false;
        if (me.Auras.Contains("Drink") || me.Auras.Contains("Food") || me.IsMounted()) return false;
        if (me.IsValid())
        {
            // Check if the main hand weapon doesn't have any Rockbiter enchantment
            bool hasAnyRockbiterEnchantment = HasAnyRockbiterEnchantment(EquipmentSlot.MainHand);
            if (!hasAnyRockbiterEnchantment && Api.Spellbook.CanCast("Rockbiter Weapon"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Rockbiter Weapon on main hand");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Rockbiter Weapon"))
                {
                    return true;
                }
            }

           

            if (Api.Spellbook.CanCast("Ghost Wolf") && !me.Auras.Contains("Ghost Wolf", false) && mana > 42 && healthPercentage > 50)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Ghost Wolf");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Ghost Wolf"))
                {
                    return true;
                }
            }

            if (Api.Spellbook.CanCast("Lightning Shield") && !me.Auras.Contains("Lightning Shield") && mana > 30)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Lighting Shield");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Lightning Shield"))
                {
                    return true;
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
                    Console.WriteLine("Trying to cast Lightning Bolt");

                    // Try casting Frostbolt
                    if (Api.Spellbook.CanCast("Lightning Bolt"))
                    {
                        Api.Spellbook.Cast("Lightning Bolt");
                        Console.WriteLine("Casting Lightning Bolt");
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
        var targetDistance = target.Position.Distance2D(me.Position);
        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };
        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        if (UsePotions())
        {
            return true; // Exit early if a potion was used
        }

       
        if (Api.Spellbook.CanCast("Earth Shock") && mana > 20 && !Api.Spellbook.OnCooldown("Earth Shock") && (target.IsCasting() || target.IsChanneling()))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Earth Shock");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Earth Shock"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Strength of Earth Totem") && !me.Auras.Contains("Strength of Earth", false) && mana > 50 && Api.UnfriendlyUnitsNearby(5, true) == 1)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Strength of Earth Totem");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Strength of Earth Totem"))
            {
                return true; // Stop here if successfully cast
            }
        }

        // Fallback to Stoneskin Totem if Strength of Earth can't be cast
        if (Api.Spellbook.CanCast("Stoneskin Totem") && !me.Auras.Contains("Stoneskin", false)  && mana > 50 && Api.UnfriendlyUnitsNearby(5, true) >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Stoneskin Totem");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Stoneskin Totem"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Healing Stream Totem") && !me.Auras.Contains("Healing Stream", false) && mana > 50 && Api.UnfriendlyUnitsNearby(5, true) >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Healing Stream Totem");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Healing Stream Totem"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Searing Totem")
        && DateTime.Now - searingTotemLastCast >= searingTotemDuration // Check if duration has elapsed
        && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Searing Totem");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Searing Totem"))
            {
                searingTotemLastCast = DateTime.Now; // Update last cast time
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Failed to cast Searing Totem");
                Console.ResetColor();
            }
        }
        if (Api.Spellbook.CanCast("Grace of Air Totem") && !me.Auras.Contains("Grace of Air", false) && mana > 50 && Api.UnfriendlyUnitsNearby(5, true) >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Grace of Air Totem");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Grace of Air Totem"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Lightning Shield") && !me.Auras.Contains("Lightning Shield") && mana > 30)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Lighting Shield");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Lightning Shield"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Healing Wave") && healthPercentage <= 50 && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Healing Wave");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Healing Wave"))
            {
                return true;
            }
        }
        
        bool hasAnyFlametongueEnchantment = HasAnyFlametongueEnchantment(EquipmentSlot.OffHand);

        if (Api.Spellbook.CanCast("Frost Shock") && !Api.Spellbook.OnCooldown("Frost Shock") && mana > 20 && !target.Auras.Contains("Frost Shock"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Frost Shock");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Frost Shock"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Flame Shock") && !Api.Spellbook.OnCooldown("Flame Shock") && !target.Auras.Contains("Flame Shock") && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Flame Shock");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Flame Shock"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Stormstrike") &&  !Api.Spellbook.OnCooldown("Stormstrike") && mana >25)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Stormstrike");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Stormstrike"))
            {
                return true;
            }
        }


        if (Api.Spellbook.CanCast("Lightning Bolt") && targetDistance > 10 && mana >20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Lightning Bolt");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Lightning Bolt"))
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


        return base.CombatPulse();
    }


    private bool HasAnyRockbiterEnchantment(EquipmentSlot slot)
    {
        return HasEnchantment(slot, "Rockbiter 7") || HasEnchantment(slot, "Rockbiter 6") || HasEnchantment(slot, "Rockbiter 5") || HasEnchantment(slot, "Rockbiter 4") || HasEnchantment(slot, "Rockbiter 3") || HasEnchantment(slot, "Rockbiter 2") || HasEnchantment(slot, "Rockbiter 1");
    }

    private bool HasAnyFlametongueEnchantment(EquipmentSlot slot)
    {
        return HasEnchantment(slot, "Flametongue 6") || HasEnchantment(slot, "Flametongue 5") || HasEnchantment(slot, "Flametongue 4") || HasEnchantment(slot, "Flametongue 3") || HasEnchantment(slot, "Flametongue 2") || HasEnchantment(slot, "Flametongue 1");
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
    public bool UsePotions()
    {
        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };

        // Check for health potions if health is low
        if (Api.Player.HealthPercent <= 70)
        {
            foreach (string hpot in HP)
            {
                int potionCount = Api.Inventory.ItemCount(hpot);

                // Check cooldown for potions
                bool isOnCooldown = potionCooldowns.ContainsKey("Potion") && (DateTime.Now - potionCooldowns["Potion"]).TotalSeconds < 130;

                if (potionCount > 0 && !isOnCooldown)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Using {hpot} for healing.");
                    Console.ResetColor();

                    if (Api.Inventory.Use(hpot))
                    {
                        potionCooldowns["Potion"] = DateTime.Now; // Update the cooldown
                        return true; // Exit early after using the potion
                    }
                }
            }
        }

        // Check for mana potions if mana is low
        if (Api.Player.ManaPercent < 70)
        {
            foreach (string mpot in MP)
            {
                int potionCount = Api.Inventory.ItemCount(mpot);

                // Check cooldown for potions
                bool isOnCooldown = potionCooldowns.ContainsKey("Potion") && (DateTime.Now - potionCooldowns["Potion"]).TotalSeconds < 130;

                if (potionCount > 0 && !isOnCooldown)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Using {mpot} for mana.");
                    Console.ResetColor();

                    if (Api.Inventory.Use(mpot))
                    {
                        potionCooldowns["Potion"] = DateTime.Now; // Update the cooldown
                        return true; // Exit early after using the potion
                    }
                }
            }
        }

        return false; // No potions were used
    }

    private void LogPlayerStats()
    {
        var me = Api.Player;

        var mana = me.Mana;
        var healthPercentage = me.HealthPercent;


        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana} Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();
        bool hasLava = HasEnchantment(EquipmentSlot.Hands, "Lava Lash");

        if (hasLava)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Has Lava Lash");
            Console.ResetColor();

        }
        bool hasMolten = HasEnchantment(EquipmentSlot.Hands, "Molten Blast");

        if (hasMolten)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Has Molten Blast");
            Console.ResetColor();

        }
        Console.ResetColor();
    }
}