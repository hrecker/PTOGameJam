﻿using System;
using UnityEngine;

public class EnemyRollGenerator : RollGenerator
{
    public override Tuple<int, int> applyPostRollModifiers(Tuple<int, int> playerEnemyRolls)
    {
        //TODO
        return playerEnemyRolls;
    }

    public override int generateInitialRoll()
    {
        return generateBasicRoll(minRoll, maxRoll);
    }
}
