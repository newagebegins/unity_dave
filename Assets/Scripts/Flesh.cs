using UnityEngine;

public class Flesh : MonoBehaviour
{
    private Body body;

    private void Awake()
    {
        body = GetComponent<Body>();
    }

    private void Update()
    {
        body.Move();
    }
}
