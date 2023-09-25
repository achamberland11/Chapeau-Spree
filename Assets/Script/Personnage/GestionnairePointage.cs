using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

/* Script � ajouter au prefab JoueurReseau qui g�re le pointage du joueur
 * Variables :
 * byte pointage : variable synchronis�e (Networked) pour le pointage du joueur
 * gestionnaireAffichagePointage : r�f�rence au script qui s'occupera d'afficher le pointage des
 * diff�rents joueurs. 
 */

public class GestionnairePointage : NetworkBehaviour
{
    [Networked]
    byte pointage { get; set; }

    GestionnaireAffichagePointage gestionnaireAffichagePointage;

    /* R�cup�ration du script GestionnaireAffichagePointage. Ce script se trouve sur le canvas
     * CanvasPointages.
     */
    void Awake()
    {
        gestionnaireAffichagePointage = FindFirstObjectByType<GestionnaireAffichagePointage>();
    }

    /* Fonction appel�e de l'ext�rieur par le script JoueurReseau lorsqu'un nouveau joueur est
     * spawn�. R�ception du nom en param�tre et appel de la fonction EnregistrementNom du script
     * gestionnaireAffichagePointage en passant le nom et le pointage.
     * Notez bien que le pointage ne sera pas n�cessairement �gal � 0. Lorsqu'un nouveau joueur est
     * spawn�, son pointage est de 0... mais les autres joueurs qui �taient pr�sents dans la partie
     * avant l'arriv�e du nouveau joueur seront aussi spawn� sur l'ordinateur du nouveau joueur. Leur
     * pointage sera donc affich� en fonction de leur vraie valeur.
     */
    public void EnregistrementNom(string leNom)
    {
        gestionnaireAffichagePointage.EnregistrementNom(leNom, pointage);
    }

    /* Fonction appel�e de l'ext�rieur par le script GestionnairePoint de vie lorsqu'un joueur a �limin�
     * un autre joueur. IMPORTANT : cette fonction s'ex�cutera uniquement sur le serveur car elle est
     * dans une s�quence qui s'ex�cute seulement pour le StateAuthority.
     * Param�tres : le nom du joueur et la valeur a ajouter a son pointage
     * 1.Augmentation du pointage du joueur et appel d'un Remote Procedure Call (RPC) pour s'assurer
     * que tous les joueurs mettre � jour l'affichage du pointage.
     */
    public void ChangementPointage(string nomJoueur, byte valeur)
    {
        //1. 
        pointage += valeur;
        RPC_ChangementPointage(nomJoueur, pointage);
    }

    /* Fonction RPC (remote procedure call) d�clench� par le serveur (RpcSources.StateAuthority) 
     * et qui sera ex�cut�e sur tous les clients (RpcTargets.All).
     * Tous les clients ex�cuteront leur fonction MiseAJourPointage(). Concr�tement, c'est le script
     * du joueur qui vient de marquer un point qui appelera la fonction.
     */
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ChangementPointage(string nomJoueur, byte valeur, RpcInfo infos = default)
    {
        gestionnaireAffichagePointage.MiseAJourPointage(nomJoueur, valeur);
    }

    /* Fonction appel�e de l'ext�rieur par le script JoueurReseau lorsqu'un joueur quitte la partie.
     * IMPORTANT : cette fonction s'ex�cutera uniquement sur le serveur
      * Param�tres : le nom du joueur
      * Appelle de la fonction SupprimerJoueur_RPC (remote procedure call)
      */
    public void SupprimeJoueur(string nomJoueur)
    {
        SupprimeJoueur_RPC(nomJoueur);
    }

    /* Fonction RPC (remote procedure call) d�clench� par le serveur (RpcSources.StateAuthority) 
    * et qui sera ex�cut�e sur tous les clients (RpcTargets.All).
    * Tous les clients ex�cuteront leur fonction SupprimeJoueur().
    */
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void SupprimeJoueur_RPC(string nomJoueur, RpcInfo infos = default)
    {
        gestionnaireAffichagePointage.SupprimeJoueur(nomJoueur);
    }
}
