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


	private TextMesh _text;
	//private GameObject _plane;
	//private MeshRenderer _meshRenderer;
	private int _zoom;


	public void Initialize(int z, int x, int y)
	{
		_zoom = z;
		string name = string.Format("{0}/{1}/{2}", z, x, y);

		Quaternion rotation = new Quaternion(0.7f, 0, 0, 0.7f);

		GameObject tileRepresentation = transform.Find("tile-data").gameObject;
		tileRepresentation.transform.SetPositionAndRotation(
			tileRepresentation.transform.position
			, rotation
		);

		_text = transform.Find("text").GetComponent<TextMesh>();
		_text.transform.SetPositionAndRotation(
			_text.transform.position
			, rotation
		);
		_text.anchor = TextAnchor.MiddleCenter;
		_text.color = Color.red;
		_text.text = name;

		gameObject.name = name;
		gameObject.SetActive(true);


		Vector2dBounds bb = Conversions.TileIdToBounds(x, y, z);
		float metersPerTilePixel = Conversions.GetTileScaleInMeters((float)bb.Center.y, z);
		float metersPerTile = metersPerTilePixel * 256f/10f;
		gameObject.transform.localScale = new Vector3(metersPerTilePixel, 1, metersPerTilePixel);
		Vector2d centerLatLng = Conversions.TileIdToCenterLatitudeLongitude(x, y, z);
		Vector2d centerWebMerc = Conversions.LatLonToMeters(bb.Center);

		string logMsg = string.Format(
			"bbox:{1}{0}bb.center:{2}{0}centerLatLng:{3}{0}centerWebMerc:{4}{0}m/pix:{5}{0}m/tile:{6}"
			, Environment.NewLine
			, bb
			, bb.Center
			, centerLatLng
			, centerWebMerc
			, metersPerTilePixel
			, metersPerTile
		);
		Debug.Log(logMsg);

		Vector3 position = new Vector3(
			(float)(centerWebMerc.x / 256d)
			, 0
			, (float)(centerWebMerc.y / 256d)
		);

		transform.localPosition = position;
	}


	public void SetActive(bool active) { gameObject.SetActive(active); }



}
