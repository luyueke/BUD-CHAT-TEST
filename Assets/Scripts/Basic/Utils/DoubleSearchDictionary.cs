using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleSearchDictionary<T1, T2> : ICollection<KeyValuePair<T1, T2>>, ICollection
{
    private Dictionary<T1, T2> dict1;
    private Dictionary<T2, T1> dict2;

    public int Count => dict1.Count;

    public bool IsReadOnly => ((ICollection<KeyValuePair<T1, T2>>)dict1).IsReadOnly;

    public bool IsSynchronized => ((ICollection)dict1).IsSynchronized;

    public object SyncRoot => ((ICollection)dict1).SyncRoot;

    public DoubleSearchDictionary()
    {
        dict1 = new Dictionary<T1, T2>();
        dict2 = new Dictionary<T2, T1>();
    }

    public void Add(T1 a, T2 b)
    {
        AddPair(a, b);
    }

    public void AddPair(T1 a, T2 b)
    {
        if (dict1.ContainsKey(a) && dict2.ContainsKey(b))
        {
            dict1.Remove(a);
            dict2.Remove(b);
        }
        
        if (!dict1.ContainsKey(a) && !dict2.ContainsKey(b))
        {
            dict1.Add(a, b);
            dict2.Add(b, a);
        }
    }

    public bool TryGet(T1 a, out T2 value)
    {
        return dict1.TryGetValue(a, out value);
    }

    public bool TryGet(T2 a, out T1 value)
    {
        return dict2.TryGetValue(a, out value);
    }
    /// <summary>
    /// 当T1跟T2同类型时
    /// </summary>
    /// <param name="a"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetOther(T1 a,out T2 value)
    {
        if(typeof(T1) == typeof(T2)){
            if(TryGet(a, out T2 value1)){
                value = value1;
                return true;
            }
            if(TryGet((T2)(object)a, out T1 value2)){
                value = (T2)(object)value2;
                return true;
            }
        }
        value = default(T2);
        return false;
    }

    public bool Remove(T1 a)
    {
        if(TryGet(a, out T2 value))
        {
            dict1.Remove(a);
            dict2.Remove(value);
            return true;
        }
        return false;
    }

    public bool Remove(T2 a)
    {
        if (TryGet(a, out T1 value))
        {
            dict2.Remove(a);
            dict1.Remove(value);
            return true;
        }
        return false;
    }

    public void Add(KeyValuePair<T1, T2> item)
    {
        AddPair(item.Key, item.Value); 
    }

    public void Clear()
    {
        dict1.Clear();
        dict2.Clear();
    }

    public bool Contains(KeyValuePair<T1, T2> item)
    {
        return ((ICollection<KeyValuePair<T1, T2>>)dict1).Contains(item);
    }

    public void CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<T1, T2>>)dict1).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<T1, T2> item)
    {
        return ((ICollection<KeyValuePair<T1, T2>>)dict1).Remove(item);
    }

    public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
    {
        return dict1.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return dict1.GetEnumerator();
    }

    public void CopyTo(Array array, int index)
    {
        ((ICollection)dict1).CopyTo(array, index);
    }
}
