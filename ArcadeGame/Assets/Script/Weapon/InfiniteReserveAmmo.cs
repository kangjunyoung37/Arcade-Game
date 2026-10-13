using System;
using UnityEngine;

public enum AmmoSupplyPhase
{
    Ready,
    Cycling,
    Reloading
}
public sealed class InfiniteReserveAmmo
{
    private readonly AmmoMagazine _magazine;
    private readonly AmmoChamber _chamber;
    private readonly BulletData _reserveAmmo;
    private readonly float _reloadDuration;
    private readonly WeaponActionType _actionType;
    private readonly float _actionCycleDuration;
    private readonly WeaponReloadType _reloadType;
    private readonly float _insertRoundDuration;

    private float _remainingTime;

    public AmmoSupplyPhase Phase {get; private set;}
    public bool IsReloading => Phase == AmmoSupplyPhase.Reloading;
    public bool IsCycling => Phase == AmmoSupplyPhase.Cycling;

    public bool CanFire => Phase == AmmoSupplyPhase.Ready && _chamber.HasLiveRound;
    public int MagazineCount => _magazine.Count;
    public int TotalLoadedRounds => _magazine.Count + (_chamber.HasLiveRound ? 1 : 0);

    public event Action SpentCaseEjected;

    public InfiniteReserveAmmo(AmmoCaliberData caliber, int magazineCapacity, BulletData reserveAmmo, float reloadDuration, WeaponActionType actionType = WeaponActionType.SelfLoading, float actionCycleDuration = 0f, WeaponReloadType reloadType = WeaponReloadType.MagazineSwap, float insertRoundDuration = 0f)
    {
        if(caliber == null)
        {
            throw new ArgumentException(nameof(caliber));
        }
        if(reserveAmmo == null)
        {
            throw new ArgumentNullException(nameof(reserveAmmo));
        }
        if(reserveAmmo.caliber != caliber)
        {
            throw new ArgumentException("Reserve ammunition must match the magazine caliber.",nameof(reserveAmmo));
        }
        if(magazineCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(magazineCapacity));
        }
        if(actionType != WeaponActionType.SelfLoading && actionType != WeaponActionType.BoltAction && actionType != WeaponActionType.PumpAction)
        {
            throw new ArgumentOutOfRangeException(nameof(actionType));
        }

        if(reloadType != WeaponReloadType.MagazineSwap && reloadType != WeaponReloadType.InsertOneRound)
        {
            throw new ArgumentOutOfRangeException(nameof(reloadType));
        }

        _reloadType = reloadType;
        _insertRoundDuration = Mathf.Max(WeaponData.MinimumShotInterval, insertRoundDuration);

        _magazine = new AmmoMagazine(caliber, magazineCapacity);
        _chamber = new AmmoChamber(caliber);
        _reserveAmmo = reserveAmmo;
        _actionType = actionType;
        _reloadDuration = Mathf.Max(WeaponData.MinimumShotInterval, reloadDuration);
        _actionCycleDuration = Mathf.Max(WeaponData.MinimumShotInterval, actionCycleDuration);
        FillMagazine();
        _chamber.TryFeedFromMagazine(_magazine);
        Phase = AmmoSupplyPhase.Ready;
    }

    public void Tick(float deltaTime)
    {
        if(deltaTime <= 0f) return;

       while(Phase != AmmoSupplyPhase.Ready)
        {
            if(deltaTime < _remainingTime)
            {
                _remainingTime -= deltaTime;
                return;
            }
            deltaTime -= _remainingTime;
            _remainingTime = 0f;
            if(Phase == AmmoSupplyPhase.Cycling)
            {
                CompleteActionCycle();
            }
            else
            {
                CompleteReload();
            }
        }
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

        if(_actionType == WeaponActionType.SelfLoading)
        {
            CompleteActionCycle();
        }
        else
        {
            Phase = AmmoSupplyPhase.Cycling;
            _remainingTime = _actionCycleDuration;
        }
        return true;
    }

    private void CompleteActionCycle()
    {
        bool hadSpentCase = _chamber.HasSpentCase;
        bool ejected = _chamber.TryEject(out _);

        if (_chamber.TryFeedFromMagazine(_magazine))
        {
            Phase = AmmoSupplyPhase.Ready;
            _remainingTime = 0f;

        }
        else
        {
            BeginReload();
        }
        if(hadSpentCase && ejected)
        {
            SpentCaseEjected?.Invoke();
        }
    }
    private void BeginReload()
    {
        Phase = AmmoSupplyPhase.Reloading;
        _remainingTime = _reloadType == WeaponReloadType.InsertOneRound ? _insertRoundDuration : _reloadDuration;
    }

    private void CompleteReload()
    {
        if(_reloadType == WeaponReloadType.MagazineSwap)
        {
            FillMagazine();
            FinishReload();
            return;        
        }
        if (!_magazine.TryLoadRound(_reserveAmmo))
        {
            FinishReload();
            return;
        }
        if (_magazine.IsFull)
        {
            FinishReload();
            return;
        }
        _remainingTime = _insertRoundDuration;
    }
    
    private void FinishReload()
    {
        _chamber.TryFeedFromMagazine(_magazine);
        Phase = AmmoSupplyPhase.Ready;
        _remainingTime = 0f;
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
