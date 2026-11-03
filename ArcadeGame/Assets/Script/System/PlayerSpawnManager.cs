using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class PlayerSpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Character playerPrefab;
    [SerializeField] private PlayerSceneBindings sceneBindings;
    [SerializeField] private PlayerSpawnPoint[] spawnPoints;

    public Character CurrentPlayer { get; private set; }
    public PlayerSpawnPoint CurrentSpawnPoint { get; private set; }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        SpawnPlayerAtRandomPoint();
    }

    public void SpawnPlayerAtRandomPoint()
    {
        if (!ValidateReferences())
            return;

        int index = Random.Range(0, spawnPoints.Length);
        SpawnPlayer(spawnPoints[index]);
    }

    public void SpawnPlayer(PlayerSpawnPoint spawnPoint)
    {
        if (!ValidateReferences() || spawnPoint == null)
            return;

        // 초기 스폰용입니다. 이미 플레이어가 있으면 중복 생성하지 않습니다.
        if (CurrentPlayer != null || gameManager.GetCharacter() != null)
        {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("이미 플레이어가 존재하므로 중복 스폰을 건너뜁니다.", this);
        #endif
            return;
        }

        Character instance = Instantiate(
            playerPrefab,
            spawnPoint.Position,
            spawnPoint.Rotation);

        // Start/Update가 실행되기 전에 씬 참조를 연결합니다.
        if (!sceneBindings.TryBind(instance))
        {
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
            return;
        }

        if (!gameManager.RegisterCharacter(instance))
        {
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
            return;
        }

        CurrentPlayer = instance;
        CurrentSpawnPoint = spawnPoint;
    }

    private bool ValidateReferences()
    {
        if (gameManager == null ||playerPrefab == null || sceneBindings == null ||!sceneBindings.HasValidReferences)
        {
            LogConfigurationError();
            return false;
        }

        if (playerPrefab.gameObject.scene.IsValid())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("Player Prefab에는 Hierarchy 오브젝트가 아니라 " +"Project 창의 프리팹을 연결해 주세요.",this);
#endif
            return false;
        }

        if (!playerPrefab.gameObject.activeSelf || !playerPrefab.enabled)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("플레이어 프리팹의 루트 오브젝트와 Character를 활성화해 주세요.", this);
#endif
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            LogConfigurationError();
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                LogConfigurationError();
                return false;
            }
        }

        return true;
    }

    private void LogConfigurationError()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError("PlayerSpawnManager의 참조와 스폰 지점 연결을 확인해 주세요.",this);
#endif
    }
}