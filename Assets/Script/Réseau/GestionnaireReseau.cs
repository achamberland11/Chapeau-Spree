using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;

public class GestionnaireReseau : MonoBehaviour, INetworkRunnerCallbacks
{
    //Contient une r�f�rence au component NetworkRunner
    NetworkRunner _runner;

    // Contient la r�f�rence au script JoueurReseau du Prefab
    public JoueurReseau joueurPrefab;

    // Pour compteur le nombre de joueurs connect�s
    public int nbJoueurs = 0;

    // Tableau de couleurs � d�finir dans l'inspecteur
    public Color[] couleurJoueurs;

    // pour m�moriser le component GestionnaireMouvementPersonnage du joueur
    GestionnaireInputs gestionnaireInputs; 

    // Fonction asynchrone pour d�marrer Fusion et cr�er une partie 
    async void CreationPartie(GameMode mode)
    {
        /*  1.Ajout du component NetworkRunner au gameObject. On garde en m�moire
            la r�f�rence � ce component dans la variable _runner.
            2.Indique au NetworkRunner qu'il doit fournir les entr�es (inputs) au  
            simulateur (Fusion)
        */
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        /*M�thode du NetworkRunner qui permet d'initialiser une partie
         * GameMode : re�u en argument. Valeur possible : Client, Host, Server, 
           AutoHostOrClient, etc.)
         * SessionName : Nom de la chambre (room) pour cette partie
         * Scene : la sc�ne qui doit �tre utilis�e pour la simulation
         * SceneManager : r�f�rence au component script 
          NetworkSceneManagerDefault qui est ajout� au m�me moment
         */
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "Chambre test",
            Scene = SceneManager.GetActiveScene().buildIndex,
            PlayerCount = 9, //ici, on limite � 9 joueurs
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()

        });
    }

    // Start is called before the first frame update
    void Start()
    {
        // Cr�ation d'une partie d�s le d�part
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


    /* Fonction du Runner pour d�finir les inputs du client dans la simulation
         * 1. On r�cup�re le component GestionnaireInputs du joueur local
         * 2. On d�finit (set) le param�tre input en lui donnant la structure de donn�es (struc) qu'on r�cup�re
         * en appelant la fonction GetInputReseau du script GestionnaireInputs. Les valeurs seront m�moris�es
         * et nous pourrons les utilis�es pour le d�placement du joueur dans un autre script. Ouf...*/
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
     * 1.On v�rifie si ce joueur est aussi le serveur. Si c'est le cas, on spawn un prefab de joueur.
     * Bonne pratique : la commande Spawn() devrait �tre utilis�e seulement par le serveur
    */
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            Debug.Log("Un joueur s'est connect� comme serveur. Spawn d'un joueur");
            /*On garde la r�f�rence au nouveau joueur cr�� par le serveur. La variable locale
             cr��e est de type JoueurReseau (nom du script qui contient la fonction Spawned()*/
            JoueurReseau nvJoueur = _runner.Spawn(joueurPrefab, Utilitaires.GetPositionSpawnAleatoire(), Quaternion.identity, player);
            /*On change la variable maCouleur du nouveauJoueur et on augmente le nombre de joueurs connect�s
            Comme j'ai seulement 9 couleurs de d�finies, je m'assure de ne pas d�passer la longueur de mon tableau*/
            nvJoueur.maCouleur = couleurJoueurs[nbJoueurs];
            nvJoueur.indexJoueur = nbJoueurs;
            nbJoueurs++;
            if (nbJoueurs >= 9) nbJoueurs = 0;
        }
        else
        {
            Debug.Log("Un joueur s'est connect� comme client. Spawn d'un joueur");
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
     * Fonction appel�e lorsqu'une connexion r�seau est refus�e ou lorsqu'un client perd
     * la connexion suite � une erreur r�seau. Le param�tre ShutdownReason est une �num�ration (enum)
     * contenant diff�rentes causes possibles.
     */
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason == ShutdownReason.GameIsFull)
        {
            Debug.Log("Le maximum de joueur est atteint. R�essayer plus tard");
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }
}