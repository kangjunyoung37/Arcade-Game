using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class PlayerSpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Character player;
    [SerializeField] private PlayerSpawnPoint[] spawnPoints;

    public PlayerSpawnPoint CurrentSpawnPoint { get; private set; }

    private CharacterController _characterController;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        player.TryGetComponent(out _characterController);
        SpawnPlayerAtRandomPoint();
    }

    public void SpawnPlayerAtRandomPoint()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        SpawnPlayer(spawnPoints[randomIndex]);
    }

    public void SpawnPlayer(PlayerSpawnPoint spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("유효하지 않은 플레이어 스폰 지점입니다.", this);
            return;
        }

        bool controllerWasEnabled =
            _characterController != null &&
            _characterController.enabled;

        // CharacterController가 활성화된 상태에서 Transform을 직접 변경하면
        // 충돌 처리 때문에 이동이 정상 적용되지 않을 수 있다.
        if (controllerWasEnabled)
        {
            _characterController.enabled = false;
        }

        player.transform.SetPositionAndRotation(
            spawnPoint.Position,
            spawnPoint.Rotation);

        if (controllerWasEnabled)
        {
            _characterController.enabled = true;
        }

        CurrentSpawnPoint = spawnPoint;
    }

    private bool ValidateReferences()
    {
        if (player == null)
        {
            Debug.LogError(
                $"{nameof(PlayerSpawnManager)}: Player가 연결되지 않았습니다.",
                this);

            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                $"{nameof(PlayerSpawnManager)}: 스폰 지점이 하나도 없습니다.",
                this);

            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) continue;

            Debug.LogError(
                $"{nameof(PlayerSpawnManager)}: Spawn Points의 {i}번 요소가 비어 있습니다.",
                this);

            return false;
        }

        return true;
    }
}