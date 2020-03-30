﻿using UnityEngine;

public class GhostBattleController : EnemyBattleController
{
    public int rollDebuff;
    private BattleController battleController;
    private bool debuffActive;
    private int debuffTurnsRemaining;

    private void Start()
    {
        battleController = GameObject.Find("BattleController").
            gameObject.GetComponent<BattleController>();
    }

    public override void ApplyPostDamageEffects(RollResult initial)
    {
        if (initial.PlayerDamage > 0 && !debuffActive)
        {
            Modifier mod = new RollBuffModifier(-rollDebuff, -rollDebuff);
            mod.isRollBounded = true;
            mod.numRollsRemaining = 2;
            battleController.AddRollBoundedMod(mod, 1, "-2 Roll: 2 turns", null);
            debuffTurnsRemaining = 2;
            debuffActive = true;
        }
        else if (debuffActive)
        {
            debuffTurnsRemaining--;
            if (debuffTurnsRemaining <= 0)
            {
                debuffActive = false;
            }
        }
    }
}
