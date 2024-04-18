using BetterBreakerBox.Configs;
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
                }
                else if (disarmTurretsTimer <= 3f)
                {
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
                }
                else if (berserkTurretsTimer <= 3f)
                {
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
                }
                else if (leaveShipTimer <= 3f)
                {
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
}
