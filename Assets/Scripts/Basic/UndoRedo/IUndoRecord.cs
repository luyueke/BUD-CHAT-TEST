using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Basic.UndoRedo
{
    public interface IUndoRecord
    {
        public void AddRecord();
    }
}

