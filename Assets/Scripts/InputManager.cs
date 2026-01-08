using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoSingleton<InputManager>
{
    InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    // Player Controls
    public float RotateInput => inputActions.Player.Rotate.ReadValue<float>();
    public bool FireInput => inputActions.Player.Fire.IsPressed();
    public bool PauseInput => inputActions.Player.Pause.WasPressedThisFrame();
    public bool EscapeInput => inputActions.Player.Pause.WasPressedThisFrame();  // Alias for PauseInput

    // Debug/Test inputs
    public bool HelpInput => Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
    public bool UpgradeInput => Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame;
    public bool RInput => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
}
