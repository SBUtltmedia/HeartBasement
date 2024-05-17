using UnityEngine;
using System.Collections;
using PowerTools;

namespace PowerTools.Quest
{


public class QuestSinMotion : MonoBehaviour {
	
	
		
	[SerializeField]
	public Vector2 m_positionMag;
	[SerializeField]
	public Vector2 m_positionDelta;
	[SerializeField]
	public float m_rotationMag;
	[SerializeField]
	public float m_rotationDelta;
	[SerializeField]
	public Vector2 m_scaleMag;
	[SerializeField]
	public Vector2 m_scaleDelta;
	[SerializeField]
	public float m_timeStep = 0;
	[SerializeField]
	public float m_snap = 0;
	
	Vector2 m_originalPosition;
	Vector2 m_cachedPosition;
	float m_originalAngle;
	float m_cachedAngle;
	Vector2 m_originalScale;
	Vector2 m_cachedScale;
		
	float m_timer;
	float m_timer2 = 0;

	PropComponent m_propComponent = null;
	QuestCameraComponent m_cameraComponent = null;

	Vector2 GetPosition()
	{
		if ( m_propComponent != null )
		{
			return m_propComponent.GetData().Position;
		}
		else if ( m_cameraComponent != null )
		{
			m_cameraComponent.GetData().GetPositionOverride();
		}
		return transform.position;
	}
	void SetPosition(Vector2 position)
	{
		if ( m_propComponent != null )
			m_propComponent.GetData().Position = position;
		else if ( m_cameraComponent != null )
			m_cameraComponent.GetData().SetPositionOverride(position);
		else 
			transform.position = position.WithZ(transform.position.z);		
	}

	// Use this for initialization
	void Start () 
	{		
		m_timer = Random.Range(0.0f,60.0f);
		m_timer2 = Random.Range(0,m_timeStep);

		m_propComponent = GetComponent<PropComponent>();
		m_cameraComponent = GetComponent<QuestCameraComponent>();


		m_originalPosition = GetPosition();
		m_originalAngle = transform.eulerAngles.z;
		m_originalScale = Vector2.zero;//transform.localScale;
		m_cachedPosition = m_originalPosition;
		m_cachedAngle = m_originalAngle;	
		m_cachedScale = transform.localScale;	
				
	}	

	
	// Update is called once per frame
	void Update () 
	{
		m_timer += Time.deltaTime;
		m_timer2 -= Time.deltaTime;
		if ( m_timer2 > 0 )
			return;
		m_timer2 = m_timeStep;
		
		//
		// Position
		//
		m_originalPosition += GetPosition() - m_cachedPosition; 
		if ( m_positionMag.x > 0.00001f || m_positionMag.y > 0.00001f )
		{			
			SetPosition(PowerTools.Quest.Utils.Snap(
					m_originalPosition + new Vector2(
					(m_positionMag.x * Mathf.Sin(m_positionDelta.x * m_timer) ), 
					(m_positionMag.y * Mathf.Sin(m_positionDelta.y * m_timer) ) ), m_snap));
		}
		
		//
		// Angle
		//
		m_originalAngle += transform.eulerAngles.z - m_cachedAngle;
		if ( m_rotationMag > 0.00001f )
		{
			if ( m_rotationMag > 359.0f)
			{	
				m_originalAngle += Time.deltaTime * m_rotationDelta;
				transform.eulerAngles = new Vector3(0,0, m_originalAngle * (transform.localScale.x > 0 ? 1.0f : -1.0f) );
			}
			else
			{
				transform.eulerAngles = new Vector3(0,0, m_originalAngle +
					( m_rotationMag * Mathf.Sin(m_rotationDelta * m_timer) ) );
			}	
		}
		
		//
		// Scale
		//
		m_originalScale += (Vector2)transform.localScale - m_cachedScale;
		if ( m_scaleMag.x > 0.00001f ||  m_scaleMag.y > 0.00001f )
		{
			transform.localScale =
				(m_originalScale +  new Vector2(
				1.0f - m_scaleMag.x + ( m_scaleMag.x * Mathf.Sin(m_scaleDelta.x * m_timer) ), 
				1.0f - m_scaleMag.y + ( m_scaleMag.y * Mathf.Sin(m_scaleDelta.y * m_timer) ) )).WithZ(transform.localScale.z) ;
		}
		
		m_cachedPosition = GetPosition();
		m_cachedAngle = transform.eulerAngles.z;
		m_cachedScale = transform.localScale;
						
	}
}

}