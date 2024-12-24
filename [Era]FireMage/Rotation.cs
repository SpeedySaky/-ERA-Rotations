using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;

public class EraFireMage : Rotation
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
    private DateTime lastDebugTime = DateTime.MinValue;
    private DateTime lastPyro = DateTime.MinValue;

    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.

        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 1550;
        FastTick = 750;

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
        var pet = me.Pet();

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        // Health percentage of the player
        var healthPercentage = me.HealthPercent;
        var mana = me.ManaPercent;
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
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
            //string[] GemTypes = { "Mana Agate", "Mana Sapphire", "Mana Emerald", "Mana Ruby", "Mana Citrine", "Mana Jade" };
            //  bool needsgem = true;

            //  foreach (string gemType in GemTypes)
            //  {
            //if (shadowApi.Inventory.HasItem(gemType))
            //  {
            // needsgem = false;
            //   break;
            // }
            // }
            //   if (Api.Spellbook.CanCast("Conjure Mana Agate") && needsgem)
            //  {
            // if (Api.Spellbook.Cast("Conjure Mana Agate"))
            //   {
            // Console.WriteLine("Conjure Mana Gem.");
            // Add further actions if needed after conjuring water
            //}
            //}
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
            bool needsWater = false; // Initialize as false
            bool needsFood = false; // Initialize as false

            foreach (string waterType in waterTypes)
            {
                if (Api.Inventory.HasItem(waterType))
                {
                    needsWater = true; // Set to true if the character has water
                    break;
                }
            }

            foreach (string foodType in foodTypes)
            {
                if (Api.Inventory.HasItem(foodType))
                {
                    needsFood = true; // Set to true if the character has food
                    break;
                }
            }

            // Now needsWater and needsFood variables will indicate if the character needs water or food
            if (!needsWater) // If the character does not have water
            {
                if (Api.Spellbook.CanCast("Conjure Water"))
                {
                    if (Api.Spellbook.Cast("Conjure Water"))
                    {
                        Console.WriteLine("Conjured water.");
                        return true;

                        // Add further actions if needed after conjuring water
                    }
                }
            }

            if (!needsFood) // If the character does not have food
            {
                if (Api.Spellbook.CanCast("Conjure Food"))
                {
                    if (Api.Spellbook.Cast("Conjure Food"))
                    {
                        Console.WriteLine("Conjured Food.");
                        return true;

                        // Add further actions if needed after conjuring food
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

                // Try casting Frostbolt
                if (Api.Spellbook.CanCast("Frostbolt"))
                {
                    Api.Spellbook.Cast("Frostbolt");
                    Console.WriteLine("Casting Frostbolt");
                    return true;
                }
                else
                {
                    // If Frostbolt fails, try casting Fireball
                    if (Api.Spellbook.CanCast("Fireball"))
                    {
                        Console.WriteLine("Casting Fireball");
                        Api.Spellbook.Cast("Fireball");
                        return true;
                    }
                }
            }
        }


        // If none of the conditions are met or casting both spells fail

        return base.PassivePulse();

    }
    public override bool CombatPulse()
    {
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;

        // Power percentages for different resources
        var mana = me.ManaPercent;
        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };
        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling()) return false;
        if (me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;
        var hasaura = me.Auras.Contains("Curse of Stalvan") || me.Auras.Contains("Curse of Blood");

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



        if (targetDistance >= 6 && targetDistance <= 12)
        {
            if (Api.Spellbook.CanCast("Frost Nova")  && !Api.Spellbook.OnCooldown("Frost Nova"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Frost Nova");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Frost Nova"))
                {
                    return true;
                }
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
        if (Api.Spellbook.CanCast("Counterspell") && !Api.Spellbook.OnCooldown("Counterspell") && (target.IsCasting() || target.IsChanneling()))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Counterspell");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Counterspell"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Evocation") && !Api.Spellbook.OnCooldown("Evocation") && mana <= 10)
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

        if (Api.Spellbook.CanCast("Mana Shield") && healthPercentage < 50 && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Mana Shield");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mana Shield"))
            {
                return true;
            }
        }

        // Offensive spells
        if (Api.Spellbook.CanCast("Pyroblast") && !target.Auras.Contains("Pyroblast", true) && mana > 30)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Pyroblast");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Pyroblast"))

            {
                return true;
            }

        }



        if (Api.Spellbook.CanCast("Scorch") && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Scorch");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Scorch"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Fire Blast") && mana > 15 && !Api.Spellbook.OnCooldown("Fire Blast") && targetDistance < 25)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Fire Blast");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Fire Blast"))
            {
                return true;
            }
        }



        if (Api.Spellbook.CanCast("Fireball") && mana > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Fireball");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Fireball"))
            {
                return true;
            }
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
    else if (Api.Spellbook.CanCast("Attack") && !me.IsAutoAttacking() && !me.IsShooting())
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
        var hasaura = me.Auras.Contains("Curse of Stalvan");

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

        Console.ResetColor();
    }
}