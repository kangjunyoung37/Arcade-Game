using UnityEngine;

public sealed class AmmoChamberTest : MonoBehaviour
{
    private const int TestCapacity = 2;

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
            Debug.LogError("테스트용 규격과 탄약을 연결해 주세요.", this);
            return;
        }

        AmmoMagazine magazine =
            new AmmoMagazine(caliber, TestCapacity);

        AmmoChamber chamber = new AmmoChamber(caliber);

        if (!magazine.IsCompatible(normalAmmo) ||
            !magazine.IsCompatible(piercingAmmo))
        {
            Debug.LogError("탄약 규격이 일치하지 않습니다.", this);
            return;
        }

        magazine.TryLoadRound(normalAmmo);
        magazine.TryLoadRound(piercingAmmo);

        bool fed = chamber.TryFeedFromMagazine(magazine);
        Debug.Log(
            $"공급: {fed}, 탄창: {magazine.Count}, 약실: {chamber.State}",
            this);

        bool fedAgain = chamber.TryFeedFromMagazine(magazine);
        Debug.Log(
            $"중복 공급: {fedAgain}, 탄창: {magazine.Count}",
            this);

        if (chamber.TryConsumeRound(out BulletData firedAmmo))
        {
            Debug.Log(
                $"소비한 탄약: {firedAmmo.name}, 약실: {chamber.State}",
                this);
        }

        bool firedAgain = chamber.TryConsumeRound(out _);
        Debug.Log($"탄피 상태에서 재발사: {firedAgain}", this);

        bool fedWithCase = chamber.TryFeedFromMagazine(magazine);
        Debug.Log(
            $"탄피가 남은 상태에서 공급: {fedWithCase}",
            this);

        bool ejected = chamber.TryEject(out BulletData recoveredAmmo);
        Debug.Log(
            $"탄피 배출: {ejected}, 회수한 실탄 있음: {recoveredAmmo != null}",
            this);

        bool fedNext = chamber.TryFeedFromMagazine(magazine);
        Debug.Log(
            $"다음 탄약 공급: {fedNext}, 탄창: {magazine.Count}",
            this);

        if (chamber.TryEject(out BulletData liveRound) &&
            liveRound != null)
        {
            // 테스트에서는 꺼낸 실탄을 탄창으로 되돌림.
            bool returned = magazine.TryLoadRound(liveRound);

            Debug.Log(
                $"미발사 탄약: {liveRound.name}, 탄창 반환: {returned}",
                this);
        }
    }
#endif
}