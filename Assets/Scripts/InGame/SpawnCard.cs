using System;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnCard : MonoBehaviour
{
    [SerializeField] private GameObject Visuals;
    
    private PlayerCharacter.SpawnCardInfo cardInfo;

    private bool isFloating;
    public bool IsFloating => isFloating;
    
    private Vector3 basePosition = Vector3.zero;
    
    public Action<SpawnCard> OnClick;
    
    public void SetData(PlayerCharacter.SpawnCardInfo card)
    {
        cardInfo = card;
    }
    
    public void SetCardOrder(int order)
    {
       // imgBG.sortingOrder = order;
    }
    
    public void OnMouseUpAsButton()
    {
        OnClick?.Invoke(this);
    }
    
    public void SetVisualsPosition(Vector3 newPos)
    {
        Visuals.transform.localPosition = newPos;
        basePosition = newPos;
    }
    
    public void SetFloating(bool isEnable)
    {
        isFloating = isEnable;
        if (!isFloating)
        {
            Visuals.transform.localPosition = basePosition;
        }
    }
}
