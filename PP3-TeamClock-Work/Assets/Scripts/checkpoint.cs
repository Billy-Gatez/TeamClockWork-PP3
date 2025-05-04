using UnityEngine;
using System.Collections;

public class checkpoint : MonoBehaviour
{
    [SerializeField] Renderer model;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && gamemanager.instance.playerSpawnPos.transform.position != transform.position)
        {
            gamemanager.instance.playerSpawnPos.transform.position = transform.position;
            StartCoroutine(checkpointFeedback());
        }
    }
    IEnumerator checkpointFeedback()
    {
        model.material.color = Color.red;
        gamemanager.instance.checkpointPopup.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        gamemanager.instance.checkpointPopup.SetActive(false);
        model.material.color = Color.white;
    }
}
