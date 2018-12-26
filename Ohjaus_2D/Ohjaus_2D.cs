using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CoordinatesScaling;

//luokkakirjasto 2D-ohjauksen käyttöön. sisältää ohjauslaitteet: multitouch, hiiri ja näppäimistö
//pelipinnan z-koordinaatin on oltava 0!
namespace Ohjaus_2D {
	
	public class Alue {
		public Rect dimensio; //sijainti ja koko
		public string nimi; //kuvaava nimi
		public bool scaled; //skaalautuu resoluution mukaan

		public Alue(Rect p_dimensio, string p_nimi, bool p_scaled=false) {
			dimensio=p_dimensio;
			nimi=p_nimi;
			scaled=p_scaled;
		}

		//tarkistaa, osuuko koordinaatti alueen sisälle
		public virtual bool isInside(Vector2 position) {
			Rect scaled_dimensio;
			if(scaled==true)
				scaled_dimensio=new Rect(GUICoordinatesScaling.Instance.Scaling_rect(dimensio));
			else
				scaled_dimensio=dimensio;
			if(position.x > scaled_dimensio.x && position.x < (scaled_dimensio.x + scaled_dimensio.width) && position.y < (Screen.height - scaled_dimensio.y) && position.y > (Screen.height - scaled_dimensio.y - scaled_dimensio.height))
				return true;
			else
				return false;
		}
	}

	public class GUITexture_Alue : Alue {
		public GUITexture gui_texture;

		public GUITexture_Alue(GUITexture p_gui_texture, string p_nimi) :base(new Rect(), p_nimi, false) { //guitexture skaalautuu automaattisesti, joten skaalausta ei tarvita
			gui_texture=p_gui_texture;
		}

		//tarkistaa, osuuko koordinaatti alueen sisälle
		public override bool isInside(Vector2 position) {
			if(gui_texture.enabled)
				return gui_texture.HitTest(position);
			return false;
		}
	}

	//luokka ohjauselementille. sisältää elementin koordinaatit, mutta ei visuaalista esitystapaa. pitää sisällään myös elementtiin liitetyn ohjauksen tilan
	public class Ohjaus_elem_2D {
		public Alue alue;
		public List<TouchPhase> hyvaksytyt_kosketukset; //määrittävät ohjauksen tilat, jotka aiheuttavat ohjaustoiminnon (voi olla useita tiloja)
		public delegate void OhjausTapahtumien_Suorittaja(Ohjaustapahtuma kosketus); //suorittaa ohjaustapahtumaan liitetyn toiminnon
		public OhjausTapahtumien_Suorittaja ohjaustapahtumien_suorittaja;
		public bool aktiivinen; //ottaako elementti vastaan kosketuksia
		
		public Ohjaus_elem_2D(Rect p_dimensio, string p_nimi, OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja, bool p_scaled=false) {
			alue=new Alue(p_dimensio, p_nimi, p_scaled);
			Initialize(p_ohjaustapahtumien_suorittaja);
		}
		public Ohjaus_elem_2D(GUITexture p_gui_texture, string p_nimi, OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja) {
			alue=new GUITexture_Alue(p_gui_texture, p_nimi);
			Initialize(p_ohjaustapahtumien_suorittaja);
		}

		public void Initialize(OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja) {
			hyvaksytyt_kosketukset=new List<TouchPhase>();
			ohjaustapahtumien_suorittaja=p_ohjaustapahtumien_suorittaja;
			aktiivinen=true;
		}

		//määrittää, mitkä tilat aiheuttavat ohjaustoiminnon
		public void LisaaHyvaksyttyKosketus(TouchPhase phase) {
			hyvaksytyt_kosketukset.Add(phase);
		}
		
		//ottaa ohjauksen vastaan ja suorittaa siihen liitetyn toiminnon, mikäli kuuluu elementille ja ohjauksen tila on hyväksytty. tunnistus tehdään ohjauksen positionin perusteella
		public bool OtaKosketus (Ohjaustapahtuma kosketus) {
			if (alue.isInside(kosketus.position)) { // osuu
				KasitteleKosketus(kosketus);
				return true; // osuu
			} else return false;
		}
		//suorittaa toiminnon, mikäli ohjauksen tila on hyväksytty
		public void KasitteleKosketus(Ohjaustapahtuma kosketus) {
			if(aktiivinen && HyvaksyttyKosketus(kosketus) && (ohjaustapahtumien_suorittaja!=null)) //jos on aktiivinen ja tila on hyväksytty (ja on asetettu toiminnon suorittaja)
				ohjaustapahtumien_suorittaja(kosketus); //suorittaa toiminnon
		}
		
		//testataan ohjauksen tila
		public bool HyvaksyttyKosketus(Ohjaustapahtuma kosketus) {
			bool hyvaksytty=false;
			hyvaksytyt_kosketukset.ForEach(delegate(TouchPhase phase) {
				if (kosketus.phase==phase) hyvaksytty=true;
			});
			return hyvaksytty;
		}
	}

	//abstracti luokka Ohjaus_2D:tä käyttäville luokille (ne täytyy periyttää tästä)
	//kun ohjaustoimintoja pyydetän Ohjaus_2D:ltä, ne palautetaan tämän kautta
	//varsinaiset ohjaustoiminnot lisätään yksi kerrallaan kutsumalla Lisaa_Ohjaus_Input_elem(input_elem)-metodia
	//Ohjaus_2D:tä käyttävän clean()-metodissa on kutsuttava Clean_parent()
	public abstract class Ohjattava_2D {
		public List<Ohjaus_elem_2D> ohjaus_elementit=null; //ohjauselementit näytöllä, joilla ohjaaminen tapahtuu (voi olla myös virtuaalisia kuten näppäimistöohjauksen tapauksessa)
		public Ohjaus_2D_rajapinta ohjaus;

		public Ohjattava_2D() {
			ohjaus=Ohjaus_2D_rajapinta.Instance;
			Initialize_parent(); //parentilla tarkoitetaan Ohjattava_2D:ä
		}

		public abstract void SuoritaToiminnot(); // jokaisen on toteutettava funktio, joka suorittaa ohjattavan laitteen toiminnot sen jälkeen, kun ohjaustilat on päivitetty. tämä kutsutaan automaattisesti

		//siivoaa rojut pois ja tekee re-initialisoinnin
		//voidaan määrätä, siivotaanko myös ohjaus (vain ensimmäisen ohjattavan täytyy)
		public void Clean_parent(bool clean_ohjaus) {
			if(clean_ohjaus) ohjaus.Clean();
			if(ohjaus_elementit!=null) {
				ohjaus_elementit.Clear();
				ohjaus_elementit=null;
			}
			Initialize_parent();
		}

		public void Initialize_parent() { //parentilla tarkoitetaan Ohjattava_2D:ä
			ohjaus_elementit=new List<Ohjaus_elem_2D>();
			ohjaus.ohjaus_hallinta.Rekisteroi_ohjattava(this); //rekisteröidään tämä ohjattava laite
		}

		//ottaa vastaan metodin, jolle delegoidaan screen-ohjaustapahtumien suorittaminen
		public void Aseta_ScreenOhjaustapahtumien_Suorittaja(Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_screen_ohjaustapahtumien_suorittaja) {
			// luodaan oletuksena virtuaalinen ohjausnappula koko ruudulle (sijaintitiedot voi halutessaan vaihtaa)
			LisaaOhjaus_elem(new Ohjaus_elem_2D(new Rect(0, 0, GUICoordinatesScaling.Instance.originalWidth, GUICoordinatesScaling.Instance.originalHeight), "Screen-ohjaus", p_screen_ohjaustapahtumien_suorittaja, true));
			// määritetään hyväksytyt kosketukset, jotka aiheuttavat ohjaustoiminnon aktiiviseksi
			ohjaus_elementit[ohjaus_elementit.Count-1].LisaaHyvaksyttyKosketus(TouchPhase.Ended);
		}
		
		// lisää ohjauselementin eli kosketusalueen
		public int LisaaOhjaus_elem(Ohjaus_elem_2D elem) {
			ohjaus_elementit.Add(elem);
			return ohjaus_elementit.Count-1; // palauttaa indeksin
		}

		//lisää ohjaukselle laitteen, jolla ohjaaminen tapahtuu
		public void Lisaa_Ohjaus_Input_elem(Ohjaus_input_2D input_elem) {
			ohjaus.ohjaus_hallinta.Lisaa_Ohjaus_Input_elem(input_elem);
		}

		public Ohjaus_elem_2D AnnaOhjauselementti(string nimi) {
			Ohjaus_elem_2D loytynyt=null;
			ohjaus_elementit.ForEach(delegate(Ohjaus_elem_2D obj) {
				if(obj.alue.nimi.Equals(nimi)) loytynyt=obj;
			});
			return loytynyt;
		}

		//siirtää ohjauslementin listassa yhden pykälän aiemmaksi, niin ohjaustoiminnot kytkeytyvät kyseisellä aleella ensisijaisemmin juuri tähän elementtiin
		//palauttaa uuden indeksin
		public int SiirraOhjauselementti_Listassa_Alkuupain(Ohjaus_elem_2D elem) {
			int ind=ohjaus_elementit.FindIndex(delegate(Ohjaus_2D.Ohjaus_elem_2D obj) {
				return obj.alue.nimi.Equals(elem.alue.nimi);
			});
			if(ind>0) { //siirretään
				Ohjaus_elem_2D aiempi=ohjaus_elementit[ind-1];
				ohjaus_elementit[ind-1]=elem;
				ohjaus_elementit[ind]=aiempi;
				return ind-1;
			}
			return ind;
		}
		//siirtää ohjauslementin listassa ihan ensimmäiseksi (alkuun)
		public void SiirraOhjauselementti_Listassa_Alkuun(Ohjaus_elem_2D elem) {
			while(SiirraOhjauselementti_Listassa_Alkuupain(elem)>0);
		}
		
		//ohjauslaite lähettää ohjaustapahtumia tätä kautta
		public void OtaVastaanOhjausTapahtuma(Ohjaustapahtuma kosketus) {
			// se ottaa kosketuksen, johon se ensimmäisenä osuu
			//ohjauselementit on oltava oikeassa järjestyksessä ja ensisijaiset ensin (jos on päällekäisiä alueita)
			bool vastaanottaja_loytynyt=false;
			ohjaus_elementit.ForEach(delegate(Ohjaus_elem_2D elem) {
				if(!vastaanottaja_loytynyt)
					if(elem.OtaKosketus(kosketus)) vastaanottaja_loytynyt=true;
			});
		}

		//havaitsee ja palauttaa raycast hit:n
		public RaycastHit Detect_RaycastHit(Vector2 screenPoint, float ray_distance) {
			Ray ray = Camera.main.ScreenPointToRay(screenPoint);
			RaycastHit hit;
			Physics.Raycast(ray, out hit, ray_distance);
			return hit;
		}
		
		//muuntaa hiiripositionit maailman koordinaateiksi
		public Vector2 ScreenToWorldPoint(Vector2 screen_point) {
			return Ohjattava_2D.Convert_to_Vector2(Camera.main.ScreenToWorldPoint(Ohjattava_2D.Convert_to_Vector3(screen_point)));
		}
		
		//muuntaa maailman koordinaatit hiiripositioniksi
		public Vector2 WorldToScreenPoint(Vector2 world_point) {
			return Ohjattava_2D.Convert_to_Vector2(Camera.main.WorldToScreenPoint(Ohjattava_2D.Convert_to_Vector3(world_point)));
		}
		
		//muuntaa nelikulmion hiiripositionit maailman koordinaateiksi
		public Rect ScreenToWorldRect(Rect screen_rect) {
			Vector2 point1=ScreenToWorldPoint(new Vector2(screen_rect.x, screen_rect.y));
			Vector2 point2=ScreenToWorldPoint(new Vector2(screen_rect.x+screen_rect.width, screen_rect.y+screen_rect.height));
			return new Rect(point1.x, point1.y, point2.x-point1.x, point2.y-point1.y);
		}
		
		//muuntaa nelikulmion maailman koordinaatit hiiripositioniksi
		public Rect WorldToScreenRect(Rect world_rect) {
			Vector2 point1=WorldToScreenPoint(new Vector2(world_rect.x, world_rect.y));
			Vector2 point2=WorldToScreenPoint(new Vector2(world_rect.width, world_rect.height));
			return new Rect(point1.x, point1.y, point2.x, point2.y);
		}

		//muuntaa Vector2:n Vector3:ksi siten että z-koordinaatti on 0 (pelipinnan taso)
		public static Vector3 Convert_to_Vector3(Vector2 vector2) {
			return new Vector3(vector2.x, vector2.y, 0); //pelipinnan z-koordinaatin on oltava 0!
		}
		
		//muuntaa Vector3:n Vector2:ksi siten että z-koordinaatti on 0 (pelipinnan taso)
		public static Vector2 Convert_to_Vector2(Vector3 vector3) {
			return new Vector2(vector3.x, vector3.y);
		}
		
		//arpoo int:n annetulta väliltä tasajakaumaa käyttäen
		public static int RandomRange(int min, int max) {
			int arvo=UnityEngine.Random.Range(min, max+1); //ilmeisesti max-arvoa ei arvota ikinä, joten kasvatetaan yhdellä
			if (arvo>max) arvo=max;
			return arvo;
		}
	}

	//muodostaa rajapinnan 2D-ohjattavalle oliolle, jonka kautta ne saavat ohjaus-tapahtumat (kosketukset)
	//rajapintaa käyttävässä luokassa täytyy toteuttaa varsinainen logiikka ja ohjaustoimintojen suorittaminen
	//ohjaaminen tapahtuu Ohjaus_handling_2D ohjaus_hallinta kautta, jolla myös määritetään miten ohjaaminen tapahtuu
	public class Ohjaus_2D_rajapinta {
		private  static Ohjaus_2D_rajapinta instance; // Singleton
		//  Instance  
		public static Ohjaus_2D_rajapinta Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new Ohjaus_2D_rajapinta();     
				return instance;   
			}
		}

		public Ohjaus_handling_2D ohjaus_hallinta=null; //hallitsee laitteita, jolla ohjaus tapahtuu
		public KuormanMittaaja kuorman_mittaaja=null;
		
		private Ohjaus_2D_rajapinta() {
			Initialize();
		}

		//siivoaa rojut pois ja tekee re-initialisoinnin
		public void Clean() {
			ohjaus_hallinta=null;
			Initialize();
		}

		public void Initialize() {
			ohjaus_hallinta=new Ohjaus_handling_2D(); // pakollinen ohjaushallintaolio, johon luodaan ohjaus_inputit
		}

		public void AsetaKuormanMittaaja(KuormanMittaaja p_kuorman_mittaaja) {
			kuorman_mittaaja=p_kuorman_mittaaja;
		}
		
		//lisää ohjaukselle laitteen, jolla ohjaaminen tapahtuu
		public void Lisaa_Ohjaus_Input_elem(Ohjaus_input_2D input_elem) {
			ohjaus_hallinta.Lisaa_Ohjaus_Input_elem(input_elem);
		}
		
		private void PyydaOhjausTapahtumat() {
			// otetaan vastaan uudet ohjaukset
			ohjaus_hallinta.PyydaOhjausTapahtumat();
		}

		//ottaa vastaan ohjauksien tilat ja suorittaa tämän jälkeen ohjattavien toiminnot
		public void SuoritaToiminnot() {
			/*if(kuorman_mittaaja!=null) {
				kuorman_mittaaja.AloitaUusiKierros();
				PyydaOhjausTapahtumat();
				kuorman_mittaaja.OtaSeuraavaMittausaikaVastaan("PyydaOhjausTapahtumat: ");
				ohjaus_hallinta.ohjattavat.ForEach(delegate(Ohjattava_2D obj) {
					obj.SuoritaToiminnot();
					kuorman_mittaaja.OtaSeuraavaMittausaikaVastaan(obj.ToString());
				});
			} else {*/
				PyydaOhjausTapahtumat();
				ohjaus_hallinta.ohjattavat.ForEach(delegate(Ohjattava_2D obj) {
					obj.SuoritaToiminnot();
				});
			//}
		}
	}

	//mittaa, kuinka paljon mitkäkin toiminnot vievät aikaa
	public class KuormanMittaaja {
		public GUIText kuorma_naytto=null;
		public float paivitys_vali=0.2f;
		public int lukeman_tarkkuus; //desimaalien lkm tuloksissa
		public bool nayta_maksimiaika=true;
		private float viime_paivitysaika;
		private float[] suoritusajat;
		private string[] nimet;
		private float yht;
		private int next_ind; //seuraavan lukeman indeksi
		private float start_time; //kierroksen alkamisaika
		private float task_start_time;

		public KuormanMittaaja(GUIText p_kuorma_naytto, float p_paivitys_vali, int p_lukeman_tarkkuus, bool p_nayta_maksimiaika, int mitattavat_lkm) {
			kuorma_naytto=p_kuorma_naytto;
			paivitys_vali=p_paivitys_vali;
			lukeman_tarkkuus=p_lukeman_tarkkuus;
			nayta_maksimiaika=p_nayta_maksimiaika;
			suoritusajat=new float[mitattavat_lkm];
			nimet=new string[mitattavat_lkm];
		}

		//usi mittauskierros alkaa
		public void AloitaUusiKierros() {
			if(nayta_maksimiaika) { //maksimiaikojen mittaus -> pitää nollata tulokset
				for(int i=0; i<suoritusajat.Length; i++)
					suoritusajat[i]=0;
				yht=0;
			}
			next_ind=0;
			start_time=Time.realtimeSinceStartup;
			task_start_time=start_time;
		}

		//toiminnon suorittaja kutsuu, kun saa homman valmiiksi
		public void OtaSeuraavaMittausaikaVastaan(string nimi) {
			float mittausaika=Time.realtimeSinceStartup-task_start_time;
			if(suoritusajat[next_ind]<mittausaika || !nayta_maksimiaika) {
				suoritusajat[next_ind]=mittausaika;
				if(nimet[next_ind]==null)
					nimet[next_ind]=nimi;
			}
			next_ind++;
			task_start_time=Time.realtimeSinceStartup; //seuraava toiminto alkaa
		}

		public void Paivita() {
			//päivitetään yhteensä arvo
			if(yht<(Time.realtimeSinceStartup-start_time) || !nayta_maksimiaika)
				yht=(Time.realtimeSinceStartup-start_time);
			//päivitetään näyttö mikäli sille on aika
			if((Time.realtimeSinceStartup-viime_paivitysaika)>paivitys_vali) {
				viime_paivitysaika=Time.realtimeSinceStartup;
				kuorma_naytto.text="";
				for(int i=0; i<suoritusajat.Length; i++) {
					if(nimet[i]!=null) //tulos on asetettu
						kuorma_naytto.text+=nimet[i] + suoritusajat[i].ToString("f" + lukeman_tarkkuus.ToString()) + "\n";
				}
				kuorma_naytto.text+="yht: " + yht.ToString("f" + lukeman_tarkkuus.ToString()) + "\n";
			}
		}
	}
	
	//sisältää ohjaustapahtuman positionin ja tilan. kaikki ohjauslaitteet käyttää tätä samaa
	public class Ohjaustapahtuma {
		public Vector2 position;
		public TouchPhase phase;
		
		public Ohjaustapahtuma(Vector2 p_position, TouchPhase p_phase) {
			position=p_position;
			phase=p_phase;
		}
	}
	
	//ohjaushallinnan pääluokka: toimii ohjattavan olion ja ohjauslaitteen olion välissä (yhdistää ne toisiinsa)
	public class Ohjaus_handling_2D {
		public List<Ohjattava_2D> ohjattavat;
		public List<Ohjaus_input_2D> input_elems;
		public List<Alue> pois_suljetut_alueet; //voidaan asettaa poissuljettuja alueita, joiden sisälle osuvat ohjauskoordinaatit hylätään
		//ohjaustoimintojen rekisteröinti voidaan kytkeä pois ohjaustapahtumiin, joissa ohjauselementti etsitään joukosta
		//ei kytke pois ohjaustoimintoja, joissa kosketus kytketään suoraan kyseiseen ohjauselementtiin
		public bool aktiivinen;

		public Ohjaus_handling_2D() {
			ohjattavat=new List<Ohjattava_2D>();
			input_elems=new List<Ohjaus_input_2D>();
			pois_suljetut_alueet=new List<Alue>();
			aktiivinen=true;
		}

		//rekisteröi (lisää, jos puuttuu) ohjattavan
		public void Rekisteroi_ohjattava(Ohjattava_2D p_ohjattava) {
			if(!ohjattavat.Contains(p_ohjattava)) ohjattavat.Add(p_ohjattava);
		}
		
		public void Lisaa_Ohjaus_Input_elem(Ohjaus_input_2D input_elem) {
			input_elems.Add(input_elem);
		}
		
		public void Poista_Ohjaus_Input_elem(int ind) {
			input_elems.RemoveAt(ind);	
		}

		public void Lisaa_Pois_suljettava_Alue(Alue p_alue) {
			pois_suljetut_alueet.Add(p_alue);
		}

		public void Poista_Pois_suljettu_Alue(string p_nimi) {
			Alue poistettava=null;
			poistettava=pois_suljetut_alueet.Find(delegate(Alue obj) {
				return obj.nimi.CompareTo(p_nimi)==0;
			});
			pois_suljetut_alueet.Remove(poistettava);
		}

		public void KytkeOhjaustoiminnot(bool p_aktiivinen) {
			aktiivinen=p_aktiivinen;
		}
		
		public void PyydaOhjausTapahtumat() {
			foreach (Ohjaus_input_2D input_elem in input_elems)
				input_elem.PalautaOhjaustapahtumat(this);
		}
		
		public void RekisteroiKosketus(Ohjaustapahtuma kosketus) {
			if(aktiivinen && OsuuSallitulleAlueelle(kosketus.position)) {
				ohjattavat.ForEach(delegate(Ohjattava_2D obj) {
					obj.OtaVastaanOhjausTapahtuma(kosketus);
				});
			}
		}

		public bool OsuuSallitulleAlueelle(Vector2 position) {
			return !pois_suljetut_alueet.Contains(pois_suljetut_alueet.Find(delegate(Alue obj) {
				return obj.isInside(position);
			}));
		}
	}
	
	//abstrakti luokka, josta kaikki ohjauslaitteet periytetään
	abstract public class Ohjaus_input_2D {
		abstract public void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling);
	}

	//luokka laitteille, jotka ohjaavat virtuaalinappuloita
	//rajapinta virtuaalisiin nappuloihin pitää toteuttaa erikseen
	//voi toteuttaa myös ilman virtuaalisten nappuloiden ohjaamista
	public abstract class VirtuaalinappuloidenOhjaaja : Ohjaus_input_2D {
		public VirtuaalinappuloidenOhjaaja() {
			Ohjaus_2D_rajapinta.Instance.Lisaa_Ohjaus_Input_elem(this); //rekisteröidään listaan, niin kytkeytyy ohjaustapahtumien suoritus
		}
	}
	
	//multitouch-ohjauksien rekisteröintiin tarkoitettu luokka
	public class Multitouch_input_2D : Ohjaus_input_2D {
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			foreach(Touch touch in Input.touches) 
			{
				ohjaus_handling.RekisteroiKosketus(new Ohjaustapahtuma(touch.position, touch.phase));
			}
		}
	}

	//ylin luokka input:lle, jolla on tila on/off
	public abstract class Tila_input_2D : Ohjaus_input_2D {
		protected const int YLHAALLA=0;
		protected const int ALHAALLA=1;
		protected int edellinen_tila=YLHAALLA;
		protected int uusi_tila=YLHAALLA;
		protected TouchPhase phase=TouchPhase.Canceled;

		protected void Paivita_uusi_tila(bool tila_alas, bool tila_ylos) { //parametrina tilasiirtymat eli tosi hetkinä, jolloin tila muuttuu
			phase=TouchPhase.Canceled;
			edellinen_tila=uusi_tila;
			// testataan alas painaminen
			if (tila_alas) {
				uusi_tila=ALHAALLA;
				phase=TouchPhase.Began;
				// testataan nosto
			} else if (tila_ylos) {
				uusi_tila=YLHAALLA;
				phase=TouchPhase.Ended;
				// testataan pito
			} else if (edellinen_tila==ALHAALLA) {
				uusi_tila=ALHAALLA;
				phase=TouchPhase.Stationary; //oletuksena. perivä ohjauslaite voi muuttaa tämän -> Moved
			}
		}

		public bool AnnaTilasiirtyma(bool uusi_tila, bool edellinen_tila, bool testataan_tila_alas) {
			if(testataan_tila_alas) { //testataan key_down
				if(uusi_tila && (!edellinen_tila))
					return true;
				else
					return false;
			} else { //testataan key_up
				if((!uusi_tila) && edellinen_tila)
					return true;
				else
					return false;
			}
		}
	}
	//etsii etsii (rekisteröi) kosketukselle ohjaus-elementin joukosta koordinaattien perusteella
	public abstract class Kosketukselle_kohteen_etsiva_Tila_input_2D : Tila_input_2D {
		public void RekisteroiKosketus(Ohjaus_handling_2D ohjaus_handling, Vector2 position) {
			if (edellinen_tila==ALHAALLA || uusi_tila==ALHAALLA) {
				ohjaus_handling.RekisteroiKosketus(new Ohjaustapahtuma(position, phase));
			}
			edellinen_tila=uusi_tila;
		}
	}
	//hiiriohjauksien rekisteröintiin tarkoitettu luokka
	public class Mouse_input_2D : Kosketukselle_kohteen_etsiva_Tila_input_2D {
		private Vector2 edel_position=new Vector2(0,0);
		private int nappi;
		
		public Mouse_input_2D(int p_nappi) {
			nappi=p_nappi;
		}
		
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			Paivita_uusi_tila(Input.GetMouseButtonDown(nappi), Input.GetMouseButtonUp(nappi));
			if((phase==TouchPhase.Stationary) && (edel_position.x!=Input.mousePosition.x && edel_position.y!=Input.mousePosition.y))
				phase=TouchPhase.Moved;
			RekisteroiKosketus(ohjaus_handling, Input.mousePosition);
			edel_position=Input.mousePosition;
		}
	}
	//näppäimistöohjauksien rekisteröintiin tarkoitettu luokka
	//rekisteröi ohjauskosketukset annetun virtuaali-positionin sisälle
	public class Key_input_2D : Kosketukselle_kohteen_etsiva_Tila_input_2D {
		private Vector2 virtual_position; // position nelikulmion sisällä, jonka perusteella painallus yhdistetään (näkymättömään?) painonappiin
		private string key;
		
		public Key_input_2D(string p_key, Rect p_virtual_area) {
			key=p_key;
			virtual_position.x=p_virtual_area.x+p_virtual_area.width/2;
			virtual_position.y=GUICoordinatesScaling.Instance.originalHeight-p_virtual_area.y-p_virtual_area.height/2;
		}
		
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			Paivita_uusi_tila(Input.GetKeyDown(key), Input.GetKeyUp(key));
			RekisteroiKosketus(ohjaus_handling, GUICoordinatesScaling.Instance.Scaling_point(virtual_position));
		}
	}

	//kytkee (rekisteröi) kosketuksen suoraan ohjaus-elementtiin, joka annetaan parametrina
	//ei tarvitse siis lisätä ohjaus_handlingin input-laite-listaan
	//jos käytetään virtuaalisessa nappulassa, silloin ei edes saa lisätä ohjaus_handlingin input-laite-listaan, jotta ei sekoitu mahdollisesti samalla alueella oleviin muihin tila_inputteihin
	public class Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D : Tila_input_2D {
		public Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D() {}
		//parametrina annetaan ohjaus_elem, johon toiminto kohdistuu
		//2 muuta parametria ovat tilasiirtymiä
		public void RekisteroiKosketus(Ohjaus_elem_2D ohjaus_elem, bool tila_alas, bool tila_ylos) {
			Paivita_uusi_tila(tila_alas, tila_ylos);
			if (phase!=TouchPhase.Canceled)
				ohjaus_elem.KasitteleKosketus(new Ohjaustapahtuma(new Vector2(ohjaus_elem.alue.dimensio.x+1, Screen.height-ohjaus_elem.alue.dimensio.y-1), phase));
		}
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {} //ei palauta tällä tavalla (ei vahdi alueita, joissa voi olla ohjauselementtejä)
	}

	//näppäimistöohjauksien rekisteröintiin tarkoitettu luokka
	//rekisteröi ohjauskosketukset suoraan annettuun ohjaus-elementtiin
	public class Key_input_2D_Suoraan_Ohjaus_elemiin : Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D {
		private string key;
		Ohjaus_elem_2D ohjaus_elem;

		public Key_input_2D_Suoraan_Ohjaus_elemiin(string p_key, Ohjaus_elem_2D p_ohjaus_elem) {
			key=p_key;
			ohjaus_elem=p_ohjaus_elem;
		}

		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			RekisteroiKosketus(ohjaus_elem, Input.GetKeyDown(key), Input.GetKeyUp(key));
			edellinen_tila=uusi_tila;
		}
	}
	
	//palauttaa satunnaisen pisteen määritellyn muodon reunalta
	public class Get_RandomPoint_from_Shape {
		public Line line;
		public Rect area;
		public float radius; public Vector2 middle_point;
		public int maaraava_ind; //kertoo minkä parametrin mukaan arvonta tehdään
		
		public Get_RandomPoint_from_Shape(Line p_line, Rect p_area, float p_radius, Vector2 p_middle_point, int p_maaraava_ind) {
			line=p_line;
			area=p_area;
			radius=p_radius;
			middle_point=p_middle_point;
			maaraava_ind=p_maaraava_ind;
		}
		
		public Vector2 Get_RandomPoint() {
			// palauttaa satunnaisen pisteen riippuen, mikä muoto oliolle on asetettu
			if(maaraava_ind==0) //(HUOM: jos mitään ei ole asetettu, palauttaa pisteen aina origosta)
				return Get_RandomPoint_in_Line(line.start_point, line.end_point);
			else if(maaraava_ind==1)
				return Get_RandomPoint_in_Border_of_Rectangle(area);
			else if(maaraava_ind==2) //palautetaan ympyrän kehältä
				return Get_RandomPoint_in_Border_of_Circle(radius, middle_point);
			else
				return new Vector2(0,0);
		}
		
		public Vector2 Get_RandomPoint_in_Line(Vector2 start_point, Vector2 end_point) {
			float t=UnityEngine.Random.Range(0f,1f);
			return Vector2.Lerp(start_point, end_point, t);
		}
		
		public Vector2 Get_RandomPoint_in_Border_of_Rectangle(Rect area) {
			int reuna=Ohjattava_2D.RandomRange(0,3);
			if(reuna==0) {
				return Get_RandomPoint_in_Line(new Vector2(area.x, area.y), new Vector2(area.x+area.width, area.y));
			} else if(reuna==1) {
				return Get_RandomPoint_in_Line(new Vector2(area.x+area.width, area.y), new Vector2(area.x+area.width, area.y+area.height));
			} else if(reuna==2) {
				return Get_RandomPoint_in_Line(new Vector2(area.x, area.y+area.height), new Vector2(area.x+area.width, area.y+area.height));
			} else {
				return Get_RandomPoint_in_Line(new Vector2(area.x, area.y), new Vector2(area.x, area.y+area.height));
			}
		}
		
		public Vector2 Get_RandomPoint_in_Border_of_Circle(float radius, Vector2 middle_point) {
			float angle = UnityEngine.Random.Range(0.0f,Mathf.PI*2);
			return new Vector2(radius*Mathf.Cos(angle)+middle_point.x, radius*Mathf.Sin(angle)+middle_point.y);
		}
		
		public class Line {
			public Vector2 start_point;
			public Vector2 end_point;
			
			public Line(Vector2 p_start_point, Vector2 p_end_point) {
				start_point=p_start_point;
				end_point=p_end_point;
			}
		}
	}

	//************** inspector *****************************
	//parametriluokkia inspectoriin ja rakentajia
	//parametriluokkaan asettuu parametrit inspectorista ja rakentajalla niistä rakennetaan elementtejä
	//parametritluokat
	[System.Serializable]
	public class Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Shape {
			line,
			border_of_rectangle,
			border_of_circle
		}
		public Shape shape;

		public Get_RandomPoint_from_Shape.Line line;
		public Rect area;
		public float radius;
		public Vector2 middle_point;
	}

	[System.Serializable]
	public class KuormanMittaaja_Parametrit_Hallintapaneeliin {
		public GUIText kuorma_naytto;
		public float paivitys_vali=0.2f;
		public int lukeman_tarkkuus=4; //desimaalien lkm tuloksissa
		public bool nayta_maksimiaika=true;
		public int mitattavat_lkm; //kuinka monelle toiminnolle varataan paikka
	}

	//rakentajat
	public class Get_RandomPoint_from_Shape_Hallintapaneelista_Rakentaja {
		public Get_RandomPoint_from_Shape Rakenna(Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin parametrit) {
			int maaraava_ind=0;
			if(parametrit.shape==Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin.Shape.line)
				maaraava_ind=0;
			else if(parametrit.shape==Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin.Shape.border_of_rectangle)
				maaraava_ind=1;
			else if(parametrit.shape==Get_RandomPoint_from_Shape_Parametrit_Hallintapaneeliin.Shape.border_of_circle)
				maaraava_ind=2;
			return new Get_RandomPoint_from_Shape(parametrit.line, parametrit.area, parametrit.radius, parametrit.middle_point, maaraava_ind);
		}
	}

	public class KuormanMittaaja_Hallintapaneelista_Rakentaja {
		public KuormanMittaaja Rakenna(KuormanMittaaja_Parametrit_Hallintapaneeliin parametrit) {
			if(parametrit.kuorma_naytto!=null) //näyttö määrää, onko parametrit annettu
				return new KuormanMittaaja(parametrit.kuorma_naytto, parametrit.paivitys_vali, parametrit.lukeman_tarkkuus, parametrit.nayta_maksimiaika, parametrit.mitattavat_lkm);
			else
				return null;
		}
	}
}