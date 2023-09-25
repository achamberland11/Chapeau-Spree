using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

/* Script qui g�re les dommages (points de vie) quand un joueur est touch�
 * Variables :
 * 
 * ##### Pour la gestion des points de vie ################################################
 * - ptsVie : variable Networked de type byte (moins lourd qu'un int) pour les points de vie du joueur.
 *            Appel de la fonction OnPtsVieChange d�s que cette variable est modifi�e par le serveur.
 * - estMort : variable bool pour savoir si le joueur est mort ou pas. Appel de la fonction OnChangeEtat
 *             d�s que cette variable est modifi�e par le serveur.
 * - estInitialise : pour savoir si le joueur est initialis�.
 * - ptsVieDepart : le nombre de points de vie au commencement ou apr�s un respawn 
 * 
 * ##### Pour les effets de changement de couleur quand le perso est touch� ###############
 * - uiCouleurTouche:la couleur de l'image quand le perso est touch�
 * - uiImageTouche : l'image qui s'affiche quand le perso est touch�
 * - persoRenderer : r�f�rence au meshrenderer du pero. Servira � changer la couleur du mat�riel
 * - couleurNormalPerso : la couleur normal du perso
 * 
 * ##### Pour g�rer la mort du perso ###############
 * - modelJoueur : r�f�rence au gameObject avec la partie visuelle du perso
 * - particulesMort_Prefab : r�f�rence au Prefab des particules de mort � instancier � la mort du perso
 * - particulesMateriel : r�f�rence au mat�riel utilis� par les particules de morts
 * - hitboxRoot : r�f�rence au component photon HitBoxRoot servant � la d�tection de collision
 * - gestionnaireMouvementPersonnage : r�f�rence au script gestionnaireMouvementPersonnage sur le perso
 * - joueurReseau : r�f�rence au script joueurReseau sur le perso
 */


public class GestionnairePointsDeVie : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnPtsVieChange))]
    byte ptsVie { get; set; } //(byte : valeur possible entre 0 et 255, aucune valeur n�gative)

    [Networked(OnChanged = nameof(OnChangeEtat))]
    public bool estMort { get; set; }

    bool estInitialise = false;
    const byte ptsVieDepart = 5;

    public Color uiCouleurTouche; //� d�finir dans l'inspecteur
    public Image uiImageTouche; //� d�finir dans l'inspecteur
    public MeshRenderer persoRenderer; //� d�finir dans l'inspecteur
    Color couleurNormalPerso;


    public GameObject modelJoueur; //� d�finir dans l'inspecteur
    public GameObject particulesMort_Prefab; //� d�finir dans l'inspecteur
    public Material particulesMateriel; //� d�finir dans l'inspecteur
    HitboxRoot hitboxRoot;

    GestionnaireMouvementPersonnage gestionnaireMouvementPersonnage;
    JoueurReseau joueurReseau;
    NetworkObject networkObject;

    public GameObject prefabChapeau;


    /*
     * On garde en m�moire la r�f�rence au component HitBoxRoot ainsi que les r�f�rences � deux
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
      * Initialisation des variables � l'apparition du personnage. On garde aussi en m�moire la couleur
      * du personnage.
      */
    void Start()
    {
        ptsVie = ptsVieDepart;
        estMort = false;
        estInitialise = true;
        couleurNormalPerso = persoRenderer.material.color;
    }

    /* Fonction publique ex�cut�e uniquement par le serveur dans le script GestionnairesArmes du joueur qui
     * a tir�.
     * 1. On quitte la fonction imm�diatement si le joueur touch� est d�j� mort
     * 2. Soustraction d'un point de vie
     * 3. Si les points de vie sont � 0 (ou moins), la variable estMort est mise � true et on appelle
     * la coroutine RessurectionServeur_CO qui g�rera un �ventuel respawn du joueur
     * Important : souvenez-vous que les variables ptsVie et estMort sont de type [Networked] et qu'une 
     * fonction sera automatiquement appel�e lorsque leur valeur change.
    */
    public void PersoEstTouche(JoueurReseau dommageFaitParQui, byte dommage)
    {
        //1.
        if (estMort)
            return;

        // Pour �viter que la variable ptsVie (de type byte) descende en bas de 0. Rappelons que la valeur
        // Rappelons qu'un byte se situe entre 0 et 255.
        if (dommage > ptsVie)
            dommage = ptsVie;

        //2.
        ptsVie -= dommage;
        Debug.Log($"{Time.time} {transform.name} est touch�. Il lui reste {ptsVie} points de vie");

        //3.
        if (ptsVie <= 0)
        {
            Debug.Log($"{Time.time} {transform.name} est mort");
            StartCoroutine(RessurectionServeur_CO());
            estMort = true;

            /*Mise � jour du pointage du joueur qui a caus� la mort en appelant la fonction 
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

    /* Fonction appel�e lorqu'on d�tecte une diminution de la variable ptsVie. On sort de la fonction
     * si la variable estInitialise = false (donc avant le start).
     * Appel de la coroutine EffetTouche_CO() qui g�rera les effets visuels
     */
    private void ReductionPtsVie()
    {
        if (!estInitialise)
            return;

        StartCoroutine(EffetTouche_CO());
    }

    /* Coroutine qui g�re les effets visuels lorsqu'un joueur est touch�.
     * 1. Changement de la couleur du joueur pour blanc
     * 2. Changement de la couleur de l'image servant � indiquer au joueur qu'il est touch�.
     *    Cette commande est effectu�e seulement sur le client qui contr�le le joueur touch�
     * 3. Apr�s un d�lai de 0.2 secondes, on remet la couleur normale au joueur touch�
     * 4. On change la couleur de l'image servant � indiquer au joueur qu'il est touch�. L'important dans
     *    cette commande est qu'on met la valeur alpha � 0 (compl�tement transparente) pour la faire dispara�tre.
     *    Cette commande est effectu�e seulement sur le client qui contr�le le joueur touch� et que le joueur
     *    touch� n'est pas mort.
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

    /* Fonction statique appel�e automatiquement lorsque que la variable [Networked] ptsVie est modifi�e
     * IMPORTANT : les fonctions statiques ne peuvent pas modifier des variables ou appeler des fonctions
     * instanci�es (non static)
     * Param�tre Changed<GestionnairePointsDeVie> changed : propre a Fusion pour permettre de g�rer les
     * valeurs actuelles ou anciennes des variables.
     
     * 1.M�morisation de la valeur actuelle de la variable ptsVie dans une variable locale
     * Notez bien comment on r�ussit � r�cup�rer la valeur de la variable non static ptsVies
     * 2.Commande qui permet de charger les anciennes valeurs des variables [Networked]
     * 3.M�morisation de l'ancienne valeur de la variable ptsVie.
     * 4.Appel de la fonction ReductionPtsVie seulement si on d�tecte une diminution de la variable
     *   ptsVie. Cela permet de ne pas appeler la fonction lorsqu'on initialise la variable ptsDeVie
     *   au d�part ou apr�s un respawn.
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

    /* Fonction statique appel�e automatiquement lorsque que la variable [Networked] estMort est modifi�e
     * IMPORTANT : les fonctions statiques ne peuvent pas modifier des variables ou appeler des fonctions
     * instanci�es (non static)
     * Param�tre Changed<GestionnairePointsDeVie> changed : propre a Fusion pour permettre de g�rer les
     * valeurs actuelles ou anciennes des variables.
     
     * 1.M�morisation de la valeur actuelle de la variable estMort dans une variable locale
     * Notez bien comment on r�ussit � r�cup�rer la valeur de la variable non static estMort
     * 2.Commande qui permet de charger les anciennes valeurs des variables [Networked]
     * 3.M�morisation de l'ancienne valeur de la variable estMort.
     * 4.Appel de la fonction Mort() seulement quand la valeur actuelle de la variable estMort est true
     *   Appel de la fonction Ressurection() quand la valeur actuelle de la variable estMort est false
     *   et que l'ancienne valeur de la variable estMort est true. Donc, quand le joueur �tait mort et qu'on
     *   change la variable estMort pour la mettre � false.
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

    /* Fonction appel�e � la mort du personnage.
     * 1. D�sactivation du joueur et de son hitboxroot qui sert � la d�tection de collision + faire tomber son chapeau
     * 2. Appelle de la fonction ActivationCharacterController(false) dans le scriptgestionnaireMouvementPersonnage
     * pour d�sactiver le CharacterConroller.
     * 3. Instanciation d'un syst�me de particules (particulesMort_Prefab) � la place du joueur. On modifie
     * la couleur du mat�riel des particules en lui donnant la couleur du joueur qui meurt. Les particules
     * sont d�truites apr�s un d�lai de 3 secondes.
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
        // commande ex�cut�e juste sur le serveur
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

    /* Fonction appel�e apr�s la mort du personnage, lorsque la variable estMort est remise � false
     * 1. On change la couleur de l'image servant � indiquer au joueur qu'il est touch�. L'important dans
     *    cette commande est qu'on met la valeur alpha � 0 (compl�tement transparente) pour la faire dispara�tre.
     *    Cette commande est effectu�e seulement sur le client qui contr�le le joueur 
     * 2. On active le hitboxroot pour r�activer la d�tection de collisions
     * 3. Appelle de la fonction ActivationCharacterController(true) dans le scriptgestionnaireMouvementPersonnage
     *    pour activer le CharacterConroller.
     * 4. Appel de la coroutine (JoueurVisible) qui r�activera le joueur
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

    /* Coroutine qui r�active le joueur apr�s un d�lai de 0.1 seconde */
    IEnumerator JoueurVisible()
    {
        yield return new WaitForSeconds(0.1f);
        modelJoueur.gameObject.SetActive(true);
    }

    /* Fonction publique appel�e par le script GestionnaireMouvementPersonnage
     * R�initialise les points de vie
     * Change l'�tat (la variable) estMort pour false
     */
    public void Respawn()
    {
        ptsVie = ptsVieDepart;
        estMort = false;
    }
}