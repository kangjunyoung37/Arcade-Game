using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagazineReloadReservationDebug : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private MagazineItemView incomingView;
    [SerializeField] private ItemDrag incomingDrag;

    [Tooltip("기존 장착 탄창의 크기를 흉내 낼 데이터입니다.")]
    [SerializeField] private MagazineData returningMagazineData;

    [SerializeField] private InventoryContainer returnContainer;
    private readonly MagazineReloadReservation _reservation =
        new MagazineReloadReservation();

    [ContextMenu("Debug/Prepare Reload Reservation")]
    private void DebugPrepare()
    {
        if (!Application.isPlaying)
            return;

        bool prepared = _reservation.TryPrepare(
            incomingView,
            incomingDrag,
            returningMagazineData,
            returnContainer);

        if (!prepared)
        {
            Debug.Log(
                $"준비 실패. 기존 준비 유지 여부: {_reservation.IsPrepared}",
                this);
            return;
        }

        InventoryGrid grid = _reservation.ReturnGrid;

        if (grid == null)
        {
            Debug.Log(
                "준비 성공: 탄창 예약 완료. 반환할 탄창은 없습니다.",
                this);
            return;
        }

        Debug.Log(
            $"준비 성공: 반환 그리드={grid.name}, " +
            $"좌표={_reservation.ReturnPosition}, " +
            $"크기={_reservation.ReturnSize}",
            this);
    }

    [ContextMenu("Debug/Cancel Reload Reservation")]
    private void DebugCancel()
    {
        if (!Application.isPlaying)
            return;

        _reservation.Cancel();

        Debug.Log("준비 취소: 탄창과 반환 공간 예약 해제.", this);
    }

    private void OnDisable()
    {
        _reservation.Cancel();
    }

    private void OnDestroy()
    {
        _reservation.Cancel();
    }
#endif
}