using UnityEngine;

public sealed class MagazineItemInstanceTest : MonoBehaviour
{
    private const int StartingLooseRounds = 4;
    private const int MinimumTestCapacity = 2;

    [SerializeField] private MagazineData magazineData;
    [SerializeField] private BulletData testAmmo;
    [SerializeField] private WeaponData testWeapon;

#if UNITY_EDITOR
    [ContextMenu("Run Magazine Item Test")]
    private void RunTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("플레이 모드에서 실행해 주세요.", this);
            return;
        }

        if (magazineData == null ||
            !magazineData.IsValid ||
            magazineData.Capacity < MinimumTestCapacity ||
            testAmmo == null ||
            testAmmo.caliber != magazineData.Caliber ||
            testWeapon == null)
        {
            Debug.LogError(
                "탄창 설정, 탄약 구경, 테스트 무기를 확인해 주세요.",
                this);
            return;
        }

        if (!testWeapon.IsMagazineCompatible(magazineData))
        {
            Debug.LogError(
                "무기와 탄창의 구경 또는 장착 규격이 일치하지 않습니다.",
                this);
            return;
        }

        // 같은 종류이지만 서로 다른 실제 탄창입니다.
        MagazineItemInstance magazineA =
            new MagazineItemInstance(magazineData);

        MagazineItemInstance magazineB =
            new MagazineItemInstance(magazineData);

        LooseAmmoStack looseAmmo =
            new LooseAmmoStack(testAmmo, StartingLooseRounds);

        bool loadedAFirst = magazineA.TryLoadOneFrom(looseAmmo);
        bool loadedASecond = magazineA.TryLoadOneFrom(looseAmmo);
        bool loadedB = magazineB.TryLoadOneFrom(looseAmmo);

        bool differentItems =
            magazineA.InstanceId != magazineB.InstanceId;

        bool sharedDefinition =
            ReferenceEquals(magazineA.Data, magazineB.Data);

        bool independentRounds =
            magazineA.RoundCount == 2 &&
            magazineB.RoundCount == 1 &&
            looseAmmo.Count == 1;

        bool correctNextAmmo =
            magazineA.TryPeekNextRound(out BulletData nextAmmo) &&
            nextAmmo == testAmmo;

        int totalRounds =
            magazineA.RoundCount +
            magazineB.RoundCount +
            looseAmmo.Count;

        bool passed =
            loadedAFirst &&
            loadedASecond &&
            loadedB &&
            differentItems &&
            sharedDefinition &&
            independentRounds &&
            correctNextAmmo &&
            totalRounds == StartingLooseRounds;

        Debug.Assert(
            passed,
            "탄창 아이템 독립성 또는 탄약 보존 테스트 실패",
            this);

        if (!passed)
            return;

        Debug.Log(
            $"테스트 통과 — A: {magazineA.RoundCount}, " +
            $"B: {magazineB.RoundCount}, 낱개: {looseAmmo.Count}, " +
            $"총 실탄: {totalRounds}, " +
            $"서로 다른 아이템: {differentItems}",
            this);
    }
#endif
}