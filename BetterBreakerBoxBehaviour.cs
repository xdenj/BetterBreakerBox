using Unity.Netcode;

namespace BetterBreakerBox
{
    internal class BetterBreakerBoxBehaviour : NetworkBehaviour
    {
        public static BetterBreakerBoxBehaviour? Instance { get; private set; }

        //public bool IsMeltdown
        //{
        //    get => _isMeltdown.Value;
        //    internal set => _isMeltdown.Value = value;
        //}
        //private readonly NetworkVariable<bool> _isMeltdown = new() { Value = false, };


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
