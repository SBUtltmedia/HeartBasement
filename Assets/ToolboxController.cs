using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolboxController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] MonoBehaviour toolboxToggle;
    void Start()
    {
        Debug.Log(toolboxToggle);
        toolboxToggle = toolboxToggle.Instance;
        Debug.Log(toolboxToggle);
        toolboxToggle.Hide();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
