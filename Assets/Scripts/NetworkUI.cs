using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    // Fonction appelée par le bouton "Héberger"
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        gameObject.SetActive(false); // Cache l'interface une fois connecté
    }

    // Fonction appelée par le bouton "Rejoindre"
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false); // Cache l'interface une fois connecté
    }
}