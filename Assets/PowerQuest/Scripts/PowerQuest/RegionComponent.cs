using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using PowerTools;

namespace PowerTools.Quest
{

//
// Region Data and functions. Persistant between scenes, as opposed to RegionComponent which lives on a GameObject in a scene.
//
[System.Serializable] 
public partial class Region : IRegion, IQuestScriptable, IQuestSaveCachable
{

	#region Region: Editor data
	//
	// Default values set in inspector
	//
	[Tooltip("Whether walking on region triggers events/tints characters, etc")]
	[FormerlySerializedAs("m_triggerEnabled")]
	[SerializeField] bool m_enabled = true;
	[Tooltip("Whether character can walk over region, if false they'll path around it")]
	[SerializeField] bool m_walkable = true;
	[Tooltip("Whether OnEnter and similar scripts affect the player only")]
	[SerializeField] bool m_playerOnly = false;
	[Tooltip("Colour to tint the player when in this area. Alpha controls the amount of tint.")]
	[SerializeField] Color m_tint = new Color(1,1,1,0);
	[Tooltip("Distance a character has to move into a region before tint is fully faded in")]
	[SerializeField] float m_fadeDistance = 0;
	[Tooltip("Amount to scale the player while in the region (at the top)")]
	[SerializeField] float m_scaleTop = 1;
	[Tooltip("Amount to scale the player while in the region (at the bottom)")]
	[SerializeField] float m_scaleBottom = 1;
	[ReadOnly][SerializeField] string m_scriptName = "RegionNew";
	

	#endregion
	#region Region: Vars: private

	//
	// Private variables
	//
	RegionComponent m_instance = null;
	
	BitArray m_characterOnRegionMask = new BitArray(64);
	BitArray m_characterOnRegionMaskOld = new BitArray(64);
	BitArray m_characterOnRegionMaskBGOld = new BitArray(64);

	#endregion
	#region Region: properties
	//
	//  Properties
	//
	public string ScriptName { get{ return m_scriptName;} }
	public MonoBehaviour Instance { get{ return m_instance; } }
	public Region Data {get {return this;} }
	public bool Enabled { get{ return m_enabled;} set{m_enabled = value; if ( m_instance != null && m_enabled ) m_instance.gameObject.SetActive(true); } }
	public bool Walkable 
	{ 
		get { return m_walkable; } 
		set 
		{
			m_walkable = value; 
			if ( m_instance )
			{
				m_instance.OnSetWalkable(m_walkable);
			}
		}
	}
	public bool PlayerOnly { get { return m_playerOnly; } set { m_playerOnly = value; } }
	public Color Tint { get{ return m_tint;} set{m_tint = value;} }
	public float FadeDistance { get{ return m_fadeDistance;} set{m_fadeDistance = value;} }
	public float ScaleTop { get{ return m_scaleTop;}  set{ m_scaleTop = value; }  }
	public float ScaleBottom { get{ return m_scaleBottom;} set{ m_scaleBottom = value;}  }

	public bool ContainsCharacter(ICharacter character = null) { return GetCharacterOnRegion(character); }

	public bool GetCharacterOnRegion(ICharacter character = null)
	{
		if ( m_instance == null || m_characterOnRegionMask == null ) 
			return false;  // If instance is null, don't used cached data, it won't be accurate

		// If null passed, return true if ANY character is in the region
		if ( character == null )
		{
			foreach( bool b in m_characterOnRegionMask )
			{
				if ( b )
					return true;
			}
		}		
		else if ( character.Data != null )
		{
			int id = PowerQuest.Get.GetCharacterId(character.Data);
			if ( id < 0 ) return false;
			return m_characterOnRegionMask.Get( id );
		}
		return false;
	}
	
	public bool ContainsPoint(Vector2 position)
	{ 
		if ( m_instance == null || m_instance.GetPolygonCollider() == null ) 
			return false;

		return m_instance.GetPolygonCollider().OverlapPoint(position);
	}


	public RegionComponent GetInstance() { return m_instance; }
	public void SetInstance(RegionComponent instance) 
	{ 
		m_instance = instance; 
		instance.SetData(this);
		instance.OnSetWalkable(m_walkable);
	}
	// Return room's script
	public QuestScript GetScript() { return (PowerQuest.Get.GetCurrentRoom() == null) ? null : PowerQuest.Get.GetCurrentRoom().GetScript(); } 

	//
	// Public Functions
	//
	#endregion
	#region Region: Functions: Public 

	public void EditorInitialise( string name )
	{
		m_scriptName = name;
	}
	public void EditorRename(string name)
	{
		m_scriptName = name;
	}

	public BitArray GetCharacterOnRegionMask() { return m_characterOnRegionMask; }
	public BitArray GetCharacterOnRegionMaskOld(bool background) { return background ? m_characterOnRegionMaskBGOld : m_characterOnRegionMaskOld; }

	#endregion
	#region Region Functions: Implementing IQuestScriptable

	// Doesn't use all functions
	public string GetScriptName() { return m_scriptName; }
	public string GetScriptClassName() { return PowerQuest.STR_REGION+m_scriptName; }
	public void HotLoadScript(System.Reflection.Assembly assembly) { /*No-op*/ }
	
	
	#endregion
	#region Region Functions: Implementing IQuestSaveCachable
	//
	// Implementing IQuestSaveCachable
	//	
	bool m_saveDirty = true;
	bool m_saveDirtyEver = false;
	public bool SaveDirty { get=>m_saveDirty; set { m_saveDirty=value; 
		if (value) 
		{
			//if ( m_saveDirtyEver == false ) Debug.Log($"Region {ScriptName} set dirty");
			m_saveDirtyEver=true; 
		} } }
	public bool SaveDirtyEver => m_saveDirtyEver;

	#endregion
	#region Region: Functions: Private 

	//
	// Internal Functions
	//


	// Handles setting up defaults incase items have been added or removed since last loading a save file
	[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}
	#endregion
}

//
// The component on the region in scene
//
public class RegionComponent : MonoBehaviour 
{
	public enum eTriggerResult { None,Enter,Exit,Stay };

	[SerializeField] Region m_data = new Region();

	PolygonCollider2D m_polygonCollider = null;

	float m_minColliderY = 0;
	float m_maxColliderY = 0;

	public Region GetData() { return m_data; }
	public void SetData(Region data) { m_data = data; }

	// Add or remove obstacle from polynav
	public void OnSetWalkable( bool walkable )
	{
		if ( m_polygonCollider == null )
			m_polygonCollider = GetComponent<PolygonCollider2D>();
		if ( walkable )
		{
			PowerQuest.Get.Pathfinder.RemoveObstacle(m_polygonCollider);
		}
		else 
		{							
			PowerQuest.Get.Pathfinder.AddObstacle(m_polygonCollider);
		}
	}

	public PolygonCollider2D GetPolygonCollider() { return m_polygonCollider; }

	// Updates whether character is active and in the region. Doesn't check if character is active/in room, so pass that in. Returns true if standing on region
	public bool UpdateCharactersOnRegion( int index, bool characterActiveInRoom, Vector2 characterPos )
	{
		//bool wasInside = m_characterOnRegionMask.Get(index);
		bool result = characterActiveInRoom && m_data.Enabled;
		if ( result )	
			result = ( m_polygonCollider != null && m_polygonCollider.OverlapPoint(characterPos) );				
		m_data.GetCharacterOnRegionMask().Set(index,result);
		return result;
	}

	public float GetScaleAt(Vector2 position)
	{
		if ( GetData().ScaleTop == 1 && GetData().ScaleBottom == 1 )
			return 1; // Scaling off
		return Mathf.Lerp( GetData().ScaleBottom, GetData().ScaleTop, Mathf.InverseLerp(m_minColliderY, m_maxColliderY, position.y ));
	}

	// Note: Only call if point is inside region
	public float GetDistanceIntoRegion(Vector2 point)
	{
		if ( m_polygonCollider == null )
			return 0;
		point -= (Vector2)transform.position;
		return RegionPolyUtil.CalcDistToEdge(m_polygonCollider.points, point);
	}
	
	// Note: Only call if point is inside region
	public float GetFadeRatio(Vector2 point)
	{
		if ( GetData().FadeDistance <= 0)
			return 1;
		if ( m_polygonCollider == null )
			return 1;
	
		return Mathf.Clamp01(GetDistanceIntoRegion(point)/GetData().FadeDistance);
	}

	// Updates and returns whether the character entered/exited/stayed in the region. this is used in non-blocking situations
	public eTriggerResult UpdateCharacterOnRegionState( int index, bool background )
	{
		eTriggerResult result = eTriggerResult.None;
		if ( index >= m_data.GetCharacterOnRegionMask().Count )
			return result; // In case of load game with different regions
		bool inside = m_data.GetCharacterOnRegionMask().Get(index);
		bool wasInside = m_data.GetCharacterOnRegionMaskOld(background).Get(index);
		

		if ( wasInside )
		{
			result = inside ? eTriggerResult.Stay : eTriggerResult.Exit;
		}
		else if ( inside )
		{
			result = eTriggerResult.Enter;
		}		 

		// update cached mask
		m_data.GetCharacterOnRegionMaskOld(background).Set(index, m_data.GetCharacterOnRegionMask().Get(index));
		return result;
	}
	

	public void OnRoomLoaded()
	{
		// There's no copy value kinda thing, so i'm doing this to make a copy efficiently
		m_data.GetCharacterOnRegionMaskOld(true).SetAll(true);
		m_data.GetCharacterOnRegionMaskOld(true).And( m_data.GetCharacterOnRegionMask() );
		m_data.GetCharacterOnRegionMaskOld(false).SetAll(true);
		m_data.GetCharacterOnRegionMaskOld(false).And( m_data.GetCharacterOnRegionMask() );
	}


	// Use this for initialization
	void Start() 
	{
		if ( m_polygonCollider == null )
			m_polygonCollider = GetComponent<PolygonCollider2D>();
		m_data.GetCharacterOnRegionMask().Length = PowerQuest.Get.GetCharacters_SaveFlagNotDirtied().Count; // kind of hacky way to get this info :/
		m_data.GetCharacterOnRegionMaskOld(true).Length = m_data.GetCharacterOnRegionMask().Length;
		m_data.GetCharacterOnRegionMaskOld(false).Length = m_data.GetCharacterOnRegionMask().Length;
		InitializeCollider();
	}

	// Sets up min and max points on the collider
	public void InitializeCollider()
	{ 
		if ( m_polygonCollider == null )
			m_polygonCollider = GetComponent<PolygonCollider2D>();		

		// Find min and max points of collider
		if ( m_polygonCollider != null )
		{
			m_minColliderY = float.MaxValue;
			m_maxColliderY = float.MinValue;
			System.Array.ForEach( m_polygonCollider.points, item=>
				{
					if ( item.y < m_minColliderY )
						m_minColliderY = item.y;
					if ( item.y > m_maxColliderY )
						m_maxColliderY = item.y;					
				} );
		}
	}

	// Called once room and everything in it has been created and PowerQuest has initialised references. After Start, Before OnEnterRoom.
	public void OnLoadComplete()
	{
		OnSetWalkable(GetData().Walkable);

	}
	/*
	// Update is called once per frame
	void Update () 
	{

	}
	*/
	
	// From https://stackoverflow.com/questions/19048122/how-to-get-the-nearest-point-outside-a-polygon-from-a-point-inside-a-polygon
	public class RegionPolyUtil
	{
		public static float CalcDistToEdge(Vector2[] poly, Vector2 point)
		{			
			float minDist = float.MaxValue;
			Vector2 pointA = poly[poly.Length-1];
			for ( int i = 0; i < poly.Length; ++i )
			{
				Vector2 pointB =  poly[i];
				float dist = (NearestPointOnLine(pointA,pointB,point)-point).sqrMagnitude;
				if ( dist < minDist )
					minDist = dist;
				pointA = pointB;
			}
			return Mathf.Sqrt(minDist);
		}

		// Gets the closed point that isn't inside the polygon... Note- doesn't work if points are too far
		public static Vector2 CalcClosestPointOnEdge(Vector2[] poly, Vector2 point)
		{
			//return GetClosestPointInSegment(ClosestSegment(poly, point), point);
			Vector2[] closestSeg = ClosestSegment(poly, point); 
			//return NewPointFromCollision(closestSeg[0], closestSeg[1], point);
			return NearestPointOnLine(closestSeg[0], closestSeg[1], point); // Get nearest point on the closest segment
		}
		
		static Vector2 NearestPointOnLine( Vector2 a, Vector2 b, Vector2 p )//float ax, float ay, float bx, float by, float px, float py)
		{
			// https://stackoverflow.com/questions/1459368/snap-point-to-a-line-java
			float apx = p.x - a.x;
			float apy = p.y - a.y;
			float abx = b.x - a.x;
			float aby = b.y - a.y;

			float ab2 = abx * abx + aby * aby;
			float ap_ab = apx * abx + apy * aby;
			float t = ap_ab / ab2;
			if (t < 0)
			{
				t = 0;
			}
			else if (t > 1)
			{
				t = 1;
			}
			return new Vector2((float)(a.x + (abx * t)), (float)(a.y + aby * t));
		}

		static Vector2[] ClosestSegment(Vector2[] points, Vector2 point)
		{

			Vector2[] returns = new Vector2[2];

			int index = ClosestPointIndex(points, point);

			returns[0] = points[index];

			Vector2[] neighbors = new Vector2[] {
				points[(index+1+points.Length)%points.Length],
				points[(index-1+points.Length)%points.Length]
			};

			float[] neighborAngles = new float[] {
				GetAngle(new Vector2[] {point, returns[0], neighbors[0]}),
				GetAngle(new Vector2[] {point, returns[0], neighbors[1]})
			};
			// The neighbor with the lower angle is the one to use
			if (neighborAngles[0] < neighborAngles[1])
			{
				returns[1] = neighbors[0];
			}
			else
			{
				returns[1] = neighbors[1];
			}

			return returns;

		}

		static float GetAngle(Vector2[] abc)
		{
			// https://stackoverflow.com/questions/1211212/how-to-calculate-an-angle-from-three-points
			// atan2(P2.y - P1.y, P2.x - P1.x) - atan2(P3.y - P1.y, P3.x - P1.x)        
			return Mathf.Atan2(abc[2].x - abc[0].y, abc[2].x - abc[0].x) - Mathf.Atan2(abc[1].y - abc[0].y, abc[1].x - abc[0].x);
		}

		static int ClosestPointIndex(Vector2[] points, Vector2 point)
		{
			int leastDistanceIndex = 0;
			float leastDistance = float.MaxValue;

			for (int i = 0; i < points.Length; i++)
			{
				float dist = (points[i]-point).sqrMagnitude;
				if (dist < leastDistance)
				{
					leastDistanceIndex = i;
					leastDistance = dist;
				}
			}

			return leastDistanceIndex;
		}


		static float GetThing(Vector2[] points, Vector2 point)
		{
			return 0;
		}
	}
}

}
