using BetterBreakerBox.Behaviours;
using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(BreakerBox))]
    internal class BreakerBoxPatch
    {
        [HarmonyPatch(nameof(BreakerBox.Start))]
        [HarmonyPostfix]
        static void StartPatch(BreakerBox __instance)
        {
            if (__instance.gameObject.GetComponent<ChargingManager>() == null)
            {
                __instance.gameObject.AddComponent<ChargingManager>();
            }
        }

        [HarmonyPatch(nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPostfix]
        static void SwitchBreakerPatch(BreakerBox __instance)
        {
            BetterBreakerBox.logger.LogDebug("Number of levers turned off:  " + __instance.leversSwitchedOff);
            BetterBreakerBox.logger.LogDebug("Current lever positions: ");
            BetterBreakerBox.SwitchesTurnedOn = "";
            for (int i = 0; i < __instance.breakerSwitches.Length; i++)
            {
                bool state = __instance.breakerSwitches[i].GetBool("turnedLeft");
                // "turnedleft" is the state of the breaker switch. turnedLeft = off means switch is turned off, turnedleft = true means switch is turned on
                BetterBreakerBox.logger.LogDebug($"Switch {i + 1}: {state}");

                //dunno if we should use an array or string representation of the states here, so doing both for now lol
                BetterBreakerBox.UpdateSwitchStates(i, state);
                BetterBreakerBox.SwitchesTurnedOn += state ? "1" : "0";
            }
            BetterBreakerBox.StatesSet = true;
        }


    }
}
