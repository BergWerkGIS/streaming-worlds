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


	public void Initialize(
		MapboxAccess mbxAccess
		, CanonicalTileId tileId
		, float unityTileScale
		, UnwrappedTileId centerTile
		, Vector2d shift
		, float factor
	)
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

		//position the tile relative to the center tile of the current viewport using the tile id
		//multiply by tile size Unity units (unityTileScale)
		//shift by distance of current viewport center to center of center tile
		Vector3 position = new Vector3(
			(tileId.X - centerTile.X) * unityTileScale - (float)shift.x / factor
			, 0
			, (centerTile.Y - tileId.Y) * unityTileScale - (float)shift.y / factor
		);

		transform.localPosition = position;

		gameObject.name = name;
		gameObject.SetActive(true);

		TileResource tileResource = TileResource.MakeRaster(tileId, null);

		mbxAccess.Request(
			tileResource.GetUrl(),
			(Response r) =>
			{
				try
				{
					if (r.HasError)
					{
						Debug.LogErrorFormat("response, hasError:{0} exceptions:{1}", r.HasError, r.ExceptionsAsString);
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
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			},
			30,
			tileId,
			"my-map-id"
		);
	}


	public void SetActive(bool active) { gameObject.SetActive(active); }



}
