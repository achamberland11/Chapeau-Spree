using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class GestionnaireListeSessions : MonoBehaviour
{
    public TextMeshProUGUI txtEtat;
    public GameObject ListeItemSessionPrefab;
    public VerticalLayoutGroup verticalLayoutGroup;


    private void Awake()
    {
        EffaceListe();
    }
    public void EffaceListe()
    {
        foreach(Transform elementListe in verticalLayoutGroup.transform)
        {
            Destroy(elementListe.gameObject);
        }

        txtEtat.gameObject.SetActive(false);
    }

    public void AjouteListe(SessionInfo sessionInfo)
    {
        GameObject nouvelItemListe = Instantiate(ListeItemSessionPrefab, verticalLayoutGroup.transform);
        InfoListeSessionItemUI nouveauInfoListeSessionItemUI = nouvelItemListe.GetComponent<InfoListeSessionItemUI>();

        nouveauInfoListeSessionItemUI.EnregistreInfos(sessionInfo);

        nouveauInfoListeSessionItemUI.OnRejoindreSession += NouveauInfoListeSessionItemUI_OnRejoindreSession;
    }

    private void NouveauInfoListeSessionItemUI_OnRejoindreSession(SessionInfo sessionInfo)
    {
        GestionnaireReseau gestionnaireReseau = FindFirstObjectByType<GestionnaireReseau>();

        gestionnaireReseau.RejoindrePartie(sessionInfo);

        GestionnaireMenuAccueil gestionnaireMenuAccueil = FindFirstObjectByType<GestionnaireMenuAccueil>();
        gestionnaireMenuAccueil.RejoindreServeur();
    }

    public void AucuneSessionTrouvee()
    {
        EffaceListe();
        txtEtat.text = "Aucune session de jeu trouvée";
        txtEtat.gameObject.SetActive(true);
    }

    public void ChercheDesSessions()
    {
        EffaceListe();
        txtEtat.text = "Recherche de partie en cours...";
        txtEtat.gameObject.SetActive(true);
    }
}
