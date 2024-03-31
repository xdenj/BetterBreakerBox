using HarmonyLib;
using System;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch(nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        public static void OnSessionStart(StartOfRound __instance)
        {
            if (!__instance.IsOwner) return;
            try
            {
                var BetterBreakerBoxManager = Object.Instantiate(BetterBreakerBox.BetterBreakerBoxManagerPrefab, __instance.transform);
                BetterBreakerBoxManager.hideFlags = HideFlags.None;
                BetterBreakerBoxManager.GetComponent<NetworkObject>().Spawn();
            }
            catch (Exception e)
            {
                BetterBreakerBox.logger.LogError($"Failed to spawn BetterBreakerBoxBehaviour:\n{e}");
            }

        }


        [HarmonyPatch(nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        static void UpdatePatch(StartOfRound __instance)
        {
            if (!BetterBreakerBox.StatesSet)
            {
                return;

            }

            if (BetterBreakerBox.LeaveShip)
            {
                StartOfRound.Instance.ShipLeave();
            }

            string switchesTurnedOn = "";
            for (int i = 0; i < BetterBreakerBox.SwitchStates.Length; i++)
            {
                switchesTurnedOn += BetterBreakerBox.SwitchStates[i] ? "1" : "0";
            }


            if (BetterBreakerBox.LastState != switchesTurnedOn)
            {
                if (BetterBreakerBox.GetSwitchActionMap()?.TryGetValue(switchesTurnedOn, out SwitchAction action) == true)
                {
                    action.Invoke();
                }

                BetterBreakerBox.LastState = switchesTurnedOn;
            }
        }

        [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPrefix]
        static void OnShipLandedMiscEventsPatch()
        {
            BetterBreakerBox.Instance.RandomizeActions();
        }


        [HarmonyPatch(nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPrefix]
        static void ShipHasLeftPatch()
        {
            BetterBreakerBox.ResetBetterBreakerBox();
            BetterBreakerBox.logger.LogInfo("Ship has left, resetting.");
        }
    }
}
