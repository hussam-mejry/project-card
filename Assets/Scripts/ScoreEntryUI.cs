using UnityEngine;
using TMPro;

public class ScoreEntryUI : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text playerScoreText;

    public void Setup(Player player)
    {
        if (player == null) return;

        playerNameText.text = player.Username;
        playerScoreText.text = player.IsEliminated ? "ELIMINATED" : player.Score.ToString();
    }
}