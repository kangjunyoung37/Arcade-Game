
// 탄창 하나를 보관하는 런타임 슬롯입니다.
// 재장전 중에는 특정 작업자가 슬롯을 예약합니다.
public sealed class MagazineSlot
{
    private AmmoMagazine _magazine;
    private object _reservationOwner;

    public bool HasMagazine => _magazine != null;
    public int RoundCount => _magazine != null ? _magazine.Count : 0;

    public bool IsReserved => _reservationOwner != null;

    // 무기 시스템 내부의 호환성 검사에 사용합니다.
    internal AmmoMagazine Magazine => _magazine;


    // 전달받은 탄창을 슬롯으로 이동합니다.
    // 호출자의 변수는 null이 됩니다.
    public MagazineSlot(ref AmmoMagazine magazine)
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

    // 예약한 작업자만 슬롯과 무기의 탄창을 교환할 수 있습니다.
    internal bool TryExchangeReserved(object owner,ref AmmoMagazine otherMagazine)
    {
        if (owner == null || !ReferenceEquals(_reservationOwner, owner))
        {
            return false;
        }

        AmmoMagazine previous = _magazine;
        _magazine = otherMagazine;
        otherMagazine = previous;

        return true;
    }

    internal void ReleaseReservation(object owner)
    {
        if (owner != null &&
            ReferenceEquals(_reservationOwner, owner))
        {
            _reservationOwner = null;
        }
    }
}
