using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

[CustomEditor(typeof(TextureExporter))]
public class TextureExporterCustomEditor:Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector ();
		TextureExporter script = (TextureExporter)target;
		if (GUILayout.Button ("Export")) 
		{
			TextureExporter.Export(script.filename, (Texture2D)script.curMaterial.GetComponent<MeshRenderer>().material.mainTexture);
			Debug.Log ("Exported to: "+script.filename);
		}
	}
}

public class TextureExporter : MonoBehaviour 
{
	public GameObject curMaterial;
	public string filename = @"I:\uSource/test/textures/test.png";

    public static Texture2D Decompress(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;

        Texture2D readableTexture = new Texture2D(source.width, source.height);
        readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);

        return readableTexture;
    }

    public static void Export(string path, Texture2D texture)
	{
		File.WriteAllBytes(path, texture.EncodeToPNG ());
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
