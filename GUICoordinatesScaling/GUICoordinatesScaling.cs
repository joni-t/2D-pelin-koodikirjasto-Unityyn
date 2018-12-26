using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*versiohistoria
 * versio: 0.1
 * ensimmäinen
 * 
 * TÄMÄ:
 * versio: 0.1.1
 * pvm: 21.5.2014
 * Muutos: Lisätty tuki usealle piirtofunktiolle.
 * UUSIN
 */

//huolehtii GUI-elementtien ja koordinaattien skaalaamisesta näytön eri resoluutioille
namespace CoordinatesScaling {
	public class GUICoordinatesScaling {
		private  static GUICoordinatesScaling instance; // Singleton
		//  Instance  
		public static GUICoordinatesScaling Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new GUICoordinatesScaling();     
				return instance;   
			}
		}
		
		private GUICoordinatesScaling() {Initialize();}

		public float originalWidth; // define here the original resolution
		public float originalHeight; // you used to create the GUI contents
		private Vector3 scale; //lasketaan skaalauskerroin kohderesoluutiolle
		private Matrix4x4 svMat; //muunnosmatriisi
		public delegate void Drawing(); //piirtää gui-elementit
		public List<Drawing> drawing_functions;

		public void AsetaParametrit(float p_originalWidth, float p_originalHeight) {
			originalWidth=p_originalWidth;
			originalHeight=p_originalHeight;
			SetScaleFactors();
			scale.z = 1;
		}

		//siivoaa rojut pois ja tekee re-initialisoinnin
		//voidaan määrätä, siivotaanko myös ohjaus (vain ensimmäisen ohjattavan täytyy)
		public void Clean() {
			drawing_functions.Clear();
			Initialize();
		}
		
		public void Initialize() {
			drawing_functions=new List<Drawing>();
		}

		//asettaa (rekisteröi, jos puuttuu) funktion listaan, joka käynnistää gui-elementtien piirtämisen
		public void RegisterDrawingFunction(Drawing p_drawing_function) {
			if(!drawing_functions.Contains(p_drawing_function))
				drawing_functions.Add(p_drawing_function);
		}

		public void SetScaleFactors() {
			scale.x = Screen.width/originalWidth; // calculate hor scale
			scale.y = Screen.height/originalHeight; // calculate vert scale
		}

		//päivittää ensin skaalauskertoimet ja piirtää sitten (kutsuu piirtofunktiota)
		public void Scaling_and_Drawing() {
			SetScaleFactors();
			svMat = GUI.matrix; // save current matrix
			// substitute matrix - only scale is altered from standard
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
			// draw your GUI controls now
			drawing_functions.ForEach(delegate(Drawing obj) {
				obj();
			});
			// restore matrix before returning
			GUI.matrix = svMat; // restore matrix
		}

		//huolehdi että scalefactorit on päivitetty (resoluutio on saatettu vaihtaa)
		public Vector2 Scaling_point(Vector2 point) {
			if((scale.x>0) && (scale.y>0))
				return new Vector2(point.x*scale.x, point.y*scale.y);
			else
				return point;
		}
		//huolehdi että scalefactorit on päivitetty (resoluutio on saatettu vaihtaa)
		public Rect Scaling_rect(Rect rect) {
			Vector2 point=Scaling_point(new Vector2(rect.x, rect.y));
			Vector2 length=Scaling_point(new Vector2(rect.width, rect.height));
			return new Rect(point.x, point.y, length.x, length.y);
		}
	}
}
