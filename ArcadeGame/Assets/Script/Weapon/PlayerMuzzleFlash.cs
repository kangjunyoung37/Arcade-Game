using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerWeapon))]
public sealed class PlayerMuzzleFlash : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private ParticleSystem muzzleFlash;

    private PlayerWeapon _weapon;

    private void Awake()
    {
        if (!TryGetComponent(out _weapon) || muzzleFlash == null)
        {
            ReportMissingReferences();
            enabled = false;
            return;
        }

        // 초기화 시 한 번만 자식 파티클까지 조회합니다.
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

            // 원본 프리팹의 Emission과 Burst 설정은 유지합니다.
        }

        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnEnable()
    {
        if (_weapon != null)
            _weapon.Fired += HandleWeaponFired;
    }

    private void OnDisable()
    {
        if (!ReferenceEquals(_weapon, null))
            _weapon.Fired -= HandleWeaponFired;

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void HandleWeaponFired(
        Vector3 shotPosition,
        float shotSpread)
    {
        if (muzzleFlash == null || !muzzleFlash.gameObject.activeInHierarchy)
        {
            return;
        }

        // 기존 입자를 지우지 않고 방출만 멈춥니다.
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 재생 시간을 처음부터 시작하여
        // 루트와 자식의 Burst를 다시 발생시킵니다.
        muzzleFlash.Play(true);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportMissingReferences()
    {
        Debug.LogError(
            "PlayerMuzzleFlash의 PlayerWeapon 또는 " +
            "Muzzle Flash 연결을 확인하세요.",
            this);
    }
}