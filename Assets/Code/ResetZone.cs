using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ResetZone : MonoBehaviour {
    public Transform player;
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            ResetPlayer();
        }
    }

    void ResetPlayer()
    {
        CharacterController controller = player.GetComponent<CharacterController>();

        // Disable controller before teleport
        controller.enabled = false;
        player.position = respawnPoint.position;
        controller.enabled = true;
    }
}
