using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public static ItemDrag CurrrentlyDraggedItem;
    
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    
    private Vector2 _originalPosition;

    public int width = 2;
    public int height = 2;
    public InventoryGrid _myGrid;
    public int currentGridX;
    public int currentGridY;
    public Transform itemVisual;
    private bool _isDragging = false;
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        
        _canvas = GetComponentInParent<Canvas>();
        _myGrid = GetComponentInParent<InventoryGrid>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        CurrrentlyDraggedItem = this;
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
        CurrrentlyDraggedItem = null;
        InventoryGrid targetGrid = eventData.pointerEnter?.GetComponentInParent<InventoryGrid>();
        if (targetGrid)
        {
            Debug.Log($"마우스가 놓인 실제 픽셀 좌표: {_rectTransform.localPosition.y}");
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

    public void RotateItem()
    {
        (width, height) = (height, width);
        Vector2 currentSize = _rectTransform.sizeDelta;
        _rectTransform.sizeDelta = new Vector2(currentSize.y, currentSize.x);
        //비주얼 아이템 넣으면 하기
        //if(itemVisual) itemVisual.Rotate(0,0,-90f);
    }
}
