using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class InfiniteReserveAmmo
{
    private readonly AmmoMagazine _magazine;
    private readonly AmmoChamber _chamber;
    private readonly BulletData _reserveAmmo;
    private readonly float _reloadDuration;

    private float _remainingReloadTime;
    public bool IsReloading {get; private set;}

    public bool CanFire => !IsReloading && _chamber.HasLiveRound;
    public int MagazineCount => _magazine.Count;
    public int TotalLoadedRounds => _magazine.Count + (_chamber.HasLiveRound ? 1 : 0);

    public InfiniteReserveAmmo(AmmoCaliberData caliber, int magazineCapacity, BulletData reserveAmmo, float reloadDuration)
    {
        if(caliber == null)
        {
            throw new ArgumentException(nameof(caliber));
        }
        if(reserveAmmo == null || reserveAmmo.caliber != caliber)
        {
            throw new ArgumentException("Reserve ammunition must match the magazine caliber.",nameof(reserveAmmo));
        }
        _magazine = new AmmoMagazine(caliber, magazineCapacity);
        _chamber = new AmmoChamber(caliber);
        _reserveAmmo = reserveAmmo;
        _reloadDuration = Mathf.Max(WeaponData.MinimumShotInterval, reloadDuration);
        FillMagazine();
        _chamber.TryFeedFromMagazine(_magazine);
    }

    public void Tick(float deltaTime)
    {
        if(!IsReloading || deltaTime <= 0f)
        {
            return;
        }
        _remainingReloadTime -= deltaTime;
        if(_remainingReloadTime > 0f)
        {
            return;
        }
        CompleteReload();
    }

    public bool TryConsumeRound(out BulletData firedAmmo)
    {
        firedAmmo = null;
        if (!CanFire)
        {
            return false;
        }
        if(!_chamber.TryConsumeRound(out firedAmmo))
        {
            return false;
        }

        _chamber.TryEject(out _);

        if(!_chamber.TryFeedFromMagazine(_magazine))
        {
            BeginReload();
        }
        return true;
    }
    private void BeginReload()
    {
        if (IsReloading)
        {
            return;
        }
        IsReloading = true;
        _remainingReloadTime = _reloadDuration;
    }

    private void CompleteReload()
    {
        FillMagazine();
        _chamber.TryFeedFromMagazine(_magazine);
        _remainingReloadTime = 0f;
        IsReloading = false;
    }

    private void FillMagazine()
    {
        while (!_magazine.IsFull)
        {
            if (!_magazine.TryLoadRound(_reserveAmmo))
            {
                break;
            }
        }
    }
}
