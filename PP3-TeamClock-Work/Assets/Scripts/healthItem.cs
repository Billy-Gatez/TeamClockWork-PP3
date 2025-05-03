// George Carter - Full Sail University
using UnityEngine;

public class healthItem : MonoBehaviour
{

    [SerializeField] int healthAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController player = other.GetComponent<playerController>();
            if (player != null)
            {
                player.PickupHealthItem(healthAmount);


                Destroy(gameObject);
            }
        }
    }
}

