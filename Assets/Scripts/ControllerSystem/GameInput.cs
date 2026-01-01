using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    // The single instance of the generated C# class
    public GameControls Controls { get; private set; }

    private void Awake()
    {
        Controls = new GameControls();
    }

    private void OnEnable()
    {
        Controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        Controls.Gameplay.Disable();
    }

    // Helper methods to keep other scripts clean
    // calling .WasPressedThisFrame() mimics Input.GetKeyDown()

    public Vector2 GetMovementInput()
    {
        return Controls.Gameplay.Move.ReadValue<Vector2>();
    }

    public bool IsJumpPressed() => Controls.Gameplay.Jump.WasPressedThisFrame();
    public bool IsJumpHeld() => Controls.Gameplay.Jump.IsPressed(); // For variable jump height if needed

    public bool IsDashPressed() => Controls.Gameplay.Dash.WasPressedThisFrame();

    public bool IsAttackMainPressed() => Controls.Gameplay.AttackMain.WasPressedThisFrame();
    public bool IsAttackSubPressed() => Controls.Gameplay.AttackSub.WasPressedThisFrame();

    public bool IsInteractPressed() => Controls.Gameplay.Interact.WasPressedThisFrame();

    public bool IsHealPressed() => Controls.Gameplay.Heal.WasPressedThisFrame();
}