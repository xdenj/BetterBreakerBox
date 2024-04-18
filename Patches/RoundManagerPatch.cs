using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch(nameof(RoundManager.SwitchPower))]
        [HarmonyPrefix]
        static bool SwitchPowerPatch(RoundManager __instance)
        {
            //disabling game's default behaviour of switching off the facility's power after a certain # of switches have been interacted with
            return false;
        }
    }
}
