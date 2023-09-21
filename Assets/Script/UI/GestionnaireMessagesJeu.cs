using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GestionnaireMessagesJeu : MonoBehaviour
{
    public TextMeshProUGUI[] textMeshProUGUIs;

    Queue messagesQueue = new Queue();

    void Start()
    {
        
    }

    public void ReceptionMessage(string messageRecu)
    {
        Debug.Log($"Message reçu : {messageRecu}");
        Debug.Log(transform.root.name);
        
        messagesQueue.Enqueue(messageRecu);

        if (messagesQueue.Count > 3)
            messagesQueue.Dequeue();

        int indexQueue = 0;
        foreach(string messageQueue in messagesQueue)
        {
            textMeshProUGUIs[indexQueue].text = messageQueue;
            indexQueue++;
        }

    }

    
}
