using System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;
using wShadow.WowBots;
using wShadow.WowBots.PartyInfo;
using System.Linq;


public class EraHunter : Rotation
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

    private int debugInterval = 5; // Set the debug interval in seconds
    private DateTime lastDebugTime = DateTime.MinValue;
    private DateTime lastCallPetTime = DateTime.MinValue;
    private TimeSpan callPetCooldown = TimeSpan.FromSeconds(10);
    private DateTime lastFeedTime = DateTime.MinValue;



    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.

        lastDebugTime = DateTime.Now;
        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 50;
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
        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;
        var pet = me.Pet();
        var PetHealth = 0.0f;
        if (IsValid(pet))
        {
            PetHealth = pet.HealthPercent;
        }

        ShadowApi shadowApi = new ShadowApi();

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }

        // Power percentages for different resources


        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsChanneling() || me.IsLooting() || me.IsFlying() || me.Auras.Contains("Drink") || me.Auras.Contains("Food") || me.IsMounted()) return false;

        string[] Arrows = { "Thorium Headed Arrow", "Jagged Arrow", "Razor Arrow", "Sharp Arrow", "Rough Arrow", "Doomshot", "Ice Threaded Arrow", "Explosive Arrow" };
        string[] Bullets = { "Thorium Shells", "Ice Threaded Bullet", "Rockshard Pellets", "Mithril Gyro-Shot", "Accurate Slugs", "Hi-Impact Mithril Slugs", "Exploding Shot", "Crafted Solid Shot", "Solid Shot", "Crafted Heavy Shot", "Heavy Shot", "Crafted Light Shot" };


        if (!IsValid(pet) && null == pet && (DateTime.Now - lastCallPetTime) >= callPetCooldown && Api.Spellbook.CanCast("Call Pet"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Call Pet.");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Call Pet"))
            {
                lastCallPetTime = DateTime.Now; // Update the lastCallPetTime after successful casting
                return true;
            }
        }
        if (null == pet && Api.Spellbook.CanCast("Revive Pet"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ressing Pet");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Revive Pet"))
            {
                return true;
            }
        }
        if (IsValid(pet) && (DateTime.Now - lastFeedTime).TotalMinutes >= 10 && Api.HasMacro("Feed"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Feeding pet.");
            Console.ResetColor();

            if (Api.UseMacro("Feed"))
            {
                lastFeedTime = DateTime.Now; // Update lastFeedTime

                // Log the estimated time until the next feeding attempt
                var nextFeedTime = lastFeedTime.AddMinutes(10);
                var timeUntilNextFeed = nextFeedTime - DateTime.Now;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Next feed pet in: {timeUntilNextFeed.TotalMinutes} minutes.");
                Console.ResetColor();

                return true;
            }
        }
        if (Api.Spellbook.CanCast("Aspect of the Cheetah") && !me.Auras.Contains("Aspect of the Cheetah", false) && !me.IsMounted() && !me.Auras.Contains(415423, false))

        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Aspect of the Cheetah");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Aspect of the Cheetah"))

                return true;

        }
        if (IsValid(pet) && PetHealth <= 30 && Api.Spellbook.CanCast("Mend Pet") && !pet.Auras.Contains("Mend Pet") && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Pet health is low healing him");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mend Pet"))

                return true;
            // Add logic here for actions when pet's health is low, e.g., healing spells
        }


        if (Api.Spellbook.CanCast("Aspect of the Hawk") && !me.Auras.Contains("Aspect of the Hawk", false) && !me.Auras.Contains("Aspect of the Cheetah", false) && !me.Auras.Contains(415423, false))

        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Aspect of the Hawk");
            Console.ResetColor();

            if (Api.Spellbook.Cast("Aspect of the Hawk"))

                return true;
        }

        var reaction = me.GetReaction(target);
        if (target.IsValid())
        {

            if (!target.IsDead() && (reaction != UnitReaction.Friendly && reaction != UnitReaction.Honored && reaction != UnitReaction.Revered && reaction != UnitReaction.Exalted) && mana > 20 && !IsNPC(target) && Api.Spellbook.CanCast("Hunter's Mark") && !target.Auras.Contains("Hunter's Mark") && healthPercentage > 50 && mana > 20 && PetHealth > 50)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Mark");
                Console.ResetColor();

                if (Api.UseMacro("Mark"))

                    // Update the lastMarkTime after successful casting
                    return true;



            }
        }
        else
        if (Api.Spellbook.CanCast("Serpent Sting") && !me.IsShooting() && !target.IsDead())
        {

            //if (distance <= 30)
            
                if (Api.Spellbook.Cast("Serpent Sting") )
                    return true;
            
        }
        else
         if (Api.Spellbook.CanCast("Auto Shot") && !me.IsShooting() && !target.IsDead())
        {

            //if (distance <= 30)

            if (Api.Spellbook.Cast("Auto Shot"))
                return true;

        }
        return base.PassivePulse();

    }

    public override bool CombatPulse()
    {
        // Variables for player and target instances
        var me = Api.Player;
        var target = Api.Target;
        var targetDistance = target.Position.Distance2D(me.Position);

        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;
        var mana = me.ManaPercent;
        var meTarget = me.Target;

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }

        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        var unfriendlyUnits = Api.UnitsTargetingMe(5, true); // Fetch units within 5 yards using 3D distance


        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };

        if (healthPercentage <= 70 && (!Api.Inventory.OnCooldown(MP) && !Api.Inventory.OnCooldown(HP)))
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
        if (IsValid(pet) && PetHealth <= 30 && Api.Spellbook.CanCast("Mend Pet") && !pet.Auras.Contains("Mend Pet") && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Pet health is low healing him");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mend Pet"))

                return true;
            // Add logic here for actions when pet's health is low, e.g., healing spells
        }

        if (mana <= 50 && (!Api.Inventory.OnCooldown(MP) || !Api.Inventory.OnCooldown(HP)))
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
      

        if (Api.Spellbook.CanCast("Hunter's Mark") && !target.Auras.Contains("Hunter's Mark"))
        {

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Mark");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Hunter's Mark"))
            {
                return true;

            }

        }

        string[] Arrows = { "Thorium Headed Arrow", "Jagged Arrow", "Razor Arrow", "Sharp Arrow", "Rough Arrow", "Doomshot", "Ice Threaded Arrow", "Explosive Arrow" };
        string[] Bullets = { "Thorium Shells", "Ice Threaded Bullet", "Rockshard Pellets", "Mithril Gyro-Shot", "Accurate Slugs", "Hi-Impact Mithril Slugs", "Exploding Shot", "Crafted Solid Shot", "Solid Shot", "Crafted Heavy Shot", "Heavy Shot", "Crafted Light Shot" };

        bool hasArrows = true;
        bool hasBullets = true;



        // Assuming targetDistance is declared and initialized
        if (targetDistance >= 8 && (hasArrows || hasBullets))

        {
            if (Api.Spellbook.CanCast("Rapid Fire") && Api.UnfriendlyUnitsNearby(10, true) >= 2 && !Api.Spellbook.OnCooldown("Rapid Fire"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Rapid Fire");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Rapid Fire"))

                    return true;

            }

            if (Api.Spellbook.CanCast("Aspect of the Hawk") && !me.Auras.Contains("Aspect of the Hawk", false) && mana > 70)

            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Aspect of the Hawk");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Aspect of the Hawk"))

                    return true;
            }
            else
            if (Api.Spellbook.CanCast("Aspect of the Viper") && !me.Auras.Contains("Aspect of the Viper", false) && mana < 30)

            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Aspect of the Viper");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Aspect of the Viper"))

                    return true;
            }

            if (Api.Spellbook.CanCast("Serpent Sting") && mana > 15 && !target.Auras.Contains("Serpent Sting") && targethealth > 35)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Serpent Sting");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Serpent Sting"))
                    return true;
            }


            if (Api.Spellbook.CanCast("Aimed Shot") && mana > 30 && targethealth > 20)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Aimed Shot");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Aimed Shot"))
                    return true;
            }
            if (Api.Spellbook.CanCast("Multi Shot") && targetDistance >= 8) //&& unfriendlyUnits >= 2)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Multi Shot");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Multi Shot"))
                    return true;
            }
            if (Api.Spellbook.CanCast("Arcane Shot") && mana > 30)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Arcane Shot");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Arcane Shot"))
                    return true;
            }
            if (Api.Spellbook.CanCast("Auto Shot") && !me.IsShooting() )
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Auto Shot");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Auto Shot"))
                    return true;
            }
        }

        if (!target.IsDead() && targetDistance <= 10)
        {
            if (Api.Spellbook.CanCast("Aspect of the Monkey") && !me.Auras.Contains("Aspect of the Monkey", false) && mana > 30)

            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Aspect of the Monkey");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Aspect of the Monkey"))

                    return true;
            }
            if (Api.Spellbook.CanCast("Deterrence") && Api.UnfriendlyUnitsNearby(10, true) >= 2 && !Api.Spellbook.OnCooldown("Deterrence"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Deterrence");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Deterrence"))

                    return true;

            }

            if (Api.Spellbook.CanCast("Wing Clip") && mana > 40 && !target.Auras.Contains("Wing Clip"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Wing Clip");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Wing Clip"))
                    return true;
            }

            if (Api.Spellbook.CanCast("Raptor Strike") && mana > 15)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Casting Raptor Strike");
                Console.ResetColor();

                if (Api.Spellbook.Cast("Raptor Strike"))
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

        // Health percentage of the player
        var healthPercentage = me.HealthPercent;

        // Power percentages for different resources
        var mana = me.ManaPercent;

        var targetDistance = target.Position.Distance2D(me.Position);

        string[] Arrows = { "Thorium Headed Arrow", "Jagged Arrow", "Razor Arrow", "Sharp Arrow", "Rough Arrow", "Doomshot", "Ice Threaded Arrow", "Explosive Arrow" };
        string[] Bullets = { "Thorium Shells", "Ice Threaded Bullet", "Rockshard Pellets", "Mithril Gyro-Shot", "Accurate Slugs", "Hi-Impact Mithril Slugs", "Exploding Shot", "Crafted Solid Shot", "Solid Shot", "Crafted Heavy Shot", "Heavy Shot", "Crafted Light Shot" };

        bool hasArrows = true;
        bool hasBullets = true;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana}% Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();
      

        foreach (var arrow in Arrows)
        {
            int quantity = Api.Inventory.ItemCount(arrow);
            if (quantity > 0)
            {
                hasArrows = true;
                Console.WriteLine($"Has {quantity} {arrow}");
            }
        }

        foreach (var bullet in Bullets)
        {
            int quantity = Api.Inventory.ItemCount(bullet);
            if (quantity > 0)
            {
                hasBullets = true;
                Console.WriteLine($"Has {quantity} {bullet}");
            }
        }

        if (hasArrows || hasBullets)
        {
            Console.WriteLine("Has ammo");
        }
        else
        {
            Console.WriteLine("No ammo");
        }



    }
}



