using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * Classe comprenant des fonctions statiques générales utilisées
 par plusieurs scripts
*/

public static class Utilitaires
{
    // Fonction statique qui retourne un vector3 aléatoire
    public static Vector3 GetPositionSpawnAleatoire()
    {
        return new Vector3(Random.Range(-10, 10), 4, Random.Range(-10, 10));
    }

    /*
     * Fonction qui permet de changer le layer de tous les enfants d'un gameOject
     * Transform transform : le transform du parent pour lequel ont veut changer le layer des enfants
     * int noLayer : le layer sur lequel on veut mettre les enfants
     * Avec un foreach, on boucle a à travers tous les enfants du parent et on change le layer.
     * Le (true) à la fin du GetComponentsInChildren indique que même les objets désactivés seront affectés.
     */
    public static void SetRenderLayerInChildren(Transform transform, int numLayer)
    {
        foreach (Transform trans in transform.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = numLayer;
        }
    }
}
