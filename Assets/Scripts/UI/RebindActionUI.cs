using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class RebindActionUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI bindingText;
    public Button rebindButton;
    public GameObject waitingTextObject;

    private InputAction inputAction; // Stores the injected action
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    // --- NEW FUNCTION: Call this to setup the button dynamically ---
    public void Initialize(InputAction action, GameObject overlayRef)
    {
        inputAction = action;
        waitingTextObject = overlayRef; // <-- We assign it here via Code, not Inspector!

        // Update Label
        actionNameText.text = action.name.ToUpper();

        UpdateBindingDisplay();

        // Hook up button
        rebindButton.onClick.RemoveAllListeners();
        rebindButton.onClick.AddListener(StartRebinding);

        // Ensure the text starts hidden
        if (waitingTextObject != null) waitingTextObject.SetActive(false);
    }

    void StartRebinding()
    {
        waitingTextObject.SetActive(true);
        rebindButton.interactable = false;

        inputAction.Disable(); // Must disable to rebind

        rebindingOperation = inputAction.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebinding())
            .Start();
    }

    void FinishRebinding()
    {
        rebindingOperation.Dispose();
        inputAction.Enable();
        waitingTextObject.SetActive(false);
        rebindButton.interactable = true;
        UpdateBindingDisplay();
    }

    void UpdateBindingDisplay()
    {
        // Get the human-readable string
        int bindingIndex = inputAction.GetBindingIndexForControl(inputAction.controls[0]);
        if (bindingIndex < 0) bindingIndex = 0;

        string displayString = InputControlPath.ToHumanReadableString(
            inputAction.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );

        bindingText.text = displayString;
    }
}