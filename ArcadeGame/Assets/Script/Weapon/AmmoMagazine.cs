using System;


public sealed class AmmoMagazine
{
    private readonly BulletData[] _rounds;

    public AmmoCaliberData Caliber{ get; }

    public int Capacity => _rounds.Length;
    public int Count { get; private set; }

    public bool IsEmpty => Count == 0;
    public bool IsFull => Count >= Capacity;

    public AmmoMagazine(AmmoCaliberData caliber, int capacity)
    {
        if(caliber == null) throw new ArgumentNullException(nameof(caliber));
        if(capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Magazine capacity must be greater than zero.");
        Caliber = caliber;
        _rounds = new BulletData[capacity];
    }

    public bool IsCompatible(BulletData ammo)
    {
        return ammo != null && Caliber != null && ammo.caliber == Caliber;
    }

    public bool TryLoadRound(BulletData ammo)
    {
        if(IsFull || !IsCompatible(ammo)) return false;
        _rounds[Count] = ammo;
        Count++;
        return true;
    }

    public bool TryPeekNextRound(out BulletData ammo)
    {
        ammo = null;
        if(IsEmpty) return false;
        ammo = _rounds[Count - 1];
        return ammo != null;
    }
    
    public bool TryTakeRound(out BulletData ammo)
    {
        if(!TryPeekNextRound(out ammo)) return false;
        Count--;
        _rounds[Count] = null;
        return true;
    }

}
