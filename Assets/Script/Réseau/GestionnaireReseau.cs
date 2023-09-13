using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;

public class GestionnaireReseau : MonoBehaviour, INetworkRunnerCallbacks
{
    //Contient une référence au component NetworkRunner
    NetworkRunner _runner;

    // Contient la référence au script JoueurReseau du Prefab
    public JoueurReseau joueurPrefab;

    // Pour compteur le nombre de joueurs connectés
    public int nbJoueurs = 0;

    // Tableau de couleurs à définir dans l'inspecteur
    public Color[] couleurJoueurs;

    // pour mémoriser le component GestionnaireMouvementPersonnage du joueur
    GestionnaireInputs gestionnaireInputs; 

    // Fonction asynchrone pour démarrer Fusion et créer une partie 
    async void CreationPartie(GameMode mode)
    {
        /*  1.Ajout du component NetworkRunner au gameObject. On garde en mémoire
            la référence à ce component dans la variable _runner.
            2.Indique au NetworkRunner qu'il doit fournir les entrées (inputs) au  
            simulateur (Fusion)
        */
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        /*Méthode du NetworkRunner qui permet d'initialiser une partie
         * GameMode : reçu en argument. Valeur possible : Client, Host, Server, 
           AutoHostOrClient, etc.)
         * SessionName : Nom de la chambre (room) pour cette partie
         * Scene : la scène qui doit être utilisée pour la simulation
         * SceneManager : référence au component script 
          NetworkSceneManagerDefault qui est ajouté au même moment
         */
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "Chambre test",
            Scene = SceneManager.GetActiveScene().buildIndex,
            PlayerCount = 9, //ici, on limite à 9 joueurs
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()

        });
    }

    // Start is called before the first frame update
    void Start()
    {
        // Création d'une partie dès le départ
        CreationPartie(GameMode.AutoHostOrClient);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }


    /* Fonction du Runner pour définir les inputs du client dans la simulation
         * 1. On récupère le component GestionnaireInputs du joueur local
         * 2. On définit (set) le paramètre input en lui donnant la structure de données (struc) qu'on récupère
         * en appelant la fonction GetInputReseau du script GestionnaireInputs. Les valeurs seront mémorisées
         * et nous pourrons les utilisées pour le déplacement du joueur dans un autre script. Ouf...*/
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        //1.
        if (gestionnaireInputs == null && JoueurReseau.Local != null)
        {
            gestionnaireInputs = JoueurReseau.Local.GetComponent<GestionnaireInputs>();
        }

        //2.
        if (gestionnaireInputs != null)
        {
            input.Set(gestionnaireInputs.GetInputReseau());
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }

    /* Lorsqu'un joueur se connecte au serveur
     * 1.On vérifie si ce joueur est aussi le serveur. Si c'est le cas, on spawn un prefab de joueur.
     * Bonne pratique : la commande Spawn() devrait être utilisée seulement par le serveur
    */
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            Debug.Log("Un joueur s'est connecté comme serveur. Spawn d'un joueur");
            /*On garde la référence au nouveau joueur créé par le serveur. La variable locale
             créée est de type JoueurReseau (nom du script qui contient la fonction Spawned()*/
            JoueurReseau nvJoueur = _runner.Spawn(joueurPrefab, Utilitaires.GetPositionSpawnAleatoire(), Quaternion.identity, player);
            /*On change la variable maCouleur du nouveauJoueur et on augmente le nombre de joueurs connectés
            Comme j'ai seulement 9 couleurs de définies, je m'assure de ne pas dépasser la longueur de mon tableau*/
            nvJoueur.maCouleur = couleurJoueurs[nbJoueurs];
            nvJoueur.indexJoueur = nbJoueurs;
            nbJoueurs++;
            if (nbJoueurs >= 9) nbJoueurs = 0;
        }
        else
        {
            Debug.Log("Un joueur s'est connecté comme client. Spawn d'un joueur");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }

    /*
     * Fonction appelée lorsqu'une connexion réseau est refusée ou lorsqu'un client perd
     * la connexion suite à une erreur réseau. Le paramètre ShutdownReason est une énumération (enum)
     * contenant différentes causes possibles.
     */
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason == ShutdownReason.GameIsFull)
        {
            Debug.Log("Le maximum de joueur est atteint. Réessayer plus tard");
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }
}