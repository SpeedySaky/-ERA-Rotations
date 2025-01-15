using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;
using wShadow.WowBots;
using wShadow.WowBots.PartyInfo;


public class RogueNoStealth : Rotation
{
    private DateTime lastRiposteAttempt = DateTime.MinValue; // Tracks the last time Riposte was attempted
    private readonly TimeSpan riposteCooldown = TimeSpan.FromSeconds(3); // 10-second cooldown for Riposte
    private Dictionary<string, DateTime> potionCooldowns = new Dictionary<string, DateTime>();

    private bool HasEnchantment(EquipmentSlot slot, string enchantmentName)
    {
        return Api.Equipment.HasEnchantment(slot, enchantmentName);
    }
    private CreatureType GetCreatureType(WowUnit unit)
    {
        return unit.Info.GetCreatureType();
    }
    private bool HasItem(object item)
        => Api.Inventory.HasItem(item);
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
    private Dictionary<string, DateTime> potionCooldowns = new Dictionary<string, DateTime>();
    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;

    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.

        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 1550;
        FastTick = 1000;

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
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        // Health percentage of the player
        var healthPercentage = me.HealthPercent;

        // Power percentages for different resources
        var energy = me.Energy; // Energy
        var points = me.ComboPoints;

        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);
        string[] instantPoisons = { "Instant Poison", "Instant Poison II", "Instant Poison III", "Instant Poison IV", "Instant Poison V", "Instant Poison VI", "Instant Poison VII" };
        string[] cripplingPoisons = { "Crippling Poison", "Crippling Poison II" };
        bool hasOffhandEnchantment = Api.Equipment.HasEnchantment(EquipmentSlot.OffHand);
        bool hasMainHandEnchantment = Api.Equipment.HasEnchantment(EquipmentSlot.MainHand);
        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        foreach (var poison in instantPoisons)
        {
            int poisonCount = Api.Inventory.ItemCount(poison);
            if (!hasMainHandEnchantment && poisonCount >= 1 && Api.HasMacro("Mainhand"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Casting {poison} on mainhand");
                Console.ResetColor();
                if (Api.UseMacro("Mainhand"))
                {
                    Console.WriteLine($"Successfully casted {poison} on mainhand");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to cast {poison} on mainhand");
                }
            }
        }
        foreach (var poison in cripplingPoisons)
        {
            int poisonCount = Api.Inventory.ItemCount(poison);
            if (!hasOffhandEnchantment && poisonCount >= 1 && Api.HasMacro("Offhand"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Casting {poison} on off-hand");
                Console.ResetColor();
                if (Api.UseMacro("Offhand"))
                {
                    Console.WriteLine($"Successfully casted {poison} on off-hand");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to cast {poison} on off-hand");
                }
            }
        }


        //if (Api.Spellbook.CanCast("Stealth") && !Api.Spellbook.OnCooldown("Stealth") && !me.Auras.Contains("Stealth", false) && targetDistance<=25)
        //{
        //    Console.ForegroundColor = ConsoleColor.Green;
        //    Console.WriteLine("Casting Stealth");
        //    Console.ResetColor();
        //    if (Api.Spellbook.Cast("Stealth"))
        //    {
        //        return true;
        //    }
        //}
       
        if (Api.Spellbook.CanCast("Sprint") && !Api.Spellbook.OnCooldown("Sprint") && targetDistance >= 40)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Sprint");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Sprint"))
            {
                return true;
            }
        }

        return base.PassivePulse();

    }

    public override bool CombatPulse()
    {
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        // Health percentage of the player
        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;
        var energy = me.Energy; // Energy
        var points = me.ComboPoints;

        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        if (UsePotions())
        {
            return true; // Exit early if a potion was used
        }



        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);

        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;


        if ( DateTime.Now - lastRiposteAttempt >= riposteCooldown // Cooldown check
        && !Api.Spellbook.OnCooldown("Riposte") // API cooldown check
        && energy > 10) // Sufficient energy
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Attempting Riposte");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Riposte"))
            {
                lastRiposteAttempt = DateTime.Now; // Set cooldown timer after successful cast
                return true;
            }
            else
            {
                lastRiposteAttempt = DateTime.Now; // Set cooldown even if the cast fails
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Riposte attempt failed, starting cooldown.");
                Console.ResetColor();
            }
        }
        if (Api.Spellbook.CanCast("Adrenaline Rush") && !Api.Spellbook.OnCooldown("Adrenaline Rush"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Adrenaline Rush");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Adrenaline Rush"))
                return true;
        }

        if (Api.Spellbook.CanCast("Kick") && !Api.Spellbook.OnCooldown("Kick")  && (target.IsCasting() || target.IsChanneling()))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Kick");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Kick"))
            {
                return true;
            }
        }
        else if (Api.Spellbook.CanCast("Kidney Shot") && !Api.Spellbook.OnCooldown("Kidney Shot")  && energy >= 25 && points >= 1 && (target.IsCasting() || target.IsChanneling()))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Kidney Shot");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Kidney Shot"))
            {
                return true;
            }
        }
        else if (Api.Spellbook.CanCast("Gouge") && !Api.Spellbook.OnCooldown("Gouge") && energy >= 45 && (target.IsCasting() || target.IsChanneling()))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Gouge");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Gouge"))
            {
                return true;
            }
        }
        if (Api.Spellbook.HasSpell("Slice and Dice") && points >= 2 && !me.Auras.Contains("Slice and Dice", true) && energy >= 25)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Casting Slice and Dice ");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Slice and Dice"))
                return true;
        }


        if (Api.Spellbook.CanCast("Evasion") && Api.UnfriendlyUnitsNearby(5, true) >= 2 && !Api.Spellbook.OnCooldown("Evasion"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Casting Evasion");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Evasion"))
                return true;
        }
        if (Api.Spellbook.HasSpell("Blade Flurry") && Api.UnfriendlyUnitsNearby(5, true) >= 2 && !Api.Spellbook.OnCooldown("Blade Flurry") && energy >= 25)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Casting Blade Flurry ");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Blade Flurry"))
                return true;
        }
        CreatureType targetCreatureType = GetCreatureType(target);

        if (Api.Spellbook.HasSpell("Rupture") && points >= 2 && !target.Auras.Contains("Rupture",true) && energy >= 25 && (targetCreatureType != CreatureType.Mechanical || targetCreatureType != CreatureType.Elemental))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Casting Rupture ");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Rupture"))
                return true;
        }
       
        if (Api.Spellbook.CanCast("Eviscerate") && points >= 3 && energy >= 35)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Casting Eviscerate ");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Eviscerate"))
                return true;
        }
        if (Api.Spellbook.CanCast("Sinister Strike") && energy >= 45)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Casting Sinister Strike ");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Sinister Strike"))
                return true;
        }
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
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;

        // Health percentage of the player
        var healthPercentage = me.HealthPercent;

        var energy = me.Energy; // Energy
        var points = me.ComboPoints;

        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);
        bool hasOffhandEnchantment = Api.Equipment.HasEnchantment(EquipmentSlot.OffHand);
        bool hasMainHandEnchantment = Api.Equipment.HasEnchantment(EquipmentSlot.MainHand);

        // Logging enchantment status
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Mainhand has enchantment: {hasMainHandEnchantment}");
        Console.WriteLine($"Offhand has enchantment: {hasOffhandEnchantment}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{energy}% Energy available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.WriteLine($"{points} points available");

        // Check for instant poisons in the inventory
        string[] instantPoisons = { "Instant Poison", "Instant Poison II", "Instant Poison III", "Instant Poison IV", "Instant Poison V", "Instant Poison VI", "Instant Poison VII" };
        bool hasInstantPoison = Api.Inventory.HasItem(instantPoisons);

        foreach (var poison in instantPoisons)
        {
            int poisonCount = Api.Inventory.ItemCount(poison);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Count of {poison}: {poisonCount}");
        }
        // Logging poison status
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Has instant poison: {hasInstantPoison}");

        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };

        foreach (string hpot in HP)
        {
            bool isOnCooldown = Api.Inventory.OnCooldown(hpot);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{hpot} is on cooldown: {isOnCooldown}");
        }
        Console.ResetColor();
        Console.ResetColor();
    }


}
