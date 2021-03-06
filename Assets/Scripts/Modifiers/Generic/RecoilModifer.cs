﻿using Battle;

namespace Modifiers.Generic
{
    // Chance to deal some recoil damage when hit by roll
    public class RecoilModifer : Modifier, IRollResultModifier
    {
        // Message to display when triggered, or null if no message should be displayed
        private string modMessage;

        public RecoilModifer(string modMessage)
        {
            priority = 5;
            this.modMessage = modMessage;
        }

        public RecoilModifer() : this(null) { }

        public RollResult ApplyRollResultMod(RollResult initial)
        {
            if (initial.GetRollDamage(actor) > 0 && RollTrigger())
            {
                initial.AddNonRollDamage(actor.Opponent(), 1);
                if (modMessage != null)
                {
                    BattleController.AddModMessage(actor, modMessage);
                }
            }
            return initial;
        }
    }
}
