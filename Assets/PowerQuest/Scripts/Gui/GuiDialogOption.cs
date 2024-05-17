using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PowerTools.Quest
{


public class GuiDialogOption : MonoBehaviour 
{
	public IQuestClickable Clickable {get;set;}
	public DialogOption Option {get;set;}
}

}