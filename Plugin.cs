using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BetterBreakerBox.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetterBreakerBox
{
    [BepInPlugin(MODGUID, MODNAME, MODVERSION)]
    public class BetterBreakerBox : BaseUnityPlugin
    {
        private const string MODGUID = "den.BetterBreakerBox";
        private const string MODNAME = "BetterBreakerBox";
        private const string MODVERSION = "1.0.0";

        private readonly Harmony harmony = new(MODGUID);

        internal static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(BetterBreakerBox.MODGUID);

        public static bool[] SwitchStates = new bool[5];
        public static bool StatesSet = false;
        public static string LastState = "";

        //config:
        private ConfigEntry<int> weightDisarmTurrets;
        private ConfigEntry<int> weightBerserkTurrets;
        private ConfigEntry<int> weightShipLeave;
        private ConfigEntry<int> weightDoNothing;

        private ConfigEntry<bool> disarmTurretsOnce;
        private ConfigEntry<bool> berserkTurretsOnce;
        private ConfigEntry<bool> shipLeaveOnce;
        private ConfigEntry<bool> doNothingOnce;


        //flags:
        public static bool ArmTurret = true;
        public static bool BerserkTurret = false;
        public static bool LeaveShip = false;

        internal static BetterBreakerBox? Instance;
        public static GameObject BetterBreakerBoxManagerPrefab = null!;
        private List<ActionDefinition> actionsDefinitions = new List<ActionDefinition>();
        private Dictionary<string, SwitchAction> switchActionMap = new Dictionary<string, SwitchAction>();

        public static Dictionary<string, SwitchAction> GetSwitchActionMap()
        {
            return Instance?.switchActionMap;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else return;
            BindConfigs();
            PopulateActions();
            //RandomizeActions();

            NetcodePatcher();
            InitializePrefabs();

            logger.LogInfo($"{MODGUID}-{MODVERSION} has been loaded!");
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


        internal void BindConfigs()
        {
            string descDisarmTurrets = "disarming Turrets in the facility";
            string descBerserkTurrets = "making Turrets enter berserk mode";
            string descShipLeave = "making the Ship leave early";
            string descDoNothing = "having no action";

            //weights:
            string weightPreDesc = "Adjusts the probability that a switch combination will trigger the action of";
            string weightPostDesc = "Higher weights make this action more likely to be assigned to one of the switch combinations.";

            weightDisarmTurrets = Config.Bind(
                "Weights",
                "weightDisarmTurrets",
                1,
                $"{weightPreDesc} {descDisarmTurrets}. {weightPostDesc}");

            weightBerserkTurrets = Config.Bind(
               "Weights",
               "weightBerserkTurrets",
               1,
               $"{weightPreDesc} {descBerserkTurrets}. {weightPostDesc}");

            weightShipLeave = Config.Bind(
                "Weights",
                "weightShipLeave",
                1,
                $"{weightPreDesc} {descShipLeave}. {weightPostDesc}");

            weightDoNothing = Config.Bind(
                "Weights",
                "weightDoNothing",
                10,
                $"{weightPreDesc} {descDoNothing}. {weightPostDesc}");

            //run once:
            string oncePreDesc = "Select if the action of";
            string oncePostDesc = "should only be assigned to a single combination of Switches";

            disarmTurretsOnce = Config.Bind("Limit", "disarmTurretsOnce", false, $"{oncePreDesc} {descDisarmTurrets} {oncePostDesc}");
            berserkTurretsOnce = Config.Bind("Limit", "berserkTurretsOnce", false, $"{oncePreDesc} {descBerserkTurrets} {oncePostDesc}");
            shipLeaveOnce = Config.Bind("Limit", "shipLeaveOnce", false, $"{oncePreDesc} {descShipLeave} {oncePostDesc}");
            doNothingOnce = Config.Bind("Limit", "doNothingOnce", false, $"{oncePreDesc} {descDoNothing} {oncePostDesc}");
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
            //actionsDefinitions.Add(new ActionDefinition(method, int:weight , bool:assignOnce));
            actionsDefinitions.Add(new ActionDefinition(DisarmTurrets, weightDisarmTurrets.Value, disarmTurretsOnce.Value));
            actionsDefinitions.Add(new ActionDefinition(BerserkTurrets, weightBerserkTurrets.Value, berserkTurretsOnce.Value));
            actionsDefinitions.Add(new ActionDefinition(ShipLeave, weightShipLeave.Value, shipLeaveOnce.Value));
            actionsDefinitions.Add(new ActionDefinition(DoNothing, weightDoNothing.Value, doNothingOnce.Value));
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
                switchActionMap[binaryKey] = selectedAction.Action;

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
            DialogueSegment[] dialogue = new DialogueSegment[32];
            int j = 0;
            logger.LogDebug("Actions have been randomly assigned to combinations: ");
            foreach (var entry in switchActionMap)
            {
                // Get the key
                string key = entry.Key;
                // Get the delegate (method name)
                string methodName = entry.Value?.Method.Name ?? "null";
                if (methodName != null && key != null)
                {
                    // Log the key and the method name to the game's console
                    logger.LogDebug($"State: {key}, Action: {methodName}");
                    dialogue[j] = new DialogueSegment
                    {
                        bodyText = key,
                        speakerText = methodName
                    };

                }
                j++;
            }
            HUDManager.Instance.ReadDialogue(dialogue);
#endif
        }


        //Actions:
        public void DisarmTurrets()
        {
            ResetActions();
            HUDManager.Instance.DisplayTip("Information", "Turrets disarmed!", false, false, "LC_Tip1");
            ArmTurret = false;
        }

        public void BerserkTurrets()
        {
            ResetActions();
            HUDManager.Instance.DisplayTip("Warning!", "Tampering detected, setting Turrets to berserk mode!", true, false, "LC_Tip1");
            BerserkTurret = true;

        }
        public void ShipLeave()
        {
            ResetActions();
            HUDManager.Instance.DisplayTip("Alert!", "Electromagnetic anomaly detected! Extraction vessel departing ahead of schedule.", true, false, "LC_Tip1");
            LeaveShip = true;
        }
        public void DoNothing()
        {
            HUDManager.Instance.DisplayTip("LOL", "We're doing nothing", false, false, "LC_Tip1");
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
        public string Name { get; set; } // Name of the action for display purposes
        public int Weight { get; set; }
        public bool AssignOnce { get; set; }

        public ActionDefinition(SwitchAction action, int weight, bool assignOnce)
        {
            Action = action;
            Weight = weight;
            AssignOnce = assignOnce;
        }
    }

    public delegate void SwitchAction();

}
