using BetterBreakerBox.Configs;
using GameNetcodeStuff;
using System;
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

        public NetworkVariable<int> hintPrice = new NetworkVariable<int>(50);

        public NetworkVariable<int> comboOne = new NetworkVariable<int>(-1);
        public NetworkVariable<int> comboTwo = new NetworkVariable<int>(-1);
        public NetworkVariable<int> comboThree = new NetworkVariable<int>(-1);

        public NetworkVariable<int> actionOne = new NetworkVariable<int>(-1);
        public NetworkVariable<int> actionTwo = new NetworkVariable<int>(-1);
        public NetworkVariable<int> actionThree = new NetworkVariable<int>(-1);

        public NetworkVariable<int> terminalOutputIndex = new NetworkVariable<int>(0);
        public NetworkVariable<bool> hasBoughtThisRound = new NetworkVariable<bool>(false);


        public void Reset()
        {
            SetHintPrice(50);
            SetComboOne(-1);
            SetComboTwo(-1);
            SetComboThree(-1);
            SetActionOne(-1);
            SetActionTwo(-1);
            SetActionThree(-1);
            SetTerminalOutputIndex(0);
            SetHasBoughtThisRound(false);
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
        public void SetComboOne(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetComboOneServerRpc(value);
                return;
            }
            comboOne.Value = value;
        }

        public void SetComboTwo(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetComboTwoServerRpc(value);
                return;
            }
            comboTwo.Value = value;
        }

        public void SetComboThree(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetComboThreeServerRpc(value);
                return;
            }
            comboThree.Value = value;
        }

        public void SetActionOne(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetActionOneServerRpc(value);
                return;
            }
            actionOne.Value = value;
        }

        public void SetActionTwo(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetActionTwoServerRpc(value);
                return;
            }
            actionTwo.Value = value;
        }

        public void SetActionThree(int value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetActionThreeServerRpc(value);
                return;
            }
            actionThree.Value = value;
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
        public void SetHasBoughtThisRound(bool value)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetHasBoughtThisRoundServerRpc(value);
                return;
            }
            hasBoughtThisRound.Value = value;
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
            if (!BetterBreakerBox.LocalPlayerTriggered) return;
            BetterBreakerBox.logger.LogDebug("Zapping local player");
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            localPlayer.DamagePlayer(10, false);
            localPlayer.beamUpParticle.Play();
            var charger = FindObjectOfType<ItemCharger>();
            localPlayer.statusEffectAudio.PlayOneShot(charger.zapAudio.clip);
        }

        // RPC to increment the terminalOutputIndex
        [ServerRpc(RequireOwnership = false)]
        public void SetTerminalOutputIndexServerRpc(int value)
        {
            terminalOutputIndex.Value = value;
        }

        // RPC to set hasBoughtThisRound
        [ServerRpc(RequireOwnership = false)]
        public void SetHasBoughtThisRoundServerRpc(bool status)
        {
            hasBoughtThisRound.Value = status;
        }

        [ServerRpc(RequireOwnership = false)]
        public void MySyncGroupCreditsServerRpc(int credits)
        {
            Terminal terminal = FindObjectOfType<Terminal>();
            terminal.SyncGroupCreditsClientRpc(credits, terminal.numberOfItemsInDropship);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetComboOneServerRpc(int value)
        {
            comboOne.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetComboTwoServerRpc(int value)
        {
            comboTwo.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetComboThreeServerRpc(int value)
        {
            comboThree.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetActionOneServerRpc(int value)
        {
            actionOne.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetActionTwoServerRpc(int value)
        {
            actionTwo.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetActionThreeServerRpc(int value)
        {
            actionThree.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetHintPriceServerRpc(int value)
        {
            hintPrice.Value = value;
        }



        void Update()
        {
            if (!BetterBreakerBox.isHost) return; //only the host should be able to trigger actions
            if (!BetterBreakerBox.LeaveShip && !BetterBreakerBox.DisarmTurrets && !BetterBreakerBox.BerserkTurrets) return; //no action to trigger

            if (BetterBreakerBox.DisarmTurrets)
            {
                DisplayTimerClientRpc("Turrets re-arming in: ", disarmTurretsTimer, BetterBreakerBoxConfig.disarmTurretsTimer.Value);
                disarmTurretsTimer -= Time.deltaTime; //decrement timer
                if (disarmTurretsTimer <= 0f)
                {
                    //Resetting flags and timer after the timer has expired
                    BetterBreakerBox.DisarmTurrets = false;
                    BetterBreakerBox.ActionLock = false;
                    disarmTurretsTimer = BetterBreakerBoxConfig.disarmTurretsTimer.Value;
                    DestroyTimerObjectClientRpc();
                    DisplayActionMessageClientRpc("<color=green>Power restored!</color>", "Turrets back online and operational.", false);
                }
                return;
            }

            if (BetterBreakerBox.BerserkTurrets)
            {
                DisplayTimerClientRpc("Turrets exiting Berserk mode in: ", berserkTurretsTimer, BetterBreakerBoxConfig.berserkTurretsTimer.Value);
                berserkTurretsTimer -= Time.deltaTime;
                if (berserkTurretsTimer <= 0f)
                {
                    BetterBreakerBox.BerserkTurrets = false;
                    BetterBreakerBox.ActionLock = false;
                    berserkTurretsTimer = BetterBreakerBoxConfig.berserkTurretsTimer.Value;
                    DestroyTimerObjectClientRpc();
                    DisplayActionMessageClientRpc("Information", "Threat neutralized, Turrets returning to regular operation.", false);
                }
                return;
            }

            if (BetterBreakerBox.LeaveShip)
            {
                DisplayTimerClientRpc("Ship departs in: ", leaveShipTimer, BetterBreakerBoxConfig.shipLeaveTimer.Value);
                leaveShipTimer -= Time.deltaTime;
                if (leaveShipTimer <= 0f)  // Move this condition up to catch when the timer first goes zero or negative
                {
                    BetterBreakerBox.LeaveShip = false;
                    BetterBreakerBox.ActionLock = false;
                    leaveShipTimer = BetterBreakerBoxConfig.shipLeaveTimer.Value;
                    StartOfRound.Instance.ShipLeave();
                    DestroyTimerObjectClientRpc();
                    DisplayActionMessageClientRpc("Emergency evacuation!", "The Company has deemed this operation too dangerous. Autopilot Ship is departing ahead of schedule!", true);
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
    public class StringArrayWrapper : INetworkSerializable
    {
        public string[] Data;

        public StringArrayWrapper()
        {
            Data = new string[0]; // Initialize with empty array to avoid null issues.
        }

        public StringArrayWrapper(string[] data)
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
                Data = new string[length];
            }

            // Serialize each element of the array
            for (int i = 0; i < length; i++)
            {
                string element = Data[i];
                serializer.SerializeValue(ref element);
                if (serializer.IsReader)
                {
                    Data[i] = element;
                }
            }
        }
    }


    [Serializable]
    public class StringWrapper : INetworkSerializable
    {
        public string Data;

        public StringWrapper()
        {
            Data = ""; // Initialize with an empty string to avoid null issues.
        }

        public StringWrapper(string data)
        {
            Data = data;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Serialize the string Data
            serializer.SerializeValue(ref Data);
        }
    }
}