using System;


public enum ChamberState
{
    Empty,
    Loaded,
    Spent
}
public sealed class AmmoChamber
{
    private BulletData _loadedRound;
    
    public AmmoCaliberData Caliber {get;}
    public ChamberState State {get; private set;}

    public bool IsEmpty => State == ChamberState.Empty;
    public bool HasLiveRound => State == ChamberState.Loaded && _loadedRound != null;
    public bool HasSpentCase => State == ChamberState.Spent;

    public AmmoChamber(AmmoCaliberData caliber)
    {
        if(caliber == null)
        {
            throw new ArgumentNullException(nameof(caliber));
        }
        Caliber = caliber;
        State = ChamberState.Empty;
    }
    public bool IsCompatible(BulletData ammo)
    {
        return ammo != null && Caliber != null && ammo.caliber == Caliber;
    }

    //약실에 탄약 한 발을 직접 넣음.
    public bool TryLoadRound(BulletData ammo)
    {
        if(!IsEmpty || !IsCompatible(ammo))
        {
            return false;
        }
        _loadedRound = ammo;
        State = ChamberState.Loaded;
        return true;
    }
    //탄창에서 한 발을 꺼내 약실로 옮김.
    public bool TryFeedFromMagazine(AmmoMagazine magazine)
    {
        if(!IsEmpty || magazine == null || Caliber == null || magazine.Caliber != Caliber)
            return false;
        if(!magazine.TryPeekNextRound(out BulletData nextRound) || !IsCompatible(nextRound))
            return false;
        if(!magazine.TryTakeRound(out BulletData takenRound))
            return false;
        _loadedRound = takenRound;
        State = ChamberState.Loaded;
        return true;
    }
    // 발사에 사용할 탄약을 소비하고 탄피 상태로 변경.
    public bool TryConsumeRound(out BulletData firedAmmo)
    {
        firedAmmo = null;
        if (!HasLiveRound)
        {
            return false;
        }
        firedAmmo = _loadedRound;
        _loadedRound = null;
        State = ChamberState.Spent;
        return true;
    }
    // 미발사 탄약 또는 사용한 탄피를 배출.
    public bool TryEject(out BulletData liveRound)
    {
        liveRound = null;
        if(IsEmpty)
        {
            return false;
        }
        if(State == ChamberState.Loaded)
        {
            liveRound = _loadedRound;
        }
        _loadedRound = null;
        State = ChamberState.Empty;
        return true;
    }
    
}
