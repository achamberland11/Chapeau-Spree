using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

/*
 * Script qui ex�cute les d�placements du joueur et ainsi que l'ajustement de direction
 * D�rive de NetworkBehaviour. Utilisation de la fonction r�seau FixedUpdateNetwork()
 * Variables :
 * - camLocale : contient la r�f�rence � la cam�ra du joueur actuel
 * - NetworkCharacterControllerPrototypeV2 : pour m�moriser le component NetworkCharacterControllerPrototypeV2 
 * du joueur
 */

public class GestionnaireMouvementJoueur : NetworkBehaviour
{
    Camera camLocale;
    NetworkCharacterControllerPrototypeV2 networkCharacterControllerPrototypeV2;

    /*
     * Avant le Start(), on m�morise la r�f�rence au component networkCharacterControllerPrototypeV2 du joueur
     * On garde en m�moire la cam�ra du joueur courant (GetComponentInChildren)
     */
    void Awake()
    {
        networkCharacterControllerPrototypeV2 = GetComponent<NetworkCharacterControllerPrototypeV2>();
        camLocale = GetComponentInChildren<Camera>();
    }


    /*
     * Fonction r�cursive r�seau pour la simulation. � utiliser pour ce qui doit �tre synchronis� entre
     * les diff�rents clients.
     * 1.R�cup�ration des Inputs m�moris�s dans le script GestionnaireReseau (input.set). Ces donn�es enregistr�es
     * sous forme de structure de donn�es (struc) doivent �tre r�cup�r�es sous la m�me forme.
     * 2.Ajustement de la direction du joueur � partir � partir des donn�es de Input enregistr�s dans les script
     * GestionnaireR�seau et GestionnaireInputs.
     * 3. Correction du vecteur de rotation pour garder seulement la rotation Y pour le personnage (la capsule)
     * 4.Calcul du vecteur de direction du d�placement en utilisant les donn�es de Input enregistr�s.
     * Avec cette formule,il y a un d�placement lat�ral (strafe) li�  � l'axe horizontal (mouvementInput.x)
     * Le vecteur est normalis� pour �tre ramen� � une longueur de 1.
     * Appel de la fonction Move() du networkCharacterControllerPrototypeV2 (fonction pr�existante)
     * 5.Si les donn�es enregistr�es indiquent un saut, on appelle la fonction Jump() du script
     * networkCharacterControllerPrototypeV2 (fonction pr�existante)
     */
    public override void FixedUpdateNetwork()
    {
        // 1.
        GetInput(out DonneesInputReseau donneesInputReseau);

        //2.
        transform.forward = donneesInputReseau.vecteurDevant;
        //3.
        Quaternion rotation = transform.rotation;
        rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
        transform.rotation = rotation;

        //4.
        Vector3 directionMouvement = transform.forward * donneesInputReseau.mouvementInput.y + transform.right * donneesInputReseau.mouvementInput.x;
        directionMouvement.Normalize();
        networkCharacterControllerPrototypeV2.Move(directionMouvement);

        //5.saut, important de le faire apr�s le d�placement
        if (donneesInputReseau.saute) networkCharacterControllerPrototypeV2.Jump();
    }
}