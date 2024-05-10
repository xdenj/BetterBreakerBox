using BetterBreakerBox.Behaviours;
using BetterBreakerBox.Configs;
using HarmonyLib;


namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {

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

                if (BetterBreakerBox.LastState != BetterBreakerBox.SwitchesTurnedOn)
                {
                    //check if the state of the switches has changed since last Update()
                    if (BetterBreakerBox.GetSwitchActionMap()?.TryGetValue(BetterBreakerBox.SwitchesTurnedOn, out ActionDefinition actionDef) == true)
                    {
                        if (!BetterBreakerBox.ActionLock && BetterBreakerBox.isBreakerBoxEnabled)
                        {
                            //Set Lock flag to prevent an action from being triggered while another action is in progress
                            BetterBreakerBox.ActionLock = true;
                            // Display action messages before invoking the action
                            if (actionDef.DisplayMessage)
                            {
                                BetterBreakerBoxManager.Instance?.DisplayActionMessageClientRpc(actionDef.HeaderText, actionDef.BodyText, actionDef.IsWarning);
                            }
                            // Now, invoke the action
                            if((BetterBreakerBox.DisarmTurrets && actionDef.Action.Method.Name == "TurretsDisarm") || (BetterBreakerBox.BerserkTurrets && actionDef.Action.Method.Name == "TurretsBerserk") || (BetterBreakerBox.LeaveShip && actionDef.Action.Method.Name == "ShipLeave"))
                            {
                                //nothing
                            }
                            else
                            {
                                if (actionDef.Action.Method.Name != "DoNothing")
                                {
                                    BetterBreakerBox.breakerBoxInstance.thisAudioSource.PlayOneShot(RoundManager.Instance.PressButtonSFX1);
                                }
                                actionDef.Action.Invoke();
                                BetterBreakerBox.LocalPlayerTriggered = false; //reset the flag
                            }
                        }
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

        [HarmonyPatch(nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPrefix]
        static void ShipHasLeftPatch()
        {
            BetterBreakerBox.ResetNewDay();
            BetterBreakerBox.logger.LogInfo("Ship has left, resetting.");
            if (!BetterBreakerBox.isHost) return;
            if (BetterBreakerBoxConfig.resetAfterDay.Value)
            {
                BetterBreakerBox.hasRandomizedActions = false;
            }
        }
    }
}