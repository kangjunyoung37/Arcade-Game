using UnityEngine;


// 무기와 탄창이 공유하는 장착 규격입니다.
// 같은 에셋을 참조하면 같은 규격으로 판단합니다.
[CreateAssetMenu( fileName = "NewMagazinePattern", menuName = "Shooting/Magazine Pattern")]
public sealed class MagazinePatternData : ScriptableObject
{
    [SerializeField] private string displayName;

    public string DisplayName => displayName;
}