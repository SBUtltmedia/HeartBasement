using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

using PowerTools.Quest;
using PowerScript;
using static GlobalScript;
public class QueryString : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string getQueryString();
    // Start is called before the first frame update
    void Start()
    {
      //Debug.Log(getQueryString());

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
