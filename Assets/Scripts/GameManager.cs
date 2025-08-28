using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    private enum GameState { Setup, InitialPeeking, Playing, RoundEnding, Scoring, GameOver }
    public enum TurnPhase { None, DrawPhase, ActionPhase, EndPhase }
    private enum SpecialActionState { None, AwaitingPeekOwn, AwaitingSpyTarget, AwaitingBlindSwapOwn, AwaitingBlindSwapOpponent }

    private GameState gameState = GameState.Setup;
    private TurnPhase turnPhase = TurnPhase.None;
    private List<Card> drawPile;
    private Stack<Card> discardPile;
    private List<Player> players;
    public PlayerUIController playerUI;
    public PlayerLayoutManager playerLayoutManager;
    public CardSpriteMap cardSpriteMap;
    private int currentPlayerIndex = 0;
    private Player currentPlayer => players[currentPlayerIndex];

    private int peekingPlayerIndex = 0;
    private int playerCount = 2;
    public int GetPlayerCount() => playerCount;
    public void SetPlayerCount(int count) => playerCount = Mathf.Clamp(count, 2, 6);
    public AIDifficulty aiDifficulty = AIDifficulty.Easy;
    private string pendingUsername = "Player";

    private Coroutine _aiTurnCoroutine = null;
    private bool _isShowingRoundResults = false;
    public bool IsShowingRoundResults() => _isShowingRoundResults;

    private Player _cambioCaller = null;
    private SpecialActionState _specialActionState = SpecialActionState.None;
    private int _blindSwapOwnIndex = -1;
    public bool IsAwaitingPlayerAction() => _specialActionState != SpecialActionState.None;

    void Awake()
    {
        if (playerLayoutManager != null) playerLayoutManager.OnOpponentCardSelected += OnOpponentCardSelected;
    }

    void Start()
    {
        if (playerUI == null) Debug.LogError("PlayerUI reference is missing!");
        if (playerLayoutManager == null) Debug.LogError("PlayerLayoutManager reference is missing!");
        if (cardSpriteMap != null) playerLayoutManager.cardSpriteMap = cardSpriteMap;
    }

    public void SetPlayerUsername(string username)
    {
        pendingUsername = string.IsNullOrEmpty(username) ? "Player" : username;
    }

    public void StartGameFromMenu()
    {
        if (playerUI != null && playerUI.gameplayUI != null) playerUI.gameplayUI.SetActive(true);
        SetupGame();
    }

    private void SetupGame()
    {
        gameState = GameState.Setup;
        List<Card> deck = DeckBuilder.CreateDeck();
        DeckBuilder.Shuffle(deck);

        players = new List<Player>();
        for (int i = 0; i < Mathf.Clamp(playerCount, 2, 6); i++)
        {
            if (i == 0) players.Add(new Player { Username = pendingUsername, IsHuman = true, IsLocalHuman = true, PlayerIndex = i });
            else players.Add(new AIPlayer(aiDifficulty, i) { PlayerIndex = i });
        }

        DeckBuilder.DealCards(deck, players, 4);
        drawPile = new List<Card>(deck);
        discardPile = new Stack<Card>();
        Card firstDiscard = drawPile[0];
        drawPile.RemoveAt(0);
        discardPile.Push(firstDiscard);

        if (playerLayoutManager != null) playerLayoutManager.SetupPlayerDisplays(players, 0);
        if (playerUI != null && discardPile.Count > 0) playerUI.UpdateDiscardPile(discardPile.Peek());

        peekingPlayerIndex = 0;
        gameState = GameState.InitialPeeking;
        StartPeekingPhase();
    }

    private void StartPeekingPhase()
    {
        if (peekingPlayerIndex >= players.Count)
        {
            gameState = GameState.Playing;
            currentPlayerIndex = 0;
            StartTurn(players[currentPlayerIndex]);
            return;
        }

        var player = players[peekingPlayerIndex];
        if (player.IsAI)
        {
            var indices = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < 2; i++)
            {
                int idx = indices[Random.Range(0, indices.Count)];
                player.PeekCard(idx);
                indices.Remove(idx);
            }
            peekingPlayerIndex++;
            StartPeekingPhase();
        }
        else
        {
            player.KnownCardIndexes.Clear();
            playerUI.StartPeekingPhase(player, () =>
            {
                peekingPlayerIndex++;
                StartPeekingPhase();
            });
        }
    }

    private void EndTurn()
    {
        if (gameState != GameState.Playing || turnPhase != TurnPhase.EndPhase) return;

        if (_aiTurnCoroutine != null) StopCoroutine(_aiTurnCoroutine);
        _aiTurnCoroutine = null;

        if (playerUI != null)
        {
            playerUI.SetInteractable(false);
            playerUI.drawPileButton.interactable = false;
            playerUI.discardPileButton.interactable = false;
            playerUI.discardButton.interactable = false;
            playerUI.cambioButton.interactable = false;
        }

        turnPhase = TurnPhase.None;

        do { currentPlayerIndex = (currentPlayerIndex + 1) % players.Count; }
        while (players[currentPlayerIndex].IsEliminated);

        StartTurn(players[currentPlayerIndex]);
    }

    private void StartTurn(Player player)
    {
        if (_aiTurnCoroutine != null) StopCoroutine(_aiTurnCoroutine);
        _aiTurnCoroutine = null;

        if (gameState != GameState.Playing) return;

        turnPhase = TurnPhase.DrawPhase;
        playerLayoutManager.UpdateCurrentPlayer(currentPlayerIndex);
        if (player.IsAI) _aiTurnCoroutine = StartCoroutine(ExecuteAITurn((AIPlayer)player));
        else playerUI.SetHumanTurn(true);
    }

    private IEnumerator ExecuteAITurn(AIPlayer ai)
    {
        yield return new WaitForSeconds(1.5f);

        Card drawnCard = DrawCard();
        if (drawnCard == null) yield break;

        ai.DrawnCard = drawnCard;
        playerUI.UpdateUI(players[0], players[0].DrawnCard);
        yield return new WaitForSeconds(1.0f);

        // AI makes its decision
        int swapIndex = ai.FindBestSwapIndex();
        if (swapIndex != -1)
        {
            PlayerSwap(ai, swapIndex);
        }
        else
        {
            PlayerDiscard(ai);
        }
    }

    public void PlayerDiscard(Player player)
    {
        if (player == null || player.DrawnCard == null) return;
        if (player == currentPlayer && turnPhase != TurnPhase.ActionPhase && turnPhase != TurnPhase.DrawPhase) return;

        Card discardedCard = player.DrawnCard;
        player.DrawnCard = null;
        discardPile.Push(discardedCard);

        if (!player.IsAI)
        {
            playerUI.UpdateUI(player, null);
            playerUI.UpdateDiscardPile(discardedCard);
            playerUI.SetInteractable(false); // Player's action part of the turn is over.
        }
        else
        {
            playerUI.UpdateDiscardPile(discardedCard);
        }

        if (discardedCard.IsSpecial)
        {
            if (player.IsAI)
            {
                _aiTurnCoroutine = StartCoroutine(ExecuteAISpecialAbility((AIPlayer)player, discardedCard));
            }
            else
            {
                // Human player triggers a special ability.
                HandleHumanSpecialAbility(discardedCard);
            }
        }
        else
        {
            turnPhase = TurnPhase.EndPhase;
            EndTurn();
        }
    }

    public void OnCambioButtonPressed()
    {
        if (gameState != GameState.Playing) return;
        _cambioCaller = currentPlayer;
        gameState = GameState.RoundEnding;
        playerUI.SetInteractable(false);
        playerUI.drawPileButton.interactable = false;
        playerUI.discardPileButton.interactable = false;
        StartCoroutine(EndRoundAndScoreCoroutine());
    }

    private IEnumerator EndRoundAndScoreCoroutine()
    {
        _isShowingRoundResults = true;
        playerUI.RevealAllHands(players);
        playerLayoutManager.RevealAllHands();
        yield return new WaitForSeconds(2f);
        EndRoundAndScore();

        playerUI.UpdateScoreboard(players);
        playerUI.scoreboardPanel.SetActive(true);
        yield return new WaitForSeconds(4f);
        playerUI.scoreboardPanel.SetActive(false);

        var activePlayers = players.Where(p => !p.IsEliminated).ToList();
        if (activePlayers.Count > 1)
        {
            _isShowingRoundResults = false;
            StartCoroutine(RestartRoundCoroutine());
        }
        else
        {
            gameState = GameState.GameOver;
            Player winner = activePlayers.FirstOrDefault();
            if (playerUI.gameOverPanel != null && playerUI.gameOverText != null)
            {
                playerUI.gameOverText.text = winner != null ? $"{winner.Username} Wins!" : "Game Over!";
                playerUI.gameOverPanel.SetActive(true);
            }
        }
    }

    private void EndRoundAndScore()
    {
        Dictionary<Player, int> handScores = new Dictionary<Player, int>();
        foreach (var p in players.Where(p => !p.IsEliminated)) handScores[p] = CalculateHandScore(p.Hand);

        int lowestScore = handScores.Count > 0 ? handScores.Values.Min() : 0;
        var winners = handScores.Where(kvp => kvp.Value == lowestScore).Select(kvp => kvp.Key).ToList();

        bool callerSucceeded = winners.Contains(_cambioCaller);
        if (_cambioCaller != null && !callerSucceeded)
        {
            _cambioCaller.Score -= (handScores[_cambioCaller] + 10);
        }

        foreach (var (player, score) in handScores)
        {
            if (player != _cambioCaller && !winners.Contains(player)) player.Score -= score;
            if (player.Score <= 0) player.IsEliminated = true;
        }
        _cambioCaller = null;
    }

    public void StartNewGame()
    {
        if (players != null)
        {
            foreach (var player in players)
            {
                player.Score = 200; // Reset to starting score
                player.IsEliminated = false;
            }
        }

        if (playerUI != null && playerUI.gameOverPanel != null)
        {
            playerUI.gameOverPanel.SetActive(false);
        }

        StartNewRound();
    }

    private IEnumerator RestartRoundCoroutine()
    {
        yield return new WaitForSeconds(1f);
        StartNewRound();
    }

    private void StartNewRound()
    {
        if (playerUI.scoreboardPanel.activeSelf) playerUI.scoreboardPanel.SetActive(false);
        _isShowingRoundResults = false;
        _cambioCaller = null;
        gameState = GameState.Setup;

        List<Card> deck = DeckBuilder.CreateDeck();
        DeckBuilder.Shuffle(deck);

        // Reset all player hands and known cards
        foreach (var p in players)
        {
            p.Hand.Clear();
            p.KnownCardIndexes.Clear();
            p.DrawnCard = null;
        }
        DeckBuilder.DealCards(deck, players.Where(p => !p.IsEliminated).ToList(), 4);

        drawPile = new List<Card>(deck);
        discardPile = new Stack<Card>();
        Card firstDiscard = drawPile[0];
        drawPile.RemoveAt(0);
        discardPile.Push(firstDiscard);

        playerUI.UpdateDiscardPile(firstDiscard);
        playerLayoutManager.SetupPlayerDisplays(players, 0);

        // Explicitly update the local player's UI to ensure their cards are face down.
        var localPlayer = players.FirstOrDefault(p => p.IsLocalHuman);
        if (localPlayer != null)
        {
            playerUI.UpdateUI(localPlayer, null);
        }

        peekingPlayerIndex = 0;
        gameState = GameState.InitialPeeking;
        StartPeekingPhase();
    }

    public void RequestScoreboardUpdate()
    {
        if (players != null) playerUI.UpdateScoreboard(players);
    }

    public void InitiatePeekAbility()
    {
        _specialActionState = SpecialActionState.AwaitingPeekOwn;
        playerUI.SetInteractable(true);
    }

    public void InitiateSpyAbility()
    {
        _specialActionState = SpecialActionState.AwaitingSpyTarget;
        playerLayoutManager.SetAllDisplaysInteractable(true);
    }

    public void InitiateBlindSwap()
    {
        _specialActionState = SpecialActionState.AwaitingBlindSwapOwn;

        // Explicitly manage UI state for this specific action
        playerUI.drawPileButton.interactable = false;
        playerUI.discardPileButton.interactable = false;
        playerUI.discardButton.interactable = false;
        playerUI.cambioButton.interactable = false;

        // Only allow interaction with the player's own cards.
        playerUI.SetInteractable(true);
    }

    public void ResolvePlayerAction(int cardIndex)
    {
        if (_specialActionState == SpecialActionState.AwaitingBlindSwapOwn)
        {
            _blindSwapOwnIndex = cardIndex;
            _specialActionState = SpecialActionState.AwaitingBlindSwapOpponent;
            playerUI.turnPhaseText.text = "Now select an OPPONENT'S card.";
            playerUI.SetInteractable(false);
            playerLayoutManager.SetAllDisplaysInteractable(true);
        }
        else if (_specialActionState == SpecialActionState.AwaitingPeekOwn)
        {
            StartCoroutine(ExecutePeekOwnCard(cardIndex));
        }
    }

    private void OnOpponentCardSelected(Player targetPlayer, int targetCardIndex)
    {
        if (_specialActionState == SpecialActionState.AwaitingSpyTarget)
        {
            StartCoroutine(ExecuteSpy(targetPlayer, targetCardIndex));
        }
        else if (_specialActionState == SpecialActionState.AwaitingBlindSwapOpponent)
        {
            ExecuteBlindSwap(targetPlayer, targetCardIndex);
        }
    }

    private IEnumerator ExecutePeekOwnCard(int index)
    {
        _specialActionState = SpecialActionState.None;
        playerUI.SetInteractable(false);

        // Check if the card was already known so we don't accidentally hide it later.
        bool wasAlreadyKnown = currentPlayer.KnownCardIndexes.Contains(index);

        // Temporarily reveal the card only on the player's UI.
        if (!wasAlreadyKnown)
        {
            currentPlayer.KnownCardIndexes.Add(index);
        }
        playerUI.UpdateUI(currentPlayer, null);

        // Wait for the peek duration.
        yield return new WaitForSeconds(3.0f);

        // IMPORTANT: Hide the card again by removing the index IF it wasn't known before.
        if (!wasAlreadyKnown)
        {
            currentPlayer.KnownCardIndexes.Remove(index);
        }
        playerUI.UpdateUI(currentPlayer, null);

        playerUI.turnPhaseText.text = "Peek complete.";
        yield return new WaitForSeconds(0.5f);

        turnPhase = TurnPhase.EndPhase;
        EndTurn();
    }

    private IEnumerator ExecuteSpy(Player target, int index)
    {
        _specialActionState = SpecialActionState.None;
        playerLayoutManager.SetAllDisplaysInteractable(false);

        PlayerHandDisplay targetDisplay = FindPlayerDisplay(target);
        if (targetDisplay != null)
        {
            StartCoroutine(targetDisplay.RevealCardTemporarily(index, 3.0f));
            targetDisplay.TriggerCardFeedback(index, new Color(0.2f, 0.5f, 1f, 0.7f), 3.0f); // Blue feedback
        }

        yield return new WaitForSeconds(3.0f);

        playerUI.turnPhaseText.text = "Spy complete.";
        yield return new WaitForSeconds(0.5f);

        turnPhase = TurnPhase.EndPhase;
        EndTurn();
    }

    private void ExecuteBlindSwap(Player target, int targetCardIndex, int ownCardIndex, Player instigator)
    {
        _specialActionState = SpecialActionState.None;

        bool instigatorKnewOwnCard = instigator.KnownCardIndexes.Contains(ownCardIndex);
        bool targetKnewTheirCard = target.KnownCardIndexes.Contains(targetCardIndex);

        if (instigatorKnewOwnCard)
        {
            instigator.KnownCardIndexes.Remove(ownCardIndex);
        }
        if (targetKnewTheirCard)
        {
            target.KnownCardIndexes.Remove(targetCardIndex);
        }

        (target.Hand[targetCardIndex], instigator.Hand[ownCardIndex]) = (instigator.Hand[ownCardIndex], target.Hand[targetCardIndex]);

        Color swapColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        PlayerHandDisplay targetDisplay = FindPlayerDisplay(target);
        if (targetDisplay != null)
        {
            targetDisplay.TriggerCardFeedback(targetCardIndex, swapColor, 2.0f);
            targetDisplay.UpdateDisplay();
        }

        if (instigator.IsAI)
        {
            PlayerHandDisplay instigatorDisplay = FindPlayerDisplay(instigator);
            if (instigatorDisplay != null)
            {
                instigatorDisplay.TriggerCardFeedback(ownCardIndex, swapColor, 2.0f);
                instigatorDisplay.UpdateDisplay();
            }
        }
        else
        {
            playerUI.TriggerCardFeedback(ownCardIndex, swapColor, 2.0f);
            playerUI.UpdateUI(instigator, null);
        }
    }

    private void ExecuteBlindSwap(Player target, int index)
    {
        playerLayoutManager.SetAllDisplaysInteractable(false);

        ExecuteBlindSwap(target, index, _blindSwapOwnIndex, currentPlayer);

        _blindSwapOwnIndex = -1;

        turnPhase = TurnPhase.EndPhase;
        EndTurn();
    }


    private PlayerHandDisplay FindPlayerDisplay(Player playerToFind)
    {
        return playerLayoutManager.GetPlayerDisplays().FirstOrDefault(d => d.CurrentPlayer == playerToFind);
    }

    public Card DrawFromDiscard()
    {
        if (!CanDrawFromDiscard()) return null; turnPhase = TurnPhase.ActionPhase; return discardPile.Pop();
    }

    public bool CanDrawFromDiscard() => gameState == GameState.Playing && turnPhase == TurnPhase.DrawPhase && discardPile.Count > 0;

    private int CalculateHandScore(List<Card> hand) => hand.Sum(card => card.NumericValue);

    private bool CanPlayerAct(Player player) => gameState == GameState.Playing && turnPhase == TurnPhase.ActionPhase && player == currentPlayer && player.DrawnCard != null;

    public void PlayerSwap(Player player, int handIndex)
    {
        if (!CanPlayerAct(player)) return;

        Card temp = player.Hand[handIndex];
        player.Hand[handIndex] = player.DrawnCard;
        discardPile.Push(temp);
        player.DrawnCard = null;

        turnPhase = TurnPhase.EndPhase;

        if (!player.KnownCardIndexes.Contains(handIndex))
        {
            player.KnownCardIndexes.Add(handIndex);
        }

        if (!player.IsAI)
        {
            playerUI.UpdateUI(player, null);
            playerUI.UpdateDiscardPile(temp);
        }

        EndTurn();
    }

    public Card DrawCard() { if (gameState != GameState.Playing || turnPhase != TurnPhase.DrawPhase) { Debug.LogWarning($"Cannot draw card in state: {gameState}, phase: {turnPhase}"); return null; } if (drawPile.Count == 0) ReshuffleDiscardPile(); if (drawPile.Count > 0) { Card card = drawPile[0]; drawPile.RemoveAt(0); turnPhase = TurnPhase.ActionPhase; return card; } return null; }

    private void ReshuffleDiscardPile()
    {
        Debug.Log("Reshuffling discard pile into draw pile");

        // Can't reshuffle if there's nothing to take.
        if (discardPile.Count <= 1)
        {
            return;
        }

        // Keep the top card of discard pile.
        Card topDiscard = discardPile.Pop();

        // Move all other cards from the discard pile to the draw pile.
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        // Shuffle the draw pile.
        DeckBuilder.Shuffle(drawPile);

        // Put the top card back onto the now-empty discard pile.
        discardPile.Push(topDiscard);
    }

    private void HandleHumanSpecialAbility(Card card)
    {
        if (playerUI != null)
        {
            playerUI.drawPileButton.interactable = false;
            playerUI.discardPileButton.interactable = false;
        }

        switch (card.Value)
        {
            case CardValue.Seven:
                playerUI.turnPhaseText.text = "You discarded a 7! Select one of YOUR cards to peek at.";
                InitiatePeekAbility();
                break;
            case CardValue.Eight:
                playerUI.turnPhaseText.text = "You discarded an 8! Select an OPPONENT'S card to spy on.";
                InitiateSpyAbility();
                break;
            case CardValue.Nine:
                playerUI.turnPhaseText.text = "You discarded a 9! First, select YOUR card to give away.";
                InitiateBlindSwap();
                break;
            case CardValue.Ten:
                turnPhase = TurnPhase.EndPhase;
                EndTurn();
                break;
        }
    }

    private IEnumerator ExecuteAISpecialAbility(AIPlayer ai, Card abilityCard)
    {
        yield return new WaitForSeconds(1.0f);

        switch (abilityCard.Value)
        {
            case CardValue.Seven: // AI Peeks its own card
                int cardToPeek = ai.ChooseCardToPeek();
                if (cardToPeek != -1)
                {
                    ai.PeekCard(cardToPeek);
                }
                break;

            case CardValue.Eight: // AI Spies on another player
                var (targetPlayer, targetCardIndex) = ai.ChooseSpyTarget(players);
                if (targetPlayer != null && targetCardIndex != -1)
                {
                    PlayerHandDisplay targetDisplay = FindPlayerDisplay(targetPlayer);
                    if (targetDisplay != null)
                    {
                        targetDisplay.TriggerCardFeedback(targetCardIndex, Color.blue, 2.0f);
                    }
                }
                break;

            case CardValue.Nine: // Blind Swap
                int ownCardIndex = Random.Range(0, ai.Hand.Count);
                var (swapTarget, swapCardIndex) = ai.ChooseSpyTarget(players);
                if (swapTarget != null && swapCardIndex != -1)
                {
                    ExecuteBlindSwap(swapTarget, swapCardIndex, ownCardIndex, ai);
                }
                break;
        }

        yield return new WaitForSeconds(0.5f);
        turnPhase = TurnPhase.EndPhase;
        EndTurn();
    }
}
