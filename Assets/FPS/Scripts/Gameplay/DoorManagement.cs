using UnityEngine;

public class DoorManagement : MonoBehaviour
{
    public KeyHealth[] keys; 
    private bool doorOpened = false;

    void Update()
    {
        
        if (!doorOpened && AllKeysDestroyed())
        {
            OpenDoor(); 
        }
    }

  
    bool AllKeysDestroyed()
    {
        foreach (KeyHealth key in keys)
        {
            if (key != null) 
            {
                return false;
            }
        }
        return true; 
    }

    
    void OpenDoor()
    {
        doorOpened = true;
        transform.position += new Vector3(0, 5, 0); 
        Debug.Log("door_open");
    }
}
