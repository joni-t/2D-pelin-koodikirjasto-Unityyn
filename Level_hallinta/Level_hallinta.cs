using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Ohjaus_laite;

//Käsittelee levelin tilaa
//Rekisteröi ja laskee tapahtumat
//Kontrolloi ja rajoittaa tapahtumien suorittamista
//Tunnistaa level failed/completed
namespace Level_hallinta {
	public class Level_hallinta {
		private  static Level_hallinta instance; // Singleton
		//  Instance  
		public static Level_hallinta Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new Level_hallinta();     
				return instance;   
			}
		}

		public bool level_kaynnissa=false; //voidaan kieltää tapahtumien suorittaminen
		public List<Tila> tilat;
		public TilajoukonTesti level_complete;
		public TilajoukonTesti level_failed;
		public bool is_level_completed;
		public bool is_level_failed;
		public bool level_ended_message_sended; //muualta on otettu vastaan tieto levelin complete/failed:sta
		public Kello levelin_kello=null;
		public uint levelia_yritetty_lkm=0; //kuinka monta kertaa leveliä on yritetty läpi onnistumatta
		public int level_ind=-1;

		private Level_hallinta() {
			Initialize();
		}

		public void Clean() {
			tilat.Clear();
			Initialize();
		}

		public void Initialize() {
			tilat=new List<Tila>();
			level_complete=new TilajoukonTesti();
			level_failed=new TilajoukonTesti();
		}

		//palauttaa tilaolion labelin perusteella
		public Tila Anna_Tila_Labellla(string label) {
			if(label==null) return null;
			Tila instanssi=null;
			tilat.ForEach(delegate(Tila item) {
				if (item.label.CompareTo(label)==0) instanssi = item;
			});
			return instanssi;
		}

		public void Start_level() {
			Debug.Log("Level kaynnissa");
			level_kaynnissa=true;
			is_level_completed=false;
			is_level_failed=false;
			level_ended_message_sended=false;
			if(level_ind!=Application.loadedLevel) //eri levell
				levelia_yritetty_lkm=0;
			level_ind=Application.loadedLevel;
		}

		public bool LevelComplete(bool pakotettu=false) {
			if(level_complete.AnnaTila() | pakotettu) {
				level_kaynnissa=false;
				is_level_completed=true;
				levelia_yritetty_lkm=0;
				Debug.Log("Level complete: " + Application.loadedLevelName);
				return true;
			}
			return false;
		}

		public bool LevelFailed() {
			if(level_failed.AnnaTila()) {
				Debug.Log("Level failed!");
				level_kaynnissa=false;
				is_level_failed=true;
				levelia_yritetty_lkm++;
				return true;
			}
			return false;
		}

		//testaa levelin tilan failed/complete
		public void PaivitaLevelinTila() {
			if(!LevelFailed())
				LevelComplete();
		}
	}

	//luokka pelin eri tiloille
	//pitävät sisällään tietoa levelin tilasta (kertovat, onko raja saavutettu ja voiko tapahtuman suorittaa)
	public abstract class Tila {
		public string label;
		public bool inverted=false; //kääntää tilan

		public Tila(string p_label, bool p_inverted=false) {
			label=p_label;
			inverted=p_inverted;
			Level_hallinta.Instance.tilat.Add(this);
		}

		public virtual bool AnnaTila() {return true;}
	}
	
	//käänteinen tila
	public class InvertoiTila : Tila {
		public Tila invertoitava;
		
		public InvertoiTila(Tila p_invertoitava, string p_label) :base(p_label) {
			invertoitava=p_invertoitava;
		}
		
		public override bool AnnaTila() {
			return !invertoitava.AnnaTila();
		}
	}
	//jonkin tapahtuman tai vastaavan laskuri
	//voidaan asettaa raja ja testata, onko raja saavutettu
	// tila on tosi (tapahtuma voidaan suorittaa), jos arvo ei ole yli rajan ja epätosi (tapahtumaa ei voida suorittaa), jos on yli rajan
	public class Laskuri : Tila {
		public int arvo;
		public int raja;

		public Laskuri(int p_raja, string p_label, bool p_inverted=false) :base(p_label, p_inverted) {
			raja = p_raja;
		}

		public void Nollaa() {
			arvo=0;
		}

		public override bool AnnaTila() {
			return inverted ? !(arvo<raja) : (arvo<raja);
		}

		//palauttaa kopion. annetaan id, jolla yksilöidään labeli toisistaan
		//kopiossa arvo on nolla, eli kopioidaan laskuri alkuparametrien mukaan
		public Laskuri AnnaKopio(int id) {
			return new Laskuri(raja, label + "_" + id.ToString(), inverted);
		}
	}

	//rajapinta levelinhallinnan ulkopuolelle
	//rekisteröi ulkoisen tapahtuman levelinhallintaan
	//rekisteröinti suorittaa jonkin toiminnon levelin hallinnassa, jos levelin hallinta antaa suorittaa sen
	public class Tapahtumien_rekisteroija<T> {
		public Toiminnon_suorittaja toiminto; //toiminnon suorittava olio
		public T kohde_olio; //rekisteröinnin suorittava olio
		bool ohita_levelin_tila=false; //mikäli on true, toiminnon saa suorittaa vielä level completen/failedin jälkeen
		
		public Tapahtumien_rekisteroija(Toiminnon_suorittaja p_toiminto, bool p_ohita_levelin_tila=false) {
			AsetaToiminto(p_toiminto);
			ohita_levelin_tila=p_ohita_levelin_tila;
		}

		public void AsetaToiminto(Toiminnon_suorittaja p_toiminto) {
			toiminto=p_toiminto;
		}

		public void AsetaKohde(T p_kohde_olio) {
			kohde_olio=p_kohde_olio;
		}

		public bool Rekisteroi() { // toiminnon suorittaja voi palauttaa rekisteröinnin kohteelle paluuviestin esim. onko laskuri tullut täyteen
			bool levelin_tila=Level_hallinta.Instance.level_kaynnissa;
			if(levelin_tila || ohita_levelin_tila) {
				bool paluu=toiminto.Suorita();
				if (levelin_tila) Level_hallinta.Instance.PaivitaLevelinTila(); //testataan levelin tilat
				return paluu;
			} else
				return false; // ei saa suorittaa
		}
	}

	//suorittaa toiminnon sille asetetulle toiminnon kohteelle
	//välittää toiminnon kohteelta tulevan paluuarvon rekisteröinnin suorittajalle, joka kertoo voiko tapahtuman suorittaa
	public abstract class Toiminnon_suorittaja {
		public Tila toiminnon_kohde;

		public Toiminnon_suorittaja(Tila p_toiminnon_kohde) {
			toiminnon_kohde = p_toiminnon_kohde;
		}

		public virtual bool Suorita() {return true;}
	}
	//muuttaa sille asetetun laskurin arvoa
	//suorittajalle annetaan muutoksen suuruus
	//kertoo rekisteröijälle, onko laskuri mennyt yli ylärajan (toimintoa ei voi suorittaa) -> katso suorita() kommentit
	public class Muuta_laskuria : Toiminnon_suorittaja {
		public new Laskuri toiminnon_kohde;
		public int muutos=1; //oletusarvo
		public TilanMuuttaja tilan_muuttaja;

		public Muuta_laskuria(Laskuri p_toiminnon_kohde) : base(p_toiminnon_kohde) {
			toiminnon_kohde=p_toiminnon_kohde;

		}

		public void AsetaParametrit(int p_muutos, bool lue_tila_muutoksen_jalkeen=false) {
			muutos = p_muutos;
			tilan_muuttaja=lue_tila_muutoksen_jalkeen ? (TilanMuuttaja) new LueTilaMuutoksenJalkeen(this) : (TilanMuuttaja) new LueTilaEnnenMuutosta(this);
		}

		public override bool Suorita()
		{
			return tilan_muuttaja.MuutaTila();
		}

		//muuttaa laskurin arvon ja palauttaa tilan
		public abstract class TilanMuuttaja {
			public Muuta_laskuria muuta_laskuria;

			public TilanMuuttaja(Muuta_laskuria p_muuta_laskuria) {
				muuta_laskuria=p_muuta_laskuria;
			}

			public abstract bool MuutaTila();
			protected void MuutaArvo() {
				//muutetaan arvoa, mikäli rajaa ei ole saavutettu
				if(((muuta_laskuria.toiminnon_kohde.arvo-muuta_laskuria.toiminnon_kohde.raja)*muuta_laskuria.muutos)<0)
					muuta_laskuria.toiminnon_kohde.arvo += muuta_laskuria.muutos;
			}
		}
		//tila luetaan ennen muutosta
		public class LueTilaEnnenMuutosta : TilanMuuttaja {
			public LueTilaEnnenMuutosta(Muuta_laskuria p_muuta_laskuria) :base(p_muuta_laskuria){}

			public override bool MuutaTila() {
				// saa suorittaa, jos tila on tosi ennen muutosta
				bool tila=muuta_laskuria.toiminnon_kohde.AnnaTila();
				MuutaArvo();
				return tila;
			}
		}
		//tila luetaan muutoksen jälkeen
		public class LueTilaMuutoksenJalkeen : TilanMuuttaja {
			public LueTilaMuutoksenJalkeen(Muuta_laskuria p_muuta_laskuria) :base(p_muuta_laskuria){}
			
			public override bool MuutaTila() {
				// saa suorittaa, jos tila on tosi muutoksen jälkeen
				MuutaArvo();
				bool tila=muuta_laskuria.toiminnon_kohde.AnnaTila();
				return tila;
			}
		}
	}

	//voidaan asettaa testattavaksi useita tiloja ja testata näin laajempien kokonaisuuksien tila
	//testataan esim. level complete
	//tila on true vain, jos kaikki tilat on true. muussa tapauksessa false
	public class TilajoukonTesti {
		public List<Tila> tilat;

		public TilajoukonTesti() {
			tilat=new List<Tila>();
		}

		public bool AnnaTila() {
			bool tila=true;
			if(tilat.Count==0)
				tila=false;
			else {
				tila=tilat.TrueForAll(delegate(Tila obj) {
					return obj.AnnaTila();
				});
			}
			return tila;
		}
	}

	//Kello mittaa ja näyttää ajan, kuinka kauan levelin läpäisy vie
	public class Kello {
		public float aika=0;
		public GUIText kellonaytto;

		public Kello(GUIText p_kellonaytto) {
			kellonaytto=p_kellonaytto;
		}

		public void Paivita() {
			aika+=Time.deltaTime;
			kellonaytto.guiText.text=MuotoileAika(aika);
		}

		public string MuotoileAika(float p_aika) {
			return (Mathf.Round(p_aika*100.0f)/100.0f).ToString();
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
	public abstract class Tila_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Tila_alussa {
			tosi,
			epatosi
		}
		public Tila_alussa tila_alussa;

		[System.Serializable]
		public class Lisaa_Tilajoukon_Testiin_Asetukset {
			public bool lisaa;
			public bool kaanteinen_tila;
		}
		public Lisaa_Tilajoukon_Testiin_Asetukset lisaa_level_complete_ehdoksi;
		public Lisaa_Tilajoukon_Testiin_Asetukset lisaa_level_failed_ehdoksi;
	}
	[System.Serializable]
	public class Laskuri_Parametrit_Hallintapaneeliin : Tila_Parametrit_Hallintapaneeliin {
		public int raja;
	}
	[System.Serializable]
	public class Muuta_laskuria_Parametrit_Hallintapaneeliin {
		public Laskuri_Parametrit_Hallintapaneeliin laskurin_asetukset;
		public int muutos=1; //oletusarvo
		[System.Serializable]
		public enum TilanLukeminen {
			lue_tila_ennen_muutosta,
			lue_tila_muutoksen_jalkeen
		}
		public TilanLukeminen tilan_lukeminen;
	}
	[System.Serializable]
	public class Toiminnon_kohteen_Valitsin {
		[System.Serializable]
		public enum Tyyppilista {
			Muuta_laskuria
		}
		public Tyyppilista tyyppilista; //valitaan toiminnon kohteen tyyppi
		public MonoBehaviour parametrit; //annetaan vastaava parametrien määrittäjä (gameobject, jossa oikeantyyppinen scripti)
		public bool ohita_levelin_tila=false; // mikäli asetettu, toiminnon saa suorittaa levelin tilasta riippumatta
	}
	[System.Serializable]
	public class LisaAsetukset_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public class Kello_asetukset {
			public Kylla_Ei kaytossa;
			public GUIText kellonaytto;
		}
		public Kello_asetukset kello_asetukset;
	}

	//************* rakentajat *********************
	public class Tila_Hallintapaneelista_Rakentaja {
		public void Kasittele(Tila_Parametrit_Hallintapaneeliin parametrit, Tila rakennettu_tila) { //periytettyjen luokkien rakentajat kutsuvat
			Kasittele_Tilajoukkoihin_Lisaaminen(parametrit, rakennettu_tila);
			Level_hallinta.Instance.tilat.Add(rakennettu_tila);
		}

		public void Kasittele_Tilajoukkoihin_Lisaaminen(Tila_Parametrit_Hallintapaneeliin parametrit, Tila rakennettu_tila) {
			Kasittele_Tilajoukkoon_Lisaaminen(parametrit.lisaa_level_complete_ehdoksi.lisaa, parametrit.lisaa_level_complete_ehdoksi.kaanteinen_tila, rakennettu_tila, Level_hallinta.Instance.level_complete);
			Kasittele_Tilajoukkoon_Lisaaminen(parametrit.lisaa_level_failed_ehdoksi.lisaa, parametrit.lisaa_level_failed_ehdoksi.kaanteinen_tila, rakennettu_tila, Level_hallinta.Instance.level_failed);
		}

		public void Kasittele_Tilajoukkoon_Lisaaminen(bool lisataanko, bool inverted, Tila rakennettu_tila, TilajoukonTesti tilajoukon_testi) {
			if(lisataanko)
				if(inverted)
					tilajoukon_testi.tilat.Add(new InvertoiTila(rakennettu_tila, rakennettu_tila.label + "_inverted"));
				else
					tilajoukon_testi.tilat.Add(rakennettu_tila);
		}
	}
	public class Laskuri_Hallintapaneelista_Rakentaja {
		public Laskuri Rakenna(Laskuri_Parametrit_Hallintapaneeliin parametrit, GameObject maaritykset) {
			Laskuri laskuri=new Laskuri(parametrit.raja, maaritykset.name, false); //perusparametrit: alussa tosi -> säädetään parametrit oikeiksi
			//säädetään parametrit
			if(((parametrit.tila_alussa==Laskuri_Parametrit_Hallintapaneeliin.Tila_alussa.epatosi) && ((laskuri.raja-laskuri.arvo)>0)) ||
			   ((parametrit.tila_alussa==Laskuri_Parametrit_Hallintapaneeliin.Tila_alussa.tosi) && ((laskuri.raja-laskuri.arvo)<0)))
				laskuri.inverted=true;
			new Tila_Hallintapaneelista_Rakentaja().Kasittele(parametrit, laskuri); //kaikille Tila-luokille kuuluvat toimenpiteet
			return laskuri;
		}
	}
	public class Muuta_laskuria_Hallintapaneelista_Rakentaja {
		public Muuta_laskuria Rakenna(Muuta_laskuria_Parametrit_Hallintapaneeliin parametrit, GameObject maaritykset) {
			Muuta_laskuria muuta_laskuria;
			muuta_laskuria = new Muuta_laskuria(RakennaLaskuri(parametrit, maaritykset));
			AsetaParametrit(muuta_laskuria, parametrit);
			return muuta_laskuria;
		}

		protected Laskuri RakennaLaskuri(Muuta_laskuria_Parametrit_Hallintapaneeliin parametrit, GameObject maaritykset) {
			return new Laskuri_Hallintapaneelista_Rakentaja().Rakenna(parametrit.laskurin_asetukset, maaritykset);
		}

		protected void AsetaParametrit(Muuta_laskuria muuta_laskuria, Muuta_laskuria_Parametrit_Hallintapaneeliin parametrit) {
			if(((muuta_laskuria.toiminnon_kohde.raja-muuta_laskuria.toiminnon_kohde.arvo)*parametrit.muutos)<0)
				parametrit.muutos*=-1; //muutos kohti rajaa
			bool lue_tila_muutoksen_jalkeen=false;
			if(parametrit.tilan_lukeminen==Muuta_laskuria_Parametrit_Hallintapaneeliin.TilanLukeminen.lue_tila_muutoksen_jalkeen)
				lue_tila_muutoksen_jalkeen=true;
			muuta_laskuria.AsetaParametrit(parametrit.muutos, lue_tila_muutoksen_jalkeen);
		}
	}
	public class LisaAsetukset_Hallintapaneelista_Rakentaja {
		public void Rakenna(LisaAsetukset_Parametrit_Hallintapaneeliin parametrit) {
			Level_hallinta level_hallinta=Level_hallinta.Instance;
			level_hallinta.levelin_kello=new Kello_Rakentaja().Rakenna(parametrit.kello_asetukset);
		}

		public class Kello_Rakentaja {
			public Kello Rakenna(LisaAsetukset_Parametrit_Hallintapaneeliin.Kello_asetukset kello_asetukset) {
				if(kello_asetukset.kaytossa==Kylla_Ei.kylla)
					return new Kello(kello_asetukset.kellonaytto);
				else
					return null;
			}
		}
	}
}