using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    // 1. DECLARE THE INSTANCE HERE (This was missing!)
    public static GameInput Instance { get; private set; }

    // The generated C# class reference
    public GameControls Controls { get; private set; }

    private void Awake()
    {
        // 2. INITIALIZE THE INSTANCE
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates if you reload the scene
            return;
        }

        Controls = new GameControls();
    }

    private void OnEnable()
    {
        if (Controls != null) Controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        if (Controls != null) Controls.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        // 3. CLEANUP (Now this works because 'Instance' exists)
        if (Controls != null) Controls.Gameplay.Disable();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Helper Methods ---
    public Vector2 GetMovementInput()
    {
        return Controls.Gameplay.Move.ReadValue<Vector2>();
    }

    public bool IsJumpPressed() => Controls.Gameplay.Jump.WasPressedThisFrame();
    public bool IsJumpHeld() => Controls.Gameplay.Jump.IsPressed();
    public bool IsDashPressed() => Controls.Gameplay.Dash.WasPressedThisFrame();
    public bool IsAttackMainPressed() => Controls.Gameplay.AttackMain.WasPressedThisFrame();
    public bool IsAttackSubPressed() => Controls.Gameplay.AttackSub.WasPressedThisFrame();
    public bool IsInteractPressed() => Controls.Gameplay.Interact.WasPressedThisFrame();
    public bool IsHealPressed() => Controls.Gameplay.Heal.WasPressedThisFrame();

    // Don't forget this one for your Pause Menu!
    public bool IsPausePressed() => Controls.Gameplay.Pause.WasPressedThisFrame();
}