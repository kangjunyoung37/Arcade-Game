using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Character), typeof(PlayerWeapon))]
public sealed class PlayerWeaponController : MonoBehaviour
{
    private const int FireMouseButton = 0;

    private Character _owner;
    private WeaponData _weapon;
    private FireMode _mode;
    private PlayerWeapon _playerWeapon;
    private int _remainingBurstShots;
    private uint _observedMagazineRevision;
    private float _nextShotTime = float.NegativeInfinity;

    private void Awake()
    {
        if (!TryGetComponent(out _owner) || !TryGetComponent(out _playerWeapon))
            enabled = false;
        
    }

    private void Update()
    {
        float now = Time.time;

        if (_owner == null || _playerWeapon == null)
        {
            CancelBurst(now);
            if (_playerWeapon != null)
            _playerWeapon.CancelReload();
            return;
        }

        if (_observedMagazineRevision != _playerWeapon.MagazineRevision)
        {
            CancelBurst(now);
            _observedMagazineRevision = _playerWeapon.MagazineRevision;
        }
        
        WeaponData currentWeapon = _playerWeapon.Data;

        // 무기 또는 발사 모드가 바뀌면 진행 중인 점사를 취소합니다.
        if (currentWeapon != _weapon || (currentWeapon != null && currentWeapon.fireMode != _mode))
        {
            CancelBurst(now);
            _playerWeapon.CancelReload();
            _weapon = currentWeapon;

            if (_weapon != null)
                _mode = _weapon.fireMode;
        }
        

        if (_weapon == null || !_owner.CanUseWeapon || !_playerWeapon.isActiveAndEnabled)
        {
            CancelBurst(now);
            _playerWeapon.CancelReload();
            return;
        }
        
        // 재장전 중에는 기존 점사를 취소하고 입력 처리를 중단합니다.
        if (_playerWeapon.IsReloading)
        {
            CancelBurst(now);
            _playerWeapon.TickReload(Time.deltaTime);
            return;
        }

        if (Time.deltaTime <= 0f)
            return;

        if (now < _nextShotTime)
            return;

        // 시작된 점사는 버튼을 놓아도 남은 발수를 처리합니다.
        if (_remainingBurstShots > 0)
        {
            TryExecuteShot(now);
            return;
        }

        switch (_mode)
        {
            case FireMode.Single:
                if (Input.GetMouseButtonDown(FireMouseButton))
                    TryExecuteShot(now);
                break;

            case FireMode.Auto:
                if (Input.GetMouseButton(FireMouseButton))
                    TryExecuteShot(now);
                break;

            case FireMode.Burst:
                if (Input.GetMouseButtonDown(FireMouseButton))
                {
                    _remainingBurstShots = _weapon.GetShotsPerCycle(_mode);

                    TryExecuteShot(now);
                }
                break;
        }
    }

    private void TryExecuteShot(float now)
    {
        // 실제 발사에 실패하면 남은 점사도 취소합니다.
        if (!_playerWeapon.TryFire())
        {
            CancelBurst(now);
            return;
        }

        if (_mode == FireMode.Burst)
        {
            _remainingBurstShots = Mathf.Max(0, _remainingBurstShots - 1);

            float delay = _remainingBurstShots > 0 ? _weapon.GetShotInterval(_mode) : _weapon.GetCycleCooldown(_mode);
            _nextShotTime = now + delay;
            return;
        }

        _nextShotTime = now + _weapon.GetShotInterval(_mode);
    }

    private void CancelBurst(float now)
    {
        if (_remainingBurstShots <= 0)
            return;

        if (_weapon != null)
        {
            // 취소 직후 다시 클릭해서 점사 딜레이를 우회하지 못하게 합니다.
            _nextShotTime = Mathf.Max( _nextShotTime, now + _weapon.GetCycleCooldown(FireMode.Burst));
        }

        _remainingBurstShots = 0;
    }

    private void OnDisable()
    {
        CancelBurst(Time.time);
        if (_playerWeapon != null)
            _playerWeapon.CancelReload();
    }
}