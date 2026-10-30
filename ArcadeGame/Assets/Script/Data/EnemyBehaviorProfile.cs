using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyBehaviorProfile",
    menuName = "Game/Enemy/Behavior Profile")]
public sealed class EnemyBehaviorProfile : ScriptableObject
{
    private const int MaximumAdditionalSearchPoints = 6;
    private const float MinimumSearchRadius = 1f;

    [Header("Combat Movement")]
    [SerializeField] private bool allowStrafe = true;
    [SerializeField] private bool allowApproach = true;
    [SerializeField] private bool allowRetreat = true;

    [Header("Tactical Actions")]
    [SerializeField] private bool allowCover = true;
    [SerializeField] private bool allowDodge = true;

    [Header("Simple Combat Movement")]
    [SerializeField] private bool allowLinearMove = false;

    [Header("Suppression")]
    [SerializeField] private bool allowSuppression = false;

    [Header("Reaction")]
    [SerializeField, Min(0f)] private float alertReactionDelay = 0.5f;
    [SerializeField, Min(0f)] private float minVisualReactionDelay = 0f;
    [SerializeField, Min(0f)] private float maxVisualReactionDelay = 0f;

    [Header("Area Search")]
    [SerializeField, Range(0, MaximumAdditionalSearchPoints)] private int additionalSearchPoints = 0;
    [SerializeField, Min(MinimumSearchRadius)]private float searchRadius = 5f;
    public bool AllowSuppression => allowSuppression;
    public bool AllowLinearMove => allowLinearMove;
    public bool AllowStrafe => allowStrafe;
    public bool AllowApproach => allowApproach;
    public bool AllowRetreat => allowRetreat;
    public bool AllowCover => allowCover;
    public bool AllowDodge => allowDodge;
    public float AlertReactionDelay => Mathf.Max(0f, alertReactionDelay);
    public float SearchRadius =>Mathf.Max(MinimumSearchRadius, searchRadius);
    
    public int AdditionalSearchPoints => Mathf.Clamp(additionalSearchPoints, 0, MaximumAdditionalSearchPoints);


    public float SampleVisualReactionDelay()
    {
        float minimum = Mathf.Max(0f, minVisualReactionDelay);
        float maximum = Mathf.Max(minimum, maxVisualReactionDelay);

        return Random.Range(minimum, maximum);
    }

}
