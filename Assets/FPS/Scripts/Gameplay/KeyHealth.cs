using UnityEngine;

public class KeyHealth : MonoBehaviour
{
    public int health = 12; 

    
    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
