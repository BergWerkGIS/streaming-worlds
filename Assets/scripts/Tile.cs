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


	public void Initialize(MapboxAccess mbxAccess, CanonicalTileId tileId, float unityTileScale)
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
		goTxt.transform.localScale = new Vector3(0.2f, 0.2f);
		TextMesh text = goTxt.AddComponent<TextMesh>();
		text.transform.SetPositionAndRotation(
			text.transform.position
			, rotation
		);
		text.anchor = TextAnchor.MiddleCenter;
		text.color = Color.red;
		//text.text = name;
		text.text = tileId.Z.ToString();


		Vector3 unityScale = new Vector3(unityTileScale, 1, unityTileScale);
		transform.localScale = unityScale;

		int maxTileCount = (int)Math.Pow(2, tileId.Z);
		int shift = maxTileCount / 2;


		Vector3 position;
		if (tileId.Z == 0)
		{
			position = new Vector3(0, 0, 0);
		}
		else
		{
			position = new Vector3(
				(tileId.X - shift) * unityScale.x + (unityScale.x / 2)
				, 0
				, (maxTileCount - tileId.Y - (shift + 1)) * unityScale.x + (unityScale.x / 2)
			);
		}

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
