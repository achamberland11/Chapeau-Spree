using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class Chapeaux : NetworkBehaviour
{
    //PlayerRef chapeau;
    public LayerMask layersCollision;
    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();

    NetworkObject networkObj;
    NetworkRigidbody networkRb;
    PlayerRef proprioChapeau;
    NetworkObject proprioNetworkObj;

    public GameObject[] chapeaux;

    private void Start()
    {
        
    }

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
                    JoueurReseau joueur = objTouche.Hitbox.transform.root.GetComponent<JoueurReseau>();
                    if (gestionnairePointage != null)
                    {
                        Debug.Log("+1 points");
                        gestionnairePointage.ChangementPointage(joueur.nomDujoueur.ToString(), 1);
                        Runner.Despawn(networkObj);
                    }
                }
            }
        }
    }

    // Fonction éxécuté seulement sur le serveur
    public void ApparitionChapeau(PlayerRef _proprioChapeau, NetworkObject _proprioNetworkObj, int indexChapeau)
    {
        Debug.Log("Apparition Chapeau");
        networkObj = GetComponent<NetworkObject>();
        networkRb = GetComponent<NetworkRigidbody>();
        proprioChapeau = _proprioChapeau;
        proprioNetworkObj = _proprioNetworkObj;

        // Apparition du chapeau sur tout les clients

        RPC_ApparitionChapeau(indexChapeau);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ApparitionChapeau(int indexChapeau, RpcInfo infos = default)
    {
        GameObject chapeau = chapeaux[indexChapeau];
        chapeau.SetActive(true);
        networkRb.InterpolationTarget = chapeau.transform;
    }
}
