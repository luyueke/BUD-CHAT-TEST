[System.Serializable]
public class Vec3
{
    public float x;
    public float y;
    public float z;

    public Vec3(float vx, float vy, float vz)
    {
        x = vx;
        y = vy;
        z = vz;
    }

    public Vec3 Clone()
    {
        return new Vec3(x, y, z);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }

    public bool Equals(Vec3 v3)
    {
        return v3 != null && this.x == v3.x && this.y == v3.y && this.z == v3.z;
    }

#if UNITY_5_6_OR_NEWER
    public static implicit operator UnityEngine.Vector3(Vec3 vec)
    {
        if (vec == null) {
            return UnityEngine.Vector3.zero;
        }
        return new UnityEngine.Vector3(vec.x, vec.y, vec.z);
    }


    public static implicit operator Vec3(UnityEngine.Vector3 vec)
    {
        return new Vec3(vec.x, vec.y, vec.z);
    }
#endif


    //
    // public static implicit operator KeyframeEngine.Math.Standard.Vector3(Vec3 vec)
    // {
    //     return new KeyframeEngine.Math.Standard.Vector3(vec.x, vec.y, vec.z);
    // }
    //
    // public static implicit operator Vec3(KeyframeEngine.Math.Standard.Vector3 vec)
    // {
    //     return new Vec3(vec.x, vec.y, vec.z);
    // }
}
