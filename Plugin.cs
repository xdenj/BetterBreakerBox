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

namespace BetterBreakerBox
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
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

        //hardcoding price of the breakerbox command
        public static int breakerboxPrice = 50;


        // action flags:
        public static bool DisarmTurrets = false;
        public static bool BerserkTurrets = false;
        public static bool LeaveShip = false;



        public static Dictionary<string, ActionDefinition> GetSwitchActionMap()
        {
            return Instance?.switchActionMap;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else return;
            MyConfig = new(base.Config);
            PopulateActions();
            NetcodePatcher();
            InitializePrefabs();
            logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID}-{MyPluginInfo.PLUGIN_VERSION} has been loaded!");
            ApplyPatches();
            AddCommand("breakerbox", new CommandInfo { Title = "breakerbox", Category = "other", Description = "Retrieve entries of the Facility's Handbook [50 credits]", DisplayTextSupplier = OnBreakerBoxCommand });
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
            string output = ""; 
            string terminalPrePreText = "<color=red>Already retrieved one entry this round.\nTry again next round!</color>\n\n";
            string terminalPostText = "\n\n<color=red>Insufficient credits to retrieve entries from the Facility Handbook.</color>\n\n";
            string terminalPreText =
                "<color=blue><u>-- Facility Handbook --</u></color>\n\n" +
                "...\n...\n\n" +
                "Breaker Box:\n" +
                "Initial inspection of the Facility's breaker box has revealed that the contractor who we hired did a terrible job with the electrical wiring. The breaker box is a mess, and the switches are not labeled. The Company has decided to leave the breaker box as is, and instead, has provided a list of actions that can be triggered by flipping the switches in a specific order.\n\n";
            var terminal = FindObjectOfType<Terminal>();

            if (BetterBreakerBoxManager.Instance is { } manager)
            {
                if (manager.hasBoughtThisRound.Value)
                {
                    output = terminalPrePreText + terminalPreText + manager.terminalOutputString.Value.Data;
                }
                else if (terminal.groupCredits < breakerboxPrice)
                {
                    output = terminalPreText + manager.terminalOutputString.Value.Data + terminalPostText;
                }
                else
                {
                    manager.SetHasBoughtThisRound(true);
                    terminal.SyncGroupCreditsClientRpc(terminal.groupCredits - breakerboxPrice, terminal.numberOfItemsInDropship);
                    manager.SetTerminalOutputString("\n" + manager.terminalOutputString.Value.Data + "\n" + "<color=orange>" + manager.terminalOutputArray.Value.Data[manager.terminalOutputIndex.Value] + "</color>") ;
                    manager.SetTerminalOutputIndex(manager.terminalOutputIndex.Value + 1);
                    output = terminalPreText + manager.terminalOutputString.Value.Data;
                }
                return output;
            }
            else
            {
                return "Error retrieving terminal output.";
            }

        }

        internal static void ResetNewDay()
        {
            SwitchesTurnedOn = "";
            StatesSet = false;
            LastState = "";
            ActionLock = false;
            LocalPlayerTriggered = false;
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
                manager.SetTerminalOutputArray(new string[3]);
                manager.SetTerminalOutputString("");
            }
            ResetNewDay();

        }

        internal static void ResetActions()
        {
            DisarmTurrets = false;
            BerserkTurrets = false;
            LeaveShip = false;
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
            //actionsDefinitions.Add(new ActionDefinition(SwitchAction action, int weight, bool assignOnce, string headerText, string bodyText, bool isWarning);
            actionsDefinitions.Add(new ActionDefinition(TurretsDisarm, BetterBreakerBoxConfig.weightDisarmTurrets.Value, BetterBreakerBoxConfig.disarmTurretsOnce.Value, "<color=red>Critical power loss!</color>", "Turrets temporarily disarmed.", false));
            actionsDefinitions.Add(new ActionDefinition(TurretsBerserk, BetterBreakerBoxConfig.weightBerserkTurrets.Value, BetterBreakerBoxConfig.berserkTurretsOnce.Value, "<color=red>Security breach detected!", "Activating Turret berserk mode for enhanced Facility protection!", true));
            actionsDefinitions.Add(new ActionDefinition(ShipLeave, BetterBreakerBoxConfig.weightShipLeave.Value, BetterBreakerBoxConfig.shipLeaveOnce.Value, "Electromagnetic anomaly!", "The Company strongly advises all Employees to evacuate to the Autopilot Ship immediately!", true));
            actionsDefinitions.Add(new ActionDefinition(DoNothing, BetterBreakerBoxConfig.weightDoNothing.Value, BetterBreakerBoxConfig.doNothingOnce.Value, "LOL", "We're doing nothing", false));
            actionsDefinitions.Add(new ActionDefinition(ChargeEnable, BetterBreakerBoxConfig.weightEnableCharge.Value, BetterBreakerBoxConfig.enableChargeOnce.Value, "<color=blue>Charging enabled!</color>", "Battery-powered items can now be charged at the Breaker Box.", false));
            actionsDefinitions.Add(new ActionDefinition(Zap, BetterBreakerBoxConfig.weightZap.Value, BetterBreakerBoxConfig.zapOnce.Value, "<color=red>ZAP!</color>", "Player has been zapped!", true));
            // Add other actions here...
        }

        internal void RandomizeActions()
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
            string[] terminalOutput = new string[3];
            for (int i = 0; i < 3; i++)
            {
                terminalOutput[i] = GetRandomSwitchCombo();
            }
            BetterBreakerBoxManager.Instance?.SetTerminalOutputArray(terminalOutput);
            BetterBreakerBoxManager.Instance?.SetTerminalOutputIndex(0); //reset terminalOutputIndex after randomizing actions
        }

        public static void DisplayActionMessage(string headerText, string bodyText, bool isWarning)
        {
            HUDManager.Instance.DisplayTip(headerText, bodyText, isWarning);
        }

        public static void DisplayTimer(string name, float timeLeft, float totalTime)
        {

            int minutes = (int)timeLeft / 60;
            int seconds = (int)timeLeft % 60;
            float percentageLeft = (timeLeft / totalTime) * 100;
            Color color = GetColorForPercentage(percentageLeft);
            if (timerObject == null)
            {
                BetterBreakerBoxManager.Instance.CreateTimerObjectClientRpc();
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

        public string GetRandomSwitchCombo()
        {
            // Filter out DoNothing and already returned actions
            var filteredActions = switchActionMap
                .Where(pair => pair.Value.Action.Method.Name != "DoNothing" && !returnedActions.Contains(pair.Value))
                .ToList();

            if (filteredActions.Count == 0)
                return "All actions have been returned.";

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
            return $"{randomAction.Action.Method.Name}: {string.Join(", ", combinations)}";
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
        public void LightsOff()
        {

        }

        public void ChargeEnable()
        {
            ResetActions();
            BreakerBox[] breakerBoxes = FindObjectsOfType<BreakerBox>();
            for (int i = 0; i < breakerBoxes.Length; i++)
            {
                BreakerBox radarBooster = breakerBoxes[i];
                if (radarBooster.GetComponent<ChargingManager>() == null)
                {
                    radarBooster.gameObject.AddComponent<ChargingManager>();
                }
                else
                {
                    BetterBreakerBoxManager.Instance.DisplayActionMessageClientRpc("Information", "Charging is already enabled.", false);
                }

            }
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

        public void SwapLocks()
        {

        }

        public void Thunderstorm()
        {

        }

        public void EMP()
        {

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

        public ActionDefinition(SwitchAction action, int weight, bool assignOnce, string headerText, string bodyText, bool isWarning)
        {
            Action = action;
            Weight = weight;
            AssignOnce = assignOnce;
            HeaderText = headerText;
            BodyText = bodyText;
            IsWarning = isWarning;
        }
    }



    public delegate void SwitchAction();

}