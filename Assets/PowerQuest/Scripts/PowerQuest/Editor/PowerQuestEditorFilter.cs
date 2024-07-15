using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using PowerTools.Quest;
using PowerTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Assertions;


namespace PowerTools.Quest
{

// The list filtering code is by Arcnor. Thanks Arcnor!
public partial class PowerQuestEditor
{
	#region Variables: Static definitions

	private static readonly Color FilterHighlightColor = Color.yellow;

	enum FilterState 
	{
		All,
		Highlighted,
		Unparented,
		Parented,
		Searched // never set, returns automatically if a SearchString is set
	}

	[Serializable]
	private class FilterContext 
	{
		FilterState _state = FilterState.All;

		public bool Show;

		public FilterState State { get{ return IsString.Empty(SearchString) ? _state : FilterState.Searched; } set { _state=value; } }
		/**
		 * Only valid when FilterState == FilterState.Parented
		 */
		public string ParentPath;
		
		/**
		 * Only valid when FilterState == FilterState.Parented
		 */
		public string SearchString;

		public FilterContext(bool showByDefault = true) {
			Show = showByDefault;
		}
	}	

	class PathsDropdown : AdvancedDropdown 
	{
		class PathItem : AdvancedDropdownItem 
		{
			public FilterState filterState { get; }
			[CanBeNull]
			public string filterParentPath { get; }

			public PathItem(string name, FilterState filterState, string filterParentPath = null) : base(name) 
			{
				this.filterState = filterState;
				this.filterParentPath = filterParentPath;
			}
		}

		readonly FilterContext _filterCtx;
		readonly Action _refreshFiltering;
		readonly bool _hasHighlighted;
		readonly bool _hasUnparented;

		readonly AdvancedDropdownItem _fakeRoot;

		public PathsDropdown(string gamePath, IEnumerable<MonoBehaviour> components, AdvancedDropdownState state, FilterContext filterCtx, Action refreshFiltering) : base(state) 
		{
			_filterCtx = filterCtx;
			_refreshFiltering = refreshFiltering;

			minimumSize = new Vector2(0, 200);
			_fakeRoot = new AdvancedDropdownItem("FakeRoot");

			foreach (var c in components) 
			{
				if (!_hasHighlighted && IsHighlighted(c)) 
				{
					_hasHighlighted = true;
				}

				var assetParentPath = GetAssetParentPath(gamePath, c);

				if (!string.IsNullOrEmpty(assetParentPath)) 
				{
					var parent = _fakeRoot;
					var parts = assetParentPath.Split('/');
					var currentPath = "";
					foreach (var part in parts) 
					{
						if (currentPath != "") 
						{
							currentPath += '/';
						}
						currentPath += part;

						var child = new PathItem(part, FilterState.Parented, currentPath);
						parent = GetOrAddNewChild(parent, child);
					}
				} 
				else 
				{
					_hasUnparented = true;
				}
			}
		}

		static AdvancedDropdownItem GetOrAddNewChild(AdvancedDropdownItem parent, PathItem newChild) 
		{
			foreach (var child in parent.children) 
			{
				if (child.name == newChild.name) 
					return child;				
			}

			parent.AddChild(newChild);
			return newChild;
		}

		protected override AdvancedDropdownItem BuildRoot() 
		{
			var root = new AdvancedDropdownItem("Filtering");

			root.AddChild(new PathItem("All", FilterState.All));

			if (_hasHighlighted) 
			{
				root.AddChild(new PathItem("Highlighted", FilterState.Highlighted));
			}

			if (_hasUnparented) 
			{
				root.AddChild(new PathItem("Unparented", FilterState.Unparented));
			}

			var childrenList = _fakeRoot.children.ToList();

			if (childrenList.Count > 0) 
			{
				root.AddSeparator();

				foreach (var child in childrenList) 
				{
					root.AddChild(child);
				}
			}

			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item) 
		{
			if (item is PathItem pathItem) 
			{
				_filterCtx.State = pathItem.filterState;
				_filterCtx.ParentPath = pathItem.filterParentPath;
			} else 
			{
				_filterCtx.State = FilterState.All;
				_filterCtx.ParentPath = null;
			}

			_refreshFiltering.Invoke();
		}
	}

	[Serializable]
	private class PrefabGroupListItem<T>
	{
		public string GroupName { get; set; }
		public List<T> Members { get; set; }

		public bool FoldedOut { get; set; } = false;
		public bool Grouped { get; set; } = true;
	}

	#endregion
	#region Variables: Serialized
	
	// Whether to show filtered list of prefabs, or the whole thing
	[SerializeField] FilterContext m_filterRooms = new FilterContext();
	[SerializeField] FilterContext m_filterCharacters = new FilterContext();
	[SerializeField] FilterContext m_filterInventory = new FilterContext();
	[SerializeField] FilterContext m_filterDialogTrees = new FilterContext();
	[SerializeField] FilterContext m_filterGuis = new FilterContext(false);
	
	#endregion
	#region Variables: Private
	
	//[CanBeNull] private string _lastDropdownName = null;

	#endregion
	#region Funcs

	static string GetAssetParentPath<T>(string gamePath, T component) where T : MonoBehaviour 
	{
		var assetPath = AssetDatabase.GetAssetPath(component);
		var assetDir = Path.GetDirectoryName(assetPath);

		if (string.IsNullOrEmpty(assetDir)) return "";

		var processedPath = assetDir.Substring(assetPath.IndexOf(gamePath, StringComparison.Ordinal) + gamePath.Length);
		#if UNITY_EDITOR_WIN
		var slashIdx = processedPath.IndexOf('\\');
		#else
		var slashIdx = processedPath.IndexOf('/');
		#endif
		
		processedPath = processedPath.Substring(slashIdx + 1);
		#if UNITY_EDITOR_WIN
		slashIdx = processedPath.LastIndexOf('\\');
		#else
		slashIdx = processedPath.LastIndexOf('/');
		#endif
		
		return slashIdx > 0
			? processedPath.Substring(0, slashIdx)
			: "";
	}

	void ApplyFilter<T>( List<T> prefabList, List<T> list, FilterContext filterCtx) where T : MonoBehaviour
	{
		list.Clear();
		if ( IsString.Valid(filterCtx.SearchString) ) {
			// Build list from any that match			
			list.AddRange(prefabList.FindAll(item=>item.name.LastIndexOf(filterCtx.SearchString, StringComparison.OrdinalIgnoreCase) >= 3)); // ideally would pass in num characters to ignore (based on item prefix)
		}
		else {
			switch (filterCtx.State) {
				case FilterState.All:
					list.AddRange(prefabList);
					break;

				case FilterState.Highlighted:
					list.AddRange(prefabList.FindAll(IsHighlighted));
					break;

				case FilterState.Unparented:
					list.AddRange(prefabList.FindAll(item => string.IsNullOrEmpty(GetAssetParentPath(m_gamePath, item))));
					break;

				case FilterState.Parented:
					list.AddRange(prefabList.FindAll(item => GetAssetParentPath(m_gamePath, item) == filterCtx.ParentPath));
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(filterCtx.State), filterCtx.State, null);
			}

			if (list.Count == 0) {
				list.AddRange(prefabList);
				filterCtx.State = FilterState.All;
			}
		}

	}

	static string GetFilterName(FilterContext filterCtx) => IsString.Valid(filterCtx.SearchString) ? $"Containing '{filterCtx.SearchString}'" : (filterCtx.State == FilterState.Parented ? filterCtx.ParentPath : filterCtx.State.ToString());

	void LayoutListHeader<T>(List<T> allPrefabs, string name, FilterContext filterContext, Rect rect) where T : MonoBehaviour 
	{
		var foldoutWidth = 120;
		filterContext.Show = EditorGUI.Foldout(new Rect(rect) { width = foldoutWidth }, filterContext.Show, name, true);
		var buttonRect = new Rect(rect) { x = foldoutWidth, width = rect.width - foldoutWidth };

		var oldColor = GUI.contentColor;

		if (filterContext.State != FilterState.All) 
		{
			GUI.contentColor = FilterHighlightColor;
		}

		if (GUI.Button(buttonRect, GetFilterName(filterContext), new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight }))
		{
			GUI.contentColor = oldColor;

			/* Enable Filter /
			if (allPrefabs.All(prefab => string.IsNullOrEmpty(GetAssetParentPath(m_gamePath, prefab)))) 
			/**/
			{
				var oldState = filterContext.State;
				// Only toggle highlighting, no parents
				filterContext.State = filterContext.State != FilterState.All
					? FilterState.All
					: FilterState.Highlighted;

				if (filterContext.State == FilterState.Highlighted && !allPrefabs.Any(IsHighlighted)) 
				{
					filterContext.State = FilterState.All;
				}

				if (oldState != filterContext.State) 
				{
					CreateMainGuiLists();
				}
			} 
			/* Enable Filter /
			else 
			{
				// TODO(arcnor): Avoid the dropdown from opening again if the button is pressed twice for the same `name`. Will need hackery because there is no "Close", thanks Unity!
				// TODO(arcnor): Maybe don't refresh all lists, but only the one that changed?
				// TODO(arcnor): We can keep the last element selected on the dropdown if we don't use a new AdvancedDropdownState() every time, but the current reset behavior might be better?
				var dropDown = new PathsDropdown(m_gamePath, allPrefabs, new AdvancedDropdownState(), filterContext, CreateMainGuiLists);
				dropDown.Show(rect);
			}
			/**/
		}

		GUI.contentColor = oldColor;
	}

	/// <summary>
	/// Analyses the given directory of prefabs and creates the <see cref="ReorderableList"/> of them for use in the
	/// PowerQuest Editor window.
	/// </summary>
	/// <remarks>Supports grouping of the elements with at most a depth of one, by looking at how the prefabs are grouped into sub dirs of <paramref name="prefabTypePath"/></remarks>
	/// <param name="listName">The name to appear in the header of the top level list header</param>
	/// <param name="prefabTypePath">The asset path associated with this object type, where either all the group folders are, or the ungrouped objects are</param>
	/// <param name="groupLists">The <see cref="GroupedPrefabContext"/> where the <see cref="ReorderableList"/>s produced will be cached</param>
	/// <param name="groupListState">Any pre-existing UI state for the grouped lists (so that certain bits of state can be carried across, like which ones were unfolded etc...), will be overridden with the new state upon return</param>
	/// <param name="allPrefabs">The complete list of prefabs for this object type, that is tracked on the global PowerQuest object (e.g. <see cref="PowerQuest.m_roomPrefabs"/>, <see cref="PowerQuest.m_characterPrefabs"/>, etc...). This is necessary as when elements are deleted, they will need to be cleaned up from this list also</param>
	/// <param name="listPrefabs">This is the list of prefabs that will actually appear in the <see cref="ReorderableList"/>s, it will be populated in this method after the given <paramref name="filterCtx"/> is applied to <paramref name="allPrefabs"/>. It is expected that this list has been cached elsewhere that that the <see cref="ReorderableList"/>s can query them for dereferencing the indexes</param>
	/// <param name="filterCtx">The filter information to appear in the header of the top level <see cref="ReorderableList"/> of groups</param>
	/// <param name="drawElementCallback"><para>The callback to draw the individual prefab elements in the <see cref="ReorderableList"/>s.</para> <para>The first argument is the list of prefabs indexed by the <paramref name="index"/> parameter.</para></param>
	/// <param name="onSelectCallback">The callback to be triggered when an element is selected in the <see cref="ReorderableList"/>s</param>
	/// <param name="onAddCallback"><para>The callback to be called when the Add button is clicked on the group <see cref="ReorderableList"/>s.</para><para>The first input string is expected to be the Path to that group</para></param>
	/// <param name="onRemoveCallback"><para>The callback to be triggered when we have to clean up a prefab.</para><para>The first input is the complete list of prefabs of that type on the global PowerQuest object that will need to be cleaned up, if it doesn't match the <see cref="ReorderableList.list"/> on the <see cref="ReorderableList"/>s</para></param>
	/// <typeparam name="T">The type of the PowerQuest Object (<see cref="RoomComponent"/>, <see cref="CharacterComponent"/>, <see cref="InventoryComponent"/>, etc...)</typeparam>
	/// <returns>The <see cref="ReorderableList"/> of the filtered prefabs for this object type</returns>
	ReorderableList FilterAndCreateReorderable<T>(
			string listName,
			string prefabTypePath,
			GroupedPrefabContext groupLists,
			ref List<PrefabGroupListItem<T>> groupListState,
			List<T> allPrefabs, List<T> listPrefabs, FilterContext filterCtx,
			Action<List<T>, Rect, int, bool, bool> drawElementCallback,
			ReorderableList.SelectCallbackDelegate onSelectCallback,
			Action<string, ReorderableList> onAddCallback,
			Func<List<T>, ReorderableList, bool> onRemoveCallback
		) where T : MonoBehaviour 
	{

		
		filterCtx.SearchString=m_searchString;
		
		ApplyFilter(allPrefabs, listPrefabs, filterCtx);

		var full = filterCtx.State == FilterState.All;
			
		groupLists.GroupedCollection.Clear();
		groupLists.UngroupedList = null;
		
		if (AreAssetsGroupedIntoSubFolders(prefabTypePath, listPrefabs, out List<PrefabGroupListItem<T>> groups, out List<T> ungrouped))
		{
			if (groupListState != null && groupListState.Count > 0)
			{
				Dictionary<string, PrefabGroupListItem<T>> oldStateDict =
					new Dictionary<string, PrefabGroupListItem<T>>();

				foreach (PrefabGroupListItem<T> groupListItem in groupListState)
				{
					oldStateDict.Add(groupListItem.GroupName, groupListItem);
				}

				foreach (PrefabGroupListItem<T> newGroupStateItem in groups)
				{
					if (oldStateDict.TryGetValue(newGroupStateItem.GroupName, out PrefabGroupListItem<T> oldItem))
					{
						newGroupStateItem.FoldedOut = oldItem.FoldedOut;
					}
				}
			}

			groupListState = groups;
				
			return CreateReorderableListWithSubfolderGroups<T>(
				filterCtx.State == FilterState.All,
				allPrefabs,
				listPrefabs,
				prefabTypePath,
				listName,
				filterCtx,
				groups,
				groupLists,
				drawElementCallback,
				onSelectCallback,
				onAddCallback,
				onRemoveCallback
			);
		}

		// Haven't returned, so list isn't filtered, so not removing from within group		

		// FIXME(arcnor): We should support adding and removing scenes when filtered (path to create stuff in is in `FilterContext.ParentPath` when state is `Parent`)
		return CreateReorderableListOfPrefabs(
			full,
			listName,
			allPrefabs,
			listPrefabs,
			filterCtx,
			(rect, index, active, focused) => drawElementCallback(listPrefabs, rect, index, active, focused), 
			onSelectCallback,
			(list) => onAddCallback?.Invoke(prefabTypePath, list),
			(list) =>  // remove callback, needs to remove from both listPrefabs and allPRefabs now
				{
					int index = list.index;
					listPrefabs.RemoveAt(index);
					onRemoveCallback?.Invoke(allPrefabs, list);
				}
		);
	}
	
	private ReorderableList CreateReorderableListOfPrefabs<T>(bool full, string listName, List<T> allPrefabs, List<T> listPrefabs, FilterContext filterCtx, ReorderableList.ElementCallbackDelegate drawElementCallback, ReorderableList.SelectCallbackDelegate onSelectCallback, ReorderableList.AddCallbackDelegate onAddCallback, ReorderableList.RemoveCallbackDelegate onRemoveCallback) where T : MonoBehaviour
	{
		ReorderableList list = new ReorderableList(listPrefabs, typeof(T), full, true, full, full)
		{
			drawHeaderCallback = rect => LayoutListHeader(allPrefabs, listName, filterCtx, rect),
			drawElementCallback = drawElementCallback,
			onSelectCallback = onSelectCallback,
			onAddCallback = onAddCallback,
			onRemoveCallback = onRemoveCallback,
			onCanRemoveCallback = list => Application.isPlaying == false
		};

		if (full)
		{
			list.onReorderCallback = reorderableList =>
			{
				// Need to reorder the complete list of prefabs to preserve the order across
				// Editor sessions, play mode and filter changes...
				Dictionary<T, int> orderLookup = new Dictionary<T, int>();

				if (reorderableList.list != null)
				{
					for (int i = 0; i < reorderableList.list.Count; ++i)
					{
						if (reorderableList.list[i] is T prefab)
						{
							orderLookup[prefab] = i;
						}
					}

					allPrefabs.Sort((x, y) =>
					{
						if (!orderLookup.TryGetValue(x, out int xIndex))
						{
							return 1;
						}

						if (!orderLookup.TryGetValue(y, out int yIndex))
						{
							return 1;
						}

						if (xIndex == yIndex)
						{
							return 0;
						}

						if (xIndex < yIndex)
						{
							return -1;
						}

						return 1;
					});
					
					EditorUtility.SetDirty(m_powerQuest);
				}
			};
		}

		return list;
	}

	#endregion
	
	#region Private Funcs

	/// <summary>
	/// Will check if the prefabs for the PQ Object Type <typeparamref name="T"/> are
	/// Grouped into Subfolders, returns true if they are, and will populate
	/// <paramref name="groupData"/> and <paramref name="ungroupedPrefabs"/> accordingly
	/// if they are.
	/// </summary>
	/// <remarks>A subdirectory depth of at most 1 is supported, its either grouped or not
	/// not grouped, we can't have a hierarchy of subgroups</remarks>
	/// <param name="prefabTypePath">The asset path associated with this object type, where either all the group folders are, or the ungrouped objects are</param>
	/// <param name="prefabs">The list of prefabs to appear in the Editor list</param>
	/// <param name="groupData">(Output) An ordered list of the grouped objects gathered into their groups, sorted in the order in which the group was first encountered when iterating through <paramref name="prefabs"/></param>
	/// <param name="ungroupedPrefabs">(Output) A list of prefabs that were not grouped (they would appear at the root level of the object type directory</param>
	/// <typeparam name="T">The type of the PowerQuest Object (<see cref="RoomComponent"/>, <see cref="CharacterComponent"/>, <see cref="InventoryComponent"/>, etc...)</typeparam>
	/// <returns>true if any of the assets are grouped into subfolders, false otherwise</returns>
	private bool AreAssetsGroupedIntoSubFolders<T>(string prefabTypePath, List<T> prefabs, out List<PrefabGroupListItem<T>> groupData, out List<T> ungroupedPrefabs) where T : MonoBehaviour
	{
		Dictionary<string, PrefabGroupListItem<T>> groupDict = new Dictionary<string, PrefabGroupListItem<T>>();

		PrefabGroupListItem<T> ungroupedGroupData = new PrefabGroupListItem<T>()
		{
			GroupName = "(Ungrouped)",
			Members = new List<T>(),
			Grouped = false
		};

		groupData = new List<PrefabGroupListItem<T>>();

		bool groupsDetected = false;

		foreach (T prefab in prefabs)
		{
			if ( prefab == null )
				continue;

			if (TryGetPrefabSubDir(prefab, prefabTypePath, out string subDir))
			{
				groupsDetected = true;
				if (groupDict.TryGetValue(subDir, out PrefabGroupListItem<T> group))
				{
					group.Members.Add(prefab);
				}
				else
				{
					// First time we have encountered the group,
					// create new group data for it and add it
					// to the list of group data to mark its position
					// in the group list...
					group = new PrefabGroupListItem<T>()
					{
						GroupName = subDir,
						Members = new List<T>() { prefab },
						Grouped = true,
					};
					
					groupDict.Add(subDir, group);
					groupData.Add(group);
				}
			}
			else
			{
				ungroupedGroupData.Members.Add(prefab);
				if (ungroupedGroupData.Members.Count == 1)
				{
					groupData.Add(ungroupedGroupData);
				}
			}
		}

		if (groupsDetected)
		{
			ungroupedPrefabs = ungroupedGroupData.Members;
			return true;
		}

		groupData = null;
		ungroupedPrefabs = null;

		return false;
	}

	/// <summary>
	/// Checks to see if a particular prefab of the specified PQ Object Type <typeparamref name="T"/>
	/// is at the root of the object directory for that type (ungrouped) or contained within a single subfolder (grouped)
	/// </summary>
	/// <param name="prefab">The prefab to check</param>
	/// <param name="prefabTypeDir">The asset path associated with this object type, where either all the group folders are, or the ungrouped objects are</param>
	/// <param name="subDir">(Output) if the prefab is in a sub directory, that sub directory will go into this string, will be set to null otherwise.</param>
	/// <typeparam name="T">The type of the PowerQuest Object (<see cref="RoomComponent"/>, <see cref="CharacterComponent"/>, <see cref="InventoryComponent"/>, etc...)</typeparam>
	/// <returns>true if the prefab is in a subfolder of <paramref name="prefabTypeDir"/>, false otherwise</returns>
	private bool TryGetPrefabSubDir<T>(T prefab, string prefabTypeDir, out string subDir) where T : MonoBehaviour
	{
		string path = QuestEditorUtils.GetPrefabPath(prefab.gameObject);

		if (!path.StartsWith(prefabTypeDir))
		{
			subDir = null;
			return false;
		}
		
		// Get the directory where all the assets for this character/room/gui/etc...
		// are...
		var assetPath = Path.GetDirectoryName(path.Substring(prefabTypeDir.Length));

		if (!string.IsNullOrEmpty(assetPath) && (assetPath[0] == '/' || assetPath[0] == '\\'))
		{
			assetPath = assetPath.Substring(1);
		}

		subDir = Path.GetDirectoryName(assetPath);
		
		if (string.IsNullOrEmpty(subDir))
		{
			subDir = null;
			return false;
		}

		return true;
	}
	
	/// <summary>
	/// Create a <see cref="ReorderableList"/> of a specified PQ Object Type <typeparamref name="T"/>
	/// with one level of sub grouping.
	/// </summary>
	/// <param name="isFullList">Set this to false if the input list has been filtered in any way (The lists behave differently if the elements have been filtered)</param>
	/// <param name="allPrefabs">The complete list of prefabs for this object type, that is tracked on the global PowerQuest object (e.g. <see cref="PowerQuest.m_roomPrefabs"/>, <see cref="PowerQuest.m_characterPrefabs"/>, etc...). This is necessary as when elements are deleted, they will need to be cleaned up from this list also</param>
	/// <param name="listedPrefabs">This is the list of prefabs that will actually appear in the <see cref="ReorderableList"/>s</param>
	/// <param name="prefabTypePath">The asset path associated with this object type, where either all the group folders are, or the ungrouped objects are</param>
	/// <param name="listName">The name to appear in the header of the top level list header</param>
	/// <param name="filterCtx">The filter information to appear in the header of the top level <see cref="ReorderableList"/> of groups</param>
	/// <param name="groups">The list of grouped up prefabs to appear in the lists</param>
	/// <param name="groupLists">The <see cref="GroupedPrefabContext"/> where the <see cref="ReorderableList"/>s produced will be cached</param>
	/// <param name="drawElementCallback"><para>The callback to draw the individual prefab elements in the group <see cref="ReorderableList"/>s.</para> <para>The first argument is the list of prefabs indexed by the <paramref name="index"/> parameter.</para></param>
	/// <param name="onSelectCallback">The callback to be triggered when an element is selected in the group <see cref="ReorderableList"/>s</param>
	/// <param name="onAddCallback"><para>The callback to be called when the Add button is clicked on the group <see cref="ReorderableList"/>s.</para><para>The first input string is expected to be the Path to that group</para></param>
	/// <param name="onRemoveCallback"><para>The callback to be triggered when we have to clean up a prefab.</para><para>The first input is the complete list of prefabs of that type on the global PowerQuest object that will need to be cleaned up, if it doesn't match the <see cref="ReorderableList.list"/> on the <see cref="ReorderableList"/>s</para></param>
	/// <typeparam name="T">The type of the PowerQuest Object (<see cref="RoomComponent"/>, <see cref="CharacterComponent"/>, <see cref="InventoryComponent"/>, etc...)</typeparam>
	/// <returns>The top level <see cref="ReorderableList"/> of groups</returns>
	ReorderableList CreateReorderableListWithSubfolderGroups<T>(
		bool isFullList,
		List<T> allPrefabs,
		List<T> listedPrefabs,
		string prefabTypePath,
		string listName,
		FilterContext filterCtx,
		List<PrefabGroupListItem<T>> groups,
		GroupedPrefabContext groupLists,
		Action<List<T>, Rect, int, bool, bool> drawElementCallback,
		ReorderableList.SelectCallbackDelegate onSelectCallback,
		Action<string, ReorderableList> onAddCallback,
		Func<List<T>, ReorderableList, bool> onRemoveCallback
	) where T : MonoBehaviour
	{
		// Create Reorderable List of subgroups:
		// Create the top level list of groups
		ReorderableList list = new ReorderableList(groups, typeof(string), isFullList, true, false, false)
		{
			drawHeaderCallback = rect => LayoutListHeader(listedPrefabs, listName, filterCtx, rect),
		};
		
		list.elementHeightCallback = (index) => GroupElementHeight<T>(isFullList, list, index);

		list.drawElementCallback = (rect, index, active, focused) => DrawGroupElement<T>(
			list,
			groupLists,
			isFullList,
			rect,
			index
		);
		
		list.onReorderCallback = reorderedList => ReorderCompletePrefabList<T>(allPrefabs, groups);
				
		// Go through and build the lists for the sub groups:
		foreach (PrefabGroupListItem<T> groupData in groups)
		{
			groupData.Members ??= new List<T>();

			string groupPath = groupData.Grouped
				? Path.Combine(prefabTypePath, groupData.GroupName)
				: prefabTypePath;

			ReorderableList groupList = new ReorderableList(groupData.Members, typeof(T), isFullList, false, isFullList, isFullList)
			{
				drawElementCallback = (subRect, subIndex, active, focused) => drawElementCallback?.Invoke(groupData.Members, subRect, subIndex, active, focused),
				onSelectCallback = onSelectCallback,
				onAddCallback = reorderableList => onAddCallback?.Invoke(groupPath, reorderableList),
				onRemoveCallback = reorderableList => RemoveFromObjectGroup<T>(onRemoveCallback, allPrefabs, groupData.Members, reorderableList),
				onCanRemoveCallback = list => Application.isPlaying == false,
				onReorderCallback = reorderedList => ReorderCompletePrefabList(allPrefabs, groups)
			};

			// Set the main  list's callbacks for context menues...
			//list.onAddCallback = groupList.onAddCallback;
			//list.onRemoveCallback = groupList.onRemoveCallback;

			if (groupData.Grouped)
			{
				groupLists.GroupedCollection.Add(groupData.GroupName, groupList);
			}
			else
			{
				groupLists.UngroupedList = groupList;
			}
		}

		return list;
	}
	

	ReorderableList FindReorderableList(RoomComponent itemComponent)
	{
		foreach ( ReorderableList list in m_listRoomGroups.GroupedCollection.Values )
		{ 
			if ( list.list.Contains(itemComponent) )
				return list;
		}
		if ( m_listRoomGroups.UngroupedList != null )
			return m_listRoomGroups.UngroupedList;
		return m_listRooms;
	}

	// Finds the reorderable list that a component is in- used for adding/deleting from context menu. This system is sooo spaghetti now eeeep.
	ReorderableList FindReorderableList<T>(T itemComponent) where T : MonoBehaviour
	{ 		
		GroupedPrefabContext groupedPrefabContext; 
		if ( itemComponent is RoomComponent )
			groupedPrefabContext = m_listRoomGroups;
		else if ( itemComponent is CharacterComponent )
			groupedPrefabContext = m_listCharacterGroups;
		else if ( itemComponent is InventoryComponent )
			groupedPrefabContext = m_listInventoryGroups;
		else if ( itemComponent is DialogTreeComponent )
			groupedPrefabContext = m_listDialogTreeGroups;
		else //if ( itemComponent is GuiComponent )
			groupedPrefabContext = m_listGuiGroups;
		foreach ( ReorderableList list in groupedPrefabContext.GroupedCollection.Values )
		{ 
			if ( list.list.Contains(itemComponent) )
				return list;
		}
		return groupedPrefabContext.UngroupedList;
	}

	/// <summary>
	/// When removing an object from a group <see cref="ReorderableList"/>,
	/// if the user decides to go ahead with it, we have to make sure we <em>also</em>
	/// go through the global list of these prefabs and clean up the entry in that list also
	/// as previously it was assumed that the list managed by the <see cref="ReorderableList"/>
	/// was the same as the global list
	/// </summary>
	/// <param name="onRemoveCallback"><para>The callback to be triggered when we have to clean up a prefab.</para><para>The first input is the complete list of prefabs of that type on the global PowerQuest object that will need to be cleaned up, if it doesn't match the <see cref="ReorderableList.list"/> on the <see cref="ReorderableList"/>s</para></param>
	/// <param name="allPrefabs">The complete list of prefabs we will need to clean up the removed prefab from.</param>
	/// <param name="groupDataMembers">The list of members in the group <paramref name="reorderableList"/> is managing.</param>
	/// <param name="reorderableList">The <see cref="ReorderableList"/> that is managing this group</param>
	/// <typeparam name="T">The type of the PowerQuest Object (<see cref="RoomComponent"/>, <see cref="CharacterComponent"/>, <see cref="InventoryComponent"/>, etc...)</typeparam>
	private void RemoveFromObjectGroup<T>(Func<List<T>, ReorderableList, bool> onRemoveCallback, List<T> allPrefabs, List<T> groupDataMembers, ReorderableList reorderableList) where T : MonoBehaviour
	{
		// Remove it from the complete list first...
		int index = reorderableList.index;

		if (index < 0 || index >= reorderableList.list.Count)
		{
			return;
		}

		if (!(reorderableList.list[index] is T pqObject))
		{
			return;
		}

		int completeListIndex = allPrefabs.FindIndex(x => x == pqObject);

		if (onRemoveCallback != null)
		{
			if (onRemoveCallback.Invoke(groupDataMembers, reorderableList))
			{
				allPrefabs.RemoveAt(completeListIndex);
				EditorUtility.SetDirty(m_powerQuest);
			}
		}
	}

	/// <summary>
	/// Whenever we reorder either a top level <see cref="ReorderableList"/> of groups, or we reorder the <see cref="ReorderableList"/> of a particular group,
	/// we need to re-order the complete index of that prefab type so that the re-ordering will be preserved when the <see cref="ReorderableList"/>s
	/// are next rebuilt (for e.g. when the Editor is restarted)
	/// </summary>
	/// <param name="completeList">The complete list of prefabs for this object type, that is tracked on the global PowerQuest object (e.g. <see cref="PowerQuest.m_roomPrefabs"/>, <see cref="PowerQuest.m_characterPrefabs"/>, etc...). This is necessary so that the reordering is not lost next time the <see cref="ReorderableList"/>s are built</param>
	/// <param name="groups">This should be identical to the <see cref="ReorderableList.list"/> on the top level <see cref="ReorderableList"/>. This will ensure that the ordering of this list and the sub lists of each group represents the ordering in the UI <see cref="ReorderableList"/>s</param>
	/// <typeparam name="T">The type of the PowerQuest Object (<see cref="RoomComponent"/>, <see cref="CharacterComponent"/>, <see cref="InventoryComponent"/>, etc...)</typeparam>
	private void ReorderCompletePrefabList<T>(List<T> completeList, List<PrefabGroupListItem<T>> groups) where T : MonoBehaviour
	{
		completeList.Clear();

		foreach (PrefabGroupListItem<T> group in groups)
		{
			completeList.AddRange(group.Members);
		}
		
		EditorUtility.SetDirty(m_powerQuest);
	}

	private float GroupElementHeight<T>(bool isFullList, ReorderableList list, int index)
	{
		if (index < 0 || index >= list.list.Count)
		{
			return 0.0f;
		}

		if (!(list.list[index] is PrefabGroupListItem<T> groupData))
		{
			return 0.0f;
		}

		if (!groupData.FoldedOut && isFullList)
		{
			// if its not folded out, its just oneline...
			return EditorGUIUtility.singleLineHeight;
		}
		
		// Otherwise we have to account for:
		// 1. the Foldout/Header
		// 2. A buffer space between the foldout and the list?
		// 3. Each member of the sub list
		// 4. A buffer space for the Add/Remove buttons...

		const float headerBufferSpace = 32.0f;
		const float memberHeight = 23.0f;
		float footerSpace = 2.0f * EditorGUIUtility.singleLineHeight;

		return headerBufferSpace + (groupData.Members.Count * memberHeight) + footerSpace;
	}

	private void DrawGroupElement<T>(
		ReorderableList list,
		GroupedPrefabContext groupedLists,
		bool isFullList,
		Rect rect,
		int index
	)
	{
		if (list.list == null || list.list.Count <= index)
		{
			return;
		}

		if (!(list.list[index] is PrefabGroupListItem<T> groupItem))
		{
			return;
		}


		Rect foldoutRect = new Rect(
			rect.x,
			rect.y,
			EditorGUIUtility.labelWidth,
			EditorGUIUtility.singleLineHeight
		);

		bool foldedOut = EditorGUI.Foldout(foldoutRect, groupItem.FoldedOut || !isFullList, groupItem.GroupName, toggleOnLabelClick: true);

		if (isFullList)
		{
			groupItem.FoldedOut = foldedOut;
		}

		if (foldedOut)
		{
			EditorGUI.indentLevel++;

			try
			{
				const float headerBufferSize = 10.0f;
				if (groupItem.Grouped)
				{
					if (groupedLists.GroupedCollection.TryGetValue(groupItem.GroupName, out ReorderableList groupList))
					{
						groupList?.DoList(
							new Rect(
								rect.x, rect.y + EditorGUIUtility.singleLineHeight + headerBufferSize,
								rect.width, rect.height - EditorGUIUtility.singleLineHeight
							)
						);
					}
				}
				else
				{
					groupedLists.UngroupedList?.DoList(
						new Rect(
							rect.x, rect.y + EditorGUIUtility.singleLineHeight + headerBufferSize,
							rect.width, rect.height - EditorGUIUtility.singleLineHeight
						)
					);
				}
			}
			finally
			{
				EditorGUI.indentLevel--;
			}
		}
	}

	#endregion
}

}
