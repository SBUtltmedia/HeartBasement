using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sortable : MonoBehaviour
{
    [SerializeField] float m_baseline = 0;    
    [SerializeField, Tooltip("If true, the baseline will be in world position, instead of local to the object. So y position of the sortable is ignored")] bool m_fixed = false;
    [SerializeField, Tooltip("If true, renderers are cached on Start for efficiency (rather than retrieved on update)")] bool m_cacheRenderers = true;
    
    public float Baseline {get=>m_baseline; set=>m_baseline=value;}
    public int SortOrder {get=>-Mathf.RoundToInt(((m_fixed?0.0f:transform.position.y) + Baseline)*10.0f);}
    public bool Fixed {get=>m_fixed; set=>m_fixed=value;}

    Renderer[] m_renderers = null;
	int m_sortOrderCached = int.MinValue;

    public void EditorRefresh()
    {
        Start();
        LateUpdate();
    }

    // Start is called before the first frame update
    void Start()
    {
        
        if ( m_cacheRenderers )
        {
            m_renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in m_renderers)
                renderer.sortingLayerName = "Default";
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        int sortOrder = SortOrder;

		if ( m_cacheRenderers && m_sortOrderCached == sortOrder ) // don't touch if nothing changed
			return;	

        if ( m_cacheRenderers == false )
            m_renderers = GetComponentsInChildren<Renderer>();
			
        foreach (Renderer renderer in m_renderers)
        {
            if ( m_cacheRenderers == false )
    			renderer.sortingLayerName = "Default";
			renderer.sortingOrder = sortOrder;
        }
		m_sortOrderCached = sortOrder;
    }
}
