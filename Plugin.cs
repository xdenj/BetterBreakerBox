using BepInEx;
using BepInEx.Logging;
using BetterBreakerBox.Behaviours;
using BetterBreakerBox.Configs;
using BetterBreakerBox.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
#if DEBUG
using System.IO;
#endif
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using TerminalApi.Classes;
using static TerminalApi.TerminalApi;
using GameNetcodeStuff;

namespace BetterBreakerBox
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID, LethalLib.Plugin.ModVersion)]
    [BepInDependency("atomic.terminalapi")]
    public class BetterBreakerBox : BaseUnityPlugin
    {
        internal static BetterBreakerBox? Instance;
        public static new BetterBreakerBoxConfig MyConfig { get; internal set; }
        private readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        internal static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);

        public static GameObject BetterBreakerBoxManagerPrefab = null!;
        private List<ActionDefinition> actionsDefinitions = new List<ActionDefinition>();
        internal Dictionary<string, ActionDefinition> switchActionMap = new Dictionary<string, ActionDefinition>();
        private HashSet<ActionDefinition> returnedActions = new HashSet<ActionDefinition>();

        internal static BreakerBox breakerBoxInstance;


        private static GameObject timerObject;
        private static TextMeshProUGUI timerTextMesh;


        //flags and other stuff
        public static bool isHost;
        public static bool hasRandomizedActions = false;
        public static bool isTimer;
        public static bool[] SwitchStates = new bool[5];
        public static string SwitchesTurnedOn = "";
        public static bool StatesSet = false;
        public static string LastState = "";
        public static bool ActionLock = false;
        public static bool LocalPlayerTriggered = false;
        public static bool isPowerOffAction = false;
        public static bool isBreakerBoxEnabled = true;
        public static bool isFacilityPowered = false;


        // action flags:
        public static bool DisarmTurrets = false;
        public static bool BerserkTurrets = false;
        public static bool LeaveShip = false;


        //action to index mapping
        internal static Dictionary<string, int> actionToIndex = new Dictionary<string, int>
        {
            {"Error", -1},
            {"TurretsDisarm", 0},
            {"TurretsBerserk", 1},
            {"ShipLeave", 2},
            {"DoNothing", 3},
            {"ChargeEnable", 4},
            {"Zap", 5},
            {"SwapDoors", 6},
            {"SwitchPower", 7},
            {"EMP", 8}
        };
        public Dictionary<int, string> indexToAction = new Dictionary<int, string>();




        public static Dictionary<string, ActionDefinition> GetSwitchActionMap()
        {
            return Instance?.switchActionMap;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else return;

            MyConfig = new(base.Config);
            indexToActionInit();
            ApplyPatches();
            PopulateActions();
            NetcodePatcher();
            InitializePrefabs();
            logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID}-{MyPluginInfo.PLUGIN_VERSION} has been loaded!");

            AddCommand("breakerbox", new CommandInfo { Title = "breakerbox", Category = "other", Description = "Retrieve entries of the Facility's Handbook [50 credits]", DisplayTextSupplier = OnBreakerBoxCommand });
        }

        private void indexToActionInit()
        {
            foreach (var pair in actionToIndex)
            {
                indexToAction[pair.Value] = pair.Key;
            }
        }
        private void InitializePrefabs()
        {
            BetterBreakerBoxManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("BetterBreakerBox Manager");
            BetterBreakerBoxManagerPrefab.AddComponent<BetterBreakerBoxManager>();
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        internal void ApplyPatches()
        {
            logger.LogInfo("Patching methods...");
            TryPatches(typeof(BreakerBoxPatch), "BreakerBox");
            TryPatches(typeof(RoundManagerPatch), "RoundManager");
            TryPatches(typeof(TurretPatch), "Turret");
            TryPatches(typeof(StartOfRoundPatch), "StartOfRound");
            TryPatches(typeof(PlayerControllerBPatch), "PlayerControllerB");
        }

        private void TryPatches(Type patchType, string name)
        {
            try
            {
                harmony.PatchAll(patchType);
                logger.LogInfo($"{name} successfully patched!");
            }
            catch (Exception e)
            {
                logger.LogError($"Couldn't patch {name}!!!:\n{e}");
            }
        }

        public string OnBreakerBoxCommand()
        {
            string combosOne = "";
            string combosTwo = "";
            string combosThree = "";
            string actionOne = "";
            string actionTwo = "";
            string actionThree = "";
            string output = "";

            string terminalPrePreText = "<color=red>Already retrieved one entry this round.\nTry again next round!</color>\n\n";
            string terminalPostText = "\n\n<color=red>Insufficient credits to retrieve entries from the Facility Handbook.</color>\n\n";
            string terminalPreText =
                "<color=blue><u>-- Facility Handbook --</u></color>\n\n" +
                "Breaker Box:\n" +
                "Initial inspection of the Facility's wiring has revealed that the previous electrician got a bit...distracted.\n" +
                "The breaker box is a mess, and the switches are not labeled. In their stead the Company has decided to promote you to Electrician Specialist!!! We expect you to see to it that the breaker box is fully functional and in pristine condition.\n" +
                "We have graciously supplied you with notes from the previous electrician. We have good faith that you won't let The Company down like the previous disappointment. You wouldn't want us to instate our disciplinary measures.\n\n";
            var terminal = FindObjectOfType<Terminal>();

            if (BetterBreakerBoxManager.Instance is { } manager)
            {
                int price = manager.hintPrice.Value;
                int index = manager.terminalOutputIndex.Value;
                bool hasBoughtThisRound = manager.hasBoughtThisRound.Value;
                if (hasBoughtThisRound)
                {
                    index = index - 1;
                }

                switch (index)
                {
                    case 0:
                        if (terminal.groupCredits >= price || hasBoughtThisRound)
                        {
                            if (manager.actionOne.Value == -1)
                            {
                                actionOne = "No entries found in Company Handbook";
                                combosOne = "";
                            }
                            else
                            {
                                actionOne = indexToAction[manager.actionOne.Value] + ": ";
                                combosOne = IntToCombos(manager.comboOne.Value);
                            }
                            output = actionOne + combosOne;
                        }

                        break;

                    case 1:
                        if (manager.actionOne.Value == -1)
                        {
                            actionOne = "No entries found in Company Handbook";
                            combosOne = "";
                        }
                        else
                        {
                            actionOne = indexToAction[manager.actionOne.Value] + ": ";
                            combosOne = IntToCombos(manager.comboOne.Value);
                        }
                        if (terminal.groupCredits >= price || hasBoughtThisRound)
                        {
                            if (manager.actionTwo.Value == -1)
                            {
                                actionTwo = "No entries found in Company Handbook";
                                combosTwo = "";
                            }
                            else
                            {
                                actionTwo = indexToAction[manager.actionTwo.Value] + ": ";
                                combosTwo = IntToCombos(manager.comboTwo.Value);
                            }
                        }
                        output = actionOne + combosOne + "\n" + actionTwo + combosTwo;
                        break;

                    case 2:
                        if (manager.actionOne.Value == -1)
                        {
                            actionOne = "No entries found in Company Handbook";
                            combosOne = "";
                        }
                        else
                        {
                            actionOne = indexToAction[manager.actionOne.Value] + ": ";
                            combosOne = IntToCombos(manager.comboOne.Value);
                        }
                        if (manager.actionTwo.Value == -1)
                        {
                            actionTwo = "No entries found in Company Handbook";
                            combosTwo = "";
                        }
                        else
                        {
                            actionTwo = indexToAction[manager.actionTwo.Value] + ": ";
                            combosTwo = IntToCombos(manager.comboTwo.Value);
                        }
                        if (terminal.groupCredits >= price || hasBoughtThisRound)
                        {
                            if (manager.actionThree.Value == -1)
                            {
                                actionThree = "No entries found in Company Handbook";
                                combosThree = "";
                            }
                            else
                            {
                                actionThree = indexToAction[manager.actionThree.Value] + ": ";
                                combosThree = IntToCombos(manager.comboThree.Value);
                            }
                        }
                        output = actionOne + combosOne + "\n" + actionTwo + combosTwo + "\n" + actionThree + combosThree;
                        break;
                }
                output = "<color=orange>" + output + "</color>";

                if (hasBoughtThisRound)
                {
                    output = terminalPrePreText + terminalPreText + output;
                }
                else if (terminal.groupCredits < price)
                {
                    output = terminalPreText + output + terminalPostText;
                }
                else
                {
                    output = terminalPreText + output;
                    manager.SetHasBoughtThisRound(true);
                    manager.SyncGroupCredits(terminal.groupCredits - price);
                    manager.SetTerminalOutputIndex(manager.terminalOutputIndex.Value + 1);
                }
                return output + "\n\n";
            }
            else
            {
                return "Error retrieving terminal output.";
            }

        }

        internal static void ResetNewDay()
        {
            DestroyTimerObject();
            SwitchesTurnedOn = "";
            StatesSet = false;
            LastState = "";
            ActionLock = false;
            LocalPlayerTriggered = false;
            isBreakerBoxEnabled = true;

            if (BetterBreakerBoxManager.Instance is { } betterBreakerBoxManagerInstance)
            {
                betterBreakerBoxManagerInstance.SetHasBoughtThisRound(false);
            }
            ResetActions();
        }

        internal static void ResetNewRound()
        {
            hasRandomizedActions = false;
            if (BetterBreakerBoxManager.Instance is { } manager)
            {
                manager.SetTerminalOutputIndex(0);
                manager.SetComboOne(0);
                manager.SetComboTwo(0);
                manager.SetComboThree(0);
                manager.SetActionOne(0);
                manager.SetActionTwo(0);
                manager.SetActionThree(0);
            }
            ResetNewDay();

        }

        internal static void ResetActions()
        {
            DisarmTurrets = false;
            BerserkTurrets = false;
            LeaveShip = false;
            isPowerOffAction = false;
        }

        public static void UpdateSwitchStates(int index, bool state)
        {
            if (index >= 0 && index < SwitchStates.Length)
            {
                SwitchStates[index] = state;
            }
        }

        private void PopulateActions()
        {
            int weightDoNothing = Math.Clamp(BetterBreakerBoxConfig.weightDoNothing.Value, 0, int.MaxValue);
            bool doNothingOnce = BetterBreakerBoxConfig.doNothingOnce.Value;

            if (new[] {
                BetterBreakerBoxConfig.disarmTurretsOnce.Value,
                BetterBreakerBoxConfig.berserkTurretsOnce.Value,
                BetterBreakerBoxConfig.shipLeaveOnce.Value,
                BetterBreakerBoxConfig.doNothingOnce.Value,
                BetterBreakerBoxConfig.enableChargeOnce.Value,
                BetterBreakerBoxConfig.zapOnce.Value,
                BetterBreakerBoxConfig.swapDoorsOnce.Value,
                BetterBreakerBoxConfig.switchPowerOnce.Value,
                BetterBreakerBoxConfig.empOnce.Value
            }.All(value => value) || new[] {
                BetterBreakerBoxConfig.weightBerserkTurrets.Value,
                BetterBreakerBoxConfig.weightDisarmTurrets.Value,
                BetterBreakerBoxConfig.weightShipLeave.Value,
                BetterBreakerBoxConfig.weightDoNothing.Value,
                BetterBreakerBoxConfig.weightEnableCharge.Value,
                BetterBreakerBoxConfig.weightZap.Value,
                BetterBreakerBoxConfig.weightSwapDoors.Value,
                BetterBreakerBoxConfig.weightSwitchPower.Value,
                BetterBreakerBoxConfig.weightEMP.Value
            }.Sum() == 0)
            {
                doNothingOnce = false; // Ensure at least one action can be assigned multiple times
                weightDoNothing = 1; // Ensure at least one action has positive weight
            }

            //actionsDefinitions.Add(new ActionDefinition(SwitchAction action, int weight, bool assignOnce, string headerText, string bodyText, bool isWarning, bool displayMessage));
            actionsDefinitions.Add(new ActionDefinition(TurretsDisarm, Math.Clamp(BetterBreakerBoxConfig.weightDisarmTurrets.Value, 0, int.MaxValue), BetterBreakerBoxConfig.disarmTurretsOnce.Value, "<color=red>Critical power loss!</color>", "Turrets temporarily disarmed.", false, true));
            actionsDefinitions.Add(new ActionDefinition(TurretsBerserk, Math.Clamp(BetterBreakerBoxConfig.weightBerserkTurrets.Value, 0, int.MaxValue), BetterBreakerBoxConfig.berserkTurretsOnce.Value, "<color=red>Security breach detected!", "Activating Turret berserk mode for enhanced Facility protection!", true, true));
            actionsDefinitions.Add(new ActionDefinition(ShipLeave, Math.Clamp(BetterBreakerBoxConfig.weightShipLeave.Value, 0, int.MaxValue), BetterBreakerBoxConfig.shipLeaveOnce.Value, "Electromagnetic anomaly!", "The Company strongly advises all Employees to evacuate to the Autopilot Ship immediately!", true, true));
            actionsDefinitions.Add(new ActionDefinition(DoNothing, weightDoNothing, doNothingOnce, "LOL", "We're doing nothing", false, true));
            actionsDefinitions.Add(new ActionDefinition(ChargeEnable, Math.Clamp(BetterBreakerBoxConfig.weightEnableCharge.Value, 0, int.MaxValue), BetterBreakerBoxConfig.enableChargeOnce.Value, "<color=blue>Charging enabled!</color>", "Battery-powered items can now be charged at the Breaker Box.", false, true));
            actionsDefinitions.Add(new ActionDefinition(Zap, Math.Clamp(BetterBreakerBoxConfig.weightZap.Value, 0, int.MaxValue), BetterBreakerBoxConfig.zapOnce.Value, "<color=red>ZAP!</color>", "Player has been zapped!", true, true));
            actionsDefinitions.Add(new ActionDefinition(SwapDoors, Math.Clamp(BetterBreakerBoxConfig.weightSwapDoors.Value, 0, int.MaxValue), BetterBreakerBoxConfig.swapDoorsOnce.Value, "<color=blue>Doors swapped!</color>", "Big doors have been swapped.", false, true));
            actionsDefinitions.Add(new ActionDefinition(SwitchPower, Math.Clamp(BetterBreakerBoxConfig.weightSwitchPower.Value, 0, int.MaxValue), BetterBreakerBoxConfig.switchPowerOnce.Value, "<color=red>Power off!</color>", "Facility power has been turned off.", true, false));
            actionsDefinitions.Add(new ActionDefinition(EMP, Math.Clamp(BetterBreakerBoxConfig.weightEMP.Value, 0, int.MaxValue), BetterBreakerBoxConfig.empOnce.Value, "<color=red>EMP!</color>", "EMP has been activated. All electronic systems offline", true, true));
            // Add other actions here...
        }

        internal async void RandomizeActions()
        {
            switchActionMap.Clear();
            List<ActionDefinition> allActions = new List<ActionDefinition>(actionsDefinitions);
            System.Random rng = new System.Random();

            // Calculate total weight for normalization
            double totalWeight = allActions.Sum(a => a.Weight);
            List<(double cumulativeWeight, ActionDefinition action)> weightedActions = new List<(double cumulativeWeight, ActionDefinition action)>();
            double cumulative = 0;

            // Create a cumulative distribution
            foreach (var action in allActions)
            {
                cumulative += action.Weight / totalWeight;
                weightedActions.Add((cumulative, action));
            }

            int combinationCount = 32;
            for (int i = 0; i < combinationCount; i++)
            {
                string binaryKey = Convert.ToString(i, 2).PadLeft(5, '0');
                double randomValue = rng.NextDouble();

                // Find the first action where the cumulative weight is greater than or equal to the random value
                var selectedAction = weightedActions.FirstOrDefault(wa => wa.cumulativeWeight >= randomValue).action;

                // Assign the action to the current combination
                switchActionMap[binaryKey] = selectedAction;

                // If the action should only be assigned once, remove it from the list
                if (selectedAction.AssignOnce)
                {
                    weightedActions.RemoveAll(wa => wa.action == selectedAction);

                    // Recalculate the cumulative distribution without the removed action
                    totalWeight -= selectedAction.Weight;
                    cumulative = 0;
                    weightedActions = weightedActions.Select(wa =>
                    {
                        cumulative += wa.action.Weight / totalWeight;
                        return (cumulative, wa.action);
                    }).ToList();
                }
            }

#if DEBUG
            //DEBUG Display of Actions mapped to switch combos and write them to a text file for persistent viewing
            var groupedByMethodName = switchActionMap
                .Where(entry => entry.Value != null)
                .GroupBy(
                    entry => entry.Value.Action.Method.Name,
                    entry => entry.Key,
                    (methodName, keys) => new { MethodName = methodName, Keys = keys });

            List<DialogueSegment> dialogueList = new List<DialogueSegment>();

            string pluginDirectory = Paths.PluginPath;
            string specificPluginPath = Path.Combine(pluginDirectory, MyPluginInfo.PLUGIN_NAME);
            Directory.CreateDirectory(specificPluginPath);
            string filePath = Path.Combine(specificPluginPath, "switches.txt");
            StringBuilder fileContent = new StringBuilder();

            foreach (var group in groupedByMethodName)
            {
                string keysCombined = string.Join(", ", group.Keys); // Combines the keys into a comma-separated list
                dialogueList.Add(new DialogueSegment
                {
                    bodyText = keysCombined,
                    speakerText = group.MethodName
                });
                // Prepare content for the file
                fileContent.AppendLine($"{group.MethodName}: {keysCombined}");
            }

            DialogueSegment[] dialogue = dialogueList.ToArray();
            HUDManager.Instance.ReadDialogue(dialogue);

            // Write to the file, overwriting any existing content
            File.WriteAllText(filePath, fileContent.ToString());
#endif
            PrepareTerminalHints();
        }

        public static void DisplayActionMessage(string headerText, string bodyText, bool isWarning)
        {
            HUDManager.Instance.DisplayTip(headerText, bodyText, isWarning);
        }

        public static void DisplayTimer(string name, float timeLeft, float totalTime)
        {
            if (FindObjectOfType<PlayerControllerB>().isPlayerDead) return;

            int minutes = (int)timeLeft / 60;
            int seconds = (int)timeLeft % 60;
            float percentageLeft = (timeLeft / totalTime) * 100;
            Color color = GetColorForPercentage(percentageLeft);
            if (timerObject == null)
            {
                CreateTimerObject();
                return;
            }

            ((TMP_Text)timerTextMesh).text = $"{minutes:D2}:{seconds:D2}";
            ((TMP_Text)timerTextMesh).color = color;
        }

        private static Color GetColorForPercentage(float percentage)
        {
            if (percentage < 25) return Color.red;
            if (percentage < 50) return Color.yellow;
            return Color.green;
        }

        public void PrepareTerminalHints()
        {
            // Filter out DoNothing and already returned actions
            returnedActions.Clear();
            var filteredActions = switchActionMap
                .Where(pair => pair.Value.Action.Method.Name != "DoNothing" && !returnedActions.Contains(pair.Value))
                .ToList();
            for (int i = 0; i < 3; i++)
            {
                if (filteredActions.Count != 0)
                {  // Get a random action from the filtered list
                    var random = new System.Random();
                    var randomAction = filteredActions[random.Next(filteredActions.Count)].Value;

                    // Add this action to the set of returned actions
                    returnedActions.Add(randomAction);

                    // Gather all switch combinations for this action
                    var combinations = switchActionMap
                        .Where(pair => pair.Value == randomAction)
                        .Select(pair => pair.Key)
                        .ToList();

                    // Return the formatted string
                    int result = 0;
                    foreach (var combination in combinations)
                    {
                        // Convert the binary string to a decimal integer
                        int value = Convert.ToInt32(combination, 2);

                        // Set the corresponding bit in the result integer
                        // Shift 1 left by (value - 1) because bit positions are 0-based
                        result |= (1 << (value - 1));
                    }
                    if (BetterBreakerBoxManager.Instance is { } manager)
                    {
                        switch (i)
                        {
                            case 0:
                                manager.SetComboOne(result);
                                manager.SetActionOne(actionToIndex[randomAction.Action.Method.Name]);
                                break;
                            case 1:
                                manager.SetComboTwo(result);
                                manager.SetActionTwo(actionToIndex[randomAction.Action.Method.Name]);
                                break;
                            case 2:
                                manager.SetComboThree(result);
                                manager.SetActionThree(actionToIndex[randomAction.Action.Method.Name]);
                                break;
                        }
                    }
                }
            }
        }

        public string IntToCombos(int intCombos)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                // Check if the ith bit is set
                if ((intCombos & (1 << i)) != 0)
                {
                    // If set, calculate the corresponding binary string
                    string combo = Convert.ToString(i + 1, 2).PadLeft(5, '0');
                    if (result.Length > 0)
                    {
                        result.Append(", ");
                    }
                    result.Append(combo);
                }
            }
            return result.ToString();
        }

        public static int BinaryStringToInt(string binaryString)
        {
            try
            {
                return Convert.ToInt32(binaryString, 2);
            }
            catch (FormatException e)
            {
                logger.LogError("Input string is not a valid binary number.\n{e}");
                return -1; // Return -1 or any other appropriate value to indicate the error
            }
        }

        public static void UpdateHintPrice(int newPrice)
        {
            if (BetterBreakerBoxManager.Instance is { } manager)
            {
                manager.SetHintPrice(Math.Clamp(newPrice, 0, int.MaxValue));
            }
        }

        //Actions:
        public void TurretsDisarm()
        {
            ResetActions();
            DisarmTurrets = true;
        }

        public void TurretsBerserk()
        {
            ResetActions();
            BerserkTurrets = true;

        }
        public void ShipLeave()
        {
            ResetActions();
            LeaveShip = true;
        }
        public void DoNothing()
        {
            ResetActions();
            ActionLock = false;
        }

        //TODO: Implement remaining actions
        public void SwitchPower()
        {
            ResetActions();
            isPowerOffAction = true;
            logger.LogDebug($"isFacilityPowered: {isFacilityPowered} | setting Facility power to: {!isFacilityPowered}");

            if (isFacilityPowered)
            {
                RoundManager.Instance.PowerSwitchOffClientRpc();
                breakerBoxInstance.breakerBoxHum.Stop();
                if (BetterBreakerBoxManager.Instance is { } manager)
                {
                    manager.DisplayActionMessageClientRpc("Warning", "Facility power has been turned off.", true);
                }
            }
            else
            {
                RoundManager.Instance.PowerSwitchOnClientRpc();
                breakerBoxInstance.breakerBoxHum.Play();
                if (BetterBreakerBoxManager.Instance is { } manager)
                {
                    manager.DisplayActionMessageClientRpc("Information", "Facility power has been turned on.", false);
                }

            }
            isFacilityPowered = !isFacilityPowered;
            //FindObjectOfType<BreakerBox>().isPowerOn = !isFacilityPowered;
            ActionLock = false;
            isPowerOffAction = false;
        }

        public void ChargeEnable()
        {
            ResetActions();
            if (breakerBoxInstance.GetComponent<ChargingManager>() == null)
            {
                breakerBoxInstance.gameObject.AddComponent<ChargingManager>();
            }
            else
            {
                BetterBreakerBoxManager.Instance.DisplayActionMessageClientRpc("Information", "Charging is already enabled.", false);
            }
            ActionLock = false;
        }

        public static void Zap()
        {
            ResetActions();
            BetterBreakerBoxManager.Instance.ZapClientRpc(Math.Clamp(BetterBreakerBoxConfig.zapDamage.Value, 0, 100));
            ActionLock = false;
        }

        public void StealthMines()
        {

        }

        public void DisableMines()
        {

        }

        public void Lockdown()
        {

        }

        public void SwapDoors()
        {
            ResetActions();
            TerminalAccessibleObject[] bigDoors = FindObjectsOfType<TerminalAccessibleObject>().Where(obj => obj.isBigDoor).ToArray();
            if (bigDoors.Length > 0)
            {
                foreach (TerminalAccessibleObject bigDoor in bigDoors)
                {
                    bigDoor.SetDoorOpen(!bigDoor.isDoorOpen);
                }
            }
            ActionLock = false;
        }

        public void Thunderstorm()
        {

        }

        public void EMP()
        {
            ResetActions();
            isPowerOffAction = true;
            isBreakerBoxEnabled = false;
            RoundManager.Instance.PowerSwitchOffClientRpc();
            RoundManager.Instance.powerOffPermanently = true;
            breakerBoxInstance.isPowerOn = false;
            breakerBoxInstance.breakerBoxHum.Stop();
            StartOfRound.Instance.PowerSurgeShip();
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            var unlockables = StartOfRound.Instance.unlockablesList.unlockables;
            var teleporterPrefab = unlockables.Find(u => u.unlockableName == "Teleporter").prefabObject;
            var teleporter = teleporterPrefab.GetComponent<ShipTeleporter>();
            localPlayer.statusEffectAudio.PlayOneShot(teleporter.teleporterBeamUpSFX);

            //set charge of all battery powered items to 0
            GrabbableObject[] grabbableObjects = FindObjectsOfType<GrabbableObject>().Where(obj => obj.itemProperties.requiresBattery).ToArray();
            if (grabbableObjects.Length > 0)
            {
                foreach (GrabbableObject grabbableObject in grabbableObjects)
                {
                    grabbableObject.insertedBattery.charge = 0f;
                }
            }

            //disable all turrets
            DisarmTurrets = true;

            //disable all doors
            TerminalAccessibleObject[] bigDoors = FindObjectsOfType<TerminalAccessibleObject>().Where(obj => obj.isBigDoor).ToArray();
            if (bigDoors.Length > 0)
            {
                foreach (TerminalAccessibleObject bigDoor in bigDoors)
                {
                    if (BetterBreakerBoxConfig.lockDoorsOnEmp.Value)
                    {
                        bigDoor.gameObject.GetComponent<AnimatedObjectTrigger>().SetBoolOnClientOnly(false);
                        logger.LogDebug($"Door {bigDoor.name} is now closed");
                    }
                }
            }
            else
            {
                logger.LogDebug("No big doors found");
            }

            //diable charging
            if (breakerBoxInstance.GetComponent<ChargingManager>() != null)
            {
                Destroy(breakerBoxInstance.GetComponent<ChargingManager>());
            }

            ActionLock = false;
        }

        public void FlipMeltdown()
        {

        }

        public static bool CreateTimerObject()
        {
            GameObject val = GameObject.Find("/Systems/UI/Canvas/IngamePlayerHUD/TopRightCorner/ControlTip1");
            if (!val)
            {
                logger.LogError("Could not find ControlTip1");
                return false;
            }
            GameObject val2 = GameObject.Find("/Systems/UI/Canvas/IngamePlayerHUD/TopRightCorner/ControlTip2");
            if (!val2)
            {
                logger.LogError("Could not find ControlTip2");
                return false;
            }
            timerObject = GameObject.Instantiate<GameObject>(val.gameObject, val.transform.parent, false);
            timerObject.name = "BBB Timer";
            float num = val.transform.position.y - val2.transform.position.y;
            timerObject.transform.Translate(0f, num, 0f);
            timerTextMesh = timerObject.GetComponentInChildren<TextMeshProUGUI>();
            logger.LogDebug("BBB Timer created");
            return true;
        }

        public static void DestroyTimerObject()
        {
            if (timerObject)
            {
                GameObject.Destroy(timerObject);
                timerObject = null;
                timerTextMesh = null;
            }
        }
    }

    public class ActionDefinition
    {
        public SwitchAction Action { get; set; }
        public int Weight { get; set; }
        public bool AssignOnce { get; set; }
        public string HeaderText { get; set; }
        public string BodyText { get; set; }
        public bool IsWarning { get; set; }
        public bool DisplayMessage { get; set; }

        public ActionDefinition(SwitchAction action, int weight, bool assignOnce, string headerText, string bodyText, bool isWarning, bool displayMessage)
        {
            Action = action;
            Weight = weight;
            AssignOnce = assignOnce;
            HeaderText = headerText;
            BodyText = bodyText;
            IsWarning = isWarning;
            DisplayMessage = displayMessage;
        }
    }



    public delegate void SwitchAction();

}