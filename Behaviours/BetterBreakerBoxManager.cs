using BetterBreakerBox.Configs;
using GameNetcodeStuff;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace BetterBreakerBox.Behaviours
{
    internal class BetterBreakerBoxManager : NetworkBehaviour
    {
        public static BetterBreakerBoxManager? Instance { get; private set; }

        internal float leaveShipTimer = Math.Clamp(BetterBreakerBoxConfig.shipLeaveTimer.Value, 0f, float.MaxValue);
        internal float disarmTurretsTimer = Math.Clamp(BetterBreakerBoxConfig.disarmTurretsTimer.Value, 0f, float.MaxValue);
        internal float berserkTurretsTimer = Math.Clamp(BetterBreakerBoxConfig.berserkTurretsTimer.Value, 0f, float.MaxValue);
        internal bool leaveShipTimerStarted = false;
        internal bool disarmTurretsTimerStarted = false;
        internal bool berserkTurretsTimerStarted = false;
        internal float startTime = 0f;
        internal TimeOfDay timeOfDay;

        public NetworkVariable<int> hintPrice = new NetworkVariable<int>(50);

        public NetworkVariable<int> terminalOutputIndex = new NetworkVariable<int>(0);
        public NetworkVariable<bool> hasBoughtThisPeriod = new NetworkVariable<bool>(false);

        public NetworkVariable<IntArrayWrapper> actions = new NetworkVariable<IntArrayWrapper>(new IntArrayWrapper());
        public NetworkVariable<IntArrayWrapper> combos = new NetworkVariable<IntArrayWrapper>(new IntArrayWrapper());


        public void Reset()
        {
            SetHintPrice(50);
            SetTerminalOutputIndex(0);
            SethasBoughtThisPeriod(false);
        }

        public void SetAction(int index, int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetActionServerRpc(index, value);
                return;
            }
            actions.Value.Data[index] = value;
        }

        public void InitializeActions(int length)
        {
            actions.Value.Data = new int[length];
        }

        public void SetCombo(int index, int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetComboServerRpc(index, value);
                return;
            }
            combos.Value.Data[index] = value;
        }

        public void InitializeCombos(int length)
        {
            combos.Value.Data = new int[length];
        }

        public void SetHintPrice(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetHintPriceServerRpc(value);
                return;
            }
            hintPrice.Value = value;
        }

        public void SetTerminalOutputIndex(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetTerminalOutputIndexServerRpc(value);
                return;
            }
            terminalOutputIndex.Value = value;
        }
        public void SethasBoughtThisPeriod(bool value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SethasBoughtThisPeriodServerRpc(value);
                return;
            }
            hasBoughtThisPeriod.Value = value;
        }

        public void SyncGroupCredits(int credits)
        {
            Terminal terminal = FindObjectOfType<Terminal>();
            if (BetterBreakerBox.isHost)
            {
                terminal.SyncGroupCreditsClientRpc(credits, terminal.numberOfItemsInDropship);
            }
            else
            {
                MySyncGroupCreditsServerRpc(credits);
            }
        }


        [ClientRpc]
        public void DisplayActionMessageClientRpc(string headerText, string bodyText, bool isWarning)
        {
            BetterBreakerBox.DisplayActionMessage(headerText, bodyText, isWarning);
        }

        [ClientRpc]
        public void DisplayTimerClientRpc(string name, float timeLeft, float totalTime)
        {
            BetterBreakerBox.DisplayTimer(name, timeLeft, totalTime);
        }

        [ClientRpc]
        public void CreateTimerObjectClientRpc()
        {
            BetterBreakerBox.CreateTimerObject();
        }

        [ClientRpc]
        public void DestroyTimerObjectClientRpc()
        {
            BetterBreakerBox.DestroyTimerObject();
        }

        [ClientRpc]
        public void ZapClientRpc()
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (BetterBreakerBox.LocalPlayerTriggered)
            {
                ZapRemotePlayerServerRpc((int)localPlayer.playerClientId);
            }
        }

        [ClientRpc]
        public void ZapDamageClientRpc(int playerID, int damage)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (playerID != (int)localPlayer.playerClientId) return; //only zap the affected player
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            localPlayer.DamagePlayer(damageNumber: damage, causeOfDeath: CauseOfDeath.Electrocution);
            localPlayer.beamUpParticle.Play();
            var charger = FindObjectOfType<ItemCharger>();
            localPlayer.statusEffectAudio.PlayOneShot(charger.zapAudio.clip);
        }

        [ClientRpc]
        public void ZapEffectsClientRpc(int playerID)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            var zappedPlayer = RoundManager.Instance.playersManager.allPlayerScripts.Where(p => (int)p.playerClientId == playerID ).FirstOrDefault();
            if (zappedPlayer != null)
            {
                if (zappedPlayer.playerClientId == localPlayer.playerClientId) return;
                zappedPlayer.beamUpParticle.Play();
                zappedPlayer.statusEffectAudio.PlayOneShot(FindObjectOfType<ItemCharger>().zapAudio.clip);
            }
        }

        [ClientRpc]
        public void ChargeEnableClientRpc()
        {
            if (BetterBreakerBox.breakerBoxInstance.GetComponent<ChargingManager>() == null)
            {
                BetterBreakerBox.breakerBoxInstance.gameObject.AddComponent<ChargingManager>();
                PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
                var unlockables = StartOfRound.Instance.unlockablesList.unlockables;
                var teleporterPrefab = unlockables.Find(u => u.unlockableName == "Teleporter").prefabObject;
                var teleporter = teleporterPrefab.GetComponent<ShipTeleporter>();
                localPlayer.statusEffectAudio.PlayOneShot(teleporter.buttonPressSFX);
            }
            else
            {
#if DEBUG
                Instance.DisplayActionMessageClientRpc("Information", "Charging is already enabled.", false);
#endif
            }
        }

        [ClientRpc]
        public void ChargeDisableClientRpc()
        {
            if (BetterBreakerBox.breakerBoxInstance.GetComponent<ChargingManager>() != null)
            {
                Destroy(BetterBreakerBox.breakerBoxInstance.GetComponent<ChargingManager>());
            }
        }

        [ClientRpc]
        public void PrepareCommandClientRpc()
        {
            BetterBreakerBox.Instance.PrepareCommand(hintPrice.Value);
        }

        [ClientRpc]
        public void ToggleBreakerBoxHumClientRpc(bool on)
        {
            BetterBreakerBox.ToggleBreakerBoxHum(on);
        }

        // RPC to increment the terminalOutputIndex
        [ServerRpc(RequireOwnership = false)]
        public void SetTerminalOutputIndexServerRpc(int value)
        {
            terminalOutputIndex.Value = value;
        }

        // RPC to set hasBoughtThisPeriod
        [ServerRpc(RequireOwnership = false)]
        public void SethasBoughtThisPeriodServerRpc(bool status)
        {
            hasBoughtThisPeriod.Value = status;
        }

        [ServerRpc(RequireOwnership = false)]
        public void MySyncGroupCreditsServerRpc(int credits)
        {
            Terminal terminal = FindObjectOfType<Terminal>();
            terminal.SyncGroupCreditsClientRpc(credits, terminal.numberOfItemsInDropship);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetHintPriceServerRpc(int value)
        {
            hintPrice.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetActionServerRpc(int index, int value)
        {
            actions.Value.Data[index] = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetComboServerRpc(int index, int value)
        {
            combos.Value.Data[index] = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PrepareCommandServerRpc()
        {
            PrepareCommandClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ZapRemotePlayerServerRpc(int playerID)
        {
            ZapEffectsClientRpc(playerID);
            ZapDamageClientRpc(playerID, Math.Clamp(BetterBreakerBoxConfig.zapDamage.Value, 0, int.MaxValue));
            if (BetterBreakerBoxConfig.enableChainZap.Value)
            {
                var zappedPlayerPosition = RoundManager.Instance.playersManager.allPlayerScripts.Where(p => (int)p.playerClientId == playerID).FirstOrDefault().transform.position;
                var playersInZapRange = RoundManager.Instance.playersManager.allPlayerScripts.Where(p => (int)p.playerClientId != playerID && Vector3.Distance(p.transform.position, zappedPlayerPosition) < 5f);
                foreach (var playerInZapRange in playersInZapRange)
                {
                    BetterBreakerBox.logger.LogInfo($"Chain zapping player {playerInZapRange.playerClientId} in range of player {playerID}");
                    ZapEffectsClientRpc((int)playerInZapRange.playerClientId);
                    ZapDamageClientRpc((int)playerInZapRange.playerClientId, Math.Clamp(BetterBreakerBoxConfig.zapDamage.Value, 0, int.MaxValue));
                }
            }
        }

        void Update()
        {
            if (!BetterBreakerBox.isHost) return; //only the host should be able to trigger actions
            if (!BetterBreakerBox.isBreakerBoxEnabled) return; //don't do anything if the breaker box is disabled
            if (!BetterBreakerBox.LeaveShip && !BetterBreakerBox.DisarmTurrets && !BetterBreakerBox.BerserkTurrets) return; //no action to trigger
            if (timeOfDay == null)
            {
                timeOfDay = TimeOfDay.Instance;
            }
            float remainingTime = timeOfDay.totalTime - timeOfDay.currentDayTime;
            if (BetterBreakerBox.DisarmTurrets)
            {
                if (!disarmTurretsTimerStarted)
                {
                    disarmTurretsTimer = disarmTurretsTimer > remainingTime ? remainingTime : disarmTurretsTimer;
                    disarmTurretsTimerStarted = true;
                    startTime = disarmTurretsTimer;
                    BetterBreakerBox.ActionLock = false;
                }
#if DEBUG
                DisplayTimerClientRpc("Turrets re-arming in: ", disarmTurretsTimer, startTime);
#endif
                disarmTurretsTimer -= (Time.deltaTime * timeOfDay.globalTimeSpeedMultiplier);
                if (disarmTurretsTimer <= 0f)
                {
                    //Resetting flags and timer after the timer has expired
                    disarmTurretsTimerStarted = false;
                    BetterBreakerBox.DisarmTurrets = false;
                    disarmTurretsTimer = BetterBreakerBoxConfig.disarmTurretsTimer.Value;
#if DEBUG
                    DestroyTimerObjectClientRpc();
                    DisplayActionMessageClientRpc("<color=green>Power restored!</color>", "Turrets back online and operational.", false);
#endif
                }
                return;
            }

            if (BetterBreakerBox.BerserkTurrets)
            {
                if (!berserkTurretsTimerStarted)
                {
                    berserkTurretsTimer = berserkTurretsTimer > remainingTime ? remainingTime : berserkTurretsTimer;
                    startTime = berserkTurretsTimer;
                    berserkTurretsTimerStarted = true;
                    BetterBreakerBox.ActionLock = false;
                }
#if DEBUG
                DisplayTimerClientRpc("Turrets exiting Berserk mode in: ", berserkTurretsTimer, startTime);
#endif
                berserkTurretsTimer -= (Time.deltaTime * timeOfDay.globalTimeSpeedMultiplier);
                if (berserkTurretsTimer <= 0f)
                {
                    berserkTurretsTimerStarted = false;
                    BetterBreakerBox.BerserkTurrets = false;

                    berserkTurretsTimer = BetterBreakerBoxConfig.berserkTurretsTimer.Value;
#if DEBUG
                    DestroyTimerObjectClientRpc();
                    DisplayActionMessageClientRpc("Information", "Threat neutralized, Turrets returning to regular operation.", false);
#endif
                }
                return;
            }

            if (BetterBreakerBox.LeaveShip)
            {
                if (!leaveShipTimerStarted)
                {
                    leaveShipTimer = leaveShipTimer > remainingTime ? remainingTime : leaveShipTimer;
                    leaveShipTimerStarted = true;
                    startTime = leaveShipTimer;
                    BetterBreakerBox.ActionLock = false;
                }
                DisplayTimerClientRpc("Ship departs in: ", leaveShipTimer, startTime);
                leaveShipTimer -= (Time.deltaTime * timeOfDay.globalTimeSpeedMultiplier);
                if (leaveShipTimer <= 0f)  // Move this condition up to catch when the timer first goes zero or negative
                {
                    leaveShipTimerStarted = false;
                    BetterBreakerBox.LeaveShip = false;
                    leaveShipTimer = BetterBreakerBoxConfig.shipLeaveTimer.Value;
                    DisplayActionMessageClientRpc("Emergency evacuation!", "The Company has deemed this operation too dangerous. Autopilot Ship is departing ahead of schedule!", true);
                    StartOfRound.Instance.ShipLeave();
                    DestroyTimerObjectClientRpc();
                }
                return;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            BetterBreakerBox.logger.LogInfo("Spawned BetterBreakerBoxManager");
            base.OnNetworkSpawn();
        }

        public override void OnDestroy()
        {
            BetterBreakerBox.logger.LogInfo("Destroyed BetterBreakerBoxManager");
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
            base.OnDestroy();
        }
    }


    [Serializable]
    public class IntArrayWrapper : INetworkSerializable
    {
        public int[] Data;

        public IntArrayWrapper()
        {
            Data = new int[0]; // Initialize with empty array to avoid null issues.
        }

        public IntArrayWrapper(int[] data)
        {
            Data = data;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // First serialize the length of the array
            int length = Data.Length;
            serializer.SerializeValue(ref length);

            // Resize the array when deserializing
            if (serializer.IsReader)
            {
                Data = new int[length];
            }

            // Serialize each element of the array
            for (int i = 0; i < length; i++)
            {
                int element = Data[i];
                serializer.SerializeValue(ref element);
                if (serializer.IsReader)
                {
                    Data[i] = element;
                }
            }
        }
    }
}