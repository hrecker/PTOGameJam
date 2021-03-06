﻿using System;
using Modifiers;
using Modifiers.Tech;
using Modifiers.StatusEffect;
using Modifiers.Generic;
using Battle;
using System.Collections.Generic;

namespace Data
{
    // Player battle techniques
    [Serializable]
    public class Tech
    {
        public string name;
        public string displayName;
        public string description;
        public string playerStatusMessage;
        public string enemyStatusMessage;
        public int cooldownRolls;
        public int numRollsInEffect;
        public ModType modType;
        public ModEffect modEffect;
        // This bool can be used to schedule this tech to 
        // activate it's cooldown after the roll completes
        public bool scheduledCooldownActivate;

        public List<Modifier> CreateTechModifiers()
        {
            List<Modifier> result = new List<Modifier>();
            switch (modType)
            {
                case ModType.HEAVYSWING:
                    result.Add(new HeavySwingModifier());
                    break;
                case ModType.RAGE:
                    result.Add(new LowHealthBuffModifier(modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange));
                    break;
                case ModType.BULWARK:
                    result.Add(new RollBuffModifier(modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange));
                    result.Add(new RollDamagePreventionModifier(false, true));
                    break;
                case ModType.SIDESWIPE:
                    result.Add(new InflictStatusOnHitModifier(StatusEffect.BREAK,
                        false, "Sideswipe!", 2));
                    break;
                case ModType.TOPPLE:
                    result.Add(new ToppleModifier(modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange));
                    break;
                case ModType.INFECT:
                    result.Add(new InflictStatusOnHitModifier(StatusEffect.POISON,
                        false, "Infect!", 3));
                    break;
                case ModType.OMEGASLASH:
                    result.Add(new OmegaSlashModifier(modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange));
                    break;
                case ModType.FORTIFY:
                    Modifier fortifyMod = new RollBuffModifier(modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange,
                        "Fortify!");
                    fortifyMod.SetBattleEffect(RollBoundedBattleEffect.BUFF);
                    result.Add(fortifyMod);
                    Modifier noDamageModFortify = new RollDamagePreventionModifier(false, true);
                    noDamageModFortify.numRollsRemaining = 1;
                    result.Add(noDamageModFortify);
                    break;
                case ModType.BANDAGE:
                    result.Add(new RollHealthChangeModifier(modEffect.playerHealthChange,
                        "Bandage!"));
                    result.Add(new RollDamagePreventionModifier(false, true));
                    break;
                case ModType.WILDCHARGE:
                    Modifier buffMod = new RollBuffModifier(0, modEffect.playerMaxRollChange,
                        "Wild Charge!");
                    buffMod.SetBattleEffect(RollBoundedBattleEffect.BUFF);
                    Modifier debuffMod = new RollBuffModifier(modEffect.playerMinRollChange, 0);
                    debuffMod.SetBattleEffect(RollBoundedBattleEffect.DEBUFF);
                    result.Add(buffMod);
                    result.Add(debuffMod);
                    break;
                case ModType.PRAYER:
                    Modifier luckMod = new LuckBuffModifier(3, "Prayer!");
                    luckMod.SetBattleEffect(RollBoundedBattleEffect.LUCKBUFF);
                    result.Add(luckMod);
                    Modifier noDamageModPrayer = new RollDamagePreventionModifier(false, true);
                    noDamageModPrayer.numRollsRemaining = 1;
                    result.Add(noDamageModPrayer);
                    break;
                case ModType.BIDE:
                    Modifier initialDebuff = new RollBuffModifier(modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange);
                    initialDebuff.SetBattleEffect(RollBoundedBattleEffect.DEBUFF);
                    initialDebuff.numRollsRemaining = numRollsInEffect - 1;
                    result.Add(initialDebuff);
                    result.Add(new BideModifier());
                    break;
                case ModType.CRIT:
                    result.Add(new CritModifier());
                    break;
                case ModType.WILDGUESS:
                    result.Add(new WildGuessModifier(modEffect.playerMaxRollChange, 3));
                    Modifier noDamageModWG = new RollDamagePreventionModifier(false, true);
                    noDamageModWG.numRollsRemaining = 1;
                    result.Add(noDamageModWG);
                    break;
                case ModType.WILDCURSE:
                    result.Add(new WildCurseModifier(modEffect.playerMinRollChange, 3));
                    Modifier noDamageModWC = new RollDamagePreventionModifier(false, true);
                    noDamageModWC.numRollsRemaining = 1;
                    result.Add(noDamageModWC);
                    break;
                case ModType.OCCULTHEALING:
                    result.Add(new OccultHealingModifier(modEffect.playerHealthChange));
                    result.Add(new RollDamagePreventionModifier(false, true));
                    break;
                case ModType.FINISHINGBLOW:
                    result.Add(new StatusPunishingRollBuffModifier(
                        modEffect.playerMinRollChange,
                        modEffect.playerMaxRollChange, "Finishing Blow!"));
                    break;
                case ModType.ZERO:
                    result.Add(new ZeroModifier());
                    break;
                case ModType.WARMUP:
                    result.Add(new WarmupModifier());
                    result.Add(new RollDamagePreventionModifier(false, true));
                    break;
                case ModType.RISKYKICK:
                    result.Add(new RiskyKickModifier(modEffect.playerMaxRollChange));
                    break;
            }
            foreach (Modifier mod in result)
            {
                mod.isRollBounded = true;
                if (mod.numRollsRemaining == 0)
                {
                    mod.numRollsRemaining = numRollsInEffect;
                }
                mod.triggerChance = modEffect.baseModTriggerChance;
                if (mod.priority == 0)
                {
                    mod.priority = modEffect.modPriority;
                }
                mod.actor = modEffect.actor;
            }
            return result;
        }

        private int currentCooldown;

        public int GetCurrentCooldown()
        {
            return currentCooldown;
        }

        public int GetBaseCooldown()
        {
            return cooldownRolls;
        }

        // isSelectedTech represents if this was the actual tech used this roll
        // it can be false in situations where something else has scheduled the cooldown
        // activation (such as the OmegaSlash tech)
        public void ActivateCooldown(bool isSelectedTech)
        {
            // Apply tech cooldown mods
            int cooldown = cooldownRolls;
            foreach (ITechModifier mod in PlayerStatus.Status.Mods.GetTechModifiers())
            {
                cooldown = mod.ApplyTechCooldownModifier(this, isSelectedTech, cooldown);
            }
            // Cooldown can't be reduced below 1
            cooldown = Math.Max(cooldown, 1);
            currentCooldown = cooldown;
        }

        public void DecrementCooldown()
        {
            if (currentCooldown > 0)
            {
                currentCooldown--;
            }
        }

        // Updates cooldown post-roll. In almost every case this just decrements.
        public void UpdateCooldownPostRoll()
        {
            if (scheduledCooldownActivate)
            {
                ActivateCooldown(false);
                scheduledCooldownActivate = false;
            }
            else
            {
                DecrementCooldown();
            }
        }

        public void ResetCooldown()
        {
            currentCooldown = 0;
        }

        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(displayName) ? name : displayName;
        }
    }
}
