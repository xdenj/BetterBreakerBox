using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using BetterBreakerBox.Patches;

namespace BetterBreakerBox
{
    [BepInPlugin(MODGUID, MODNAME, MODVERSION)]
    public class BetterBreakerBox : BaseUnityPlugin
    {
        private const string MODGUID = "den.BetterBreakerBox";
        private const string MODNAME = "BetterBreakerBox";
        private const string MODVERSION = "1.0.0";

        private readonly Harmony harmony = new(MODGUID);



        internal ManualLogSource logger;

        public static bool[] SwitchStates = new bool[5];
        public static bool StatesSet = false;
        public static string LastState = "";

        //flags:
        public static bool ArmTurret = true;
        public static bool BerserkTurret = false;
        public static bool LeaveShip = false;

        internal static BetterBreakerBox Instance;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else return;

            logger = BepInEx.Logging.Logger.CreateLogSource(MODGUID);
            logger.LogInfo($"{MODGUID}-{MODVERSION} has been loaded! Patching methods...");
            //harmony.PatchAll(typeof(BetterBreakerBox)); // not needed, apparently

            try
            {
                harmony.PatchAll(typeof(BreakerBoxPatch));
                logger.LogInfo($"BreakerBox patches applied!");
            }
            catch (Exception e)
            {
                logger.LogError($"Error patching BreakerBox!: {e.Message}");
            }

            try
            {
                harmony.PatchAll(typeof(RoundManagerPatch));
                logger.LogInfo($"RoundManager patches applied!");
            }
            catch (Exception e)
            {
                logger.LogError($"Error patching RoundManager!: {e.Message}");
            }


            try
            {
                harmony.PatchAll(typeof(TurretPatch));
                logger.LogInfo($"Turret patches applied!");
            }
            catch (Exception e)
            {
                logger.LogError($"Error patching Turret!: {e.Message}");
            }

            try
            {
                harmony.PatchAll(typeof(StartOfRoundPatch));
                logger.LogInfo($"StartOfRound patches applied!");
            }
            catch (Exception e)
            {
                logger.LogError($"Error patching StartOfRound!: {e.Message}");
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
            ResetActions();
            LeaveShip = true;
        }

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
}
