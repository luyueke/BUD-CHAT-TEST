using UnityEngine;
[DisallowMultipleComponent]
public class HideWhenRunInTime : MonoBehaviour
{

    private void Awake()
    {
        gameObject.SetActive(false);
    }
}