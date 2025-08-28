using System;
using UnityEngine;

public enum CardSuit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum CardValue
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13
}

public enum CardAbility
{
    None,
    Peek,      // 7
    Spy,       // 8
    BlindSwap, // 9
    PeekAndSwap // 10
}

public class Card
{
    public CardSuit Suit { get; private set; }
    public CardValue Value { get; private set; }
    public int NumericValue 
    {
        get
        {
            if (Value == CardValue.Jack || Value == CardValue.Queen || Value == CardValue.King)
                return 20;
            return (int)Value;
        }
    }
    public bool HasPower => Value == CardValue.Nine;
    public bool IsSpecial => Ability != CardAbility.None;
    public Guid InstanceId { get; private set; }
    public CardAbility Ability { get; private set; }
    
    public Card(CardSuit suit, int rank)
    {
        Suit = suit;
        Value = (CardValue)rank;
        InstanceId = Guid.NewGuid();
        Ability = CardAbility.None;
        if (Value == CardValue.Seven) Ability = CardAbility.Peek;
        else if (Value == CardValue.Eight) Ability = CardAbility.Spy;
        else if (Value == CardValue.Nine) Ability = CardAbility.BlindSwap;
        else if (Value == CardValue.Ten) Ability = CardAbility.PeekAndSwap;
    }

    public override string ToString()
    {
        return $"{Value} of {Suit}";
    }
}

