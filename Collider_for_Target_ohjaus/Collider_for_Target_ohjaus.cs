using UnityEngine;
using System.Collections;
using Ohjaus_2D;
using Target_ohjaus;


// ************************** TÄTÄ EI ENÄÄ TARVITA ****************************************
//Tämä on siirretty Liita_Prefab_Target_ohjaukseen.cs scriptiin


//tämä mokkula liitetään jokaiselle prefabille, joka on liitetty Target_ohjaukseen ja Target_ohjaus haluaa ottaa vastaan törmäystapahtumat
//tämä havaitsee törmäystapahtumat ja lähettää tiedot edelleen Target_ohjaukselle
namespace Collider_for_Target_ohjaus {
	public class Collider_for_Target_ohjaus : MonoBehaviour {
		//events
		public const int int_OnCollisionEnter=0;
		public const int int_OnCollisionExit=1;
		public const int int_OnCollisionStay=2;
		public const int int_OnTriggerEnter=3;
		public const int int_OnTriggerExit=4;
		public const int int_OnTriggerStay=5;

		void OnCollisionEnter (Collision other)
		{
			Target_ohjaus.Target_ohjaus.Instance.GetCollisions(gameObject, int_OnCollisionEnter, ref other);
		}

		void OnCollisionExit (Collision other)
		{
			Target_ohjaus.Target_ohjaus.Instance.GetCollisions(gameObject, int_OnCollisionExit, ref other);
		}

		void OnCollisionStay (Collision other)
		{
			Target_ohjaus.Target_ohjaus.Instance.GetCollisions(gameObject, int_OnCollisionStay, ref other);
		}

		void OnTriggerEnter (Collider other)
		{
			Target_ohjaus.Target_ohjaus.Instance.GetTriggers(gameObject, int_OnTriggerEnter, other);
		}

		void OnTriggerExit (Collider other)
		{
			Target_ohjaus.Target_ohjaus.Instance.GetTriggers(gameObject, int_OnTriggerExit, other);
		}

		void OnTriggerStay (Collider other)
		{
			Target_ohjaus.Target_ohjaus.Instance.GetTriggers(gameObject, int_OnTriggerStay, other);
		}
	}
}
