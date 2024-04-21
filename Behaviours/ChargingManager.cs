using GameNetcodeStuff;
using UnityEngine;

namespace BetterBreakerBox.Behaviours
{
    internal class ChargingManager : MonoBehaviour
    {
        InteractTrigger interactComponent;
        void Start()
        {
            GameObject charger = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), transform.position, Quaternion.Euler(Vector3.zero), transform);
            charger.transform.localScale = new Vector3(1f, 1f, 1f);
            charger.tag = nameof(InteractTrigger);
            charger.layer = LayerMask.NameToLayer("Props");
            charger.name = "Charger";
            charger.GetComponent<BoxCollider>().isTrigger = true;
            SetupInteractTrigger(ref charger);
        }

        void SetupInteractTrigger(ref GameObject charger)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            InteractTrigger interactTrigger = charger.AddComponent<InteractTrigger>();
            interactTrigger.interactable = true;
            interactTrigger.holdInteraction = true;
            interactTrigger.timeToHold = 2f;
            interactTrigger.holdingInteractEvent = new InteractEventFloat();
            interactTrigger.onInteractEarly = new InteractEvent();
            interactTrigger.onStopInteract = new InteractEvent();
            interactTrigger.onInteract = new InteractEvent();
            interactTrigger.onInteract.AddListener(OnChargeInteract);
            interactTrigger.hoverIcon = localPlayer.grabItemIcon;
            interactTrigger.disabledHoverIcon = localPlayer.grabItemIcon;
            interactComponent = interactTrigger;
        }
        void Update()
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (localPlayer == null || !localPlayer.isHoldingObject)
            {
                SetInteractable(false, "Not holding an item to charge.");
                return;
            }

            GrabbableObject heldObject = localPlayer.currentlyHeldObjectServer;
            SetInteractable(heldObject.itemProperties.requiresBattery, heldObject.itemProperties.requiresBattery ? "Charge item: [LMB]" : "Requires battery-powered item.");
        }
        void SetInteractable(bool interact, string text)
        {
            interactComponent.interactable = interact;
            if (interact) interactComponent.hoverTip = text;
            else interactComponent.disabledHoverTip = text;
        }
        void OnChargeInteract(PlayerControllerB interactingPlayer)
        {
            GrabbableObject heldObject = interactingPlayer.currentlyHeldObjectServer;
            heldObject.insertedBattery.charge = Mathf.Clamp(heldObject.insertedBattery.charge + (50f / 100f), 0f, 1f);
        }
    }
}