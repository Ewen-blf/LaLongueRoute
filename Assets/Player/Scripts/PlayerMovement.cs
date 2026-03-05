using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Mouvement")]
    public float speed = 5f;
    
    [Header("Caméra & Vue")]
    public GameObject playerCamera;
    public float mouseSensitivity = 0.2f; // Sensibilité de la souris

    private InputAction moveAction;
    private InputAction lookAction;
    private float xRotation = 0f; // Pour bloquer la vue en haut/bas

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerCamera.SetActive(true);
            playerCamera.GetComponent<AudioListener>().enabled = true;
            
            // On cache et on bloque le curseur de la souris au centre de l'écran
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Start()
    {
        // --- Configuration du Mouvement ---
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        moveAction.Enable();

        // --- Configuration de la Vue (Souris) ---
        lookAction = new InputAction("Look");
        lookAction.AddBinding("<Mouse>/delta"); // Lit le déplacement physique de la souris
        lookAction.Enable();
    }

    void OnDestroy()
    {
        moveAction?.Disable();
        lookAction?.Disable();
    }

    void Update()
    {
        if (!IsOwner) return;

        // 1. GESTION DE LA VUE (Look)
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        // Rotation Haut/Bas (sur la Caméra uniquement)
        xRotation -= lookInput.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Bloque à 90 degrés pour ne pas faire un tour complet
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotation Gauche/Droite (sur tout le Corps)
        transform.Rotate(Vector3.up * lookInput.x);


        // 2. GESTION DU MOUVEMENT (Move)
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        
        // On calcule la direction en fonction de là où le joueur regarde !
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * speed * Time.deltaTime;
    }
}