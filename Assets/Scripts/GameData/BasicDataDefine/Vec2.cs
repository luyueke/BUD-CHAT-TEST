using System;
using UnityEngine;

[Serializable]
public class Vec2
{
    public float x;
    public float y;

    public Vec2(float vx, float vy)
    {
        x = vx;
        y = vy;
    }

    public static implicit operator Vector2(Vec2 vec)
    {
        return new Vector2(vec.x, vec.y);
    }


    public static implicit operator Vec2(Vector2 vec)
    {
        return new Vec2(vec.x, vec.y);
    }
    
    public bool Equals(Vec2 v2)
    {
        return v2 != null && this.x == v2.x && this.y == v2.y;
    }
    
    // public static implicit operator KeyframeEngine.Math.Standard.Vector2(Vec2 vec)
    // {
    //     return new KeyframeEngine.Math.Standard.Vector2(vec.x, vec.y);
    // }
    //
    // public static implicit operator Vec2(KeyframeEngine.Math.Standard.Vector2 vec)
    // {
    //     return new Vec2(vec.x, vec.y);
    // }
    
    
}

[Serializable]
public class Vec2Int
{
    public int x;
    public int y;

    public Vec2Int(int vx, int vy)
    {
        x = vx;
        y = vy;
    }

    public static implicit operator Vector2Int(Vec2Int vec)
    {
        return new Vector2Int(vec.x, vec.y);
    }


    public static implicit operator Vec2Int(Vector2Int vec)
    {
        return new Vec2Int(vec.x, vec.y);
    }
}
