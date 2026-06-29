using UnityEngine;

[System.Serializable]
public class Quaternion4
{
    public float x;
    public float y;
    public float z;
    public float w;
    public Quaternion4(float vx, float vy, float vz,float vw)
    {
        x = vx;
        y = vy;
        z = vz;
        w = vw;
    }

    public Quaternion4 Clone()
    {
        return new Quaternion4(x, y, z,w);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z}, {w})";
    }

    public bool Equals(Quaternion v3)
    {
        return v3 != null && this.x == v3.x && this.y == v3.y && this.z == v3.z&& this.w == v3.w;
    }

    public static implicit operator UnityEngine.Quaternion(Quaternion4 vec)
    {
        if (vec == null) {
            return UnityEngine.Quaternion.identity;
        }
        return new UnityEngine.Quaternion(vec.x, vec.y, vec.z, vec.w);
    }

    public static implicit operator Quaternion4(UnityEngine.Quaternion vec)
    {
        return new Quaternion4(vec.x, vec.y, vec.z, vec.w);
    }    
}