using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerGrab : NetworkBehaviour
{
    [Header("Références")]
    public Transform playerCamera;
    
    [Header("Paramètres du Grab")]
    public float grabRange = 5f; 
    public float minGrabDistance = 1.5f;
    public float maxGrabDistance = 4f;
    public float scrollSensitivity = 0.5f;
    public float scrollSmoothing = 10f;
    
    [Header("Paramètres de Rotation")]
    public float rotationSpeed = 0.5f; 
    
    // NOUVEAU : Une variable publique pour savoir si on tourne l'objet
    public bool IsRotating { get; private set; }
    
    private InputAction grabAction;
    private InputAction scrollAction;
    private InputAction rotateButtonAction; 
    private InputAction rotateDeltaAction;  
    
    private NetworkObject heldObject;
    private Rigidbody heldRb;
    
    private float currentHoldDistance;
    private float targetHoldDistance;
    private Quaternion rotationOffset; 

    void Start()
    {
        grabAction = new InputAction("Grab", binding: "<Keyboard>/f");
        grabAction.Enable();

        scrollAction = new InputAction("Scroll", binding: "<Mouse>/scroll/y");
        scrollAction.Enable();

        rotateButtonAction = new InputAction("RotateButton", binding: "<Mouse>/leftButton");
        rotateButtonAction.Enable();

        rotateDeltaAction = new InputAction("RotateDelta", binding: "<Mouse>/delta");
        rotateDeltaAction.Enable();
    }

    public override void OnDestroy()
    {
        grabAction?.Disable();
        scrollAction?.Disable();
        rotateButtonAction?.Disable();
        rotateDeltaAction?.Disable();
        base.OnDestroy();
    }

    void Update()
    {
        if (!IsOwner) return;

        // On réinitialise l'état à chaque frame
        IsRotating = false;

        if (grabAction.WasPressedThisFrame())
        {
            if (heldObject == null) TryGrabObject();
            else DropObject();
        }

        if (heldObject != null)
        {
            float scrollValue = scrollAction.ReadValue<float>();
            if (scrollValue != 0)
            {
                float scrollDirection = Mathf.Sign(scrollValue);
                targetHoldDistance += scrollDirection * scrollSensitivity;
                targetHoldDistance = Mathf.Clamp(targetHoldDistance, minGrabDistance, maxGrabDistance);
            }
            currentHoldDistance = Mathf.Lerp(currentHoldDistance, targetHoldDistance, Time.deltaTime * scrollSmoothing);

            if (rotateButtonAction.IsPressed())
            {
                // NOUVEAU : Le joueur maintient le clic, on passe la variable à Vrai !
                IsRotating = true; 

                Vector2 mouseDelta = rotateDeltaAction.ReadValue<Vector2>();
                
                Quaternion rotX = Quaternion.AngleAxis(-mouseDelta.y * rotationSpeed, Vector3.right);
                Quaternion rotY = Quaternion.AngleAxis(-mouseDelta.x * rotationSpeed, Vector3.up);
                
                rotationOffset = rotX * rotY * rotationOffset;
            }

            Vector3 targetPosition = playerCamera.position + playerCamera.forward * currentHoldDistance;
            heldObject.transform.position = targetPosition;
            heldObject.transform.rotation = playerCamera.rotation * rotationOffset;
        }
    }

    void TryGrabObject()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange))
        {
            NetworkObject netObj = hit.collider.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                targetHoldDistance = Vector3.Distance(playerCamera.position, hit.transform.position);
                targetHoldDistance = Mathf.Clamp(targetHoldDistance, minGrabDistance, maxGrabDistance);
                currentHoldDistance = targetHoldDistance; 
                
                rotationOffset = Quaternion.Inverse(playerCamera.rotation) * hit.transform.rotation;
                
                RequestGrabServerRpc(netObj.NetworkObjectId);
            }
        }
    }

    [ServerRpc]
    void RequestGrabServerRpc(ulong objectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            netObj.ChangeOwnership(rpcParams.Receive.SenderClientId);
            ClientRpcParams clientParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { rpcParams.Receive.SenderClientId } } };
            ConfirmGrabClientRpc(objectId, clientParams);
        }
    }

    [ClientRpc]
    void ConfirmGrabClientRpc(ulong objectId, ClientRpcParams clientParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            heldObject = netObj;
            heldRb = heldObject.GetComponent<Rigidbody>();
            heldRb.isKinematic = true; 
        }
    }

    void DropObject()
    {
        if (heldObject != null)
        {
            heldRb.isKinematic = false;
            RequestDropServerRpc(heldObject.NetworkObjectId);
            heldObject = null;
            heldRb = null;
        }
    }

    [ServerRpc]
    void RequestDropServerRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            netObj.RemoveOwnership();
        }
    }
}