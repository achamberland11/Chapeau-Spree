using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapeau : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Fantome")
        {
            collision.gameObject.GetComponent<JoueurReseau>().pointage++;
            Debug.Log(collision.gameObject.GetComponent<JoueurReseau>().pointage);
            Destroy(gameObject, 0.1f);
        }
    }
}
