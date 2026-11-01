using UnityEngine;

public sealed class AmmoMagazineTest : MonoBehaviour
{
    private const int TestCapacity = 3;

    [SerializeField] private AmmoCaliberData caliber;
    [SerializeField] private BulletData normalAmmo;
    [SerializeField] private BulletData piercingAmmo;

#if UNITY_EDITOR
    private void Start()
    {
        if (caliber == null ||
            normalAmmo == null ||
            piercingAmmo == null)
        {
            Debug.LogError("테스트용 탄약과 규격을 연결해 주세요.", this);
            return;
        }

        AmmoMagazine magazine =
            new AmmoMagazine(caliber, TestCapacity);

        if (!magazine.IsCompatible(normalAmmo) ||
            !magazine.IsCompatible(piercingAmmo))
        {
            Debug.LogError("탄약과 탄창의 규격이 다릅니다.", this);
            return;
        }

        magazine.TryLoadRound(normalAmmo);
        magazine.TryLoadRound(normalAmmo);
        magazine.TryLoadRound(piercingAmmo);

        Debug.Log($"장전 후 잔탄: {magazine.Count}", this);

        bool loadedExtra = magazine.TryLoadRound(normalAmmo);
        Debug.Log($"가득 찬 탄창에 추가 장전: {loadedExtra}", this);

        if (magazine.TryPeekNextRound(out BulletData nextAmmo))
        {
            Debug.Log(
                $"다음 탄약: {nextAmmo.name}, 잔탄: {magazine.Count}",
                this);
        }

        while (magazine.TryTakeRound(out BulletData ammo))
        {
            Debug.Log(
                $"꺼낸 탄약: {ammo.name}, 남은 탄약: {magazine.Count}",
                this);
        }

        Debug.Log($"탄창이 비었는가: {magazine.IsEmpty}", this);
    }
#endif
}