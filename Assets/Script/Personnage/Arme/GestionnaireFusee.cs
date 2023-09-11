using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/*Script qui g�re la fus�e, une fois qu'elle est spawn� par le serveur
 * Variables :
 * - GameObject prefabExplosion: Le prefab avec particules qui sera utilis� pour l'explosion. � d�finir dans l'inspecteur.
 * - Transform pointDetectionCollision : point d'origine de la sphere qui sera cr��e pour la d�tection de collision.
 * A d�finir dans l'inspecteur.
 * - LayerMask layersCollision : D�termine le layer qui sera sensible � la d�tection de collision
 * - TickTimer dureeVieFusee : Timer propre a fusion. D�sactiv� au d�part. Permettra de cr�er une temporisation pour l'explosion
 * de la fus�e apr�s un certain temps.
 * - List<LagCompensatedHit> infosCollisionsList: Liste qui contiendra les objets touch�s lors de l'explosion de la fus�e
 * - int vitesseFusee : la vitesse de d�placement de la fus�e
 * - PlayerRef lanceur: R�f�rence au joueur qui a lanc� la fus�e
 * - string nomLanceur: Le nom du joueur qui lance la fus�e
 * - NetworkObject networkObject: R�f�rence au component NetworkObject de la fus�e
 * - NetworkObject lanceurNetworkObject:R�f�rence au component NetworkObject du joueur qui lance la fus�e
 */
public class GestionnaireFusee : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject prefabExplosion;

    [Header("D�tection de collisions")]
    public Transform pointDetectionCollision;
    public LayerMask layersCollision;

    TickTimer dureeVieFusee = TickTimer.None;
    List<LagCompensatedHit> infosCollisionsList = new List<LagCompensatedHit>();

    int vitesseFusee = 20;

    PlayerRef lanceur;
    string nomLanceur;
    NetworkObject networkObject;
    NetworkObject lanceurNetworkObject;


    /*
  * Fonction appeler par le script GestionnaireArmes. Elle g�re la fus�e tout juste apr�s son spawning
  * Param�tres :
  * - PlayerRef lanceur : le joueur qui lance la fus�e
  * - NetworkObject lanceurNetworkObject : r�f�rence au component NetworkObject du joueur qui lance la fus�e
  * - string nomLanceur : le nom du joueur qui lance la fus�e
  * 1. On m�morise la r�f�rence au component NetworkObject.
  * 2. On m�morise dans des variables : le joueur qui lance la fus�e ainsi que son nom et �galement son component
  * Networkobject. Notez l'utilisation du "this" qui permet de distinguer la variable du param�tre de la fonction qui 
  * porte le m�me nom.
  * 3.Cr�ation d'un timer r�seau (TickTimer) d'une dur�e de 10 secondes
  */
    public void LanceFusee(PlayerRef lanceur, NetworkObject lanceurNetworkObject, string nomLanceur)
    {
        //1.
        networkObject = GetComponent<NetworkObject>();
        //2.
        this.lanceur = lanceur;
        this.nomLanceur = nomLanceur;
        this.lanceurNetworkObject = lanceurNetworkObject;
        //3.
        dureeVieFusee = TickTimer.CreateFromSeconds(Runner, 10);
    }

    /*
        * Fonction qui s'occupe du d�placement et qui g�re l'explosion de la fus�e et la d�tection de collision
        * 1.D�placement de la fus�e dans son axe des Z (son devant)
        * 2.Si on est sur le serveur et que le timer de 10 secondes est expir�, le serveur d�truit (despawn) la fus�e qui explosera.
        * La suite du script s'ex�cutera seulement si le timer n'est pas expir�
        * 3. Premi�re v�rification avec un OverlapSphere tout petit (rayon de 0.5 m�tre). L'objectif est d'�viter la d�tection
        * de proximit� du joueur qui tire la fus�e lorsqu'il vient juste de tirer la fus�e
        * 4. On passe � travers la liste et si on d�tecte la pr�sence de joueur qui tire, on met la variable contactValide � false
        * 5. Seulement si contactValide est true, on fait un deuxi�me OverlapSphere avec un rayon plus grand et on passe � travers 
        * la liste des objets d�tect�s. 
        * - On tente de r�cup�rer le component (script) gestionnairePointsDeVie de l'objet touch�. Renverra null si l'objet n'en 
        * poss�de pas.
        * - Si l'objet poss�de un component gestionnairePointsDeVie, c'est qu'il s'agit d'un joueur. On peut alors appeler la 
        * fonction PersoEstTouche() pr�sente dans ce script. On envoie en param�tres le nom du lanceur de fus�e et le 
        * dommage a appliquer
        * 6.Le serveur fait dispara�tre (Despawn) la fus�e (elle disparaitra sur tous les clients)
        * Pour �viter de r�p�ter plusieurs fois, on s'assure d�sactive le timer (TickTimer.None)
        */
    public override void FixedUpdateNetwork()
    {
        //1.
        transform.position += transform.forward * Runner.DeltaTime * vitesseFusee;
        //2.
        if (Object.HasStateAuthority)
        {
            if (dureeVieFusee.Expired(Runner))
            {
                Runner.Despawn(networkObject);
                return;
            }
            //
            int nbObjetsTouches = Runner.LagCompensation.OverlapSphere(pointDetectionCollision.position, 0.5f, lanceur, infosCollisionsList, layersCollision, HitOptions.IncludePhysX);

            //4.
            bool contactValide = false;
            if (nbObjetsTouches > 0)
                contactValide = true;

            foreach (LagCompensatedHit objetTouche in infosCollisionsList)
            {
                if (objetTouche.Hitbox != null)
                {
                    //est-ce qu'on se touche soit m�me
                    if (objetTouche.Hitbox.Root.GetBehaviour<NetworkObject>() == lanceurNetworkObject)
                        contactValide = false;
                }
            }
            //5.
            if (contactValide)
            {
                nbObjetsTouches = Runner.LagCompensation.OverlapSphere(pointDetectionCollision.position, 4f, lanceur, infosCollisionsList, layersCollision, HitOptions.None);
                foreach (LagCompensatedHit objetTouche in infosCollisionsList)
                {
                    GestionnairePointsDeVie gestionnairePointsDeVie = objetTouche.Hitbox.transform.root.GetComponent<GestionnairePointsDeVie>();
                    if (gestionnairePointsDeVie != null)
                        gestionnairePointsDeVie.PersoEstTouche(nomLanceur, 20);
                }
                //6.
                Runner.Despawn(networkObject);
            }
        }
    }

    /*
     * Override de la fonction Despawned. Fontion ex�cut� automatiquement sur tous les clients, au moment ou l'objet fus�e est
     * retir� du jeu (Despawned)
     * Chaque client va instancier localement un syst�me de particule d'explosion � la position d�terminer, soit l'avant de la fus�e
     */
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instantiate(prefabExplosion, pointDetectionCollision.position, Quaternion.identity);
    }
}