using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

/* Script à ajouter au prefab JoueurReseau qui gère le pointage du joueur
 * Variables :
 * byte pointage : variable synchronisée (Networked) pour le pointage du joueur
 * gestionnaireAffichagePointage : référence au script qui s'occupera d'afficher le pointage des
 * différents joueurs. 
 */

public class GestionnairePointage : NetworkBehaviour
{
    [Networked]
    byte pointage { get; set; }

    GestionnaireAffichagePointage gestionnaireAffichagePointage;

    /* Récupération du script GestionnaireAffichagePointage. Ce script se trouve sur le canvas
     * CanvasPointages.
     */
    void Awake()
    {
        gestionnaireAffichagePointage = FindFirstObjectByType<GestionnaireAffichagePointage>();
    }

    /* Fonction appelée de l'extérieur par le script JoueurReseau lorsqu'un nouveau joueur est
     * spawné. Réception du nom en paramètre et appel de la fonction EnregistrementNom du script
     * gestionnaireAffichagePointage en passant le nom et le pointage.
     * Notez bien que le pointage ne sera pas nécessairement égal à 0. Lorsqu'un nouveau joueur est
     * spawné, son pointage est de 0... mais les autres joueurs qui étaient présents dans la partie
     * avant l'arrivée du nouveau joueur seront aussi spawné sur l'ordinateur du nouveau joueur. Leur
     * pointage sera donc affiché en fonction de leur vraie valeur.
     */
    public void EnregistrementNom(string leNom)
    {
        gestionnaireAffichagePointage.EnregistrementNom(leNom, pointage);
    }

    /* Fonction appelée de l'extérieur par le script GestionnairePoint de vie lorsqu'un joueur a éliminé
     * un autre joueur. IMPORTANT : cette fonction s'exécutera uniquement sur le serveur car elle est
     * dans une séquence qui s'exécute seulement pour le StateAuthority.
     * Paramètres : le nom du joueur et la valeur a ajouter a son pointage
     * 1.Augmentation du pointage du joueur et appel d'un Remote Procedure Call (RPC) pour s'assurer
     * que tous les joueurs mettre à jour l'affichage du pointage.
     */
    public void ChangementPointage(string nomJoueur, byte valeur)
    {
        //1. 
        pointage += valeur;
        RPC_ChangementPointage(nomJoueur, pointage);
    }

    /* Fonction RPC (remote procedure call) déclenché par le serveur (RpcSources.StateAuthority) 
     * et qui sera exécutée sur tous les clients (RpcTargets.All).
     * Tous les clients exécuteront leur fonction MiseAJourPointage(). Concrètement, c'est le script
     * du joueur qui vient de marquer un point qui appelera la fonction.
     */
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ChangementPointage(string nomJoueur, byte valeur, RpcInfo infos = default)
    {
        gestionnaireAffichagePointage.MiseAJourPointage(nomJoueur, valeur);
    }

    /* Fonction appelée de l'extérieur par le script JoueurReseau lorsqu'un joueur quitte la partie.
     * IMPORTANT : cette fonction s'exécutera uniquement sur le serveur
      * Paramètres : le nom du joueur
      * Appelle de la fonction SupprimerJoueur_RPC (remote procedure call)
      */
    public void SupprimeJoueur(string nomJoueur)
    {
        SupprimeJoueur_RPC(nomJoueur);
    }

    /* Fonction RPC (remote procedure call) déclenché par le serveur (RpcSources.StateAuthority) 
    * et qui sera exécutée sur tous les clients (RpcTargets.All).
    * Tous les clients exécuteront leur fonction SupprimeJoueur().
    */
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void SupprimeJoueur_RPC(string nomJoueur, RpcInfo infos = default)
    {
        gestionnaireAffichagePointage.SupprimeJoueur(nomJoueur);
    }
}
