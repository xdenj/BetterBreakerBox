using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        [HarmonyPatch(nameof(Turret.Update))]
        [HarmonyPrefix]
        static bool UpdatePatch(Turret __instance)
        {
            if (!BetterBreakerBox.ArmTurret)
            {
                return false;
            }

            if (BetterBreakerBox.BerserkTurret)
            {
                MethodInfo switchTurretModeMethod = typeof(Turret).GetMethod("SwitchTurretMode", BindingFlags.Instance | BindingFlags.NonPublic);

                if (switchTurretModeMethod != null)
                {
                    // Specify the parameters for the method
                    object[] parameters = [3]; // Setting the mode to 3

                    // Invoke the method on the instance of Turret
                    switchTurretModeMethod.Invoke(__instance, parameters);
                }
                else
                {
                    // Method not found, handle accordingly
                    BetterBreakerBox.Instance.logger.LogError("SwitchTurretMode method not found!");
                }

            }
            return true;
        }
    }
}
