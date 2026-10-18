using UnityEngine;

public static class ProjectileSpread
{
    public const float MaximumHalfAngle = 90f;
    private const float FullCircleRadians = Mathf.PI * 2f;

    public static Vector3 SampleLocalDirection(float halfAngleDegress)
    {
        float angle = Mathf.Clamp(halfAngleDegress, 0f, MaximumHalfAngle);
        if(angle <= 0f)
        {
            return Vector3.forward;
        }
        float minimumCosine = Mathf.Cos(angle * Mathf.Deg2Rad);

        // 원뿔 탄퍼짐 구현
        float cosine = Mathf.Lerp(minimumCosine, 1f, Random.value);
        float sine = Mathf.Sqrt(Mathf.Max(0f, 1f - cosine * cosine));
        float azimuth = Random.value * FullCircleRadians;

        return new Vector3(sine * Mathf.Cos(azimuth), sine * Mathf.Sin(azimuth), cosine);

    }
}
