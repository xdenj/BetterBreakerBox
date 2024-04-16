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
        public static void StartPatch(StartOfRound __instance)
        {
            BetterBreakerBox.isHost = GameNetworkManager.Instance.isHostingGame;
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
                //don't do anything if switches' states haven been read once yet
                return;
            }

            if (BetterBreakerBox.isHost)
            {
                //check if the action lock is set
                if (BetterBreakerBox.ActionLock) return;
                if (BetterBreakerBox.LastState != BetterBreakerBox.SwitchesTurnedOn)
                {
                    //check if the state of the switches has changed since last Update()
                    if (BetterBreakerBox.GetSwitchActionMap()?.TryGetValue(BetterBreakerBox.SwitchesTurnedOn, out ActionDefinition actionDef) == true)
                    {
                        //Set Lock flag to prevent an action from being triggered while another action is in progress
                        BetterBreakerBox.ActionLock = true;
                        // Display action messages before invoking the action
                        BetterBreakerBoxBehaviour.Instance?.DisplayActionMessageClientRpc(actionDef.HeaderText, actionDef.BodyText, actionDef.IsWarning);
                        // Now, invoke the action
                        actionDef.Action.Invoke();
                    }
                    //keep track of current state of the switches
                    BetterBreakerBox.LastState = BetterBreakerBox.SwitchesTurnedOn;
                }
            }

        }

        [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPrefix]
        static void OnShipLandedMiscEventsPatch()
        {
            if (BetterBreakerBox.isHost)
            {
                
                BetterBreakerBox.Instance.RandomizeActions();
            }
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
