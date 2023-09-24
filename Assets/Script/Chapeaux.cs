using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapeaux : NetworkBehaviour
{
    //PlayerRef chapeau;
    public LayerMask layersCollision;
    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();

    NetworkObject networkObj;
    NetworkRigidbody networkRb;
    PlayerRef proprioChapeau;
    NetworkObject proprioNetworkObj;

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {        
            int nbJoueursTouche = Runner.LagCompensation.OverlapSphere(transform.position, 1f, proprioChapeau, infosCollisionsList, layersCollision);

            bool contactValide = false;
            if(nbJoueursTouche > 0)
                contactValide = true;
            foreach (LagCompensatedHit objTouche in infosCollisionsList)
            {
                //est-ce qu'on se touche soit même
                if (objTouche.Hitbox.Root.GetBehaviour<NetworkObject>() == proprioNetworkObj)
                    contactValide = false;
                //Debug.Log("+1 point");
                //Runner.Despawn(networkObj);
            }

            if (contactValide)
            {
                nbJoueursTouche = Runner.LagCompensation.OverlapSphere(transform.position, 1f, proprioChapeau, infosCollisionsList, layersCollision, HitOptions.None);
                foreach (LagCompensatedHit objTouche in infosCollisionsList)
                {
                    GestionnairePointage gestionnairePointage = objTouche.Hitbox.transform.root.GetComponent<GestionnairePointage>();
                    if (gestionnairePointage != null)
                    {
                        Debug.Log("+1 points");
                        Runner.Despawn(networkObj);
                    }
                }
            }
        }
    }

    public void ApparitionChapeau(PlayerRef _proprioChapeau, NetworkObject _proprioNetworkObj, GameObject modeleChapeau)
    {
        networkObj = GetComponent<NetworkObject>();
        networkRb = GetComponent<NetworkRigidbody>();
        proprioChapeau = _proprioChapeau;
        proprioNetworkObj = _proprioNetworkObj;

        GameObject chapeau = Instantiate(modeleChapeau, transform.position, transform.rotation, transform);
        chapeau.SetActive(true);
        networkRb.InterpolationTarget = chapeau.transform;
    }
}
