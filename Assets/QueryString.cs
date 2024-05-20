using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
public class QueryString : MonoBehaviour
{
           [DllImport("__Internal")]
    private static extern string getQueryString();
    // Start is called before the first frame update
    void Start()
    {
      //Debug.Log(getQueryString());
      Globals.urlQuery = getQueryString();
      Debug.Log(Globals.urlQuery); 
      if (Globals.urlQuey.contains("test")){
        Globals.testflag = true;
      }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
