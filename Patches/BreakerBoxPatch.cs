using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(BreakerBox))]
    internal class BreakerBoxPatch
    {
        [HarmonyPatch(nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPostfix]
        static void SwitchBreakerPatch(BreakerBox __instance)
        {
            BetterBreakerBox.Instance.logger.LogInfo("Number of levers turned off:  " + __instance.leversSwitchedOff);
            BetterBreakerBox.Instance.logger.LogInfo("Current lever positions: ");
            for (int i = 0; i < __instance.breakerSwitches.Length; i++)
            {
                bool state = __instance.breakerSwitches[i].GetBool("turnedLeft");
                // turnedleft = false means switch is turned off, turnedleft = true means switch is turned on
                BetterBreakerBox.Instance.logger.LogInfo($"Switch {i + 1}: {state}");
                BetterBreakerBox.SwitchStates[i] = state;
            }
            BetterBreakerBox.StatesSet = true;
        }


    }
}
