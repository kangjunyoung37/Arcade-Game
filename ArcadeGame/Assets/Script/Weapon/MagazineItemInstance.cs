using System;


// 실제 탄창 아이템 하나의 런타임 상태입니다.
// 같은 MagazineData를 사용해도 탄약 상태는 각각 독립적입니다.
public sealed class MagazineItemInstance : IInventoryItemLock
{
    private readonly AmmoMagazine _ammo;
    public event Action Changed;
    private object _reservationOwner;
    public Guid InstanceId { get; }
    public MagazineData Data { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    public int RoundCount => _ammo.Count;
    public int Capacity => _ammo.Capacity;
    public bool IsReserved => _reservationOwner != null;
    public bool IsEmpty => _ammo.IsEmpty;
    public bool IsFull => _ammo.IsFull;

    public MagazineItemInstance(MagazineData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (!data.IsValid)
            throw new ArgumentException( "Magazine settings are invalid.", nameof(data));
        

        InstanceId = Guid.NewGuid();
        Data = data;
        GridWidth = data.GridWidth;
        GridHeight = data.GridHeight;
        // 실제 탄약 보관 공간은 탄창마다 별도로 생성합니다.
        _ammo = new AmmoMagazine(data.Caliber, data.Capacity);
    }

    // 낱개 탄약 묶음에서 이 탄창으로 한 발 이동합니다.
    // 성공한 경우에만 원본 탄약 수가 줄어듭니다.
    public bool TryLoadOneFrom(LooseAmmoStack source)
    {
        if (source == null || IsReserved)
            return false;

        if (!source.TryLoadOneInto(_ammo))
            return false;

        Changed?.Invoke();
        return true;
    }

    // 다음에 공급될 탄약을 소비하지 않고 확인합니다.
    public bool TryPeekNextRound(out BulletData ammo)
    {
        return _ammo.TryPeekNextRound(out ammo);
    }

    /// <summary>
    /// 이 탄창에서 약실로 한 발을 옮깁니다.
    /// 실패하면 탄창과 약실의 탄약 상태는 유지됩니다.
    /// </summary>
    public bool TryFeedIntoChamber(AmmoChamber chamber, object reservationOwner = null)
    {
        if (chamber == null)
            return false;

        
        if (IsReserved && !IsReservedBy(reservationOwner))
            return false;

        if (!chamber.TryFeedFromMagazine(_ammo))
            return false;

        Changed?.Invoke();
        return true;
    }
    /// <summary>
    /// 다른 작업이 예약하지 않은 탄창을 예약합니다.
    /// 같은 작업자가 다시 요청해도 중복 예약은 거절합니다.
    /// </summary>
    public bool TryReserve(object owner)
    {
        if (owner == null || IsReserved)
            return false;

        _reservationOwner = owner;
        return true;
    }

    //해당 작업자가 이 탄창의 예약자인지 확인합니다.
    public bool IsReservedBy(object owner)
    {
        return owner != null &&
            ReferenceEquals(_reservationOwner, owner);
    }

    // 예약한 작업자만 예약을 해제할 수 있습니다.
    public bool ReleaseReservation(object owner)
    {
        if (!IsReservedBy(owner))
            return false;

        _reservationOwner = null;
        return true;
    }
}