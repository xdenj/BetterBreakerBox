using BepInEx.Logging;
using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(BreakerBox))]
    internal class BreakerBoxPatch
    {
        [HarmonyPatch(nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPrefix]
        static bool SwitchBreakerPostfix(BreakerBox __instance)
        {
            BetterBreakerBox.isFacilityPowered = __instance.isPowerOn;
            BetterBreakerBox.logger.LogDebug($"isPowerOn {__instance.isPowerOn}");
            BetterBreakerBox.LocalPlayerTriggered = false;
            BetterBreakerBox.logger.LogDebug("Number of levers turned off:  " + __instance.leversSwitchedOff);
            BetterBreakerBox.logger.LogDebug("Current lever positions: ");
            BetterBreakerBox.SwitchesTurnedOn = "";
            for (int i = 0; i < __instance.breakerSwitches.Length; i++)
            {
                bool state = __instance.breakerSwitches[i].GetBool("turnedLeft");
                // "turnedleft" is the state of the breaker switch. turnedLeft = false means switch is turned off, turnedleft = true means switch is turned on

                if (__instance.breakerSwitches[i].gameObject.GetComponent<AnimatedObjectTrigger>().localPlayerTriggered && (BetterBreakerBox.SwitchStates[i] != state)) //if state has changed AND trigger was local player
                {
                    BetterBreakerBox.logger.LogDebug($"Switch {i + 1} was triggered by the local player");
                    BetterBreakerBox.LocalPlayerTriggered = true;
                }
                BetterBreakerBox.logger.LogDebug($"Switch {i + 1}: {state}");

                //dunno if we should use an array or string representation of the states here, so doing both for now lol
                BetterBreakerBox.UpdateSwitchStates(i, state);
                BetterBreakerBox.SwitchesTurnedOn += state ? "1" : "0";
            }
            BetterBreakerBox.StatesSet = true;
            return false;
        }

        [HarmonyPatch(nameof(BreakerBox.Start))]
        [HarmonyPostfix]
        static void StartPatch(BreakerBox __instance)
        {
            BetterBreakerBox.breakerBoxInstance = __instance;

            if (!BetterBreakerBox.isHost)
            {
                return; //only the host should randomize the switches
            }
            //randomize the breaker box switches at the start of the game
            //for (int i = 0; i < __instance.breakerSwitches.Length; i++)
            //{
            //    bool state = UnityEngine.Random.Range(0, 2) == 0;
            //    __instance.breakerSwitches[i].SetBool("turnedLeft", value: state);
            //}
            foreach (var breakerSwitch in __instance.breakerSwitches)
            {
                bool randomState = UnityEngine.Random.Range(0, 2) == 0;
                AnimatedObjectTrigger component = breakerSwitch.gameObject.GetComponent<AnimatedObjectTrigger>();
                component.boolValue = randomState;
                component.setInitialState = randomState;
                breakerSwitch.SetBool("turnedLeft", randomState);
            }
        }
    }
}