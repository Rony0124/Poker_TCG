using UnityEngine;

public partial class PlayerCharacter : MonoBehaviour
{
    void Start()
    {
        InitializeDeck();
        
        DrawCards();
    }

    private void OnClickCard(SpawnCard card)
    {
        if (card.IsFloating)
        {
            card.SetVisualsPosition(Vector3.zero);
            card.SetFloating(false);
                
            var cardIdx = cardsOnHand.IndexOf(card);
            RotateCard(card, cardRotations[cardIdx].z, animationSpeed / 10);
                
            currentCardSelected.Remove(card);
        }
        else
        {
            if(currentCardSelected.Count >= HandCountMax)
                return;
                
            card.SetVisualsPosition(Vector3.up * 0.05f);
            card.SetFloating(true);
            
            RotateCard(card, 0, 0);
                
            currentCardSelected.Add(card);
        }
    }
}
