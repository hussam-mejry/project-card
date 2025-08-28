using UnityEngine;

[System.Serializable]
public class PlayerLayout
{
    public int playerCount; // The total number of players for this layout (e.g., 3)
    public Transform[] anchorPoints; // The specific anchors to use for this player count
}