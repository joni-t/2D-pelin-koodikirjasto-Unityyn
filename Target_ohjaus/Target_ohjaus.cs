using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Ohjaus_2D;

namespace Target_ohjaus {
	using Target_ohjaus_Level_hallintaan;

	//Target_ohjaus Singletonina
	//huolehtii gameobjectien luomisesta kosketuksesta ja myös spawnerilla
	//huolehtii myös gameobjectien törmäysten hallinnasta, mutta käsittely annetaan hallintaoliolle
	//sisältää rajapinnan Ohjaus_2D:hen, jonka kautta saadaan ohjaustoiminnot (kosketukset)
	public class Target_ohjaus : Ohjattava_2D {
		private  static Target_ohjaus instance; // Singleton
		//  Instance  
		public static Target_ohjaus Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new Target_ohjaus();     
				return instance;   
			}
		}
		
		//Target_ohjauksen hallitsemat olio-ryhmät
		public List<Prefab_and_instances> gameObject_Prefabs;
		public List<Prefab_and_instances> gameObject_Prefabs_before_cleaning; //tähän lisätään prefabit rakennusvaiheessa ennen clean:ä ja kopioidaan sen jälkeen varsinaiseen listaan
		public List<GameObject_Spawner> gameObject_Spawners;
		public List<GameObject_Spawner.Spawner_Timerin_Kayttaja> spawner_timerin_kayttajat;
		public List<Collisions_and_Triggers_Handling> collisions_and_Triggers_Handlers;
		public Rect public_allowed_area; //sallittu alue kaikille prefabeille, jos ei erikseen annettu -> jos menee alueen ulkopuolelle, peliobjekti poistetaan (moving-olio huolehtii tästä)
		public bool use_allowed_area; //voidaan asettaa pois
		public SmoothCamera smooth_camera=null; //liikuttaa kameraa seuraten sille asetettua targettia
		public bool clean_suoritettu=false; //käynnistysvaiheessa (awake) onko jo suoritettu
		//collision-events
		public const int int_OnCollisionEnter=0;
		public const int int_OnCollisionExit=1;
		public const int int_OnCollisionStay=2;
		public const int int_OnTriggerEnter=3;
		public const int int_OnTriggerExit=4;
		public const int int_OnTriggerStay=5;
		public const int int_OnControllerColliderHit=6;
		
		private Target_ohjaus() {
			Initialize();
			clean_suoritettu=false;
		}

		//siivoaa rojut pois ja tekee re-initialisoinnin
		//voidaan määrätä, siivotaanko myös ohjaus (vain ensimmäisen ohjattavan täytyy)
		public void Clean(bool clean_ohjaus) {
			Clean_parent(clean_ohjaus);
			gameObject_Prefabs.ForEach(delegate(Prefab_and_instances obj) {
				obj.modified_GameObjects.Clear();
				obj.target_prefabs.Clear();
			});
			gameObject_Prefabs.Clear();
			gameObject_Spawners.ForEach(delegate(GameObject_Spawner obj) {
				obj.prefabs_to_spawn.Clear();
			});
			gameObject_Spawners.Clear();
			spawner_timerin_kayttajat.Clear();
			collisions_and_Triggers_Handlers.ForEach(delegate(Collisions_and_Triggers_Handling obj) {
				obj.col_events.Clear();
				obj.list_other_prefab_and_instances.Clear();
				obj.list_prefab_and_instances.Clear();
			});
			collisions_and_Triggers_Handlers.Clear();
			Initialize();
		}

		public void Initialize() {
			gameObject_Prefabs=new List<Prefab_and_instances>();
			if(gameObject_Prefabs_before_cleaning!=null) {
				gameObject_Prefabs.AddRange(gameObject_Prefabs_before_cleaning); //siirretään ennen clean:ä lisätyt
				gameObject_Prefabs_before_cleaning.Clear();
			}
			clean_suoritettu=true; //nyt lisätään vain varsinaiseen listaan
			gameObject_Prefabs_before_cleaning=new List<Prefab_and_instances>();
			gameObject_Spawners=new List<GameObject_Spawner>();
			spawner_timerin_kayttajat=new List<GameObject_Spawner.Spawner_Timerin_Kayttaja>();
			collisions_and_Triggers_Handlers=new List<Collisions_and_Triggers_Handling>();
		}

		//asettaa kameran seuraamaan targettia
		public void AsetaSmoothCamera(SmoothCamera p_smooth_camera) {
			smooth_camera=p_smooth_camera;
		}
		
		//listaa prefabit tagin perusteella
		public List<Prefab_and_instances> Get_Prefabs_with_Tag(string tag) {
			if(tag==null) return null;
			List<Prefab_and_instances> lista=new List<Prefab_and_instances>();
			gameObject_Prefabs.ForEach(delegate(Prefab_and_instances prefab_and_instances) {
				if (prefab_and_instances.thePrefab.tag.CompareTo(tag)==0) lista.Add(prefab_and_instances);
			});
			return lista;
		}
		
		//palauttaa prefabin nimen perusteella
		public Prefab_and_instances Get_Prefab_with_Name(string name) {
			if(name==null) return null;
			name=Prefab_and_instances.RemoveCloneinName(name);
			Prefab_and_instances instanssi=null;
			gameObject_Prefabs.ForEach(delegate(Prefab_and_instances prefab_and_instances) {
				if(prefab_and_instances.thePrefab!=null)
					if(prefab_and_instances.thePrefab.name.CompareTo(name)==0) instanssi = prefab_and_instances;
			});
			return instanssi;
		}
		
		//listaa prefabien nimet, joilla on annettu tagi
		public List<string> Get_Names_of_Prefabs_with_Tag(string tag) {
			if(tag==null) return null;
			List<string> lista=new List<string>();
			Get_Prefabs_with_Tag(tag).ForEach(delegate (Prefab_and_instances prefab_and_instances) {
				lista.Add(prefab_and_instances.thePrefab.name);
			});
			return lista;
		}
		
		//liikuttaa kaikkia peliobjektia niiden omilla liikuttelijoilla yhden time_stepin verran
		//kierroksen päätteeksi poistetaan tuhotut modified_gameobjectit
		public void Move_All(float deltaTime) {
			gameObject_Prefabs.ForEach(delegate(Prefab_and_instances obj) {
				obj.Move_GameObjects_and_RemoveDestroyed(deltaTime);
			});
		}
		
		//kutsutaan kaikkia spawnreita eli luo niissä määriteltyjä gameobjecteja
		private void Spawn_All() {
			gameObject_Spawners.ForEach(delegate(GameObject_Spawner obj) {
				obj.Spawn();
			});
			spawner_timerin_kayttajat.ForEach(delegate(GameObject_Spawner.Spawner_Timerin_Kayttaja obj) {
				obj.Spawn();
			});
		}

		public override void SuoritaToiminnot() {
			Spawn_All();
			if(smooth_camera!=null) smooth_camera.Paivita();
		}

		public override string ToString ()
		{
			return string.Format ("[Target_ohjaus: ]");
		}
		
		//ottaa törmäystiedot vastaan Collider_for_Target_ohjaus:lta ja siirtää tiedot GetCollisions_and_Triggers-metodille joka delegoi käsittelyn eteenpäin
		public void GetCollisions(GameObject gameObject, int col_event, Collision collision) {
			//mahdollisen uuden objektin koordinaatiksi asetetaan ensimmäinen törmäyskohta
			if(collision.contacts.Length==0) //ei saada tietoa törmäyskohdasta
				GetTriggers(gameObject, col_event, collision.collider);
			else
				GetCollisions_and_Triggers(gameObject, col_event, collision.gameObject, Ohjattava_2D.Convert_to_Vector2(collision.contacts[0].point));
		}
		
		//ottaa törmäystiedot (isTrigger is true) vastaan Collider_for_Target_ohjaus:lta ja siirtää tiedot GetCollisions_and_Triggers-metodille joka delegoi käsittelyn eteenpäin
		public void GetTriggers(GameObject gameObject, int col_event, Collider collider) {
			//määritetään törmäyskohta mahdollisesti uuden gameobjectin luomista varten
			//jos jompi kumpi törmäävistä kappaleista on pyöreä, saadaan törmäyskohta helposti määritettyä kehälle
			//muussa tapauksessa mahdollisen uuden objektin koordinaatiksi asetetaan törmäävien kappaleiden keskipisteiden välisen janan puoliväli
			//törmäyskohdalle pitäisi tehdä parempi toteutus, jos tarvitaan!!!
			//voi käyttää myös Collision-luokkaa, jos mahdollista
			float etaisyys=(gameObject.transform.position-collider.gameObject.transform.position).magnitude;
			float suhdeluku=0.5f;
			if (gameObject.collider.GetType().Equals(typeof(SphereCollider))) {
				suhdeluku=gameObject.transform.localScale.x/etaisyys;
			} else if (collider.GetType().Equals(typeof(SphereCollider))) {
				suhdeluku=(etaisyys-collider.gameObject.transform.localScale.x)/etaisyys;
			}
			GetCollisions_and_Triggers(gameObject, col_event, collider.gameObject, Vector2.Lerp(Ohjattava_2D.Convert_to_Vector2(gameObject.transform.position), Ohjattava_2D.Convert_to_Vector2(collider.gameObject.transform.position), suhdeluku));
		}
		
		//ottaa törmäystiedot vastaan ja delegoi käsittelyn edelleen Collisions_and_Triggers_Handling-luokan oliolle (oliot käydään läpi ja olio tunnistaa, kuuluuko törmäys sille)
		public void GetCollisions_and_Triggers(GameObject gameObject, int col_event, GameObject otherGameObject, Vector2 coordinates_of_possible_instantiated_gameobject) {
			Prefab_and_instances prefab_and_instances=Get_Prefab_with_Name(gameObject.name);
			if (prefab_and_instances!=null) {
				Prefab_and_instances.Modified_GameObject modified_GameObject=prefab_and_instances.Get_Modified_GameObject(gameObject);
				if (modified_GameObject==null) //jos instanssia ei ole lisätty modified_gameobject-listaan, luodaan väliaikainen, jota ei aseteta talteen
					modified_GameObject=new Prefab_and_instances.Modified_GameObject(prefab_and_instances.thePrefab, Ohjattava_2D.Convert_to_Vector2(gameObject.transform.position), gameObject.transform.rotation, null, new Vector2(0,0), null, gameObject);
				if(!modified_GameObject.IsDestroyed()) { //jos gameobjectia ei ole juuri poistettu toisen törmäyksen seurauksena
					Prefab_and_instances other_prefab_and_instances=Get_Prefab_with_Name(otherGameObject.name);
					if (other_prefab_and_instances!=null) {
						Prefab_and_instances.Modified_GameObject other_modified_GameObject=other_prefab_and_instances.Get_Modified_GameObject(otherGameObject);
						if (other_modified_GameObject==null) //jos instanssia ei ole lisätty modified_gameobject-listaan, luodaan väliaikainen, jota ei aseteta talteen
							other_modified_GameObject=new Prefab_and_instances.Modified_GameObject(other_prefab_and_instances.thePrefab, Ohjattava_2D.Convert_to_Vector2(otherGameObject.transform.position), otherGameObject.transform.rotation, null, new Vector2(0,0), null, otherGameObject);
						if(!other_modified_GameObject.IsDestroyed()) { //jos gameobjectia ei ole juuri poistettu toisen törmäyksen seurauksena
							collisions_and_Triggers_Handlers.ForEach(delegate(Collisions_and_Triggers_Handling obj) {
								obj.Detect_and_Handling(prefab_and_instances, modified_GameObject, other_prefab_and_instances, other_modified_GameObject, col_event, coordinates_of_possible_instantiated_gameobject);
							});
						}
					}
				}
			}
		}
		
		//luokka huolehtii peliobjektien synnyttämisestä automaattisesti tietyin väliajoin
		//luotavien määrä (kerralla) voi vaihdella satunnaisesti
		//levelin hallinta määrittää yhteensä luotavien lukumäärän
		//peliobjektit luodaan satunnaisesti, joko nelikulmion tai ympyrän reunalle tai viivalle
		//nopeus voidaan arpoa satunnaisesti tai asettaa kaikille sama
		public class GameObject_Spawner : RekisteroitavanTapahtumanSuorittaja<GameObject_Spawner> {
			public List<Prefab_and_instances> prefabs_to_spawn; // lista prefabeista, joista arvotaan, mikä tai mitkä luodaan
			public Spawner_Timer spawner_timer;
			public Get_RandomPoint_from_Shape randoming_initial_point;
			public Get_RandomPoint_from_Shape randoming_target_point;
			public bool set_random_speed; //jos asetettu, kloonataan prefabin liikuttelijasta luotavalle gameobjektille oma ja arvotaan sille satunnainen nopeus
			public float random_speed_min;
			public float random_speed_max;
			
			public GameObject_Spawner(List<Prefab_and_instances> p_prefabs_to_spawn, Spawner_Timer p_spawner_timer, Get_RandomPoint_from_Shape p_randoming_initial_point, Get_RandomPoint_from_Shape p_randoming_target_point, bool p_set_random_speed, float p_random_speed_min, float p_random_speed_max) {
				prefabs_to_spawn=p_prefabs_to_spawn;
				spawner_timer=p_spawner_timer;
				randoming_initial_point=p_randoming_initial_point;
				randoming_target_point=p_randoming_target_point;
				set_random_speed=p_set_random_speed;
				random_speed_min=p_random_speed_min;
				random_speed_max=p_random_speed_max;
			}
			
			//Luo spawnerille asetuista prefabeista gameobjekteja
			public Prefab_and_instances.Modified_GameObject Spawn() {
				int prefab_ind;
				Prefab_and_instances.Moving_Gameobject moving_handling=null;
				int nmb_of_gameobjects_to_spawn=spawner_timer.Spawn();
				for(int i=0; i<nmb_of_gameobjects_to_spawn; i++) {
					if(RekisteroiTapahtuma()) { // jos levelinhallinta antaa suorittaa toiminnon eli ei ole luotu liikaa
						prefab_ind=Ohjattava_2D.RandomRange(0, prefabs_to_spawn.Count-1);
						if(set_random_speed==true) {
							//kloonataan prefabin liikuttelija ja asetetaan sille satunnainen nopeus
							moving_handling=(Prefab_and_instances.Moving_Gameobject) prefabs_to_spawn[prefab_ind].moving_handling.Clone();
							moving_handling.speed=UnityEngine.Random.Range(random_speed_min, random_speed_max);
						}
						return prefabs_to_spawn[prefab_ind].Instantiate_GameObject_to_Position_and_Set_Rotation_Toward_Another_Position(randoming_initial_point.Get_RandomPoint(), randoming_target_point.Get_RandomPoint(), moving_handling);
					}
				}
				return null;
			}
			
			public void AsetaTapahtumanRekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<GameObject_Spawner> p_tapahtuman_rekisteroija) {
				base.AsetaTapahtumanRekisteroija(p_tapahtuman_rekisteroija, this);
			}

			//ajastin spawnereille
			public class Spawner_Timer {
				public float delaytime;
				public int min_nmb_of_gameobjects_to_spawn_at_time;
				public int max_nmb_of_gameobjects_to_spawn_at_time;
				private float next_spawn_time;

				public Spawner_Timer(float p_delaytime, int p_min_nmb_of_gameobjects_to_spawn_at_time, int p_max_nmb_of_gameobjects_to_spawn_at_time) {
					delaytime=p_delaytime;
					min_nmb_of_gameobjects_to_spawn_at_time=p_min_nmb_of_gameobjects_to_spawn_at_time;
					max_nmb_of_gameobjects_to_spawn_at_time=p_max_nmb_of_gameobjects_to_spawn_at_time;
				}
				
				//plauttaa lukumäärän, kuinka monta luodaan juuri nyt
				public int Spawn() {
					if(Time.time>next_spawn_time) {
						next_spawn_time=Time.time+delaytime;
						return Ohjattava_2D.RandomRange(min_nmb_of_gameobjects_to_spawn_at_time, max_nmb_of_gameobjects_to_spawn_at_time);
					} else return 0; //ei juuri nyt
				}
			}

			//asennetaan tästä periytetty luokka käyttökohteeseen
			public abstract class Spawner_Timerin_Kayttaja {
				public Spawner_Timer spawner_timer;
				protected int nmb_of_to_spawn=0;

				public Spawner_Timerin_Kayttaja(Spawner_Timer p_spawner_timer) {
					spawner_timer=p_spawner_timer;
					Target_ohjaus.Instance.spawner_timerin_kayttajat.Add(this);
				}

				//tämä pitää toteuttaa. spawn-kutsut tulee automaattisesti
				public virtual void Spawn() {
					nmb_of_to_spawn=spawner_timer.Spawn();
				}

				//poistaa target-ohjauksesta (esim. kun käyttäjä on tuhoutunut)
				public void Removing() {
					Target_ohjaus.Instance.spawner_timerin_kayttajat.Remove(this);
				}
			}
		}
		
		//luokka käsittelee gameobjectien törmäystapahtumat
		public class Collisions_and_Triggers_Handling : RekisteroitavanTapahtumanSuorittaja<Collisions_and_Triggers_Handling> {
			public List<Prefab_and_instances> list_prefab_and_instances; //prefabit, joihin törmätään
			public List<Prefab_and_instances> list_other_prefab_and_instances; //prefabit, jotka törmäävät
			public List<int> col_events; //tapahtumat, jotka laukaisevat käsittelyn
			public ForDestroyingInstantiated prefab_to_instantiate_handling; //törmäyksessä mahdollisesti luotava (animaatio) gameobjectin hallinta
			public Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Instantiate_Kohdistus instantiate_kohdistus_maaritys;
			public bool destroy_gameobject;
			public bool destroy_other_gameobject;
			public delegate void ToiminnonSuorittaja(Collisions_and_Triggers_Handling tormays_olio);
			public ToiminnonSuorittaja toiminto;
			
			public Collisions_and_Triggers_Handling(List<Prefab_and_instances> p_list_prefab_and_instances, List<Prefab_and_instances> p_list_other_prefab_and_instances, List<int> p_col_events, bool p_destroy_gameobject, bool p_destroy_other_gameobject, ForDestroyingInstantiated p_prefab_to_instantiate_handling, ToiminnonSuorittaja p_toiminto, Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Instantiate_Kohdistus p_instantiate_kohdistus_maaritys=Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Instantiate_Kohdistus.asetus_muualla) {
				list_prefab_and_instances=p_list_prefab_and_instances;
				list_other_prefab_and_instances=p_list_other_prefab_and_instances;
				col_events=p_col_events;
				destroy_gameobject=p_destroy_gameobject;
				destroy_other_gameobject=p_destroy_other_gameobject;
				prefab_to_instantiate_handling=p_prefab_to_instantiate_handling;
				toiminto=p_toiminto;
				instantiate_kohdistus_maaritys=p_instantiate_kohdistus_maaritys;
			}
			
			public Collisions_and_Triggers_Handling(string prefab_tag, string other_prefab_tag, List<int> p_col_events, bool p_destroy_gameobject, bool p_destroy_other_gameobject, ForDestroyingInstantiated p_prefab_to_instantiate_handling, ToiminnonSuorittaja p_toiminto)
			: this(Target_ohjaus.Instance.Get_Prefabs_with_Tag(prefab_tag), Target_ohjaus.Instance.Get_Prefabs_with_Tag(other_prefab_tag), p_col_events, p_destroy_gameobject, p_destroy_other_gameobject, p_prefab_to_instantiate_handling, p_toiminto) {}			
			
			//testaa kuuluuko törmäys itselle ja käsittelee tapahtuman
			public bool Detect_and_Handling(Prefab_and_instances prefab_and_instances, Prefab_and_instances.Modified_GameObject modified_GameObject, Prefab_and_instances other_prefab_and_instances, Prefab_and_instances.Modified_GameObject othet_modified_GameObject, int col_event, Vector2 coordinates_of_possible_instantiated_gameobject) {
				//testataan, kuuluuko törmäys itselle
				bool loytyi=false;
				if (LoytyykoListasta(prefab_and_instances.thePrefab.name, list_prefab_and_instances))
					if (LoytyykoListasta(other_prefab_and_instances.thePrefab.name, list_other_prefab_and_instances))
						loytyi=col_events.Exists(delegate(int obj) {
							return col_event==obj;
						});
				//jos kuuluu, käsitellään törmäystapahtuma
				if(loytyi) {
					if(RekisteroiTapahtuma()) { // jos levelinhallinta antaa suorittaa toiminnon
						//tuhotaan mahdollisesti törmäävät gameobjectit
						if(destroy_gameobject) prefab_and_instances.Destroy_GameObject(modified_GameObject, 0f);
						if(destroy_other_gameobject) other_prefab_and_instances.Destroy_GameObject(othet_modified_GameObject, 0f);
						//luodaan mahdollisesti törmäyksessä syntyvä gameobjecti
						if(this.prefab_to_instantiate_handling!=null) {
							//ratkaistaan kohdistus-transform, jonka suhteen ForDestroyingInstantiated ratkaisee koordinaatit (jos valittu)
							Transform kohdistus=null;
							if(instantiate_kohdistus_maaritys==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Instantiate_Kohdistus.joukon1_mukaan)
								kohdistus=modified_GameObject.gameobject.transform;
							else if(instantiate_kohdistus_maaritys==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Instantiate_Kohdistus.joukon2_mukaan)
								kohdistus=othet_modified_GameObject.gameobject.transform;
							prefab_to_instantiate_handling.Instantiate(coordinates_of_possible_instantiated_gameobject, kohdistus);
						}
						//suoritetaan mahdollisesti törmäykseen liittyvä vapaamuotoinen lisätoiminto
						if(toiminto!=null) toiminto(this);
					}
				}
				return loytyi;
			}

			public bool LoytyykoListasta(string testattavan_nimi, List<Prefab_and_instances> p_list_prefab_and_instances) {
				return p_list_prefab_and_instances.Exists(delegate(Prefab_and_instances obj) {
					return testattavan_nimi.CompareTo(obj.thePrefab.name)==0;
				});
			}
			
			public void AsetaTapahtumanRekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<Collisions_and_Triggers_Handling> p_tapahtuman_rekisteroija) {
				base.AsetaTapahtumanRekisteroija(p_tapahtuman_rekisteroija, this);
			}
		}
	}
	
	//luokalla voidaan luoda prefabille väliaikaisija peliobjekteja kuten räjähdyksiä, jotka voidaan asettaa saman tien tuhottavaksi
	public class ForDestroyingInstantiated {
		public Prefab_and_instances prefab_to_instantiate; //luotava gameobject
		public bool destroy_instantiated_gameobject;
		public float time_to_destroy_instantiated_gameobject;
		//koordinaatti ja rotaatio asetukset
		public Koordinaattiasetukset_Hallintapaneeliin instantiate_koordinaatin_maaritys=null; //mikäli on null, käytetään koordinaattia, joka annetaan istantiate-kutsussa
		public Rotationasetukset_Hallintapaneeliin instantiate_rotation_maaritys=null; //mikäli null, käytetään Quaternion.identity
		public Koordinaattiasetukset_Hallintapaneelista_Rakentaja koordinaattien_ratkaisija=new Koordinaattiasetukset_Hallintapaneelista_Rakentaja();
		public Rotationasetukset_Hallintapaneelista_Rakentaja rotation_ratkaisija=new Rotationasetukset_Hallintapaneelista_Rakentaja();

		public ForDestroyingInstantiated(GameObject prefab_to_instantiate_game_object, bool p_destroy_instantiated_gameobject, float p_time_to_destroy_instantiated_gameobject, Koordinaattiasetukset_Hallintapaneeliin p_instantiate_koordinaatin_maaritys=null, Rotationasetukset_Hallintapaneeliin p_instantiate_rotation_maaritys=null) {
			Prefab_and_instances_Hallintapaneelista_Rakentaja prefab_and_instances_rakentaja=new Prefab_and_instances_Hallintapaneelista_Rakentaja();
			this.prefab_to_instantiate=prefab_and_instances_rakentaja.Rekisteroi_Prefab(prefab_to_instantiate_game_object); //rekisteröidään varmuuden vuoksi, koska prefabista ei ole vielä instanssia (prefabin asetuksia ei ole vielä suoritettu). parametrit täydentyy, kun eka instanssi luodaan
			destroy_instantiated_gameobject=p_destroy_instantiated_gameobject;
			time_to_destroy_instantiated_gameobject=p_time_to_destroy_instantiated_gameobject;
			instantiate_koordinaatin_maaritys=p_instantiate_koordinaatin_maaritys;
			instantiate_rotation_maaritys=p_instantiate_rotation_maaritys;
		}
		
		public void Instantiate(Vector2 coordinates, Transform kohditus=null) {
			//ratkaistaan koordinaatti ja rotation
			if(instantiate_koordinaatin_maaritys!=null) { //jos on null, käytetään kutsujan antamaa koordinaattia
				coordinates=koordinaattien_ratkaisija.Rakenna(instantiate_koordinaatin_maaritys, kohditus);
			}
			Quaternion rotation=Quaternion.identity; //oletuksena, mikäli rotationia ei ole määritetty
			if(instantiate_rotation_maaritys!=null)
				rotation=rotation_ratkaisija.Rakenna(instantiate_rotation_maaritys, kohditus);
			Prefab_and_instances.Modified_GameObject instantiated_gameobject=this.prefab_to_instantiate.Instantiate_new_GameObject(coordinates, rotation, null, new Vector2(0,0), null);
			//mahdollisesti tuhhotaan syntynyt gameobjecti
			if(destroy_instantiated_gameobject && instantiated_gameobject!=null) prefab_to_instantiate.Destroy_GameObject(instantiated_gameobject, time_to_destroy_instantiated_gameobject);
		}
	}

	//luokka sisältää prefabin määritykset, gameobjectien instanssit ja toimintaoliot kuten liikuttelijan
	public class Prefab_and_instances : RekisteroitavanTapahtumanSuorittaja_for_Prefab_and_instances {
		public GameObject thePrefab; //varsinainen prefab: sisältää gameobjektin tiedot kuten nimen ja tagin
		public List<Modified_GameObject> modified_GameObjects; //peliobjectien varsinaiset instanssit (löytyy modifioidun olion sisältä)
		public List<Prefab_and_instances> target_prefabs; //prefabille mahdollisesti targetiksi asetetut prefabit
		public Rect allowed_area; //sallittu alue -> jos menee alueen ulkopuolelle, peliobjekti poistetaan
		public Moving_Gameobject moving_handling=null; //huolehtii peliobjektien liikuttamisesta
		public ForDestroyingInstantiated handling_prefab_to_instantiate_in_destroying=null; // kun peliobjekti tuhotaan, tämä luo esim. tuhoutumisanimaation ja huolehtii myös sen tuhoamisesta
		public bool parametrit_taydennetty=false; //tieto ettei parametreja täydennetä monta kertaa eri instansseilta

		//annetaan vain prefab
		//muut parametrit täydennetään myöhemmin, koska kaikkille prefabeille ei ole vielä luotu oliota (esim. targeteille)
		//luodaan tästä awake()-funktiosta käsin ja start():ssa täydennetään muut parametrit
		public Prefab_and_instances(GameObject p_thePrefab) {
			thePrefab=p_thePrefab;
			modified_GameObjects=new List<Modified_GameObject>();
			target_prefabs=new List<Prefab_and_instances>();
			thePrefab.name=RemoveCloneinName(thePrefab.name);
		}

		//annetaan kaikki parametrit
		public Prefab_and_instances(GameObject p_thePrefab, List<Prefab_and_instances> p_target_prefabs, Rect p_allowed_area, Moving_Gameobject p_moving_handling, ForDestroyingInstantiated p_handling_prefab_to_instantiate_in_destroying, List<string> p_name_of_target_prefabs_not_awailable=null) :this(p_thePrefab) {
			TaydennaParametrit(p_target_prefabs, p_allowed_area, p_moving_handling, p_handling_prefab_to_instantiate_in_destroying);
		}

		//olio voidaan luoda awake()-funktiosta asettamalla vain prefab-parametri. muut parametrit, kuten targetit täydennetään tästä start()-vaiheessa
		public void TaydennaParametrit(List<Prefab_and_instances> p_target_prefabs, Rect p_allowed_area, Moving_Gameobject p_moving_handling, ForDestroyingInstantiated p_handling_prefab_to_instantiate_in_destroying) {
			if (p_target_prefabs==null)
				target_prefabs=new List<Prefab_and_instances>();
			else
				target_prefabs=p_target_prefabs;
			if(p_allowed_area==null) p_allowed_area=Target_ohjaus.Instance.public_allowed_area; //jos ei erikseen anneta, käytetään yleistä asetusta
			allowed_area=p_allowed_area;
			moving_handling=p_moving_handling;
			handling_prefab_to_instantiate_in_destroying=p_handling_prefab_to_instantiate_in_destroying;
			parametrit_taydennetty=true; //merkiksi, että parametrit on jo täydennetty
		}
		
		public void AsetaTapahtumanRekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> p_tapahtuman_rekisteroija_for_instantiate, Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> p_tapahtuman_rekisteroija_for_destroy) {
			base.AsetaTapahtumanRekisteroija(p_tapahtuman_rekisteroija_for_instantiate, p_tapahtuman_rekisteroija_for_destroy, this);
		}

		//poistetaan nimestä mahdollinen "(Clone)" lopusta
		public static string RemoveCloneinName(string name) {
			if (name.EndsWith("(Clone)")) {
				name=name.Substring(0,name.Length-7);
			}
			return name;
		}
		
		//lisää olemassa olevan gameObjectin, jos sitä ei ole jo lisätty
		public Modified_GameObject Add_new_GameObject(GameObject gameObject, GameObject p_target_gameobject, Vector2 p_target_position, Moving_Gameobject moving_handling) {
			Modified_GameObject modified_gameobject=modified_GameObjects.Find(delegate(Modified_GameObject obj) {
				return obj.TestaaOnkoSamaInstanssi(gameObject);
			});
			if(modified_gameobject==null) {
				modified_gameobject=new Modified_GameObject(this.thePrefab, Ohjattava_2D.Convert_to_Vector2(gameObject.transform.position), gameObject.transform.rotation, p_target_gameobject, p_target_position, moving_handling, gameObject);
				modified_GameObjects.Add(modified_gameobject);
			}
			return modified_gameobject;
		}
		
		//luo ja lisää uuden gameObjectin
		public Modified_GameObject Instantiate_new_GameObject(Vector2 initial_position, Quaternion initial_quaternion, GameObject p_target_gameobject, Vector2 p_target_position, Moving_Gameobject moving_handling) {
			if(RekisteroiTapahtuma_instantiate()) { // jos levelinhallinta antaa suorittaa toiminnon
				Modified_GameObject modified_gameobject=new Modified_GameObject(this.thePrefab, initial_position, initial_quaternion, p_target_gameobject, p_target_position, moving_handling, null);
				modified_GameObjects.Add(modified_gameobject);
				return modified_gameobject;
			} else return null;
		}
		//peliobjektien luomiseen apufunktiot
		public Modified_GameObject Instantiate_GameObject_to_Position_and_Set_Rotation_Toward_Another_Position(Vector2 initial_position, Vector2 target_position, Prefab_and_instances.Moving_Gameobject moving_handling) {
			//luodaan peliobjekti initial_positioniin ja asetetaan kohti annettua target_positionia
			return this.Instantiate_new_GameObject(initial_position, Quaternion.FromToRotation(Vector3.right, target_position-initial_position), null, target_position, moving_handling);
		}
		public Modified_GameObject Instantiate_GameObject_to_Position_and_Set_Rotation_Toward_Given_Target(Vector2 initial_position, GameObject target_gameobject, Prefab_and_instances.Moving_Gameobject moving_handling) {
			//luodaan peliobjekti initial_positioniin ja asetetaan kohti annettua target_positionia
			return this.Instantiate_new_GameObject(initial_position, Quaternion.FromToRotation(Vector3.right, target_gameobject.transform.position-Ohjattava_2D.Convert_to_Vector3(initial_position)), target_gameobject, new Vector2(0,0), moving_handling);
		}
		public Modified_GameObject Instantiate_GameObject_to_Position_and_Set_Rotation_Toward_Closest_Target(Vector2 initial_position, Vector2 find_target_from_position, float max_distance_to_target, bool if_not_find_target_instantiate_still, Prefab_and_instances.Moving_Gameobject moving_handling) {
			//luodaan peliobjekti initial_positioniin ja asetetaan kohti lähimmän targetin_positionia
			//mikäli lähin target on liian kaukana, asetetaan kohti annettua positionia, josta lähintä targettia etsitään (mikäli asetettu)
			GameObject target_gameobject = this.Find_Nearest_Target_for_GameObject_from_given_position(find_target_from_position, max_distance_to_target);
			if(target_gameobject!=null)
				return this.Instantiate_GameObject_to_Position_and_Set_Rotation_Toward_Given_Target(initial_position, target_gameobject, moving_handling);
			else if(if_not_find_target_instantiate_still) //luodaan silti kohti positionia, josta lähintä targettia etsittiin
				return this.Instantiate_GameObject_to_Position_and_Set_Rotation_Toward_Another_Position(initial_position, find_target_from_position, moving_handling);
			return null;
		}
		
		//lisää targetiksi prefabit, jonka nimet annetaan
		public void AddTargets(List<string> names) {
			names.ForEach(delegate(String name) {
				AddTarget(name);
			});
		}
		
		//lisää targetiksi prefabin, jonka nimi annetaan
		public void AddTarget(string name) {
			Prefab_and_instances prefab_and_instances;
			prefab_and_instances=Target_ohjaus.Instance.Get_Prefab_with_Name(name);
			if (prefab_and_instances!=null) this.target_prefabs.Add(prefab_and_instances);
		}
		
		//liikuttaa prefabin kaikkia peliobjecteja, kutsumalla niihin erikseen liitettyjä liikuttelija-olioita (tai prefabille yleiseksi luotua liikuttelijaoliota, jos ei peliobjektilla ole omaa)
		//tuhottavaksi asetettavat poistetaan
		public void Move_GameObjects_and_RemoveDestroyed(float deltaTime) {
			modified_GameObjects.ForEach(delegate(Modified_GameObject modified_GameObject) {
				if(!modified_GameObject.IsDestroyed()) {
					modified_GameObject.Move_GameObject(this, deltaTime);
				} else {
					//siivotaan olio pois, jos gameobject on tuhottu
					if (handling_prefab_to_instantiate_in_destroying!=null && IsInside_in_the_AllowedArea(modified_GameObject)) { // //luodaan esim. tuhoutumisanimaatio (mutta jos on alueen ulkopuolella, tuhotaan hiljaa)
						handling_prefab_to_instantiate_in_destroying.Instantiate(Ohjattava_2D.Convert_to_Vector2(modified_GameObject.gameobject.transform.position), modified_GameObject.gameobject.transform);
					}
					if(thePrefab.Equals(modified_GameObject.gameobject))
						modified_GameObject.gameobject.SetActive(false); //ei poisteta itse prefab-instanssia vaan kytketään vain pois
					else
						GameObject.Destroy(modified_GameObject.gameobject); //poistetaan itse gameobject
					modified_GameObjects.Remove(modified_GameObject);
				}
			});
		}

		//siivoaa pois oliot, joiden gameobject on tuhottu
		public void Remove_Destroyed_ModifiedGameobjects() {
			modified_GameObjects.ForEach(delegate(Modified_GameObject obj) {
				if(obj.IsDestroyed()) {
					if (handling_prefab_to_instantiate_in_destroying!=null && IsInside_in_the_AllowedArea(obj)) { // //luodaan esim. tuhoutumisanimaatio (mutta jos on alueen ulkopuolella, tuhotaan hiljaa)
						handling_prefab_to_instantiate_in_destroying.Instantiate(Ohjattava_2D.Convert_to_Vector2(obj.gameobject.transform.position), obj.gameobject.transform);
					}
					if(thePrefab.Equals(obj.gameobject))
						obj.gameobject.SetActive(false); //ei poisteta itse prefab-instanssia vaan kytketään vain pois
					else
						GameObject.Destroy(obj.gameobject); //poistetaan itse gameobject
					modified_GameObjects.Remove(obj);
				}
			});
		}
		
		//lisää prefabille targetiksi prefabit, joiden nimet annetaan
		public void Add_Target_Prefabs_with_Names(List<string> names) {
			if (target_prefabs==null) target_prefabs=new List<Prefab_and_instances>();
			Target_ohjaus.Instance.gameObject_Prefabs.ForEach(delegate(Prefab_and_instances prefab_and_instances) {
				if (prefab_and_instances.thePrefab.name.CompareTo(thePrefab.name)==0) target_prefabs.Add(prefab_and_instances);
			});
		}
		
		//tarkistaa, onko peliobjekti sallitun alueen sisällä (jos on käytössä)
		public bool IsInside_in_the_AllowedArea(Modified_GameObject modified_gameobject) {
			if(Target_ohjaus.Instance.use_allowed_area) {
				Vector2 position=modified_gameobject.gameobject.transform.position;
				if(position.x > this.allowed_area.x && position.x < (this.allowed_area.x + this.allowed_area.width) && position.y < (this.allowed_area.y + this.allowed_area.height) && position.y > this.allowed_area.y)
					return true;
				else
					return false;
			} else return true;
		}
		
		//funktio palauttaa annetusta positionista lähimmän targetin
		//mikäli lähin on liian kaukana eli etäisyys on suurempi kuin maksimi etäisyys, palautetaan null
		public GameObject Find_Nearest_Target_for_GameObject_from_given_position(Vector2 position, float max_dist_to_target) {
			GameObject target_gameobject=null;
			float dist;
			float closest_dist=max_dist_to_target; //suurin sallittu etäisyys targettiin
			//käydään targetit läpi jos on
			if (this.target_prefabs!=null)
				this.target_prefabs.ForEach(delegate(Prefab_and_instances target_prefab_and_instances) {
					target_prefab_and_instances.modified_GameObjects.ForEach(delegate(Prefab_and_instances.Modified_GameObject target_modified_gameobject) {
						if(target_modified_gameobject.gameobject.activeSelf) { //jos on aktiivinen
							dist = Vector2.Distance(Ohjattava_2D.Convert_to_Vector3(position), target_modified_gameobject.gameobject.transform.position);
							if (dist < closest_dist) {
								closest_dist = dist;
								target_gameobject = target_modified_gameobject.gameobject;
							}
						}
					});
				});
			return target_gameobject;
		}
		
		//tuhoaa varsinaisen gameobjectin ja poistaa sen myös prefabin instanssilistasta (eli täydellinen siivoaminen)
		public void Destroy_GameObject(Modified_GameObject item, float time) {
			if (item!=null)
				if(!item.IsDestroyed()) //jos ei ole jo asetettu poistettavaksi
					if(RekisteroiTapahtuma_destroy(item)) // jos levelinhallinta antaa suorittaa toiminnon
						item.Set_toDestroying(time); //asetetaan myöhemmin poistettavaksi tai heti seuraavalle siivouskerralle mikäli aika 0 (jää eliminoimaan tuplatärmäykset)
		}

		//etsii annetun varsinaisen gameobjectin ja palauttaa modifioidun version, jonka sisällä varsinainen on
		public Modified_GameObject Get_Modified_GameObject(GameObject gameobject) {
			Modified_GameObject matched=null;
			modified_GameObjects.ForEach(delegate(Modified_GameObject modified_gameobject) {
				if(modified_gameobject.gameobject.Equals(gameobject))
					matched=modified_gameobject;
			});
			return matched;
		}
		
		//gameobjectin modifioitu versio, joka toteuttaa varsinaiselle gameobjectille kaikki sen toiminnot kuten liikuttelun
		public class Modified_GameObject {
			public GameObject gameobject; //hallittava varsinainen peliobjekti
			public GameObject target_gameobject; //peliobjektille mahdollisesti asetettu targetti
			public Vector2 target_position; //mahdollinen kiinteä target positio
			public Vector2 current_direction; //tämänhetkinen liikkumissuunta
			public Moving_Gameobject moving_handling; //huolehtii peliobjektin liikuttamisesta (tämä on kyseiselle peliobjektille oma liikuttelija, joka ohittaa prefabin liikuttelijan)
			private bool destroyed=false; //olio pitää säilyttää, jotta ehkäistään tuplatörmäykset. Target-ohjaus siivoaa poistetut pois moving-kierroksen päätteeksi
			private float time_to_destroying; //aika, jonka jälkeen olion saa tuhota
			
			public Modified_GameObject(GameObject thePrefab, Vector2 initial_position, Quaternion initial_quaternion, GameObject p_target_gameobject, Vector2 p_target_position, Moving_Gameobject p_moving_handling, GameObject p_gameObject) {
				target_gameobject=p_target_gameobject;
				target_position=p_target_position;
				moving_handling=p_moving_handling;
				if(p_gameObject==null) { //jos ei annetan gameObjectia, se luodaan nyt
					this.gameobject=GameObject.Instantiate(thePrefab, Ohjattava_2D.Convert_to_Vector3(initial_position), initial_quaternion) as GameObject;
				} else
					this.gameobject=p_gameObject;
				this.gameobject.name=Prefab_and_instances.RemoveCloneinName(this.gameobject.name);
				current_direction=Ohjattava_2D.Convert_to_Vector2(this.gameobject.transform.forward);
			}

			//vaihtaa peliobjectin prefabin. näin voidaan muuttaa ukkeli toisennäköiseksi
			public void Change_the_Prefab(GameObject new_thePrefab) {
				Vector3 initial_position=this.gameobject.transform.position;
				Quaternion initial_quaternion=this.gameobject.transform.rotation;
				GameObject.Destroy(this.gameobject, 0f);
				this.gameobject=GameObject.Instantiate(new_thePrefab, initial_position, initial_quaternion) as GameObject;
			}

			public bool TestaaOnkoSamaInstanssi(GameObject testattava_gameobject) {
				return gameobject.Equals(testattava_gameobject);
			}
			
			//liikuttaa peliobjektia timestepin verran
			public void Move_GameObject(Prefab_and_instances prefab_and_instances, float deltaTime) {
				//käytetään ensisijaisesti peliobjektin omaa moving handlin:ia, jos on
				//ja muuten prefabin moving_handlingia
				//jos ei ole asetettu moving_handlingia, ei tehdä mitään
				if(this.moving_handling!=null)
					this.moving_handling.Move_step(prefab_and_instances, this, deltaTime);
				else if(prefab_and_instances.moving_handling!=null)
					prefab_and_instances.moving_handling.Move_step(prefab_and_instances, this, deltaTime);
			}

			//asettaa olion poistettavaksi sen jälkeen kun gameobject poistetaan
			public void Set_toDestroying(float time_countdown) {
				destroyed=true;
				time_to_destroying=Time.time+time_countdown;
			}
			public bool IsDestroyed() {
				if(destroyed) {
					if(time_to_destroying<=Time.time)
						return true;
					else
						return false;
				}
				return false;
			}
		}
		
		//liikuttelemisesta hoitavat luokat
		//huolehtivat myös sallitun alueen ulkopuolelle menevien peliobjektien poistamisesta
		//huolehtii myös pyörittämisestä, joka ei liity kulkusuuntaan
		public class Moving_Gameobject : ICloneable {
			public float speed;
			public float turn_speed;
			public Rotate_GameObject rotationer; //huolehtii pyörittämisestä, joka ei liity kulkusuuntaan
			
			public Moving_Gameobject(float p_speed, float p_turn_speed, Rotate_GameObject p_rotationer) {
				speed=p_speed;
				turn_speed=p_turn_speed;
				rotationer=p_rotationer;
			}
			
			//ylin luokka huolehtii peliobjektin poistamisesta, mikäli menee sallitun alueen ulkopuolelle
			//ja kiertämisen
			public virtual void Move_step(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject, float deltaTime) {
				if (rotationer!=null) {
					rotationer.Rotate(hallittava_gameobject, deltaTime);
				}
				Remove_GameObject_if_not_Inside_in_AllowedArea(prefab_and_instances, hallittava_gameobject);
			}
			
			//siivoaa pois sallitun alueen ulkopuolelle eksyvät peliobjektit
			public void Remove_GameObject_if_not_Inside_in_AllowedArea(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject) {
				if(!prefab_and_instances.IsInside_in_the_AllowedArea(hallittava_gameobject))
					prefab_and_instances.Destroy_GameObject(hallittava_gameobject, 0f);
			}
			
			//olion kloonaaja
			public object Clone()
			{
				return this.MemberwiseClone();
			}
			
		}
		//liikuttaa kohti kulkusuuntaansa, joka saatiin alku- ja kohdepositionista (jatkaa matkaa kohdepositionin yli)
		public class MoveToSelectedDirection : Moving_Gameobject {
			public MoveToSelectedDirection(float p_speed, float p_turn_speed, Rotate_GameObject p_rotationer) : base(p_speed, p_turn_speed, p_rotationer) {}
			
			public override void Move_step(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject, float deltaTime) {
				//edetään askel eteen päin
				Transform transform=hallittava_gameobject.gameobject.transform;
				transform.position+=Ohjattava_2D.Convert_to_Vector3(hallittava_gameobject.current_direction)*speed*deltaTime;
				//poistetaan, mikäli menee alueen ulkopuolelle
				base.Move_step(prefab_and_instances,hallittava_gameobject, deltaTime);
			}
			
			public void UpdateCurrentDirection(Modified_GameObject hallittava_gameobject) {
				//asettaa suunnan kohti target positionia
				//mikäli ollaan target positionissa, säilytetään vanha suunta
				Transform transform=hallittava_gameobject.gameobject.transform;
				hallittava_gameobject.current_direction=hallittava_gameobject.target_position-Ohjattava_2D.Convert_to_Vector2(transform.position);
				float pituus=hallittava_gameobject.current_direction.magnitude;
				if (pituus>0.001) hallittava_gameobject.current_direction /= pituus;
			}
		}
		//sama kuin edellinen, mutta pysähtyy kohdepositioonsa
		public class MoveToSelectedPosition : MoveToSelectedDirection {
			protected float distance;
			public MoveToSelectedPosition(float p_speed, float p_turn_speed, Rotate_GameObject p_rotationer) : base(p_speed, p_turn_speed, p_rotationer) {}
			
			public override void Move_step(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject, float deltaTime) {
				//käännetään kurssi heti kohti target positionia
				Transform transform=hallittava_gameobject.gameobject.transform;
				UpdateCurrentDirection(hallittava_gameobject);
				//ja edetään askel ja poistetaan, mikäli menee alueen ulkopuolelle
				if (distance==0) //lasketaan vain ekalla kerralla tässä
					distance=Vector2.Distance(Ohjattava_2D.Convert_to_Vector2(transform.position), hallittava_gameobject.target_position);
				float step=speed*deltaTime;
				if(step>distance)					
					deltaTime=distance/speed; // rajoitetaan askeleen pituutta ettei mene target_positionin ohi
				base.Move_step(prefab_and_instances,hallittava_gameobject, deltaTime);
				distance=Vector2.Distance(Ohjattava_2D.Convert_to_Vector2(transform.position), hallittava_gameobject.target_position);
			}
		}
		//sama kuin edellinen ja luo kohteessaan räjähdysanimaation (tai minkä vaan)
		public class MoveToSelectedPosition_and_ThenDestroying : MoveToSelectedPosition {
			public Prefab_and_instances other_prefab_and_instances;
			public float time_to_destroy_other_gameobject;
			public float liipaisu_etaisyys_kohdepositionista=0.5f; //oletusarvo
			public MoveToSelectedPosition_and_ThenDestroying(float p_speed, float p_turn_speed, Rotate_GameObject p_rotationer) : base(p_speed, p_turn_speed, p_rotationer) {}
			
			public void AsetaLiipaisuetaisyys_kohdepositionista(float p_liipaisu_etaisyys_kohdepositionista) {
				liipaisu_etaisyys_kohdepositionista=p_liipaisu_etaisyys_kohdepositionista;
			}
			
			public override void Move_step(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject, float deltaTime) {
				base.Move_step(prefab_and_instances,hallittava_gameobject, deltaTime);
				if (distance<liipaisu_etaisyys_kohdepositionista) { //liipaistaan eli tuhoudutaan
					prefab_and_instances.Destroy_GameObject(hallittava_gameobject, 0f);
				}
			}
		}
		//liikuttaa kohti valittua targettia (jos targettia ei ole, liikuttaa kohti kulkusuuntaansa)
		public class MoveTowardSelectedTarget : MoveToSelectedDirection {
			public MoveTowardSelectedTarget(float p_speed, float p_turn_speed, Rotate_GameObject p_rotationer) : base(p_speed, p_turn_speed, p_rotationer) {}
			
			public override void Move_step(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject, float deltaTime) {
				//päivitetään kurssi kohti targettia, mikäli on vielä olemassa, muussa tapauksessa säilytetään vanha suunta
				Transform transform=hallittava_gameobject.gameobject.transform;
				if (hallittava_gameobject.target_gameobject != null) {
					hallittava_gameobject.target_position=Ohjattava_2D.Convert_to_Vector2(hallittava_gameobject.target_gameobject.transform.position);
					UpdateCurrentDirection(hallittava_gameobject);
				}
				//liikutaan ja poistetaan, mikäli menee alueen ulkopuolelle
				base.Move_step(prefab_and_instances,hallittava_gameobject, deltaTime);
			}
		}
		//etsii tällä hetkellä lähimmän targetin ja liikuttaa kohti sitä (jos ei löydy, liikuttaa kohti kulkusuuntaansa)
		public class MoveTowardClosestTarget : MoveTowardSelectedTarget {
			public MoveTowardClosestTarget(float p_speed, float p_turn_speed, Rotate_GameObject p_rotationer) : base(p_speed, p_turn_speed, p_rotationer) {}
			
			public override void Move_step(Prefab_and_instances prefab_and_instances, Modified_GameObject hallittava_gameobject, float deltaTime) {
				//etsitään tällä hetkellä lähin target
				hallittava_gameobject.target_gameobject=prefab_and_instances.Find_Nearest_Target_for_GameObject_from_given_position(hallittava_gameobject.gameobject.transform.position, Mathf.Infinity);
				base.Move_step(prefab_and_instances, hallittava_gameobject, deltaTime); //käännytään ja edetään kohti targettia
			}
		}
		
		//luokat huolehtivat gameobjectien kiertämisestä
		//ylin luokka vain kiertää
		public class Rotate_GameObject : ICloneable {
			public Vector3 eulerAngles; //vapaan kiertämisen kulmakertoimet (x, y, z) kerrotaan deltaTime:lla
			
			public Rotate_GameObject(Vector3 p_eulerAngles) {
				eulerAngles=p_eulerAngles;
			}
			
			//kierretään
			public virtual void Rotate(Modified_GameObject hallittava_gameobject, float deltaTime) {
				hallittava_gameobject.gameobject.transform.Rotate(deltaTime*eulerAngles.x, deltaTime*eulerAngles.y, deltaTime*eulerAngles.z);
			}
			
			//olion kloonaus
			public object Clone()
			{
				return this.MemberwiseClone();
			}
		}
		//kääntää ensin kohti asetettua kulkusuuntaan ja sen jälkeen kierto
		public class SetRotationFirst_to_Direction_and_Then_Rotate : Rotate_GameObject {
			public SetRotationFirst_to_Direction_and_Then_Rotate(Vector3 p_eulerAngles) : base(p_eulerAngles) {}
			
			public override void Rotate(Modified_GameObject hallittava_gameobject, float deltaTime) {
				//käännetään kulkusuuntaan päin
				float pituus=hallittava_gameobject.current_direction.magnitude;
				if (pituus>0) // päivitetään vain kun suuntavektorin pituus ei ole nolla
					hallittava_gameobject.gameobject.transform.rotation=Quaternion.FromToRotation(Vector3.right, Ohjattava_2D.Convert_to_Vector3(hallittava_gameobject.current_direction));
				//tehdään kierto
				base.Rotate(hallittava_gameobject, deltaTime);
			}
		}
	}

	/* Tinkerbox.SmoothCamera
	*
	* A Smooth Following Camera With Settable Boundries
	*
	* This script can either grab the tagged player or
	* accept an assigned transform in the inspector.
	*
	* It zooms out when the target is moving fast to
	* allow the user to see what's coming up. It also
	* has settable camera boundries to avoid showing off
	* unfinished areas of the level.
	*
	*/
	public class SmoothCamera {
		
		// Assignable Target, Leave null to user player tag...
		public Transform target;
		public Rect boundries = new Rect(0,0,10,10);
		
		//Tweakables
		public float movementLagFactor = 0.1f;
		public float maximumZoom = 1.1f;
		public float zoomSpeedThreshold = 0.1f;
		
		//We keep some initial values around as a baseline value.
		private float initialZoom;
		
		//We use this to calculate zoom
		//private Vector2 lastPlayerPosition;
		
		public SmoothCamera(Transform p_target, Rect p_boundries, float p_movementLagFactor, float p_maximumZoom, float p_zoomSpeedThreshold) {
			target=p_target;
			if(target == null) {
				try {
					target = GameObject.FindWithTag("Player").transform;
				} catch {
					Debug.LogError("Nothing in this scene has a player tag, and the target of the camera has not been assigned");
				}
			}
			boundries=p_boundries;
			movementLagFactor=p_movementLagFactor;
			maximumZoom=p_maximumZoom;
			zoomSpeedThreshold=p_zoomSpeedThreshold;
			
			//initialization
			initialZoom = Camera.main.orthographicSize;
		}

		public void Paivita() {
			if(target!=null) {
				//Quadratic interpolation of camera position.
				Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, target.position, movementLagFactor);
				//Reset Z distance for 2D
				Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -10);
			}
		}
	}

	//************** inspector *****************************
	//parametriluokkia inspectoriin ja rakentajia
	//parametriluokkaan asettuu parametrit inspectorista ja rakentajalla niistä rakennetaan elementtejä
	//parametritluokat
	[System.Serializable]
	public enum Kylla_Ei {
		ei,
		kylla
	}
	[System.Serializable]
	public class Prefab_and_instances_Parametrit_Hallintapaneeliin {
		public List<GameObject> targetPrefabs;
		public List<string> targetPrefabs_with_Tags;
		public Rect allowed_area;
		public Moving_Gameobject_Parametrit_Hallintapaneeliin moving_parameters;
		public ForDestroyingInstantiated_Parametrit_Hallintapaneeliin handling_instantiate_in_destroying;
		public bool luo_prefabille_spawneri;
		public GameObject_Spawner_Parametrit_Hallintapaneeliin spawnerin_asetukset;
	}
	[System.Serializable]
	public class Moving_Gameobject_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum MovingMode {
			ei_kaytossa,
			only_rotation,
			MoveToSelectedDirection,
			MoveToSelectedPosition,
			MoveToSelectedPosition_and_ThenDestroying,
			MoveTowardSelectedTarget,
			MoveTowardClosestTarget
		}
		public MovingMode moving_mode;

		public float speed;
		public float turn_speed;
		public float liipaisu_etaisyys_kohdepositionista_vain_MoveToSelectedPosition_and_ThenDestroying=0.1f;
		public Kylla_Ei add_rotation;
		public Rotate_GameObject_Parametrit_Hallintapaneeliin rotation_parameters;
	}
	[System.Serializable]
	public class Rotate_GameObject_Parametrit_Hallintapaneeliin {
		public Vector3 eulerAngles;
		public bool aseta_ennen_kiertoa_kulkusuuntaan;
	}
	[System.Serializable]
	public class ForDestroyingInstantiated_Parametrit_Hallintapaneeliin {
		public GameObject prefab_to_instantiate=null;
		public bool destroy_instantiated_gameobject;
		public float time_to_destroy_instantiated_gameobject;
		public bool koordinaattien_maaritys_vapaasti;
		public Koordinaattiasetukset_Hallintapaneeliin instantiate_koordinaatin_maaritys;
		public Rotationasetukset_Hallintapaneeliin instantiate_rotation_maaritys;
	}
	[System.Serializable]
	public class GameObject_Spawner_Parametrit_Hallintapaneeliin {
		public GameObject_SpawnerTimer_Parametrit_Hallintapaneeliin timer;
		public Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin randoming_initial_point;
		public Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin randoming_target_point;
		public bool set_random_speed;
		public float random_speed_min;
		public float random_speed_max;
		public List<GameObject> lisaa_muita_prefabeja;
		public bool aseta_tapahtumien_rekisterointi;
		public Toiminnon_kohteen_Valitsin_Kaikille tapahtumien_rekisterointi;
	}
	[System.Serializable]
	public class GameObject_SpawnerTimer_Parametrit_Hallintapaneeliin {
		public float delaytime=1;
		public int min_nmb_of_gameobjects_to_spawn_at_time=1;
		public int max_nmb_of_gameobjects_to_spawn_at_time=1;
	}
	[System.Serializable]
	public class Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public class Tormaysjoukko {
			public List<GameObject> prefab_list;
			public List<string> prefab_tags;
		}
		public Tormaysjoukko joukko1;
		public Tormaysjoukko joukko2;
		[System.Serializable]
		public enum Col_event {
			OnCollisionEnter,
			OnCollisionExit,
			OnCollisionStay,
			OnTriggerEnter,
			OnTriggerExit,
			OnTriggerStay,
			OnControllerColliderHit
		}
		public List<Col_event> col_events;
		public bool destroy_gameobject;
		public bool destroy_other_gameobject;
		public Kylla_Ei instantiate_new_prefab;
		public ForDestroyingInstantiated_Parametrit_Hallintapaneeliin prefab_to_instantiate;
		[System.Serializable]
		public enum Instantiate_Kohdistus {
			asetus_muualla,
			joukon1_mukaan,
			joukon2_mukaan
		}
		public Instantiate_Kohdistus instantiate_kohdistus; //voidaan kohdistaa syntyvä palikka jomman kumman törmääjän mukaan vapaasti
		public bool suorita_lisatoiminto;
		public bool aseta_tapahtumien_rekisterointi;
		public Toiminnon_kohteen_Valitsin_Kaikille tapahtumien_rekisterointi;
	}
	[System.Serializable]
	public class Koordinaattiasetukset_Hallintapaneeliin {
		[System.Serializable]
		public enum Maaritystapa {
			absoluuttinen, //annetaan absoluuttisesti
			suhteessa_transform_positioniin, //annetaan siirtymä transform.position:sta
			suhteessa_transform_positionin_forwardiin, //annetaan siirtymä tranform.position:sta forward:n suuntaan (annetut koordinaatit skaalaavat forward-vectorin)
			suhteessa_transform_positionin_rightiin //annetaan siirtymä tranform.position:sta right:n suuntaan (annetut koordinaatit skaalaavat forward-vectorin)
		}
		public Maaritystapa maaritystapa;
		public Vector2 koordinaattiparametri;
	}
	[System.Serializable]
	public class Rotationasetukset_Hallintapaneeliin {
		public enum Maaritystapa {
			quaternion_identity,
			absoluuttinen,
			suhteessa_transform_rotationiin
		}
		public Maaritystapa maaritystapa;
		public Vector3 kiertoparametri;
	}
	[System.Serializable]
	public class SmoothCamera_Asetukset_Hallintapaneeliin {
		public bool kamera_kaytossa=true;
		// Assignable Target, Leave null to user player tag...
		public Transform target;
		public Rect boundries = new Rect(0,0,10,10);
		
		//Tweakables
		public float movementLagFactor = 0.1f;
		public float maximumZoom = 1.1f;
		public float zoomSpeedThreshold = 0.1f;
	}

	//************* rakentajat *********************
	public class Prefab_and_instances_Hallintapaneelista_Rakentaja {
		public Target_ohjaus.GameObject_Spawner gameobject_spawner=null; //spawneri talteen, josta siihen päästään käsiksi

		//suoritetaan awake()-funktion kautta
		public Prefab_and_instances Rekisteroi_Prefab(GameObject thePrefab) {
			Prefab_and_instances prefab_and_instances=Target_ohjaus.Instance.Get_Prefab_with_Name(thePrefab.name);
			if(prefab_and_instances==null) { //rekisteröidään vain kerran (ekalla instanssilla)
				prefab_and_instances=new Prefab_and_instances(thePrefab);
				Target_ohjaus.Instance.gameObject_Prefabs.Add(prefab_and_instances);
				if(!Target_ohjaus.Instance.clean_suoritettu) //clean suorittamatta -> lisätään toiseen listaan, koska varsinainen lista tyhjennetään clean:ssä
					Target_ohjaus.Instance.gameObject_Prefabs_before_cleaning.Add(prefab_and_instances);
			}
			return prefab_and_instances;
		}
		//suoritetaan start()-funktion kautta, kun kaikki prefabit on rekisteröity target-ohjaukseen
		public Prefab_and_instances Rakenna(Prefab_and_instances_Parametrit_Hallintapaneeliin parametrit, GameObject thePrefab) {
			Prefab_and_instances prefab_and_instances=Target_ohjaus.Instance.Get_Prefab_with_Name(thePrefab.name); //otetaan lisätty olio hallittavaksi
			//lisätään loput rojut olioon
			List<Prefab_and_instances> target_prefabs=Tee_Lista_Gameobject_ja_Tag_Listasta(parametrit.targetPrefabs_with_Tags, parametrit.targetPrefabs);
			Prefab_and_instances.Moving_Gameobject moving_handling=(new Moving_Gameobject_Hallintapaneelista_Rakentaja()).Rakenna(parametrit.moving_parameters);
			ForDestroyingInstantiated handling_prefab_to_instantiate_in_destroying=(new ForDestroyingInstantiated_Hallintapaneelista_Rakentaja()).Rakenna(parametrit.handling_instantiate_in_destroying);
			prefab_and_instances.TaydennaParametrit(target_prefabs, parametrit.allowed_area, moving_handling, handling_prefab_to_instantiate_in_destroying);
			//luodaan spawneri
			if(parametrit.luo_prefabille_spawneri)
				gameobject_spawner=new GameObject_Spawner_Hallintapaneelista_Rakentaja().Rakenna(parametrit.spawnerin_asetukset, thePrefab);
			return prefab_and_instances;
		}

		//palauttaa prefabit List<Prefab_and_instances>:na
		//rekisteröi prefabin target-ohjauksen Prefab_and_instances-listaan, mikäli sitä ei siellä ole
		//puuttuvan rekisteröinti tehdään vain mikäli prefab annetaan gameobjectina eikä tagina
		public static List<Prefab_and_instances> Tee_Lista_Gameobject_ja_Tag_Listasta(List<string> Prefabs_with_Tags, List<GameObject> Prefabs) {
			List<Prefab_and_instances> prefab_and_instances=new List<Prefab_and_instances>();
			Prefab_and_instances_Hallintapaneelista_Rakentaja prefab_and_instances_rakentaja=new Prefab_and_instances_Hallintapaneelista_Rakentaja();
			//tagi lista läpi
			Prefabs_with_Tags.ForEach(delegate(string obj) {
				prefab_and_instances.AddRange(Target_ohjaus.Instance.Get_Prefabs_with_Tag(obj));
			});
			//gameobject-lista läpi
			Prefab_and_instances item;
			Prefabs.ForEach(delegate(GameObject obj) {
				item=Target_ohjaus.Instance.Get_Prefab_with_Name(obj.name);
				if(item==null) //jos puuttuu
					item=prefab_and_instances_rakentaja.Rekisteroi_Prefab(obj); //luodaan prefab_and_instances-olio ilman asetuksia. asetukset ajetaan sitten, kun ensimmäinen instanssi luodaan
				prefab_and_instances.Add(item);
			});
			return prefab_and_instances;
		}
	}
	public class Moving_Gameobject_Hallintapaneelista_Rakentaja {
		public Prefab_and_instances.Moving_Gameobject Rakenna(Moving_Gameobject_Parametrit_Hallintapaneeliin parametrit) {
			Prefab_and_instances.Rotate_GameObject rotationer=null;
			if(parametrit.add_rotation==Kylla_Ei.kylla)
				rotationer=(new Rotate_GameObject_Hallintapaneelista_Rakentaja()).Rakenna(parametrit.rotation_parameters);
			if(parametrit.moving_mode==Moving_Gameobject_Parametrit_Hallintapaneeliin.MovingMode.only_rotation)
				return new Prefab_and_instances.Moving_Gameobject(parametrit.speed, parametrit.turn_speed, rotationer);
			else if(parametrit.moving_mode==Moving_Gameobject_Parametrit_Hallintapaneeliin.MovingMode.MoveToSelectedDirection)
				return new Prefab_and_instances.MoveToSelectedDirection(parametrit.speed, parametrit.turn_speed, rotationer);
			else if(parametrit.moving_mode==Moving_Gameobject_Parametrit_Hallintapaneeliin.MovingMode.MoveToSelectedPosition)
				return new Prefab_and_instances.MoveToSelectedPosition(parametrit.speed, parametrit.turn_speed, rotationer);
			else if(parametrit.moving_mode==Moving_Gameobject_Parametrit_Hallintapaneeliin.MovingMode.MoveToSelectedPosition_and_ThenDestroying) {
				Prefab_and_instances.MoveToSelectedPosition_and_ThenDestroying moving = new Prefab_and_instances.MoveToSelectedPosition_and_ThenDestroying(parametrit.speed, parametrit.turn_speed, rotationer);
				moving.AsetaLiipaisuetaisyys_kohdepositionista(parametrit.liipaisu_etaisyys_kohdepositionista_vain_MoveToSelectedPosition_and_ThenDestroying);
				return moving;
			} else if(parametrit.moving_mode==Moving_Gameobject_Parametrit_Hallintapaneeliin.MovingMode.MoveTowardSelectedTarget)
				return new Prefab_and_instances.MoveTowardSelectedTarget(parametrit.speed, parametrit.turn_speed, rotationer);
			else if(parametrit.moving_mode==Moving_Gameobject_Parametrit_Hallintapaneeliin.MovingMode.MoveTowardClosestTarget)
				return new Prefab_and_instances.MoveTowardClosestTarget(parametrit.speed, parametrit.turn_speed, rotationer);
			else
				return null; // ei käytössä
		}
	}
	public class Rotate_GameObject_Hallintapaneelista_Rakentaja {
		public Prefab_and_instances.Rotate_GameObject Rakenna(Rotate_GameObject_Parametrit_Hallintapaneeliin parametrit) {
			if(parametrit.aseta_ennen_kiertoa_kulkusuuntaan==true)
				return new Prefab_and_instances.SetRotationFirst_to_Direction_and_Then_Rotate(parametrit.eulerAngles);
			else
				return new Prefab_and_instances.Rotate_GameObject(parametrit.eulerAngles);
		}
	}
	public class ForDestroyingInstantiated_Hallintapaneelista_Rakentaja {
		public ForDestroyingInstantiated Rakenna(ForDestroyingInstantiated_Parametrit_Hallintapaneeliin parametrit) {
			if(parametrit.prefab_to_instantiate!=null) {
				//koordinaattien määrittäjät
				Koordinaattiasetukset_Hallintapaneeliin instantiate_koordinaatin_maaritys=null; //mikäli on null, käytetään koordinaattia, joka annetaan istantiate-kutsussa
				Rotationasetukset_Hallintapaneeliin instantiate_rotation_maaritys=null;
				if(parametrit.koordinaattien_maaritys_vapaasti) {
					instantiate_koordinaatin_maaritys=parametrit.instantiate_koordinaatin_maaritys;
					instantiate_rotation_maaritys=parametrit.instantiate_rotation_maaritys;
				}
				return new ForDestroyingInstantiated(parametrit.prefab_to_instantiate, parametrit.destroy_instantiated_gameobject, parametrit.time_to_destroy_instantiated_gameobject, instantiate_koordinaatin_maaritys, instantiate_rotation_maaritys);
			} else
				return null;
		}
	}
	public class GameObject_Spawner_Hallintapaneelista_Rakentaja {
		public Target_ohjaus.GameObject_Spawner Rakenna(GameObject_Spawner_Parametrit_Hallintapaneeliin parametrit, GameObject thePrefab) {
			parametrit.lisaa_muita_prefabeja.Add(thePrefab);
			List<Prefab_and_instances> prefabs_to_spawn=Prefab_and_instances_Hallintapaneelista_Rakentaja.Tee_Lista_Gameobject_ja_Tag_Listasta(new List<string>(), parametrit.lisaa_muita_prefabeja);
			Get_RandomPoint_from_Shape randoming_initial_point=new Get_RandomPoint_from_Shape_Hallintapaneelista_Rakentaja().Rakenna(parametrit.randoming_initial_point);
			Get_RandomPoint_from_Shape randoming_target_point=new Get_RandomPoint_from_Shape_Hallintapaneelista_Rakentaja().Rakenna(parametrit.randoming_target_point);
			Target_ohjaus.GameObject_Spawner spawner=new Target_ohjaus.GameObject_Spawner(prefabs_to_spawn, new GameObject_SpawnerTimer_Hallintapaneelista_Rakentaja().Rakenna(parametrit.timer), randoming_initial_point, randoming_target_point, parametrit.set_random_speed, parametrit.random_speed_min, parametrit.random_speed_max);
			Target_ohjaus.Instance.gameObject_Spawners.Add(spawner);
			return spawner;
		}
	}
	public class GameObject_SpawnerTimer_Hallintapaneelista_Rakentaja {
		public Target_ohjaus.GameObject_Spawner.Spawner_Timer  Rakenna(GameObject_SpawnerTimer_Parametrit_Hallintapaneeliin parametrit) {
			return new Target_ohjaus.GameObject_Spawner.Spawner_Timer(parametrit.delaytime, parametrit.min_nmb_of_gameobjects_to_spawn_at_time, parametrit.max_nmb_of_gameobjects_to_spawn_at_time);
		}
	}
	public class Collisions_and_Triggers_Handling_Hallintapaneelista_Rakentaja {
		public delegate Target_ohjaus.Collisions_and_Triggers_Handling.ToiminnonSuorittaja LisatoiminnonSuorittajaDelegaatinPalauttaja(); //palauttaa delegaatin lisätoiminnan suorittajadelegaatin palauttajaan

		public Target_ohjaus.Collisions_and_Triggers_Handling Rakenna(Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin parametrit, GameObject tormays_prefab, LisatoiminnonSuorittajaDelegaatinPalauttaja lisatoiminto_delegaatin_palauttaja) {
			List<Prefab_and_instances> list_prefab_and_instances=Prefab_and_instances_Hallintapaneelista_Rakentaja.Tee_Lista_Gameobject_ja_Tag_Listasta(parametrit.joukko1.prefab_tags, parametrit.joukko1.prefab_list);
			List<Prefab_and_instances> list_other_prefab_and_instances=Prefab_and_instances_Hallintapaneelista_Rakentaja.Tee_Lista_Gameobject_ja_Tag_Listasta(parametrit.joukko2.prefab_tags, parametrit.joukko2.prefab_list);
			List<int> col_events=new List<int>();
			parametrit.col_events.ForEach(delegate(Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event obj) {
				if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnCollisionEnter)
					col_events.Add(Target_ohjaus.int_OnCollisionEnter);
				else if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnCollisionExit)
					col_events.Add(Target_ohjaus.int_OnCollisionExit);
				else if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnCollisionStay)
					col_events.Add(Target_ohjaus.int_OnCollisionStay);
				else if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnTriggerEnter)
					col_events.Add(Target_ohjaus.int_OnTriggerEnter);
				else if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnTriggerExit)
					col_events.Add(Target_ohjaus.int_OnTriggerExit);
				else if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnTriggerStay)
					col_events.Add(Target_ohjaus.int_OnTriggerStay);
				else if(obj==Collisions_and_Triggers_Handling_Parametrit_Hallintapaneeliin.Col_event.OnControllerColliderHit)
					col_events.Add(Target_ohjaus.int_OnControllerColliderHit);
			});
			//tarkistukset
			if(list_prefab_and_instances.Count==0 || list_other_prefab_and_instances.Count==0) Debug.LogWarning("Tormaysjoukon listassa ei ole yhtaan osapuolta! : " + tormays_prefab.name);
			if(col_events.Count==0) Debug.LogError("Yhtaan tormaystapahtuman tyyppia ei ole maaritetty!");
			ForDestroyingInstantiated prefab_to_instantiate_handling=null;
			if(parametrit.instantiate_new_prefab==Kylla_Ei.kylla)
				prefab_to_instantiate_handling=new ForDestroyingInstantiated_Hallintapaneelista_Rakentaja().Rakenna(parametrit.prefab_to_instantiate);
			//otetaan mahdollinen viittaus lisätoiminnon suorittajaan
			Target_ohjaus.Collisions_and_Triggers_Handling.ToiminnonSuorittaja toiminto=null;
			if(parametrit.suorita_lisatoiminto) {
				toiminto=lisatoiminto_delegaatin_palauttaja(); //palauttaa delegaatin lisätoiminnon suorittajaan
			}
			Target_ohjaus.Collisions_and_Triggers_Handling handling=new Target_ohjaus.Collisions_and_Triggers_Handling(list_prefab_and_instances, list_other_prefab_and_instances, col_events, parametrit.destroy_gameobject, parametrit.destroy_other_gameobject, prefab_to_instantiate_handling, toiminto, parametrit.instantiate_kohdistus);
			Target_ohjaus.Instance.collisions_and_Triggers_Handlers.Add(handling);
			return handling;
		}
	}
	public class Koordinaattiasetukset_Hallintapaneelista_Rakentaja {
		public Vector2 Rakenna(Koordinaattiasetukset_Hallintapaneeliin parametrit, Transform transform) {
			Vector2 koordinaatti=new Vector2();
			if(parametrit.maaritystapa==Koordinaattiasetukset_Hallintapaneeliin.Maaritystapa.absoluuttinen)
				koordinaatti=parametrit.koordinaattiparametri;
			else if(parametrit.maaritystapa==Koordinaattiasetukset_Hallintapaneeliin.Maaritystapa.suhteessa_transform_positioniin) {
				koordinaatti=Ohjattava_2D.Convert_to_Vector2(transform.position);
				koordinaatti+=parametrit.koordinaattiparametri; //lisätään positioniin koordinaattiparametri
			} else if(parametrit.maaritystapa==Koordinaattiasetukset_Hallintapaneeliin.Maaritystapa.suhteessa_transform_positionin_forwardiin) {
				koordinaatti=LaskeKoordinaatitVektorinSuuntaan(transform.position, transform.forward, parametrit.koordinaattiparametri);
			} else if(parametrit.maaritystapa==Koordinaattiasetukset_Hallintapaneeliin.Maaritystapa.suhteessa_transform_positionin_rightiin) {
				koordinaatti=LaskeKoordinaatitVektorinSuuntaan(transform.position, transform.right, parametrit.koordinaattiparametri);
			}
			return koordinaatti;
		}

		public Vector2 LaskeKoordinaatitVektorinSuuntaan(Vector2 initial_position, Vector2 suuntavektori, Vector2 skaalaaja) {
			initial_position.x+=suuntavektori.x*skaalaaja.x;
			initial_position.y+=suuntavektori.y*skaalaaja.y;
			return initial_position;
		}
	}
	public class Rotationasetukset_Hallintapaneelista_Rakentaja {
		public Quaternion Rakenna(Rotationasetukset_Hallintapaneeliin parametrit, Transform transform) {
			Quaternion rotation=new Quaternion();
			if(parametrit.maaritystapa==Rotationasetukset_Hallintapaneeliin.Maaritystapa.quaternion_identity)
				rotation=Quaternion.identity;
			else if(parametrit.maaritystapa==Rotationasetukset_Hallintapaneeliin.Maaritystapa.absoluuttinen)
				rotation=Quaternion.Euler(parametrit.kiertoparametri);
			else if(parametrit.maaritystapa==Rotationasetukset_Hallintapaneeliin.Maaritystapa.suhteessa_transform_rotationiin)
				rotation=Quaternion.Euler(transform.rotation.eulerAngles+parametrit.kiertoparametri);
			return rotation;
		}
	}
	public class SmoothCamera_Hallintapaneelista_Rakentaja {
		public SmoothCamera Rakenna(SmoothCamera_Asetukset_Hallintapaneeliin parametrit) {
			return new SmoothCamera(parametrit.target, parametrit.boundries, parametrit.movementLagFactor, parametrit.maximumZoom, parametrit.zoomSpeedThreshold);
		}
	}
}

//tässä on koodattu rajapinta Target-ohjauksen liittämiseksi levelin hallintaan
namespace Target_ohjaus_Level_hallintaan {
	using Level_hallinta;
	using Target_ohjaus;

	//pitää sisällään instanssikohtaisen tilan
	//antaa tilan mikäli on kyseinen instanssi
	public class Instanssikohtainen_Laskuri {
		public Laskuri laskuri;
		public Prefab_and_instances.Modified_GameObject modified_gameObject;

		public Instanssikohtainen_Laskuri(Laskuri p_laskuri, Prefab_and_instances.Modified_GameObject p_modified_gameObject) {
			laskuri=p_laskuri;
			modified_gameObject=p_modified_gameObject;
		}

		public Tila AnnaLaskuri_JosKuuluuInstanssille(Prefab_and_instances.Modified_GameObject p_modified_gameObject) {
			if(p_modified_gameObject.Equals(modified_gameObject))
				return laskuri;
			else
				return null;			
		}
	}

	public class InstanssikohtainenLaskuriHallitsija {
		public Laskuri edustaja; //laskuri, joka edustaa prefabin kaikkia laskureita ja antaa tilan levelin tilanhallitsijalle (complete ja tai failded, jos asetettu)
		public List<Instanssikohtainen_Laskuri> instanssikohtaiset_laskurit;
		public Instanssikohtainen_Laskuri edel_paivitetty=null; //edellisen kerran toimenpiteen (laskurin päivityksen) kohteena ollut instanssi
		private int seur_id=0;

		public InstanssikohtainenLaskuriHallitsija(Laskuri p_edustaja) {
			edustaja=p_edustaja;
			instanssikohtaiset_laskurit=new List<Instanssikohtainen_Laskuri>();
		}

		public Instanssikohtainen_Laskuri AnnaInstanssikohtainenLaskuri(Prefab_and_instances.Modified_GameObject p_modified_gameObject) {
			Instanssikohtainen_Laskuri laskuri=instanssikohtaiset_laskurit.Find(delegate(Instanssikohtainen_Laskuri obj) {
				return obj.modified_gameObject.Equals(p_modified_gameObject);
			});
			if(laskuri==null) { //instanssia ei löydy -> luodaan
				laskuri=new Instanssikohtainen_Laskuri(edustaja.AnnaKopio(seur_id), p_modified_gameObject);
				seur_id++;
				instanssikohtaiset_laskurit.Add(laskuri);
				edustaja.arvo=laskuri.laskuri.arvo; //asetetaan arvo edustajaan, koska on lähimpänä alkutilaa
			}
			return laskuri;
		}

		//päivittää edustajan arvon, mikäli päivitetty laskurin tila oli lähimpänä alkutilaa
		//eli siis päivitettyä laskuria aiempaa tilaa ei ole missään laskurissa
		//päivitetyn laskurin paikka listassa päivitetään, että lista pysyy järjestyksessä (lopussa on aiemmat tilat)
		//poistaa myös aiemmin käsitellyn instanssin, mikäli instanssi (gameobject) on tuhoutunut (poistettu)
		public void PaivitaEdustajanTila(Laskuri paivitettu_laskuri) {
			//tarkistetaan onko aiemmin käsitelty instanssi poistettu (tuhoutunut)
			if(edel_paivitetty!=null)
				if(edel_paivitetty.modified_gameObject.IsDestroyed()) instanssikohtaiset_laskurit.Remove(edel_paivitetty);
			//varsinainen pävitysrutiini
			if(instanssikohtaiset_laskurit.Count==1) { //erikoistapaus: vain yksi
				edustaja.arvo=paivitettu_laskuri.arvo;
				edel_paivitetty=instanssikohtaiset_laskurit[0]; //otetaan käsittelyn kohteena oleva instanssi talteen
			} else { //enemmän kuin 1
				int paivitetyn_ind=instanssikohtaiset_laskurit.FindIndex(delegate(Instanssikohtainen_Laskuri obj) {
					return obj.laskuri.Equals(paivitettu_laskuri);
				});
				edel_paivitetty=instanssikohtaiset_laskurit[paivitetyn_ind]; //otetaan käsittelyn kohteena oleva instanssi talteen
				if(paivitetyn_ind==(instanssikohtaiset_laskurit.Count-1)) { //saattoi olla lähimpänä alkutilaa -> verrataan seuraavaa
					if(instanssikohtaiset_laskurit[paivitetyn_ind-1].laskuri.arvo==paivitettu_laskuri.arvo) { //oli
						edustaja.arvo=paivitettu_laskuri.arvo;
					}
				} else { //etsitään päivitetylle uusi paikka -> lista järjestyksessä
					int uusi_ind=instanssikohtaiset_laskurit.FindIndex(delegate(Instanssikohtainen_Laskuri obj) {
						return obj.laskuri.arvo==paivitettu_laskuri.arvo;
					});
					Instanssikohtainen_Laskuri laskuri=instanssikohtaiset_laskurit[paivitetyn_ind];
					instanssikohtaiset_laskurit.RemoveAt(paivitetyn_ind);
					instanssikohtaiset_laskurit.Insert(uusi_ind, laskuri);
				}
			}
		}
	}

	//luokka Target_ohjaukselle tapahtumien rekisteröintiin: ei toimintoja
	//tarkoituksena ettei tarvitse liittää varsinaista levelin_hallintaa Target_ohjaukselle, vaan vain tämä rajapinta
	public class Target_ohjaus_Tapahtumien_rekisteroija<T> : Tapahtumien_rekisteroija<T> {
		public Target_ohjaus_Tapahtumien_rekisteroija(Toiminnon_suorittaja p_toiminto, bool ohita_levelin_tila=false) :base(p_toiminto, ohita_levelin_tila) {}
	}

	//suorittaa toiminnot, jotka rekisteröi Target-ohjauksen jokin luokka
	//Target-ohjauksen luokat, jotka rekisteröivät tapahtumia levelin hallintaan, täytyy periyttää tästä
	public abstract class RekisteroitavanTapahtumanSuorittaja<T> {
		public Target_ohjaus_Tapahtumien_rekisteroija<T> tapahtuman_rekisteroija=null; //rekisteröi tapahtuman levelin hallintaan
		
		//asettaa tapahtuman rekisteröijän tapahtuman suorittajalle ja asettaa kohdeolioksi tapahtuman suorittajan (kutsuja)
		public virtual void AsetaTapahtumanRekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<T> p_tapahtuman_rekisteroija, T kohde) {
			tapahtuman_rekisteroija=p_tapahtuman_rekisteroija;
			if(tapahtuman_rekisteroija!=null)
				tapahtuman_rekisteroija.AsetaKohde(kohde);
		}
		
		public bool RekisteroiTapahtuma() {
			if(tapahtuman_rekisteroija!=null) {
				return tapahtuman_rekisteroija.Rekisteroi();
			}
			else
				return true; // toiminnon saa suorittaa jos ei ole asetettu tapahtuman rekisteröijää
		}

		public Target_ohjaus_Tapahtumien_rekisteroija<T> TarkistaTapahtumanRekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<T> p_tapahtuman_rekisteroija) {
			if(p_tapahtuman_rekisteroija!=null)
				if(p_tapahtuman_rekisteroija.toiminto!=null)
					return p_tapahtuman_rekisteroija; //ok
			return null; //puutteellinen
		}
	}
	//Prefab_and_instances luokka rekisteröi 2 toimintoa (instantiate ja destroy), joten vaatii oman toteutuksen
	public abstract class RekisteroitavanTapahtumanSuorittaja_for_Prefab_and_instances : RekisteroitavanTapahtumanSuorittaja<Prefab_and_instances> {
		public Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> tapahtuman_rekisteroija_for_instantiate=null; //rekisteröi instantiate-tapahtuman levelin hallintaan
		public Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> tapahtuman_rekisteroija_for_destroy=null; //rekisteröi destroy-tapahtuman levelin hallintaan
		public Prefab_and_instances.Modified_GameObject kohde_Modified_GameObject; //Modified_GameObject, joka rekisteröinnin aiheutti
		
		//asettaa tapahtuman rekisteröijän tapahtuman suorittajalle ja asettaa kohdeolioksi tapahtuman suorittajan (kutsuja)
		public void AsetaTapahtumanRekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> p_tapahtuman_rekisteroija_for_instantiate, Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> p_tapahtuman_rekisteroija_for_destroy, Prefab_and_instances kohde) {
			tapahtuman_rekisteroija_for_instantiate=TarkistaTapahtumanRekisteroija(p_tapahtuman_rekisteroija_for_instantiate);
			tapahtuman_rekisteroija_for_destroy=TarkistaTapahtumanRekisteroija(p_tapahtuman_rekisteroija_for_destroy);
			base.AsetaTapahtumanRekisteroija(tapahtuman_rekisteroija_for_instantiate,kohde);
			base.AsetaTapahtumanRekisteroija(tapahtuman_rekisteroija_for_destroy,kohde);
		}
		
		public bool RekisteroiTapahtuma_instantiate() {
			tapahtuman_rekisteroija=tapahtuman_rekisteroija_for_instantiate; //asetetaan rekisteröitävä tapahtuma
			kohde_Modified_GameObject=null; //item:a ei ole olemassa ennen rekisteröintiä
			return RekisteroiTapahtuma();
		}
		
		public bool RekisteroiTapahtuma_destroy(Prefab_and_instances.Modified_GameObject p_kohde_Modified_GameObject) {
			tapahtuman_rekisteroija=tapahtuman_rekisteroija_for_destroy; //asetetaan rekisteröitävä tapahtuma
			kohde_Modified_GameObject=p_kohde_Modified_GameObject;
			return RekisteroiTapahtuma();
		}
	}
	
	//vaihtaa prefabin riippuen eri laskrin arvoista
	//tarkoitettu Target-ohjuksen Prefab_and_instances-luokalle
	public class Vaihda_prefab : Muuta_laskuria {
		public Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> tapahtuman_rekisteroija; //asetetaan sen prefabin, jonka instanssi vaihdetaan
		public InstanssikohtainenLaskuriHallitsija instanssi_kohtainen_laskurihallitsija;
		public List<Prefabin_vaihto_laskurin_arvolla> vaihtolista; //lista, josta korvaavia prefabeja poimitaan
		public List<Prefab_and_instances.Modified_GameObject> korvatut_instanssit; //lista gameobjecteista, jotka on korvattu uudella (palautusta varten)
		
		public Vaihda_prefab(Laskuri p_toiminnon_kohde) :base(p_toiminnon_kohde) {
			instanssi_kohtainen_laskurihallitsija=new InstanssikohtainenLaskuriHallitsija(toiminnon_kohde);
			instanssi_kohtainen_laskurihallitsija.edustaja=p_toiminnon_kohde;
			vaihtolista=new List<Prefabin_vaihto_laskurin_arvolla>();
			korvatut_instanssit=new List<Prefab_and_instances.Modified_GameObject>();
		}
		
		//asettaa tapahtumien rekisteröijän
		public void AsetaTapahtumien_rekisteroija(Target_ohjaus_Tapahtumien_rekisteroija<Prefab_and_instances> p_tapahtuman_rekisteroija) {
			tapahtuman_rekisteroija=p_tapahtuman_rekisteroija;
		}
		
		public void LisaaVaihto(int laskurin_arvo, GameObject the_prefab) {
			vaihtolista.Add(new Prefabin_vaihto_laskurin_arvolla(laskurin_arvo, the_prefab));
		}
		
		//asettaa alkutilaan
		public void Nollaa() {			
			korvatut_instanssit.ForEach(delegate(Prefab_and_instances.Modified_GameObject obj) {
				obj.Change_the_Prefab(tapahtuman_rekisteroija.kohde_olio.thePrefab);
			});
			toiminnon_kohde.Nollaa();
		}

		public override bool Suorita() {
			//asetetaan instanssikohtainen laskuri ja päivitetään se
			toiminnon_kohde=instanssi_kohtainen_laskurihallitsija.AnnaInstanssikohtainenLaskuri(tapahtuman_rekisteroija.kohde_olio.kohde_Modified_GameObject).laskuri;
			bool paluu=base.Suorita(); // päivittää laskurin
			//prefabin vaihto?
			Prefabin_vaihto_laskurin_arvolla vaihtaja=vaihtolista.Find(delegate(Prefabin_vaihto_laskurin_arvolla obj) {
				return obj.laskurin_arvo==toiminnon_kohde.arvo;
			});
			if(vaihtaja!=null) { //vaihdetaan
			   	tapahtuman_rekisteroija.kohde_olio.kohde_Modified_GameObject.Change_the_Prefab(vaihtaja.the_prefab);
				korvatut_instanssit.Add(tapahtuman_rekisteroija.kohde_olio.kohde_Modified_GameObject);
			}
			//asetetaan edustajaan tila, joka edustaa kaikkien instanssien tiloja
			//tämä tila vaikuttaa levelin tilaan (complete tai failed), mikäli on asetettu
			instanssi_kohtainen_laskurihallitsija.PaivitaEdustajanTila(toiminnon_kohde);
			return paluu;
		}
		
		//sisältää prefabit, jotka vaihdetaan ari laskurin arvoilla
		public class Prefabin_vaihto_laskurin_arvolla {
			public int laskurin_arvo;
			public GameObject the_prefab;
			
			public Prefabin_vaihto_laskurin_arvolla(int p_laskurin_arvo, GameObject p_the_prefab) {
				laskurin_arvo=p_laskurin_arvo;
				the_prefab=p_the_prefab;
			}
		}
	}

	//************** inspector *****************************
	//parametriluokkia inspectoriin ja rakentajia
	//parametriluokkaan asettuu parametrit inspectorista ja rakentajalla niistä rakennetaan elementtejä
	//parametritluokat
	[System.Serializable]
	public enum Kylla_Ei {
		ei,
		kylla
	}
	[System.Serializable]
	public class Vaihda_prefab_Parametrit_Hallintapaneeliin : Muuta_laskuria_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public class Prefabin_vaihto_laskurin_arvolla {
			public int laskurin_arvo;
			public GameObject the_prefab;
		}
		public List<Prefabin_vaihto_laskurin_arvolla> vaihtolista;
	}
	[System.Serializable]
	public class Toiminnon_kohteen_Valitsin_Kaikille : Toiminnon_kohteen_Valitsin {} //rajapinta levelinhallinnan luokkaan
	[System.Serializable]
	public class Toiminnon_kohteen_Valitsin_for_Prefab_and_Instances : Toiminnon_kohteen_Valitsin_Kaikille {
		[System.Serializable]
		public enum Tyyppilista2 {
			Valitse_listasta_1,
			Vaihda_prefab
		}
		public Tyyppilista2 tyyppilista2; //valitaan toiminnon kohteen tyyppi
	}

	//************* rakentajat *********************
	public class Vaihda_prefab_Hallintapaneelista_Rakentaja : Muuta_laskuria_Hallintapaneelista_Rakentaja {
		public Vaihda_prefab Rakenna(Vaihda_prefab_Parametrit_Hallintapaneeliin parametrit, GameObject maaritykset) {
			Vaihda_prefab vaihda_prefab;
			vaihda_prefab = new Vaihda_prefab(RakennaLaskuri(parametrit, maaritykset));
			AsetaParametrit(vaihda_prefab, parametrit);
			//lisätään vaihdot
			parametrit.vaihtolista.ForEach(delegate(Vaihda_prefab_Parametrit_Hallintapaneeliin.Prefabin_vaihto_laskurin_arvolla obj) {
				vaihda_prefab.LisaaVaihto(obj.laskurin_arvo, obj.the_prefab);
			});
			return vaihda_prefab;
		}
	}
}