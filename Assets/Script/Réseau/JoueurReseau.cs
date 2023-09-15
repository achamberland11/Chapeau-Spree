using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // namespace pour utiliser les classes de Fusion
/* 
 * 1.Les objets réseau ne doivent pas dériver de MonoBehavior, mais bien de NetworkBehavior
 * Importation de l'interface IPlayerLeft
 * 2.Variable pour mémoriser l'instance du joueur
 * 3.Fonction Spawned() : Semblable au Start(), mais pour les objets réseaux
 * Sera exécuté lorsque le personnage sera créé (spawn)
* Test si le personnage créé est le personnage contrôlé par l'utilisateur local.
 * HasInputAuthority permet de vérifier cela.
 * Retourne true si on est sur le client qui a généré la création du joueur
 * Retourne false pour les autres clients
 * 4. Lorsqu'un joueur se déconnecte du réseau, on élimine (Despawn) son joueur.
 */

public class JoueurReseau : NetworkBehaviour, IPlayerLeft //1.
{
    //Variable qui sera automatiquement synchronisée par le serveur sur tous les clients
    [Networked] public Color maCouleur { get; set; }
    [Networked] public int indexJoueur { get; set; }
    [Networked] public int pointage { get; set; }

    public static JoueurReseau Local; //.2

    public Transform modeleJoueur;

    public string nomDujoueur = "Hancock";

    // Un chapeau est assigné en fonction de son index
    public GameObject[] chapeau;

    // Représente la liste des chapeau obtenu par le joueur
    List<GameObject> chapeauxObtenus = new List<GameObject>();


    /*
    * Au départ, on change la couleur du joueur. La variable maCouleur sera définie
    * par le serveur dans le script GestionnaireReseau.La fonction Start() sera appelée après la fonction Spawned().
    */
    private void Start()
    {
        GetComponentInChildren<MeshRenderer>().material.color = maCouleur;
        ActiverChapeau();
    }

    public override void Spawned() //3.
    {
        // Object est l'équivalant de gameObject pour les objets réseaux
        if (Object.HasInputAuthority)
        {
            Local = this;

            //Si c'est le joueur du client, on appel la fonction pour le rendre invisible
            Utilitaires.SetRenderLayerInChildren(modeleJoueur, LayerMask.NameToLayer("JoueurLocal"));

            //On désactive la mainCamera. Assurez-vous que la caméra de départ possède bien le tag MainCamera
            Camera.main.gameObject.SetActive(false);

            Debug.Log("Un joueur local a été créé");
        }
        else
        {
            //Si le joueur créé est contrôlé par un autre joueur, on désactive le component caméra de cet objet
            Camera camLocale = GetComponentInChildren<Camera>();
            camLocale.enabled = false;

            // On désactive aussi le component AudioListener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;

            Debug.Log("Un joueur réseau a été créé");
        }
    }

    public void ActiverChapeau()
    {
        chapeau[indexJoueur].SetActive(true);
    }

    public void PlayerLeft(PlayerRef player) //.4
    {
        if (player == Object.InputAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}