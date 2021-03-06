﻿using Battle;
using Modifiers.Generic;
using System;

namespace Modifiers.StatusEffect
{
    // Raises max roll by 1 and reduces min roll by 1, and causes self recoil equal 
    // to half of the roll difference when this actor rolls higher
    public class EnragedModifier : RollBuffModifier, IRollResultModifier, IOneTimeEffectModifier
    {
        private float recoilRatio = 0.5f;

        public EnragedModifier(BattleActor actor) : base(-1, 1)
        {
            statusEffect = Battle.StatusEffect.ENRAGED;
            battleEffect = RollBoundedBattleEffect.ENRAGED;
            // Higher intensity increases recoil
            int intensity = GetStatusEffectIntensity(actor);
            recoilRatio += (0.5f * intensity);
        }

        public void ApplyOneTimeEffectMod()
        {
            actor.Status().ActiveEffects.Add(Battle.StatusEffect.ENRAGED);
        }

        public bool CanApply()
        {
            return true;
        }

        protected override void OnDeregister()
        {
            actor.Status().ActiveEffects.Remove(Battle.StatusEffect.ENRAGED);
        }

        public RollResult ApplyRollResultMod(RollResult initial)
        {
            if (initial.GetRollValue(actor) > initial.GetRollValue(actor.Opponent()))
            {
                // Half of roll difference is dealt back as recoil
                int recoilDamage = (int)Math.Ceiling(
                    (initial.GetRollValue(actor) - initial.GetRollValue(actor.Opponent())) * recoilRatio);
                initial.AddNonRollDamage(actor, recoilDamage);
            }
            return initial;
        }
    }
}
