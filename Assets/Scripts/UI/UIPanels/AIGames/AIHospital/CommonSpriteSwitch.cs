using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonSpriteSwitch : MonoBehaviour
{
    private Image _self;
    public List<Sprite> _imgList;
    public uint _index;
    // Start is called before the first frame update
    public void Awake()
    {
        _self = GetComponent<Image>();
    }


    public void Switch(uint index,bool SetNative = true)
    {
        if (_self==null)
        {
            return;
        }
        _index = index;
        if (index >= _imgList.Count)
        {
            LoggerUtils.LogError("索引越界");
        }
        else
        {
            _self.sprite = _imgList[(int)index];
            if (SetNative)
            {
                _self.SetNativeSize();
            }
        }
    }
}
