using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Player_Controller controller = other.GetComponent<Player_Controller>();

        if (controller != null)
        {
            if (controller.health < controller.maxHealth)
            {
                //controller.ChangeHealth(1);
                Destroy(gameObject);
            }
        }
    }
}