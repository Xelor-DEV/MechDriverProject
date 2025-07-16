using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class DeviceAssignmentManager : MonoBehaviour
{
    [Header("References")]
    public PlayerConfigData configData;
    public InputActionAsset inputActions; // Referencia al InputActionAsset
    public bool enableDisplayAssignment = true; // Habilitar/deshabilitar asignación de displays

    [Header("UI References")]
    public TMP_Text promptText;
    public TMP_Text displayText; // Texto para mostrar display actual

    [Header("Input Settings")]
    public InputActionReference navigationAction;
    public InputActionReference confirmAction;
    public InputActionReference cancelAction;
    public float axisThreshold = 0.5f; // Sensibilidad para detección de axis

    private InputAction joinAction;
    private int currentPlayerIndex;
    private bool isAssignmentComplete;
    private List<InputDevice> assignedDevices = new List<InputDevice>();
    private Dictionary<InputDevice, string> deviceSchemeMap = new Dictionary<InputDevice, string>();

    // Variables para asignación de displays
    private int currentDisplayIndex = 0;
    private bool isAssigningDisplay = false;
    private float lastAxisValue = 0f;
    private bool axisInUse = false;

    private void Awake()
    {
        configData.playerConfigs = new PlayerConfigData.PlayerConfig[configData.maxPlayers];

        // Crear acción para detectar cualquier botón presionado
        joinAction = new InputAction(binding: "/*/<button>");
        joinAction.Enable();

        // Precalcular esquemas para dispositivos
        PrecalculateDeviceSchemes();
    }

    // Precalcular los esquemas válidos para cada dispositivo
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

    public void StartAssignment()
    {
        currentPlayerIndex = 0;
        isAssignmentComplete = false;
        isAssigningDisplay = false;
        assignedDevices.Clear();
        promptText.gameObject.SetActive(true);
        displayText.gameObject.SetActive(false);
        UpdatePrompt();
    }

    private void Update()
    {
        if (isAssignmentComplete) return;

        if (isAssigningDisplay)
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
        // Usar eventos para detección de dispositivos
        if (joinAction.triggered && joinAction.activeControl != null)
        {
            InputDevice device = joinAction.activeControl.device;
            TryAssignDevice(device);
        }
    }

    private void HandleDisplaySelection()
    {
        InputDevice currentDevice = GetCurrentPlayerDevice();
        if (currentDevice == null) return;

        // Solo procesar input del dispositivo asignado a este jugador
        if (navigationAction.action.controls.Any(c => c.device == currentDevice))
        {
            float axisValue = navigationAction.action.ReadValue<Vector2>().x;

            // Detectar cambio en el axis
            if (Mathf.Abs(axisValue) > axisThreshold && !axisInUse)
            {
                axisInUse = true;
                currentDisplayIndex += (axisValue > 0) ? 1 : -1;

                // Asegurar que esté dentro de los límites
                currentDisplayIndex = Mathf.Clamp(
                    currentDisplayIndex,
                    0,
                    Display.displays.Length - 1
                );

                UpdateDisplayText();
            }
            else if (Mathf.Abs(axisValue) < axisThreshold)
            {
                axisInUse = false;
            }
        }

        // Confirmar selección
        if (confirmAction.action.triggered &&
            confirmAction.action.controls.Any(c => c.device == currentDevice))
        {
            configData.playerConfigs[currentPlayerIndex].displayIndex = currentDisplayIndex;
            NextDisplayAssignment();
        }
    }

    private InputDevice GetCurrentPlayerDevice()
    {
        if (currentPlayerIndex >= configData.playerConfigs.Length) return null;
        string deviceId = configData.playerConfigs[currentPlayerIndex].deviceId;
        if (string.IsNullOrEmpty(deviceId)) return null;
        return InputSystem.GetDeviceById(int.Parse(deviceId));
    }

    private void TryAssignDevice(InputDevice device)
    {
        // Verificar si el dispositivo ya está asignado
        if (assignedDevices.Contains(device)) return;

        // Verificar restricciones de dispositivos únicos
        if (IsUniqueDeviceType(device) && IsDeviceTypeAlreadyAssigned(device))
        {
            Debug.Log($"Dispositivo {device.displayName} ya está asignado");
            return;
        }

        // Obtener esquema de control válido
        if (!deviceSchemeMap.TryGetValue(device, out string controlScheme))
        {
            Debug.LogWarning($"No se encontró esquema para: {device.displayName}");
            return;
        }

        AssignDevice(device, controlScheme);
    }

    private bool IsUniqueDeviceType(InputDevice device)
    {
        // Dispositivos que solo pueden ser asignados a un jugador
        return device is Keyboard || device is Mouse;
    }

    private bool IsDeviceTypeAlreadyAssigned(InputDevice device)
    {
        // Comprobar si ya existe un dispositivo del mismo tipo asignado
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
        // Para teclado/ratón, asignar ambos dispositivos como un set
        if (device is Keyboard || device is Mouse)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && !assignedDevices.Contains(keyboard))
                assignedDevices.Add(keyboard);

            if (mouse != null && !assignedDevices.Contains(mouse))
                assignedDevices.Add(mouse);

            device = keyboard; // Usar uno como representante
        }
        else
        {
            assignedDevices.Add(device);
        }

        // Guardar configuración
        configData.playerConfigs[currentPlayerIndex] = new PlayerConfigData.PlayerConfig
        {
            deviceId = device.deviceId.ToString(),
            controlScheme = controlScheme,
            displayIndex = 0 // Se configurará después
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
            FinalizeAssignment();
        }
    }

    private void StartDisplayAssignment()
    {
        currentPlayerIndex = 0;
        isAssigningDisplay = true;
        currentDisplayIndex = 0;
        displayText.gameObject.SetActive(true);
        StartCoroutine(AssignDisplays());
    }

    private System.Collections.IEnumerator AssignDisplays()
    {
        for (int i = 0; i < configData.maxPlayers; i++)
        {
            currentPlayerIndex = i;
            InputDevice device = GetCurrentPlayerDevice();

            if (device == null)
            {
                Debug.LogWarning($"No se encontró dispositivo para jugador {i + 1}");
                continue;
            }

            // Resetear valores
            currentDisplayIndex = 0;
            lastAxisValue = 0f;
            axisInUse = false;

            promptText.text = $"Jugador {i + 1}: Selecciona tu display";
            UpdateDisplayText();

            // Esperar a que el jugador confirme
            bool displaySelected = false;
            while (!displaySelected)
            {
                // Verificar confirmación
                if (confirmAction.action.triggered &&
                    confirmAction.action.controls.Any(c => c.device == device))
                {
                    displaySelected = true;
                }
                yield return null;
            }

            // Guardar selección
            configData.playerConfigs[i].displayIndex = currentDisplayIndex;
        }

        FinalizeAssignment();
    }

    private void NextDisplayAssignment()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= configData.maxPlayers)
        {
            FinalizeAssignment();
        }
        else
        {
            // Preparar para siguiente jugador
            currentDisplayIndex = 0;
            lastAxisValue = 0f;
            axisInUse = false;
            promptText.text = $"Jugador {currentPlayerIndex + 1}: Selecciona tu display";
            UpdateDisplayText();
        }
    }

    private void UpdateDisplayText()
    {
        displayText.text = $"Display: {currentDisplayIndex}";
    }

    private void FinalizeAssignment()
    {
        isAssignmentComplete = true;
        isAssigningDisplay = false;
        displayText.gameObject.SetActive(false);
        promptText.text = "Configuración completada!\n" +
                          "Jugador 1: Presiona CONFIRMAR para continuar\n" +
                          "Presiona CANCELAR para repetir";

        // Habilitar acciones para confirmación final
        confirmAction.action.Enable();
        cancelAction.action.Enable();

        // Esperar confirmación del jugador 1
        StartCoroutine(WaitForFinalConfirmation());
    }

    private System.Collections.IEnumerator WaitForFinalConfirmation()
    {
        bool decisionMade = false;
        InputDevice player1Device = GetCurrentPlayerDevice();

        while (!decisionMade)
        {
            if (confirmAction.action.triggered &&
                confirmAction.action.controls.Any(c => c.device == player1Device))
            {
                decisionMade = true;
                SaveConfiguration();
                SceneManager.LoadScene("GameScene");
            }
            else if (cancelAction.action.triggered &&
                     cancelAction.action.controls.Any(c => c.device == player1Device))
            {
                decisionMade = true;
                StartAssignment();
            }
            yield return null;
        }
    }

    private void UpdatePrompt()
    {
        promptText.text = $"Presiona un botón para asignar controlador al Jugador {currentPlayerIndex + 1}";
    }

    private void SaveConfiguration()
    {
        // Aquí podrías guardar a disco si es necesario
        Debug.Log("Configuración guardada en ScriptableObject");
    }
}