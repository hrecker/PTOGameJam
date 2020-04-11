﻿public class StalwartModifier : Modifier, IRollResultModifier
{
    public RollResult ApplyRollResultMod(RollResult initial)
    {
        if (-initial.GetTotalPlayerHealthChange() >= PlayerStatus.Health && RollTrigger())
        {
            int damageReduction = (-initial.GetTotalPlayerHealthChange()) - PlayerStatus.Health + 1;
            initial.PlayerDamage -= damageReduction;
            BattleController.AddPlayerModMessage("Stalwart!");
        }
        return initial;
    }
}
