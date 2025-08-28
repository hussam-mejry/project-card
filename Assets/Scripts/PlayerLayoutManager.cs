using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerLayoutManager : MonoBehaviour
{
    public GameObject playerHandDisplayPrefab;
    public List<PlayerLayout> playerLayouts;
    public CardSpriteMap cardSpriteMap;
    private List<PlayerHandDisplay> playerDisplays = new List<PlayerHandDisplay>();
    public Action<Player, int> OnOpponentCardSelected;

    void Start()
    {
        if (cardSpriteMap == null)
        {
            cardSpriteMap = UnityEngine.Object.FindAnyObjectByType<CardSpriteMap>();
            if (cardSpriteMap == null)
            {
                Debug.LogError("CardSpriteMap reference is missing! Please assign it in the inspector.");
            }
        }
    }

    public void SetupPlayerDisplays(List<Player> players, int localPlayerIndex)
    {
        if (cardSpriteMap == null)
        {
            Debug.LogError("CardSpriteMap is null! Cannot setup player displays.");
            return;
        }

        foreach (var display in playerDisplays)
        {
            if (display != null) Destroy(display.gameObject);
        }
        playerDisplays.Clear();

        int totalPlayerCount = players.Count;
        PlayerLayout activeLayout = playerLayouts.FirstOrDefault(layout => layout.playerCount == totalPlayerCount);

        if (activeLayout == null || activeLayout.anchorPoints == null)
        {
            Debug.LogError($"No layout defined in PlayerLayoutManager for {totalPlayerCount} players!");
            return;
        }

        Transform[] activeAnchors = activeLayout.anchorPoints;
        int displayIndex = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (i == localPlayerIndex) continue; // Skip the local player

            if (displayIndex < activeAnchors.Length)
            {
                Transform parentAnchor = activeAnchors[displayIndex];
                var displayObj = Instantiate(playerHandDisplayPrefab, parentAnchor);
                var display = displayObj.GetComponent<PlayerHandDisplay>();

                if (display != null)
                {
                    display.spriteMap = cardSpriteMap;
                    display.Initialize(players[i], i, players[i].Username);
                    display.OnCardSelected += (player, cardIndex) => OnOpponentCardSelected?.Invoke(player, cardIndex);
                    playerDisplays.Add(display);
                }
                displayIndex++;
            }
        }
    }

    public void UpdateAllDisplays(List<Player> players, int currentPlayerIndex)
    {
        foreach (var display in playerDisplays)
        {
            if (display != null)
            {
                display.UpdateDisplay();
                display.SetHighlighted(display.CurrentPlayer == players[currentPlayerIndex]);
            }
        }
    }

    public void UpdateCurrentPlayer(int playerIndex)
    {
        foreach (var display in playerDisplays)
        {
            if (display != null)
            {
                display.SetHighlighted(display.CurrentPlayer.PlayerIndex == playerIndex);
            }
        }
    }

    public void RevealAllHands()
    {
        foreach (var display in playerDisplays)
        {
            if (display != null)
            {
                display.RevealHand();
            }
        }
    }

    public void SetAllDisplaysInteractable(bool interactable)
    {
        foreach (var display in playerDisplays)
        {
            display.SetCardsInteractable(interactable);
        }
    }

    public List<PlayerHandDisplay> GetPlayerDisplays()
    {
        return playerDisplays;
    }
}