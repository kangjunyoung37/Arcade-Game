using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponMuzzleFlash : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private ParticleSystem muzzleFlash;

    private PlayerWeapon _playerWeapon;
    private EnemyWeapon _enemyWeapon;

    private void Awake()
    {
        // 초기화할 때만 조회합니다.
        bool hasPlayer = TryGetComponent(out _playerWeapon);
        bool hasEnemy = TryGetComponent(out _enemyWeapon);

        // 플레이어/적 무기 중 정확히 하나가 필요합니다.
        if (hasPlayer == hasEnemy || muzzleFlash == null)
        {
            ReportConfigurationError();
            enabled = false;
            return;
        }

        ParticleSystem[] systems = muzzleFlash.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem system = systems[i];

            if (system == null)
                continue;

            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;

        }

        ClearEffect();
    }

    private void OnEnable()
    {
        if (_playerWeapon != null)
            _playerWeapon.Fired += HandlePlayerFired;

        if (_enemyWeapon != null)
            _enemyWeapon.Fired += PlayEffect;
    }

    private void OnDisable()
    {
        if (!ReferenceEquals(_playerWeapon, null))
            _playerWeapon.Fired -= HandlePlayerFired;

        if (!ReferenceEquals(_enemyWeapon, null))
            _enemyWeapon.Fired -= PlayEffect;

        ClearEffect();
    }

    private void HandlePlayerFired( Vector3 shotPosition, float shotSpread)
    {
        PlayEffect();
    }

    private void PlayEffect()
    {
        if (muzzleFlash == null ||!muzzleFlash.gameObject.activeInHierarchy)
            return;
        
        // 기존 입자는 남기고 방출 시간을 다시 시작합니다.
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        muzzleFlash.Play(true);
    }

    private void ClearEffect()
    {
        if (muzzleFlash == null)
            return;

        muzzleFlash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportConfigurationError()
    {
        Debug.LogError(
            "WeaponMuzzleFlash에는 동일 오브젝트의 " +
            "PlayerWeapon 또는 EnemyWeapon 중 하나와 " +
            "Muzzle Flash 연결이 필요합니다.",
            this);
    }
}