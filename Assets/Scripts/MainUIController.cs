using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Controls the main menu, campaign level selection, and skirmish setup UI.
/// Attach this component to the same GameObject as the PanelRenderer.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class MainUIController : MonoBehaviour
{
    public enum MapSize
    {
        Small,
        Medium,
        Large
    }

    public enum AIStrength
    {
        Easy,
        Normal,
        Hard
    }

    // Subscribe to these from your game/session manager.
    public event Action<int> CampaignStartRequested;
    public event Action<MapSize, AIStrength, int> SkirmishStartRequested;

    [SerializeField] private PanelRenderer panelRenderer;

    private VisualElement root;
    private VisualElement mainPage;
    private VisualElement campaignPage;
    private VisualElement skirmishPage;

    private Button campaignStartButton;
    private Label campaignSelectionLabel;

    private Button[] levelButtons;
    private Button[] mapSizeButtons;
    private Button[] aiButtons;
    private Button[] opponentButtons;

    private readonly List<Button> boundButtons = new List<Button>();
    private readonly List<Action> boundActions = new List<Action>();

    private int uiVersion = -1;

    private int selectedCampaignLevel = -1;
    private MapSize selectedMapSize = MapSize.Medium;
    private AIStrength selectedAIStrength = AIStrength.Normal;
    private int selectedOpponentCount = 2;

    public int SelectedCampaignLevel => selectedCampaignLevel;
    public MapSize SelectedMapSize => selectedMapSize;
    public AIStrength SelectedAIStrength => selectedAIStrength;
    public int SelectedOpponentCount => selectedOpponentCount;

    public void OnCampaignStartRequest(int campaign)
    {
        
        MainMenuOptions.GameMode = "Campaign";
        MainMenuOptions.SelectedMap = ""+campaign;
        SceneManager.LoadScene("GameScene");

    }

    public void OnSkirmishStartRequest(MapSize mapSize, AIStrength aiStrength, int numOpponents)
    {
        MainMenuOptions.GameMode = "Skirmish";

        MainMenuOptions.MapSize = mapSize.ToString();
        MainMenuOptions.AIDifficulty = aiStrength.ToString();
        MainMenuOptions.OpponentsCount = numOpponents;
        SceneManager.LoadScene("GameScene");


    }

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        CampaignStartRequested += OnCampaignStartRequest;
        SkirmishStartRequested += OnSkirmishStartRequest;
    }

    private void OnEnable()
    {
        if (panelRenderer == null)
        {
            Debug.LogError(
                "MainUIController requires a PanelRenderer component.",
                this
            );

            enabled = false;
            return;
        }

        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);

        UnbindButtons();
    }

    private void OnUIReload(
        PanelRenderer renderer,
        VisualElement rootElement,
        int version)
    {
        // PanelRenderer can invoke this callback without rebuilding the UI.
        // Skip initialization if the UI version has not changed.
        if (version == uiVersion)
            return;

        uiVersion = version;
        root = rootElement;

        UnbindButtons();

        CacheElements();
        BindButtons();
        RefreshSelections();
        ShowPage(mainPage);
    }

    private void CacheElements()
    {
        mainPage = root.Q<VisualElement>("main-page");
        campaignPage = root.Q<VisualElement>("campaign-page");
        skirmishPage = root.Q<VisualElement>("skirmish-page");

        campaignStartButton = root.Q<Button>("campaign-start-button");
        campaignSelectionLabel = root.Q<Label>("campaign-selection-label");

        levelButtons = new Button[6];

        for (int i = 0; i < levelButtons.Length; i++)
            levelButtons[i] = root.Q<Button>($"level-{i + 1}");

        mapSizeButtons = new Button[]
        {
            root.Q<Button>("map-small"),
            root.Q<Button>("map-medium"),
            root.Q<Button>("map-large")
        };

        aiButtons = new Button[]
        {
            root.Q<Button>("ai-easy"),
            root.Q<Button>("ai-normal"),
            root.Q<Button>("ai-hard")
        };

        opponentButtons = new Button[]
        {
            root.Q<Button>("opponents-1"),
            root.Q<Button>("opponents-2"),
            root.Q<Button>("opponents-3")
        };
    }

    private void BindButtons()
    {
        Bind(root.Q<Button>("campaign-button"),
            () => ShowPage(campaignPage));

        Bind(root.Q<Button>("skirmish-button"),
            () => ShowPage(skirmishPage));

        Bind(root.Q<Button>("campaign-back-button"),
            () => ShowPage(mainPage));

        Bind(root.Q<Button>("skirmish-back-button"),
            () => ShowPage(mainPage));

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + 1;

            Bind(levelButtons[i],
                () => SelectCampaignLevel(level));
        }

        Bind(campaignStartButton, StartCampaign);

        Bind(root.Q<Button>("skirmish-start-button"),
            StartSkirmish);

        BindChoice(
            mapSizeButtons[0],
            mapSizeButtons,
            () => selectedMapSize = MapSize.Small
        );

        BindChoice(
            mapSizeButtons[1],
            mapSizeButtons,
            () => selectedMapSize = MapSize.Medium
        );

        BindChoice(
            mapSizeButtons[2],
            mapSizeButtons,
            () => selectedMapSize = MapSize.Large
        );

        BindChoice(
            aiButtons[0],
            aiButtons,
            () => selectedAIStrength = AIStrength.Easy
        );

        BindChoice(
            aiButtons[1],
            aiButtons,
            () => selectedAIStrength = AIStrength.Normal
        );

        BindChoice(
            aiButtons[2],
            aiButtons,
            () => selectedAIStrength = AIStrength.Hard
        );

        BindChoice(
            opponentButtons[0],
            opponentButtons,
            () => selectedOpponentCount = 1
        );

        BindChoice(
            opponentButtons[1],
            opponentButtons,
            () => selectedOpponentCount = 2
        );

        BindChoice(
            opponentButtons[2],
            opponentButtons,
            () => selectedOpponentCount = 3
        );
    }

    private void RefreshSelections()
    {
        // Campaign selection
        if (selectedCampaignLevel >= 1 &&
            selectedCampaignLevel <= levelButtons.Length)
        {
            SetSelected(
                levelButtons,
                levelButtons[selectedCampaignLevel - 1]
            );
        }
        else
        {
            SetSelected(levelButtons, null);
        }

        if (campaignSelectionLabel != null)
        {
            campaignSelectionLabel.text =
                selectedCampaignLevel > 0
                    ? $"LEVEL {selectedCampaignLevel:00} SELECTED"
                    : "NO LEVEL SELECTED";
        }

        campaignStartButton?.SetEnabled(selectedCampaignLevel > 0);

        // Skirmish selections
        SetSelected(
            mapSizeButtons,
            mapSizeButtons[(int)selectedMapSize]
        );

        SetSelected(
            aiButtons,
            aiButtons[(int)selectedAIStrength]
        );

        SetSelected(
            opponentButtons,
            opponentButtons[selectedOpponentCount - 1]
        );
    }

    private void Bind(Button button, Action callback)
    {
        if (button == null)
        {
            Debug.LogError(
                "MainUIController: A required button was not found in MainUI.uxml.",
                this
            );

            return;
        }

        button.clicked += callback;

        boundButtons.Add(button);
        boundActions.Add(callback);
    }

    private void BindChoice(
        Button button,
        Button[] group,
        Action setValue)
    {
        Bind(button, () =>
        {
            SetSelected(group, button);
            setValue();
        });
    }

    private void SetSelected(
        Button[] group,
        Button selectedButton)
    {
        if (group == null)
            return;

        foreach (Button button in group)
        {
            if (button == null)
                continue;

            button.RemoveFromClassList("selected");
        }

        selectedButton?.AddToClassList("selected");
    }

    private void SelectCampaignLevel(int level)
    {
        selectedCampaignLevel = level;

        SetSelected(
            levelButtons,
            levelButtons[level - 1]
        );

        if (campaignSelectionLabel != null)
            campaignSelectionLabel.text = $"LEVEL {level:00} SELECTED";

        campaignStartButton?.SetEnabled(true);
    }

    private void ShowPage(VisualElement pageToShow)
    {
        if (mainPage == null ||
            campaignPage == null ||
            skirmishPage == null ||
            pageToShow == null)
        {
            return;
        }

        mainPage.style.display =
            pageToShow == mainPage
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        campaignPage.style.display =
            pageToShow == campaignPage
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        skirmishPage.style.display =
            pageToShow == skirmishPage
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    private void StartCampaign()
    {
        if (selectedCampaignLevel < 1)
            return;

        if (CampaignStartRequested != null)
        {
            CampaignStartRequested.Invoke(selectedCampaignLevel);
        }
        else
        {
            Debug.Log(
                $"Campaign start requested. Level: {selectedCampaignLevel}",
                this
            );
        }
    }

    private void StartSkirmish()
    {
        if (SkirmishStartRequested != null)
        {
            SkirmishStartRequested.Invoke(
                selectedMapSize,
                selectedAIStrength,
                selectedOpponentCount
            );
        }
        else
        {
            Debug.Log(
                $"Skirmish start requested. Map: {selectedMapSize}, " +
                $"AI: {selectedAIStrength}, " +
                $"Opponents: {selectedOpponentCount}",
                this
            );
        }
    }

    private void UnbindButtons()
    {
        for (int i = 0; i < boundButtons.Count; i++)
        {
            if (boundButtons[i] != null)
                boundButtons[i].clicked -= boundActions[i];
        }

        boundButtons.Clear();
        boundActions.Clear();
    }
}