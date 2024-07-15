using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpritePixelCollider2D : MonoBehaviour
{
    public enum RegenerationMode { Manual, Automatic, Continuous }
    [Range(0, 1)]
    public float alphaCutoff = 0.5f;
    public RegenerationMode regenerationMode = RegenerationMode.Continuous;
	
	SpriteRenderer m_renderer = null;
	PolygonCollider2D m_collider = null;
	PowerTools.PowerSprite m_powerSprite = null;

	Sprite m_sprite = null;

    public void Start()
    {
	
		m_powerSprite = GetComponent<PowerTools.PowerSprite>();

        if (regenerationMode == RegenerationMode.Continuous || regenerationMode == RegenerationMode.Automatic)
        {
            Regenerate(true);
        }
    }
    private void Update()
    {
        if (regenerationMode == RegenerationMode.Continuous)
        {
            Regenerate();
        }
        else if (regenerationMode == RegenerationMode.Automatic)
        {

        }
    }
    public void Regenerate(bool warn = false)
    {

        alphaCutoff = Mathf.Clamp(alphaCutoff, 0, 1);

		if ( m_collider == null )
			m_collider = GetComponent<PolygonCollider2D>();
        if (m_collider == null)
            throw new Exception($"PixelCollider2D could not be regenerated because there is no PolygonCollider2D component on \"{gameObject.name}\".");

		if ( m_renderer == null )
			m_renderer = GetComponent<SpriteRenderer>();
        if (m_renderer == null)
        {
            m_collider.pathCount = 0;
            throw new Exception($"PixelCollider2D could not be regenerated because there is no SpriteRenderer component on \"{gameObject.name}\".");
        }

		if ( m_renderer.sprite == m_sprite || m_renderer.sprite == null )
			return;
		m_sprite = m_renderer.sprite; // cache sprite so don't regenete unless needed

        if (m_renderer.sprite.texture == null)
        {
            //m_collider.pathCount = 0;
            return;
        }
        if (m_renderer.sprite.texture.isReadable == false && warn)
        {
            //m_collider.pathCount = 0;
            //throw new Exception($"PixelCollider2D could not be regenerated because on \"{gameObject.name}\" because the sprite does not allow read/write operations.");
			Debug.LogWarning($"PixelCollider2D could not be regenerated because on \"{gameObject.name}\" because the sprite does not allow read/write operations. Enable this in the sprite's import settings");
			return;
        }

        List<List<Vector2Int>> Pixel_Paths = GetUnitPaths(m_renderer.sprite.texture, alphaCutoff);
        Pixel_Paths = SimplifyPathsPhase1(Pixel_Paths);
        Pixel_Paths = SimplifyPathsPhase2(Pixel_Paths);
        List<List<Vector2>> World_Paths = FinalizePaths(Pixel_Paths, m_renderer.sprite);
        m_collider.pathCount = World_Paths.Count;
        for (int p = 0; p < World_Paths.Count; p++)
        {
            m_collider.SetPath(p, World_Paths[p].ToArray());
        }
		
		// add sprite offset from powersprite
		if ( m_powerSprite != null )
			m_collider.offset = m_powerSprite.Offset;
				
    }
    private List<List<Vector2>> FinalizePaths(List<List<Vector2Int>> Pixel_Paths, Sprite sprite)
    {
        Vector2 pivot = sprite.pivot;
        pivot.x *= Mathf.Abs(sprite.bounds.max.x - sprite.bounds.min.x);
        pivot.x /= sprite.texture.width;
        pivot.y *= Mathf.Abs(sprite.bounds.max.y - sprite.bounds.min.y);
        pivot.y /= sprite.texture.height;

        List<List<Vector2>> Output = new List<List<Vector2>>();
        for (int p = 0; p < Pixel_Paths.Count; p++)
        {
            List<Vector2> Current_List = new List<Vector2>();
            for (int o = 0; o < Pixel_Paths[p].Count; o++)
            {
                Vector2 point = Pixel_Paths[p][o];
                point.x *= Mathf.Abs(sprite.bounds.max.x - sprite.bounds.min.x);
                point.x /= sprite.texture.width;
                point.y *= Mathf.Abs(sprite.bounds.max.y - sprite.bounds.min.y);
                point.y /= sprite.texture.height;
                point -= pivot;
                Current_List.Add(point);
            }
            Output.Add(Current_List);
        }
        return Output;
    }
    private static List<List<Vector2Int>> SimplifyPathsPhase1(List<List<Vector2Int>> Unit_Paths)
    {
        List<List<Vector2Int>> Output = new List<List<Vector2Int>>();
        while (Unit_Paths.Count > 0)
        {
            List<Vector2Int> Current_Path = new List<Vector2Int>(Unit_Paths[0]);
            Unit_Paths.RemoveAt(0);
            bool Keep_Looping = true;
            while (Keep_Looping)
            {
                Keep_Looping = false;
                for (int p = 0; p < Unit_Paths.Count; p++)
                {
                    if (Current_Path[Current_Path.Count - 1] == Unit_Paths[p][0])
                    {
                        Keep_Looping = true;
                        Current_Path.RemoveAt(Current_Path.Count - 1);
                        Current_Path.AddRange(Unit_Paths[p]);
                        Unit_Paths.RemoveAt(p);
                        p--;
                    }
                    else if (Current_Path[0] == Unit_Paths[p][Unit_Paths[p].Count - 1])
                    {
                        Keep_Looping = true;
                        Current_Path.RemoveAt(0);
                        Current_Path.InsertRange(0, Unit_Paths[p]);
                        Unit_Paths.RemoveAt(p);
                        p--;
                    }
                    else
                    {
                        List<Vector2Int> Flipped_Path = new List<Vector2Int>(Unit_Paths[p]);
                        Flipped_Path.Reverse();
                        if (Current_Path[Current_Path.Count - 1] == Flipped_Path[0])
                        {
                            Keep_Looping = true;
                            Current_Path.RemoveAt(Current_Path.Count - 1);
                            Current_Path.AddRange(Flipped_Path);
                            Unit_Paths.RemoveAt(p);
                            p--;
                        }
                        else if (Current_Path[0] == Flipped_Path[Flipped_Path.Count - 1])
                        {
                            Keep_Looping = true;
                            Current_Path.RemoveAt(0);
                            Current_Path.InsertRange(0, Flipped_Path);
                            Unit_Paths.RemoveAt(p);
                            p--;
                        }
                    }
                }
            }
            Output.Add(Current_Path);
        }
        return Output;
    }
    private static List<List<Vector2Int>> SimplifyPathsPhase2(List<List<Vector2Int>> Input_Paths)
    {
        for (int pa = 0; pa < Input_Paths.Count; pa++)
        {
            for (int po = 0; po < Input_Paths[pa].Count; po++)
            {
                Vector2Int Start = new Vector2Int();
                if (po == 0)
                {
                    Start = Input_Paths[pa][Input_Paths[pa].Count - 1];
                }
                else
                {
                    Start = Input_Paths[pa][po - 1];
                }
                Vector2Int End = new Vector2Int();
                if (po == Input_Paths[pa].Count - 1)
                {
                    End = Input_Paths[pa][0];
                }
                else
                {
                    End = Input_Paths[pa][po + 1];
                }
                Vector2Int Current_Point = Input_Paths[pa][po];
                Vector2 Direction1 = Current_Point - (Vector2)Start;
                Direction1 /= Direction1.magnitude;
                Vector2 Direction2 = End - (Vector2)Start;
                Direction2 /= Direction2.magnitude;
                if (Direction1 == Direction2)
                {
                    Input_Paths[pa].RemoveAt(po);
                    po--;
                }
            }
        }
        return Input_Paths;
    }
    private static List<List<Vector2Int>> GetUnitPaths(Texture2D texture, float alphaCutoff)
    {
        List<List<Vector2Int>> Output = new List<List<Vector2Int>>();
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                if (PixelIsSolid(texture, new Vector2Int(x, y), alphaCutoff))
                {
                    if (!PixelIsSolid(texture, new Vector2Int(x, y + 1), alphaCutoff))
                    {
                        Output.Add(new List<Vector2Int>() { new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1) });
                    }
                    if (!PixelIsSolid(texture, new Vector2Int(x, y - 1), alphaCutoff))
                    {
                        Output.Add(new List<Vector2Int>() { new Vector2Int(x, y), new Vector2Int(x + 1, y) });
                    }
                    if (!PixelIsSolid(texture, new Vector2Int(x + 1, y), alphaCutoff))
                    {
                        Output.Add(new List<Vector2Int>() { new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1) });
                    }
                    if (!PixelIsSolid(texture, new Vector2Int(x - 1, y), alphaCutoff))
                    {
                        Output.Add(new List<Vector2Int>() { new Vector2Int(x, y), new Vector2Int(x, y + 1) });
                    }
                }
            }
        }
        return Output;
    }
    private static bool PixelIsSolid(Texture2D texture, Vector2Int point, float alphaCutoff)
    {
        if (point.x < 0 || point.y < 0 || point.x >= texture.width || point.y >= texture.height)
        {
            return false;
        }
        float pixelAlpha = texture.GetPixel(point.x, point.y).a;
        if (alphaCutoff == 0)
        {
            if (pixelAlpha != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (alphaCutoff == 1)
        {
            if (pixelAlpha == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return pixelAlpha >= alphaCutoff;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpritePixelCollider2D))]
public class SpritePixelColider2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SpritePixelCollider2D PC2D = (SpritePixelCollider2D)target;
        if (GUILayout.Button("Regenerate Collider"))
        {
            PC2D.Regenerate();
        }
    }
}
#endif
