using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreEntryUI : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text playerScoreText;
    public TMP_Text playerStatusText; // Assign a new TextMeshPro object for status
    public Image backgroundImage; // Assign the main background image of the prefab

    public Color normalColor = Color.white; // Color for a normal entry
    public Color winnerColor = Color.green; // Color for the winner
    public Color penaltyColor = Color.red; // Color for a failed Cambio call

    public void Setup(Player player, bool isWinner, bool hadPenalty)
    {
        if (player == null) return;

        playerNameText.text = player.Username;
        playerScoreText.text = player.IsEliminated ? "ELIMINATED" : player.Score.ToString();

        // Default state
        playerStatusText.text = "";
        if (backgroundImage != null) backgroundImage.color = normalColor;

        if (player.IsEliminated)
        {
            playerStatusText.text = "Eliminated";
        }
        else if (hadPenalty)
        {
            playerStatusText.text = "Cambio Failed!";
            if (backgroundImage != null) backgroundImage.color = penaltyColor;
        }
        else if (isWinner)
        {
            playerStatusText.text = "Round Winner!";
            if (backgroundImage != null) backgroundImage.color = winnerColor;
        }
    }
}