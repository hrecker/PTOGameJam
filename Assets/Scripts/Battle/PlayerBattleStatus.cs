﻿using UnityEngine;

public class PlayerBattleStatus : BattleStatus
{
    void Awake()
    {
        UpdateBattleStatusHealth();
    }

    protected override void Update()
    {
        UpdateBattleStatusHealth();
        base.Update();
    }

    public override void ApplyResult(RollResult rollResult)
    {
        ApplyHealthChange(-rollResult.PlayerDamage + rollResult.PlayerHeal);
    }

    public override void ApplyHealthChange(int diff)
    {
        PlayerStatus.Health += diff;
        UpdateBattleStatusHealth();
    }

    private void UpdateBattleStatusHealth()
    {
        currentHealth = PlayerStatus.Health;
        maxHealth = PlayerStatus.MaxHealth;
    }
}
