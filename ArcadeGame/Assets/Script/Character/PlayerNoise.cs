using System;
using UnityEngine;

public enum PlayerNoiseType
{
    Unknown,
    Footstep,
    Gunshot
}
public static class PlayerNoise
{
    public static event Action<Character, Vector3, float, PlayerNoiseType> Emitted;

    public static void Emit(Character source, Vector3 position, float radius, PlayerNoiseType type = PlayerNoiseType.Unknown)
    {
        if(source == null || radius <= 0f)
        {
            return;
        }
        Emitted?.Invoke(source, position, radius, type);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Emitted = null;
    }
} 
