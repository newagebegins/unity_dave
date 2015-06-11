using UnityEngine;
using System.Collections;

public class DestroyScript : MonoBehaviour
{
    public void DestroyMyself()
    {
        Destroy(transform.parent.gameObject);
    }
}
