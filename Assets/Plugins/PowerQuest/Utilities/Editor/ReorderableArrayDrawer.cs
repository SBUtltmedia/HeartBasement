using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PowerTools.Quest
{

[CustomPropertyDrawer(typeof(ReorderableArrayAttribute))]
public class ReorderableArrayDrawer : PropertyDrawer {
    
    // Private methods 
    //--------------------------------

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string arrayName = property.propertyPath.Split('.')[0];
        SerializedProperty arrayProperty = property.serializedObject.FindProperty(arrayName);
            
        float buttonWidth = 18.0f;
        float buttonHeight = 15.0f;

        //string path = property.propertyPath;
        Rect objectFieldRect = position;
        objectFieldRect.xMax = position.width - buttonWidth * 4.0f;

        EditorGUI.PropertyField(objectFieldRect, property, true);

        Rect buttonUpRect = position;
        buttonUpRect.xMin = position.width - buttonWidth * 4.0f;
        buttonUpRect.xMax = position.width - buttonWidth * 3.0f;
        buttonUpRect.yMax = position.yMin + buttonHeight;
		if (GUI.Button(buttonUpRect, "\u25B2", EditorStyles.miniButtonLeft))
        {
            int srcIndex = FindIndex(arrayProperty, property);
            int dstIndex = Mathf.Max(srcIndex - 1, 0);

            if (srcIndex != dstIndex)
            {
                arrayProperty.MoveArrayElement(srcIndex, dstIndex);
            }
        }

        Rect buttonDownRect = position;
        buttonDownRect.xMin = position.width - buttonWidth * 3.0f;
        buttonDownRect.xMax = position.width - buttonWidth * 2.0f;
        buttonDownRect.yMax = position.yMin + buttonHeight;
		if (GUI.Button(buttonDownRect, "\u25BC", EditorStyles.miniButtonMid))
        {           
            int srcIndex = FindIndex(arrayProperty, property);
            int dstIndex = Mathf.Min(srcIndex + 1, arrayProperty.arraySize - 1);

            if (srcIndex != dstIndex)
            {
                arrayProperty.MoveArrayElement(srcIndex, dstIndex);
            }
        }

        Rect buttonInsertRect = position;
        buttonInsertRect.xMin = position.width - buttonWidth * 2.0f;
        buttonInsertRect.xMax = position.width - buttonWidth * 1.0f;
        buttonInsertRect.yMax = position.yMin + buttonHeight;
        if (GUI.Button(buttonInsertRect, "+", EditorStyles.miniButtonMid))
        {           
            int srcIndex = FindIndex(arrayProperty, property);
            arrayProperty.InsertArrayElementAtIndex(srcIndex);
        }

        Rect buttonDeleteRect = position;
        buttonDeleteRect.xMin = position.width - buttonWidth * 1.0f;
        buttonDeleteRect.xMax = position.width;
        buttonDeleteRect.yMax = position.yMin + buttonHeight;
        if (GUI.Button(buttonDeleteRect, "X", EditorStyles.miniButtonRight))
        {           
            int srcIndex = FindIndex(arrayProperty, property);            
            arrayProperty.DeleteArrayElementAtIndex(srcIndex);            
        }
    }

    int FindIndex(SerializedProperty arrayProperty, SerializedProperty property)
    {
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty p = arrayProperty.GetArrayElementAtIndex(i);
            if (SerializedProperty.EqualContents(p, property))
            {
                return i;
            }
        }

        return -1;
    }

}

}