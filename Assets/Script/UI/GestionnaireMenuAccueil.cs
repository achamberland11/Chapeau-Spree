using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class GestionnaireMenuAccueil : MonoBehaviour
{
    [Header("Pannels")]
    public GameObject panelNomDuJoueur;
    public GameObject panelListeSessions;
    public GameObject panelCreationSession;
    public GameObject panelEtat;

    [Header("InfosJoueurs")]
    public TMP_InputField inputField_NomDuJoueur;

    [Header("NouvelleSession")]
    public TMP_InputField inputField_NomSession;

    void Start()
    {
        if (PlayerPrefs.HasKey("NomDuJoueur"))
            inputField_NomDuJoueur.text = PlayerPrefs.GetString("NomDuJoueur");
    }

    void CacheTousLesPanels()
    {
        panelNomDuJoueur.SetActive(false);
        panelListeSessions.SetActive(false);
        panelCreationSession.SetActive(false);
        panelEtat.SetActive(false);
    }

    public void BtnTrouveParties()
    {
        PlayerPrefs.SetString("NomDuJoueur", inputField_NomDuJoueur.text);
        PlayerPrefs.Save();


        GestionnaireReseau gestionnaireReseau = FindFirstObjectByType<GestionnaireReseau>();
        gestionnaireReseau.RejoindreLeLobby();

        CacheTousLesPanels();
        panelListeSessions.SetActive(true);
        FindFirstObjectByType<GestionnaireListeSessions>(FindObjectsInactive.Include).ChercheDesSessions();

    }

    public void BtnNouvellePartie()
    {
        CacheTousLesPanels();
        panelCreationSession.SetActive(true);
    }

    public void BtnCreationNouvelleSession()
    {
        GestionnaireReseau gestionnaireReseau = FindFirstObjectByType<GestionnaireReseau>();

        gestionnaireReseau.CreationPartie(inputField_NomSession.text, "Niveau");

        CacheTousLesPanels();
        panelEtat.SetActive(true);
    }

    public void RejoindreServeur()
    {
        CacheTousLesPanels();
        panelEtat.SetActive(true);
    }
}
