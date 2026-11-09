using UnityEngine;


// 같은 종류의 탄창들이 공유하는 설정입니다.
// 잔탄 수는 이 에셋에 저장하지 않습니다.
[CreateAssetMenu( fileName = "NewMagazine", menuName = "Shooting/Magazine Data")]
public sealed class MagazineData : ScriptableObject
{
    [Header("Presentation")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Compatibility")]
    [SerializeField] private AmmoCaliberData caliber;
    [SerializeField] private MagazinePatternData pattern;

    [Header("Capacity")]
    [SerializeField, Min(1)] private int capacity = 30;

    [Header("Inventory Size")]
    [SerializeField, Min(1)] private int gridWidth = 1;
    [SerializeField, Min(1)] private int gridHeight = 2;

    public string DisplayName => displayName;
    public Sprite Icon => icon;

    public AmmoCaliberData Caliber => caliber;
    public MagazinePatternData Pattern => pattern;

    public int Capacity => capacity;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    public bool IsValid => caliber != null && pattern != null && capacity > 0 && gridWidth > 0 && gridHeight > 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        capacity = Mathf.Max(1, capacity);
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
    }
#endif
}