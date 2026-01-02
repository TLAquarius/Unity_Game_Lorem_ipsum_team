using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsMenuGenerator : MonoBehaviour
{
    [Header("Setup")]
    public GameObject rebindRowPrefab;
    public Transform listContainer;

    [Header("UI References")]
    public GameObject waitingTextOverlay; // <--- NEW SLOT: Drag your "Press Key" text here!

    private GameControls gameControls;

    void Start()
    {
        gameControls = new GameControls();

        foreach (InputAction action in gameControls.Gameplay.Get().actions)
        {
            if (action.name == "Move") continue;
            CreateRebindButton(action);
        }
    }

    void CreateRebindButton(InputAction action)
    {
        GameObject newRow = Instantiate(rebindRowPrefab, listContainer);

        var uiScript = newRow.GetComponent<RebindActionUI>();
        if (uiScript != null)
        {
            // <--- Pass the text object here!
            uiScript.Initialize(action, waitingTextOverlay);
        }
    }
}