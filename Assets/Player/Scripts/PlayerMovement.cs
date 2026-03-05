using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Mouvement")]
    public float speed = 5f;
    
    [Header("Caméra & Vue")]
    public GameObject playerCamera;
    public float mouseSensitivity = 0.2f;

    private InputAction moveAction;
    private InputAction lookAction;
    private float xRotation = 0f;

    // NOUVEAU : Une référence vers notre script PlayerGrab
    private PlayerGrab playerGrab;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerCamera.SetActive(true);
            playerCamera.GetComponent<AudioListener>().enabled = true;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Start()
    {
        // On récupère automatiquement le script PlayerGrab qui est sur le même joueur
        playerGrab = GetComponent<PlayerGrab>();

        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        moveAction.Enable();

        lookAction = new InputAction("Look");
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.Enable();
    }

    public override void OnDestroy()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        base.OnDestroy();
    }

    void Update()
    {
        if (!IsOwner) return;

        // 1. GESTION DE LA VUE (Look)
        // NOUVEAU : On applique la rotation SEULEMENT si on n'est pas en train de tourner un objet
        if (playerGrab == null || !playerGrab.IsRotating)
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;

            xRotation -= lookInput.y;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * lookInput.x);
        }

        // 2. GESTION DU MOUVEMENT (Move)
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * speed * Time.deltaTime;
    }
}