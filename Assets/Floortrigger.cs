using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floortrigger : MonoBehaviour
{
    public WalkerAgent walkerAgent; 

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            walkerAgent.StartChasingPlayer(); 
        }
    }
}

