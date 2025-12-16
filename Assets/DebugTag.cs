using UnityEngine;

public class DebugTag : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("This object tag is: " + gameObject.tag);
    }
}