using HarmonyLib;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(LungProp))]
    internal class LungPropPatch
    {
        [HarmonyPatch(nameof(LungProp.EquipItem))]
        [HarmonyPrefix]
        static void EquipItemPrefix(LungProp __instance)
        {
            if (__instance.isLungDocked)
            {
                BetterBreakerBox.isTriggeredByLungProp = true;
                BetterBreakerBox.isBreakerBoxEnabled = false;
            }
        }

        [HarmonyPatch(nameof(LungProp.DisconnectFromMachinery))]
        [HarmonyPostfix]
        static void EquipItemPostfix(LungProp __instance)
        {
            BetterBreakerBox.isTriggeredByLungProp = false;
        }


    }
}
