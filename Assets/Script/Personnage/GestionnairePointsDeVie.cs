using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

/* Script qui gère les dommages (points de vie) quand un joueur est touché
 * Variables :
 * 
 * ##### Pour la gestion des points de vie ################################################
 * - ptsVie : variable Networked de type byte (moins lourd qu'un int) pour les points de vie du joueur.
 *            Appel de la fonction OnPtsVieChange dès que cette variable est modifiée par le serveur.
 * - estMort : variable bool pour savoir si le joueur est mort ou pas. Appel de la fonction OnChangeEtat
 *             dès que cette variable est modifiée par le serveur.
 * - estInitialise : pour savoir si le joueur est initialisé.
 * - ptsVieDepart : le nombre de points de vie au commencement ou après un respawn 
 * 
 * ##### Pour les effets de changement de couleur quand le perso est touché ###############
 * - uiCouleurTouche:la couleur de l'image quand le perso est touché
 * - uiImageTouche : l'image qui s'affiche quand le perso est touché
 * - persoRenderer : référence au meshrenderer du pero. Servira à changer la couleur du matériel
 * - couleurNormalPerso : la couleur normal du perso
 * 
 * ##### Pour gérer la mort du perso ###############
 * - modelJoueur : référence au gameObject avec la partie visuelle du perso
 * - particulesMort_Prefab : référence au Prefab des particules de mort à instancier à la mort du perso
 * - particulesMateriel : référence au matériel utilisé par les particules de morts
 * - hitboxRoot : référence au component photon HitBoxRoot servant à la détection de collision
 * - gestionnaireMouvementPersonnage : référence au script gestionnaireMouvementPersonnage sur le perso
 * - joueurReseau : référence au script joueurReseau sur le perso
 */


public class GestionnairePointsDeVie : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnPtsVieChange))]
    byte ptsVie { get; set; } //(byte : valeur possible entre 0 et 255, aucune valeur négative)

    [Networked(OnChanged = nameof(OnChangeEtat))]
    public bool estMort { get; set; }

    bool estInitialise = false;
    const byte ptsVieDepart = 5;

    public Color uiCouleurTouche; //à définir dans l'inspecteur
    public Image uiImageTouche; //à définir dans l'inspecteur
    public MeshRenderer persoRenderer; //à définir dans l'inspecteur
    Color couleurNormalPerso;


    public GameObject modelJoueur; //à définir dans l'inspecteur
    public GameObject particulesMort_Prefab; //à définir dans l'inspecteur
    public Material particulesMateriel; //à définir dans l'inspecteur
    HitboxRoot hitboxRoot;

    GestionnaireMouvementPersonnage gestionnaireMouvementPersonnage;
    JoueurReseau joueurReseau;
    NetworkObject networkObject;

    public GameObject prefabChapeau;


    /*
     * On garde en mémoire la référence au component HitBoxRoot ainsi que les références à deux
     * components (scripts) sur le perso : GestionnaireMouvementPersonnage et JoueurReseau
     */
    private void Awake()
    {
        hitboxRoot = GetComponent<HitboxRoot>();
        gestionnaireMouvementPersonnage = GetComponent<GestionnaireMouvementPersonnage>();
        joueurReseau = GetComponent<JoueurReseau>();
        networkObject = GetComponent<NetworkObject>();
    }

    /*
      * Initialisation des variables à l'apparition du personnage. On garde aussi en mémoire la couleur
      * du personnage.
      */
    void Start()
    {
        ptsVie = ptsVieDepart;
        estMort = false;
        estInitialise = true;
        couleurNormalPerso = persoRenderer.material.color;
    }

    /* Fonction publique exécutée uniquement par le serveur dans le script GestionnairesArmes du joueur qui
     * a tiré.
     * 1. On quitte la fonction immédiatement si le joueur touché est déjà mort
     * 2. Soustraction d'un point de vie
     * 3. Si les points de vie sont à 0 (ou moins), la variable estMort est mise à true et on appelle
     * la coroutine RessurectionServeur_CO qui gérera un éventuel respawn du joueur
     * Important : souvenez-vous que les variables ptsVie et estMort sont de type [Networked] et qu'une 
     * fonction sera automatiquement appelée lorsque leur valeur change.
    */
    public void PersoEstTouche(JoueurReseau dommageFaitParQui, byte dommage)
    {
        //1.
        if (estMort)
            return;

        // Pour éviter que la variable ptsVie (de type byte) descende en bas de 0. Rappelons que la valeur
        // Rappelons qu'un byte se situe entre 0 et 255.
        if (dommage > ptsVie)
            dommage = ptsVie;

        //2.
        ptsVie -= dommage;
        Debug.Log($"{Time.time} {transform.name} est touché. Il lui reste {ptsVie} points de vie");

        //3.
        if (ptsVie <= 0)
        {
            Debug.Log($"{Time.time} {transform.name} est mort");
            StartCoroutine(RessurectionServeur_CO());
            estMort = true;

            /*Mise à jour du pointage du joueur qui a causé la mort en appelant la fonction 
             * ChangementPointage() de ce joueur */
            //dommageFaitParQui.GetComponent<GestionnairePointage>().ChangementPointage(dommageFaitParQui.nomDujoueur.ToString(), 1);
        }
    }

    /* Enumarator qui attend 2 secondes et qui appelle ensuite la fonction DemandeRespawn
     * du script gestionnaireMouvementPersonnage.
    */
    IEnumerator RessurectionServeur_CO()
    {
        yield return new WaitForSeconds(2);
        gestionnaireMouvementPersonnage.DemandeRespawn();
    }

    /* Fonction appelée lorqu'on détecte une diminution de la variable ptsVie. On sort de la fonction
     * si la variable estInitialise = false (donc avant le start).
     * Appel de la coroutine EffetTouche_CO() qui gérera les effets visuels
     */
    private void ReductionPtsVie()
    {
        if (!estInitialise)
            return;

        StartCoroutine(EffetTouche_CO());
    }

    /* Coroutine qui gère les effets visuels lorsqu'un joueur est touché.
     * 1. Changement de la couleur du joueur pour blanc
     * 2. Changement de la couleur de l'image servant à indiquer au joueur qu'il est touché.
     *    Cette commande est effectuée seulement sur le client qui contrôle le joueur touché
     * 3. Après un délai de 0.2 secondes, on remet la couleur normale au joueur touché
     * 4. On change la couleur de l'image servant à indiquer au joueur qu'il est touché. L'important dans
     *    cette commande est qu'on met la valeur alpha à 0 (complètement transparente) pour la faire disparaître.
     *    Cette commande est effectuée seulement sur le client qui contrôle le joueur touché et que le joueur
     *    touché n'est pas mort.
    */
    IEnumerator EffetTouche_CO()
    {
        //.1
        persoRenderer.material.color = Color.white;
        //2.
        if (Object.HasInputAuthority)
            uiImageTouche.color = uiCouleurTouche;
        //3.
        yield return new WaitForSeconds(0.2f);
        persoRenderer.material.color = couleurNormalPerso;
        //4.
        if (Object.HasInputAuthority && !estMort)
            uiImageTouche.color = new Color(0, 0, 0, 0);
    }

    /* Fonction statique appelée automatiquement lorsque que la variable [Networked] ptsVie est modifiée
     * IMPORTANT : les fonctions statiques ne peuvent pas modifier des variables ou appeler des fonctions
     * instanciées (non static)
     * Paramètre Changed<GestionnairePointsDeVie> changed : propre a Fusion pour permettre de gérer les
     * valeurs actuelles ou anciennes des variables.
     
     * 1.Mémorisation de la valeur actuelle de la variable ptsVie dans une variable locale
     * Notez bien comment on réussit à récupérer la valeur de la variable non static ptsVies
     * 2.Commande qui permet de charger les anciennes valeurs des variables [Networked]
     * 3.Mémorisation de l'ancienne valeur de la variable ptsVie.
     * 4.Appel de la fonction ReductionPtsVie seulement si on détecte une diminution de la variable
     *   ptsVie. Cela permet de ne pas appeler la fonction lorsqu'on initialise la variable ptsDeVie
     *   au départ ou après un respawn.
     */
    static void OnPtsVieChange(Changed<GestionnairePointsDeVie> changed)
    {
        Debug.Log($"{Time.time} Valeur PtsVie = {changed.Behaviour.ptsVie}");
        //1.
        byte nouveauPtsvie = changed.Behaviour.ptsVie;
        //2.
        changed.LoadOld();
        //3.
        byte ancienPtsVie = changed.Behaviour.ptsVie;
        //4.
        if (nouveauPtsvie < ancienPtsVie)
            changed.Behaviour.ReductionPtsVie(); // pour appeler fonction non statique
    }

    /* Fonction statique appelée automatiquement lorsque que la variable [Networked] estMort est modifiée
     * IMPORTANT : les fonctions statiques ne peuvent pas modifier des variables ou appeler des fonctions
     * instanciées (non static)
     * Paramètre Changed<GestionnairePointsDeVie> changed : propre a Fusion pour permettre de gérer les
     * valeurs actuelles ou anciennes des variables.
     
     * 1.Mémorisation de la valeur actuelle de la variable estMort dans une variable locale
     * Notez bien comment on réussit à récupérer la valeur de la variable non static estMort
     * 2.Commande qui permet de charger les anciennes valeurs des variables [Networked]
     * 3.Mémorisation de l'ancienne valeur de la variable estMort.
     * 4.Appel de la fonction Mort() seulement quand la valeur actuelle de la variable estMort est true
     *   Appel de la fonction Ressurection() quand la valeur actuelle de la variable estMort est false
     *   et que l'ancienne valeur de la variable estMort est true. Donc, quand le joueur était mort et qu'on
     *   change la variable estMort pour la mettre à false.
     */
    static void OnChangeEtat(Changed<GestionnairePointsDeVie> changed)
    {
        Debug.Log($"{Time.time} Valeur estMort = {changed.Behaviour.estMort}");
        //1.
        bool estMortNouveau = changed.Behaviour.estMort;
        //2.
        changed.LoadOld();
        //3.
        bool estMortAncien = changed.Behaviour.estMort;
        //4.
        if (estMortNouveau)
        {
            changed.Behaviour.Mort();
        }
        else if (!estMortNouveau && estMortAncien)
        {
            changed.Behaviour.Resurrection();
        }
    }

    /* Fonction appelée à la mort du personnage.
     * 1. Désactivation du joueur et de son hitboxroot qui sert à la détection de collision + faire tomber son chapeau
     * 2. Appelle de la fonction ActivationCharacterController(false) dans le scriptgestionnaireMouvementPersonnage
     * pour désactiver le CharacterConroller.
     * 3. Instanciation d'un système de particules (particulesMort_Prefab) à la place du joueur. On modifie
     * la couleur du matériel des particules en lui donnant la couleur du joueur qui meurt. Les particules
     * sont détruites après un délai de 3 secondes.
     */
    private void Mort()
    {
        //1.
        modelJoueur.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false;

        GameObject chapeauActif = joueurReseau.chapeau[joueurReseau.indexJoueur];
        chapeauActif.SetActive(false);

        Vector3 positionChapeau = chapeauActif.transform.position;
        Quaternion oriantationChapeau = chapeauActif.transform.rotation;
        // commande exécutée juste sur le serveur
        Runner.Spawn(prefabChapeau, positionChapeau, oriantationChapeau, Object.InputAuthority, (runner, chapeau) =>
        {
            chapeau.GetComponent<Chapeaux>().ApparitionChapeau(Object.InputAuthority, networkObject, joueurReseau.indexJoueur);
        });

        /*
        GameObject chapeauARamasser = Instantiate(chapeauActif);

        chapeauARamasser.transform.position = chapeauActif.transform.position;
        chapeauARamasser.transform.parent = null;
        chapeauARamasser.SetActive(true);
        chapeauARamasser.GetComponent<Rigidbody>().isKinematic = false;
        chapeauARamasser.GetComponent<Rigidbody>().useGravity = true;
        chapeauARamasser.GetComponent<Collider>().enabled = true;
        chapeauARamasser.GetComponent<Chapeaux>().enabled = true;
        chapeauARamasser.layer = chapeauActif.layer;
        */

        //2.
        gestionnaireMouvementPersonnage.ActivationCharacterController(false);
        //3.
        GameObject nouvelleParticule = Instantiate(particulesMort_Prefab, transform.position, Quaternion.identity);
        particulesMateriel.color = joueurReseau.maCouleur;
        Destroy(nouvelleParticule, 3);
    }

    /* Fonction appelée après la mort du personnage, lorsque la variable estMort est remise à false
     * 1. On change la couleur de l'image servant à indiquer au joueur qu'il est touché. L'important dans
     *    cette commande est qu'on met la valeur alpha à 0 (complètement transparente) pour la faire disparaître.
     *    Cette commande est effectuée seulement sur le client qui contrôle le joueur 
     * 2. On active le hitboxroot pour réactiver la détection de collisions
     * 3. Appelle de la fonction ActivationCharacterController(true) dans le scriptgestionnaireMouvementPersonnage
     *    pour activer le CharacterConroller.
     * 4. Appel de la coroutine (JoueurVisible) qui réactivera le joueur
     */
    private void Resurrection()
    {
        joueurReseau.ActiverChapeau();
        //1.
        if (Object.HasInputAuthority)
            uiImageTouche.color = new Color(0, 0, 0, 0);
        //2.
        hitboxRoot.HitboxRootActive = true;
        //3.
        gestionnaireMouvementPersonnage.ActivationCharacterController(true);
        //4.
        StartCoroutine(JoueurVisible());
    }

    /* Coroutine qui réactive le joueur après un délai de 0.1 seconde */
    IEnumerator JoueurVisible()
    {
        yield return new WaitForSeconds(0.1f);
        modelJoueur.gameObject.SetActive(true);
    }

    /* Fonction publique appelée par le script GestionnaireMouvementPersonnage
     * Réinitialise les points de vie
     * Change l'état (la variable) estMort pour false
     */
    public void Respawn()
    {
        ptsVie = ptsVieDepart;
        estMort = false;
    }
}