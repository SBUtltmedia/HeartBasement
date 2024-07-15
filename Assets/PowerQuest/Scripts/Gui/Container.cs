using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

// A Quest Gui Control that contains other gui controls. Add a grid container component too for some more functionality.
[System.Serializable] 
[AddComponentMenu("Quest Gui/Container")]
public partial class Container : GuiControl, IContainer
{
    GridContainer m_grid = null;

    [Tooltip("If true, the container's size (for Fit To/Align To purposes) expands as items are added/removed from the grid")]
    [SerializeField] bool m_dynamicGridSize = false;
    
    public GridContainer Grid => m_grid;

    void InitComponents()
    {
        if ( m_grid == null )
        {
            m_grid = GetComponent<GridContainer>();            
        }
    }

    void Awake()
    {
        InitComponents();
    }

    // Start is called before the first frame update
    protected override void Start()
    {
		base.Start();
    }

    // Update is called once per frame
    void Update()
    {        
    }


    public override void UpdateBaseline()
    {
        // "Don't update baseline of child items
    }
    
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform exclude = null)
	{        
        if ( Application.isEditor )
            InitComponents();
        if ( m_grid != null )
        {
            if ( m_dynamicGridSize )            
                return m_grid.FilledRect;
            return m_grid.Rect;
        }
        return base.GetRect(exclude);
	}
	
	public bool GetIsControlInGrid(IGuiControl control)
	{
		// This stuff could be in the container itself (eg. add GetItemUp/GetItemDown/GetItemLeft/GetItemRight)
		if ( Grid == null || Grid.Items == null || Grid.Items.Count == 0 || control == null || control.Instance == null )
			return false;	
		return Grid.Items.Exists(item=>item == control.Instance.transform);	
	}

	public IGuiControl GetNextControlUp(IGuiControl current) => GetNextControl(current, Quest.eGuiNav.Up);
	public IGuiControl GetNextControlDown(IGuiControl current) => GetNextControl(current, Quest.eGuiNav.Down);
	public IGuiControl GetNextControlLeft(IGuiControl current) => GetNextControl(current, Quest.eGuiNav.Left);
	public IGuiControl GetNextControlRight(IGuiControl current) => GetNextControl(current, Quest.eGuiNav.Right);

	// Returns next control in direction, or null if none found
	public IGuiControl GetNextControl(IGuiControl current, Quest.eGuiNav dir)
	{
		// This stuff could be in the container itself (eg. add GetItemUp/GetItemDown/GetItemLeft/GetItemRight)
		if ( Grid == null || Grid.Items == null || Grid.Items.Count == 0 )
			return null;
			
		int index = 0;
		if ( current != null && current.Instance != null ) // Find current item
			index = Grid.Items.FindIndex(item=>item == current.Instance.transform);
			

		IGuiControl result = null;
		while ( result == null )
		{
			// Keep trying
			if ( dir == eGuiNav.Right )
			{
				index++;
				if ( index % Grid.ColumnsPerRow == 0 || index >= Grid.Items.Count )
					return null; // reached end of row
			}
			else if ( dir == eGuiNav.Left )
			{
				if ( index % Grid.ColumnsPerRow == 0 || index <= 0 )
					return null; // reach start of row
				index--;
			}
			else if ( dir == eGuiNav.Up)
			{
				if ( index / Grid.ColumnsPerRow <= 0 )
					return null; // reached first row
				index -= Grid.ColumnsPerRow;
			}
			else if ( dir == eGuiNav.Down )
			{
				index += Grid.ColumnsPerRow;
				if ( index >= Grid.Items.Count )
				{
					return null; // reached last row
					// Potentially in this case we'd want to jump to the final clickable item, rather than just not scrolling down.
				}
			}
			
			result = Grid.Items[index].GetComponent<IGuiControl>();		
			if ( result == null || (result as IQuestClickable).Clickable == false )
				result = null; // result not clickable, so we'll continue looping
		}
		return result;
	}
}

}
