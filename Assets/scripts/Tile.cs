using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{

	void Start() { }
	void Update() { }

	private void OnDestroy()
	{
		if (null != _texture)
		{
			Destroy(_texture);
			_texture = null;
		}
	}


	private Texture2D _texture;


	public void Initialize(MapboxAccess mbxAccess, CanonicalTileId tileId, float unityTileScale, Vector2d center)
	{
		string name = tileId.ToString();
		Quaternion rotation = new Quaternion(0.7f, 0, 0, 0.7f);


		GameObject tileRepresentation = GameObject.CreatePrimitive(PrimitiveType.Quad);
		tileRepresentation.transform.parent = transform;
		tileRepresentation.name = "tile-data";
		tileRepresentation.transform.SetPositionAndRotation(
			tileRepresentation.transform.position
			, rotation
		);


		GameObject goTxt = new GameObject("text");
		goTxt.transform.parent = transform;
		goTxt.transform.localScale = new Vector3(0.15f, 0.15f);
		TextMesh text = goTxt.AddComponent<TextMesh>();
		text.transform.SetPositionAndRotation(
			text.transform.position
			, rotation
		);
		text.anchor = TextAnchor.MiddleCenter;
		text.alignment = TextAlignment.Center;
		text.fontStyle = FontStyle.Bold;
		text.color = Color.red;
		text.text = name.Replace("/", Environment.NewLine);
		//text.text = tileId.Z.ToString();


		Vector3 unityScale = new Vector3(unityTileScale, 1, unityTileScale);
		transform.localScale = unityScale;

		//number of tiles along the world's edge at current zoomlevel
		int maxTileCount = (int)Math.Pow(2, tileId.Z);
		int shift = maxTileCount / 2;


		Vector3 position;
		// shift origin from slippy map tile id system to unity coordinate system:
		// slippy map tile ids: start from top left and increase to the right and to the bottom
		// unity coordinate system: cartesian "starting in the middle (0/0)" going in all 4 directions
		if (tileId.Z == 0)
		{
			position = new Vector3(0 - (float)center.x, 0, 0 - (float)center.y);
		}
		else
		{
			// x: * shift to west by half of all x tiles at that zoom
			//    * multiply by tile scale in Unity units
			//    * add another half tile scale to get center 
			// z: * reverse order from top -> bottom to bottom -> top
			//    * shift to north by half of all y tiles at that zoom, plus 1
			//    * multiply by tile scale in Unity units
			//    * add another half tile scale to get center 
			position = new Vector3(
				((tileId.X - shift) * unityTileScale + (unityTileScale / 2)) - (float)center.x
				, 0
				, ((maxTileCount - tileId.Y - (shift + 1)) * unityTileScale + (unityTileScale / 2)) - (float)center.y
			);
		}
		Debug.LogFormat("{0}: position[unity units]:{1}/{2}", tileId, position.x, position.z);

		transform.localPosition = position;

		gameObject.name = name;
		gameObject.SetActive(true);

		TileResource tileResource = TileResource.MakeRaster(tileId, null);

		mbxAccess.Request(
			tileResource.GetUrl(),
			(Response r) =>
			{

				if (r.HasError)
				{
					Debug.LogFormat("response, hasError:{0} exceptions:{1}", r.HasError, r.ExceptionsAsString);
					return;
				}
				//
				if (null == tileRepresentation) { return; }
				MeshRenderer mr = tileRepresentation.GetComponent<MeshRenderer>();
				if (null == mr) { return; }
				_texture = new Texture2D(0, 0, TextureFormat.RGB24, true);
				_texture.wrapMode = TextureWrapMode.Clamp;
				_texture.LoadImage(r.Data);
				mr.material.mainTexture = _texture;
				//mr.material.shader = Shader.Find("Unlit/Transparent");
			},
			30,
			tileId,
			"my-map-id"
		);
	}


	public void SetActive(bool active) { gameObject.SetActive(active); }



}
