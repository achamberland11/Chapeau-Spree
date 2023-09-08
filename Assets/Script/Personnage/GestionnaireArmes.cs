using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // ne pas oublier ce namespace

/* Script qui gère le tir du joueur qui dérive de NetworkBehaviour
 * Variables :
 * - ilTir : variable réseau [Networked] qui peut être modifiée uniquement par le serveur (stateAuthority) et
 * qui sera synchronisé sur tous les clients
 * (OnChanged = nameof(OnTir)) : Spécifie la fonction static a exécuter quand la variable change. Dans
 * ce cas, dès que la variable ilTir change, la fonction OnTir() sera appelée.
 * 
 * - tempsDernierTir : pour limiter la cadence de tir
 * - delaiTirLocal : delai entre 2 tir (local)
 * - delaiTirServeur:delai entre 2 tir (réseau)
 * 
 * - origineTir : point d'origine du rayon généré pour le tir (la caméra)
 * - layersCollisionTir : layers à considérer pour la détection de collision. 
 *   En choisir deux dans l'inspecteur: Default et HitBoxReseau
 * - distanceTir : la distance de portée du tir
 * 
 * - particulesTir : le système de particules à activer à chaque tir. Définir dans l'inspecteur en
 * glissant le l'objet ParticulesTir qui est enfant du fusil
 * 
 * - gestionnairePointsDeVie : Référence au script GestionnairePointDevie
 */

public class GestionnaireArmes : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnTir))]
    public bool ilTir { get; set; } // variable réseau peuvent seulement être changée par le serveur (stateAuthority)

    float tempsDernierTir = 0;
    float delaiTirLocal = 0.15f;
    float delaiTirServeur = 0.1f;

    // pour le raycast
    public Transform origineTir; // définir dans Unity avec la caméra
    public LayerMask layersCollisionTir; // définir dans Unity
    public float distanceTir = 100f;

    public ParticleSystem particulesTir;

    GestionnairePointsDeVie gestionnairePointsDeVie;

    /*
     * On garde en mémoire le component (script) GestionnairePointsDeVie pour pouvoir
     * communiquer avec lui.
     */
    void Awake()
    {
        gestionnairePointsDeVie = GetComponent<GestionnairePointsDeVie>();
    }


    /*
    * Fonction qui détecte le tir et déclenche tout le processus
    * 1. Si le joueur est mort, on ne veut pas qu'il puisse tirer. On quitte la fonction immédiatement
    * 2.On récupère les données enregistrées dans la structure de données donneesInputReseau et on 
    * vérifie la variable appuieBoutonTir. Si elle est à true, on active la fonction TirLocal en passant
    * comme paramètre le vector indiquant le devant du personnage.
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
        }
    }


    /* Gestion locale du tir (sur le client)
    * 1.On sort de la fonction si le tir ne respecte pas le délai entre 2 tir.
    * 2.Appel de la coroutine qui activera les particules et lancera le Tir pour le réseau (autres clients)
    * 3.Raycast réseau propre à Fusion avec une compensation de délai.
    * Paramètres:
    *   - origineTir.position (vector3) : position d'origine du rayon;
    *   - vecteurDevant (vector3) : direction du rayon;
    *   - distanceTir (float) : longueur du rayon
    *   - Object.InputAuthority : Indique au serveur le joueur à l'origine du tir
    *   - out var infosCollisions : variable pour récupérer les informations si le rayon touche un objet
    *   - layersCollisionTir : indique les layers sensibles au rayon. Seuls les objets sur ces layers seront considérés.
    *   - HitOptions.IncludePhysX : précise quels types de collider sont sensibles au rayon.IncludePhysX permet
    *   de détecter les colliders normaux en plus des collider fusion de type Hitbox.
    * 4.Variable locale ToucheAutreJoueur est initialiséee à false.
    * Variable locale distanceTouche : récupère la distance entre l'origine du rayon et le point d'impact
    * 5.Vérification du type d'objet touché par le rayon.
    * - Si c'est un hitbox (objet réseau), on change la variable toucheAutreJoueur
    * - Si c'est un collider normal, on affiche un message dans la console
    * 6.Débogage : pour voir le rayon. Seulement visible dans l'éditeur
    * 7.Mémorisation du temps du tir. Servira pour empêcher des tirs trop rapides. */
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
            Debug.Log($"{Time.time} {transform.name} a touché le joueur {infosCollisions.Hitbox.transform.root.name}");
            toucheAutreJoueur = true;

            // si nous sommes sur le code exécuté sur le serveur :
            // On appelle la fonction PersoEstTouche du joueur touché dans le script GestionnairePointsDeVie
            if (Object.HasStateAuthority)
            {
                infosCollisions.Hitbox.transform.root.GetComponent<GestionnairePointsDeVie>().PersoEstTouche();
            }
        }
        else if (infosCollisions.Collider != null)
        {
            Debug.Log($"{Time.time} {transform.name} a touché l'objet {infosCollisions.Collider.transform.root.name}");
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


    /* Coroutine qui déclenche le système de particules localement et qui gère la variable bool ilTir en l'activant
     * d'abord (true) puis en la désactivant après un délai défini dans la variable delaiTirServeur.
     * Important : souvenez-vous de l'expression [Networked(OnChanged = nameof(OnTir))] associée à la
     * variable ilTir. En changeant cette variable ici, la fonction OnTir() sera automatiquement appelée.
     */
    IEnumerator EffetTirCoroutine()
    {
        ilTir = true; // comme la variable networked est changé, la fonction OnTir sera appelée
        if (Object.HasInputAuthority)
        {
            if(!Runner.IsResimulation) particulesTir.Play();
        }
        yield return new WaitForSeconds(delaiTirServeur);
        ilTir = false;
    }

    /* Fonction static (c'est obligé...) appelée par le serveur lorsque la variable ilTir est modifiée
    * Note importante : dans une fonction static, on ne peut accéder aux variables et fonctions instanciées
    * 1.var locale bool ilTirValeurActuelle : récupération de la valeur actuelle de la variable ilTir
    * 2.Commande qui permet de charger l'ancienne valeur de la variable
    * 3.var locale ilTirValeurAncienne : récupération de l'ancienne valeur de la variable ilTir
    * 4.Appel de la fonction TirDistant() seulement si ilTirValeurActuelle = true 
    * et ilTirValeurAncienne = false. Permet de limiter la cadance de tir.
    * Notez la façon particulière d'appeler une fonction instanciée à partir d'une fonction static.*/
    static void OnTir(Changed<GestionnaireArmes> changed)
    {
        Debug.Log($"{Time.time} Valeur OnTir() = {changed.Behaviour.ilTir}");

        // Dans fonction static, on ne peut pas changer ilTir = true, Utiliser changed.Behaviour.ilTir
        //1.
        bool ilTirValeurActuelle = changed.Behaviour.ilTir;
        //2.
        changed.LoadOld(); // charge la valeur précédente de la variable;
        //3.
        bool ilTirValeurAncienne = changed.Behaviour.ilTir;
        //4.
        if (ilTirValeurActuelle && !ilTirValeurAncienne) // pour tirer seulement une fois
            changed.Behaviour.TirDistant(); // appel fonction non static dans fonction static
    }


    /* Fonction qui permet d'activer le système de particule pour le personnage qui a tiré
     * sur tous les client connectés. Sur l'ordinateur du joueur qui a tiré, l'activation du système
     * de particules à déjà été faite dans la fonction TirLocal(). Il faut cependant s'assurer que ce joueur
     * tirera aussi sur l'ordinateur des autres joueurs.
     * On déclenche ainsi le système de particules seulement si le client ne possède pas le InputAuthority
     * sur le joueur.
     * 
     */
    void TirDistant()
    {
        //seulement pour les objets distants (par pour le joueur local)
        if (!Object.HasInputAuthority) particulesTir.Play();
    }
}
