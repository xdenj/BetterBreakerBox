using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
                switch (switchesTurnedOn)
                {
                    case "00001":
                        BetterBreakerBox.Instance.DisarmTurrets();
                        HUDManager.Instance.DisplayTip("Information", "Turrets disarmed!", false, false, "LC_Tip1");
                        break;

                    case "00010":
                        BetterBreakerBox.Instance.BerserkTurrets();
                        HUDManager.Instance.DisplayTip("Warning!", "Tampering detected, setting Turrets to berserk mode!", true, false, "LC_Tip1");
                        break;

                    case "00011":
                        BetterBreakerBox.Instance.ShipLeave();
                        HUDManager.Instance.DisplayTip("Warning!", "Something something, the ship is leaving", true, false, "LC_Tip1");
                        break;

                    default:
                        HUDManager.Instance.DisplayTip("Switch states updated!", switchesTurnedOn, false, false, "LC_Tip1");
                        break;
                }
                BetterBreakerBox.LastState = switchesTurnedOn;
            }
        }



        [HarmonyPatch(nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPrefix]
        static void ShipHasLeftPatch()
        {
            BetterBreakerBox.ResetBetterBreakerBox();
            BetterBreakerBox.Instance.logger.LogInfo("Ship has left, resetting.");
        }


    }
}
