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



	public int Zoom { get { return _zoom; } }


	private int _zoom;


	public void Initialize(int z, int x, int y)
	{
		_zoom = z;
		string name = string.Format("{0}/{1}/{2}", z, x, y);
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
		text.text = z.ToString();


		Vector2dBounds bb = Conversions.TileIdToBounds(x, y, z);
		//float metersPerTilePixel = Conversions.GetTileScaleInMeters((float)bb.Center.y, z);
		//float metersPerTilePixel = Conversions.GetTileScaleInMeters((float)bb.South, z);
		float metersPerPixel = Conversions.GetTileScaleInMeters(z);
		double scaleDownFactor = 100d;
		float metersPerTile = (float)((metersPerPixel * 256d) / scaleDownFactor);

		//Vector3 unityScale = new Vector3(metersPerTile, 1, metersPerTile);
		//HACK use metersPerPixel as 'scale' to stay within Unity's float limits
		Vector3 unityScale = new Vector3(metersPerPixel, 1, metersPerPixel);

		transform.localScale = unityScale;
		Vector2d centerLatLng = Conversions.TileIdToCenterLatitudeLongitude(x, y, z);
		Vector2d centerWebMerc = Conversions.LatLonToMeters(bb.Center);

		string logMsg = string.Format(
			"{1}{0}bbox:{2}{0}bb.center:{3}{0}centerLatLng:{4}{0}centerWebMerc:{5}{0}m/pix:{6}{0}m/tile:{7}"
			, Environment.NewLine
			, string.Format("{0}/{1}/{2}", z, x, y)
			, bb
			, bb.Center
			, centerLatLng
			, centerWebMerc
			, metersPerPixel
			, metersPerTile
		);
		//Debug.Log(logMsg);

		Vector3 position = new Vector3(
			(float)(centerWebMerc.x / 256d) // divide by 256 as we are using metersPerPixel for scale
			, 0
			, (float)(centerWebMerc.y / 256d) // divide by 256 as we are using metersPerPixel for scale
		);

		transform.localPosition = position;

		gameObject.name = name;
		gameObject.SetActive(true);

	}


	public void SetActive(bool active) { gameObject.SetActive(active); }



}
