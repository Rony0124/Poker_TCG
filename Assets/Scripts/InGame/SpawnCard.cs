using UnityEngine;

public class SpawnCard : MonoBehaviour
{
    private PlayerCharacter.SpawnCardInfo cardInfo;
    
    public void SetData(PlayerCharacter.SpawnCardInfo card)
    {
        cardInfo = card;
    }
    
    public void SetCardOrder(int order)
    {
       // imgBG.sortingOrder = order;
    }
}
