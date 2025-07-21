using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DeviceAssignmentManager : MonoBehaviour
{
    [Header("References")]
    public PlayerConfigData configData;
    public InputActionAsset inputActions;
    public bool enableDisplayAssignment = true;

    [Header("UI References")]
    public TMP_Text promptText;
    public TMP_Text displayText;

    [Header("Input Settings")]
    public InputActionReference navigationAction;
    public InputActionReference confirmAction;
    public InputActionReference cancelAction;
    public float axisThreshold = 0.5f;

    private InputAction joinAction;
    private int currentPlayerIndex;
    private bool isAssignmentComplete;
    private List<InputDevice> assignedDevices = new List<InputDevice>();
    private Dictionary<InputDevice, string> deviceSchemeMap = new Dictionary<InputDevice, string>();

    // Variables para asignación de displays
    private int currentDisplayIndex = 0;
    private bool isAssigningDisplay = false;
    private bool isFinalConfirmation = false;
    private float lastAxisValue = 0f;
    private bool axisInUse = false;

    private void Awake()
    {
        configData.playerConfigs = new PlayerConfigData.PlayerConfig[configData.maxPlayers];

        joinAction = new InputAction(binding: "/*/<button>");
        joinAction.Enable();

        PrecalculateDeviceSchemes();
    }

    private void PrecalculateDeviceSchemes()
    {
        foreach (var device in InputSystem.devices)
        {
            foreach (var scheme in inputActions.controlSchemes)
            {
                if (scheme.SupportsDevice(device))
                {
                    deviceSchemeMap[device] = scheme.name;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        StartAssignment();
    }

    private void OnEnable()
    {
        navigationAction.action.Enable();
        confirmAction.action.Enable();
        cancelAction.action.Enable();
    }

    private void OnDisable()
    {
        navigationAction.action.Disable();
        confirmAction.action.Disable();
        cancelAction.action.Disable();
    }

    public void StartAssignment()
    {
        currentPlayerIndex = 0;
        isAssignmentComplete = false;
        isAssigningDisplay = false;
        isFinalConfirmation = false;
        assignedDevices.Clear();
        promptText.gameObject.SetActive(true);
        displayText.gameObject.SetActive(false);
        UpdatePrompt();
    }

    private void Update()
    {
        if (isAssignmentComplete) return;

        if (isFinalConfirmation)
        {
            HandleFinalConfirmation();
        }
        else if (isAssigningDisplay)
        {
            HandleDisplaySelection();
        }
        else
        {
            HandleDeviceAssignment();
        }
    }

    private void HandleDeviceAssignment()
    {
        if (joinAction.triggered && joinAction.activeControl != null)
        {
            InputDevice device = joinAction.activeControl.device;
            TryAssignDevice(device);
        }
    }

    private void HandleDisplaySelection()
    {
        InputDevice currentDevice = GetPlayerDevice(currentPlayerIndex);
        if (currentDevice == null) return;

        // Leer el valor COMPUESTO del action de navegación
        float axisValue = navigationAction.action.ReadValue<float>();

        // Filtrar solo si el dispositivo activo es el del jugador actual
        bool isCurrentDevice = false;
        if (navigationAction.action.activeControl != null)
        {
            isCurrentDevice = (navigationAction.action.activeControl.device == currentDevice);
        }

        // Manejar navegación solo si el dispositivo actual es el activo
        if (isCurrentDevice && Mathf.Abs(axisValue) > axisThreshold)
        {
            if (!axisInUse)
            {
                axisInUse = true;
                // Usar el valor real del eje (negativo/positivo)
                currentDisplayIndex += (axisValue > 0) ? 1 : -1;
                currentDisplayIndex = Mathf.Clamp(currentDisplayIndex, 0, 7);
                UpdateDisplayText();
            }
        }
        else if (Mathf.Abs(axisValue) < axisThreshold)
        {
            axisInUse = false;
        }

        // Confirmación (se mantiene igual)
        if (confirmAction.action.triggered && confirmAction.action.activeControl != null)
        {
            if (confirmAction.action.activeControl.device == currentDevice)
            {
                configData.playerConfigs[currentPlayerIndex].displayIndex = currentDisplayIndex;
                NextDisplayAssignment();
            }
        }
    }


    private void HandleFinalConfirmation()
    {
        InputDevice player1Device = GetPlayerDevice(0);
        if (player1Device == null) return;

        // Validar dispositivo EXACTO para confirmación
        if (confirmAction.action.triggered && confirmAction.action.activeControl != null)
        {
            if (confirmAction.action.activeControl.device == player1Device)
            {
                SaveConfiguration();
                SceneManager.LoadScene("Players");
            }
        }

        // Validar dispositivo EXACTO para cancelación
        if (cancelAction.action.triggered && cancelAction.action.activeControl != null)
        {
            if (cancelAction.action.activeControl.device == player1Device)
            {
                StartAssignment();
            }
        }
    }

    // Obtener dispositivo de un jugador específico por índice
    private InputDevice GetPlayerDevice(int playerIndex)
    {
        if (playerIndex >= configData.playerConfigs.Length ||
            playerIndex < 0 ||
            configData.playerConfigs[playerIndex] == null)
        {
            return null;
        }

        string deviceId = configData.playerConfigs[playerIndex].deviceId;
        if (string.IsNullOrEmpty(deviceId)) return null;

        return InputSystem.GetDeviceById(int.Parse(deviceId));
    }

    private void TryAssignDevice(InputDevice device)
    {
        if (assignedDevices.Contains(device)) return;

        if (IsUniqueDeviceType(device) && IsDeviceTypeAlreadyAssigned(device))
        {
            Debug.Log($"Dispositivo {device.displayName} ya está asignado");
            return;
        }

        if (!deviceSchemeMap.TryGetValue(device, out string controlScheme))
        {
            Debug.LogWarning($"No se encontró esquema para: {device.displayName}");
            return;
        }

        AssignDevice(device, controlScheme);
    }

    private bool IsUniqueDeviceType(InputDevice device)
    {
        return device is Keyboard || device is Mouse;
    }

    private bool IsDeviceTypeAlreadyAssigned(InputDevice device)
    {
        System.Type deviceType = device.GetType();

        foreach (var assignedDevice in assignedDevices)
        {
            if (assignedDevice.GetType() == deviceType ||
                (device is Mouse && assignedDevice is Keyboard) ||
                (device is Keyboard && assignedDevice is Mouse))
            {
                return true;
            }
        }
        return false;
    }

    private void AssignDevice(InputDevice device, string controlScheme)
    {
        if (device is Keyboard || device is Mouse)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && !assignedDevices.Contains(keyboard))
                assignedDevices.Add(keyboard);

            if (mouse != null && !assignedDevices.Contains(mouse))
                assignedDevices.Add(mouse);

            device = keyboard;
        }
        else
        {
            assignedDevices.Add(device);
        }

        configData.playerConfigs[currentPlayerIndex] = new PlayerConfigData.PlayerConfig
        {
            deviceId = device.deviceId.ToString(),
            controlScheme = controlScheme,
            displayIndex = 0
        };

        currentPlayerIndex++;

        if (currentPlayerIndex >= configData.maxPlayers)
        {
            CompleteDeviceAssignment();
        }
        else
        {
            UpdatePrompt();
        }
    }

    private void CompleteDeviceAssignment()
    {
        promptText.text = "¡Todos los dispositivos asignados!";

        if (enableDisplayAssignment)
        {
            StartDisplayAssignment();
        }
        else
        {
            StartFinalConfirmation();
        }
    }

    private void StartDisplayAssignment()
    {
        currentPlayerIndex = 0;
        isAssigningDisplay = true;
        currentDisplayIndex = 0;
        displayText.gameObject.SetActive(true);
        UpdateDisplayText();
        promptText.text = $"Jugador {currentPlayerIndex + 1}: Selecciona tu display";
    }

    private void NextDisplayAssignment()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= configData.maxPlayers)
        {
            StartFinalConfirmation();
        }
        else
        {
            currentDisplayIndex = 0;
            axisInUse = false;
            UpdateDisplayText();
            promptText.text = $"Jugador {currentPlayerIndex + 1}: Selecciona tu display";
        }
    }

    private void StartFinalConfirmation()
    {
        isAssigningDisplay = false;
        isFinalConfirmation = true;
        displayText.gameObject.SetActive(false);
        promptText.text = "Configuración completada!\n" +
                          "Jugador 1: Presiona CONFIRMAR para continuar\n" +
                          "Presiona CANCELAR para repetir";
    }

    private void UpdateDisplayText()
    {
        displayText.text = $"Display: {currentDisplayIndex + 1}";
    }

    private void UpdatePrompt()
    {
        promptText.text = $"Presiona un botón para asignar controlador al Jugador {currentPlayerIndex + 1}";
    }

    private void SaveConfiguration()
    {
        Debug.Log("Configuración guardada en ScriptableObject");
        // Aquí podrías guardar a disco si es necesario
    }
}