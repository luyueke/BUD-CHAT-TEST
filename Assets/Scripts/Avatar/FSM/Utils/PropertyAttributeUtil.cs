using System;
using System.Reflection;
using UnityEngine;

public class PropertyAttributeUtil
{
    public static T GetEnumPropertyAttribute<T>(Enum value) where T : PropertyAttribute
    {
        Type enumType = value.GetType();
        // 获取枚举常数名称。
        string name = Enum.GetName(enumType, value);
        if (name != null)
        {
            // 获取枚举字段。
            FieldInfo fieldInfo = enumType.GetField(name);
            if (fieldInfo != null)
            {
                // 获取描述的属性。
                T attr = Attribute.GetCustomAttribute(fieldInfo,
                    typeof(T), false) as T;
                if (attr != null)
                {
                    return attr;
                }
            }
        }
        return null;
    }
}
