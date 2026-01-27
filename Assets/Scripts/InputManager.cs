using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoSingleton<InputManager>
{
    InputSystem_Actions inputActions;

    public override bool Initialize()
    {
        if (!base.Initialize()) return false;

        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        return true;
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

    // Pointer/Touch position
    public Vector2 PointerPosition
    {
        get
        {
            // 터치스크린이 있으면 터치 위치 사용
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
            // 마우스가 있으면 마우스 위치 사용 (에디터/PC 테스트용)
            else if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            // 둘 다 없으면 화면 중앙 반환
            return new Vector2(Screen.width / 2f, Screen.height / 2f);
        }
    }
}
