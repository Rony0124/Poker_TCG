using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PlayerCharacter
{
    public enum CardType
    {
        None,
        Diamond,
        Club,
        Spade,
        Heart
    }
    
    [Serializable]
    public struct SpawnCardInfo
    {
        public GameObject spawnAllyPrefab;
        public CardType cardType;
    }
    
    [SerializeField]
    private List<SpawnCardInfo> spawnCards;
    [SerializeField] 
    private SpawnCard spawnCardPrefab;
    
    [SerializeField]
    private Transform hand;
    [SerializeField]
    private Transform deck;
    
    [Header("Visuals")]
    [SerializeField] private float cardY;
    [SerializeField] private float cardXSpacing;
    [SerializeField] private float cardYSpacing;
    [Range(0, 5)]
    [SerializeField] private float cardAngle = 3;
        
    [Range(0.2f, 2f)]
    [SerializeField] private float animationSpeed;
    
    private Vector3[] cardPositions;
    private Vector3[] cardRotations;
    
    private List<SpawnCard> cardsOnHand;
    private Queue<SpawnCardInfo> cardsOnDeck;
    
    private readonly int MaxCardCapacityOnHand = 4;
    
    private void InitializeDeck()
    {
        cardsOnHand = new List<SpawnCard>();
        cardsOnDeck = new Queue<SpawnCardInfo>();
        
        var spawnCardsCache = spawnCards.ToList();

        Shuffle(spawnCardsCache);
    }
    
    public void Shuffle(List<SpawnCardInfo> cards)
    {
        cards.ShuffleList();
        
        foreach (var card in cards)
        {
            cardsOnDeck.Enqueue(card);
        }
    }

    public void DrawCards()
    {
        var cardVoids = MaxCardCapacityOnHand - cardsOnHand.Count;
            
        if (cardVoids < 1)
        {
            Debug.Log($"no space on hand left!");
            return;
        }
        
        for (var i = 0; i < cardVoids; i++)
        {
            SpawnCard pokerCard = Instantiate(spawnCardPrefab);

            if (!TryDrawCard(out var spawnCard))
                return;
            
            pokerCard.SetData(spawnCard);  

            AddCard(pokerCard);
        }
    }
    
    private bool TryDrawCard(out SpawnCardInfo card)
    {
        card = new();
        
        if (cardsOnDeck.Count < 1)
            return false;

        card = cardsOnDeck.Dequeue();
            
        return true;
    }
    
    public void AddCard(SpawnCard pokerCard)
    {
        cardsOnHand.Add(pokerCard);
        
        //덱에서 나오는 연출을 위해 위치 초기 설정
        pokerCard.transform.SetParent(hand);
        pokerCard.transform.localPosition = deck.transform.localPosition;
        pokerCard.transform.localScale = Vector3.one;

        ArrangeHand(animationSpeed);
        
        //TODO 카드 이벤트 추가는 어레인지 마치고 넣어준다
       // StartCoroutine(ListenCardEvents(pokerCard));
    }
    
    private void ArrangeHand(float duration)
    {
            cardPositions = new Vector3[cardsOnHand.Count];
            cardRotations = new Vector3[cardsOnHand.Count];
            float xspace = cardXSpacing / 2;
            float yspace = 0;
            float angle = cardAngle;
            int mid = cardsOnHand.Count / 2;

            if (cardsOnHand.Count % 2 == 1)
            {
                cardPositions[mid] = new Vector3(0, 0, 0);
                cardRotations[mid] = new Vector3(0, 0, 0);

                RelocateCard(cardsOnHand[mid], 0, 0, 0, duration);
                mid++;
                xspace = cardXSpacing;
                yspace = -cardYSpacing;
            }

            for (int i = mid; i < cardsOnHand.Count; i++)
            {
                cardPositions[i] = new Vector3(xspace, yspace, 0);
                cardRotations[i] = new Vector3(0, 0, -angle);
                cardPositions[cardsOnHand.Count - i - 1] = new Vector3(-xspace, yspace, 0);
                cardRotations[cardsOnHand.Count - i - 1] = new Vector3(0, 0, angle);

                RelocateCard(cardsOnHand[i], xspace, yspace, -angle, duration);
                RelocateCard(cardsOnHand[cardsOnHand.Count - i - 1], -xspace, yspace, angle, duration);
                
                cardsOnHand[cardsOnHand.Count - i - 1].SetCardOrder(cardsOnHand.Count - i - 1);
                cardsOnHand[i].SetCardOrder(i);

                xspace += cardXSpacing;
                yspace -= cardYSpacing;
                yspace *= 1.5f;
                angle += cardAngle;
            }
    }
        
    private void RelocateCard(SpawnCard card, float x, float y, float angle, float duration)
    {
        PositionCard(card, x, y, duration);
        RotateCard(card, angle, duration);
    }
        
    private void PositionCard(SpawnCard card, float x, float y, float duration)
    {
        card.transform.TweenMove(new Vector3(x, cardY + y, 0), duration);
    }
    private void RotateCard(SpawnCard card, float angle, float duration)
    {
        card.transform.TweenRotate(new Vector3(0, 0, angle), duration);
    }

}
