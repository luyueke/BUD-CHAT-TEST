// @Author: YangJie
// @Description:
// @Date:  2023/12/04
// @Modify:

using System;

namespace xasset
{

    [AttributeUsage(AttributeTargets.Class)]
    public class RequestPriorityAttribute : System.Attribute
    {
        public int RequestPriority { get; private set; }
        public RequestPriorityAttribute()
        {
            RequestPriority = 0;
        }

        public RequestPriorityAttribute(int priority)
        {
            RequestPriority = priority;
        }


    }
    
}