/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 14:56:05
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-18 15:19:06
 * @ Description: 基础类的扩展方法
 */
using Pb.Base;
using UnityEngine;

namespace Pb.Map
{
    public static class PVectorEx
    {
        private const float FloatFix = 10000;


        public static Vector3 ToVector3(this PVector3 pVector3)
        {
            if (pVector3 == null) {
                return Vector3.zero;
            }
            return new Vector3(pVector3.X, pVector3.Y, pVector3.Z);
        }

        public static PVector3 ToPB(this Vector3 uVector3)
        {
            var pbVector3 = new PVector3();
            pbVector3.X = uVector3.x;
            pbVector3.Y = uVector3.y;
            pbVector3.Z = uVector3.z;
            return pbVector3;
        }
        
        public static PB_Vector3 ToPB_Vector3(this Vector3 uVector3)
        {
            var pbVector3 = new PB_Vector3();
            pbVector3.X = uVector3.x;
            pbVector3.Y = uVector3.y;
            pbVector3.Z = uVector3.z;
            return pbVector3;
        }

        public static Vector3 LimitVector3(this Vector3 target, float min = 0.0001f)
        {
            for (int i = 0; i < 3; i++)
            {
                target[i] = Mathf.Max(target[i],min);
            }
            return target;
        }

        public static Vector2 ToVector2(this PVector2 pVector2)
        {
            if (pVector2 == null) {
                return Vector2.zero;
            }
            return new Vector2(pVector2.X, pVector2.Y);
        }

        public static PVector2 ToPB(this Vector2 uVector2)
        {
            var pbVector2 = new PVector2();
            pbVector2.X = uVector2.x;
            pbVector2.Y = uVector2.y;
            return pbVector2;
        }

        public static Color ToColor(this PColor pColor)
        {
            if (pColor == null) {
                return Color.clear;
            }
            return new Color(pColor.R, pColor.G, pColor.B, pColor.A);
        }

        public static PColor ToPB(this Color uColor)
        {
            var pColor = new PColor();
            pColor.R = uColor.r;
            pColor.G = uColor.g;
            pColor.B = uColor.b;
            pColor.A = uColor.a;
            return pColor;
        }

        public static PB_Quaternion ToGamePB(this Quaternion quaternion)
        {
            var pQuaternion = new PB_Quaternion{};
            pQuaternion.X = quaternion.x;
            pQuaternion.Y = quaternion.y;
            pQuaternion.Z = quaternion.z;
            pQuaternion.W = quaternion.w;
            return pQuaternion;
        }

        public static PB_Quaternion ToFixPB(this Quaternion quaternion)
        {
            var pQuaternion = new PB_Quaternion{};
            pQuaternion.X = quaternion.x * FloatFix;
            pQuaternion.Y = quaternion.y * FloatFix;
            pQuaternion.Z = quaternion.z * FloatFix;
            pQuaternion.W = quaternion.w * FloatFix;
            return pQuaternion;
        }

        public static Quaternion ToGameQuaternion(this PB_Quaternion pQuaternion)
        {
            if (pQuaternion == null) {
                return Quaternion.identity;
            }
            var quaternion = new Quaternion();
            quaternion.x = pQuaternion.X;
            quaternion.y = pQuaternion.Y;
            quaternion.z = pQuaternion.Z;
            quaternion.w = pQuaternion.W;
            return quaternion;
        }


        public static Quaternion ToFixQuaternion(this PB_Quaternion pQuaternion)
        {
            if (pQuaternion == null) {
                return Quaternion.identity;
            }
            var quaternion = new Quaternion();
            quaternion.x = pQuaternion.X / FloatFix;
            quaternion.y = pQuaternion.Y / FloatFix;
            quaternion.z = pQuaternion.Z / FloatFix;
            quaternion.w = pQuaternion.W / FloatFix;
            return quaternion;
        }


        public static PB_Vector3 ToGamePB(this Vector3 quaternion)
        {
            var pVector3 = new PB_Vector3{};
            pVector3.X = quaternion.x;
            pVector3.Y = quaternion.y;
            pVector3.Z = quaternion.z;
            return pVector3;
        }

        public static PB_Vector3 ToFixPB(this Vector3 quaternion)
        {
            var pVector3 = new PB_Vector3{};
            pVector3.X = quaternion.x * FloatFix;
            pVector3.Y = quaternion.y * FloatFix;
            pVector3.Z = quaternion.z * FloatFix;
            return pVector3;
        }

        public static Vector3 ToFixVector3(this PB_Vector3 pVector3)
        {
            if (pVector3 == null) {
                return Vector3.zero;
            }
            var vector = new Vector3();
            vector.x = pVector3.X / FloatFix;
            vector.y = pVector3.Y / FloatFix;
            vector.z = pVector3.Z / FloatFix;
            return vector;
        }

        public static Vector3 ToVector3(this PB_Vector3 pVector3)
        {
            if (pVector3 == null) {
                return Vector3.zero;
            }
            var vector = new Vector3();
            vector.x = pVector3.X;
            vector.y = pVector3.Y;
            vector.z = pVector3.Z;
            return vector;
        }
    }
}
