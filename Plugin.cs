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
using GameNetcodeStuff;
using TerminalApi.Classes;
using static TerminalApi.TerminalApi;


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

        internal void PrepareCommand(int price)
        {
            if (GetKeyword("breakerbox") == null)
            {
                AddCommand("breakerbox", new CommandInfo { Title = "breakerbox", Category = "other", Description = $"Retrieve entries of the Facility's Handbook [{Math.Clamp(price, 0, Int32.MaxValue)} credits]", DisplayTextSupplier = OnBreakerBoxCommand });
            }
        }

        public string OnBreakerBoxCommand()
        {
            var terminal = FindObjectOfType<Terminal>();
            string output = "";
            string terminalHasBought = "<color=red>Already retrieved entries this period.\nTry again next period!</color>\n\n";
            string terminalInsufficientCredits = "\n\n<color=red>Insufficient credits to retrieve entries from the Facility Handbook.</color>\n\n";
            string terminalDescription =
                "<color=blue><u>-- Facility Handbook --</u></color>\n\n" +
                "Breaker Box:\n" +
                "Initial inspection of the Facility's wiring has revealed that the previous electrician got a bit...distracted.\n" +
                "The breaker box is a mess, and the switches are not labeled. In their stead the Company has decided to promote you to Electrician Specialist!!! We expect you to see to it that the breaker box is fully functional and in pristine condition.\n" +
                "We have graciously supplied you with notes from the previous electrician. We have good faith that you won't let The Company down like the previous disappointment. You wouldn't want us to instate our disciplinary measures.\n" +
                "<color=white>Switch position → = 0, Switch position ← = 1</color>\n\n";


            if (BetterBreakerBoxManager.Instance is { } manager)
            {
                bool hasBoughtThisPeriod = manager.hasBoughtThisPeriod.Value;
                int price = manager.hintPrice.Value;
                bool hasSufficientCredits = terminal.groupCredits >= price;
                int limit = manager.terminalOutputIndex.Value;
                double randomNumber = new System.Random().NextDouble();
                int nextLimit = randomNumber < 0.2 ? 1 : (randomNumber < 0.5 ? 2 : (randomNumber < 0.8 ? 3 : 4));

                int[] actions = manager.actions.Value.Data;
                int[] combos = manager.combos.Value.Data;

                if (!hasBoughtThisPeriod)
                {
                    if (hasSufficientCredits)
                    {
                        manager.SyncGroupCredits(terminal.groupCredits - price);
                        terminal.PlayTerminalAudioServerRpc(0);
                        manager.SethasBoughtThisPeriod(true);
                        limit = limit + nextLimit;
                        manager.SetTerminalOutputIndex(limit);
                    }
                }

                if (actions.Length > 0 && combos.Length > 0)
                {
                    for (int i = 0; i < limit; i++)
                    {
                        if (i >= actions.Length || i >= combos.Length)
                        {
                            output = output + "No more entries available.\n";
                            break;
                        }
                        output = output + $"<color=orange>{indexToAction[actions[i]]}: {IntToCombos(combos[i])}</color>\n";
                    }
                }
                output = terminalDescription + output;

                if (hasBoughtThisPeriod)
                {
                    output = terminalHasBought + output;
                    terminal.PlayTerminalAudioServerRpc(2);
                }
                else if (!hasSufficientCredits)
                {
                    output = output + terminalInsufficientCredits;
                    terminal.PlayTerminalAudioServerRpc(2);
                }

            }
            return output;
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
            ResetActions();
        }

        internal static void ResetNewRound()
        {
            hasRandomizedActions = false;
            if (BetterBreakerBoxManager.Instance is { } manager)
            {
                manager.SetTerminalOutputIndex(0);
                manager.InitializeActions(0);
                manager.InitializeCombos(0);
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
            bool displayMessage = false;
#if DEBUG
            displayMessage = true;
#endif

            actionsDefinitions.Add(new ActionDefinition(TurretsDisarm, Math.Clamp(BetterBreakerBoxConfig.weightDisarmTurrets.Value, 0, int.MaxValue), BetterBreakerBoxConfig.disarmTurretsOnce.Value, "<color=red>Critical power loss!</color>", "Turrets temporarily disarmed.", false, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(TurretsBerserk, Math.Clamp(BetterBreakerBoxConfig.weightBerserkTurrets.Value, 0, int.MaxValue), BetterBreakerBoxConfig.berserkTurretsOnce.Value, "<color=red>Security breach detected!", "Activating Turret berserk mode for enhanced Facility protection!", true, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(ShipLeave, Math.Clamp(BetterBreakerBoxConfig.weightShipLeave.Value, 0, int.MaxValue), BetterBreakerBoxConfig.shipLeaveOnce.Value, "Electromagnetic anomaly!", "The Company strongly advises all Employees to evacuate to the Autopilot Ship immediately!", true, true));
            actionsDefinitions.Add(new ActionDefinition(DoNothing, weightDoNothing, doNothingOnce, "LOL", "We're doing nothing", false, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(ChargeEnable, Math.Clamp(BetterBreakerBoxConfig.weightEnableCharge.Value, 0, int.MaxValue), BetterBreakerBoxConfig.enableChargeOnce.Value, "<color=blue>Charging enabled!</color>", "Battery-powered items can now be charged at the Breaker Box.", false, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(Zap, Math.Clamp(BetterBreakerBoxConfig.weightZap.Value, 0, int.MaxValue), BetterBreakerBoxConfig.zapOnce.Value, "<color=red>ZAP!</color>", "Player has been zapped!", true, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(SwapDoors, Math.Clamp(BetterBreakerBoxConfig.weightSwapDoors.Value, 0, int.MaxValue), BetterBreakerBoxConfig.swapDoorsOnce.Value, "<color=blue>Doors swapped!</color>", "Big doors have been swapped.", false, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(SwitchPower, Math.Clamp(BetterBreakerBoxConfig.weightSwitchPower.Value, 0, int.MaxValue), BetterBreakerBoxConfig.switchPowerOnce.Value, "<color=red>Power off!</color>", "Facility power has been turned off.", true, displayMessage));
            actionsDefinitions.Add(new ActionDefinition(EMP, Math.Clamp(BetterBreakerBoxConfig.weightEMP.Value, 0, int.MaxValue), BetterBreakerBoxConfig.empOnce.Value, "<color=red>EMP!</color>", "EMP has been activated. All electronic systems offline", true, displayMessage));
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
            int currentCombination = 0;

            // If the ensureAction flag is set, assign each action with weight > 0 to a combination
            if (BetterBreakerBoxConfig.ensureAction.Value)
            {
                var actionsToAssign = weightedActions.Where(wa => wa.action.Weight > 0).ToList();

                // Shuffle the actions to assign
                actionsToAssign = actionsToAssign.OrderBy(a => rng.Next()).ToList();

                foreach (var actionToAssign in actionsToAssign)
                {
                    string binaryKey = Convert.ToString(currentCombination++, 2).PadLeft(5, '0');
                    switchActionMap[binaryKey] = actionToAssign.action;

                    // If the action should only be assigned once, remove it from the list
                    if (actionToAssign.action.AssignOnce)
                    {
                        weightedActions.RemoveAll(wa => wa.action == actionToAssign.action);

                        // Recalculate the cumulative distribution without the removed action
                        totalWeight -= actionToAssign.action.Weight;
                        cumulative = 0;
                        weightedActions = weightedActions.Select(wa =>
                        {
                            cumulative += wa.action.Weight / totalWeight;
                            return (cumulative, wa.action);
                        }).ToList();
                    }
                }
            }

            // Assign the remaining combinations based on weight
            for (int i = currentCombination; i < combinationCount; i++)
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
            returnedActions.Clear();
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
                .Where(pair => pair.Value.Action.Method.Name != "DoNothing")
                .ToList();
            if (filteredActions.Count > 0)
            {
                if (BetterBreakerBoxManager.Instance is { } manager)
                {
                    manager.InitializeActions(filteredActions.Count);
                    manager.InitializeCombos(filteredActions.Count);
                    int counter = filteredActions.Count;
                    for (int i = 0; i < counter; i++)
                    {
                        filteredActions = filteredActions
                            .Where(pair => !returnedActions.Contains(pair.Value))
                            .ToList();
                        counter = filteredActions.Count;
                        if (counter < 1)
                        {
                            break;
                        }
                        // Get a random action from the filtered list
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
                        manager.SetAction(index: i, value: actionToIndex[randomAction.Action.Method.Name]);
                        manager.SetCombo(index: i, value: result);
                    }
                }
            }
            else
            {
                if (BetterBreakerBoxManager.Instance is { } manager)
                {
                    manager.InitializeActions(1);
                    manager.SetAction(index: 0, value: -1);
                    manager.SetCombo(index: 0, value: -1);
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
                    combo = combo[^5..]; // Take the last 5 characters
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
                BetterBreakerBoxManager.Instance.ToggleBreakerBoxHumClientRpc(false);
#if DEBUG
                if (BetterBreakerBoxManager.Instance is { } manager)
                {
                    manager.DisplayActionMessageClientRpc("Warning", "Facility power has been turned off.", true);
                }
#endif
            }
            else
            {
                RoundManager.Instance.PowerSwitchOnClientRpc();
                BetterBreakerBoxManager.Instance.ToggleBreakerBoxHumClientRpc(true);
#if DEBUG
                if (BetterBreakerBoxManager.Instance is { } manager)
                {
                    manager.DisplayActionMessageClientRpc("Information", "Facility power has been turned on.", false);
                }
#endif
            }
            isFacilityPowered = !isFacilityPowered;
            //FindObjectOfType<BreakerBox>().isPowerOn = !isFacilityPowered;
            ActionLock = false;
            isPowerOffAction = false;
        }

        public void ChargeEnable()
        {
            ResetActions();
            BetterBreakerBoxManager.Instance.ChargeEnableClientRpc();
            ActionLock = false;
        }

        public static void Zap()
        {
            ResetActions();
            BetterBreakerBoxManager.Instance.ZapClientRpc();
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
            BetterBreakerBoxManager.Instance.ToggleBreakerBoxHumClientRpc(false);
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
            BetterBreakerBoxManager.Instance.ChargeDisableClientRpc();

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

        public static void ToggleBreakerBoxHum(bool on)
        {
            if (breakerBoxInstance != null)
            {
                if (on)
                {
                    breakerBoxInstance.breakerBoxHum.Play();
                }
                else
                {
                    breakerBoxInstance.breakerBoxHum.Stop();
                }
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