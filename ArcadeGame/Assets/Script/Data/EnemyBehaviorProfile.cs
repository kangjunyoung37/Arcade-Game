using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyBehaviorProfile",
    menuName = "Game/Enemy/Behavior Profile")]
public sealed class EnemyBehaviorProfile : ScriptableObject
{
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

    public bool AllowSuppression => allowSuppression;
    public bool AllowLinearMove => allowLinearMove;
    public bool AllowStrafe => allowStrafe;
    public bool AllowApproach => allowApproach;
    public bool AllowRetreat => allowRetreat;
    public bool AllowCover => allowCover;
    public bool AllowDodge => allowDodge;
}
