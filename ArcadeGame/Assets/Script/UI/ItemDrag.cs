using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    
    private Vector2 _originalPosition;

    public int width = 2;
    public int height = 2;
    public InventoryGrid _myGrid;
    public int currentGridX;
    public int currentGridY;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        
        _canvas = GetComponentInParent<Canvas>();
        _myGrid = GetComponentInParent<InventoryGrid>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalPosition = _rectTransform.anchoredPosition;
        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;
        if (_myGrid != null)
        {
            _myGrid.RemoveItem(this,currentGridX,currentGridY);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        
        InventoryGrid targetGrid = eventData.pointerEnter?.GetComponentInParent<InventoryGrid>();
        if (targetGrid)
        {
            Vector2Int dropIndex = targetGrid.GetIndexFromPosition(_rectTransform.localPosition);
            if (targetGrid.CanPlaceItem(dropIndex.x, dropIndex.y, this.width, this.height))
            {
                transform.SetParent(targetGrid.itemContainer);
                _rectTransform.localPosition = targetGrid.GetPositionFromIndex(dropIndex.x, dropIndex.y);
                targetGrid.AddItem(this,dropIndex.x,dropIndex.y);

                _myGrid = targetGrid;
                currentGridX = dropIndex.x;
                currentGridY = dropIndex.y;
                return;
            }
        }
        _rectTransform.localPosition = _originalPosition;
        if (_myGrid)
        {
            _myGrid.AddItem(this,currentGridX,currentGridY);
        }
    }
}
