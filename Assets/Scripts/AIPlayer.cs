using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AIPlayer : Player
{
    public AIDifficulty Difficulty { get; private set; }


    public AIPlayer(AIDifficulty difficulty, int playerIndex) : base()
    {
        Difficulty = difficulty;
        PlayerIndex = playerIndex;
        IsHuman = false;
        IsLocalHuman = false;
        IsAI = true;
        Username = $"AI Player {playerIndex}";
    }

    public int FindBestSwapIndex()
    {
        if (DrawnCard == null) return -1;

        int bestIndexToSwap = -1;
        int highestValueInHand = -1;

        // Find the highest-value card among the ones the AI knows about.
        foreach (int index in KnownCardIndexes)
        {
            if (index < Hand.Count)
            {
                int cardValue = Hand[index].NumericValue;
                if (cardValue > highestValueInHand)
                {
                    highestValueInHand = cardValue;
                    bestIndexToSwap = index;
                }
            }
        }

        // If we have a known card to compare against and the drawn card is better (lower value).
        if (bestIndexToSwap != -1 && DrawnCard.NumericValue < highestValueInHand)
        {
            return bestIndexToSwap;
        }

        // Default to discarding if no beneficial swap is identified.
        return -1;
    }

    public int ChooseCardToPeek()
    {
        var unknownIndices = new List<int>();
        for (int i = 0; i < Hand.Count; i++)
        {
            if (!KnownCardIndexes.Contains(i))
            {
                unknownIndices.Add(i);
            }
        }

        if (unknownIndices.Count > 0)
        {
            return unknownIndices[Random.Range(0, unknownIndices.Count)];
        }

        // If all cards are known, peek a random one again.
        return Random.Range(0, Hand.Count);
    }

    public (Player, int) ChooseSpyTarget(List<Player> allPlayers)
    {
        // Find a player that isn't this AI and isn't eliminated.
        var potentialTargets = allPlayers.Where(p => p != this && !p.IsEliminated).ToList();
        if (potentialTargets.Count == 0) return (null, -1);

        Player targetPlayer = potentialTargets[Random.Range(0, potentialTargets.Count)];

        // Find a card in the target's hand that this AI doesn't know.
        var unknownCardIndices = new List<int>();
        for (int i = 0; i < targetPlayer.Hand.Count; i++)
        {
            if (!targetPlayer.KnownCardIndexes.Contains(i))
            {
                unknownCardIndices.Add(i);
            }
        }

        if (unknownCardIndices.Count > 0)
        {
            int targetCardIndex = unknownCardIndices[Random.Range(0, unknownCardIndices.Count)];
            return (targetPlayer, targetCardIndex);
        }

        // If all cards are known, just pick a random one.
        return (targetPlayer, Random.Range(0, targetPlayer.Hand.Count));
    }

    public override void TakeTurn(GameManager gameManager)
    {
        // AI's turn logic is now handled by GameManager.ExecuteAITurn
        // This method is kept for compatibility with the Player base class
    }
}