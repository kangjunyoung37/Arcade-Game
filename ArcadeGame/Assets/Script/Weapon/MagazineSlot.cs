// 재장전 테스트에 사용하는 탄창 보관 슬롯입니다.
// 실제 인벤토리 그리드의 칸과는 아직 연결되지 않았습니다.
public sealed class MagazineSlot
{
    private MagazineItemInstance _magazine;
    private object _reservationOwner;

    public bool HasMagazine => _magazine != null;

    public int RoundCount => _magazine != null ? _magazine.RoundCount : 0;

    public bool IsReserved => _reservationOwner != null;

    internal MagazineItemInstance Magazine => _magazine;

    // 호출자의 변수에서 슬롯으로 참조를 옮깁니다.
    public MagazineSlot(ref MagazineItemInstance magazine)
    {
        _magazine = magazine;
        magazine = null;
    }

    internal bool TryReserve(object owner)
    {
        if (owner == null || IsReserved)
            return false;

        _reservationOwner = owner;
        return true;
    }

    // 예약한 작업자만 탄창을 교환할 수 있습니다.
    internal bool TryExchangeReserved( object owner, ref MagazineItemInstance otherMagazine)
    {
        if (owner == null || !ReferenceEquals(_reservationOwner, owner))
            return false;
        

        MagazineItemInstance previous = _magazine;

        _magazine = otherMagazine;
        otherMagazine = previous;

        return true;
    }

    internal void ReleaseReservation(object owner)
    {
        if (owner != null && ReferenceEquals(_reservationOwner, owner))
        {
            _reservationOwner = null;
        }
    }
}