using BetterBreakerBox.Behaviours;
using BetterBreakerBox.Configs;
using HarmonyLib;
using System;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterBreakerBox.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch(nameof(RoundManager.Start))]
        [HarmonyPostfix]
        static void StartPatch(RoundManager __instance)
        {
            BetterBreakerBox.hasRandomizedActions = false;
            BetterBreakerBox.isHost = GameNetworkManager.Instance.isHostingGame;
            if (__instance.IsOwner)
            {
                if (BetterBreakerBoxManager.Instance != null)
                {
                    return;
                }
                try
                {
                    var BetterBreakerBoxManager = Object.Instantiate(BetterBreakerBox.BetterBreakerBoxManagerPrefab, __instance.transform);
                    BetterBreakerBoxManager.hideFlags = HideFlags.None;
                    BetterBreakerBoxManager.GetComponent<NetworkObject>().Spawn();
                    BetterBreakerBox.logger.LogDebug("Spawned BetterBreakerBoxManager");
                }
                catch (Exception e)
                {
                    BetterBreakerBox.logger.LogError($"Failed to spawn BetterBreakerBoxManager:\n{e}");
                }
            }
            if (!BetterBreakerBox.isHost)
            {
                BetterBreakerBox.Instance.PrepareCommand(BetterBreakerBoxManager.Instance.hintPrice.Value);
            }
        }

        [HarmonyPatch(nameof(RoundManager.Update))]
        [HarmonyPostfix]
        static void UpdatePatch(RoundManager __instance)
        {
            if (!BetterBreakerBox.isHost) return;
            if ((TimeOfDay.Instance.daysUntilDeadline == 3 && !BetterBreakerBox.hasRandomizedActions) || (BetterBreakerBoxConfig.resetAfterDay.Value && !BetterBreakerBox.hasRandomizedActions && TimeOfDay.Instance.daysUntilDeadline != 3))
            {
                BetterBreakerBoxManager.Instance.Reset();

                BetterBreakerBox.Instance.RandomizeActions();//randomize actions at beginning of first day
                BetterBreakerBox.Instance.PrepareTerminalHints();

                BetterBreakerBox.UpdateHintPrice(Math.Clamp(BetterBreakerBoxConfig.hintPrice.Value, 0, Int32.MaxValue));
                BetterBreakerBox.Instance.PrepareCommand(BetterBreakerBoxManager.Instance.hintPrice.Value);
                BetterBreakerBox.hasRandomizedActions = true;
                BetterBreakerBox.logger.LogDebug($"Randomized actions at beginning of {(BetterBreakerBoxConfig.resetAfterDay.Value ? "day" : "round")}");

            }
            else if (TimeOfDay.Instance.daysUntilDeadline <= 0)
            {
                BetterBreakerBox.hasRandomizedActions = false;
            }
        }

        [HarmonyPatch(nameof(RoundManager.SwitchPower))]
        [HarmonyPrefix]
        static bool SwitchPowerPatch(RoundManager __instance)
        {
            return false;
        }
    }
}