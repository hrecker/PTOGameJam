﻿using System;

namespace Modifiers
{
    // Modifiers affecting the values of the roll after it has been rolled
    public interface IRollValueModifier
    {
        Tuple<int, int> ApplyRollValueMod(int playerRoll, int enemyRoll);
    }
}
