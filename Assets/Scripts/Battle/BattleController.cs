﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Battle.Enemies;
using Data;
using Modifiers;
using Levels;

namespace Battle
{
    public class BattleController : MonoBehaviour
    {
        public float rollInterval = 1.0f;
        public float victoryDisplayTime = 2.0f;
        private float timer = 0.0f;
        private bool completed;
        private bool fastforwardActive;
        private float baseRollInterval;

        public GameObject enemy;
        public GameObject resultTextPanel;
        public TechButtonController techUI;
        public GameObject exitToMenuButton;
        public GameObject itemIconPrefab;
        public GameObject itemDropPanel;

        public Text playerRollUI;
        public Text enemyRollUI;
        public GameObject playerRollDamageUI;
        public GameObject playerRollHealUI;
        private GameObject enemyRollDamageUI;
        private GameObject enemyRollHealUI;
        private Text playerRollDamageText;
        private Text playerRollHealText;
        private Text enemyRollDamageText;
        private Text enemyRollHealText;

        public RollGenerator playerRollGenerator;
        public Transform enemyParent;
        private EnemyBattleController enemyController;
        public BattleStatus playerBattleStatus;
        private BattleStatus enemyBattleStatus;

        public RectTransform playerStatusMessagesParent;
        public RectTransform enemyStatusMessagesParent;
        public RectTransform playerModMessagesParent;
        public RectTransform enemyModMessagesParent;
        public GameObject statusMessagePrefab;
        public float statusMessageSpacing = 40.0f;

        private Dictionary<string, Modifier> techMods;
        private List<string> playerStatusMessagesToShow;
        private List<string> enemyStatusMessagesToShow;
        private static List<string> playerModMessagesToShow;
        private static List<string> enemyModMessagesToShow;
        private static int currentRoll;

        private void Awake()
        {
            baseRollInterval = rollInterval;
            // Add enemycontroller for the given enemy
            string enemyName = CurrentLevel.currentEnemyName;
            Type controllerType = Data.Cache.GetEnemy(enemyName).GetEnemyControllerType();
            enemy.AddComponent(controllerType);
            enemyController = enemy.GetComponent<EnemyBattleController>();
            enemyBattleStatus = enemy.GetComponent<EnemyBattleStatus>();
            enemyRollDamageUI = enemy.gameObject.transform.Find("RollDamage").gameObject;
            enemyRollHealUI = enemy.gameObject.transform.Find("RollHeal").gameObject;
            playerModMessagesToShow = new List<string>();
            enemyModMessagesToShow = new List<string>();
        }

        private void Start()
        {
            currentRoll = 1;
            exitToMenuButton.SetActive(false);
            itemDropPanel.SetActive(false);
            resultTextPanel.SetActive(false);
            techMods = new Dictionary<string, Modifier>();
            playerRollDamageText = playerRollDamageUI.GetComponentInChildren<Text>();
            playerRollHealText = playerRollHealUI.GetComponentInChildren<Text>();
            enemyRollDamageText = enemyRollDamageUI.GetComponentInChildren<Text>();
            enemyRollHealText = enemyRollHealUI.GetComponentInChildren<Text>();
            playerStatusMessagesToShow = new List<string>();
            enemyStatusMessagesToShow = new List<string>();
            playerRollDamageUI.SetActive(false);
            playerRollHealUI.SetActive(false);
            enemyRollDamageUI.SetActive(false);
            enemyRollHealUI.SetActive(false);
            CheckBattleComplete();
        }

        void Update()
        {
            timer += Time.deltaTime;

            if (!completed && timer >= rollInterval)
            {
                Roll();
                timer = timer - rollInterval;
            }
            else if (completed && timer >= victoryDisplayTime)
            {
                enabled = false;
                // If player wins and it's not the boss fight, return to map. Otherwise just stop here.
                if (playerBattleStatus.currentHealth > 0 && CurrentLevel.currentEnemyName != "Boss")
                {
                    SceneManager.LoadScene("MapScene");
                }
            }
        }

        public void ToggleFastforward()
        {
            float intervalFractionCompleted = timer / rollInterval;
            if (fastforwardActive)
            {
                rollInterval = baseRollInterval;
            }
            else
            {
                rollInterval = baseRollInterval / 2;
            }
            timer = intervalFractionCompleted * rollInterval;
            fastforwardActive = !fastforwardActive;
        }

        private void Roll()
        {
            if (playerRollGenerator == null || enemyController == null ||
                playerBattleStatus == null || enemyBattleStatus == null)
            {
                Debug.LogWarning("Skipping roll - one or more rollgenerators " +
                    "or battlestatuses are null");
                return;
            }

            playerModMessagesToShow.Clear();
            enemyModMessagesToShow.Clear();

            // If there are modifiers to add, add before the roll starts
            // Add mods for selected techs
            if (techUI.GetSelectedTech() != null)
            {
                Tech selected = techUI.GetSelectedTech();
                if (!techMods.ContainsKey(selected.name))
                {
                    techMods.Add(selected.name, selected.CreateTechModifier());
                }
                // Add UI messages if necessary
                playerStatusMessagesToShow.Add(selected.playerStatusMessage);
                enemyStatusMessagesToShow.Add(selected.enemyStatusMessage);
                PlayerStatus.Mods.RegisterModifier(techMods[selected.name]);
            }
            techUI.Roll();

            // Generate roll numeric values
            bool enemyModApplied = false;
            int playerInitial = playerRollGenerator.GenerateInitialRoll();
            int enemyInitial = enemyController.GenerateInitialRoll();
            Tuple<int, int> rollValues = new Tuple<int, int>(playerInitial, enemyInitial);
            // Apply enemy and player mods to get final roll values
            foreach (IRollValueModifier mod in PlayerStatus.Mods.GetRollValueModifiers())
            {
                if (!enemyModApplied)
                {
                    int priority = ((Modifier)mod).priority;
                    if (enemyController.GetRollValuePriority() <= priority)
                    {
                        rollValues = enemyController.ApplyRollValueMods(
                            rollValues.Item1, rollValues.Item2);
                        enemyModApplied = true;
                    }
                }
                rollValues = mod.ApplyRollValueMod(rollValues.Item1, rollValues.Item2);
            }
            if (!enemyModApplied)
            {
                rollValues = enemyController.ApplyRollValueMods(
                    rollValues.Item1, rollValues.Item2);
            }

            // Generate roll results
            enemyModApplied = false;
            int playerDamage = Math.Max(0, rollValues.Item2 - rollValues.Item1);
            int enemyDamage = Math.Max(0, rollValues.Item1 - rollValues.Item2);
            RollResult rollResult = new RollResult
            { PlayerDamage = playerDamage, EnemyDamage = enemyDamage };
            // Again apply enemy and player mods
            foreach (IRollResultModifier mod in PlayerStatus.Mods.GetRollResultModifiers())
            {
                if (!enemyModApplied)
                {
                    int priority = ((Modifier)mod).priority;
                    if (enemyController.GetRollResultPriority() <= priority)
                    {
                        rollResult = enemyController.ApplyRollResultMods(rollResult);
                        enemyModApplied = true;
                    }
                }
                rollResult = mod.ApplyRollResultMod(rollResult);
            }
            if (!enemyModApplied)
            {
                rollResult = enemyController.ApplyRollResultMods(rollResult);
            }

            // Apply roll results
            playerBattleStatus.ApplyResult(rollResult);
            enemyBattleStatus.ApplyResult(rollResult);

            // Apply enemy and player post damage effects
            enemyModApplied = false;
            foreach (IPostDamageModifier mod in PlayerStatus.Mods.GetPostDamageModifiers())
            {
                if (!enemyModApplied)
                {
                    int priority = ((Modifier)mod).priority;
                    if (enemyController.GetPostDamagePriority() <= priority)
                    {
                        enemyController.ApplyPostDamageEffects(rollResult);
                        enemyModApplied = true;
                    }
                }
                mod.ApplyPostDamageMod(rollResult);
            }
            if (!enemyModApplied)
            {
                enemyController.ApplyPostDamageEffects(rollResult);
            }

            // Decrement roll-bounded mods
            PlayerStatus.Mods.DecrementAndDeregisterModsIfNecessary();
            currentRoll++;

            // Battle status messages
            CreateStatusMessages(playerStatusMessagesToShow, enemyStatusMessagesToShow);
            CreateModMessages();
            playerStatusMessagesToShow.Clear();
            enemyStatusMessagesToShow.Clear();

            CheckBattleComplete();

            UpdateRollUI(rollValues.Item1, rollValues.Item2, !completed);
            UpdateHealthUI(rollResult, !completed);
        }

        private void CheckBattleComplete()
        {
            // Disable when the battle is over, and display result
            if (playerBattleStatus.currentHealth <= 0 || enemyBattleStatus.currentHealth <= 0)
            {
                // End any roll-bounded modifiers
                PlayerStatus.Mods.DeregisterAllRollBoundedMods();

                // Reset roll count
                currentRoll = 0;

                if (playerBattleStatus.currentHealth > 0)
                {
                    if (CurrentLevel.currentEnemyName == "Boss")
                    {
                        ShowResult("Dungeon Defeated!", true);
                    }
                    else
                    {
                        ShowResult("Victory", false);
                        GenerateItemDrop();
                    }
                }
                else
                {
                    ShowResult("Defeat", true);
                }
                completed = true;
            }
        }

        private void ShowResult(string resultText, bool showExitButton)
        {
            resultTextPanel.SetActive(true);
            resultTextPanel.GetComponentInChildren<Text>().text = resultText;
            exitToMenuButton.SetActive(showExitButton);
        }

        private void UpdateRollUI(int playerRoll, int enemyRoll, bool fade)
        {
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

        private void UpdateHealthUI(RollResult rollResult, bool fade)
        {
            UpdateHealthUI(rollResult.PlayerDamage, playerRollDamageUI,
                playerRollDamageText, fade);
            UpdateHealthUI(rollResult.PlayerHeal, playerRollHealUI,
                playerRollHealText, fade);
            UpdateHealthUI(rollResult.EnemyDamage, enemyRollDamageUI,
                enemyRollDamageText, fade);
            UpdateHealthUI(rollResult.EnemyHeal, enemyRollHealUI,
                enemyRollHealText, fade);
        }

        private void UpdateHealthUI(int healthDiff, GameObject uiParent, Text text, bool fade)
        {
            if (healthDiff == 0)
            {
                uiParent.SetActive(false);
            }
            else
            {
                uiParent.GetComponent<Image>().CrossFadeAlpha(1, 0, false);
                text.CrossFadeAlpha(1, 0, false);
                uiParent.SetActive(true);
                text.text = healthDiff.ToString();
                if (fade)
                {
                    uiParent.GetComponent<Image>().CrossFadeAlpha(0, rollInterval, false);
                    text.CrossFadeAlpha(0, rollInterval, false);
                }
            }
        }

        public bool ApplyPlayerItem(Item item)
        {
            if (completed)
            {
                return false;
            }

            // Update PlayerStatus
            if (PlayerStatus.UseItem(item, true))
            {
                // Add status messages if there are any
                CreateStatusMessages(new List<string>() { item.playerStatusMessage },
                    new List<string>() { item.enemyStatusMessage });
                return true;
            }
            return false;
        }

        public void GenerateItemDrop()
        {
            Item itemDrop = CurrentLevel.GetEnemyItemDrop();
            if (itemDrop != null)
            {
                if (itemDrop.name == "Key")
                {
                    PlayerStatus.KeyCount++;
                }
                else
                {
                    PlayerStatus.AddItem(itemDrop);
                }
                itemDropPanel.SetActive(true);
                GameObject newIcon = BattleItemUI.InstantiateItemIcon(itemDrop,
                    itemIconPrefab, itemDropPanel.transform, false);
                newIcon.GetComponent<ItemIcon>().ItemCount = 1;
            }
        }

        public void AddRollBoundedMod(Modifier mod, int priority)
        {
            AddRollBoundedMod(mod, null, null);
        }

        // Used when something else (an enemy) needs to add a mod that ends when the battle ends
        public void AddRollBoundedMod(Modifier mod, string playerUiMessage, string enemyUiMessage)
        {
            PlayerStatus.Mods.RegisterModifier(mod);
            playerStatusMessagesToShow.Add(playerUiMessage);
            enemyStatusMessagesToShow.Add(enemyUiMessage);
        }

        public static void AddPlayerModMessage(string message)
        {
            playerModMessagesToShow.Add(message);
        }

        public static void AddEnemyModMessage(string message)
        {
            enemyModMessagesToShow.Add(message);
        }

        private void CreateModMessages()
        {
            for (int i = 0; i < playerModMessagesToShow.Count; i++)
            {
                CreateStatusMessage(playerModMessagesToShow[i], playerModMessagesParent, i);
            }
            for (int i = 0; i < enemyModMessagesToShow.Count; i++)
            {
                CreateStatusMessage(enemyModMessagesToShow[i], enemyModMessagesParent, i);
            }
        }

        private void CreateStatusMessages(List<string> playerMessages, List<string> enemyMessages)
        {
            if (playerMessages != null)
            {
                for (int i = 0; i < playerMessages.Count; i++)
                {
                    CreateStatusMessage(playerMessages[i], playerStatusMessagesParent, i);
                }
            }
            if (enemyMessages != null)
            {
                for (int i = 0; i < enemyMessages.Count; i++)
                {
                    CreateStatusMessage(enemyMessages[i], enemyStatusMessagesParent, i);
                }
            }
        }

        private void CreateStatusMessage(string message, RectTransform parent, int index)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            GameObject newMessage = Instantiate(statusMessagePrefab, parent);
            Text messageText = newMessage.GetComponentInChildren<Text>();
            messageText.text = message;
            if (index > 0)
            {
                newMessage.GetComponent<RectTransform>().anchoredPosition +=
                    (statusMessageSpacing * index * Vector2.down);
            }
        }

        public static int GetCurrentRoll()
        {
            return currentRoll;
        }
    }
}
