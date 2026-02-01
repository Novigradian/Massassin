using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor; // Allows us to stop the game in the editor
#endif

public class Canvas : MonoBehaviour
{
    public static Canvas Instance;

    [Header("UI Panels")]
    [Tooltip("The container holding Play, Settings, Credits, Exit")]
    public GameObject mainButtonsPanel; 
    
    [Tooltip("The panel that appears when Play is clicked")]
    public GameObject levelSelectionPanel;
    
    [Tooltip("The panel that appears when Settings is clicked")]
    public GameObject settingsPanel;
    
    [Tooltip("The panel that appears when Credits is clicked")]
    public GameObject creditsPanel;

    [Tooltip("The panel that appears when player loses")]
    public GameObject losePanel;

    [Tooltip("The panel that appears when player wins")]
    public GameObject winPanel;

    public bool isTesting = false;

    private void Awake()
    {
        // --- SINGLETON LOGIC ---
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Ensure we start in the correct state (Main Menu Open, others Closed)
        if (!isTesting) ShowMainMenu();  
    }

    // --- BUTTON FUNCTIONS ---

    public void OnPlayClicked()
    {
        mainButtonsPanel.SetActive(false);
        levelSelectionPanel.SetActive(true);
    }

    public void OnSettingsClicked()
    {
        mainButtonsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnCreditsClicked()
    {
        mainButtonsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void OnExitClicked()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();

        // This allows the "Exit" button to stop the game while you are testing in Unity Editor
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #endif
    }

    // --- HELPER FUNCTION (Connect this to 'Back' buttons inside sub-panels) ---
    public void OnBackToMenuClicked()
    {
        // Turn off all sub-panels
        levelSelectionPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        // Turn on main menu
        mainButtonsPanel.SetActive(true);
    }

    private void ShowMainMenu()
    {
        // Helper to reset UI state on start
        if(mainButtonsPanel) mainButtonsPanel.SetActive(true);
        if(levelSelectionPanel) levelSelectionPanel.SetActive(false);
        if(settingsPanel) settingsPanel.SetActive(false);
        if(creditsPanel) creditsPanel.SetActive(false);
    }

    public void ShowLosePanel()
    {
        losePanel.SetActive(true);
    }

    public void ShowWinPanel()
    {
        winPanel.SetActive(true);
    }

    public void ReloadCurrentLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadGameLevelByName(string levelName)
    {
        // Optional: Switch music if you haven't already
        if (AudioManager.Instance != null) {
            AudioManager.Instance.SwitchToGameTheme();
        }

        SceneManager.LoadScene(levelName);
    }

    public void LoadUILevelByName(string levelName)
    {
        // Optional: Switch music if you haven't already
        if (AudioManager.Instance != null) {
            AudioManager.Instance.SwitchToMenuTheme();
        }

        SceneManager.LoadScene(levelName);
        ShowMainMenu();
    }

    public void EnterGameMode()
    {
        if(mainButtonsPanel) mainButtonsPanel.SetActive(false);
        if(levelSelectionPanel) levelSelectionPanel.SetActive(false);
        if(settingsPanel) settingsPanel.SetActive(false);
        if(creditsPanel) creditsPanel.SetActive(false);
    }


}