using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
/*
 * Ce script n'est pas une classe, mais bien une structure de données (struct)
 * Dérive de INetworkInput qui est une interface de Fusion
 * Permet de mémoriser des valeurs avec de variables
 * mouvementInput : un vector2 qui servira au déplacement
 * vecteurDevant : un vecteur de direction représentant le devant (l'axe des Z) du personnage dans le monde
 * saute : une booléenne pour savoir le si personnage saute
 * Notez l'utilisation du type NetworkBool qui est une variable réseau qui sera automatiquement synchronisée
 * pour tous les clients
 */
public struct DonneesInputReseau : INetworkInput
{
    public Vector2 mouvementInput;
    public Vector3 vecteurDevant;
    public NetworkBool saute;
    /* pour le serveur : permet de savoir si le joueur a appuyé sur le bouton de tir
    la variable sera définie dans le script GestionnaireInputs*/
    public NetworkBool appuieBoutonTir;
}