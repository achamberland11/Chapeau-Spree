using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
/* Script qui gère la grenade, une fois qu'elle est spawné par le serveur
 * Variables :
 * - GameObject prefabExplosion : Le prefab avec particules qui sera utilisé pour l'explosion. À définir dans l'inspecteur.
 * - LayerMask layersCollision : Détermine le layer qui sera sensible à la détection de collision
 * - PlayerRef lanceur: Référence au joueur qui a lancé la grenade
 * - string nomLanceur: Le nom du joueur qui lance la grenade
 * - TickTimer timerExplosion : Timer propre a fusion. Désactivé au départ. Permettra de créer une temporisation pour l'explosion
 * de la grenade.
 * - List<LagCompensatedHit> infosCollisionsList: Liste qui contiendra les objets touchés lors de l'explosion de la grenade
 * - NetworkObject networkObject: Référence au component NetworkObject de la grenade
 * - NetworkRigidbody networkRigidbody:Référence au component networkRigidbody de la grenade
 */
public class GestionnaireGrenade : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject prefabExplosion;

    [Header("Détection de collisions")]
    public LayerMask layersCollision;

    PlayerRef lanceur;
    string nomLanceur;

    TickTimer timerExplosion = TickTimer.None;
    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();

    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;

    /*
   * Fonction appeler par le script GestionnaireArmes. Elle gère la grenade tout juste après son spawning
   * Paramètres :
   * - Vector3 forceDuLance : la force (et la direction) qui doit être appliquée à la grenade
   * - PlayerRef lanceur : le joueur qui lance la grenade
   * - string nomLanceur : le nom du joueur qui lance la grenade
   * 1. On mémorise la référence aux components NetworkObject et NetworkRigidbody
   * 2. On applique une force "impulse" à la grenade. La valeur de la force appliquée est reçu en paramètre (force du lancer)
   * 3. On mémorise dans des variables : le joueur qui lance la grenade ainsi que son nom. Notez l'utilisation du "this"
   * qui permet de distinguer la variable du paramètre de la fonction qui porte le même nom.
   * 4.Création d'un timer réseau (TickTimer) d'une durée de 2 secondes
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
    * Fonction qui s'exécute sur le serveur uniquement et qui gère l'explosion de la grenade et la détection de collision
    * 1. Si on est sur le serveur et que le timer de 2 secondes est expiré.
    * 2. Vérification de la présence de joueur à proximité de l'explosion. Pour y arriver on créer une sphere invisible
    * "OverlapSphere":
    * - La sphere prendra origine à la position de la grenade et aura un rayon de 4 mètres. 
    * - On doit également indiquer quel joueur à lancé la grenade
    * - Préciser une liste qui contiendra les objets détectés par la sphère. 
    * - Finalement on précise le layer qui doit être considéré. Seuls les objets sur ce layer seront détectés.
    * 
    * 3. On passe à travers la liste des objets détectés par le OverlapSphere. Les objets contenu dans cette liste sont de type
    * LagCompensatedHit qui est propre à Fusion:
    * - On tente de récupérer le component (script) gestionnairePointsDeVie de l'objet touché. Renverra null si l'objet n'en   
        possède pas.
    * - Si l'objet possède un component gestionnairePointsDeVie, c'est qu'il s'agit d'un joueur. On peut alors appeler la 
        fonction
    * PersoEstTouche() présente dans ce script. On envoie en paramètres le nom du lanceur de grenade et le dommage a appliquer
    * 
    * 4.Le serveur fait disparaître (Despawn) la grenade (elle disparaitra sur tous les clients)
    * Pour éviter de répéter plusieurs fois, on s'assure désactive le timer (TickTimer.None)
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
    * Override de la fonction Despawned. Fontion exécuté automatiquement sur tous les clients, au moment ou l'objet grenade est
    * retiré du jeu (Despawned)
    * Chaque client va instancier localement un système de particule d'explosion à la position de la partie visuel de la 
    * grenade (meshRenderer) pour éviter un possible décalage visuel.
    */
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer meshGrenade = GetComponentInChildren<MeshRenderer>(); // pour éviter un décalage
        Instantiate(prefabExplosion, meshGrenade.transform.position, Quaternion.identity);
        Destroy(prefabExplosion, 3f);
    }
}