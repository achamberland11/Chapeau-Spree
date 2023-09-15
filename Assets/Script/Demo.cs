using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    int pointage;
    GameObject chapeauBase;
    List<GameObject> chapeauxObtenus = new List<GameObject>();

    void Ajout()
    {
        if(pointage == 0)
        {
            GameObject nvChapeau = Instantiate(gameObject);
            nvChapeau.transform.position = new Vector3(chapeauBase.transform.position.x, chapeauBase.transform.position.y + chapeauBase.GetComponent<MeshRenderer>().bounds.size.y, chapeauBase.transform.position.z);
        }
        else
        {
            GameObject nvChapeau = Instantiate(gameObject);
            nvChapeau.transform.position = new Vector3
                (chapeauxObtenus[pointage].transform.position.x, 
                chapeauxObtenus[pointage].transform.position.y + chapeauxObtenus[pointage].GetComponent<MeshRenderer>().bounds.size.y, 
                chapeauxObtenus[pointage].transform.position.z);
            chapeauxObtenus.Add(nvChapeau);
        }
    }
}
