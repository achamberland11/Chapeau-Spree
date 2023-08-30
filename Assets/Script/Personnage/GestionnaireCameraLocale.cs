using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/*
 * Script qui g�re la rotation de la cam�ra FPS (hors simulation)
 * La rotation de la cam�ra (gauche/droite et haut/bas) se fera localement uniquement.Au niveau du r�seau,
 * seule la cam�ra locale est active. Les cam�ras des autres joueurs (non n�cessaire) seront d�sactiv�es.
 * Le personnage ne pivotera pas (rotate) comme tel. Seule sa direction (transform.forward) sera ajust�e par le
 * Runner.
 * 
 * Variables :
 * - ancrageCamera :pour m�moriser la position de l'objet vide plac� � la position que l'on veut donner � la cam�ra
 * - localCamera : contient la r�f�rence � la cam�ra du joueur actuel
 * - vueInput : Vector2 contenant les d�placements de la souris, horizontal et vertical. Variable d�finie dans la
 * fonction "SetInputVue" qui est appel�e de l'ext�rieur, par le script "GestionnaireInputs"
 * cameraRotationX : rotation X a appliquer � la cam�ra
 * cameraRotationY : rotation y a appliquer � la cam�ra
 * - NetworkCharacterControllerPrototypeV2 : pour m�moriser le component NetworkCharacterControllerPrototypeV2 
 * du joueur. On s'en sert uniquement pour r�cup�rer les variables vitesseVueHautBas et rotationSpeed qui 
 * sont stock�es dans le component NetworkCharacterControllerPrototypeV2
 */
public class GestionnaireCameraLocale : MonoBehaviour
{
    public Transform ancrageCamera;
    Camera localCamera;

    Vector2 vueInput;

    float cameraRotationX = 0f;
    float cameraRotationY = 0f;

    NetworkCharacterControllerPrototypeV2 networkCharacterControllerPrototypeV2;

    /*
     * Avant le Start(), on garde en m�moire la cam�ra du joueur courant  et le component 
     * networkCharacterControllerPrototypeV2 du joueur
     * 
     */
    void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeV2 = GetComponentInParent<NetworkCharacterControllerPrototypeV2>();
    }

    /*
     * On d�tache la cam�ra locale de son parent (le joueur). La cam�ra sera alors au premier niveau
     * de la hi�rarchie.
     */
    void Start()
    {
        if (localCamera.enabled)
            localCamera.transform.parent = null;
    }

    /*
    * Positionnement et ajustement de la rotation de la cam�ra locale. On utilise le LateUpdate() qui
    * s'ex�cute apr�s le Update. On s'assure ainsi que toutes les modifications du Update seront d�j� appliqu�es.
    * 1. On s'assure de faire la mise � jour seulement sur le joueur local
    * 2. Ajustement de la position de la cam�ra au point d'ancrage (t�te du perso)
    * 3. Calcul de la rotation X et Y. 
    * La rotation X (haut/bas) est associ�e au mouvement vertical de la souris. La valeur est limit�e (clamp)
    * entre -90 et 90.
    * La rotation Y (gauche/droite) est associ�e au mouvement horizontal de la souris
    * 4. Ajustement de la rotation X et Y de la cam�ra
    */
    void LateUpdate()
    {
        //1.
        if (ancrageCamera == null) return;
        if (!localCamera.enabled) return;
        //2.
        localCamera.transform.position = ancrageCamera.position;
        //3.
        cameraRotationX -= vueInput.y * Time.deltaTime * networkCharacterControllerPrototypeV2.vitesseVueHautBas;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);

        cameraRotationY += vueInput.x * Time.deltaTime * networkCharacterControllerPrototypeV2.rotationSpeed;
        //4.
        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
    }

    /*
     * Fonction publique appel�e de l'ext�rieur, par le script GestionnaireInput. Permet de recevoir la valeur
     * de rotation de la souris fournie par le Update (hors simulation) pour l'ajustement de la rotation de la cam�ra
     */
    public void SetInputVue(Vector2 vueInputVecteur)
    {
        vueInput = vueInputVecteur;
    }
}