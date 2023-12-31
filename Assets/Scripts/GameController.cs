using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Animations;

public class GameController : MonoBehaviour
{
    static public GameController instance = null;
    public Deck playerDeck = new Deck();
    public Deck enemyDeck = new Deck();

    public Hand playersHand = new Hand();
    public Hand enemysHand = new Hand();
    public Player player = null;
    public Player enemy = null;

    public List<CardData> cards = new List<CardData>();

    public Sprite[] healthNumbers = new Sprite[10];
    public Sprite[] damageNumbers = new Sprite[10];
    public GameObject cardPrefab = null;
    public Canvas canvas = null;
    public bool isPlayable = false;
    public GameObject effectFromLeftPrefab = null;
    public GameObject effectFromRightPrefab = null;

    public AudioSource playerDieAudio = null;
    public AudioSource enemyDieAudio = null;

    public Sprite fireBallImage = null;
    public Sprite iceBallImage = null;
    public Sprite multiFireBallImage = null;
    public Sprite multiIceBallImage = null;
    public Sprite fireAndIceBallImage = null;
    public Sprite destructBallImage = null;
    public bool playersTurn = true;
    public Text turnText = null;
    public Image enemySkipTurn = null;
    public Sprite fireDemon = null;
    public Sprite iceDemon = null;
    public Text scoreText = null;
    public int playerScore = 0;
    public int playerKills = 0;

    private void Awake()
    {
        instance = this;
        SetUpEnemy();
        playerDeck.Create();
        enemyDeck.Create();

        StartCoroutine(DealHands());
    }
    public void Quit()
    {
        SceneManager.LoadScene(0);
    }
    
    public void SkipTurn()
    {
        if (playersTurn && isPlayable)
            NextPlayersTurn();
    }
    internal IEnumerator DealHands()
    {
        yield return new WaitForSeconds(1);
        for (int t=0;t<3;t++)
        {
            playerDeck.DealCard(playersHand);
            enemyDeck.DealCard(enemysHand);
            yield return new WaitForSeconds(1);
        }
        isPlayable = true;
    }
    internal bool UseCard(Card card, Player usingOnPlayer, Hand fromHand)
    {

        //castCard
        //RemoveCard
        //DealReplacementCard
        if (!CardValid(card, usingOnPlayer, fromHand))
            return false;
        isPlayable = false;
        CastCard(card,usingOnPlayer,fromHand);
        player.glowImage.gameObject.SetActive(false);
        enemy.glowImage.gameObject.SetActive(false);
        fromHand.RemoveCard(card);
        return false;
    }
    internal bool CardValid(Card cardBeingPlayed, Player usingOnPlayer, Hand fromHand)
    {
        bool valid = false;
        if (cardBeingPlayed == null)
            return false;
        if (fromHand.isPlayers)
        {
            //attack cards from player
            if (cardBeingPlayed.cardData.cost<=player.mana)
            {
                if (usingOnPlayer.isPlayer && cardBeingPlayed.cardData.isDefenceCard)
                    valid = true;
                if (!usingOnPlayer.isPlayer && !cardBeingPlayed.cardData.isDefenceCard)
                    valid = true;
            }
        }
        else
        //Enemy's attack card and defence card
        {
            if (cardBeingPlayed.cardData.cost <= enemy.mana)
            {
                if (!usingOnPlayer.isPlayer && cardBeingPlayed.cardData.isDefenceCard)
                    valid = true;
                if (!usingOnPlayer.isPlayer && !cardBeingPlayed.cardData.isDefenceCard)
                    valid = true;
            }
        }
        return valid;
    }
    internal void CastCard(Card card, Player usingOnPlayer, Hand fromHand)
    {
        if (card.cardData.isMirrorCard)
        {
            usingOnPlayer.SetMirror(true);
            usingOnPlayer.PlayMirrorSound();
            NextPlayersTurn();
            isPlayable = true;
        }
        else
        {
            if (card.cardData.isDefenceCard) //health cards
            {
                usingOnPlayer.health +=card.cardData.damage;
                usingOnPlayer.PlayHealSound();
                if (usingOnPlayer.health > usingOnPlayer.maxHealth)
                    usingOnPlayer.health = usingOnPlayer.maxHealth;

                UpdateHealths();
                StartCoroutine(CastHealEffect(usingOnPlayer));
            }
            else //attack card
            {
                CastAttackEffect(card, usingOnPlayer);
            }
            //Add Score
            if (fromHand.isPlayers)
                playerScore += card.cardData.damage;
            UpdateScore();
        }
        //update player mana
        if (fromHand.isPlayers)
        {
            GameController.instance.player.mana -= card.cardData.cost;
            GameController.instance.player.UpdateManaBalls();
        }
        else
        {
            GameController.instance.enemy.mana -= card.cardData.cost;
            GameController.instance.enemy.UpdateManaBalls();
        }
    }

    private IEnumerator CastHealEffect(Player usingOnPlayer)
    {
        yield return new WaitForSeconds(0.5f);
        NextPlayersTurn();
        isPlayable = true;
    }
    internal void CastAttackEffect(Card card, Player usingOnPlayer)
    {
        GameObject effectGO = null;
        if (usingOnPlayer.isPlayer)
            effectGO = Instantiate(effectFromRightPrefab, canvas.gameObject.transform);
        else
            effectGO = Instantiate(effectFromLeftPrefab, canvas.gameObject.transform);

        Effect effect = effectGO.GetComponent<Effect>();
        if (effect)
        {
            effect.targetPlayer = usingOnPlayer;
            effect.sourceCard = card;
            
            switch(card.cardData.damageType)
            {
                case CardData.DamageType.Fire:
                    if (card.cardData.isMulti)
                        effect.effectImage.sprite = multiFireBallImage;
                    else
                        effect.effectImage.sprite = fireBallImage;
                    effect.PlayFireballSound();
                    break;
                case CardData.DamageType.Ice:
                    if (card.cardData.isMulti)
                        effect.effectImage.sprite = multiIceBallImage;
                    else
                        effect.effectImage.sprite = iceBallImage;
                    effect.PlayIceSound();
                    break;
                case CardData.DamageType.Both:
                    effect.effectImage.sprite = fireAndIceBallImage;
                    effect.PlayFireballSound();
                    effect.PlayIceSound();
                    break;
                case CardData.DamageType.Destroy:
                    effect.effectImage.sprite = destructBallImage;
                    effect.PlayDestuctSound();
                    effect.PlayDestuctSound();
                    break;
            }
        }
    }
    internal void UpdateHealths()
    {
        player.UpdateHealth();
        enemy.UpdateHealth();

        if (player.health <= 0)
        {
            // GameOver
            StartCoroutine(GameOver());
        }
        if (enemy.health <= 0)
        {
            playerKills++;
            playerScore += 100;
            UpdateScore();
            //New enemy
            StartCoroutine(NewEnemy());
        }
    }
    private IEnumerator NewEnemy()
    {
        enemy.gameObject.SetActive(false);
        //clear enemy hand
        enemysHand.ClearHand();
        yield return new WaitForSeconds(0.75f);
        //Set new enemy
        SetUpEnemy();
        enemy.gameObject.SetActive(true);
        StartCoroutine(DealHands());
    }
    private void SetUpEnemy()
    {
        enemy.mana = 0;
        enemy.health = 5;
        enemy.UpdateHealth();
        enemy.isFire = true;
        if (UnityEngine.Random.Range(0, 2) == 1)
            enemy.isFire = false;
        if (enemy.isFire)
            enemy.playerImage.sprite = fireDemon;
        else
            enemy.playerImage.sprite = iceDemon;

    }
    private IEnumerator GameOver()
    {
        yield return new WaitForSeconds(1);
        UnityEngine.SceneManagement.SceneManager.LoadScene(2);
    }
    internal void NextPlayersTurn()
    {
        playersTurn = !playersTurn;
        bool enemyIsDead = false;

        if (playersTurn)
        {
            if(player.mana < 5)
            player.mana++;
        }
        else
        {
            if (enemy.health > 0)
            {
                if (enemy.mana < 5)
                    enemy.mana++;
            }
            else
                enemyIsDead = true;
        }
 
        //Set turn text
        if (enemyIsDead)
        {
            playersTurn = !playersTurn;
            if (player.mana < 5)
                player.mana++;
        }
        else
        {
            SetTurnText();
            if (!playersTurn)
                MonstersTurn();
        }
        
        player.UpdateManaBalls();
        enemy.UpdateManaBalls();
    }
    internal void SetTurnText()
    {
        if(playersTurn)
        {
            turnText.text = "Merlin's Turn";
        }
        else
        {
            turnText.text = "Enemy's Turn";
        }
    }
    private void MonstersTurn()
    {
        Card card = AIChooseCard();
        StartCoroutine(MonsterCastCard(card));
    }
    private Card AIChooseCard()
    {
        //checks all cards in enemy hand to see if they are valid to be used
        List<Card> available = new List<Card>();
        for (int i = 0; i < 3; i ++)
        {
            if (CardValid(enemysHand.cards[i], enemy, enemysHand))
                available.Add(enemysHand.cards[i]);
            else if (CardValid(enemysHand.cards[i], player, enemysHand))
                available.Add(enemysHand.cards[i]);
        }
        if (available.Count == 0) // none available
        {
            NextPlayersTurn();
            return null;
        }
        int choice = UnityEngine.Random.Range(0, available.Count);
        return available[choice];
    }
    private IEnumerator MonsterCastCard(Card card)
    {
        yield return new WaitForSeconds(0.5f);
        if (card)
        {
            // Turn over player
            TurnCard(card);
            yield return new WaitForSeconds(2);
            //Use Card
            if (card.cardData.isDefenceCard)
                UseCard(card, enemy, enemysHand);
            else //attack card
                UseCard(card, player, enemysHand);
    
            yield return new WaitForSeconds(1);
            //Deal card
            enemyDeck.DealCard(enemysHand);
            yield return new WaitForSeconds(1);
        }
        else // no card to choose, so skip turn
        {
            //Display enemy SkipTurn 
            enemySkipTurn.gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            //Hide enemy SkipTurn
            enemySkipTurn.gameObject.SetActive(false);
        }
    }
    internal void TurnCard(Card card)
    {
        Animator animator = card.GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.SetTrigger("Flip");
        }
        else
            Debug.LogError("No Animator Found.");
    }
    private void UpdateScore()
    {
        scoreText.text = "Demons killed: " + playerKills.ToString() + ". Score: " + playerScore.ToString();
    }
    internal void PlayPlayerDieSound()
    {
        playerDieAudio.Play();
    }
    internal void PlayEnemyDieSound()
    {
        enemyDieAudio.Play();
    }
}