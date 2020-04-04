﻿using System;

public class PlayerRollGenerator : RollGenerator
{
    private void Start()
    {
        minRoll = PlayerStatus.BaseMinRoll;
        maxRoll = PlayerStatus.BaseMaxRoll;
    }

    public override int GenerateInitialRoll()
    {
        int min = minRoll;
        int max = maxRoll;
        foreach (IRollGenerationModifier mod in PlayerStatus.Mods.GetRollGenerationModifiers())
        {
            Tuple<int, int> modified = mod.apply(min, max);
            min = modified.Item1;
            max = modified.Item2;
        }
        return GenerateBasicRoll(min, max);
    }
}
