using BepInEx;
using BepInEx.Logging;
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
using UnityEngine;

namespace BetterBreakerBox
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class BetterBreakerBox : BaseUnityPlugin
    {
        internal static BetterBreakerBox? Instance;
        public static new BetterBreakerBoxConfig MyConfig { get; internal set; }
        private readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        internal static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);

        public static GameObject BetterBreakerBoxManagerPrefab = null!;
        private List<ActionDefinition> actionsDefinitions = new List<ActionDefinition>();
        internal Dictionary<string, ActionDefinition> switchActionMap = new Dictionary<string, ActionDefinition>();

        //flags and other stuff
        public static bool isHost;
        public static bool[] SwitchStates = new bool[5];
        public static string SwitchesTurnedOn = "";
        public static bool StatesSet = false;
        public static string LastState = "";
        
        // action flags:
        public static bool ArmTurret = true;
        public static bool BerserkTurret = false;
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
        }

        private void InitializePrefabs()
        {
            BetterBreakerBoxManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("BetterBreakerBox Manager");
            BetterBreakerBoxManagerPrefab.AddComponent<BetterBreakerBoxBehaviour>();
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

        internal static void ResetBetterBreakerBox()
        {
            StatesSet = false;
            LastState = "";
            ResetActions();
        }

        internal static void ResetActions()
        {
            ArmTurret = true;
            BerserkTurret = false;
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
            actionsDefinitions.Add(new ActionDefinition(DisarmTurrets, BetterBreakerBoxConfig.weightDisarmTurrets.Value, BetterBreakerBoxConfig.disarmTurretsOnce.Value, "Information", "Turrets disarmed!", false));
            actionsDefinitions.Add(new ActionDefinition(BerserkTurrets, BetterBreakerBoxConfig.weightBerserkTurrets.Value, BetterBreakerBoxConfig.berserkTurretsOnce.Value, "Warning!", "Tampering detected, setting Turrets to berserk mode!", true));
            actionsDefinitions.Add(new ActionDefinition(ShipLeave, BetterBreakerBoxConfig.weightShipLeave.Value, BetterBreakerBoxConfig.shipLeaveOnce.Value, "Alert!", "Electromagnetic anomaly detected! Extraction vessel departing ahead of schedule.", true));
            actionsDefinitions.Add(new ActionDefinition(DoNothing, BetterBreakerBoxConfig.weightDoNothing.Value, BetterBreakerBoxConfig.doNothingOnce.Value, "LOL", "We're doing nothing", false));
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
            // Use BepInEx's API to get the path to your plugin's directory
            string pluginDirectory = Paths.PluginPath; // This gives you the 'BepInEx/plugins' path

            // If you want it specifically in a folder named after your plugin, append your plugin's name
            string specificPluginPath = Path.Combine(pluginDirectory, MyPluginInfo.PLUGIN_NAME);
            Directory.CreateDirectory(specificPluginPath); // Ensure your plugin's directory exists

            // Define the file path within your specific plugin directory
            string filePath = Path.Combine(specificPluginPath, "swiches.txt");

            // Use a StringBuilder to accumulate all the text to write it at once
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
        }

        public static void DisplayActionMessage(string headerText, string bodyText, bool isWarning)
        {
            HUDManager.Instance.DisplayTip(headerText, bodyText, isWarning);
        }

        //Actions:
        public void DisarmTurrets()
        {
            ResetActions();
            ArmTurret = false;
        }

        public void BerserkTurrets()
        {
            ResetActions();
            BerserkTurret = true;

        }
        public void ShipLeave()
        {
            // TODO: implement a timer before ship leaves
            ResetActions();
            LeaveShip = true;
        }
        public void DoNothing()
        {
            ResetActions();        
        }

        //TODO: Implement remaining actions
        public void LightsOff()
        {

        }

        public void EnableCharge()
        {

        }

        public void Zap()
        {

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
