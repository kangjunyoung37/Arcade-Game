using System;

/// <summary>
/// 같은 종류의 낱개 탄약을 보관하는 실제 묶음입니다.
/// </summary>
public sealed class LooseAmmoStack : IInventoryItemLock
{
    private object _reservationOwner;

    public BulletData Ammo { get; }
    public int Count { get; private set; }

    public bool IsEmpty => Count == 0;
    public bool IsReserved => _reservationOwner != null;

    public event Action Changed;

    public LooseAmmoStack(BulletData ammo, int count)
    {
        if (ammo == null)
            throw new ArgumentNullException(nameof(ammo));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        Ammo = ammo;
        Count = count;
    }


    // 성공한 경우에만 탄약 한 발을 실제로 옮깁니다.
    // 예약된 묶음은 예약 소유자만 사용할 수 있습니다.
    public bool TryLoadOneInto(AmmoMagazine magazine, object reservationOwner = null)
    {
        if (magazine == null || IsEmpty || Ammo == null)
            return false;

        if (IsReserved && !IsReservedBy(reservationOwner))
            return false;

        if (!magazine.TryLoadRound(Ammo))
            return false;

        Count--;

        // 탄창 증가와 원본 감소가 모두 끝난 후 알립니다.
        Changed?.Invoke();
        return true;
    }

    public bool TryReserve(object owner)
    {
        if (owner == null || IsReserved)
            return false;

        _reservationOwner = owner;
        return true;
    }

    public bool IsReservedBy(object owner)
    {
        return owner != null && ReferenceEquals(_reservationOwner, owner);
    }

    public bool ReleaseReservation(object owner)
    {
        if (!IsReservedBy(owner))
            return false;

        _reservationOwner = null;
        return true;
    }
}