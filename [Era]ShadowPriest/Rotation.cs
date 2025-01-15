tusing System;
using System.Threading;
using wShadow.Templates;
using System.Collections.Generic;
using wShadow.Warcraft.Classes;
using wShadow.Warcraft.Defines;
using wShadow.Warcraft.Managers;

public class EraShadowPriest : Rotation
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
    private bool HasItem(object item) => Api.Inventory.HasItem(item);
    private Dictionary<string, DateTime> potionCooldowns = new Dictionary<string, DateTime>();

    private DateTime lastDebugTime = DateTime.MinValue;
    private int debugInterval = 5; // Set the debug interval in seconds

    private bool HasEnchantment(EquipmentSlot slot, string enchantmentName)
    {
        return Api.Equipment.HasEnchantment(slot, enchantmentName);
    }




    public override void Initialize()
    {
        // Can set min/max levels required for this rotation.


        LogPlayerStats();
        // Use this method to set your tick speeds.
        // The simplest calculation for optimal ticks (to avoid key spam and false attempts)

        // Assuming wShadow is an instance of some class containing UnitRatings property
        SlowTick = 750;
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
        var mana = me.ManaPercent;
        var target = Api.Target;
        var targethealth = target.HealthPercent;

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        // Health percentage of the player
        var healthPercentage = me.HealthPercent;

        // Power percentages for different resources

        // Target distance from the player

        if (me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food"))
        {
            return false;
        }

        if (Api.Spellbook.CanCast("Renew") && !me.Auras.Contains("Renew",true) && healthPercentage < 80)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Renew");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Renew"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Power Word: Fortitude") && !me.Auras.Contains("Power Word: Fortitude",true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Power Word: Fortitude");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Power Word: Fortitude"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Shadow Protection") && !me.Auras.Contains("Shadow Protection",true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Shadow Protection");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Power Shadow Protection"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Inner Fire") && !me.Auras.Contains("Inner Fire",true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Inner Fire");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Inner Fire"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Power Word: Shield") && !me.Auras.Contains("Power Word: Shield",true) && me.InCombat())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Power Word: Shield");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Power Word: Shield"))
            {
                return true;
            }
        }

        if (!me.Auras.Contains("Shadowform", false) && Api.Spellbook.CanCast("Shadowform",true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Shadowform");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Shadowform"))
            {
                return true;
            }
        }
        var reaction = me.GetReaction(target);
        if (target.IsValid())
        {
            if (!target.IsDead())
            {
                if (reaction != UnitReaction.Friendly && reaction != UnitReaction.Honored && reaction != UnitReaction.Revered && reaction != UnitReaction.Exalted)
                {
                    if (mana >= 5)
                    {
                        if (!IsNPC(target))
                        {
                            if (Api.Spellbook.CanCast("Smite"))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Casting Smite");
                                Console.ResetColor();
                                if (Api.Spellbook.Cast("Smite"))
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Smite is not ready to be cast.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Target is an NPC.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Mana is not above 20%.");
                    }
                }
                else
                {
                    Console.WriteLine("Target is friendly, honored, revered, or exalted.");
                }
            }
            else
            {
                Console.WriteLine("Target is dead.");
            }
        }

        return base.PassivePulse();
    }


    public override bool CombatPulse()
    {
        var me = Api.Player;
        var target = Api.Target;
        var mana = me.ManaPercent;

        if ((DateTime.Now - lastDebugTime).TotalSeconds >= debugInterval)
        {
            LogPlayerStats();
            lastDebugTime = DateTime.Now; // Update lastDebugTime
        }
        // Health percentage of the player
        var healthPercentage = me.HealthPercent;
        var targethealth = target.HealthPercent;
        if (!me.IsValid() || !target.IsValid() || me.IsDead() || me.IsGhost() || me.IsCasting() || me.IsMoving() || me.IsChanneling() || me.IsMounted() || me.Auras.Contains("Drink") || me.Auras.Contains("Food")) return false;

        string[] HP = { "Major Healing Potion", "Superior Healing Potion", "Greater Healing Potion", "Healing Potion", "Lesser Healing Potion", "Minor Healing Potion" };
        string[] MP = { "Major Mana Potion", "Superior Mana Potion", "Greater Mana Potion", "Mana Potion", "Lesser Mana Potion", "Minor Mana Potion" };


        if (UsePotions())
        {
            return true; // Exit early if a potion was used
        }
        if (Api.Spellbook.CanCast("Renew") && !me.Auras.Contains("Renew", true) && healthPercentage < 80 && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Renew");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Renew"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Lesser Heal")  && healthPercentage < 60 && me.Level <20 && mana > 20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Lesser Heal");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Lesser Heal"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Flash Heal") && healthPercentage < 60 && me.Level >= 20 && mana >20)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Flash Heal");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Flash Heal"))
            {
                return true;
            }
        }
        // Target distance from the player
        var targetDistance = target.Position.Distance2D(me.Position);
        if (Api.Spellbook.CanCast("Power Word: Shield") && !me.Auras.Contains("Power Word: Shield",true) && mana > 15 && !me.Auras.Contains("Weakened Soul",true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Power Word: Shield");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Power Word: Shield"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Silence") && !Api.Spellbook.OnCooldown("Silence") && (target.IsCasting() || target.IsChanneling()))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Silence");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Silence"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Shadow Word: Pain") && !target.Auras.Contains("Shadow Word: Pain",true) && targethealth >= 30 && mana > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Shadow Word: Pain");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Shadow Word: Pain"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Vampiric Embrace") && !target.Auras.Contains("Vampiric Embrace",true) && targethealth >= 30 && mana > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Vampiric Embrace");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Vampiric Embrace"))
            {
                return true;
            }
        }
        if (Api.Spellbook.CanCast("Mind Blast") && targethealth >= 30 && mana > 10 && !Api.Spellbook.OnCooldown("Mind Blast"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Mind Blast");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Mind Blast"))
            {
                return true;
            }
        }

        if (Api.Spellbook.CanCast("Smite") && mana > 70 && !me.Auras.Contains("Shadowform", true))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Casting Smite");
            Console.ResetColor();
            if (Api.Spellbook.Cast("Smite"))

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

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{mana}% Mana available");
        Console.WriteLine($"{healthPercentage}% Health available");
        Console.ResetColor();
        if (Api.HasMacro("Shoot"))
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
    }




}