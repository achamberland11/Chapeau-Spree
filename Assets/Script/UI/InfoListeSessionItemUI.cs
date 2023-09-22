using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System; // pour utiliser Action

public class InfoListeSessionItemUI : MonoBehaviour
{
    public TextMeshProUGUI txt_NomSession;
    public TextMeshProUGUI txt_NombreJoueurs;
    public Button btn_Joindre;

    SessionInfo sessionInfos; //classe fusion

    //evenements
    public event Action<SessionInfo> OnRejoindreSession;

    public void EnregistreInfos(SessionInfo sessionInfos)
    {
        this.sessionInfos = sessionInfos;
        txt_NomSession.text = $"{sessionInfos.Name}";
        txt_NombreJoueurs.text = $"{sessionInfos.PlayerCount.ToString()}/{sessionInfos.MaxPlayers.ToString()}";

        bool boutonJoindreActif = true;

        if (sessionInfos.PlayerCount >= sessionInfos.MaxPlayers)
            boutonJoindreActif = false;

        btn_Joindre.gameObject.SetActive(boutonJoindreActif);
    }

    public void OnClick()
    {
        //Invoquer l'événement OnRejoindreSession
        OnRejoindreSession?.Invoke(sessionInfos);
    }

}
