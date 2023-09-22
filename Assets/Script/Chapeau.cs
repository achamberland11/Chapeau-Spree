using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapeau : NetworkBehaviour
{
    PlayerRef chapeau;
    public LayerMask layersCollision;
    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();

    public override void FixedUpdateNetwork()
    {
        int nb = Runner.LagCompensation.OverlapSphere(transform.position, 1, chapeau, infosCollisionsList, layersCollision);

        foreach(LagCompensatedHit objTouche in infosCollisionsList)
        {
            GestionnairePointage pointage = objTouche.Hitbox.transform.root.GetComponent<GestionnairePointage>();
            if (pointage != null)
            {
                pointage.AugmenterPoint();
            }
        }
    }
}
