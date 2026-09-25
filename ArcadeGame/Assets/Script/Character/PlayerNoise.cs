using System;
using UnityEngine;

public static class PlayerNoise
{
    public static event Action<Character, Vector3, float> Emitted;

    public static void Emit(Character source, Vector3 position, float radius)
    {
        if(source == null || radius <= 0f)
        {
            return;
        }
        Emitted?.Invoke(source, position, radius);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Emitted = null;
    }
} 
