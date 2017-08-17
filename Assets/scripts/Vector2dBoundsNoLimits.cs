using System.Collections;
using System.Collections.Generic;

namespace Mapbox.Utils
{


	public class Vector2dBoundsNoLimitsX
	{
		public Vector2d SouthWest;
		public Vector2d NorthEast;


		public Vector2dBoundsNoLimitsX(Vector2d sw, Vector2d ne)
		{
			SouthWest = sw;
			NorthEast = ne;
		}

		public Vector2d Center
		{
			get
			{
				double lng = (SouthWest.x + NorthEast.x) / 2d;
				double lat = (SouthWest.y + NorthEast.y) / 2d;

				return new Vector2d(lng, lat);
			}
		}

		public Vector2dBounds ToVector2dBounds()
		{
			return new Vector2dBounds(SouthWest, NorthEast);
		}


		public override string ToString()
		{
			return string.Format("{0},{1}", SouthWest, NorthEast);
		}


	}



}