using UnityEngine;
using UnityEngine.InputSystem;

public class GamePlayerManager : MonoBehaviour
{
    public PlayerConfigData configData;
    public PlayerInputManager playerInputManager;
    public GameObject playerPrefab;

    private void Start()
    {
        playerInputManager = GetComponent<PlayerInputManager>();
        playerInputManager.playerPrefab = playerPrefab;

        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        for (int i = 0; i < configData.maxPlayers; i++)
        {
            var config = configData.playerConfigs[i];
            if (config == null) continue;

            PlayerInput player;
            InputDevice device = null;

            // Manejar caso especial de teclado/ratón
            if (config.controlScheme == "KeyboardAndMouse")
            {
                player = playerInputManager.JoinPlayer(
                    playerIndex: i,
                    pairWithDevices: new InputDevice[] { Keyboard.current, Mouse.current },
                    controlScheme: config.controlScheme
                );
            }
            else
            {
                device = InputSystem.GetDeviceById(int.Parse(config.deviceId));
                player = playerInputManager.JoinPlayer(
                    playerIndex: i,
                    pairWithDevice: device,
                    controlScheme: config.controlScheme
                );
            }

            SetupPlayerDisplay(player, config.displayIndex);
        }
    }

    private void SetupPlayerDisplay(PlayerInput player, int displayIndex)
    {
        if (displayIndex < Display.displays.Length)
        {
            // Activar display si es necesario
            if (!Display.displays[displayIndex].active)
            {
                Display.displays[displayIndex].Activate();
            }

            // Asignar cámara al display
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera)
            {
                playerCamera.targetDisplay = displayIndex;
            }
        }
        else
        {
            Debug.LogWarning($"Display {displayIndex} no disponible");
        }
    }
}