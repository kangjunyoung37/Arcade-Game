using UnityEngine;

public enum SurfaceType
{
    Default,
    Flesh,
    Metal,
    Wood,
    Dirt
}
public class SurfaceProp : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Default;
}
