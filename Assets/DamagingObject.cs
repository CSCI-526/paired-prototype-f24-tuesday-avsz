using UnityEngine;
using Unity.FPS.Game; // Add this line to access the Health class

public class DamagingObject : MonoBehaviour
{
    public float DamageAmount = 25f; // Amount of damage this object will inflict
    //public bool DestroyOnCollision = false; // Whether this object should be destroyed after colliding with the player
    string playerTag = "Player"; 
    public GameObject player;
    private void OnCollisionEnter(Collision collision)
    {
        //Print collision gameObject info
         //return;
        // Check if collision is with some PART of the WalkerRagdoll
         if (player == null)
            player = GameObject.Find("Player");
        Debug.Log("Collision with: " + collision.gameObject.name);
        string objectName = collision.gameObject.name;
        if (objectName == "lower_arm_R" || objectName == "lower_arm_L" || 
        objectName == "upper_arm_L" || objectName == "upper_arm_R" ||
        objectName == "hand_L" || objectName == "hand_R" ||
        objectName == "shinL" || objectName == "thighL" ||
        objectName == "shinR" || objectName == "thighR" ||
        objectName == "chest" || objectName == "hips" ||
        objectName == "head" || objectName == "Body")
        {
            Debug.Log("Collision with: " + collision.gameObject.name);
                    Health playerHealth = player.GetComponent<Health>(); 
                    playerHealth.TakeDamage(DamageAmount, gameObject); // Inflict damage to the player
           
        }
       
        //GameObject player = GameObject.FindGameObjectWithTag(playerTag);

    }

    // If you're using triggers instead of collisions, use this method:
    private void OnTriggerEnter(Collider other)
    {
    }
}