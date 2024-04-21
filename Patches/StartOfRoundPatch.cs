using BetterBreakerBox.Behaviours;
using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        //moved to RoundManagerPatch
        //[HarmonyPatch(nameof(StartOfRound.Start))]
        //[HarmonyPostfix]
        //public static void StartPatch(StartOfRound __instance)
        //{
        //    BetterBreakerBox.hasRandomizedActions = false;
        //    BetterBreakerBox.logger.LogDebug($"Time until deadine: {TimeOfDay.Instance.timeUntilDeadline}");
        //    BetterBreakerBox.boughtThisRound = false;
        //    //BetterBreakerBox.isHost = GameNetworkManager.Instance.isHostingGame;
        //    if (!__instance.IsOwner) return;
        //    try
        //    {
        //        var BetterBreakerBoxManager = Object.Instantiate(BetterBreakerBox.BetterBreakerBoxManagerPrefab, __instance.transform);
        //        BetterBreakerBoxManager.hideFlags = HideFlags.None;
        //        BetterBreakerBoxManager.GetComponent<NetworkObject>().Spawn();
        //    }
        //    catch (Exception e)
        //    {
        //        BetterBreakerBox.logger.LogError($"Failed to spawn BetterBreakerBoxManager:\n{e}");
        //    }
        //}

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
                        BetterBreakerBoxManager.Instance?.DisplayActionMessageClientRpc(actionDef.HeaderText, actionDef.BodyText, actionDef.IsWarning);
                        // Now, invoke the action
                        actionDef.Action.Invoke();
                    }
                    //keep track of current state of the switches
                    BetterBreakerBox.LastState = BetterBreakerBox.SwitchesTurnedOn;
                }
            }

        }

        [HarmonyPatch(nameof(StartOfRound.playersFiredGameOver))]
        [HarmonyPostfix]
        static void playersFiredGameOverPatch()
        {
            BetterBreakerBox.ResetNewRound(); //reset the flags for the new round after death
        }

        //[HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents))]
        //[HarmonyPrefix]
        //static void OnShipLandedMiscEventsPatch()
        //{
        //    if (BetterBreakerBox.isHost)
        //    {

        //        BetterBreakerBox.Instance.RandomizeActions();
        //    }
        //}

        [HarmonyPatch(nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPrefix]
        static void ShipHasLeftPatch()
        {
            BetterBreakerBox.ResetNewDay();
            BetterBreakerBox.logger.LogInfo("Ship has left, resetting.");
        }
    }
}