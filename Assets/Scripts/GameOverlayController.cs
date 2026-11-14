using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameOverlayController : MonoBehaviour
{
    private PanelRenderer _panelRenderer;
    
    private Button PauseButton;
    private Button OneXButton;
    private Button TwoXButton;
    private Button ThreeXButton;
    private List<Button> speedButtons = new List<Button>();

    private void OnEnable()
    {
        _panelRenderer = GetComponent<PanelRenderer>();

        // Register the callback to get elements safely when the panel loads
        _panelRenderer.RegisterUIReloadCallback(OnUiReloaded);
    }

    private void OnDisable()
    {
        // Unregister to prevent memory leaks and duplicate references
        _panelRenderer.UnregisterUIReloadCallback(OnUiReloaded);

        if (PauseButton != null)
        {
            PauseButton.UnregisterCallback<ClickEvent>(OnPauseClicked);
        }

        if (OneXButton != null)
        {
            OneXButton.UnregisterCallback<ClickEvent>(OnOneXClicked);
        }
        if (TwoXButton != null)
        {
            TwoXButton.UnregisterCallback<ClickEvent>(OnTwoXClicked);
        }
        if (ThreeXButton != null)
        {
            ThreeXButton.UnregisterCallback<ClickEvent>(OnThreeXClicked);
        }
    }

    private void OnUiReloaded(PanelRenderer pr, VisualElement root)
    {
        // Query elements directly from the provided root argument
        PauseButton = root.Q<Button>("PauseButton");
        OneXButton = root.Q<Button>("1xButton");
        TwoXButton = root.Q<Button>("2xButton");
        ThreeXButton = root.Q<Button>("3xButton");


        speedButtons.Add(PauseButton);
        speedButtons.Add(OneXButton);
        speedButtons.Add(TwoXButton);
        speedButtons.Add(ThreeXButton);

        PauseButton.RegisterCallback<ClickEvent>(OnPauseClicked);
        OneXButton.RegisterCallback<ClickEvent>(OnOneXClicked);
        TwoXButton.RegisterCallback<ClickEvent>(OnTwoXClicked);
        ThreeXButton.RegisterCallback<ClickEvent>(OnThreeXClicked);



    }

    private void OnPauseClicked(ClickEvent evt)
    {
        SetSelectedSpeedButton(evt.target as Button);
        Debug.Log("Button clicked!");
        GameManager.Instance.gameTimeScale = 0f; // Pause the game
    }

    private void OnOneXClicked(ClickEvent evt)
    {
        SetSelectedSpeedButton(evt.target as Button);
        Debug.Log("1x Button clicked!");
        GameManager.Instance.gameTimeScale = 1f; // Resume the game
    }

    private void OnTwoXClicked(ClickEvent evt)
    {
        SetSelectedSpeedButton(evt.target as Button);
        Debug.Log("2x Button clicked!");
        GameManager.Instance.gameTimeScale = 2f;
    }

    private void OnThreeXClicked(ClickEvent evt)
    {
        SetSelectedSpeedButton(evt.target as Button);
        Debug.Log("3x Button clicked!");
        GameManager.Instance.gameTimeScale = 3f;
    }

    void SetSelectedSpeedButton(Button btn)
    {
        foreach (Button b in speedButtons) { b.RemoveFromClassList("speed-button-selected"); }
        btn.AddToClassList("speed-button-selected");
    }
}
