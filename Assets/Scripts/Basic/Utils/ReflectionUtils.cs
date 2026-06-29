using System;
using System.Reflection;

namespace Basic.Utils
{
    public class ReflectionUtils
    {
        #region Basic
        
        public static MemberInfo[] GetAllMembers(Type type)
        {
            return type.GetMembers();
        }

        public static FieldInfo[] GetPublicFields(Type type)
        {
            return type.GetFields(BindingFlags.Public);
        }

        public static MethodInfo[] GetPublicMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public);
        }

        #endregion

        #region For Instance

        public static MethodInfo[] GetPublicMethodsFromInstance(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        }

        #endregion
    }
}