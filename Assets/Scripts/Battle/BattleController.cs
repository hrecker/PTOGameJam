﻿using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleController : MonoBehaviour
{
    public float rollInterval = 1.0f;
    private float timer = 0.0f;

    public Text playerRollUI;
    public Text enemyRollUI;
    public Text previousPlayerRoll1UI;
    public Text previousEnemyRoll1UI;
    public Text previousPlayerRoll2UI;
    public Text previousEnemyRoll2UI;

    public RollGenerator playerRollGenerator;
    public RollGenerator enemyRollGenerator;
    public RollResultGenerator playerRollResultGenerator;
    public RollResultGenerator enemyRollResultGenerator;
    public BattleStatus playerBattleStatus;
    public BattleStatus enemyBattleStatus;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= rollInterval)
        {
            Roll();
            timer = timer - rollInterval;
        }
    }

    private void Roll()
    {
        if (playerRollGenerator == null || enemyRollGenerator == null ||
            playerRollResultGenerator == null || enemyRollResultGenerator == null ||
            playerBattleStatus == null || enemyBattleStatus == null)
        {
            Debug.LogWarning("Skipping roll - one or more rollgenerators, " +
                "rollresultgenerators, or battlestatuses are null");
            return;
        }

        // Generate roll numeric values
        int playerInitial = playerRollGenerator.generateInitialRoll();
        int enemyInitial = enemyRollGenerator.generateInitialRoll();
        Tuple<int, int> rollValues = new Tuple<int, int>(playerInitial, enemyInitial);
        // Apply enemy mods first, then player mods to get final roll values
        rollValues = enemyRollGenerator.applyPostRollModifiers(rollValues);
        rollValues = playerRollGenerator.applyPostRollModifiers(rollValues);

        // Generate roll results
        int playerDamage = Math.Max(0, rollValues.Item2 - rollValues.Item1);
        int enemyDamage = Math.Max(0, rollValues.Item1 - rollValues.Item2);
        RollResult rollResult = new RollResult 
        { PlayerDamage = playerDamage, EnemyDamage = enemyDamage };
        // Again apply enemy result mods forst, then player
        rollResult = enemyRollResultGenerator.applyModifiers(rollResult);
        rollResult = playerRollResultGenerator.applyModifiers(rollResult);

        // Apply roll results
        playerBattleStatus.applyResult(rollResult);
        enemyBattleStatus.applyResult(rollResult);

        // Disable when the battle is over
        if (playerBattleStatus.currentHealth <= 0 || enemyBattleStatus.currentHealth <= 0)
        {
            this.enabled = false;
        }

        UpdateRollUI(rollValues.Item1, rollValues.Item2, this.enabled);
    }

    private void UpdateRollUI(int playerRoll, int enemyRoll, bool fade)
    {
        // Update text values
        previousEnemyRoll2UI.text = previousEnemyRoll1UI.text;
        previousPlayerRoll2UI.text = previousPlayerRoll1UI.text;

        previousPlayerRoll1UI.text = playerRollUI.text;
        previousEnemyRoll1UI.text = enemyRollUI.text;

        // Reset the alpha for the texts
        playerRollUI.CrossFadeAlpha(1, 0, false);
        enemyRollUI.CrossFadeAlpha(1, 0, false);
        playerRollUI.text = playerRoll.ToString();
        enemyRollUI.text = enemyRoll.ToString();

        if (fade)
        {
            // Fade out over the roll interval
            playerRollUI.CrossFadeAlpha(0, rollInterval, false);
            enemyRollUI.CrossFadeAlpha(0, rollInterval, false);
        }
    }
}
