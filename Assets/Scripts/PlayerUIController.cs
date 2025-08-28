using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerUIController : MonoBehaviour
{
    public GameObject gameplayUI;
    public Button[] cardButtons;
    public Image[] cardImages;
    public GameObject[] cardHighlights;
    public Button discardButton;
    public Button cambioButton;
    public Button drawPileButton;
    public Button discardPileButton;
    public GameObject peekingPanel;
    public TMP_Text peekingInstructionText;
    public TMP_Text turnPhaseText;
    public Image drawnCardImage;
    public Image discardPileImage;
    public CardSpriteMap spriteMap;
    public Sprite cardBackSprite;
    public GameObject scoreboardPanel;
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    public Button restartButton;
    public GameObject scoreEntryPrefab;
    public Transform scoreboardContent;

    [SerializeField] private GameManager gameManager;
    private Player currentPlayer;
    private Action onPeekingComplete;
    private bool isPeeking;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
        {
            if (scoreboardPanel != null && !scoreboardPanel.activeSelf)
            {
                gameManager.RequestScoreboardUpdate();
                scoreboardPanel.SetActive(true);
            }
        }
        else
        {
            if (scoreboardPanel != null && scoreboardPanel.activeSelf && !gameManager.IsShowingRoundResults())
            {
                scoreboardPanel.SetActive(false);
            }
        }
    }

    void Start()
    {
        if (gameplayUI != null) gameplayUI.SetActive(false);
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int index = i;
            if (cardButtons[i] != null) cardButtons[i].onClick.AddListener(() => OnCardButtonClicked(index));
        }
        if (discardButton != null) discardButton.onClick.AddListener(OnDiscardButtonClicked);
        if (cambioButton != null) cambioButton.onClick.AddListener(OnCambioButtonClicked);
        if (drawPileButton != null) drawPileButton.onClick.AddListener(OnDrawPileClicked);
        if (discardPileButton != null) discardPileButton.onClick.AddListener(OnDiscardPileClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

        // Ensure highlights are off by default
        foreach (var highlight in cardHighlights)
        {
            if (highlight != null) highlight.SetActive(false);
        }

        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (peekingPanel != null) peekingPanel.SetActive(false);
        SetInteractable(false);
    }

    public void UpdateUI(Player player, Card drawnCard)
    {
        currentPlayer = player;
        if (cardImages == null) return;
        int maxCards = Mathf.Min(cardImages.Length, player.Hand.Count);

        for (int i = 0; i < cardImages.Length; i++)
        {
            if (cardImages[i] != null) cardImages[i].gameObject.SetActive(i < maxCards);
        }

        for (int i = 0; i < maxCards; i++)
        {
            if (cardImages[i] != null)
            {
                var card = player.Hand[i];
                bool shouldShowFace = player.KnownCardIndexes.Contains(i);
                cardImages[i].sprite = shouldShowFace ? spriteMap.GetSprite(card.Suit, (int)card.Value) : cardBackSprite;
            }
        }

        if (drawnCardImage != null)
        {
            drawnCardImage.gameObject.SetActive(drawnCard != null);
            if (drawnCard != null) drawnCardImage.sprite = spriteMap.GetSprite(drawnCard.Suit, (int)drawnCard.Value);
        }
    }

    public void UpdateDiscardPile(Card topCard)
    {
        if (discardPileImage != null && topCard != null)
        {
            discardPileImage.sprite = spriteMap.GetSprite(topCard.Suit, (int)topCard.Value);
            discardPileImage.gameObject.SetActive(true);
        }
    }

    public void SetInteractable(bool interactable)
    {
        foreach (var button in cardButtons)
        {
            if (button != null) button.interactable = interactable;
        }
        if (discardButton != null) discardButton.interactable = interactable;
        if (cambioButton != null) cambioButton.interactable = interactable;
    }

    public void SetHumanTurn(bool isHumanTurn)
    {
        if (isHumanTurn)
        {
            SetInteractable(false);
            if (drawPileButton != null) drawPileButton.interactable = true;
            if (discardPileButton != null) discardPileButton.interactable = true;
            if (cambioButton != null) cambioButton.interactable = true;
            if (turnPhaseText != null) turnPhaseText.text = "Your Turn - Draw a Card";
        }
        else
        {
            SetInteractable(false);
            if (turnPhaseText != null) turnPhaseText.text = "Waiting for other players...";
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

    private void OnDrawPileClicked()
    {
        Card drawnCard = gameManager.DrawCard();
        if (drawnCard != null)
        {
            currentPlayer.DrawnCard = drawnCard;
            UpdateUI(currentPlayer, drawnCard);
            EnableActionPhase();
        }
    }

    private void OnDiscardPileClicked()
    {
        if (!gameManager.CanDrawFromDiscard()) return;
        Card drawnCard = gameManager.DrawFromDiscard();
        if (drawnCard != null)
        {
            currentPlayer.DrawnCard = drawnCard;
            UpdateUI(currentPlayer, drawnCard);
            EnableActionPhase();
        }
    }

    private void HandleHumanSpecialCard(Card card)
    {
        SetInteractable(false);
        drawPileButton.interactable = false;
        discardPileButton.interactable = false;
        discardButton.interactable = false;

        switch (card.Value)
        {
            case CardValue.Seven:
                turnPhaseText.text = "You drew a 7! Select one of YOUR cards to peek at.";
                gameManager.InitiatePeekAbility();
                break;
            case CardValue.Eight:
                turnPhaseText.text = "You drew an 8! Select an OPPONENT'S card to spy on.";
                gameManager.InitiateSpyAbility();
                break;
            case CardValue.Nine:
                turnPhaseText.text = "You drew a 9! First, select YOUR card to give away.";
                gameManager.InitiateBlindSwap();
                break;
            case CardValue.Ten:
                turnPhaseText.text = "You drew a 10! Select YOUR card to peek at first.";
                StartSinglePeekPhase(currentPlayer, () =>
                {
                    turnPhaseText.text = "Choose to SWAP with a card or DISCARD the 10.";
                    EnableActionPhase();
                });
                break;
        }
    }

    public void EnableActionPhase()
    {
        SetInteractable(false);
        if (currentPlayer != null && currentPlayer.DrawnCard != null)
        {
            foreach (var button in cardButtons)
            {
                if (button != null) button.interactable = true;
            }
            if (discardButton != null) discardButton.interactable = true;
            if (turnPhaseText != null) turnPhaseText.text = "Choose Action: Swap or Discard";
        }
        if (cambioButton != null) cambioButton.interactable = true;
    }

    public void StartPeekingPhase(Player player, Action onComplete)
    {
        isPeeking = true;
        currentPlayer = player;
        onPeekingComplete = onComplete;
        SetInteractable(false);
        if (peekingPanel != null) peekingPanel.SetActive(true);
        if (peekingInstructionText != null) peekingInstructionText.text = "Select two cards to peek at";

        foreach (var button in cardButtons)
        {
            if (button != null) button.interactable = true;
        }
    }

    public void StartSinglePeekPhase(Player player, Action onComplete)
    {
        isPeeking = true;
        currentPlayer = player;
        onPeekingComplete = onComplete;
        SetInteractable(false);
        if (peekingPanel != null) peekingPanel.SetActive(true);
        if (peekingInstructionText != null) peekingInstructionText.text = "Select one card to peek at";

        foreach (var button in cardButtons)
        {
            if (button != null) button.interactable = true;
        }
    }

    private void OnCardButtonClicked(int index)
    {
        if (gameManager.IsAwaitingPlayerAction()) gameManager.ResolvePlayerAction(index);
        else if (isPeeking) HandlePeekingCardClick(index);
        else if (currentPlayer != null && currentPlayer.DrawnCard != null) gameManager.PlayerSwap(currentPlayer, index);
    }

    private void HandlePeekingCardClick(int index)
    {
        if (currentPlayer == null || index >= currentPlayer.Hand.Count) return;

        if (currentPlayer.KnownCardIndexes.Contains(index)) return;

        currentPlayer.PeekCard(index);
        Card card = currentPlayer.Hand[index];
        cardImages[index].sprite = spriteMap.GetSprite(card.Suit, (int)card.Value);
        cardButtons[index].interactable = false;

        bool isSinglePeekMode = peekingInstructionText.text.Contains("one card");
        bool isInitialPeekComplete = !isSinglePeekMode && currentPlayer.KnownCardIndexes.Count >= 2;

        if (isSinglePeekMode || isInitialPeekComplete)
        {
            EndPeekingPhase();
        }
    }

    private void EndPeekingPhase()
    {
        isPeeking = false;
        if (peekingPanel != null) peekingPanel.SetActive(false);
        SetInteractable(false);
        var callback = onPeekingComplete;
        onPeekingComplete = null;
        callback?.Invoke();
    }

    private void OnRestartClicked()
    {
        if (gameManager != null) gameManager.StartNewGame();
    }

    public void UpdateScoreboard(List<Player> players)
    {
        if (scoreEntryPrefab == null || scoreboardContent == null) return;

        foreach (Transform child in scoreboardContent)
        {
            Destroy(child.gameObject);
        }

        if (players == null) return;

        foreach (var player in players)
        {
            var entryObj = Instantiate(scoreEntryPrefab, scoreboardContent);
            var entryUI = entryObj.GetComponent<ScoreEntryUI>();
            if (entryUI != null)
            {
                entryUI.Setup(player);
            }
        }
    }

    private void OnDiscardButtonClicked()
    {
        if (gameManager != null) gameManager.PlayerDiscard(currentPlayer);
    }

    private void OnCambioButtonClicked()
    {
        if (gameManager != null)
            gameManager.OnCambioButtonPressed();
    }

    public void RevealAllHands(List<Player> players)
    {
        if (currentPlayer != null)
        {
            for (int i = 0; i < currentPlayer.Hand.Count && i < cardImages.Length; i++)
            {
                cardImages[i].sprite = spriteMap.GetSprite(currentPlayer.Hand[i].Suit, (int)currentPlayer.Hand[i].Value);
            }
        }
    }
}