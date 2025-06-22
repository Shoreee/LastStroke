using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthDownCollectible : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Player_Controller controller = other.GetComponent<Player_Controller>();

        if (controller != null)
        {
            if (controller.health > 0)
            {
                //controller.ChangeHealth(-1);
                Destroy(gameObject);
            }
        }
    }
}
