using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;
using UnityEngine.SceneManagement;

/* Script qui gère l'affichage du pointage. Notez qu'il s'agit d'un script Unity standard et non
 * d'un script Fusion.
 * Variables :
 *  - Dictionary<string, byte> infosJoueursPointages : Dictionnaire pour mémoriser le nom de chaque joueur
 * auquel on associe son pointage.
 * - txt_InfoPointageJoueur :Tableau de zones texte du canvasPointage. À définir dans l'inspecteur en
 * ajoutant les 10 zones de texte (il y a un maximum de 10 joueurs par partie)
 */
public class GestionnaireAffichagePointage : NetworkBehaviour
{
    static Dictionary<string, byte> infosJoueursPointages = new Dictionary<string, byte>();

    public TextMeshProUGUI[] txt_InfoPointageJoueurs;
    public TextMeshProUGUI[] txt_InfoPointageJoueursFinal;
    public TextMeshProUGUI txt_TempsRetourMenu;

    public GameObject panelPointage;
    public GameObject panelPointageFinal;

    public NetworkObject joueurLocal;
    public PlayerRef refJoueur;

    /* Fonction appelée de l'extérieur par le script GestionnairePointage. Lors du spawn d'un joueur,
     * on mémorise dans le dictionnaire son nom et son pointage. Par la suite, on appelle fonction 
     * AffichePointage() qui s'occupe de l'affichage visuel du noms de joueurs et de leur pointage.
     */
    public void EnregistrementNom(string leNom, byte pointage)
    {
        infosJoueursPointages.Add(leNom.ToString(), pointage);
        AffichePointage();
    }

    /* Fonction appelée de l'extérieur par le script GestionnairePointage suite à un RPC (remote
     * procedure call). Recoit en paramètre le nom du joueur et son nouveau pointage. Mise à jour
     * du dictionnaire et appel de la fonction AffichePointage().
     */
    public void MiseAJourPointage(string nomJoueur, byte valeur)
    {
        infosJoueursPointages[nomJoueur] = valeur;
        AffichePointage();
    }

    /* Fonction pour l'affichage du nom des joueurs et de leur pointage.
     * 1. On vide tous les champs texte
     * 2. On passe tous les éléments du dictionnaire infosJoueursPointages avec un foreach.Affichage
     * à la fois du nom et du pointage de chaque joueur. Puisque nous avons besoin de récupérer la clé
     * et la valeur du dictionnaire, regardez bien le type de variable utilisé : KeyValuePair. De cette
     * façon, on peut récupérer le nom (itemDictio.Key) et le pointage (itemDictio.Value).
     */
    void AffichePointage()
    {
        //1.
        foreach (var zonTexte in txt_InfoPointageJoueurs)
        {
            zonTexte.text = string.Empty;
        }
        //2. 
        var i = 0;
        foreach (KeyValuePair<string, byte> itemDictio in infosJoueursPointages)
        {
            txt_InfoPointageJoueurs[i].text = $"{itemDictio.Key} : {itemDictio.Value} points";
            i++;
        }
    }

    /* Fonction appelé de l'extérieur par le script GestionnairePointage lorsqu'un joueur 
     * quitte la partie.On supprime alors son entrée du dictionnaire et on rafraichit l'affichage.
    */
    public void SupprimeJoueur(string nomJoueur)
    {
        infosJoueursPointages.Remove(nomJoueur);
        AffichePointage();
    }

    public void AfficherPointageFinal()
    {
        RPC_AffichePointageFinale();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AffichePointageFinale()
    {
        panelPointage.SetActive(false); 
        panelPointageFinal.SetActive(true);
        StartCoroutine(TimerRetourMenu());

        //1.
        foreach (var zonTexte in txt_InfoPointageJoueursFinal)
        {
            zonTexte.text = string.Empty;
        }
        //2. 
        var i = 0;
        foreach (KeyValuePair<string, byte> itemDictio in infosJoueursPointages)
        {
            txt_InfoPointageJoueursFinal[i].text = $"{itemDictio.Key} : {itemDictio.Value} points";
            i++;
        }
    }

    public void RetourAuMenu()
    {
        Debug.Log("Retour Au Menu");
        //Runner.Despawn(joueurLocal);
        if (joueurLocal.HasStateAuthority)
        {
            Debug.Log("Serveur");
            Runner.Shutdown();
            RPC_RetourAuMenu();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_RetourAuMenu()
    {
        SceneManager.LoadScene(0);
    }

    IEnumerator TimerRetourMenu()
    {
        int temps = 10;
        while (temps > 0)
        {
            yield return new WaitForSeconds(1);
            temps--;
            txt_TempsRetourMenu.SetText("Retour au Menu : " + temps.ToString());
        }
        RetourAuMenu();
        StopCoroutine(TimerRetourMenu());
    }
}