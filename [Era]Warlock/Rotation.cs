using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;

public class EraWarlock : Rotation
{

    private bool HasItem(object item)
        => Api.Inventory.HasItem(item);
    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;
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
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        var mana = me.ManaPercent;
        var pet = me.Pet();
        var PetHealth = 0.0f;
        if (IsValid(pet))
        {
            PetHealth = pet.HealthPercent;
        }
        var TargetHealth = 0.0f;
        if (IsValid(target))
        {
            TargetHealth = target.HealthPercent;
        }

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        // Health percentage of the player
        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;
        // Target distance from the player
        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        if (me.IsValid())
        {
            if (Api.Spellbook.CanCast("Demon Armor") && !me.Auras.Contains("Demon Armor", false))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Demon Armor");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Demon Armor"))
                    return true;
            }
            if (Api.Spellbook.CanCast("Demon Skin") && !me.Auras.Contains("Demon Armor", false) && !me.Auras.Contains("Demon Skin", false))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Demon Skin");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Demon Skin"))
                    return true;
            }

            if ((!IsValid(pet)) && Api.Spellbook.CanCast("Summon Voidwalker") && HasItem("Soul Shard") && mana > 25 && Api.Spellbook.HasSpell("Summon Voidwalker"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Summon Voidwalker.");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Summon Voidwalker"))
                {
                    return true;
                }
            }
            else if ((!IsValid(pet)) && Api.Spellbook.CanCast("Summon Imp") && mana > 30)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Summon Imp.");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Summon Imp"))
                {
                    return true;
                }
            }


            if (PetHealth < 50 && healthPercentage > 50 && Api.Spellbook.CanCast("Health Funnel"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Healing Pet ");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Health Funnel"))
                    return true;
            }


            if (Api.Spellbook.CanCast("Life Tap") && healthPercentage > 80 && mana < 30)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Life Tap");
                Console.ResetColor();
                if (Api.Spellbook.Cast("Life Tap"))
                {
                    return true;
                }
            }
        }
        //string[] healthstoneTypes = { "Minor Healthstone", "Lesser Healthstone", "Healthstone", "Greater Healthstone", "Major Healthstone", "Master Healthstone", "Demonic Healthstone", "Fel Healthstone" };


        var reaction = me.GetReaction(target);

        if (target.IsValid())
        {

            if (!target.IsDead() && (reaction != UnitReaction.Friendly && reaction != UnitReaction.Honored && reaction != UnitReaction.Revered && reaction != UnitReaction.Exalted) && mana > 20 && !IsNPC(target) && healthPercentage > 50 && mana > 20 && PetHealth > 50)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Petattack");
                Console.ResetColor();

                if (Api.UseMacro("Petattack"))

                    // Update the lastMarkTime after successful casting
                    return true;



            }
        }
        else
        {
            if (target.IsValid())
            {
                if (!target.IsDead() && (reaction != UnitReaction.Friendly && reaction != UnitReaction.Honored && reaction != UnitReaction.Revered && reaction != UnitReaction.Exalted) && mana > 20 && !IsNPC(target) && healthPercentage > 50 && mana > 20 && PetHealth > 50)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Casting Shadow Bolt");
                    Console.ResetColor();

                    if (Api.Spellbook.Cast("Shadow Bolt"))
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
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        var mana = me.ManaPercent;
        var targethealth = target.HealthPercent;
        var healthPercentage = me.HealthPercent;
        var pet = me.Pet();
        var PetHealth = 0.0f;
        if (IsValid(pet))
        {
            PetHealth = pet.HealthPercent;
        }
        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };

        if (UsePotions())
        {
            return true; // Exit early if a potion was used
        }

        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;


        var meTarget = me.Target;

        if (meTarget == null || target.IsDead())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Assist Pet");
            Console.ResetColor();

            // Use the Target property to set the player's target to the pet's target
            if (Api.UseMacro("AssistPet"))
            {
                // Successfully assisted the pet, continue rotation
                // Don't return true here, continue with the rest of the combat logic
                // without triggering a premature exit
            }
        }

        if (Api.Spellbook.CanCast("Drain Soul") && Api.Inventory.ItemCount("Soul Shard") <= 2 && targethealth <= 30 && mana > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Drain Soul");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Drain Soul"))
            {
                return true;
            }
        }




        if (Api.Spellbook.CanCast("Drain Life") && healthPercentage <= 50 && mana >= 5)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Drain Life");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Drain Life"))
                return true;
        }
        if (Api.Spellbook.CanCast("Immolate") && !target.Auras.Contains("Immolate") && mana >= 10 && targethealth >= 30)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Immolate");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Immolate"))
                return true;
        }
        if (Api.Spellbook.CanCast("Curse of Agony") && !target.Auras.Contains("Curse of Agony") && mana >= 10 && targethealth >= 30)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Curse of Agony");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Curse of Agony"))
                return true;
        }

        if (Api.Spellbook.CanCast("Corruption") && !target.Auras.Contains("Corruption") && mana >= 10 && targethealth >= 30)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Corruption");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Corruption"))
                return true;
        }

        if (Api.Spellbook.CanCast("Shadow Bolt") && mana >= 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Shadow Bolt");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Shadow Bolt"))
                return true;
        }

        if (Api.Equipment.HasItem(EquipmentSlot.Extra) && Api.Spellbook.CanCast("Shoot") && !me.IsShooting())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ranged weapon is equipped. Attempting to cast Shoot.");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Shoot"))
            {
                return true;
            }
        }
        else if (Api.Spellbook.CanCast("Attack") && (!me.IsAutoAttacking() && !me.IsShooting()))
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
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        var mana = me.Mana;
        var targethealth = target.HealthPercent;

        // Health percentage of the player
        var healthPercentage = me.HealthPercent;

        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);
        var pet = me.Pet();
        var PetHealth = 0.0f;
        if (IsValid(pet))
        {
            PetHealth = pet.HealthPercent;
        }


        if (IsValid(pet))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Pet is alive.");
            Console.ResetColor();
            // Additional actions for when the pet is dead
        }
        else
      if ((!IsValid(pet) || PetHealth <= 0))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Pet is dead.");
            Console.ResetColor();
            // Additional actions for when the pet's health is low
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana}% Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();



        // Access the Bank property and use its methods
        if (Api.Equipment.HasItem(EquipmentSlot.Extra) && Api.HasMacro("Shoot"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Wand macro 'Shoot' is present.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("INGAME ..... Create Macro ");
            Console.WriteLine("Macro name : Shoot");
            Console.WriteLine("Macro code : /cast !Shoot");

            Console.WriteLine("Save macro, exit options and when ingame RELOAD UI");
            Console.ResetColor();
        }

        if (HasItem("Lesser Healthstone"))
        {
            Console.WriteLine("Lesser Healthstone found in inventory");
            Console.ResetColor();
        }

        if (Api.Spellbook.HasSpell(6202))
        {
            Console.WriteLine("Can Cast Create Healthstone (Lesser)");
            Console.ResetColor();
        }
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