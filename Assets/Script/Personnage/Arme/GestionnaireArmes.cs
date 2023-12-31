using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // ne pas oublier ce namespace

/* Script qui g�re le tir du joueur qui d�rive de NetworkBehaviour
 * Variables :
 * - ilTir : variable r�seau [Networked] qui peut �tre modifi�e uniquement par le serveur (stateAuthority) et
 * qui sera synchronis� sur tous les clients
 * (OnChanged = nameof(OnTir)) : Sp�cifie la fonction static a ex�cuter quand la variable change. Dans
 * ce cas, d�s que la variable ilTir change, la fonction OnTir() sera appel�e.
 * 
 * - tempsDernierTir : pour limiter la cadence de tir
 * - delaiTirLocal : delai entre 2 tir (local)
 * - delaiTirServeur:delai entre 2 tir (r�seau)
 * 
 * - origineTir : point d'origine du rayon g�n�r� pour le tir (la cam�ra)
 * - layersCollisionTir : layers � consid�rer pour la d�tection de collision. 
 *   En choisir deux dans l'inspecteur: Default et HitBoxReseau
 * - distanceTir : la distance de port�e du tir
 * 
 * - particulesTir : le syst�me de particules � activer � chaque tir. D�finir dans l'inspecteur en
 * glissant le l'objet ParticulesTir qui est enfant du fusil
 * 
 * - gestionnairePointsDeVie : R�f�rence au script GestionnairePointDevie
 */

public class GestionnaireArmes : NetworkBehaviour
{

    [Networked(OnChanged = nameof(OnTir))]
    public bool ilTir { get; set; } // variable r�seau peuvent seulement �tre chang�e par le serveur (stateAuthority)

    float tempsDernierTir = 0;
    float delaiTirLocal = 0.15f;
    float delaiTirServeur = 0.1f;

    // pour le raycast
    public Transform origineTir; // d�finir dans Unity avec la cam�ra
    public LayerMask layersCollisionTir; // d�finir dans Unity
    public float distanceTir = 100f;

    [Header("Prefabs")]
    public GameObject prefabGrenade; // Prefab de la grenade � d�finir dans l'inspecteur
    public GameObject prefabFusee; // Prefab de la fus�e. � d�finir dans l'inspecteur
    public ParticleSystem particulesTir;


    GestionnairePointsDeVie gestionnairePointsDeVie;
    JoueurReseau joueurReseau;
    NetworkObject networkObject;

    //Timer r�seau
    TickTimer delaiTirGrenade = TickTimer.None;
    TickTimer delaiTirfusee = TickTimer.None;

    string nomTireur;

    /*
     * On garde en m�moire le component (script) GestionnairePointsDeVie pour pouvoir
     * communiquer avec lui.
     */
    void Awake()
    {
        gestionnairePointsDeVie = GetComponent<GestionnairePointsDeVie>();
        joueurReseau = GetComponent<JoueurReseau>();
        networkObject = GetComponent<NetworkObject>();
    }


    /*
    * Fonction qui d�tecte le tir et d�clenche tout le processus
    * 1. Si le joueur est mort, on ne veut pas qu'il puisse tirer. On quitte la fonction imm�diatement
    * 2.On r�cup�re les donn�es enregistr�es dans la structure de donn�es donneesInputReseau et on 
    * v�rifie la variable appuieBoutonTir. Si elle est � true, on active la fonction TirLocal en passant
    * comme param�tre le vector indiquant le devant du personnage.
    */
    public override void FixedUpdateNetwork()
    {
        //1.
        if (gestionnairePointsDeVie.estMort)
            return;
        //2.
        if (GetInput(out DonneesInputReseau donneesInputReseau))
        {
            if (donneesInputReseau.appuieBoutonTir)
            {
                TirLocal(donneesInputReseau.vecteurDevant);
            }

            /*V�rification de la strucutre r�seau. Est-ce que le joueur a appuy� sur le bouton 
            * pour lancer une grenade. Si oui, appele de la fonction LanceGrenade en passant la direction du joueur
            */
            if (donneesInputReseau.appuieBoutonGrenade)
            {
                LanceGrenade(donneesInputReseau.vecteurDevant);
            }
            if (donneesInputReseau.appuieBoutonFusee)
            {
                LanceFusee(donneesInputReseau.vecteurDevant);
            }
        }
    }


    /* Gestion locale du tir (sur le client)
    * 1.On sort de la fonction si le tir ne respecte pas le d�lai entre 2 tir.
    * 2.Appel de la coroutine qui activera les particules et lancera le Tir pour le r�seau (autres clients)
    * 3.Raycast r�seau propre � Fusion avec une compensation de d�lai.
    * Param�tres:
    *   - origineTir.position (vector3) : position d'origine du rayon;
    *   - vecteurDevant (vector3) : direction du rayon;
    *   - distanceTir (float) : longueur du rayon
    *   - Object.InputAuthority : Indique au serveur le joueur � l'origine du tir
    *   - out var infosCollisions : variable pour r�cup�rer les informations si le rayon touche un objet
    *   - layersCollisionTir : indique les layers sensibles au rayon. Seuls les objets sur ces layers seront consid�r�s.
    *   - HitOptions.IncludePhysX : pr�cise quels types de collider sont sensibles au rayon.IncludePhysX permet
    *   de d�tecter les colliders normaux en plus des collider fusion de type Hitbox.
    * 4.Variable locale ToucheAutreJoueur est initialis�ee � false.
    * Variable locale distanceTouche : r�cup�re la distance entre l'origine du rayon et le point d'impact
    * 5.V�rification du type d'objet touch� par le rayon.
    * - Si c'est un hitbox (objet r�seau), on change la variable toucheAutreJoueur
    * - Si c'est un collider normal, on affiche un message dans la console
    * 6.D�bogage : pour voir le rayon. Seulement visible dans l'�diteur
    * 7.M�morisation du temps du tir. Servira pour emp�cher des tirs trop rapides. */
    void TirLocal(Vector3 vecteurDevant)
    {
        //1.
        if (Time.time - tempsDernierTir < delaiTirLocal)
            return;
        //2.
        StartCoroutine(EffetTirCoroutine());

        //3.
        Runner.LagCompensation.Raycast(origineTir.position, vecteurDevant, distanceTir, Object.InputAuthority, out var infosCollisions, layersCollisionTir, HitOptions.IncludePhysX);

        //4.
        bool toucheAutreJoueur = false;
        float distanceJoueurTouche = infosCollisions.Distance;

        //5.
        if (infosCollisions.Hitbox != null)
        {
            Debug.Log($"{Time.time} {transform.name} a touch� le joueur {infosCollisions.Hitbox.transform.root.name}");
            toucheAutreJoueur = true;

            // si nous sommes sur le code ex�cut� sur le serveur :
            // On appelle la fonction PersoEstTouche du joueur touch� dans le script GestionnairePointsDeVie
            if (Object.HasStateAuthority)
            {
                infosCollisions.Hitbox.transform.root.GetComponent<GestionnairePointsDeVie>().PersoEstTouche(joueurReseau, 1);
            }
        }
        else if (infosCollisions.Collider != null)
        {
            Debug.Log($"{Time.time} {transform.name} a touch� l'objet {infosCollisions.Collider.transform.root.name}");
        }

        //6. 
        if (toucheAutreJoueur)
        {
            Debug.DrawRay(origineTir.position, vecteurDevant * distanceJoueurTouche, Color.red, 1);
        }
        else
        {
            Debug.DrawRay(origineTir.position, vecteurDevant * distanceJoueurTouche, Color.green, 1);
        }
        //7.
        tempsDernierTir = Time.time;
    }


    /* Coroutine qui d�clenche le syst�me de particules localement et qui g�re la variable bool ilTir en l'activant
     * d'abord (true) puis en la d�sactivant apr�s un d�lai d�fini dans la variable delaiTirServeur.
     * Important : souvenez-vous de l'expression [Networked(OnChanged = nameof(OnTir))] associ�e � la
     * variable ilTir. En changeant cette variable ici, la fonction OnTir() sera automatiquement appel�e.
     */
    IEnumerator EffetTirCoroutine()
    {
        ilTir = true; // comme la variable networked est chang�, la fonction OnTir sera appel�e
        if (Object.HasInputAuthority)
        {
            if(!Runner.IsResimulation) particulesTir.Play();
        }
        yield return new WaitForSeconds(delaiTirServeur);
        ilTir = false;
    }

    /* Fonction static (c'est oblig�...) appel�e par le serveur lorsque la variable ilTir est modifi�e
    * Note importante : dans une fonction static, on ne peut acc�der aux variables et fonctions instanci�es
    * 1.var locale bool ilTirValeurActuelle : r�cup�ration de la valeur actuelle de la variable ilTir
    * 2.Commande qui permet de charger l'ancienne valeur de la variable
    * 3.var locale ilTirValeurAncienne : r�cup�ration de l'ancienne valeur de la variable ilTir
    * 4.Appel de la fonction TirDistant() seulement si ilTirValeurActuelle = true 
    * et ilTirValeurAncienne = false. Permet de limiter la cadance de tir.
    * Notez la fa�on particuli�re d'appeler une fonction instanci�e � partir d'une fonction static.*/
    static void OnTir(Changed<GestionnaireArmes> changed)
    {
        Debug.Log($"{Time.time} Valeur OnTir() = {changed.Behaviour.ilTir}");

        // Dans fonction static, on ne peut pas changer ilTir = true, Utiliser changed.Behaviour.ilTir
        //1.
        bool ilTirValeurActuelle = changed.Behaviour.ilTir;
        //2.
        changed.LoadOld(); // charge la valeur pr�c�dente de la variable;
        //3.
        bool ilTirValeurAncienne = changed.Behaviour.ilTir;
        //4.
        if (ilTirValeurActuelle && !ilTirValeurAncienne) // pour tirer seulement une fois
            changed.Behaviour.TirDistant(); // appel fonction non static dans fonction static
    }


    /* Fonction qui permet d'activer le syst�me de particule pour le personnage qui a tir�
     * sur tous les client connect�s. Sur l'ordinateur du joueur qui a tir�, l'activation du syst�me
     * de particules � d�j� �t� faite dans la fonction TirLocal(). Il faut cependant s'assurer que ce joueur
     * tirera aussi sur l'ordinateur des autres joueurs.
     * On d�clenche ainsi le syst�me de particules seulement si le client ne poss�de pas le InputAuthority
     * sur le joueur.
     * 
     */
    void TirDistant()
    {
        //seulement pour les objets distants (par pour le joueur local)
        if (!Object.HasInputAuthority) particulesTir.Play();
    }


    /* Fonction qui permet de faire apparaitre une grenade (spawn)sur tous les client connect�s.
     * Param�tre vecteurDevant : Orientation du personnage dans le monde. La grenade sera lanc� dans cette direction
     * 1.V�rification du Timer. S'il est expir� ou ne s'ex�cute pas :
     * 2.Calcul de la position de d�part de la grenade qui sera cr��e.Pour �viter un contact avec soi-m�me, on s'assure de
     * la faire appara�tre un peu plus loins devant.
     * 3. Calcul de l'orientation de d�part de la grenade qui sera cr��e. Son axe des Z sera orient� selon 
     * l'axe des Z du personnage. Peu utile puisque la grenade est une sph�re. Utile pour d'autres formes d'objet.
     * 
     * 4.Cette commande est propre au serveur et ne sera pas ex�cut�e sur les clients.
     * G�n�ration d'une grenade (spawn) � la position et orientation d�termin�es.
     * Il faut �galement pr�cis� le joueur qui aura le InputAuthority sur cette grenade
     * 
     * La partie suivante est une expression Lambda permettant une fonction anonyme qui s'ex�cutera tout juste apr�s
     * la cr�ation (spawn) de la grenade.Voici son fonctionnement : Lorsque le spawn est fait, deux param�tres sont re�us,
     * soit runner et laGrenade. laGrenade contient la r�f�rence au NetworkObjet cr�� (la grenade...). On appelle la fonction
     * LanceGrenade() pr�ssente dans le script GestionnaireGrenade de la nouvelle grenade cr��e. Trois param�tres sont pass�s:
     * A- L'orientation du devant du joueur multipli� par 15. Cela deviendra la force du lancer.
     * B- Le joueur qui a l'InputAuthority sur la grenade. Cela deviendra le lanceur
     * C- Le nom du joueur qui lance la grenade. Cela deviendra le nom du lanceur
     * 
     * //5. Timer propre a fusion. Permet de cr�er une temporisation pour �viter qu'on puisse tirer des grenades trop rapidement.
     */
    void LanceGrenade(Vector3 vecteurDevant)
    {
        //1.
        if (delaiTirGrenade.ExpiredOrNotRunning(Runner))
        {
            //2.
            Vector3 positionGrenade = origineTir.position + vecteurDevant * 1.5f;
            //3.
            Quaternion orientationGrenade = Quaternion.LookRotation(vecteurDevant);
            //4.
            // commande ex�cut�e juste sur le serveur
            Runner.Spawn(prefabGrenade, positionGrenade, orientationGrenade, Object.InputAuthority, (runner, laGrenade) =>
            {
                laGrenade.GetComponent<GestionnaireGrenade>().LanceGrenade(vecteurDevant * 15, Object.InputAuthority, joueurReseau);
            });
            //5.
            delaiTirGrenade = TickTimer.CreateFromSeconds(Runner, 1f);
        }
    }


    /* Fonction qui permet de faire apparaitre une fus�e (spawn)sur tous les client connect�s.
     * Param�tre vecteurDevant : Orientation du personnage dans le monde. La fus�e sera lanc� dans cette direction
     * 1.V�rification du Timer. S'il est expir� ou ne s'ex�cute pas :
     * 2.Calcul de la position de d�part de la fus�e qui sera cr��e.Pour �viter un contact avec soi-m�me, on s'assure de
     * la faire appara�tre un peu plus loins devant.
     * 3. Calcul de l'orientation de d�part de la fus�e qui sera cr��e. Son axe des Z sera orient� selon 
     * l'axe des Z du personnage.
     * 
     * 4.Cette commande est propre au serveur et ne sera pas ex�cut�e sur les clients.
     * G�n�ration d'une fus�e (spawn) � la position et orientation d�termin�es.
     * Il faut �galement pr�cis� le joueur qui aura le InputAuthority sur cette fus�e
     * 
     * La partie suivante est une expression Lambda permettant une fonction anonyme qui s'ex�cutera tout juste apr�s
     * la cr�ation (spawn) de la fus�e.Voici son fonctionnement : Lorsque le spawn est fait, deux param�tres sont re�us,
     * soit runner et laFusee. laFusee contient la r�f�rence au NetworkObjet cr�� (la fus�e...). On appelle la fonction
     * LanceFus�e() pr�ssente dans le script GestionnaireFusee de la nouvelle fus�e cr��e. Trois param�tres sont pass�s:
     * A- Le joueur qui a l'InputAuthority sur la fus�e. Cela deviendra le lanceur
     * B- La r�f�rence au component NetworkObject de joueur qui lance la fus�e. Cela deviendra lanceurNetworkObject
     * C- Le nom du joueur qui lance la fus�e. Cela deviendra le nom du lanceur
     * 
     * //5. Timer propre a fusion. Permet de cr�er une temporisation pour �viter qu'on puisse tirer des fus�es trop rapidement.
     */
    private void LanceFusee(Vector3 vecteurDevant)
    {
        //1.
        if (delaiTirfusee.ExpiredOrNotRunning(Runner))
        {
            //2.
            Vector3 positionFusee = origineTir.position + vecteurDevant * 1.5f;
            //3.
            Quaternion orientationFusee = Quaternion.LookRotation(vecteurDevant);

            //4.
            // commande ex�cut�e juste sur le serveur uniquement
            Runner.Spawn(prefabFusee, positionFusee, orientationFusee, Object.InputAuthority, (runner, laFusee) =>
            {
                laFusee.GetComponent<GestionnaireFusee>().LanceFusee(Object.InputAuthority, networkObject, joueurReseau);
            });
            //5.
            delaiTirfusee = TickTimer.CreateFromSeconds(Runner, 3f);
        }
    }
}