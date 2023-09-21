using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GestionnaireMenuAccueil : MonoBehaviour
{
    public TMP_InputField inputField;

    void Start()
    {
        if (PlayerPrefs.HasKey("NomDuJoueur"))
            inputField.text = PlayerPrefs.GetString("NomDuJoueur");
    }

    public void BtnJoindrePartie()
    {
        PlayerPrefs.SetString("NomDuJoueur", inputField.text);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Niveau");
    }

    
}
