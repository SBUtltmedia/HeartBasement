using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;

namespace PowerTools.Quest
{

[SelectionBase]
public partial class RoomComponent : MonoBehaviour 
{

	[SerializeField] Room m_data = new Room();

	[SerializeField] [HideInInspector] public string m_debugStartFunction = null;

	// this is stored mainly for editor
	[SerializeField] [HideInInspector] List<HotspotComponent> m_hotspotComponents = new List<HotspotComponent>();
	[SerializeField] [HideInInspector] List<PropComponent> m_propComponents = new List<PropComponent>();
	[SerializeField] [HideInInspector] List<RegionComponent> m_regionComponents = new List<RegionComponent>();
	[SerializeField] [HideInInspector] List<WalkableComponent> m_walkableAreas = new List<WalkableComponent>();
	[SerializeField, ReadOnly, NonReorderable] List<AnimationClip> m_animations =  new List<AnimationClip>();
	[SerializeField, ReadOnly, NonReorderable] List<Sprite> m_sprites = new List<Sprite>();	

	// Used by editor when importing art, checks if variants should use their own art... very specific so should be turned temporarily when needed.
	[SerializeField, HideInInspector] bool m_variantShouldOverrideArt = false; 
	
	Pathfinder m_pathfinder = new Pathfinder();

	public Room GetData() { return m_data; } 
	public void SetData(Room data) { m_data = data; }
	public GameObject GetPrefab()  { return ( Application.isPlaying && m_data.GetPrefab() != null ) ? m_data.GetPrefab() : gameObject; } // Used by editor
	public List<HotspotComponent> GetHotspotComponents() { return m_hotspotComponents; }
	public List<PropComponent> GetPropComponents() { return m_propComponents; }
	public List<RegionComponent> GetRegionComponents() { return m_regionComponents; }
	public List<WalkableComponent> GetWalkableAreas() { return m_walkableAreas; }

	public AnimationClip GetAnimation(string animName) { return QuestUtils.FindByName(m_animations, animName); }
	public List<AnimationClip> GetAnimations() { return m_animations; }
	
	
	public Sprite GetSprite(string animName) { return PowerQuest.FindSpriteInList(m_sprites, animName); }
	public List<Sprite> GetSprites() { return m_sprites; }

	public string DebugStartFunction => m_debugStartFunction;
		
	public bool EditorVariantShouldOverrideArt => m_variantShouldOverrideArt;

	// Called once room and everything in it has been created and PowerQuest has initialised references. After Start, Before OnEnterRoom.
	public void OnLoadComplete()
	{
		m_hotspotComponents.ForEach(item=>item.OnLoadComplete());
		m_propComponents.ForEach(item=>item.OnLoadComplete());
		m_regionComponents.ForEach(item=>item.OnLoadComplete());
				
		if ( PowerQuest.Get.GetPixelCamEnabled() )
		{
			// If pixel cam's enabled, set props with parallax to "sub-pixel layer"
			int layerHighRes = LayerMask.NameToLayer("HighRes");
			m_propComponents.ForEach(item=>
			{	
				if ( item.GetParallax() != 0.0f )
					item.gameObject.layer = layerHighRes;
			
			});
			
		}		

		ExOnLoadComplete();
	}


	public void EditorUpdateChildComponents()
	{
		m_hotspotComponents.Clear();
		m_hotspotComponents.AddRange(GetComponentsInChildren<HotspotComponent>(true));

		m_propComponents.Clear();
		m_propComponents.AddRange(GetComponentsInChildren<PropComponent>(true));

		m_regionComponents.Clear();
		m_regionComponents.AddRange(GetComponentsInChildren<RegionComponent>(true));

		m_walkableAreas.Clear();
		m_walkableAreas.AddRange(GetComponentsInChildren<WalkableComponent>(true));
	}

	// returns true if walkable area was found
	public bool SetActiveWalkableArea(int id)
	{
		if ( m_walkableAreas.IsIndexValid(id) )
		{			
			m_pathfinder.SetMainPolygon( m_walkableAreas[id].PolygonCollider );
			
			// Find holes
			if ( m_walkableAreas.IsIndexValid(m_data.ActiveWalkableArea) )
			{
				PolygonCollider2D[] childColliders = m_walkableAreas[id].transform.GetComponentsInChildren<PolygonCollider2D>();
				foreach( PolygonCollider2D collider in childColliders )
				{
					if( collider.transform != m_walkableAreas[id].transform )
						m_pathfinder.AddObstacle(collider);
				}
			}
			return true;
		}
		return false;
	}

	public Pathfinder GetPathfinder() { return m_pathfinder; }
		
	/*
	void Awake()
	{		
		UpdateWalkableAreaList();
		m_pathfinder.SetMainPolygon( m_walkableAreas.IsIndexValid(m_data.ActiveWalkableArea) ? m_walkableAreas[m_data.ActiveWalkableArea].PolygonCollider : null );

		// Find holes
		if ( m_walkableAreas.IsIndexValid(m_data.ActiveWalkableArea) )
		{
			PolygonCollider2D[] childColliders = m_walkableAreas[0].transform.GetComponentsInChildren<PolygonCollider2D>();
			foreach( PolygonCollider2D collider in childColliders )
			{
				if( collider.transform != m_walkableAreas[0].transform )
					m_pathfinder.AddObstacle(collider);
			}
		}
	}
	*/
	
	void Awake()
	{ 	
		ExAwake();
	}
	void Start() 
	{
		ExStart();
	}	
	// Update is called once per frame
	void Update () 
	{
		ExUpdate();	
	}
	
	/*
	void UpdateWalkableAreaList()
	{
		m_walkableAreas.Clear();
		m_walkableAreas.AddRange( GetComponentsInChildren<WalkableComponent>(true) );
	}*/

	//
	// Partial methods for extentions	
	//
	partial void ExAwake();
	partial void ExStart();
	partial void ExUpdate();
	// Called once room and everything in it has been created and PowerQuest has initialised references. After Start, Before OnEnterRoom.
	partial void ExOnLoadComplete();

}

}
