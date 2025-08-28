using System.Collections.Generic;

public static class DeckBuilder
{
    public static List<Card> CreateDeck()
    {
        List<Card> deck = new List<Card>();

        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                deck.Add(new Card(suit, rank));
                deck.Add(new Card(suit, rank));
            }
        }

        return deck;
    }

    public static void Shuffle(List<Card> deck)
    {
        System.Random rng = new System.Random();

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    public static void DealCards(List<Card> deck, List<Player> players, int cardsPerPlayer)
    {
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            foreach (Player player in players)
            {
                if (deck.Count == 0) return;

                Card dealtCard = deck[0];
                deck.RemoveAt(0);
                player.ReceiveCard(dealtCard);
            }
        }
    }
}
