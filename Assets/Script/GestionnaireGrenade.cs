using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
/* Script qui g�re la grenade, une fois qu'elle est spawn� par le serveur
 * Variables :
 * - GameObject prefabExplosion : Le prefab avec particules qui sera utilis� pour l'explosion. � d�finir dans l'inspecteur.
 * - LayerMask layersCollision : D�termine le layer qui sera sensible � la d�tection de collision
 * - PlayerRef lanceur: R�f�rence au joueur qui a lanc� la grenade
 * - string nomLanceur: Le nom du joueur qui lance la grenade
 * - TickTimer timerExplosion : Timer propre a fusion. D�sactiv� au d�part. Permettra de cr�er une temporisation pour l'explosion
 * de la grenade.
 * - List<LagCompensatedHit> infosCollisionsList: Liste qui contiendra les objets touch�s lors de l'explosion de la grenade
 * - NetworkObject networkObject: R�f�rence au component NetworkObject de la grenade
 * - NetworkRigidbody networkRigidbody:R�f�rence au component networkRigidbody de la grenade
 */
public class GestionnaireGrenade : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject prefabExplosion;

    [Header("D�tection de collisions")]
    public LayerMask layersCollision;

    PlayerRef lanceur;
    string nomLanceur;

    TickTimer timerExplosion = TickTimer.None;
    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();

    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;

    /*
   * Fonction appeler par le script GestionnaireArmes. Elle g�re la grenade tout juste apr�s son spawning
   * Param�tres :
   * - Vector3 forceDuLance : la force (et la direction) qui doit �tre appliqu�e � la grenade
   * - PlayerRef lanceur : le joueur qui lance la grenade
   * - string nomLanceur : le nom du joueur qui lance la grenade
   * 1. On m�morise la r�f�rence aux components NetworkObject et NetworkRigidbody
   * 2. On applique une force "impulse" � la grenade. La valeur de la force appliqu�e est re�u en param�tre (force du lancer)
   * 3. On m�morise dans des variables : le joueur qui lance la grenade ainsi que son nom. Notez l'utilisation du "this"
   * qui permet de distinguer la variable du param�tre de la fonction qui porte le m�me nom.
   * 4.Cr�ation d'un timer r�seau (TickTimer) d'une dur�e de 2 secondes
   */
    public void LanceGrenade(Vector3 forceDuLance, PlayerRef lanceur, string nomLanceur)
    {
        //1.
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        //2.
        networkRigidbody.Rigidbody.AddForce(forceDuLance, ForceMode.Impulse);
        //3.
        this.lanceur = lanceur;
        this.nomLanceur = nomLanceur;
        //4.
        timerExplosion = TickTimer.CreateFromSeconds(Runner, 2);
    }

    /*
    * Fonction qui s'ex�cute sur le serveur uniquement et qui g�re l'explosion de la grenade et la d�tection de collision
    * 1. Si on est sur le serveur et que le timer de 2 secondes est expir�.
    * 2. V�rification de la pr�sence de joueur � proximit� de l'explosion. Pour y arriver on cr�er une sphere invisible
    * "OverlapSphere":
    * - La sphere prendra origine � la position de la grenade et aura un rayon de 4 m�tres. 
    * - On doit �galement indiquer quel joueur � lanc� la grenade
    * - Pr�ciser une liste qui contiendra les objets d�tect�s par la sph�re. 
    * - Finalement on pr�cise le layer qui doit �tre consid�r�. Seuls les objets sur ce layer seront d�tect�s.
    * 
    * 3. On passe � travers la liste des objets d�tect�s par le OverlapSphere. Les objets contenu dans cette liste sont de type
    * LagCompensatedHit qui est propre � Fusion:
    * - On tente de r�cup�rer le component (script) gestionnairePointsDeVie de l'objet touch�. Renverra null si l'objet n'en   
        poss�de pas.
    * - Si l'objet poss�de un component gestionnairePointsDeVie, c'est qu'il s'agit d'un joueur. On peut alors appeler la 
        fonction
    * PersoEstTouche() pr�sente dans ce script. On envoie en param�tres le nom du lanceur de grenade et le dommage a appliquer
    * 
    * 4.Le serveur fait dispara�tre (Despawn) la grenade (elle disparaitra sur tous les clients)
    * Pour �viter de r�p�ter plusieurs fois, on s'assure d�sactive le timer (TickTimer.None)
    */
    public override void FixedUpdateNetwork()
    {
        //.1
        if (Object.HasStateAuthority)
        {
            if (timerExplosion.Expired(Runner))
            {
                //2.
                int nbJoueursTouche = Runner.LagCompensation.OverlapSphere(transform.position, 4, lanceur, infosCollisionsList, layersCollision);

                //3.
                foreach (LagCompensatedHit objetTouche in infosCollisionsList)
                {
                    GestionnairePointsDeVie gestionnairePointsDeVie = objetTouche.Hitbox.transform.root.GetComponent<GestionnairePointsDeVie>();
                    if (gestionnairePointsDeVie != null)
                        gestionnairePointsDeVie.PersoEstTouche(nomLanceur, 10);
                }
                //4.
                Runner.Despawn(networkObject);
                timerExplosion = TickTimer.None;
            }
        }
    }




    /*
    * Override de la fonction Despawned. Fontion ex�cut� automatiquement sur tous les clients, au moment ou l'objet grenade est
    * retir� du jeu (Despawned)
    * Chaque client va instancier localement un syst�me de particule d'explosion � la position de la partie visuel de la 
    * grenade (meshRenderer) pour �viter un possible d�calage visuel.
    */
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer meshGrenade = GetComponentInChildren<MeshRenderer>(); // pour �viter un d�calage
        Instantiate(prefabExplosion, meshGrenade.transform.position, Quaternion.identity);
        Destroy(prefabExplosion, 3f);
    }
}