using UnityEngine;

public class Door : MonoBehaviour
{
    [HideInInspector]
    public bool isClosed = true;
    
    public void Open()
    {
        if (isClosed)
        {
            isClosed = false;
            gameObject.SendMessage("OnOpenDoor");
        }
    }

    public void Enter()
    {
        if (!isClosed)
        {
            gameObject.SendMessage("OnEnterDoor", null, SendMessageOptions.DontRequireReceiver);
        }
    }
}
