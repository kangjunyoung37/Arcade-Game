using UnityEngine;

public sealed class DetachableMagazineTest : MonoBehaviour
{
    private const int TestMagazineCapacity = 2;
    private const int StartingLooseRounds = 5;

    [SerializeField] private BulletData testAmmo;

#if UNITY_EDITOR
    [ContextMenu("Run Magazine Data Test")]
    private void RunTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("플레이 모드에서 실행해 주세요.", this);
            return;
        }

        if (testAmmo == null || testAmmo.caliber == null)
        {
            Debug.LogError("구경이 설정된 테스트 탄약을 연결해 주세요.", this);
            return;
        }

        // 테스트 시작 시에만 생성합니다.
        LooseAmmoStack looseAmmo =
            new LooseAmmoStack(testAmmo, StartingLooseRounds);

        AmmoMagazine magazineA =
            new AmmoMagazine(testAmmo.caliber, TestMagazineCapacity);

        AmmoMagazine magazineB =
            new AmmoMagazine(testAmmo.caliber, TestMagazineCapacity);

        AmmoChamber chamber = new AmmoChamber(testAmmo.caliber);

        // 1. A에 한 발씩 두 번 삽입합니다.
        bool firstLoaded = looseAmmo.TryLoadOneInto(magazineA);
        bool secondLoaded = looseAmmo.TryLoadOneInto(magazineA);

        Debug.Assert(
            firstLoaded &&
            secondLoaded &&
            magazineA.Count == 2 &&
            looseAmmo.Count == 3,
            "실패: A에 두 발을 이동하는 과정",
            this);

        // 2. 가득 찬 탄창에는 더 들어가지 않아야 합니다.
        bool loadedIntoFullMagazine =
            looseAmmo.TryLoadOneInto(magazineA);

        Debug.Assert(
            !loadedIntoFullMagazine &&
            magazineA.Count == 2 &&
            looseAmmo.Count == 3,
            "실패: 가득 찬 탄창에 넣으면서 탄약 수가 변했습니다.",
            this);

        // 3. B에는 한 발만 넣습니다.
        bool loadedB = looseAmmo.TryLoadOneInto(magazineB);

        Debug.Assert(
            loadedB &&
            magazineA.Count == 2 &&
            magazineB.Count == 1 &&
            looseAmmo.Count == 2,
            "실패: 탄창별 탄약 보관",
            this);

        // 4. A를 장착했다고 가정하고 약실로 한 발 공급합니다.
        AmmoMagazine equippedMagazine = magazineA;
        bool fedFromA = chamber.TryFeedFromMagazine(equippedMagazine);

        Debug.Assert(
            fedFromA &&
            magazineA.Count == 1 &&
            chamber.HasLiveRound,
            "실패: A에서 약실로 탄약 공급",
            this);

        // 5. A를 보관하고 B를 장착했다고 가정합니다.
        // 탄창 객체를 복사하거나 새로 생성하지 않습니다.
        AmmoMagazine removedMagazine = equippedMagazine;
        equippedMagazine = magazineB;

        Debug.Assert(
            ReferenceEquals(removedMagazine, magazineA) &&
            removedMagazine.Count == 1 &&
            equippedMagazine.Count == 1 &&
            chamber.HasLiveRound,
            "실패: 탄창 교체 시 기존 잔탄 또는 약실이 변경되었습니다.",
            this);

        // 6. 약실에 남아 있던 탄약을 한 발 발사했다고 가정합니다.
        bool consumed = chamber.TryConsumeRound(out BulletData firedAmmo);
        bool ejected = chamber.TryEject(out _);
        bool fedFromB = chamber.TryFeedFromMagazine(equippedMagazine);

        Debug.Assert(
            consumed &&
            firedAmmo == testAmmo &&
            ejected &&
            fedFromB &&
            magazineA.Count == 1 &&
            magazineB.Count == 0 &&
            chamber.HasLiveRound &&
            looseAmmo.Count == 2,
            "실패: 발사 후 B에서 다음 탄약 공급",
            this);

        // 처음 5발 중 1발 발사 → 남은 실탄은 총 4발입니다.
        int remainingRounds =
            looseAmmo.Count +
            magazineA.Count +
            magazineB.Count +
            (chamber.HasLiveRound ? 1 : 0);

        Debug.Assert(
            remainingRounds == StartingLooseRounds - 1,
            "실패: 탄약이 복제되거나 유실되었습니다.",
            this);

        Debug.Log(
            $"테스트 종료 — 낱개: {looseAmmo.Count}, " +
            $"탄창 A: {magazineA.Count}, 탄창 B: {magazineB.Count}, " +
            $"약실: {(chamber.HasLiveRound ? 1 : 0)}. " +
            "Assertion 오류가 없는지 확인하세요.",
            this);
    }
#endif
}