using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PowerTools.Quest
{


#region Class: EditorUtils
public class EditorUtils
{
	public static readonly int NUM_BUILD_IN_PROPERTIES = 8;
	
	public delegate void RefreshTargetDelegate();
	public delegate void CreateListItemGUIDelegate( int index );
	public delegate T CreateNewListItemDelegate<T>( T before, T after );
	
	public static void UpdateListInspector<T>( ref List<T> list, RefreshTargetDelegate refreshTargetDelegate, CreateListItemGUIDelegate createItemGUIDelegate, CreateNewListItemDelegate<T> createNewItemDelegate )  where T : new()
	{
		GUILayout.Space(5);		
		
		int addAt = -2;
		int removeAt = -1;
		int upAt = -1;
		int downAt = -1;
		
		int count = list.Count;
		for (int i = 0; i < count; ++i )
		{
			EditorGUILayout.BeginHorizontal();
			
				//EditorGUILayout.BeginVertical(GUILayout.MaxWidth(25));
				EditorGUILayout.LabelField( i.ToString()+"-",EditorStyles.boldLabel, GUILayout.MaxWidth(17) );
				
				//EditorGUILayout.EndVertical();
			
				createItemGUIDelegate( i );
			
			
				if ( GUILayout.Button( new GUIContent("\u25B2"), EditorStyles.miniButtonLeft, GUILayout.MaxWidth(20) ) )
				{
					upAt = i;
				}
				if ( GUILayout.Button( new GUIContent("\u25BC"), EditorStyles.miniButtonMid,GUILayout.MaxWidth(20) ) )
				{
					downAt = i;
				}
				if ( GUILayout.Button( new GUIContent("+"), EditorStyles.miniButtonMid,GUILayout.MaxWidth(20) ) )
				{
					addAt = i;
				}
				if ( GUILayout.Button( new GUIContent("X"),  EditorStyles.miniButtonRight, GUILayout.MaxWidth(20) ) )
				{
					removeAt = i;
				}
			
			EditorGUILayout.EndHorizontal();
			
				
				//EditorGUILayout.BeginVertical(GUILayout.MaxWidth(40));
				
				//EditorGUILayout.EndVertical();
			
		}
		if ( count == 0 )
		{
			if ( GUILayout.Button( new GUIContent("+"), EditorStyles.miniButtonMid,GUILayout.MaxWidth(20) ) )
			{
				addAt = -1;
			}
		}
		
		if ( addAt >= -1 )
		{
			T prev = default(T);			
			T next = default(T);
			if ( addAt == -1 )
			{				
				if ( count > 0 )
				{
					prev = list[count-1];
				}
			}
			else 
			{
				prev = list[addAt];
			}
			
			if ( addAt+1 >= count )
			{			
				if ( count > 0 )
				{
					next = list[0];
				}		
			}
			else
			{
				next = list[addAt+1];
			}
			
			if ( createNewItemDelegate != null )
				list.Insert(addAt+1, createNewItemDelegate( prev, next ) );			
			else
				list.Insert(addAt+1, new T() );			
			
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();
		}
		
		else if ( removeAt >= 0 )
		{
			list.RemoveAt(removeAt);
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();	
		}		
		
		else if ( upAt > 0 )
		{
			T temp = list[upAt-1];
			list[upAt-1] = list[upAt];
			list[upAt] = temp;
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();
		}
		else if ( downAt >= 0 && downAt < list.Count-1)
		{
			T temp = list[downAt];
			list[downAt] = list[downAt+1];
			list[downAt+1] = temp;
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();
		}
		
	}

	
	// Trying using sprite data to generate collider. This needs the sprite to be the currently visible one
	// Also won't handle saving/loading or even room changes. Probably better as a flag for "use sprite collision" and it'll update on sprite change
	// Or, drag sprites in with ID to choose which to use for collider based on ID/Name
	public static void UpdateClickableCollider(GameObject clickable)
	{		
		SpriteRenderer renderer = clickable.GetComponentInChildren<SpriteRenderer>();
		PolygonCollider2D polygonCollider = clickable.GetComponentInChildren<PolygonCollider2D>();
		if( renderer == null || polygonCollider == null )
			return;

		Sprite sprite = renderer.sprite;
		if ( sprite == null )
			return;

		polygonCollider.pathCount = sprite.GetPhysicsShapeCount();
		
		List<Vector2> path = new List<Vector2>();		
		for (int i = 0; i < polygonCollider.pathCount; i++) 
		{
			path.Clear();
			sprite.GetPhysicsShape(i, path);
			polygonCollider.SetPath(i, path.ToArray());
		}
		// offset by powersprite offset
		PowerTools.PowerSprite powerSprite = renderer.GetComponent<PowerTools.PowerSprite>();		
		if ( powerSprite != null )
			polygonCollider.offset = powerSprite.Offset;
				
	}
	
}

#endregion

[CustomPropertyDrawer(typeof(ParallaxAttribute))]
public class ParallaxPropertyDrawer  : PropertyDrawer
{   
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property)+20;
	} 

	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		pos.height = EditorGUI.GetPropertyHeight(prop);
		EditorGUI.BeginProperty(pos, label, prop);		 
		Rect pos2 = EditorGUI.PrefixLabel (pos, GUIUtility.GetControlID (FocusType.Passive), new GUIContent("Depth", "Distant: 0 to 1. No Parallax: 1. Closer to camera: 1+"));
		EditorGUI.BeginChangeCheck ();					
		//float newVal = EditorGUI.Slider( pos, "Depth", -prop.floatValue+1, 0.0f, 2.0f);
		float newVal = EditorGUI.FloatField( pos, new GUIContent("Depth", "Distant: 0 to 1. No Parallax: 1. Closer to camera: 1+"), -prop.floatValue+1 );
		if ( EditorGUI.EndChangeCheck() )
		{
			prop.floatValue = -newVal+1;
		}				

		EditorGUI.EndProperty();
		
		pos.y = pos.y+18;
		pos.height = 20;
		if ( newVal < 0 )
			EditorGUI.HelpBox(pos,"Camera Angling (reverse movement)", MessageType.Warning);	
		else if ( newVal == 0 )
			EditorGUI.HelpBox(pos,"Full Distance (stationary)", MessageType.Error);	
		else if (newVal < 1 )
			EditorGUI.HelpBox(pos,"Further from camera (less movement)", MessageType.Info);	
		else if (  newVal == 1 )
			EditorGUI.HelpBox(pos,"No Parallax", MessageType.None);	
		else
			EditorGUI.HelpBox(pos,"Closer to camera (More movement)", MessageType.Info);	
		

	}
}

[CustomPropertyDrawer (typeof (MinMaxRange))]
public class MinMaxRangeDrawer :  PropertyDrawer
{
	// Scleas by passed in input factor- Really only used for mapping volume to easier to use values. May be removed later
	float Scaled(float input,float scaleFactor)
	{
		if ( scaleFactor == 1 )
			return input;
		return Mathf.Pow(input,scaleFactor);
	}
	
	float Unscaled(float input,float scaleFactor)
	{		
		if ( scaleFactor == 1 )
			return input;
		return Mathf.Pow(Mathf.Clamp01(input),1.0f/scaleFactor);
	}

	public override void OnGUI (Rect pos, SerializedProperty prop, GUIContent label) 
	{		
		EditorGUI.BeginProperty(pos, label, prop);
		
		SerializedProperty min = prop.FindPropertyRelative ("m_min");
		SerializedProperty max = prop.FindPropertyRelative ("m_max");
		SerializedProperty hasMax = prop.FindPropertyRelative("m_hasMax");
		SerializedProperty hasValue = prop.FindPropertyRelative("m_hasValue");
			
		float toWidth = 16;
		float maxLblWidth = 55;
		float buttonWidth = 20;
		
		float rectWidth = 0;	
		pos = EditorGUI.PrefixLabel (pos, GUIUtility.GetControlID (FocusType.Passive), label);
		const float minWidth = 160;
		if ( pos.width < minWidth )
		{
			pos.x = pos.x + (pos.width-minWidth);
			pos.width = pos.width-(pos.width-minWidth); 
		}

		float posX = pos.x;
		
		 // Don't make child fields be indented
		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
				
		if ( min != null && max != null && hasMax != null )
		{
			bool hasMaxVal = hasMax.boolValue;

			var ranges = (MinMaxRangeAttribute[])fieldInfo.GetCustomAttributes(typeof(MinMaxRangeAttribute), true);
			if (ranges.Length > 0)
			{
				float rangeMin = ranges[0].Min;
				float rangeMax = ranges[0].Max;				
				
				float scaleFactor = ranges[0].ScaleFactor;		

				const float rangeBoundsLabelWidth = 45f;
				
				float minValue = Unscaled(min.floatValue,scaleFactor);
				float maxValue = hasMaxVal ? Unscaled(max.floatValue,scaleFactor) : Unscaled(min.floatValue,scaleFactor);
				float oldMin = minValue;
				float oldMax = maxValue;

				if ( hasMaxVal )
				{
					var rangeBoundsLabel1Rect = new Rect(pos);
					rangeBoundsLabel1Rect.width = rangeBoundsLabelWidth;


					minValue = EditorGUI.DelayedFloatField(rangeBoundsLabel1Rect, minValue);
					pos.xMin += rangeBoundsLabelWidth + 5;

					var rangeBoundsLabel2Rect = new Rect(pos);
					rangeBoundsLabel2Rect.xMin = rangeBoundsLabel2Rect.xMax - rangeBoundsLabelWidth;
					maxValue = EditorGUI.DelayedFloatField(rangeBoundsLabel2Rect, maxValue);
					pos.xMax -= rangeBoundsLabelWidth + 5;

					EditorGUI.MinMaxSlider(pos, ref minValue, ref maxValue, rangeMin, rangeMax);	
				}
				else 
				{
					var rangeBoundsLabel1Rect = new Rect(pos);
					rangeBoundsLabel1Rect.width = rangeBoundsLabelWidth;
					
					bool addMax = GUI.Button(rangeBoundsLabel1Rect,"Range");//EditorGUI.Toggle(rangeBoundsLabel1Rect,false);
					pos.xMin += rangeBoundsLabelWidth + 5;

					var rangeBoundsLabel2Rect = new Rect(pos);
					rangeBoundsLabel2Rect.xMin = rangeBoundsLabel2Rect.xMax - rangeBoundsLabelWidth;

					if ( addMax )
					{
						if ( minValue >= rangeMax )
							minValue = minValue-0.1f;
						else
							maxValue = minValue+0.1f;
					}
					else 
					{
						minValue = EditorGUI.Slider(pos, minValue, rangeMin, rangeMax);
						maxValue=minValue;
					}

				
				}

				if ( minValue != oldMin || maxValue != oldMax )
				{
					min.floatValue = Scaled(minValue,scaleFactor);
					max.floatValue = Scaled(maxValue,scaleFactor);
					hasMaxVal = max.floatValue > min.floatValue;

					hasMax.boolValue = hasMaxVal;
					hasValue.boolValue = false;
				}
			}
			else 
			{

				
				if ( hasMaxVal == false )
				{
					rectWidth = pos.width-(maxLblWidth+4+buttonWidth);
				}
				else
				{
					rectWidth = (pos.width-(toWidth+2+buttonWidth+2))*0.5f;
				}
				
				EditorGUI.BeginChangeCheck ();			
				float minVal = EditorGUI.DelayedFloatField( new Rect(posX, pos.y,rectWidth, pos.height), min.floatValue);		
				
				if ( EditorGUI.EndChangeCheck() )
				{
					min.floatValue = minVal;
					if ( max.floatValue < minVal )
					{
						max.floatValue = minVal;
					}	
					hasValue.boolValue = false;
				}
				
				if ( hasMaxVal )
				{
					
					posX += rectWidth+2;
					
					EditorGUI.LabelField( new Rect(posX, pos.y,toWidth, pos.height), "to");
					posX += toWidth;
					
					EditorGUI.BeginChangeCheck ();			
					float maxVal = EditorGUI.DelayedFloatField( new Rect(posX, pos.y,rectWidth, pos.height), max.floatValue);
					if ( EditorGUI.EndChangeCheck() )
					{
						max.floatValue = maxVal;
						hasValue.boolValue = false;
					}
					posX += rectWidth+2;
				}
				else 
				{
					posX += rectWidth+4;
					EditorGUI.LabelField( new Rect(posX, pos.y,maxLblWidth, pos.height), "Is Range");
					posX += maxLblWidth;
				}
				
				// button
				EditorGUI.BeginChangeCheck ();		
				hasMaxVal = EditorGUI.Toggle(new Rect(posX, pos.y, buttonWidth, pos.height),hasMaxVal);			
				if ( EditorGUI.EndChangeCheck() )
				{
					hasMax.boolValue = hasMaxVal;
					hasValue.boolValue = false;
				}
			}
		}
		else
		{
			Debug.LogWarning("Min or max properties null");
		}
		
		EditorGUI.indentLevel = indent;
		
		EditorGUI.EndProperty();
	}
}

public static class EditorExtension
{
	public static int DrawBitMaskField (Rect aPosition, int aMask, System.Type aType, GUIContent aLabel)
	{
		var itemNames = System.Enum.GetNames(aType);
		var itemValues = System.Enum.GetValues(aType) as int[];
		
		int val = aMask;
		int maskVal = 0;
		for(int i = 0; i < itemValues.Length; i++)
		{
			if (itemValues[i] != 0)
			{
				if ((val & itemValues[i]) == itemValues[i])
					maskVal |= 1 << i;
			}
			else if (val == 0)
				maskVal |= 1 << i;
		}
		int newMaskVal = EditorGUI.MaskField(aPosition, aLabel, maskVal, itemNames);
		int changes = maskVal ^ newMaskVal;
		
		for(int i = 0; i < itemValues.Length; i++)
		{
			if ((changes & (1 << i)) != 0)            // has this list item changed?
			{
				if ((newMaskVal & (1 << i)) != 0)     // has it been set?
				{
					if (itemValues[i] == 0)           // special case: if "0" is set, just set the val to 0
					{
						val = 0;
						break;
					}
					else
						val |= itemValues[i];
				}
				else                                  // it has been reset
				{
					val &= ~itemValues[i];
				}
			}
		}
		return val;
	}
}

[CustomPropertyDrawer(typeof(BitMaskAttribute))]
public class EnumBitMaskPropertyDrawer : PropertyDrawer
{
	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		var typeAttr = attribute as BitMaskAttribute;
		// Add the actual int value behind the field name
		label.text = label.text + "("+prop.intValue+")";
		prop.intValue = EditorExtension.DrawBitMaskField(position, prop.intValue, typeAttr.propType, label);
	}
}

// Press Alt-Enter to add new line in text box in inspector
public class CreateNewLine : EditorWindow
{
	[MenuItem("Edit/Insert New Line &\r")]
	static void InsertNewLine()
	{
		EditorGUIUtility.systemCopyBuffer = System.Environment.NewLine;
		EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Paste"));
	}

}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
	public override float GetPropertyHeight(SerializedProperty property,
											GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	public override void OnGUI(Rect position,
							   SerializedProperty property,
							   GUIContent label)
	{
		GUI.enabled = false;
		EditorGUI.PropertyField(position, property, label, true);
		GUI.enabled = true;
	}
}

[CustomPropertyDrawer(typeof(Rect))]
public class RectPropertyDrawer : PropertyDrawer
{
	static readonly GUIContent[] labels1 = new GUIContent[]
	{
		new GUIContent("X"),
		new GUIContent("Y"),
		new GUIContent("W"),
		new GUIContent("H")
	};
	static readonly GUIContent[] labels2 = new GUIContent[]
	{		
		new GUIContent("L"),
		new GUIContent("R"),
		new GUIContent("B"),		
		new GUIContent("T")
	};

	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) {
		return base.GetPropertyHeight (prop, label) * 3;
	}

	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		Rect resultRect = prop.rectValue;

		float lineHeight = base.GetPropertyHeight (prop, label);

		Rect guiRect = position;
		guiRect.height = lineHeight;


		EditorGUI.BeginProperty(position, label, prop);

		EditorGUI.LabelField(guiRect, label);

		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		//guiRect = EditorGUI.IndentedRect(guiRect);	
		guiRect.y += lineHeight;
		guiRect.width = guiRect.width - 20;
		guiRect.x += 20;

		float[] values = new float[]{ resultRect.center.x, resultRect.center.y, resultRect.width, resultRect.height};

		EditorGUI.BeginChangeCheck();

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Center Pos/Size"),*/ labels1, values );

		Rect tempRect = resultRect;
		Vector2 rectCenter = resultRect.center;
		if ( tempRect.center.x != values[0] ) { rectCenter.x = values[0]; resultRect.center = rectCenter;}
		if ( tempRect.center.y != values[1] ) { rectCenter.y = values[1];	resultRect.center = rectCenter;}		
		if ( tempRect.width != values[2] ) { resultRect.width = values[2]; resultRect.center = rectCenter;}
		if ( tempRect.height != values[3] ) { resultRect.height = values[3]; resultRect.center = rectCenter;}

		guiRect.y += lineHeight;

		values = new float[]{ resultRect.xMin, resultRect.xMax, resultRect.yMax, resultRect.yMin };

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Bounds"),*/ labels2, values );

		if ( tempRect.xMin != values[0] ) resultRect.xMin = values[0];
		if ( tempRect.xMax != values[1] ) resultRect.xMax = values[1];
		if ( tempRect.yMax != values[2] ) resultRect.yMax = values[2];
		if ( tempRect.yMin != values[3] ) resultRect.yMin = values[3];



		EditorGUI.indentLevel = indent;

		//rect.center.y = rect.center.y;
		//rect.center.
		//EditorGUI.MultiFloatField( position, label, labels, values );
		//GUILayout.Label("blah");

		if ( EditorGUI.EndChangeCheck() )
		{
			prop.rectValue = resultRect;
		}

		EditorGUI.EndProperty();
	}
}


[CustomPropertyDrawer(typeof(RectCentered))]
public class RectCenteredPropertyDrawer : PropertyDrawer
{
	static readonly GUIContent[] labels1 = new GUIContent[]
	{
		new GUIContent("X"),
		new GUIContent("Y"),
		new GUIContent("W"),
		new GUIContent("H")
	};
	static readonly GUIContent[] labels2 = new GUIContent[]
	{		
		new GUIContent("L"),
		new GUIContent("R"),
		new GUIContent("B"),		
		new GUIContent("T")
	};

	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) {
		return base.GetPropertyHeight (prop, label) * 3;
	}

	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		SerializedProperty min = prop.FindPropertyRelative ("m_min");
		SerializedProperty max = prop.FindPropertyRelative ("m_max");
		RectCentered resultRect = new RectCentered( min.vector2Value, max.vector2Value );

		float lineHeight = base.GetPropertyHeight (prop, label);

		Rect guiRect = position;
		guiRect.height = lineHeight;

		EditorGUI.BeginProperty(position, label, prop);

		EditorGUI.LabelField(guiRect, label);

		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
	
		guiRect.y += lineHeight;
		guiRect.width = guiRect.width - 20;
		guiRect.x += 20;

		float[] values = new float[]{ resultRect.Center.x, resultRect.Center.y, resultRect.Width, resultRect.Height};

		EditorGUI.BeginChangeCheck();

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Center Pos/Size"),*/ labels1, values );

		RectCentered tempRect = new RectCentered(resultRect);
		Vector2 rectCenter = resultRect.Center;
		if ( tempRect.Center.x != values[0] ) { rectCenter.x = values[0]; resultRect.Center = rectCenter;}
		if ( tempRect.Center.y != values[1] ) { rectCenter.y = values[1];	resultRect.Center = rectCenter;}		
		if ( tempRect.Width != values[2] ) { resultRect.Width = values[2]; resultRect.Center = rectCenter;}
		if ( tempRect.Height != values[3] ) { resultRect.Height = values[3]; resultRect.Center = rectCenter;}


		guiRect.y += lineHeight;

		values = new float[]{ resultRect.MinX, resultRect.MaxX, resultRect.MinY, resultRect.MaxY };

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Bounds"),*/ labels2, values );

		if ( tempRect.MinX != values[0] ) resultRect.MinX = values[0];
		if ( tempRect.MaxX != values[1] ) resultRect.MaxX = values[1];
		if ( tempRect.MinY != values[2] ) resultRect.MinY = values[2];
		if ( tempRect.MaxY != values[3] ) resultRect.MaxY = values[3];

		EditorGUI.indentLevel = indent;

		if ( EditorGUI.EndChangeCheck() )
		{
			min.vector2Value = resultRect.Min;
			max.vector2Value = resultRect.Max;
		}

		EditorGUI.EndProperty();
	}
}

/*
	Weird idea for automatically adding references to something like this somewhere
		public Dictionary<System.Guid, Object> m_thingJonWontLike = null;

[CustomPropertyDrawer(typeof(ObjectReferenceAttribute ))]
public class ObjectReferenceDrawer : PropertyDrawer
{
	static int id = 0;
	public override void OnGUI(Rect position,
							   SerializedProperty property,
							   GUIContent label)
	{
		Object oldRefValue = property.objectReferenceValue;
		EditorGUI.PropertyField(position, property, label, true);
		if ( GUI.changed && oldRefValue != property.objectReferenceValue )
		{
			if ( property.objectReferenceValue != null )
			{
				Debug.Log( id.ToString() + " " + property.objectReferenceValue.ToString());
			}
			else 
			{
				Debug.Log( id.ToString() + " null");
			}
			id++;
			// TODO- Remove previous, and add new to list of referenced objects
		}

	}
}
*/
}

/**
	EditorLayouter. For laying out horizontal unity editor guis that require a 'rect'

	Usage:
		// init wiht gui's rect
		LayoutRect layout = new LayoutRect(rect);

		// Set up fields. Chained together like this...
		// This example has a 40px field, a little space, then a field that takes up 20% of spare space, another 20px field, and another that uses the remaining.
		layout.Fixed(40).Space.Variable(.2).Fixed(20).Stretched;

		// Use layout instead of rect for fields

		GUI.Button(layout,"My Button");
		GUI.Label(layout, "A label");
		GUI.Button(layout,"My next Button");
		etc

	Improvements: Could support 'minWidth' for variable fields pretty easily I guess...

*/
public class EditorLayouter
{
	enum eType { Fixed, Space, Variable };

	struct LayoutItem
	{
		public eType type;
		public float size;
	}
	
	int m_index = 0;	
	List<LayoutItem> m_items = new List<LayoutItem>();

	Rect m_rect = new Rect();
	Rect m_currRect = new Rect();

	public EditorLayouter(){}
	public EditorLayouter(Rect rect) 
	{
		m_rect = rect;
		m_currRect = m_rect;
		m_currRect.width = 0; // start with 0 width
	}


	// Implicitly cast to rect, then move to the next item rect
	public static implicit operator Rect(EditorLayouter self) 
	{
		return self.NextRect();
	}

	// Skip this element ( eg: if don't actually need the rect for it for it in the ui)
	public void Skip() {  NextRect(); }

	// Addes a fixed width item
	public EditorLayouter Fixed(float width)
	{
		m_items.Add(new LayoutItem(){type=eType.Fixed,size=width});
		return this;
	}

	// Adds a little space, sometimes necessary. You never use the Rect from this, its added between getting the rect for other items
	public EditorLayouter Space {get
	{
		m_items.Add(new LayoutItem(){type=eType.Space, size=2});
		return this;
	}}

	// Adds an item that stretches to fill space (multiple will divide up the space). Same as Variable(1);
	public EditorLayouter Stretched {get
	{
		m_items.Add(new LayoutItem(){type=eType.Variable,size=1});
			return this;
	}}
	// Adds an item that stretches, with a ratio for how much they should use (eg: you could have one that's 0.2 for 20% widdth, and one that's 0.8 for 80%... I dunno). A variable(2) will be twice as big as a variable(1)
	public EditorLayouter Variable(float ratio=1) 
	{	
		m_items.Add(new LayoutItem(){type=eType.Variable,size=ratio});
		return this;
	}
	
	// Adds an item that stretches to fit a gui label with passed in text (or minWidth, whichever is larger)
	public EditorLayouter Label(string text, float minWidth = 0) 
	{	
		if ( string.IsNullOrEmpty(text) )
		{ 
			m_items.Add(new LayoutItem(){type=eType.Fixed,size=2});
			return this;
		}
		float min,max;
		EditorStyles.label.CalcMinMaxWidth(new GUIContent(text),out min, out max);
		m_items.Add(new LayoutItem(){type=eType.Fixed,size=Mathf.Max(minWidth, max)});
		return this;
	}


	Rect NextRect()
	{
		// Offset from last item
		m_currRect.x += m_currRect.width;		

		// Skip spaces
		while ( m_index < m_items.Count && m_items[m_index].type == eType.Space )
		{	
			m_currRect.x += m_items[m_index].size; // space amount
			m_index++;
		}

		if ( m_index >= m_items.Count )
		{
			Debug.LogError("Tried to get too many items from Gui Layout!");
			return new Rect();
		}

		switch( m_items[m_index].type )
		{
			case eType.Fixed:
			{
				m_currRect.width = m_items[m_index].size;
			} break;
			case eType.Variable:
			{
				float ratio = m_items[m_index].size;
				float totalRatio = 0;
				float remainingWidth = m_rect.width;
				foreach( LayoutItem item in m_items )
				{
					if ( item.type == eType.Variable )
						totalRatio += item.size;
					else 
					remainingWidth -= item.size;
				}
				m_currRect.width = remainingWidth * (ratio / totalRatio);
			} break;
		}
		m_index++;
		
		return m_currRect; 
	}
		

}
