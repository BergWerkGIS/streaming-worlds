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


	public void Initialize(CanonicalTileId tileId, MapboxAccess mbxAccess)
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


		Vector2dBounds bb = Conversions.TileIdToBounds(tileId.X, tileId.Y, tileId.Z);
		//float metersPerTilePixel = Conversions.GetTileScaleInMeters((float)bb.Center.y, z);
		//float metersPerTilePixel = Conversions.GetTileScaleInMeters((float)bb.South, z);
		float metersPerPixel = 0;
		try
		{
			metersPerPixel = Conversions.GetTileScaleInMeters(tileId.Z);
		}
		catch (Exception ex)
		{
			Debug.LogWarning(tileId);
			Debug.LogWarning(ex);
			return;
		}

		//double scaleDownFactor = 100d;
		//float metersPerTile = (float)((metersPerPixel * 256d) / scaleDownFactor);

		//Vector3 unityScale = new Vector3(metersPerTile, 1, metersPerTile);
		//HACK use metersPerPixel as 'scale' to stay within Unity's float limits
		Vector3 unityScale = new Vector3(metersPerPixel, 1, metersPerPixel);

		transform.localScale = unityScale;
		//Vector2d centerLatLng = Conversions.TileIdToCenterLatitudeLongitude(x, y, z);
		Debug.LogFormat("bbox:{0}", bb);
		Vector2d wmSW = Conversions.LatLonToMeters(bb.SouthWest);
		Vector2d wmNE = Conversions.LatLonToMeters(bb.NorthEast);
		Debug.LogFormat("sw:{0}   ne:{1}", wmSW, wmNE);
		Debug.LogFormat("center, lng:{0} lat:{1}", bb.Center.x, bb.Center.y);
		Vector2d centerWebMerc = Conversions.LatLonToMeters(bb.Center);
		Debug.LogFormat("centerWebMerc, x:{0} y:{1} /256: {2}/{3}", centerWebMerc.x, centerWebMerc.y, centerWebMerc.x / 256, centerWebMerc.y / 256);



		//string logMsg = string.Format(
		//	"{1}{0}bbox:{2}{0}bb.center:{3}{0}centerLatLng:{4}{0}centerWebMerc:{5}{0}m/pix:{6}{0}m/tile:{7}"
		//	, Environment.NewLine
		//	, string.Format("{0}/{1}/{2}", z, x, y)
		//	, bb
		//	, bb.Center
		//	, centerLatLng
		//	, centerWebMerc
		//	, metersPerPixel
		//	, metersPerTile
		//);
		//Debug.Log(logMsg);

		Vector3 position = new Vector3(
			(float)(centerWebMerc.x / 256d) // divide by 256 as we are using metersPerPixel for scale
			, 0
			, (float)(centerWebMerc.y / 256d) // divide by 256 as we are using metersPerPixel for scale
		);

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
