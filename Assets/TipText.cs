using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipText : MonoBehaviour
{

    public List<string> messages;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Text>().text = messages[Random.Range(0, messages.Count)];
        
    }

    // Update is called once per frame
    void Update()
    {
        customText();
    }

    private void customText() {
        if (GetComponent<Text>().text.Equals("Now in Swedish!"))
        {
            GameObject.FindGameObjectWithTag("tipText").GetComponentInChildren<Text>().text = "Spela";
        }
        else if (GetComponent<Text>().text.Equals("Now in Bird language!"))
        {
            GameObject.FindGameObjectWithTag("tipText").GetComponentInChildren<Text>().text = "sksksksks";
        }
    }
}
