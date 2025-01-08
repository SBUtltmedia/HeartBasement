using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{
/**
 * Grid container, for laying out objects on a grid.
 * 
 * Currently objects are always place left to right, then top to bottom. 
 */

[ExecuteInEditMode]
[AddComponentMenu("Quest Gui Layout/Grid")]
public class GridContainer : MonoBehaviour
{
	//////////////////////////////////////////////////////////////////////////////////////////
	// Definitions

	/*
	enum eCorner { TopLeft,TopRight,BottomLeft,BottomRight }
	enum eAxis { Horizontal, Vertical }
	*/

	//////////////////////////////////////////////////////////////////////////////////////////
	// Editor vars	


    [Tooltip("If true, child objects of the container are automatically added to the container")]
    [SerializeField] bool m_autoLayoutChildren = false;

	[SerializeField] Vector2 m_itemSpacing = new Vector2(16,16);
		
	[Tooltip("How many items are displayed on a row before wrapping to the next one. If zero, it never wraps")]	
	[Min(0)]
	[SerializeField] int m_columnsPerRow = 0;
	//[Tooltip("How many rows are displayed before the list box scrolls")]
	//[SerializeField] int m_rows = 0;

	
	[Tooltip("Number of columns visible before scrolling is necessary. Only used when m_rowWidth is Zero")]
	[Min(0)]
	[SerializeField] int m_scrollColumns = 0;
	
	[Tooltip("Number of rows visible before scrolling. If zero, it's treated as infinite rows")]
	[Min(0)]
	[SerializeField] int m_scrollRows = 0;	

	[Tooltip("How many pixels outside the size to continue drawing objects. Useful for smooth scrolling with a sprite-mask")]
	[SerializeField] Vector2 m_overdraw = Vector2.zero;

	//[Tooltip("Whether to leave a space where a Disabled/Hidden item is in the list. If true, other items will fill in the gap")]
	//[SerializeField] bool m_removeDisabledItems = true;
	
	[SerializeField] List<Transform> m_items = new List<Transform>();
	

	//[SerializeField, HideInInspector] RectCentered m_customSize = RectCentered.zero;
	// Size in world pos
	[SerializeField, HideInInspector] Vector2 m_size = Vector2.zero;//RectCentered.zero;


	// Maybe should start assuming these don't change
	
	/*
		Horizontal/vertical changes how the index maps to the columns
		Start corner, etc changes the "offset" x/y of an index. EG: topright, then (0,0) is top right...
	*/
	/*
	[SerializeField] eCorner m_startCorner = eCorner.TopLeft;
	[SerializeField] eAxis m_startAxis = eAxis.Horizontal;
	*/
	
	//////////////////////////////////////////////////////////////////////////////////////////
	// Private vars

	Vector2 m_offset = Vector2.zero;	
    int m_numChildrenCached = -1;
	
	//////////////////////////////////////////////////////////////////////////////////////////
	// Public functions

	// Set custom size based on the current position, grid size, and number of grid columns and rows. This doesn't happen automatically when setting number or rows/columns
	public void SetSizeFromGrid() 
	{
		m_size = new Vector2(WidthInColumns * m_itemSpacing.x,HeightInRows * m_itemSpacing.y);
	}

	public RectCentered FilledRect { get 
	{
		Vector2 size = new Vector2(FilledColumns * m_itemSpacing.x, FilledRows * m_itemSpacing.y);
		RectCentered result = new RectCentered();
		result.Size = size;

		// Grid pivot is center of first item. To ge tthe center, we do pos + (width - itemspacing)*0.5f.  Note that y on the offset is flipped (because down is negative)
		result.Center = (Vector2)transform.position + (result.Size - ItemSpacing).WithFlippedY()*0.5f;
		return result;
	}}

	// The size in world coords of the grid. Setting this will change the columns per row, and scroll rows, etc to fit items in the grid
	public RectCentered Rect
	{ 
		get
		{
			if ( m_size == Vector2.zero )
				SetSizeFromGrid();
			
			RectCentered result = new RectCentered();
			result.Size = m_size;

			// Grid pivot is center of first item. To ge tthe center, we do pos + (width - itemspacing)*0.5f.  Note that y on the offset is flipped (because down is negative)
			result.Center = (Vector2)transform.position + (result.Size - ItemSpacing).WithFlippedY()*0.5f;
			return result;
		} 
		set
		{
			int rows = Mathf.Max(1,Mathf.FloorToInt(value.Height / ItemSpacing.y));
			int columns = Mathf.Max(1,Mathf.FloorToInt(value.Width / ItemSpacing.x));
			if ( rows > 1 )
			{
				ColumnsPerRow = columns;
				ScrollColumns = 0;
			}
			else 
			{
				ColumnsPerRow = 0;
				ScrollColumns = columns;	
			}
			ScrollRows = rows;			

			// Basically reverse of "get"
			transform.position = (value.Center - (value.Size - ItemSpacing).WithFlippedY()*0.5f).WithZ(transform.position.z);
			
			m_size = value.Size;

			DoLayout();
		} 
	}

	public Vector2 ItemSpacing 
	{
		get=>m_itemSpacing;
		set
		{
			Vector2 newSpacing = new Vector2(Mathf.Max(1,value.x), Mathf.Max(1,value.y));
			if ( newSpacing != m_itemSpacing )
			{
				m_itemSpacing = newSpacing;
				SetSizeFromGrid();
				DoLayout();
			}
		}
	}

	public void AddItem( Transform item )
	{
		m_items.Add(item);
		DoLayout();
	}

	public void RemoveItem( Transform item )
	{
		m_items.Remove(item);
		DoLayout();
	}
	
	public void RemoveAt(int index) 
	{
		m_items.RemoveAt(index);
		DoLayout();
	}

	// The list of items
	public List<Transform> Items => m_items;
	
	public bool GetItemVisible(int index)
	{
		Vector2 pos = GetItemPos(index);
		
		Vector2 visibleSize = FilledSize;
		pos -= ScrollOffset;
		bool visible = pos.x >= -m_overdraw.x && pos.y <= m_overdraw.y ;
		if ( visibleSize.x > 0 )
			visible &= pos.x < visibleSize.x+m_overdraw.x;
		if ( visibleSize.y > 0 )
			visible &= pos.y > -(visibleSize.y+m_overdraw.y);
		return visible;	
	}

	public Vector2 GetItemPos(int index)
	{		
		index = ItemIndex(index);
		Vector2 pos = Vector2.zero;				
		pos.x = IndexToColumn(index) * (float)m_itemSpacing.x;
		pos.y = -IndexToRow(index) * (float)m_itemSpacing.y;
		
		//if ( m_startCorner == eCorner.TopRight || m_startCorner == eCorner.BottomRight )
		//	pos.x = -pos.x;			
		//if ( m_startCorner == eCorner.BottomLeft || m_startCorner == eCorner.BottomRight )
		//	pos.y = -pos.y;

		return pos;
	}
	
	/// Retrieves the index of the item
	public int GetItemIndex( Transform item )
	{
		return Items.FindIndex(gridItem=>gridItem == item); 
	}
	/// Retrieves the index of the item (or its monobehaviour)
	public int GetItemIndex( IGuiControl item )
	{
		Transform itemTransform = item.Instance.transform;
		return Items.FindIndex(gridItem=>gridItem == itemTransform); 
	}
	/// Retrieves the index of the item (or its monobehaviour)
	public int GetItemIndex<T>( T item ) where T : MonoBehaviour
	{
		Transform itemTransform = item.transform;
		return Items.FindIndex(gridItem=>gridItem == itemTransform); 
	}
	
	/// Columns before wrapping around to next row 0 if no limit
	public int ColumnsPerRow { get => m_columnsPerRow; set { m_columnsPerRow = Mathf.Max(0,value); DoLayout(); } }

	/// Columns before scrolling. 0 if no limit
	public int ScrollColumns { get => m_scrollColumns; set { m_scrollColumns = Mathf.Max(0,value); DoLayout(); } }
	
	/// Rows before scrolling. 0 if no limit
	public int ScrollRows { get => m_scrollRows; set { m_scrollRows = Mathf.Max(0,value); DoLayout(); } }

	/// The number of columns of items (filled columns, not the max columns)
	public int FilledColumns => Constrain(ItemCount,m_columnsPerRow,m_scrollColumns);
	
	/// The number of rows of items (ie: not the max)
	public int FilledRows => Constrain(LastFilledRow, HeightInRows);
	
	/// Gives the column for an item index	
	public int IndexToColumn(int index) => (index <= 0 ? 0 : m_columnsPerRow <= 0 ? index : (index % m_columnsPerRow) );

	/// Gives the row for an item index
	public int IndexToRow(int index) => ( (m_columnsPerRow <= 0 || index <= 0) ? 0 : (index / m_columnsPerRow) );

	/// The last column with an item in it, even if outside bounds
	public int LastFilledColumn => Constrain(ItemCount,m_columnsPerRow);  // if wrapping enabled, and ahve warpped, return wrap width, else return number of items
			
	/// The last row with an item in it, even if outside bounds
	public int LastFilledRow => IndexToRow(ItemCount-1)+1; // Return the row of the last item (plus 1, because we want the total num, not last index)

	/// Returns the number of columns of the gridContainer, before scrolling or wrapping. Basically the "width" of the grid in columns.
	public int WidthInColumns { get {
		
		if ( m_scrollColumns > 0 && (m_columnsPerRow <= 0 || m_columnsPerRow > m_scrollColumns ) )
			return m_scrollColumns; // If columns num is set, and row width isn't (or is bigger than columns)

		if ( m_columnsPerRow > 0 )
			return m_columnsPerRow;

		return 0;			
		
	} }

	/// Returns the max number of columns that can be shown, or 0 if infinite
	public int HeightInRows => m_scrollRows;
	
	/// Grid size (world units) is the size before scrolling. If no max is set, returns 0 for that axis
	//public Vector2 GridSize => new Vector2(WidthInColumns * m_itemSpacing.x,HeightInRows * m_itemSpacing.y);

	/// Actual size of container's current visible contents (world units) 
	public Vector2 FilledSize  => new Vector2( FilledColumns * m_itemSpacing.x, FilledRows * m_itemSpacing.y );

	 
	/// Scroll offset in world coordinates
	public Vector2 ScrollOffset
	{
		get { return m_offset; }
		set
		{
			if ( value != m_offset )
			{
				m_offset = value;
				DoLayout();
			}
		}
	}


	// Todo: only change if possible, and return true on success
	public void NextRow()    { ScrollOffset = ScrollOffset - new Vector2(0,m_itemSpacing.y); }	
	public void NextColumn() { ScrollOffset = ScrollOffset + new Vector2(m_itemSpacing.x,0); }	
	public void PrevRow()    { ScrollOffset = ScrollOffset + new Vector2(0,m_itemSpacing.y); }
	public void PrevColumn() { ScrollOffset = ScrollOffset - new Vector2(m_itemSpacing.x,0); }
		
	public bool HasNextColumn() { return WidthInColumns <= 0 ? false : ColumnOffset + WidthInColumns < LastFilledColumn; }
	public bool HasPrevColumn() { return ColumnOffset > 0; }
	public bool HasNextRow() { return m_columnsPerRow <= 0 || HeightInRows <= 0 ? false : RowOffset + HeightInRows < LastFilledRow; }	// don't scroll if there's no wrapping, or no row constraints
	public bool HasPrevRow() { return RowOffset > 0; }

	public int RowOffset 
	{ 
		get { return -Mathf.RoundToInt(m_offset.y / m_itemSpacing.y ); } 
		set	{ ScrollOffset = ScrollOffset.WithY( -value * m_itemSpacing.y ); }
	}
	public int ColumnOffset
	{
		get { return Mathf.RoundToInt(m_offset.x / m_itemSpacing.x ); } 
		set	{ ScrollOffset = ScrollOffset.WithX(value * m_itemSpacing.x); }
	}


	public void ForceUpdate() { DoLayout(); }


	//////////////////////////////////////////////////////////////////////////////////////////
	// Unity functions

	// Start is called before the first frame update
	void Start()
    {     
		DoLayout();   
    }

	void LateUpdate()
	{		
		//if ( Application.isPlaying == false )
			DoLayout();
	}
	

	//////////////////////////////////////////////////////////////////////////////////////////
	// Private Funcs
	
	int ItemIndex(int itemId)
	{
		/*if ( m_removeDisabledItems )
		{		
			int result = 0;	
			for (int i= 0; i < itemId; ++i)
			{
				if ( m_items[i].gameObject.activeSelf)
					++result;
			}			
			return result;
		}*/
		return itemId;
	}

	int ItemCount { get 
	{		
		/*if ( m_removeDisabledItems )
		{			
			int result = 0; 
			m_items.ForEach(item=> {if ( item.gameObject.activeSelf ) ++result;} );
			return result;
		}*/
		return m_items.Count;
	} }

	void DoLayout()
	{		

		UpdateLayoutChildren();

		for ( int i = 0; i < m_items.Count; ++i )
		{
			Vector2 pos = GetItemPos(i) - ScrollOffset;//new Vector2(-ScrollOffset.x,ScrollOffset.y);
			Transform item = m_items[i];
			if ( m_items[i] == null )
				return;

			if ( GetItemVisible(i) )
			{
				item.gameObject.SetActive(true);
				m_items[i].position = (transform.position + (Vector3)pos).WithZ(m_items[i].position.z);
			}
			else 
			{
				item.gameObject.SetActive(false);
			}
		}

	}

	// Sets grid items to child objects, if m_containChildren is true
    void UpdateLayoutChildren()
    {   
        if ( m_autoLayoutChildren == false || transform.childCount == m_numChildrenCached )
            return;

        int numChildren = transform.childCount;

        // Remove excess items
        while ( m_items.Count > numChildren )
            m_items.RemoveAt(m_items.Count-1);

        // Update child items
        for ( int i = 0; i < numChildren; ++i )
        {
            if ( i < m_items.Count )
                m_items[i]=transform.GetChild(i);
			else 
				m_items.Add(transform.GetChild(i));
        }

        m_numChildrenCached = numChildren;

    }
	
	// Constrains the value, unless the constraint is zero, then it's ignored. Can optionally set 2 consraints. 
	// Useful because the wrapping, and scroll limits are all ignored if zero.
	int Constrain(int value, int constraint1 = 0, int constraint2 = 0)
	{		
		if ( constraint2 > 0 )
			value = Constrain( value, constraint2 );
		if ( constraint1 > 0 && constraint1 < value )
			return constraint1;			
		return value > 0 ? value : 0;	
	}
}
}
