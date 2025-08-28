using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class PlayerHandDisplay : MonoBehaviour
{
    public Image[] cardImages;
    public GameObject[] cardHighlights; // Assign Highlight1, Highlight2, etc. here in the prefab
    public TMP_Text playerNameText;
    public GameObject currentPlayerIndicator; // Assign the turn indicator object here
    public Sprite cardBackSprite;
    public CardSpriteMap spriteMap;

    private Player player;
    public Player CurrentPlayer => player;
    public Action<Player, int> OnCardSelected;

    public void Initialize(Player player, int index, string displayName = null)
    {
        this.player = player;

        if (playerNameText != null)
        {
            playerNameText.text = displayName ?? $"Player {index + 1}";
        }

        for (int i = 0; i < cardImages.Length; i++)
        {
            int cardIndex = i;
            var button = cardImages[i].GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCardSelected?.Invoke(this.player, cardIndex));
            }
        }

        foreach (var highlight in cardHighlights)
        {
            if (highlight != null) highlight.SetActive(false);
        }

        SetHighlighted(false);
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (player == null || cardImages == null) return;

        for (int i = 0; i < cardImages.Length; i++)
        {
            if (cardImages[i] != null)
            {
                if (i < player.Hand.Count)
                {
                    cardImages[i].gameObject.SetActive(true);
                    cardImages[i].sprite = cardBackSprite;
                }
                else
                {
                    cardImages[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void TriggerCardFeedback(int cardIndex, Color feedbackColor, float duration)
    {
        if (cardIndex < cardHighlights.Length && cardHighlights[cardIndex] != null)
        {
            StartCoroutine(ShowFeedbackEffect(cardHighlights[cardIndex], feedbackColor, duration));
        }
    }

    private IEnumerator ShowFeedbackEffect(GameObject highlight, Color color, float duration)
    {
        Image highlightImage = highlight.GetComponent<Image>();
        if (highlightImage != null)
        {
            highlightImage.color = color;
            highlight.SetActive(true);
            yield return new WaitForSeconds(duration);
            highlight.SetActive(false);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (currentPlayerIndicator != null)
        {
            currentPlayerIndicator.SetActive(highlighted);
        }
    }

    public void RevealHand()
    {
        if (player == null || cardImages == null || spriteMap == null) return;

        for (int i = 0; i < cardImages.Length && i < player.Hand.Count; i++)
        {
            if (cardImages[i] != null)
            {
                Card card = player.Hand[i];
                cardImages[i].sprite = spriteMap.GetSprite(card.Suit, (int)card.Value);
            }
        }
    }

    public void SetCardsInteractable(bool interactable)
    {
        foreach (var image in cardImages)
        {
            if (image != null)
            {
                var button = image.GetComponent<Button>();
                if (button != null) button.interactable = interactable;
            }
        }
    }

    public void RevealSingleCard(int index, bool reveal)
    {
        if (player == null || cardImages == null || index >= cardImages.Length || index >= player.Hand.Count)
        {
            return;
        }
    }

    public IEnumerator RevealCardTemporarily(int cardIndex, float duration)
    {
        if (player == null || cardIndex >= player.Hand.Count || cardImages[cardIndex] == null)
        {
            yield break;
        }

        Sprite originalSprite = cardImages[cardIndex].sprite;

        Card cardToReveal = player.Hand[cardIndex];
        cardImages[cardIndex].sprite = spriteMap.GetSprite(cardToReveal.Suit, (int)cardToReveal.Value);

        yield return new WaitForSeconds(duration);

        cardImages[cardIndex].sprite = originalSprite;
    }
}