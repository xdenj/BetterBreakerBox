using GameNetcodeStuff;
using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPrefix]
        static void KillPlayerPatch(PlayerControllerB __instance)
        {
            BetterBreakerBox.logger.LogDebug("Player died, destroying timer object.");
            BetterBreakerBox.DestroyTimerObject();
        }
    }
}
