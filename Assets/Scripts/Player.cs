using System;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public List<Card> Hand { get; private set; }
    // Add this to hold the currently drawn card
    public Card DrawnCard { get; set; }
    public List<int> KnownCardIndexes { get; private set; }

    public bool IsHuman { get; set; } = false;
    public bool IsLocalHuman { get; set; } = false;
    public bool IsAI { get; set; } = false;

    public int TurnCount { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    public bool IsEliminated { get; set; }
    public int PlayerIndex { get; set; }

    public Player()
    {
        Hand = new List<Card>();

        DrawnCard = null;
        KnownCardIndexes = new List<int>();
        Score = 200;
        IsEliminated = false;
    }

    public void ReceiveCard(Card card)
    {
        Hand.Add(card);
    }

    public void PeekCard(int index)
    {
        if (index >= 0 && index < Hand.Count && !KnownCardIndexes.Contains(index))
        {
            KnownCardIndexes.Add(index);
            Debug.Log($"Peeked at card {index}: {Hand[index]}");
        }
    }


    public override string ToString()
    {
        return $"Player hand (face down): {Hand.Count} cards";
    }

    // Update DrawCard to store the drawn card
    public void DrawCard(Card card)
    {
        DrawnCard = card;
        Debug.Log($"Player draws: {card}");
    }

    // Swap drawn card with card at handIndex; returns replaced card
    public Card SwapDrawnWithHand(int index)
    {
        if (DrawnCard == null || index < 0 || index >= Hand.Count)
            return null;

        Card discarded = Hand[index];       // Save old card
        Hand[index] = DrawnCard;

        if (!KnownCardIndexes.Contains(index))
            KnownCardIndexes.Add(index);

        DrawnCard = null;

        return discarded;
    }



    // Discard the drawn card (no swap)
    public Card DiscardDrawn()
    {
        if (DrawnCard == null)
            throw new InvalidOperationException("No card drawn to discard.");

        Card cardToDiscard = DrawnCard;
        DrawnCard = null;
        return cardToDiscard;
    }

    public virtual void TakeTurn(GameManager gameManager)
    {
        // Human player: UI will handle
    }
}
