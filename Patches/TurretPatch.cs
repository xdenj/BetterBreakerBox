using HarmonyLib;
using System.Reflection;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        private static MethodInfo switchTurretModeMethod = null;
        private static readonly object[] berserkParameter = new object[] { 3 };

        [HarmonyPatch(nameof(Turret.Update))]
        [HarmonyPrefix]
        static bool UpdatePatch(Turret __instance)
        {
            // If the turret shouldn't be armed, prevent the Update method from executing.
            if (BetterBreakerBox.DisarmTurrets)
            {
                return false;
            }
            // Attempt to retrieve the method only if switchTurretModeMethod is null; otherwise, reuse the existing MethodInfo.
            switchTurretModeMethod ??= typeof(Turret).GetMethod("SwitchTurretMode", BindingFlags.Instance | BindingFlags.NonPublic);

            // If BerserkTurret flag is set, try to invoke the SwitchTurretMode method with a parameter of 3.
            if (BetterBreakerBox.BerserkTurrets)
            {
                if (switchTurretModeMethod != null)
                {
                    // Invoke the method on the instance of Turret.
                    switchTurretModeMethod.Invoke(__instance, berserkParameter);
                }
                else
                {
                    // If the method is not found, log an error.
                    BetterBreakerBox.logger.LogWarning("SwitchTurretMode() method not found! Not setting berserk mode!");
                }
            }
            // Always allow the original Update method to execute after applying our custom logic.
            return true;
        }
    }
}