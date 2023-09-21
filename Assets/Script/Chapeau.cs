using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapeau : NetworkBehaviour
{

    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();
    public LayerMask layersCollision;


    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            //2.
            /*int nbJoueursTouche = Runner.LagCompensation.OverlapSphere(transform.position, 1, infosCollisionsList, layersCollision);*/

            //3.
            foreach (LagCompensatedHit objetTouche in infosCollisionsList)
            {

            }
            //4.
            Runner.Despawn(GetComponent<NetworkObject>());
        }
    }

/*    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Fantome")
        {
            collision.gameObject.GetComponent<JoueurReseau>().pointage++;
            Debug.Log(collision.gameObject.GetComponent<JoueurReseau>().pointage);
            Destroy(gameObject, 0.1f);
        }
    }*/
}
