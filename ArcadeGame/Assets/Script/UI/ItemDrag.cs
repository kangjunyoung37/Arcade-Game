using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float DragAlpha = 0.6f;

    public static ItemDrag CurrrentlyDraggedItem;

    [Header("Item Footprint")]
    public int width = 2;
    public int height = 2;

    [Header("Placement")]
    public InventoryGrid _myGrid;
    public int currentGridX;
    public int currentGridY;

    [Header("Drag")]
    [SerializeField] private RectTransform dragLayer;
    public Transform itemVisual;

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;

    private bool _isDragging;
    private int _previewWidth;
    private int _previewHeight;

    private Vector2 _grabOffset;
    private float _savedAlpha;
    private bool _savedBlocksRaycasts;

    public bool IsPlaced { get; private set; }
    public bool IsDragging => _isDragging;

    private void Awake()
    {
        CacheComponents();

        if (_myGrid == null)
            _myGrid = GetComponentInParent<InventoryGrid>();
    }

    private void CacheComponents()
    {
        // 비활성 UI에서 초기 연결되는 경우도 지원합니다.
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public bool TryInitialize( InventoryGrid initialGrid, int initialWidth, int initialHeight)
    {
        CacheComponents();

        if (IsPlaced)
        {
            if (!_isDragging)
                RestoreCommittedPlacement();

            return true;
        }

        if (initialGrid == null)
            return false;

        bool placed = initialGrid.TryPlaceItem( this,currentGridX, currentGridY, initialWidth, initialHeight);

        if (!placed && initialGrid.TryFindSpace(initialWidth, initialHeight, out Vector2Int freePosition))
        {
            placed = initialGrid.TryPlaceItem( this, freePosition.x, freePosition.y, initialWidth, initialHeight);
        }

        if (!placed)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        return placed;
    }

    internal void ApplyPlacement( InventoryGrid grid, int x, int y, int itemWidth, int itemHeight)
    {
        CacheComponents();

        _myGrid = grid;
        currentGridX = x;
        currentGridY = y;

        // 확정된 크기만 여기에 저장합니다.
        width = itemWidth;
        height = itemHeight;
        IsPlaced = true;

        RestoreCommittedPlacement();

        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || CurrrentlyDraggedItem != null || !IsPlaced || _myGrid == null || dragLayer == null || !dragLayer.gameObject.activeInHierarchy)
        {
            return;
        }

        CacheComponents();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle( dragLayer, eventData.position, eventData.pressEventCamera, out Vector2 localPointer))
        {
            return;
        }

        _previewWidth = width;
        _previewHeight = height;

        _savedAlpha = _canvasGroup.alpha;
        _savedBlocksRaycasts = _canvasGroup.blocksRaycasts;

        _isDragging = true;
        CurrrentlyDraggedItem = this;

        // UI만 드래그 레이어로 옮깁니다.
        // 원래 그리드의 비트마스크와 소유 배열은 유지합니다.
        _rect.SetParent(dragLayer, true);
        _rect.SetAsLastSibling();

        _grabOffset = (Vector2)dragLayer.InverseTransformPoint(_rect.position) - localPointer;

        _canvasGroup.alpha = DragAlpha;
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || dragLayer == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle( dragLayer, eventData.position, eventData.pressEventCamera,out Vector2 localPointer))
        {
            _rect.anchoredPosition = localPointer + _grabOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        InventoryGrid targetGrid = null;

        // 매 프레임이 아니라 드롭한 순간에 대상 보관함을 찾습니다.
        if (eventData.pointerEnter != null)
        {
            targetGrid = eventData.pointerEnter.GetComponentInParent<InventoryGrid>();
        }

        bool placed = false;

        FinishDragState();

        if (targetGrid != null & targetGrid.isActiveAndEnabled && targetGrid.ContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint( targetGrid.ContainerRect, eventData.position, eventData.pressEventCamera))
        {
            Vector2 targetPosition = targetGrid.ContainerRect.InverseTransformPoint(_rect.position);
            Vector2Int cell = targetGrid.GetIndexFromPosition(targetPosition);
            placed = targetGrid.TryPlaceItem( this, cell.x, cell.y, _previewWidth, _previewHeight);
        }

        if (!placed)
            RestoreCommittedPlacement();
    }

    public void RotateItem()
    {
        if (!_isDragging || _myGrid == null)
            return;

        int previousWidth = _previewWidth;
        _previewWidth = _previewHeight;
        _previewHeight = previousWidth;

        // 확정 width/height는 변경하지 않습니다.
        _rect.sizeDelta = _myGrid.GetItemSize(_previewWidth, _previewHeight);
    }

    public void CancelDrag()
    {
        if (!_isDragging)
            return;

        FinishDragState();
        RestoreCommittedPlacement();
    }

    private void FinishDragState()
    {
        _isDragging = false;

        if (CurrrentlyDraggedItem == this)
            CurrrentlyDraggedItem = null;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _savedAlpha;
            _canvasGroup.blocksRaycasts = _savedBlocksRaycasts;
        }
    }

    private void RestoreCommittedPlacement()
    {
        if (!IsPlaced || _myGrid == null || _myGrid.ContainerRect == null || _rect == null)
        {
            return;
        }

        _rect.SetParent(_myGrid.ContainerRect, false);

        _rect.anchorMin = new Vector2(0f, 1f);
        _rect.anchorMax = new Vector2(0f, 1f);
        _rect.pivot = new Vector2(0f, 1f);

        _rect.localScale = Vector3.one;
        _rect.localRotation = Quaternion.identity;

        _rect.anchoredPosition =
            _myGrid.GetPositionFromIndex(currentGridX, currentGridY);

        _rect.sizeDelta =
            _myGrid.GetItemSize(width, height);
    }

    private void OnDisable()
    {
        CancelDrag();
    }

    private void OnDestroy()
    {
        if (CurrrentlyDraggedItem == this)
            CurrrentlyDraggedItem = null;

        if (IsPlaced && _myGrid != null)
            _myGrid.ForgetDestroyedItem(this);
    }
}