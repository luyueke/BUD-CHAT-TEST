using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicPreviewView : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnClose() {
        gameObject.SetActive(false);
    }
}
