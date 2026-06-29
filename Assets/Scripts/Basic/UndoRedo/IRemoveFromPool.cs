using System.Collections;
using System.Collections.Generic;
using Basic.UndoRedo;
using UnityEngine;

namespace Basic.UndoRedo
{
    public interface IRemoveFromPool
    {
        //超过undo最大限制被清除或redo时添加record导致redo栈被清除
        public void OnRemoveFromPool(UndoRecord record,int fromType);
    }

}
