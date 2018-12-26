using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ohjaus_2D;
using Menu;
using CoordinatesScaling;

//ohjauslaitteeseen voi lisätä joko OnGUI-nappuloita, GUITexture- tai virtuaali-nappuloita
//samaan ohjauslaitteeseen saa laittaa vain OnGUI- tai muun-tyyppisiä nappuloita
//OnGUI-tyyppisen ohjauslaitteen elementit piirretään OnGUI-kutsussa
//muu-tyyppisen ohjauslaitteen elementit "piirretään" update-kutsussa. todellisuudessa niitä ei piirretä, vaan ainoastaan nappuloiden ohjaustilat päivitetään
namespace Ohjaus_laite {
	//luokka in-game ohjausnappuloille (ryhmälle)
	public class Ohjaus_laite : Ohjaus_2D.Ohjattava_2D {
		public Menu_elem_container ohjausnappulat_container; // ohjausnappuloiden GUI-elementit lisätään containeriin
		public Ohjauslaitteen_Kayttaja ohjauslaitteen_kayttaja=null;
		public string kayttaja_prefab; //prefabin nimi, joka käyttää ohjauslaitetta
		private bool on_OnGUI_tavaraa=false; //ainakin osa nappuloista on GUI-elementtejä
		private bool on_Muuta_tavaraa=false; //ainakin osa nappuloista on muita kuin GUI-elementtejä
		
		public Ohjaus_laite(string p_kayttaja_prefab) {
			ohjausnappulat_container=new Menu_elem_container();
			kayttaja_prefab=p_kayttaja_prefab;
			Ohjauslaitteet.Instance.LisaaOhjauslaite(this); //lisätään ohjauslaitekokoelmaan
		}

		public override string ToString ()
		{
			return ohjauslaitteen_kayttaja.ToString();
		}

		//näillä tarkistetaan, että ohjauslaitteen nappulat ovat yhtä tyyppiä
		public bool Onko_OnGUI_tyyppinen() {
			OnkoMolempiTyyppinen();
			return on_OnGUI_tavaraa;
		}
		public bool Onko_Muu_tyyppinen() {
			OnkoMolempiTyyppinen();
			return on_Muuta_tavaraa;
		}
		public bool OnkoMolempiTyyppinen() {
			if(on_OnGUI_tavaraa && on_Muuta_tavaraa) {
				Debug.LogError("Ohjauslaitteessa on seka OnGUI-elementteja etta muita elementeja, kun saisi olla vain OnGUI- tai muita tyyppeja!");
				return true;
			} else return false;
		}

		//nappula GUI-elementtinä
		public Button_elem Lisaa_Ohjausnappi(GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja gui_funktio, GUIContent gui_content, Rect position, string nimi, GUIStyle gui_style, bool tukee_multitouch=true, string key=null, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja ohjaustapahtuman_suorittaja=null, List<TouchPhase> hyvaksytyt_kosketukset=null) {
			Button_elem nappula=new Button_elem(gui_funktio, gui_content, position, nimi, ohjaustapahtuman_suorittaja, null, gui_style, hyvaksytyt_kosketukset, this, tukee_multitouch);
			ohjausnappulat_container.Lisaa_Menu_elem(nappula);
			if(key.Length>0) ohjaus.Lisaa_Ohjaus_Input_elem(new Key_input_2D(key, position)); // liitetään nappulaan näppäin
			on_OnGUI_tavaraa=true;
			return nappula;
		}
		//nappula GUITexturena (ei piirretä, vaan on scenellä GameObjectina)
		public Button_elem Lisaa_Ohjausnappi(GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja gui_funktio, GUITexture gui_texture, string nimi, string key=null, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja ohjaustapahtuman_suorittaja=null, List<TouchPhase> hyvaksytyt_kosketukset=null) {
			Button_elem nappula=new Button_elem(gui_funktio, gui_texture, nimi, ohjaustapahtuman_suorittaja, this, hyvaksytyt_kosketukset);
			ohjausnappulat_container.Lisaa_Menu_elem(nappula);
			if(key.Length>0) ohjaus.Lisaa_Ohjaus_Input_elem(new Key_input_2D_Suoraan_Ohjaus_elemiin(key, nappula.ohjaus_elem_tilan_tallettaja)); // liitetään nappulaan näppäin
			on_Muuta_tavaraa=true;
			return nappula;
		}
		//nappula virtualbuttonina (ei piirretä, vaan on virtuaalinen)
		public Button_elem Lisaa_Ohjausnappi(GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja gui_funktio, string nimi, string key=null, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja ohjaustapahtuman_suorittaja=null, List<TouchPhase> hyvaksytyt_kosketukset=null) {
			Button_elem nappula=new Button_elem(gui_funktio, nimi, ohjaustapahtuman_suorittaja, this, hyvaksytyt_kosketukset);
			ohjausnappulat_container.Lisaa_Menu_elem(nappula);
			if(key.Length>0) ohjaus.Lisaa_Ohjaus_Input_elem(new Key_input_2D_Suoraan_Ohjaus_elemiin(key, nappula.ohjaus_elem_tilan_tallettaja)); // liitetään nappulaan näppäin
			on_Muuta_tavaraa=true;
			return nappula;
		}
		
		//piirtää elementit ja päivittää tilat riippuen miten niitä ohjataan
		//kutsutaan riippuen ohjauslaitteen tyypistä joko OnGUI:ssa (GUI-elementit) tai Updatessa (GUITexture ja virtualbutton)
		//mikäli suoritetaan OnGUI:ssa, ohjauslaitteen käyttäjällä on velvollisuus suorittaa kutsu
		//jos on toteutettu GUI-elementteinä, kosketukset on tunnistettu aiemmin ja ne asetetaan nappuloihin tilaksi
		//GUITexture-toteutuksena nappuloiden tilat tunnistetaan "piirtämisen" yhteydessä
		//virtualbuttonijn tapauksessa nappia pitää painaa ennen piirtämistä ja tilat tunnistetaan samalla tavalla kuin GUITexturen tapauksessa
		public void PaivitaTilat() {
			ohjausnappulat_container.Piirra_elementit();
		}

		//kutsutaan automaattisesti ohjaus_2d:stä
		public override void SuoritaToiminnot() {
			PaivitaTilat();
			ohjauslaitteen_kayttaja.SuoritaToiminnot();
		}

		public void AsetaOhjauslaitteenKayttaja(Ohjauslaitteen_Kayttaja p_ohjauslaitteen_kayttaja) {
			ohjauslaitteen_kayttaja=p_ohjauslaitteen_kayttaja;
		}
	}

	//ohjauslaitteen käyttäjät periytetään tästä
	//constructori tekee tarkistukset, että ohjauslaitteen nappulat ovat samaa tyyppiä
	//asettaa myös ohjauslaitteen käyttäjän
	public abstract class Ohjauslaitteen_Kayttaja {
		public Ohjaus_laite ohjaus_laite;

		public Ohjauslaitteen_Kayttaja(Ohjaus_laite p_ohjaus_laite) {
			ohjaus_laite=p_ohjaus_laite;
			ohjaus_laite.OnkoMolempiTyyppinen();
			ohjaus_laite.AsetaOhjauslaitteenKayttaja(this);
		}

		//suorittaa toiminnot sen jälkeen, kun ohjausnappuloiden tilat on selvitetty
		public abstract void SuoritaToiminnot();

		//asetetaan koordinaattien skaalausoliolle piirto-funktio (otetaan piirtofunktio käyttöön). mikäli ohjauslaitteen nappulat toteutettiin GUI-elementteinä
		public void RegisterDrawingFunction_if_Necessary(GUICoordinatesScaling.Drawing p_draving_function) {
			if(ohjaus_laite.Onko_OnGUI_tyyppinen()) GUICoordinatesScaling.Instance.RegisterDrawingFunction(p_draving_function);
		}
	}

	//kokoelma kaikista ohjauslaitteista
	//antaa ohjasnappuloille yksilöllisen nimen, jolloin saman nimiset nappulat erotetaan eri prefabeihin ja niiden instansseihin
	public class Ohjauslaitteet {
		private  static Ohjauslaitteet instance; // Singleton
		//  Instance  
		public static Ohjauslaitteet Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new Ohjauslaitteet();     
				return instance;   
			}
		}
		
		private Ohjauslaitteet() {
			Initialize();
		}
		public List<Prefabin_Ohjauslaitteet> prefabien_ohjauslaitteet;

		//siivoaa rojut pois ja tekee re-initialisoinnin
		public void Clean() {
			prefabien_ohjauslaitteet.Clear();
			Initialize();
		}
		
		public void Initialize() {
			prefabien_ohjauslaitteet=new List<Prefabin_Ohjauslaitteet>();
		}

		public Prefabin_Ohjauslaitteet AnnaPrefabinOhjauslaitteet(string prefab) {
			return prefabien_ohjauslaitteet.Find(delegate(Prefabin_Ohjauslaitteet obj) {
				return obj.prefab.CompareTo(prefab)==0;
			});
		}

		public void LisaaOhjauslaite(Ohjaus_laite ohjauslaite) {
			Prefabin_Ohjauslaitteet ohjauslaitteet=AnnaPrefabinOhjauslaitteet(ohjauslaite.kayttaja_prefab);
			if(ohjauslaitteet==null) { // uusi prefab -> lisätään
				ohjauslaitteet=new Prefabin_Ohjauslaitteet(ohjauslaite.kayttaja_prefab, ohjauslaite);
				prefabien_ohjauslaitteet.Add(ohjauslaitteet);
			} else // lisätään vain uusi ohjauslaite
				ohjauslaitteet.ohjauslaitteet.Add(ohjauslaite);
		}

		//lisää nappulan nimeen etuliitteen, jossa on prefabin nimi ja (prefin jonkin instanssin) ohjauslaitteen numero
		public string Anna_Nappulalle_Yksilollinen_Nimi(Ohjaus_laite ohjauslaite, string nappulan_nimi) {
			Prefabin_Ohjauslaitteet prefabin_ohjauslaitteet=AnnaPrefabinOhjauslaitteet(ohjauslaite.kayttaja_prefab);
			int ohjauslaite_ind=prefabin_ohjauslaitteet.ohjauslaitteet.IndexOf(ohjauslaite);
			return prefabin_ohjauslaitteet.prefab + "_ohjauslaite_" + ohjauslaite_ind.ToString() + "_" + nappulan_nimi;
		}

		public class Prefabin_Ohjauslaitteet {
			public string prefab;
			public List<Ohjaus_laite> ohjauslaitteet;

			public Prefabin_Ohjauslaitteet(string p_prefab, Ohjaus_laite eka_ohjauslaite) {
				prefab=p_prefab;
				ohjauslaitteet=new List<Ohjaus_laite>();
				ohjauslaitteet.Add(eka_ohjauslaite);
			}
		}
	}

	//apuluokka virtuaalisen nappulan painamiseen
	//helppo käyttää: annetaan vain tila: aktiivinen tai passiivinen
	public class VirtuaalisenNappulan_PainajaApuri {
		public Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D tila_input=new Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D();
		public Button_elem nappula;
		//tilat, joilla nappulaa ohjataan. nappulan tilan tallettaja määrittää nappulan ohjaustilojen perusteella varsinaisen nappulan tilan riippen asetuksista (tilan tallettajan hyväksytyistä kosketuksista)
		private bool tila;
		private bool edellinen_tila;

		public VirtuaalisenNappulan_PainajaApuri(Button_elem p_nappula) {
			nappula=p_nappula;
		}

		//asetetaan uusi tila, jolla nappulaa ohjataan
		//nappulan asetuksista riippuu, mikä tila varsinaiseen nappulaan asettuu ja mikä välitetään ohjauslaitteen käyttäjälle
		public void AsetaUusiTila(bool uusi_tila) {
			if(nappula!=null) {
				edellinen_tila=tila;
				tila=uusi_tila;
				tila_input.RekisteroiKosketus(nappula.ohjaus_elem_tilan_tallettaja, tila_input.AnnaTilasiirtyma(tila, edellinen_tila, true), tila_input.AnnaTilasiirtyma(tila, edellinen_tila, false));
			}
		}
	}

	//inspector
	//ohjauslaitteen rakentajat laittavat tästä olion inspectoriin
	[System.Serializable]
	public class Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum NappulanToimintatapa {
			Repeat_button, //jatkuva
			Button // impulssi
			
		};
		public NappulanToimintatapa nappulan_toimintapa;

		public HyvaksyttyjenKosketuksienMaarittaminen_Parametrit_Hallintapaneeliin hyvaksytyt_kosketukset;

		[System.Serializable]
		public enum NappulanToteutustapa {
			GUI_elementtina,
			GUITexturena,
			Virtuaalisena //ei näy missään fyysisesti. käytetään rajapintana muihin järjetelmiin
		}
		public NappulanToteutustapa nappulan_toteutustapa;
		
		[System.Serializable]
		public class GUI_elementtina { //anna nämä asetukset siinä tapauksessa, jos valitaan tämä toteutustapa
			public string GUIContent_text;
			public Rect position;
			public GUIStyle gui_style;
		}
		public GUI_elementtina GUI_elementtina_asetukset=new GUI_elementtina();
		
		[System.Serializable]
		public class GUITexturena { //tai annakin nämä asetukset siinä tapauksessa, jos valitaan tämä toteutustapa
			public GUITexture gui_texture;
		}
		public GUITexturena GUITexturena_asetukset=new GUITexturena();

		public string nimi;
		public string key; //näppäintoiminto
	}
	//ohjauslaitteen rakentajat rakentaa nappulan parametreista tämän luokan oliolla
	public class Hallintapaneelista_Rakentaja {
		public Menu.Button_elem RakennaNappula_Hallintapaneelista(Parametrit_Hallintapaneeliin parametrit, Ohjaus_laite ohjaus_laite) {
			Menu.GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja gui_funktio=null;
			if(parametrit.nappulan_toimintapa==Parametrit_Hallintapaneeliin.NappulanToimintatapa.Button)
				gui_funktio=GUI.Button;
			else if(parametrit.nappulan_toimintapa==Parametrit_Hallintapaneeliin.NappulanToimintatapa.Repeat_button)
				gui_funktio=GUI.RepeatButton;
			//määritetään hyväksytyt kosketukset
			List<TouchPhase> hyvaksytyt_kosketukset=new HyvaksyttyjenKosketuksienMaarittaminen_Hallintapaneelista_Rakentaja().Rakenna(parametrit.hyvaksytyt_kosketukset);
			//rakennetaan nappula
			string yksilollinen_nimi=Ohjauslaitteet.Instance.Anna_Nappulalle_Yksilollinen_Nimi(ohjaus_laite, parametrit.nimi);
			if(parametrit.nappulan_toteutustapa==Parametrit_Hallintapaneeliin.NappulanToteutustapa.GUI_elementtina) { //nappula toteutetaan GUI-elementtinä
				return ohjaus_laite.Lisaa_Ohjausnappi(gui_funktio, new GUIContent(parametrit.GUI_elementtina_asetukset.GUIContent_text), parametrit.GUI_elementtina_asetukset.position, yksilollinen_nimi, parametrit.GUI_elementtina_asetukset.gui_style, true, parametrit.key, null, hyvaksytyt_kosketukset);
			} else if(parametrit.nappulan_toteutustapa==Parametrit_Hallintapaneeliin.NappulanToteutustapa.GUITexturena) { //nappula toteutetaan GUI-texturena
				return ohjaus_laite.Lisaa_Ohjausnappi(gui_funktio, parametrit.GUITexturena_asetukset.gui_texture, yksilollinen_nimi, parametrit.key, null, hyvaksytyt_kosketukset);
			} else if(parametrit.nappulan_toteutustapa==Parametrit_Hallintapaneeliin.NappulanToteutustapa.Virtuaalisena) { //nappula toteutetaan virtuaalisena
				return ohjaus_laite.Lisaa_Ohjausnappi(gui_funktio, yksilollinen_nimi, parametrit.key, null, hyvaksytyt_kosketukset);
			} else
				return null; //ei toteuteta tätä ohjaussuuntaa
		}
	}
}
