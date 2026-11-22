using System;


/// 같은 종류의 낱개 탄약 묶음입니다.
public sealed class LooseAmmoStack
{
    public BulletData Ammo { get; }
    public int Count { get; private set; }

    public bool IsEmpty => Count == 0;

    public LooseAmmoStack(BulletData ammo, int count)
    {
        if (ammo == null)
            throw new ArgumentNullException(nameof(ammo));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        Ammo = ammo;
        Count = count;
    }


    // 이 탄약 묶음에서 대상 탄창으로 정확히 한 발 이동합니다.
    // 실패하면 양쪽의 탄약 수는 변하지 않습니다.
    public bool TryLoadOneInto(AmmoMagazine magazine)
    {
        if (magazine == null || IsEmpty || Ammo == null)
            return false;

        // 탄창 가득 참, 구경 불일치 등 AmmoMagazine이 검사합니다.
        if (!magazine.TryLoadRound(Ammo))
            return false;

        // 탄창에 실제로 들어간 경우에만 원본 수량을 줄입니다.
        Count--;
        return true;
    }
}