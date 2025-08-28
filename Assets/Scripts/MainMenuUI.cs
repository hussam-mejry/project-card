using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject gamePanel;
    public TMP_InputField usernameInputField;
    public Button startGameButton;
    public GameManager gameManager;
    public TMP_Dropdown playerCountDropdown;
    public TMP_Dropdown aiDifficultyDropdown;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        gamePanel.SetActive(false);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        // Setup player count dropdown
        if (playerCountDropdown != null && gameManager != null)
        {
            playerCountDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            for (int i = 2; i <= 6; i++)
                options.Add(i.ToString());
            playerCountDropdown.AddOptions(options);
            int currentCount = gameManager.GetPlayerCount();
            playerCountDropdown.value = Mathf.Clamp(currentCount, 2, 6) - 2;
            playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);
        }

        // Setup AI difficulty dropdown
        if (aiDifficultyDropdown != null && gameManager != null)
        {
            aiDifficultyDropdown.ClearOptions();
            aiDifficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "Easy", "Medium", "Hard" });
            aiDifficultyDropdown.value = (int)gameManager.aiDifficulty;
            aiDifficultyDropdown.onValueChanged.AddListener(OnAIDifficultyChanged);
        }
    }

    void OnStartGameClicked()
    {
        string username = usernameInputField.text.Trim();
        if (string.IsNullOrEmpty(username))
            username = "Player";

        gameManager.SetPlayerUsername(username);
        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameManager.StartGameFromMenu();
    }

    void OnPlayerCountChanged(int index)
    {
        if (gameManager != null)
            gameManager.SetPlayerCount(index + 2);
    }

    void OnAIDifficultyChanged(int index)
    {
        if (gameManager != null)
            gameManager.aiDifficulty = (AIDifficulty)index;
    }
} 