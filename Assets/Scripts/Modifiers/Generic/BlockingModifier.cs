﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockingModifier : Modifier, IRollResultModifier
{
    public RollResult apply(RollResult initial)
    {
        //TODO may cause issues with health gain related stuff
        initial.PlayerDamage = 0;
        return initial;
    }
}