using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/*
 * Script qui gère la rotation de la caméra FPS (hors simulation)
 * La rotation de la caméra (gauche/droite et haut/bas) se fera localement uniquement.Au niveau du réseau,
 * seule la caméra locale est active. Les caméras des autres joueurs (non nécessaire) seront désactivées.
 * Le personnage ne pivotera pas (rotate) comme tel. Seule sa direction (transform.forward) sera ajustée par le
 * Runner.
 * 
 * Variables :
 * - ancrageCamera :pour mémoriser la position de l'objet vide placé à la position que l'on veut donner à la caméra
 * - localCamera : contient la référence à la caméra du joueur actuel
 * - vueInput : Vector2 contenant les déplacements de la souris, horizontal et vertical. Variable définie dans la
 * fonction "SetInputVue" qui est appelée de l'extérieur, par le script "GestionnaireInputs"
 * cameraRotationX : rotation X a appliquer à la caméra
 * cameraRotationY : rotation y a appliquer à la caméra
 * - NetworkCharacterControllerPrototypeV2 : pour mémoriser le component NetworkCharacterControllerPrototypeV2 
 * du joueur. On s'en sert uniquement pour récupérer les variables vitesseVueHautBas et rotationSpeed qui 
 * sont stockées dans le component NetworkCharacterControllerPrototypeV2
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
     * Avant le Start(), on garde en mémoire la caméra du joueur courant  et le component 
     * networkCharacterControllerPrototypeV2 du joueur
     * 
     */
    void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeV2 = GetComponentInParent<NetworkCharacterControllerPrototypeV2>();
    }

    /*
     * On détache la caméra locale de son parent (le joueur). La caméra sera alors au premier niveau
     * de la hiérarchie.
     */
    void Start()
    {
        if (localCamera.enabled)
            localCamera.transform.parent = null;
    }

    /*
    * Positionnement et ajustement de la rotation de la caméra locale. On utilise le LateUpdate() qui
    * s'exécute après le Update. On s'assure ainsi que toutes les modifications du Update seront déjà appliquées.
    * 1. On s'assure de faire la mise à jour seulement sur le joueur local
    * 2. Ajustement de la position de la caméra au point d'ancrage (tête du perso)
    * 3. Calcul de la rotation X et Y. 
    * La rotation X (haut/bas) est associée au mouvement vertical de la souris. La valeur est limitée (clamp)
    * entre -90 et 90.
    * La rotation Y (gauche/droite) est associée au mouvement horizontal de la souris
    * 4. Ajustement de la rotation X et Y de la caméra
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
     * Fonction publique appelée de l'extérieur, par le script GestionnaireInput. Permet de recevoir la valeur
     * de rotation de la souris fournie par le Update (hors simulation) pour l'ajustement de la rotation de la caméra
     */
    public void SetInputVue(Vector2 vueInputVecteur)
    {
        vueInput = vueInputVecteur;
    }
}