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
            return false;
        }
    }
}
