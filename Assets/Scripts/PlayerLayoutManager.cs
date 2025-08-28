using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerLayoutManager : MonoBehaviour
{
    public GameObject playerHandDisplayPrefab;
    public Transform[] enemyHandAnchors;
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
        // Clear existing displays
        foreach (var display in playerDisplays)
        {
            if (display != null)
                Destroy(display.gameObject);
        }
        playerDisplays.Clear();

        // Create new displays for all non-local players
        int displayIndex = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (i != localPlayerIndex && displayIndex < enemyHandAnchors.Length)
            {
                var displayObj = Instantiate(playerHandDisplayPrefab, enemyHandAnchors[displayIndex]);
                var display = displayObj.GetComponent<PlayerHandDisplay>();
                if (display != null)
                {
                    display.spriteMap = cardSpriteMap;
                    display.Initialize(players[i], i, players[i].Username);
                    // Subscribe to the event
                    display.OnCardSelected += (player, cardIndex) => OnOpponentCardSelected?.Invoke(player, cardIndex);
                    playerDisplays.Add(display);
                }
                displayIndex++;
            }
        }

        UpdateAllDisplays(players, localPlayerIndex);
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