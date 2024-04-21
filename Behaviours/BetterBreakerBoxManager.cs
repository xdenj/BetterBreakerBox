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

        internal float leaveShipTimer = BetterBreakerBoxConfig.shipLeaveTimer.Value;
        internal float disarmTurretsTimer = BetterBreakerBoxConfig.disarmTurretsTimer.Value;
        internal float berserkTurretsTimer = BetterBreakerBoxConfig.berserkTurretsTimer.Value;

        public NetworkVariable<StringArrayWrapper> terminalOutputArray = new NetworkVariable<StringArrayWrapper>(new StringArrayWrapper());
        public NetworkVariable<int> terminalOutputIndex = new NetworkVariable<int>();
        public NetworkVariable<StringWrapper> terminalOutputString = new NetworkVariable<StringWrapper>(new StringWrapper(""));

        public NetworkVariable<bool> hasBoughtThisRound = new NetworkVariable<bool>();

        public void SetTerminalOutputArray(string[] output)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetTerminalOutputArrayServerRpc(new StringArrayWrapper(output));
                return;
            }
            terminalOutputArray.Value = new StringArrayWrapper(output);
        }

        public string[] GetTerminalOutput()
        {
            return terminalOutputArray.Value.Data;
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

        public void SetTerminalOutputString(string text)
        {
            if (!BetterBreakerBox.isHost)
            {
                SetTerminalOutputStringServerRpc(text);
                return;
            }
            terminalOutputString.Value.Data = text;
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
            localPlayer.DamagePlayer(10, false);
            FindObjectOfType<ItemCharger>().zapAudio.Play();
        }

        // RPC to update terminalOutput
        [ServerRpc(RequireOwnership = false)]
        public void SetTerminalOutputArrayServerRpc(StringArrayWrapper newOutput)
        {
            terminalOutputArray.Value = newOutput;
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
        public void SetTerminalOutputStringServerRpc(string text)
        {
            terminalOutputString.Value.Data = text;
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