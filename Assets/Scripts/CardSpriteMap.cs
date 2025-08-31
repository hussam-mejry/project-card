using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardSpriteMap", menuName = "Card Game/Card Sprite Map")]
public class CardSpriteMap : ScriptableObject
{
    public List<CardSpriteEntry> entries;

    [System.Serializable]
    public class CardSpriteEntry
    {
        public CardSuit suit;
        public int rank;
        public Sprite sprite;
    }

    private Dictionary<(CardSuit, int), Sprite> _lookup;

    public Sprite GetSprite(CardSuit suit, int rank)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<(CardSuit, int), Sprite>();
            foreach (var entry in entries)
                _lookup[(entry.suit, entry.rank)] = entry.sprite;
        }

        return _lookup.TryGetValue((suit, rank), out var sprite) ? sprite : null;
    }
}

