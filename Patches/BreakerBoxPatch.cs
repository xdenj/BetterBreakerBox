using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(BreakerBox))]
    internal class BreakerBoxPatch
    {
        internal static GameObject stickyNotePrefab;
        internal static GameObject stickyNoteInstance;

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
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;

            for (int i = 0; i < __instance.breakerSwitches.Length; i++)
            {
                bool state = __instance.breakerSwitches[i].GetBool("turnedLeft");
                // "turnedleft" is the state of the breaker switch. turnedLeft = false means switch is turned off, turnedleft = true means switch is turned on

                InteractTrigger localPlayerInteractedWith = localPlayer.hoveringOverTrigger;
                InteractTrigger switchInteractedWith = __instance.breakerSwitches[i].gameObject.GetComponent<InteractTrigger>();

                if ((localPlayerInteractedWith == switchInteractedWith) && (BetterBreakerBox.SwitchStates[i] != state)) //if state has changed AND trigger was local player
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
            stickyNotePrefab = BetterBreakerBox.BetterBreakerBoxAssets.LoadAsset<GameObject>("assets/prefabinstance/stickynoteitem.prefab");
            Transform powerBoxDoorTransform = __instance.transform.Find("Mesh/PowerBoxDoor");
            try
            {
                GameObject stickyNoteInstance = GameObject.Instantiate(stickyNotePrefab, powerBoxDoorTransform);
                stickyNoteInstance.transform.position -= stickyNoteInstance.transform.right * 0.25f;
                stickyNoteInstance.transform.position -= stickyNoteInstance.transform.up * 0.25f;
                stickyNoteInstance.transform.position -= stickyNoteInstance.transform.forward * 0.025f;
                stickyNoteInstance.transform.Rotate(0, 180, 0);
            }

            catch (System.Exception e)
            {
                BetterBreakerBox.logger.LogError($"Failed to spawn sticky note:\n{e}");
            }
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