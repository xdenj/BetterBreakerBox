using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch(nameof(RoundManager.SwitchPower))]
        [HarmonyPrefix]
        static bool SwitchPowerPatch(RoundManager __instance)
        {
            return false;
        }
    }
}
