using BetterBreakerBox.Configs;
using DunGen;
using Unity.Netcode;
using UnityEngine;

namespace BetterBreakerBox
{
    internal class BetterBreakerBoxBehaviour : NetworkBehaviour
    {
        public static BetterBreakerBoxBehaviour? Instance { get; private set; }

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
                DisplayTimerClientRpc("Time left until Turrets are re-armed: ", disarmTurretsTimer, BetterBreakerBoxConfig.disarmTurretsTimer.Value);
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
                DisplayTimerClientRpc("Time left until Turrets exit berserk mode: ", berserkTurretsTimer, BetterBreakerBoxConfig.berserkTurretsTimer.Value);
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
                DisplayTimerClientRpc("Time left until Ship departs: ", leaveShipTimer, BetterBreakerBoxConfig.shipLeaveTimer.Value);
                BetterBreakerBox.logger.LogDebug("Leave ship timer: " + leaveShipTimer);

                leaveShipTimer -= Time.deltaTime;
                BetterBreakerBox.logger.LogDebug("Leave ship timer: " + leaveShipTimer);

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
            BetterBreakerBox.logger.LogInfo("Spawned BetterBreakerBoxBehaviour");
            base.OnNetworkSpawn();
        }

        public override void OnDestroy()
        {
            BetterBreakerBox.logger.LogInfo("Destroyed BetterBreakerBoxBehaviour");
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
