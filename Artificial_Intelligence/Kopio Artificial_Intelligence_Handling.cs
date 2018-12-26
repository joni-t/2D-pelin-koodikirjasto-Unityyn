using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Target_ohjaus;
using Peliukkeli_ohjaus;
using Ohjaus_2D;
using Ohjaus_laite;
using Menu;

namespace Artificial_Intelligence {
	//perii ohjausinput2D:n eli on palautetaava ohjaustapahtumat ja tässä tapauksessa virtuaalinappuloiden painallukset
	//virtuaalinapeissa on toiminnonsuorittaja, joka ohjaa napin varsinaista tilaa, joka ohjaa varsinaisia toimintoja
	public class Artificial_Intelligence_Handling : VirtuaalinappuloidenOhjaaja {
		public Prefab_and_instances this_Prefab_and_instances;
		public Prefab_and_instances.Modified_GameObject this_Modified_GameObject;
		public TargetinHavaitsija targetin_havaitsija=null;
		public EsteenHavaitsija esteen_havaitsija=null;
		//toimintamoodit
		public ToimintaMoodi ei_havaintoa=null;
		public ToimintaMoodi havainto_tehty=null;
		public ToimintaMoodi nakee=null;
		public ToimintaMoodi nakee_takana=null;
		public ToimintaMoodi current_toimintamoodi;
		
		public Artificial_Intelligence_Handling(GameObject this_gameobject) {
			this_Prefab_and_instances=Target_ohjaus.Target_ohjaus.Instance.Get_Prefab_with_Name(this_gameobject.name);
			this_Modified_GameObject=this_Prefab_and_instances.Add_new_GameObject(this_gameobject, null, Vector2.zero, null);
		}
		
		public void LisaaTargetinHavaitsija(TargetinHavaitsija p_targetin_havaitsija) {
			targetin_havaitsija=p_targetin_havaitsija;
		}
		
		public void LisaaEsteenHavaitsija(EsteenHavaitsija p_esteen_havaitsija) {
			esteen_havaitsija=p_esteen_havaitsija;
		}
		
		//lisää toimintamoodin erityyppisiin moodeihin, jotka valitaan
		public void LisaaToimintaMoodi(ToimintaMoodi moodi, bool aseta_ei_havaintoa, bool aseta_havainto_tehty, bool aseta_nakee, bool aseta_nakee_takana) {
			if(aseta_ei_havaintoa) {
				ei_havaintoa=moodi;
				current_toimintamoodi=moodi;
			}
			if(aseta_havainto_tehty)
				havainto_tehty=moodi;
			if(aseta_nakee)
				nakee=moodi;
			if(aseta_nakee_takana)
				nakee_takana=moodi;
		}
		
		//palauttaa true, mikäli moodi vaihtuu
		public bool VaihdaToimintaMoodi (ToimintaMoodi moodi) {
			if (moodi != null) {
				if (!moodi.Equals (current_toimintamoodi)) {
					current_toimintamoodi = moodi;
					moodi.liikekohteiden_maarittaja.AsetaSeuraavaKohde (moodi.liikekohteiden_maarittaja.AnnaSeuraavaKohde ());
					return true;
				} else
					return false;
			} else {
				current_toimintamoodi = null;
				return false;
			}
		}
		
		//ohjaus_handling kutsuu tätä: tästä käynnistyy toimintojen suorittaminen, jonka tavoitteena on painaa virtuaalinappuloita
		//ei siis varsinaisesti palauta ohjaustapahtumia, vaan ratkoo ne (generoi virtuaalinappuloiden tilat) ja asettaa ne suoraan kohteeseen (virtuaalinappuloihin)
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			if(this_Modified_GameObject.gameobject!=null) {
				//bool on_jumissa=false;
				//if(current_toimintamoodi!=null)
				//	on_jumissa=current_toimintamoodi.liikekohteiden_maarittaja.OnkoTodettuJumiutuneeksi();
				if(this_Modified_GameObject.gameobject.activeSelf /*& !on_jumissa*/) {
					//havaitaanko target -> päivitetään toimintamoodi
					if(targetin_havaitsija!=null) {
						targetin_havaitsija.EtsiTarget();
						targetin_havaitsija.Havaitse();
					}
					//havaitaan esteet edessä ja kierretään ne
					if(esteen_havaitsija!=null) {
						esteen_havaitsija.SuoritaToiminnot();
						//liikutaan
						if(current_toimintamoodi!=null)
							current_toimintamoodi.SuoritaToiminnot(esteen_havaitsija.liikutaan_kohti_esteen_jalkeista_positionia, esteen_havaitsija.seuraava_kohde, esteen_havaitsija.liikutaan_kohti_esteen_jalkeista_positionia);
					} else if(current_toimintamoodi!=null) //liikutaan
						current_toimintamoodi.SuoritaToiminnot(false, Vector2.zero, false);
				}
			}
		}
		
		//tarkkailee targettia ja asettaa toimintamoodin sen mukaan
		public class TargetinHavaitsija {
			public Artificial_Intelligence_Handling this_Artificial_Intelligence_Handling;
			public float max_distance_to_target;
			public float nakokentta_asteina;
			public int layerMask;
			public float hakuvali_kun_ei_targettia;
			public float hakuvali_kun_on_target;
			public float nakoyhteyden_testausvali;
			public bool onHavaittu=false; //on ainakin kerran havaittu
			public bool nakyyJuuri=false;
			public bool nakyyJuuriTakana=false;
			private float edel_targetin_hakuaika=-1000000;
			private float edel_nakoyhteyden_testausaika=-1000000;
			bool voi_nakya_takana=false;
			bool voi_nakya_edessa=false;
			GameObject finded_target;
			Vector2 suunta_targettiin;
			float kulma;
			Ray ray;
			RaycastHit hit;
			
			public TargetinHavaitsija(Artificial_Intelligence_Handling p_this_Artificial_Intelligence_Handling, float p_max_distance_to_target, float p_nakokentta_asteina, int p_layerMask, float p_hakuvali_kun_ei_targettia, float p_hakuvali_kun_on_target, float p_nakoyhteyden_testausvali) {
				this_Artificial_Intelligence_Handling=p_this_Artificial_Intelligence_Handling;
				max_distance_to_target=p_max_distance_to_target;
				nakokentta_asteina=p_nakokentta_asteina;
				layerMask=p_layerMask;
				hakuvali_kun_ei_targettia=p_hakuvali_kun_ei_targettia;
				hakuvali_kun_on_target=p_hakuvali_kun_on_target;
				nakoyhteyden_testausvali=p_nakoyhteyden_testausvali;
			}
			
			public void EtsiTarget() {
				bool hae=false;
				//haetaan target määrä välein
				if(this_Artificial_Intelligence_Handling.this_Modified_GameObject.target_gameobject==null) { //jos ei ole targettia
					if((Time.time-edel_targetin_hakuaika)>hakuvali_kun_ei_targettia) hae=true;
				} else if((Time.time-edel_targetin_hakuaika)>hakuvali_kun_on_target) hae=true;
				if(hae) {
					edel_targetin_hakuaika=Time.time;
					finded_target=this_Artificial_Intelligence_Handling.this_Prefab_and_instances.Find_Nearest_Target_for_GameObject_from_given_position(Ohjattava_2D.Convert_to_Vector2(this_Artificial_Intelligence_Handling.this_Modified_GameObject.gameobject.transform.position), max_distance_to_target);
					if(finded_target!=null) //kun on kerran nähty, ei hukata sitä ennen kuin vastaan tulee uusi target
						this_Artificial_Intelligence_Handling.this_Modified_GameObject.target_gameobject=finded_target;
				}
			}
			
			public void Havaitse() {
				nakyyJuuri=false;
				nakyyJuuriTakana=false;
				if(this_Artificial_Intelligence_Handling.this_Modified_GameObject.target_gameobject!=null) { //testataan näköyhteys
					if((Time.time-edel_nakoyhteyden_testausaika)<nakoyhteyden_testausvali) return; //testataan näköyhteys määrävälein
					edel_nakoyhteyden_testausaika=Time.time;
					suunta_targettiin=Ohjattava_2D.Convert_to_Vector2(this_Artificial_Intelligence_Handling.this_Modified_GameObject.target_gameobject.transform.position-this_Artificial_Intelligence_Handling.this_Modified_GameObject.gameobject.transform.position);
					kulma=Vector2.Angle(Ohjattava_2D.Convert_to_Vector2(this_Artificial_Intelligence_Handling.this_Modified_GameObject.gameobject.transform.right), suunta_targettiin);
					voi_nakya_takana=false;
					voi_nakya_edessa=false;
					if(kulma<(nakokentta_asteina/2))
						voi_nakya_edessa=true;
					if((180-kulma)<(nakokentta_asteina/2))
					   voi_nakya_takana=true;
					//Debug.Log("kulma: " + kulma + ", 180-kulma: " + (180-kulma) + ", nakokentta_asteina: " + nakokentta_asteina + ", voi_nakya_edessa: " + voi_nakya_edessa.ToString() + ", voi_nakya_takana: " + voi_nakya_takana.ToString());
					if(voi_nakya_edessa || voi_nakya_takana) {
						ray=new Ray(this_Artificial_Intelligence_Handling.this_Modified_GameObject.gameobject.transform.position, suunta_targettiin);
						if(Physics.Raycast(ray, out hit, max_distance_to_target, layerMask)) { //jos osuu johonkin -> tutkitaan osuma
							if(hit.collider.gameObject.Equals(this_Artificial_Intelligence_Handling.this_Modified_GameObject.target_gameobject)) {
								if(Vector2.Dot(Ohjattava_2D.Convert_to_Vector2(this_Artificial_Intelligence_Handling.this_Modified_GameObject.gameobject.transform.right), suunta_targettiin)>0) {
									//Debug.Log("testataan edesta");
									if(voi_nakya_edessa) {
										//näkee edessä päin
										onHavaittu=true;
										nakyyJuuri=true;
										if(this_Artificial_Intelligence_Handling.VaihdaToimintaMoodi(this_Artificial_Intelligence_Handling.nakee))
											Debug.Log("Vaihdetaan toimintamoodi: nakee");
										return;
									}
								} else if(voi_nakya_takana) {
									//Debug.Log("testataan takaa");
									onHavaittu=true;
									nakyyJuuriTakana=true;
									if(this_Artificial_Intelligence_Handling.VaihdaToimintaMoodi(this_Artificial_Intelligence_Handling.nakee_takana))
										Debug.Log("Vaihdetaan toimintamoodi: nakee takana");
									return;
								}
							}
						}
					}
				}
				//Debug.Log("Testataan onHavaittu: " + onHavaittu.ToString());
				if(onHavaittu) { //on havaittu joskus
					if(this_Artificial_Intelligence_Handling.VaihdaToimintaMoodi(this_Artificial_Intelligence_Handling.havainto_tehty))
						Debug.Log("Vaihdetaan toimintamoodi: havainto tehty");
				}
			}
		}

		//havaitsee esteet ja kiertää ne
		public class EsteenHavaitsija {
			public float liipaisuetaisyys_kohdepositiossa; //reagointietäisyys seuraavassa kohteessa
			public float jumiutuneeksi_rajatun_alueen_sade; //jos ukkeli jää pyörimään alueen sisälle, todetaan se jumiutuneeksi ja liike pysäytetään
			public float havaitsijan_etaisyys_ukkelista; //ukkelin positionista mitattuna. paikka siirtyy kulkusuunnan ja nopeuden mukaan mukaan. annetaan aika jonka kuluessa ukkeli saavuttaisi havaitsijan
			public float esteeseen_reagointietaisyys; //miltä etäisyydeltä esteeseen aletaam reagoimaan (oletetaan että edessä on este). käytetään esteisiin, jotka ovat tyypiltään "aukko lattiassa" eli siis, kun nopeudella ei ole merkitystä
			public Vector2 esteeseen_reagointiaika; //minkä ajan päässä olevaan esteeseen reagoidaan (oletetaan että edessä on este). käytetään esteisiin, jotka ovat tyypiltään "seinä edessä" eli siis, kun nopeudella on merkitystä. annetaan suunnassa x ja y
			public uint seinan_kaltevuusraja; //tätä jyrkemmät katsotaan seinäksi
			public float ukkelin_pituus; //varaa tilaa ukkelille siten, että ukkelin positionista katsoen puolet pituudesta ylös ja puolet alas
			public uint etsinta_alueen_sade; //kuinka pitkälle ulotetaan etsintä
			public Havaitsija havaitsija; //havaitsija, joka muodostuu raycast-hit:sta
			public Prefab_and_instances.Modified_GameObject this_Modified_GameObject;
			public Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija;
			
			List<Reuna> reunat; //havaitut reunat
			Vector2 this_position; //ukkelin paikka
			Vector2 havaitsijan_paikka;
			Vector2 edel_havaitsijan_paikka=Vector2.zero;
			Vector2 edel_kauimmainen_havaitsijan_paikka; //reunojen havannoinnissa edelliselä kerralla kauimmainen havaitsijan paikka
			Vector2 current_velocity; //ukkelin nopeus
			Vector2 current_direction; //ukkelin suunta
			float etaisyys_havaitsijan_edessa_olevaan_esteeseen;
			Vector2 uusi_kauimmainen_havaitsijan_paikka;
			float etaisyys_uusi_ja_edel_havaitijan_paikka;
			List<Reuna> laajennetut_reunat;
			bool havaittu_este_juuri_nyt_edessa;
			Reuna[] tarkkailtavat_reunat=new Reuna[4]; //tarkkailtavat reunat ja perässä niiden indeksit (käyttää: HavaitseReunat)
			const uint havaittu_este=0; //mahdollinen havaittu este
			const uint havaitsijan_edessa_oleva_este=1; //este, joka ei ole vielä liian lähellä (otetaan talteen, koska havaitsija ei välttämättä havaitse estettä enää liian lähellä)
			const uint havaittu_este_tarpeeksi_kaukana=2; //mahdollinen havaitsijan edessä oleva este (reuna) eli havaitsijaa ei pidä siirtää reunan läpi
			const uint havaitsijan_edessa_oleva_este_tarpeeksi_kaukana=3;
			Reuna[] tarkkailtavat_reunat2=new Reuna[3]; // EtsiEsteenJalkeinenPositio käyttää
			const uint nykyinen_este=0;
			const uint edellinen_este=1;
			const uint este_juuri_nyt_tarpeeksi_kaukana=2;
			Reuna uusi_este=null; //eteen tuleva uusi este, kun etsitään esteen jälkeistä positionia
			bool este_on_seina=false; //kertoo, minkälainen este on kyseessä
			uint umpikujalaskuri; //kun etsitään esteen jälkeistä positionia
			Reuna tahan_voi_hypata=null; //paikka, johon voidaan laskeutua, kun hypätään esteen yli
			bool jumissa=false; //esteen jälkeisen paikan etsintä päätyy umpikujaan
			List<Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet> polku=new List<Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet>(); //polku esteen yli tai ohi
			Vector2 havaitsijan_siirtosuunta;
			Vector2 tavoite_suunta; //suunta, mihin pyritään
			float pistetulo;
			Vector2 siirto_suunta; //havaitsijan siirtosuunta
			float siirtoaskel; //havaitsijan
			float havaitsijan_laskennallinen_nopeus; //kuinka nopeasti havaitsijaa liikutetaan kun etsitään esteen jälkeistä positionia
			Ray ray;
			RaycastHit hit;
			float ukkelin_pituus_per_2;
			public bool liikutaan_kohti_esteen_jalkeista_positionia=false; //kertoo, että este ja sen jälkeinen positio on löytynyt -> liikutaan kohti sitä
			Staattinen_LiikeKohteidenMaarittaja esteen_kierto_etappien_maarittaja; //tähän määritetään "polku", joka on suunnitelma esteen kiertämiseksi
			public Vector2 seuraava_kohde; //seuraava kohde polulla
			float putoamiskiihtyvyys_kertaa_2=2*Physics.gravity.y;
			float putoamiskiihtyvyys_kertaa_puoli=0.5f*Physics.gravity.y;
			Vector2 normaali;
			Vector2 vali;
			Vector2 tavoite_suunta_x=Vector2.zero;
			float reunan_kulma=0;
			bool este_on_edessa;
			float etaisyys_reunaan;
			float ajasta_riippuva_esteeseen_reagointietaisyys;
			Vector2 ukkelin_puolikas_mitta_y;
			Vector2 uusi_positio=Vector2.zero;
			float h;
			float t_h;
			float t_total;
			float s;
			float valimatka_x;
			bool hyppaa;
			
			public EsteenHavaitsija(float p_liipaisuetaisyys_kohdepositiossa, float p_jumiutuneeksi_rajatun_alueen_sade, float p_havaitsijan_etaisyys_ukkelista, float p_havaitsija_viiksen_pituus, float p_aukon_minimipituus, uint p_reunan_maksimi_kulmaheitto, uint p_reunojen_hylkaysetaisyys, uint p_kynnys_lkm_hylkaystarkistukseen, float p_esteeseen_reagointietaisyys, Vector2 p_esteeseen_reagointiaika, uint p_seinan_kaltevuusraja, float p_ukkelin_pituus, uint p_etsinta_alueen_sade, int p_layerMask, List<string> p_ei_saa_osua_tagit, Prefab_and_instances.Modified_GameObject p_this_Modified_GameObject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija p_peliukkelin_liikkeen_hallitsija) {
				reunat=new List<Reuna>();
				havaitsija=new Havaitsija(p_havaitsija_viiksen_pituus, p_aukon_minimipituus, p_reunan_maksimi_kulmaheitto, p_reunojen_hylkaysetaisyys, p_kynnys_lkm_hylkaystarkistukseen, p_layerMask, p_ei_saa_osua_tagit);
				for(uint i=0; i<tarkkailtavat_reunat.Length; i++)
					tarkkailtavat_reunat[i]=null;
				for(uint i=0; i<tarkkailtavat_reunat2.Length; i++)
					tarkkailtavat_reunat2[i]=null;

				liipaisuetaisyys_kohdepositiossa=p_liipaisuetaisyys_kohdepositiossa;
				jumiutuneeksi_rajatun_alueen_sade=p_jumiutuneeksi_rajatun_alueen_sade;
				havaitsijan_etaisyys_ukkelista=p_havaitsijan_etaisyys_ukkelista;
				esteeseen_reagointietaisyys=p_esteeseen_reagointietaisyys;
				esteeseen_reagointiaika=p_esteeseen_reagointiaika;
				seinan_kaltevuusraja=p_seinan_kaltevuusraja;
				ukkelin_pituus=p_ukkelin_pituus;
				ukkelin_pituus_per_2=ukkelin_pituus/2;
				etsinta_alueen_sade=p_etsinta_alueen_sade;
				this_Modified_GameObject=p_this_Modified_GameObject;
				peliukkelin_liikkeen_hallitsija=p_peliukkelin_liikkeen_hallitsija;
				edel_kauimmainen_havaitsijan_paikka=Ohjattava_2D.Convert_to_Vector2(this_Modified_GameObject.gameobject.transform.position);
				siirtoaskel=havaitsija.aukon_minimipituus/2;
				ukkelin_puolikas_mitta_y=new Vector2(0, ukkelin_pituus_per_2);
			}
			
			public void SuoritaToiminnot() {
				if(!jumissa) {
					tavoite_suunta=(this_Modified_GameObject.target_position-havaitsijan_paikka).normalized;
					tavoite_suunta_x.x=tavoite_suunta.x;
					HavaitseReunat();
					if(liikutaan_kohti_esteen_jalkeista_positionia) {
						//testataan, ollaanko esteen jälkeisessä positionissa -> valmis
						if(tahan_voi_hypata.MinX() < this_position.x & this_position.x < tahan_voi_hypata.MaxX()) {
							liikutaan_kohti_esteen_jalkeista_positionia=false;
							Debug.Log("este ylitetty! position: " + this_position);
						} else {
							MaaritaSeuraavaValipositioPolulla(); //ohjataan ukkelia
						}
					} else if(tarkkailtavat_reunat[havaittu_este]!=null) { //este havaittu -> etsitään paikka, minne voi hypätä
						Debug.Log("havaittu_este: " + tarkkailtavat_reunat[havaittu_este].ToString() + ", this_position: " + this_position);
						if(EtsiEsteenJalkeinenPositio()) {
							Debug.Log("tahan voi hypata: " + tahan_voi_hypata.ToString() + ", this_position: "  + this_position);
							Debug.DrawLine(tahan_voi_hypata.alkupiste, tahan_voi_hypata.loppupiste, Color.white, 10);
							liikutaan_kohti_esteen_jalkeista_positionia=true;
						}
					}
				}
			}
			
			//tutkii ympäristöä tietyn matkan päässä edessä (tutkii joka suuntaan, mutta havaitsijan paikka on edessä menosuunnassa)
			//muodostaa yhtenäisistä havaintopisteistä reunoja (pintoja)
			public void HavaitseReunat() {
				this_position=Ohjattava_2D.Convert_to_Vector2(this_Modified_GameObject.gameobject.transform.position);
				current_velocity=peliukkelin_liikkeen_hallitsija.GetVelocity();
				current_direction=current_velocity.normalized;
				havaitsijan_paikka=this_position+current_velocity*havaitsijan_etaisyys_ukkelista;
				if(tarkkailtavat_reunat[havaitsijan_edessa_oleva_este]!=null) { //joudutaan ehkä rajoittamaan havaitsijan paikkaa siten, ettei se mene esteen läpi 
					etaisyys_havaitsijan_edessa_olevaan_esteeseen=tarkkailtavat_reunat[havaitsijan_edessa_oleva_este].SuunnattuEtaisyysPisteesta(this_position, current_direction); //otetaan etäisyydestä kymmenys pois (jätetään pelivaraa)
					if(etaisyys_havaitsijan_edessa_olevaan_esteeseen<Vector2.Distance(this_position, havaitsijan_paikka)) //siirretään havaitsijaa
						havaitsijan_paikka=this_position+(havaitsijan_paikka-this_position).normalized*(etaisyys_havaitsijan_edessa_olevaan_esteeseen-esteeseen_reagointietaisyys);
				}
				//asetetaan havaitsija ensin ukkelin kohdalle ja sitten liikutetaan havaitsijaa edellisestä kauimmaisesta havaitsijan pisteestä uuteen kauimmaiseen
				uusi_kauimmainen_havaitsijan_paikka=havaitsijan_paikka;
				if(Vector2.Distance(edel_kauimmainen_havaitsijan_paikka, havaitsijan_paikka)>Vector2.Distance(this_position, havaitsijan_paikka)) { //jos ukkeli meni edellisen havaintolaajuuden yli
					edel_kauimmainen_havaitsijan_paikka=this_position;
				}
				etaisyys_uusi_ja_edel_havaitijan_paikka=Vector2.Distance(uusi_kauimmainen_havaitsijan_paikka, edel_kauimmainen_havaitsijan_paikka);
				havaitsijan_siirtosuunta=(uusi_kauimmainen_havaitsijan_paikka-edel_kauimmainen_havaitsijan_paikka).normalized;
				//Debug.Log("HavaitseReunat, this_position: " + this_position + ", havaitsijan_paikka (kauimmainen): " + havaitsijan_paikka + ", havaitsijan_siirtosuunta: " + havaitsijan_siirtosuunta);
				havaitsijan_paikka=this_position;
				do {
					//Debug.Log("havaitaan, havaitsijan_paikka: " + havaitsijan_paikka);
					//tehdään havainnot uusista reunoista (pinnoista)
					laajennetut_reunat=havaitsija.Havaitse(havaitsijan_paikka, this_position, reunat, true, tarkkailtavat_reunat);
					//havaitaan mahdollinen este (viimeisenä havaittu)
					//havaitaan myös havaitsijan edessä oleva este -> ei siirretä havaitsijaa esteen läpi
					havaittu_este_juuri_nyt_edessa=false;
					tarkkailtavat_reunat[havaittu_este]=null;
					tarkkailtavat_reunat[havaitsijan_edessa_oleva_este]=null;
					//Debug.Log("havaitaan reunat, this_position: " + this_position + ", havaitsijan_paikka: " + havaitsijan_paikka);
					//testataan ensiksi aiemmin talteen otettu liian kaukana oleva este
					if(tarkkailtavat_reunat[havaittu_este_tarpeeksi_kaukana]!=null)
						if(OnkoEdessaEste(tarkkailtavat_reunat[havaittu_este_tarpeeksi_kaukana], this_position, current_velocity, false, ref havaittu_este_juuri_nyt_edessa))
						if(havaittu_este_juuri_nyt_edessa) {
							tarkkailtavat_reunat[havaittu_este]=tarkkailtavat_reunat[havaittu_este_tarpeeksi_kaukana];
							tarkkailtavat_reunat[havaitsijan_edessa_oleva_este]=tarkkailtavat_reunat[havaittu_este_tarpeeksi_kaukana];
						}
					if(tarkkailtavat_reunat[havaitsijan_edessa_oleva_este_tarpeeksi_kaukana]!=null)
						if(OnkoEdessaEste(tarkkailtavat_reunat[havaitsijan_edessa_oleva_este_tarpeeksi_kaukana], havaitsijan_paikka, current_direction, true, ref havaittu_este_juuri_nyt_edessa))
						if(havaittu_este_juuri_nyt_edessa) {
							tarkkailtavat_reunat[havaitsijan_edessa_oleva_este]=tarkkailtavat_reunat[havaitsijan_edessa_oleva_este_tarpeeksi_kaukana];
						}
					//testataan nyt tielle osuneet
					laajennetut_reunat.ForEach(delegate(Reuna obj) {
						if(OnkoEdessaEste(obj, this_position, current_velocity, false, ref havaittu_este_juuri_nyt_edessa)) {
							if(havaittu_este_juuri_nyt_edessa) {
								//Debug.Log("havaittu_este_juuri_nyt_edessa");
								tarkkailtavat_reunat[havaittu_este]=obj;
								tarkkailtavat_reunat[havaitsijan_edessa_oleva_este]=obj;
							} // else Debug.Log("havaittu_este_ei_juuri_nyt_edessa");
							tarkkailtavat_reunat[havaittu_este_tarpeeksi_kaukana]=obj;
							tarkkailtavat_reunat[havaitsijan_edessa_oleva_este_tarpeeksi_kaukana]=obj;
						}
						if(OnkoEdessaEste(obj, havaitsijan_paikka, current_direction, true, ref havaittu_este_juuri_nyt_edessa)) {
							if(havaittu_este_juuri_nyt_edessa) {
								tarkkailtavat_reunat[havaitsijan_edessa_oleva_este]=obj;
							}
							tarkkailtavat_reunat[havaitsijan_edessa_oleva_este_tarpeeksi_kaukana]=obj;
						}
					});
					//siirretään havaitsijaa
					if(Vector2.Distance(havaitsijan_paikka, this_position)==0) //jos havaitsija on ukkelin kohdalla (ensimmäinen havaintopaikka)
						havaitsijan_paikka=edel_kauimmainen_havaitsijan_paikka; //tehdään siirto edellisestä kauimmaisesta havaitsijan paikasta
					if(etaisyys_uusi_ja_edel_havaitijan_paikka>Vector2.Distance(edel_kauimmainen_havaitsijan_paikka, havaitsijan_paikka)) { //jos havaitsija ei ole päätepisteessään
						havaitsijan_paikka+=havaitsijan_siirtosuunta*siirtoaskel; //siirretään havaitsijaa
						if(etaisyys_uusi_ja_edel_havaitijan_paikka<Vector2.Distance(edel_kauimmainen_havaitsijan_paikka, havaitsijan_paikka)) //jos meni yli päätepisteen
							havaitsijan_paikka=uusi_kauimmainen_havaitsijan_paikka;
					} else if(Vector2.Distance(havaitsijan_paikka, uusi_kauimmainen_havaitsijan_paikka)==0)
						break;
					
					/*Debug.Log("laajennetut reunat:");
					laajennetut_reunat.ForEach(delegate(Reuna obj) {
						Debug.Log(obj.ToString());
					});
					Debug.Log("Kaikki reunat:");
					reunat.ForEach(delegate(Reuna obj) {
						Debug.Log(obj.ToString());
					});*/
					
				} while(tarkkailtavat_reunat[havaittu_este]==null);
				edel_kauimmainen_havaitsijan_paikka=uusi_kauimmainen_havaitsijan_paikka;
				/*if(tarkkailtavat_reunat[havaittu_este]!=null) {
					Debug.Log("este havaittu, reunat:");
					laajennetut_reunat.ForEach(delegate(Reuna obj) {
						Debug.Log(obj.ToString());
					});
				}*/
			}

			//kun este on havaittu, etsitään laskeutumispaikka
			public bool EtsiEsteenJalkeinenPositio() {
				Debug.Log("Etsitaan esteen jalkeista positionia");
				tahan_voi_hypata=null;
				umpikujalaskuri=0; //kasvatetaan laskuria, kun on epäilyt juuttumisesta umpikujaan -> kun laskuri kasva riittävästi, todetaan olevan umpikujassa
				polku.Clear();
				havaitsijan_paikka=this_position;
				umpikujalaskuri=0;
				//tehdään ensimmäinen siirto, ettei havaitsija ole this_positionissa
				siirto_suunta=tarkkailtavat_reunat[havaittu_este].Direction();
				if(Vector2.Dot(siirto_suunta, tavoite_suunta)<0) siirto_suunta*=-1; //suunta tavoitetta kohti
				havaitsijan_paikka+=siirtoaskel*siirto_suunta;
				polku.Add(new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(new Vector2(havaitsijan_paikka.x, havaitsijan_paikka.y), null));
				edel_havaitsijan_paikka.x = havaitsijan_paikka.x;
				edel_havaitsijan_paikka.y = havaitsijan_paikka.y;
				tarkkailtavat_reunat2[nykyinen_este]=tarkkailtavat_reunat[havaittu_este];
				uusi_este=tarkkailtavat_reunat2[nykyinen_este];
				tarkkailtavat_reunat2[edellinen_este]=null;
				tarkkailtavat_reunat2[este_juuri_nyt_tarpeeksi_kaukana]=null;
				havaitsijan_laskennallinen_nopeus=esteeseen_reagointietaisyys/Vector2.Dot(esteeseen_reagointiaika, siirto_suunta); //lasketaan havaitsijalle laskennallinen nopeus siten, että ajasta riippuva esteeseen reagointietäisyys on esteeseen (paikallaan) reagointietäisyyden verran
				//tutkitaan kunnes löytyy laskeutumispaikka tai mennään etsintäalueen ulkopuolelle
				while(tahan_voi_hypata==null && Vector2.Distance(this_position, havaitsijan_paikka)<=etsinta_alueen_sade) {
					//siirretään havaitsijaa
					if(uusi_este!=null) {
						//Debug.Log("edessa on este: " + uusi_este.ToString());
						if(!uusi_este.Equals(tarkkailtavat_reunat2[nykyinen_este])) { //este vaihtuu -> joudutaan ratkaisemaan siirtosuunta suhteessa esteeseen
							//Debug.Log("este vaihtui");
							if(tarkkailtavat_reunat2[nykyinen_este]!=null) {
								if(tarkkailtavat_reunat2[nykyinen_este].Equals(tarkkailtavat_reunat2[edellinen_este])) //kasvatetaan umpikujalaskuria
									if(++umpikujalaskuri>30) { //umpikuja -> lopetetaan
										Debug.Log("umpikuja");
										return false;
									}
							}
							tavoite_suunta=(this_Modified_GameObject.target_position-havaitsijan_paikka).normalized;
							pistetulo=Vector2.Dot(uusi_este.ProjectPointLine(havaitsijan_paikka).normalized, tavoite_suunta);
							if(pistetulo>0) { //tavoitesuunnassa on este edessä -> kierretään pois päin this_positionista
								siirto_suunta=uusi_este.Direction();
								if(Vector2.Dot(siirto_suunta, havaitsijan_paikka-this_position)<0) siirto_suunta*=-1; //suunta poispäin this_positionista
								//siirretään havaitsija lähelle esteeseen reagointietäisyyttä
								normaali=uusi_este.ProjectPointLine(havaitsijan_paikka);
								havaitsijan_paikka=havaitsijan_paikka+normaali-normaali.normalized*esteeseen_reagointietaisyys;
							} else siirto_suunta=tavoite_suunta; //este ei ole kulkusuunnassa edessä
							tarkkailtavat_reunat2[edellinen_este]=tarkkailtavat_reunat2[nykyinen_este];
							tarkkailtavat_reunat2[nykyinen_este]=uusi_este;
						}
					} else {
						if(tarkkailtavat_reunat2[nykyinen_este]!=null) { //muuttuu esteettömäksi
							siirto_suunta=tavoite_suunta; //ei ole estettä kulkusuunnassa edessä
							tarkkailtavat_reunat2[edellinen_este]=tarkkailtavat_reunat2[nykyinen_este];
							tarkkailtavat_reunat2[nykyinen_este]=null;
						}
						//Debug.Log("ei ole estetta");
					}
					if(tarkkailtavat_reunat2[nykyinen_este]!=null || tarkkailtavat_reunat2[edellinen_este]!=null) //siirtosuunta muuttuu -> päivitetään havaitsijan_laskennallinen_nopeus
						havaitsijan_laskennallinen_nopeus=esteeseen_reagointietaisyys/Vector2.Dot(esteeseen_reagointiaika, siirto_suunta);
					havaitsijan_paikka+=siirtoaskel*siirto_suunta;
					//Debug.Log("havaitsijan_paikka: " + havaitsijan_paikka);
					//testataan, että polun edellisestä kohdepisteestä on näköyhteys uuteen paikkaan
					vali=havaitsijan_paikka-polku[polku.Count-1].koordinaatti;
					ray=new Ray(polku[polku.Count-1].koordinaatti, vali);
					if(Physics.Raycast(ray, out hit, vali.magnitude)) { //jos välissä on este -> lisätään uusi kohde polulle
						polku.Add(new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(new Vector2(edel_havaitsijan_paikka.x, edel_havaitsijan_paikka.y), null));
						Debug.DrawLine(polku[polku.Count-2].koordinaatti, polku[polku.Count-1].koordinaatti, Color.white, 10);
						Debug.Log("lisataan etappi: " + polku[polku.Count-1].koordinaatti);
					}
					//havaitaan uudet reunapisteet
					List<Reuna> laajennetut_reunat=havaitsija.Havaitse(havaitsijan_paikka, this_position, reunat, false, tarkkailtavat_reunat2);
					//havaitaan mahdollinen (uusi) este (viimeisenä havaittu)
					uusi_este=null;
					havaittu_este_juuri_nyt_edessa=false;
					//testataan ensiksi aiemmin talteen otettu liian kaukana oleva este
					if(tarkkailtavat_reunat2[este_juuri_nyt_tarpeeksi_kaukana]!=null)
						if(OnkoEdessaEste(tarkkailtavat_reunat2[este_juuri_nyt_tarpeeksi_kaukana], havaitsijan_paikka, tavoite_suunta*havaitsijan_laskennallinen_nopeus, true, ref havaittu_este_juuri_nyt_edessa))
							if(havaittu_este_juuri_nyt_edessa) {
								//Debug.Log("aiemmin kaukana ollut este on nyt lahella");
								uusi_este=tarkkailtavat_reunat2[este_juuri_nyt_tarpeeksi_kaukana];
							}
					//testataan juuri eteen tulleet
					laajennetut_reunat.ForEach(delegate(Reuna obj) {
						if(OnkoEdessaEste(obj, havaitsijan_paikka, tavoite_suunta*havaitsijan_laskennallinen_nopeus, true, ref havaittu_este_juuri_nyt_edessa)) {
							if(havaittu_este_juuri_nyt_edessa) {
								//Debug.Log("havaittu_este_juuri_nyt_edessa");
								uusi_este=obj;
							} //else Debug.Log("este_juuri_nyt_tarpeeksi_kaukana");
							tarkkailtavat_reunat2[este_juuri_nyt_tarpeeksi_kaukana]=obj;
						}
					});
					//jos edessä ei ole nyt (enää) estettä, selvitetään voisiko johonkin hypätä
					int mitka_pisteet=0;
					int mitka_pisteet2=0;
					if(uusi_este==null) //jos ei ole (enää) estettä
						foreach(Reuna reuna in laajennetut_reunat)
						if(VoikoTahanHypata(reuna, havaitsijan_paikka))
							//hyppypaikan pitää olla lähempänä kohdepositionia kuin esteen
							if(tarkkailtavat_reunat[havaittu_este].Etaisyys(this_Modified_GameObject.target_position, ref mitka_pisteet)>reuna.Etaisyys(this_Modified_GameObject.target_position, ref mitka_pisteet2)) {
								tahan_voi_hypata=reuna;
								//lasketaan loppupositio reunan tasolle
								havaitsijan_paikka.y=(reuna.loppupiste.y-reuna.alkupiste.y)/(reuna.loppupiste.x-reuna.alkupiste.x)*(havaitsijan_paikka.x-reuna.alkupiste.x)+reuna.alkupiste.y+ukkelin_pituus_per_2;
								polku.Add(new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(new Vector2(havaitsijan_paikka.x, havaitsijan_paikka.y), null));
								Debug.DrawLine(polku[polku.Count-2].koordinaatti, polku[polku.Count-1].koordinaatti, Color.white, 10);
								Debug.Log("lisataan etappi: " + polku[polku.Count-1].koordinaatti);
								//poistetaan ensimmäinen etapii, koska se on tämän hetkinen positio
								polku.RemoveAt(0);
								esteen_kierto_etappien_maarittaja=new Staattinen_LiikeKohteidenMaarittaja(polku, false, 0, this_Modified_GameObject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade);
								MaaritaSeuraavaValipositioPolulla();
								return true;
							}
					edel_havaitsijan_paikka=new Vector2(havaitsijan_paikka.x, havaitsijan_paikka.y);
				}
				Debug.Log("Ei loytynyt esteen jalkeista positionia, havaitsijan_paikka: " + havaitsijan_paikka + ", target_position: " + this_Modified_GameObject.target_position + ", this_position: " + this_position);
				Debug.Log("etaisyys: " + Vector2.Distance(this_position, havaitsijan_paikka) + ", etsinta_alueen_sade: " + etsinta_alueen_sade);
				return false;
			}

			//asettaa seuraavan koordinaatin esteen ylityspolulla riippuen ukkelin sijainnista ja seuraavasta kohteesta (ja edessä olevasta esteestä)
			public void MaaritaSeuraavaValipositioPolulla() {
				//määritetään seuraava välipositio polulta
				this_Modified_GameObject.target_position=esteen_kierto_etappien_maarittaja.AnnaSeuraavaKohde(); //vaihdetaan esteen havaitsijan tarjoama
				esteen_kierto_etappien_maarittaja.TarkistaSijainti(null);
				seuraava_kohde=esteen_kierto_etappien_maarittaja.AnnaSeuraavaKohde();
				//selvitetään, että liike kantaa seuraavaan kohteeseen -> ohjataan tarvittaessa hyppäämään korkeammalle
				seuraava_kohde=EnnakoiHyppaamalla(seuraava_kohde, current_velocity);
				if(tarkkailtavat_reunat[havaittu_este]!=null) { //este havaittu -> joudutaan muuttamaan suuntaa suhteessa esteeseen, jotta ei mennä liian lähelle sitä
					seuraava_kohde=SiirraKohdePositioPoisReunaanPain(seuraava_kohde, tarkkailtavat_reunat[havaittu_este]);
					esteen_kierto_etappien_maarittaja.SiirraSeuraavaKohde(seuraava_kohde);
					//Debug.Log("kierretaan este: " + seuraava_kohde);
				}
			}

			//parametrit reunan tyypin toteamiseen
			public void MuodostaReunanTutkimisParametrit(Reuna reuna, ref float reunan_kulma) {
				reunan_kulma=Reuna.Angle_0_90(reuna.Angle(tavoite_suunta_x));
			}
			
			//ilmoittaa esteestä, kun se on riittävän lähellä
			//jos OnkoEdessaReuna on asetettu, reunan ei tarvitse olla este vaan kaikki edessä olevat reunat huomioidaan, mutta ei esteenä
			//este_on_juuri_nyt_edessa on false, mikäli este ei ole vielä liian lähellä
			public bool OnkoEdessaEste(Reuna reuna, Vector2 position, Vector2 nopeus, bool OnkoEdessaReuna, ref bool este_on_juuri_nyt_edessa) {
				este_on_juuri_nyt_edessa=false;
				MuodostaReunanTutkimisParametrit(reuna, ref reunan_kulma);
				if(reunan_kulma>seinan_kaltevuusraja) //seinä
					este_on_seina=true;
				else
					este_on_seina=false;
				//Debug.Log ("OnkoEdessaEste: " + reuna.ToString () + ", position: " + position + ", nopeus: " + nopeus + ", OnkoEdessaReuna: " + OnkoEdessaReuna.ToString() + ", este_on_seina: " + este_on_seina.ToString());
				if(nopeus.magnitude>0) { //on liikkeessä -> otetaan huomioon liikesuunta ja suuruus
					este_on_edessa=false;
					etaisyys_reunaan=reuna.SuunnattuEtaisyysPisteesta(position, nopeus.normalized);
					ajasta_riippuva_esteeseen_reagointietaisyys=Vector2.Dot(nopeus, esteeseen_reagointiaika);
					if(ajasta_riippuva_esteeseen_reagointietaisyys<esteeseen_reagointietaisyys) ajasta_riippuva_esteeseen_reagointietaisyys=esteeseen_reagointietaisyys; //jos menee alle nolla-nopeuden rajan
					//Debug.Log("etaisyys_reunaan: " + etaisyys_reunaan);
					if(etaisyys_reunaan<Mathf.Infinity) {
						//Debug.Log("este on ainakin kaukana edessa");
						este_on_edessa=true; //este on ainakin kaukana edessä
						if(etaisyys_reunaan<=ajasta_riippuva_esteeseen_reagointietaisyys)
							este_on_juuri_nyt_edessa=true; //este on reagointietäisyyden sisällä
					} else { //testataan myös ylhäältä
						etaisyys_reunaan=reuna.SuunnattuEtaisyysPisteesta(position+ukkelin_puolikas_mitta_y, nopeus.normalized);
						if(etaisyys_reunaan<=ajasta_riippuva_esteeseen_reagointietaisyys) {
							//Debug.Log("este on ylhaalla");
							este_on_edessa=true;
							este_on_juuri_nyt_edessa=true;
						} else {
							etaisyys_reunaan=reuna.SuunnattuEtaisyysPisteesta(position-ukkelin_puolikas_mitta_y, nopeus.normalized);
							if(etaisyys_reunaan<=ajasta_riippuva_esteeseen_reagointietaisyys)
								if(!reuna.saa_osua || OnkoEdessaReuna) { //ja alhaalta mikäli ei saa osua tai riittää, että on reuna
									//Debug.Log("este on alhaalla");
									este_on_edessa=true;
									este_on_juuri_nyt_edessa=true;
								}
						}
					}
					//Debug.Log("este_on_juuri_nyt_edessa: " + este_on_juuri_nyt_edessa.ToString());
					if(este_on_edessa) {
						Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
						return true;
					} else if((!este_on_seina) && (!OnkoEdessaReuna)) //testataan lattian loppuminen
						if(reuna.Etaisyys(position-new Vector2(0, ukkelin_pituus_per_2), nopeus.normalized)<=esteeseen_reagointietaisyys) {
							//Debug.Log("lattia loppuu vauhdissa");
							Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
							este_on_juuri_nyt_edessa=true;
							return true;
						}
				} else if(!OnkoEdessaReuna) {
					if(este_on_seina) { //seinä
						if(reuna.LyhinEtaisyysPisteeseen(position)<=esteeseen_reagointietaisyys) {
							//Debug.Log("edessa on seina");
							Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
							este_on_juuri_nyt_edessa=true;
							return true;
						}
					} else { //lattia (loppuu)
						if(reuna.Etaisyys(position-new Vector2(0, ukkelin_pituus_per_2), tavoite_suunta)<=esteeseen_reagointietaisyys) {
							//Debug.Log("lattia loppuu");
							Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
							este_on_juuri_nyt_edessa=true;
							return true;
						}
					}
				}
				return false;
			}

			//ilmoittaa, voiko tähän hypätä
			public bool VoikoTahanHypata(Reuna reuna, Vector2 position) {
				MuodostaReunanTutkimisParametrit(reuna, ref reunan_kulma);
				//Debug.Log ("VoikoTahanHypata: " + reuna.ToString () + ", position: " + position);
				if(reunan_kulma>seinan_kaltevuusraja) { //seinä
					return false;
				} else { //lattia
					//Debug.Log ("normaali_y: " + reuna.ProjectPointLine (position).y + ", minX_ehto: " + (reuna.MinX () < position.x).ToString () + ", maxX_ehto: " + (position.x < reuna.MaxX ()).ToString ());
					if(reuna.ProjectPointLine(position).y<0 && reuna.MinX() < position.x && position.x < reuna.MaxX() && reuna.saa_osua) {
						Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.yellow, 2);
						//Debug.Log("Voi hypata");
						return true;
					}
				}
				//Debug.Log("Ei voi hypata");
				return false;
			}

			//kun ukkelia liikutetaan kiertäen estettä, joudutaan seuraavaa kohdepositionia siirtämään, jos eteen tulee este
			//tämä siirtää positionia normaalin suuntaan (pois päin reunasta), kunnes kohdepositio on samassa suunnassa kuin esteen reuna (tai esteestä poispäin)
			public Vector2 SiirraKohdePositioPoisReunaanPain(Vector2 kohde_positio, Reuna este_reuna) {
				normaali=este_reuna.ProjectPointLine(kohde_positio).normalized;
				pistetulo=Vector2.Dot(normaali, (kohde_positio-this_position).normalized);
				uusi_positio.x=kohde_positio.x; uusi_positio.y=kohde_positio.y;
				while(pistetulo>0) {
					uusi_positio-=normaali*havaitsija.aukon_minimipituus;
					normaali=este_reuna.ProjectPointLine(uusi_positio).normalized;
					pistetulo=Vector2.Dot(normaali, (uusi_positio-this_position).normalized);
				}
				return uusi_positio;
			}

			//asettaa ukkelin hyppäämään ennakkoon, jotta lento kantaa varmasti kohdepositioniin asti
			//kantamaa vahditaan koko lennon ajan (eli kutsutaan jatkuvasti) ja kun se riittää vapaa-pudotuksella, hyppyyn pakottaminen keskeytetään
			public Vector2 EnnakoiHyppaamalla(Vector2 kohde_positio, Vector2 nopeus) {
				//lasketaan, mihin päädytään tämän hetkisellä tilalla (nopeudella ja positionilla)
				if(nopeus.y>0) { //jos nousee ylös päin
					h=Mathf.Pow(nopeus.y,2)/(putoamiskiihtyvyys_kertaa_2); //lakikorkeus
					t_h=nopeus.y/Physics.gravity.y; //nousuaika
				} else {
					h=0;
					t_h=0;
				}
				if((this_position.y+h)>kohde_positio.y) // jos ja/tai menee korkeammalle kuin kohde
					t_total=t_h+Mathf.Sqrt((this_position.y-kohde_positio.y+h)/putoamiskiihtyvyys_kertaa_puoli); //kokonaislentoaika siihen asti kunnes y-koordinaatti on kohde-positionin tasolla
				else
					t_total=0;
				//lasketaan kantama kokonaisajalle
				s=nopeus.x*t_total;
				valimatka_x=kohde_positio.x-this_position.x;
				hyppaa=false;
				if((s*valimatka_x)>0) { //menee oikeaan suuntaan -> lasketaan, riittääkö kantama
					if((Mathf.Abs(s)-Mathf.Abs(valimatka_x))<0) //jos kantama ei riitä, ohjataan hyppäämään (tai jatkamaan hyppäämistä) korkeammalle
						hyppaa=true;
				} else
					hyppaa=true;
				if(hyppaa) {
					Debug.Log("hyppaa: " + this_position);
					if(valimatka_x>0)
						kohde_positio=this_position+new Vector2(havaitsija.aukon_minimipituus, havaitsija.aukon_minimipituus);
					else
						kohde_positio=this_position+new Vector2(-havaitsija.aukon_minimipituus, havaitsija.aukon_minimipituus);
				}
				return kohde_positio;
			}
		}

		//raycast-hit ympäristön havaitsija
		public class Havaitsija {
			public float havaitsija_viiksen_pituus; //viiksen pituus nopeuden ollessa nolla viiksen suuntaan päin
			public float aukon_minimipituus; //vain tätä suuremmat aukot tunnistetaan aukoksi
			public uint reunan_maksimi_kulmaheitto; //kun pisteistä muodostetaan reuna ja laajennetaan pidemmäksi, saa jatkeen kulmaero heittää korkeintaan tämän
			public uint reunojen_hylkaysetaisyys; //tätä kauemmat reunat poistetaan
			public uint kynnys_lkm_hylkaystarkistukseen; //tarkistaa ja hylkää kaukaisia reunoja vain, jos reunojen määrä ylittää kynnyksen
			public int layerMask;
			public List<string> ei_saa_osua_tagit; //lista prefabeista, joihin ei saa osua tai kuolee
			
			public Havaitsijan_Viiksi[] esteen_havaitsijan_viikset=new Havaitsijan_Viiksi[8];
			public List<Reuna> laajennetut_reunat=new List<Reuna>();

			public Ray ray;
			public RaycastHit hit;
			int mika=-1; //turha parametri
			List<Reuna> poistettavat=new List<Reuna>();
			string tag;
			Vector2 uusi_piste=Vector2.zero;
			Vector2 uusi_piste2=Vector2.zero;
			Vector2 viiksen_suunta=Vector2.zero;
			float viiksen_kulma_nyt;
			bool paa_viiksi;
			float aukon_minimipituus_per_2;
			Vector2 ratkaistu_piste;
			float delta_angle;
			float pi_kertaa_2=2*Mathf.PI;
			bool siirretaan_hieman=false; //havaitsijan viikseä siirretään silloin hieman, kun pääviiksi laajentaa jotain reunaa (havaintopiste jää reunan päähän) -> halutaan tietää laajemmin
			Vector2 hieman_siirtosuunta=Vector2.zero;
			bool saa_osua;
			List<Reuna> lahimmat_reunat;
			bool kuuluu_johonkin_reunaan=false;
			bool ennestaan_laajentamaton_reuna;
			bool laajennus_ok;
			bool saa_osua2;
			
			public Havaitsija(float p_havaitsija_viiksen_pituus, float p_aukon_minimipituus, uint p_reunan_maksimi_kulmaheitto, uint p_reunojen_hylkaysetaisyys, uint p_kynnys_lkm_hylkaystarkistukseen, int p_layerMask, List<string> p_ei_saa_osua_tagit) {
				havaitsija_viiksen_pituus=p_havaitsija_viiksen_pituus;
				aukon_minimipituus=p_aukon_minimipituus;
				reunan_maksimi_kulmaheitto=p_reunan_maksimi_kulmaheitto;
				reunojen_hylkaysetaisyys=p_reunojen_hylkaysetaisyys;
				kynnys_lkm_hylkaystarkistukseen=p_kynnys_lkm_hylkaystarkistukseen;
				layerMask=p_layerMask;
				ei_saa_osua_tagit=p_ei_saa_osua_tagit;

				//havaitsijat 45-asteen kulman välein
				for(int i=0; i<8; i++)
					esteen_havaitsijan_viikset[i]=new Havaitsijan_Viiksi(this, Mathf.PI/4*i);
				
				aukon_minimipituus_per_2=aukon_minimipituus/2;
			}
			
			//palauttaa laajennetut reunat
			//tarkkailtava reuna on erityinen ja jos se vaihtuu, muutos välittyy kutsujalle
			public List<Reuna> Havaitse(Vector2 havaitsijan_paikka, Vector2 this_position, List<Reuna> reunat, bool poista_kaukaiset_reunat, Reuna[] tarkkailtavat_reunat) {
				poistettavat.Clear();
				foreach(Havaitsijan_Viiksi viiksi in esteen_havaitsijan_viikset) {
					viiksen_suunta.x=Mathf.Cos(viiksi.viiksen_kulma); viiksen_suunta.y=Mathf.Sin(viiksi.viiksen_kulma);
					viiksen_kulma_nyt=viiksi.viiksen_kulma;
					siirretaan_hieman=false; //havaitsijan viikseä siirretään silloin hieman, kun pääviiksi laajentaa jotain reunaa (havaintopiste jää reunan päähän) -> halutaan tietää laajemmin
					paa_viiksi=true;
					while(paa_viiksi || siirretaan_hieman) {
						paa_viiksi=false;
						if(siirretaan_hieman) {
							//siirretään havaintopistettä hieman eteenpäin -> saadaan lisää tietoa reunan laajuudesta
							//määritetään viiksen suunta
							ratkaistu_piste=uusi_piste+hieman_siirtosuunta*aukon_minimipituus_per_2; //haluttu havaintopiste
							delta_angle=Reuna.DirectedAngle(uusi_piste-havaitsijan_paikka, ratkaistu_piste-havaitsijan_paikka)*Mathf.Deg2Rad;
							//Debug.Log("ratkaistu_piste: " + ratkaistu_piste + ", viiksen_kulma_aiemmin: " + viiksen_kulma_nyt);
							viiksen_kulma_nyt+=delta_angle;
							if(viiksen_kulma_nyt>pi_kertaa_2)
								viiksen_kulma_nyt-=pi_kertaa_2;
							viiksen_suunta.x=Mathf.Cos(viiksen_kulma_nyt); viiksen_suunta.y=Mathf.Sin(viiksen_kulma_nyt);
						}
						siirretaan_hieman=false;
						ray=new Ray(havaitsijan_paikka, viiksen_suunta);
						Debug.DrawLine(havaitsijan_paikka, Ohjattava_2D.Convert_to_Vector3(havaitsijan_paikka+havaitsija_viiksen_pituus*viiksen_suunta), Color.yellow, 0.1f);
						if(Physics.Raycast(ray, out hit, havaitsija_viiksen_pituus, layerMask)) { //jos osuu johonkin -> rekisteröidään piste
							tag=hit.collider.tag;
							saa_osua=!ei_saa_osua_tagit.Contains(tag);
							uusi_piste=Ohjattava_2D.Convert_to_Vector2(hit.point);
							//Debug.Log("uusi_piste: " + uusi_piste + ", viiksen_kulma: " + viiksen_kulma + ", viiksen_kulma_nyt: " + viiksen_kulma_nyt);
							//etsitään riittävän lähimmät reunat
							lahimmat_reunat=reunat.FindAll(delegate(Reuna obj) {
								return ((obj.Etaisyys(uusi_piste, ref mika)<=aukon_minimipituus) || obj.OnkoPisteReunanSisalla(uusi_piste, reunan_maksimi_kulmaheitto));
							});
							//voisiko lähempää reunapistettä siirtää (jatkaa reunaa) uuteen havaintopisteeseen
							kuuluu_johonkin_reunaan=false;
							foreach(Reuna reuna in lahimmat_reunat) {
								if(Vector2.Distance(reuna.alkupiste, reuna.loppupiste)==0) {
									ennestaan_laajentamaton_reuna=true; //reunaan ei ole vielä yhdistetty yhtä enempää pistettä -> suunta ei tiedossa
								}
								else
									ennestaan_laajentamaton_reuna=false;
								//Debug.Log("reuna: " + reuna + ", ennestaan_laajentamaton_reuna: " + ennestaan_laajentamaton_reuna.ToString());
								if(reuna.LaajennaReunaJosEhdotTayttyy(uusi_piste, saa_osua, reunan_maksimi_kulmaheitto, aukon_minimipituus)) {
									laajennus_ok=false;
									if(ennestaan_laajentamaton_reuna) { //testataan, että todellakin on reuna eikä esim. pisteet kohtisuorassa olevilla reunoilla kulmassa
										ray=new Ray(havaitsijan_paikka, (Vector2.Lerp(reuna.alkupiste, reuna.loppupiste, 0.5f)-havaitsijan_paikka).normalized);
										if(Physics.Raycast(ray, out hit, havaitsija_viiksen_pituus, layerMask)) { //jos osuu johonkin
											saa_osua2=!ei_saa_osua_tagit.Contains(hit.collider.tag);
											uusi_piste2=Ohjattava_2D.Convert_to_Vector2(hit.point);
											if(reuna.LaajennaReunaJosEhdotTayttyy(uusi_piste2, saa_osua2, reunan_maksimi_kulmaheitto, aukon_minimipituus))
												if(Vector2.Distance(reuna.alkupiste, reuna.loppupiste)>0) //jos ei laajenna olemassa olevaan pisteeseen
													laajennus_ok=true;
										}
										if(!laajennus_ok)
											reuna.PalautaNollaPituiseksi(uusi_piste); //kumotaan laajennus
									} else
										laajennus_ok=true;
									//Debug.Log("laajennus_ok: " + laajennus_ok.ToString());
									if(laajennus_ok) {
										kuuluu_johonkin_reunaan=true;
										if(!laajennetut_reunat.Contains(reuna))
											if(Vector2.Distance(reuna.alkupiste, reuna.loppupiste)>aukon_minimipituus) //pitää ylittää aukon minimipituus
												laajennetut_reunat.Add(reuna);
										//pitäisikö havaintopistettä siirtää, eli havaintopiste on nyt reunan tämänhetkisessä päätepisteessä
										if(reuna.Etaisyys(uusi_piste, ref mika)==0) {
											siirretaan_hieman=true;
											hieman_siirtosuunta=reuna.Direction();
											if(mika==0) //havaintopisteen kohdalla on alkupiste
												hieman_siirtosuunta*=(-1);
											//Debug.Log("tutkittavana_oleva_reuna: " + reuna.ToString());
										}
									}
								}
							}
							if(kuuluu_johonkin_reunaan) {
								//yhdistetään samansuuntaiset vierekkäiset reunat, jos tulivat riittävän lähelle toisiaan
								YhdistaSamansuuntaisetVierekkaisetReunat(lahimmat_reunat, laajennetut_reunat, reunat, tarkkailtavat_reunat);
							} else {
								//perustetaan uusi reuna
								//Debug.Log("uusi reuna");
								reunat.Add(new Reuna(new Vector2(uusi_piste.x, uusi_piste.y), new Vector2(uusi_piste.x, uusi_piste.y), saa_osua, tag));
							}
						}
					}
				}
				//yhdistetään samansuuntaiset vierekkäiset reunat, jos tulivat riittävän lähelle toisiaan
				YhdistaSamansuuntaisetVierekkaisetReunat(laajennetut_reunat, laajennetut_reunat, reunat, tarkkailtavat_reunat);
				//poistetaan liian kauaksi jääneet reunat (löytyvät listan alusta päin), jos kynnys ylittyy
				if(poista_kaukaiset_reunat && reunat.Count>kynnys_lkm_hylkaystarkistukseen)
					while(reunat.Count>0)
						if(reunat[0].Etaisyys(this_position, ref mika)>reunojen_hylkaysetaisyys)
							reunat.Remove(reunat[0]);
				else
					break;
				return laajennetut_reunat;
			}

			//yhdistetään samansuuntaiset vierekkäiset reunat, jos tulivat riittävän lähelle toisiaan
			//tarkkailtavaa reunaa vahditaan ja jos se yhdistetään toiseen reunaan siten, että se sulautuu toiseen olioon, välitetään uusi reuna kutsujalle
			public void YhdistaSamansuuntaisetVierekkaisetReunat(List<Reuna> yhdistettava_joukko, List<Reuna> laajennetut_reunat, List<Reuna> kaiki_reunat, Reuna[] tarkkailtavat_reunat) {
				poistettavat.Clear();
				for(int i=0; i<(yhdistettava_joukko.Count-1); i++)
					if(!poistettavat.Contains(yhdistettava_joukko[i]))
						for(int j=i+1; j<yhdistettava_joukko.Count; j++)
						if(!poistettavat.Contains(yhdistettava_joukko[j])) {
							//Debug.Log("koitetaan yhdistaa reunat: " + yhdistettava_joukko[i].ToString() + " ja " + yhdistettava_joukko[j].ToString());
							if(yhdistettava_joukko[i].YhdistaReunatJosEhdotTayttyy(yhdistettava_joukko[j], aukon_minimipituus, reunan_maksimi_kulmaheitto)) {
								//Debug.Log("yhdistettiin reunaan: " + yhdistettava_joukko[i].ToString() + " ja " + yhdistettava_joukko[j].ToString());
								poistettavat.Add(yhdistettava_joukko[j]);
								for(int k=0; k<tarkkailtavat_reunat.Length; k++) {
									if(yhdistettava_joukko[j].Equals(tarkkailtavat_reunat[k]))
										tarkkailtavat_reunat[k]=yhdistettava_joukko[i]; //välitetään tarkkailtava reuna -> vaihdetaan se
								}
								if(laajennetut_reunat.Contains(yhdistettava_joukko[j])) {
									laajennetut_reunat.Remove(yhdistettava_joukko[j]); //poistettava pois
									if(!laajennetut_reunat.Contains(yhdistettava_joukko[i]))
										laajennetut_reunat.Add(yhdistettava_joukko[i]); //lisätään se, johon yhdistettiin, jos puuttui
								}
							}
						}
				//poistetaan poistetut (toiseen reunaan yhdistetyt) reunat
				poistettavat.ForEach(delegate(Reuna obj) {
					kaiki_reunat.Remove(obj);
					yhdistettava_joukko.Remove(obj);
				});
			}

			public class Havaitsijan_Viiksi {
				public Havaitsija havaitsija;
				public float viiksen_kulma;
				public Reuna havaittu_reuna=null;

				public Havaitsijan_Viiksi(Havaitsija p_havaitsija, float p_viiksen_kulma) {
					viiksen_kulma=p_viiksen_kulma;
					havaitsija=p_havaitsija;
				}
			}
		}

        //luokka esittää suoraa reunaa (pintaa)
        public class Reuna {
			public Vector2 alkupiste;
			public Vector2 loppupiste;
			public bool saa_osua;
			public string tag;
			
			public Reuna(Vector2 p_alkupiste, Vector2 p_loppupiste, bool p_saa_osua, string p_tag) {
				alkupiste=p_alkupiste;
				loppupiste=p_loppupiste;
				saa_osua=p_saa_osua;
				tag=p_tag;
			}
			
			public override string ToString ()
			{
				return alkupiste + "->" + loppupiste + " (" + tag + ")";
			}
			
			public Vector2 Direction() {
				return (loppupiste-alkupiste).normalized;
			}

			public float MinX() {
				if(alkupiste.x<loppupiste.x)
					return alkupiste.x;
				else
					return loppupiste.x;
			}
			public float MaxX() {
				if(alkupiste.x>loppupiste.x)
					return alkupiste.x;
				else
					return loppupiste.x;
			}
			public float MinY() {
				if(alkupiste.y<loppupiste.y)
					return alkupiste.y;
				else
					return loppupiste.y;
			}
			public float MaxY() {
				if(alkupiste.y>loppupiste.y)
					return alkupiste.y;
				else
					return loppupiste.y;
			}
			
			//palauttaa reunan takaisin nolla-pituiseksi
			public void PalautaNollaPituiseksi(Vector2 vaarin_laajennettu_pisteeseen) {
				Debug.DrawLine(alkupiste, loppupiste, Color.black, 10);
				if(Vector2.Distance(alkupiste, vaarin_laajennettu_pisteeseen)==0)
					alkupiste=loppupiste;
				else if(Vector2.Distance(loppupiste, vaarin_laajennettu_pisteeseen)==0)
					loppupiste=alkupiste;
			}
			
			//antaa kulman 0-180 astetta
			public static float Angle(Vector2 suunta1, Vector2 suunta2) {
				float angle;
				if(suunta1.magnitude==0 || suunta2.magnitude==0)
					angle=0;
				else
					angle=Vector2.Angle(suunta1, suunta2);
				return angle;
			}
			//antaa kulman 0-180 astetta
			public float Angle(Reuna toinen_reuna) {
				return Angle(toinen_reuna.Direction(), Direction());
			}
			//antaa kulman 0-180 astetta
			public float Angle(Vector2 suunta, bool annettu_piste=false) {
				if(annettu_piste) { //suunta onkin piste -> ratkaistaan suunta
					Vector2 lahempi_piste=alkupiste;
					if(Vector2.Distance(suunta,alkupiste)>Vector2.Distance(suunta,loppupiste)) lahempi_piste=loppupiste;
					return Angle((suunta-lahempi_piste).normalized, Direction());
				} else
					return Angle(Direction(), suunta);
			}
			//suunnattu kulma 0-360 astetta from- > to vastapäivään
			public static float DirectedAngle(Vector2 fromVector, Vector2 toVector) {
				float ang = Angle(fromVector, toVector);
				Vector3 cross = Vector3.Cross(fromVector, toVector);
				if (cross.z < 0)
					ang = 360 - ang;
				return ang;
			}
			//antaa (muuntaa) kulman 0-90 astetta
			public static float Angle_0_90(float angle) {
				if(angle>180)
					angle=360-angle;
				if(angle>90)
					angle=180-angle;
				return angle;
			}

			//lyhin etäisyys pisteestä suoraan
			public float LyhinEtaisyysPisteeseen(Vector2 piste)
			{
				return (ProjectPointLine(piste)).magnitude;
			}
			//pisteen kautta kulkeva suoran normaali
			public Vector2 ProjectPointLine(Vector2 piste)
			{
				Vector2 project=Ohjattava_2D.Convert_to_Vector2(Vector3.Project(Ohjattava_2D.Convert_to_Vector3(piste-alkupiste), Ohjattava_2D.Convert_to_Vector3(loppupiste-alkupiste)))+alkupiste;
				return project-piste;
			}
			//pisteen kautta kulkeva suunnatun viivan pituus reunaan asti
			public float SuunnattuEtaisyysPisteesta(Vector2 piste, Vector2 suunta) {
				Vector2 normaali=ProjectPointLine(piste);
				float pistetulo=Vector2.Dot(normaali.normalized,suunta);
				if(pistetulo>0) { //voi leikata reunaa
					float suunnatun_janan_pituus=LyhinEtaisyysPisteeseen(piste)/Mathf.Cos(Angle(normaali, suunta)*Mathf.Deg2Rad);
					Vector2 suunnattu_jana=suunta*suunnatun_janan_pituus;
					//selvitetään, leikkaako jana reunaa
					Vector2 leikkauspiste=piste+suunnattu_jana;
					float min_x=alkupiste.x;
					float max_x=loppupiste.x;
					if(min_x>max_x) {
						min_x=loppupiste.x;
						max_x=alkupiste.x;
					}
					float min_y=alkupiste.y;
					float max_y=loppupiste.y;
					if(min_y>max_y) {
						min_y=loppupiste.y;
						max_y=alkupiste.y;
					}
					if(min_x<=leikkauspiste.x && leikkauspiste.x<=max_x && min_y<=leikkauspiste.y && leikkauspiste.y<=max_y) //leikkaa
						return suunnatun_janan_pituus;
					else
						return Mathf.Infinity;
				} else return Mathf.Infinity; //ei leikkaa reunaa
			}
			
			//etäisyys toiseen reunaan ja kertoo mitkä pisteet ovat ne lähimmät
			public float Etaisyys(Reuna toinen_reuna, ref int mitka_pisteet) {
				mitka_pisteet=0;
				float etaisyys=Vector2.Distance(alkupiste, toinen_reuna.alkupiste);
				float etaisyys2=Vector2.Distance(alkupiste, toinen_reuna.loppupiste);
				if(etaisyys2<etaisyys) {
					etaisyys=etaisyys2;
					mitka_pisteet=1;
				}
				etaisyys2=Vector2.Distance(loppupiste, toinen_reuna.alkupiste);
				if(etaisyys2<etaisyys) {
					etaisyys=etaisyys2;
					mitka_pisteet=2;
				}
				etaisyys2=Vector2.Distance(loppupiste, toinen_reuna.loppupiste);
				if(etaisyys2<etaisyys) {
					etaisyys=etaisyys2;
					mitka_pisteet=3;
				}
				return etaisyys;
			}
			
			//kertoo myös, kumpaan pisteeseen on lyhyempi etaisyys
			public float Etaisyys(Vector2 piste, ref int kumpaan_pisteeseen) {
				kumpaan_pisteeseen=0;
				float etaisyys=Vector2.Distance(alkupiste, piste);
				float etaisyys2=Vector2.Distance(loppupiste, piste);
				if(etaisyys2<etaisyys) {
					etaisyys=etaisyys2;
					kumpaan_pisteeseen=1;
				}
				return etaisyys;
			}
			
			//laskee etäisyyden pisteestä reunan pisteeseen, joka on lähempänä annettua suuntaa
			public float Etaisyys(Vector2 piste, Vector2 suuntaan) {
				int mika=-1;
				float pistetulo=Vector2.Dot(loppupiste-alkupiste, suuntaan);
				if(pistetulo>0)
					return Vector2.Distance(loppupiste, piste);
				else if(pistetulo<0)
					return Vector2.Distance(alkupiste, piste);
				else //kohtisuorassa -> lasketaan lähempään reunaan
					return Etaisyys(piste, ref mika);
			}
			
			//kertoo onko annettu piste reunan sisällä
			public bool OnkoPisteReunanSisalla(Vector2 piste, uint reunan_maksimi_kulmaheitto) {
				float pituus=Vector2.Distance(alkupiste, loppupiste);
				if(Angle_0_90(Angle(piste, true))<=reunan_maksimi_kulmaheitto)
					if(Vector2.Distance(piste, alkupiste)<pituus && Vector2.Distance(piste, loppupiste)<pituus)
						return true;
				return false;
			}
			
			//kertoo onko toinen reuna reunan sisällä ja kertoo mitkä pisteet ovat
			public bool OnkoToinenReunaReunanSisalla(Reuna toinen_reuna, ref int mitka_pisteet, uint reunan_maksimi_kulmaheitto, bool ei_jatketa_rekursiivisesti=false) {
				mitka_pisteet=-1;
				if(OnkoPisteReunanSisalla(toinen_reuna.alkupiste, reunan_maksimi_kulmaheitto))
					mitka_pisteet=0;
				if(OnkoPisteReunanSisalla(toinen_reuna.loppupiste, reunan_maksimi_kulmaheitto)) {
					if(mitka_pisteet==0)
						mitka_pisteet=2; //molemmat pisteet
					else
						mitka_pisteet=1;
				}
				if(mitka_pisteet>(-1))
					return true;
				else if(!ei_jatketa_rekursiivisesti) {
					if(toinen_reuna.OnkoToinenReunaReunanSisalla(this, ref mitka_pisteet, reunan_maksimi_kulmaheitto, true)) { //onko tämä reuna kokonaan sen toisen reunan sisällä (ainut mahdollisuus)
						mitka_pisteet=3; //tämä reuna on kokonaan toisen reunan sisällä
						return true;
					} else 
						mitka_pisteet=-1;
				}
				return false;
			}
			
			
			//laajentaa reunan lähemmän reunapisteen uuteen pisteeseen, mikäli reunan suuntakulma ei muutu liikaa
			//lajennuksen on myös kasvatettava pituutta (ei ole reunapisteiden välissä) ja saa_osua-ehdon on oltava sama
			//palauttaa true, mikäli laajennetaan tai piste kuuluu jo reunaan
			public bool LaajennaReunaJosEhdotTayttyy(Vector2 uusi_piste, bool p_saa_osua, uint reunan_maksimi_kulmaheitto, float aukon_minimipituus) {
				bool paluu=false;
				Color vari;
				if(saa_osua)
					vari=Color.yellow;
				else
					vari=Color.green;
				//Debug.Log("LaajennaReunaJosEhdotTayttyy: " + ToString() + ", uusi_piste: " + uusi_piste + ", Angle_0_90: " + Angle_0_90(Angle(uusi_piste, true)) + ", saa osua ehto: " + (saa_osua==p_saa_osua).ToString());
				//Debug.Log("kulmaehto: " + (Angle_0_90(Angle(uusi_piste, true))<reunan_maksimi_kulmaheitto).ToString() + ", pituus_ehto: " + (Vector2.Distance(alkupiste, loppupiste)==0).ToString());
				if(Angle_0_90(Angle(uusi_piste, true))<reunan_maksimi_kulmaheitto || Vector2.Distance(alkupiste, loppupiste)==0) //jos suuntaa ei ole vielä näkyvissä -> kelpaa kaikki suunnat laajennukselle
					if(saa_osua==p_saa_osua) {
						paluu=true;
						if(Vector2.Distance(uusi_piste,alkupiste)<Vector2.Distance(uusi_piste,loppupiste)) { //alkupiste lähempänä
							if(Vector2.Distance(uusi_piste, loppupiste)>Vector2.Distance(alkupiste, loppupiste)) { //jos uusi piste todella laajentaa
								Debug.DrawLine(alkupiste, uusi_piste, vari, 10);
								alkupiste=uusi_piste;
							}
						} else if(Vector2.Distance(uusi_piste, alkupiste)>Vector2.Distance(alkupiste, loppupiste)) { //jos uusi piste todella laajentaa
							Debug.DrawLine(loppupiste, uusi_piste, vari, 10);
							loppupiste=uusi_piste;
						}
					}
				//Debug.Log("paluu: " + paluu.ToString());
				return paluu;
			}
			
			//yhdistää kaksi tarpeeksi lähellä toisiaan olevaa reunaa, mikäli ovat riittävän samansuuntaiset
			public bool YhdistaReunatJosEhdotTayttyy(Reuna toinen_reuna, float aukon_minimipituus, uint reunan_maksimi_kulmaheitto) {
				int mitka_pisteet_lahimpina=0; //mitkä ovat ne lähimmät pisteet
				int mitka_pisteet_sisalla=-1; //mitkä pisteet ovat reunan sisällä
				Color vari;
				if(saa_osua)
					vari=Color.yellow;
				else
					vari=Color.green;
				//Debug.Log("Yhdistettavien kulma_ero: " + Angle_0_90(Angle(toinen_reuna)) + ", sallittu: " + reunan_maksimi_kulmaheitto);
				if(Angle_0_90(Angle(toinen_reuna))<=reunan_maksimi_kulmaheitto) //kulmaero täsmää
					//testataan, onko toinen reuna osittain tai jompikumpi kokonaan toisen reunan sisällä
				if(OnkoToinenReunaReunanSisalla(toinen_reuna, ref mitka_pisteet_sisalla, reunan_maksimi_kulmaheitto)) {
					//Debug.Log("yhdistetaan sisakkaiset reunat: " + ToString() + "_ja_" + toinen_reuna.ToString());
					if(mitka_pisteet_sisalla==0) { //alkupiste
						Etaisyys(toinen_reuna.loppupiste, ref mitka_pisteet_lahimpina);
						if(mitka_pisteet_lahimpina==0) {
							Debug.DrawLine(alkupiste, toinen_reuna.loppupiste, vari, 10);
							alkupiste=toinen_reuna.loppupiste;
						} else if(mitka_pisteet_lahimpina==1) {
							Debug.DrawLine(loppupiste, toinen_reuna.loppupiste, vari, 10);
							loppupiste=toinen_reuna.loppupiste;
						}
					} else if(mitka_pisteet_sisalla==1) { //loppupiste
						Etaisyys(toinen_reuna.alkupiste, ref mitka_pisteet_lahimpina);
						if(mitka_pisteet_lahimpina==0) {
							Debug.DrawLine(alkupiste, toinen_reuna.alkupiste, vari, 10);
							alkupiste=toinen_reuna.alkupiste;
						} else if(mitka_pisteet_lahimpina==1) {	
							Debug.DrawLine(loppupiste, toinen_reuna.alkupiste, vari, 10);
							loppupiste=toinen_reuna.alkupiste;
						}
					} else if(mitka_pisteet_sisalla==2) { //molemmat -> ei muutoksia tähän reunaan
						Debug.DrawLine(alkupiste, loppupiste, vari, 10);
					} else if(mitka_pisteet_sisalla==3) { //tämä reuna on kokonaan toisen sisällä -> vaihdetaan
						Debug.DrawLine(toinen_reuna.alkupiste, toinen_reuna.loppupiste, vari, 10);
						alkupiste=toinen_reuna.alkupiste;
						loppupiste=toinen_reuna.loppupiste;
					}
					return true;
					//tutkitaan vain reunojen välinen etäisyys
				} else if(Etaisyys(toinen_reuna, ref mitka_pisteet_lahimpina)<=aukon_minimipituus) { //yhdistetään
					//Debug.Log("yhdistetaan vierekkaiset reunat: " + ToString() + "_ja_" + toinen_reuna.ToString());
					if(mitka_pisteet_lahimpina==0) {
						Debug.DrawLine(alkupiste, toinen_reuna.loppupiste, vari, 10);
						alkupiste=toinen_reuna.loppupiste;
					}
					else if(mitka_pisteet_lahimpina==1) {
						Debug.DrawLine(alkupiste, toinen_reuna.alkupiste, vari, 10);
						alkupiste=toinen_reuna.alkupiste;
					}
					else if(mitka_pisteet_lahimpina==2) {
						Debug.DrawLine(loppupiste, toinen_reuna.loppupiste, vari, 10);
						loppupiste=toinen_reuna.loppupiste;
					}
					else if(mitka_pisteet_lahimpina==3) {
						Debug.DrawLine(loppupiste, toinen_reuna.alkupiste, vari, 10);
						loppupiste=toinen_reuna.alkupiste;
					}
					return true;
				}
				return false;
			}
			
			public bool Exist(Vector2 piste) {
				return (piste.Equals(alkupiste) || piste.Equals(loppupiste));
			}
        }
    }
	
    //määrittää ukkelin käyttäytymistä (liikettä ja ampumista). voidaan määrittää toiminta staattisesti tai suhteessa targettiin)
    public class ToimintaMoodi {
		public LiikeKohteidenMaarittaja liikekohteiden_maarittaja;
		public Liikuttaja liikuttaja;
		public AseenKayttaja aseen_kayttaja;
		
		public ToimintaMoodi(LiikeKohteidenMaarittaja p_liikekohteiden_maarittaja, Liikuttaja p_liikuttaja, AseenKayttaja p_aseen_kayttaja) {
			liikekohteiden_maarittaja=p_liikekohteiden_maarittaja;
			liikuttaja=p_liikuttaja;
			aseen_kayttaja=p_aseen_kayttaja;
		}
		
		//voidaan antaa myös vaihtoehtoinen kohde, joka ohittaa toimintamoodin oman kohteen
		//voidaan sallia myös hyppy, vaikka liikuttelija ei muuten saisi liikkua kuin x-tasossa
		public void SuoritaToiminnot(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde, bool saa_hypata) {
			if(aseen_kayttaja!=null) aseen_kayttaja.PaivitaTila(); //ammutaan ensin, koska ampuminen mahdollisesti käskee ukkelin olla paikallaan
			liikuttaja.PaivitaOhjauksienTilat(liikekohteiden_maarittaja.TarkistaSijainti(liikuttaja, annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde), annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, saa_hypata);
		}
    }
	
    //asettaa ja vaihtaa kohdeposition tilanteen mukaan
    public abstract class LiikeKohteidenMaarittaja {
		public Prefab_and_instances.Modified_GameObject modified_gameobject;
		public float liipaisuetaisyys_kohdepositiossa; //kuinka pitkän matkan päästä tunnistetaan olevn kohteessa
		public float jumiutuneeksi_rajatun_alueen_sade; //jos ukkeli jää pyörimään alueen sisälle, todetaan se jumiutuneeksi ja liike pysäytetään
		//edel framen arvot
		private Vector2 edel_kohde=Vector2.zero;
		private Vector2 edel_jumiutuneeksi_testattava_alue=Vector2.zero; //testataan tässä alueella, jääkö ukkeli pyörimään
		private Vector2 position;
		private Vector2 edel_position;
		//paikalliselle alueelle (tai uudelle kohteelle) sallitaan yksi raju käännös. nollataan kun edetään riittävästi tai kohde vaihtuu. tällä ehkäistään, ettei ukkeli jää pyörimään paikalleen -> liike pysäytetään
		private bool raju_kaannos_kaytetty=false;
		private bool kohteen_vaihdoksen_kaantyminen_kayttamatta=false; //kun kohde vaihdetaan, sallitaan rajua käännöstä siihen asti, kunnes kurssi on saatu muutettua riittävästi uuteen kohteeseen päin
		private float kohteen_vaihtumisaika=0; //ukkeli voi jumiutua (teoriassa) suoraan paikalleen, kun kohde vaihtuu, ilman, että rajua käännöstä on suoritettu -> todetaan jumiutuneeksi, kun on ollut riittävän aikaa jumissa
		private bool todettu_jumiutuneeksi=false; //kun on todettu jumiutuneeksi, ei enää testata -> ukkeli on jumissa
		//seuraavan kohteen liipaisutiedot
		private bool on_saapunut_seuraava_kohteen_alueelle=false;
		private float edel_etaisyys_seuraavan_kohteen_keskustaan;
		
		public LiikeKohteidenMaarittaja(Prefab_and_instances.Modified_GameObject p_modified_gameobject, float p_liipaisuetaisyys_kohdepositiossa, float p_jumiutuneeksi_rajatun_alueen_sade, Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet seuraava_kohde, bool kohde_annettu_suhteelisena_alkupositioon) {
			modified_gameobject=p_modified_gameobject;
			liipaisuetaisyys_kohdepositiossa=p_liipaisuetaisyys_kohdepositiossa;
			jumiutuneeksi_rajatun_alueen_sade=p_jumiutuneeksi_rajatun_alueen_sade;
			if(kohde_annettu_suhteelisena_alkupositioon)
				modified_gameobject.target_position=MuunnaSuhteellinenKoordinaattiAbsoluuttiseksi(seuraava_kohde.koordinaatti); //voidaan antaa vain koordinaattina
			else if(seuraava_kohde.sijainti!=null)
				modified_gameobject.target_position=Ohjattava_2D.Convert_to_Vector2(seuraava_kohde.sijainti.transform.position);
			else
				modified_gameobject.target_position=seuraava_kohde.koordinaatti;
			edel_position=Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position);
		}
		
		public abstract bool TarkistaSijainti(Liikuttaja liikuttaja, bool annetaan_vaihtoehtoinen_kohde=false, Vector2 vaihtoehtoinen_kohde=new Vector2()); //palauttaa true, mikäli ukkelia pitää liikuttaa (ei ole odottamassa)
		public abstract Vector2 AnnaSeuraavaKohde();
		
		public bool OnkoTodettuJumiutuneeksi() {
			if(todettu_jumiutuneeksi)
				return true;
			else
				return false;
		}
		
		//varmistaa, että seuraava kohde on asetettu oikein, eikä esim. vaihtoehtoisena kohteena
		public void VarmistaSeuraavaKohdeOikeaksi(bool annetaan_vaihtoehtoinen_kohde) {
			if(annetaan_vaihtoehtoinen_kohde) //varmistetaan, että seuraavakohde ei ole vaihtoehtoinen kohde
				modified_gameobject.target_position=AnnaSeuraavaKohde();
		}
		
		//palauttaa seuraavan kohteen
		public Vector2 AnnaKohde(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde) {
			if(annetaan_vaihtoehtoinen_kohde)
				return vaihtoehtoinen_kohde;
			else
				return modified_gameobject.target_position;
		}
		
		//selvittää, ollaanko saavuttu seuraavaan kohteeseen
		//ollaan kohteessa, kun ollaan mahdollisimman lähellä kohteen keskustaa
		public bool OnkoKohteessa(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde) {
			Vector2 kohde=Vector2.zero;
			float etaisyys=0;
			position=Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position);
			Ympyran_ja_Suoran_Leikkauspisteet kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija=new Ympyran_ja_Suoran_Leikkauspisteet();
			if(OnkoAlueella(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, ref kohde, ref etaisyys)) { //jos ollaan alueella
				if(on_saapunut_seuraava_kohteen_alueelle==false) { //jos saavuttiin juuri
					//testataan, mentiinkö saman tien keskustasta yli (eli ollaan menossa kaueuemmaksi keskustasta)
					kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.Ratkaise(edel_position, position, kohde, Vector2.Distance(position, kohde), true);
					if(kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu1_loytyi & kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu2_loytyi) //ollaan menty jo tarpeeksi pitkälle -> ollaan kohteessa
						return true;
					else {
						edel_etaisyys_seuraavan_kohteen_keskustaan=etaisyys;
						on_saapunut_seuraava_kohteen_alueelle=true;
						return false;
					}
				} else { //myöhemmin testataan, alkaako etäisyys keskustaan kasvamaan
					if(etaisyys>edel_etaisyys_seuraavan_kohteen_keskustaan) { //ollaan käyty alueella ja etäisyys alkaa kasvaa
						on_saapunut_seuraava_kohteen_alueelle=false;
						return true;
					} else {
						edel_etaisyys_seuraavan_kohteen_keskustaan=etaisyys;
						return false;
					}
				}
			} else { //testataan, mentiinkö alueen läpi ja yli -> ollaan kohteessa
				kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.Ratkaise(edel_position, position, kohde, liipaisuetaisyys_kohdepositiossa, true);
				if(kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu1_loytyi & kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu2_loytyi) //mentiin läpi
					return true;
				else
					return false;
			}
		}
		
		//selvittää, ollaanko seuraavan kohteen alueella
		//vaikka on alueella, ei ole kohteessa vasta kuin ollaan mahdollisimman lähellä kohteen keskustaa
		public bool OnkoAlueella(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde, ref Vector2 kohde, ref float etaisyys) {
			kohde=AnnaKohde(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde);
			etaisyys=Vector2.Distance(kohde, Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position));
			if(etaisyys<=liipaisuetaisyys_kohdepositiossa)//jos ollaan alueella
				return true;
			else
				return false;
		}
		
		//testaa, onko ukkeli jäänyt johonkin jumiin
		//eli ei pääse eteenpäin tai koordinaatti on tavoittamattomissa
		public bool JaikoPyorimaanEesTaas(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde) {
			if(todettu_jumiutuneeksi)
				return true;
			else {
				bool paluu=false;
				Vector2 kohde=Vector2.zero;
				float etaisyys=0;
				Vector2 position=Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position);
				float pistetulo=0;
				Vector2 tavoite_suunta=Vector2.zero;
				//testataan, jos ei olla seuraavan kohteen alueella
				if(!OnkoAlueella(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, ref kohde, ref etaisyys)) { //jos ei olla seuraavan kohteen alueella, eikä edellisen
					float etaisyys_jumiutumis_testaus_alueelle=Vector2.Distance(position, edel_jumiutuneeksi_testattava_alue);
					if((kohde.x==edel_kohde.x) & (kohde.y==edel_kohde.y) & (etaisyys_jumiutumis_testaus_alueelle<jumiutuneeksi_rajatun_alueen_sade)) { //kohde on sama
						tavoite_suunta=(kohde-Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position)).normalized;
						pistetulo=Vector2.Dot(tavoite_suunta, (position-edel_position).normalized);
						if((pistetulo<0) | (raju_kaannos_kaytetty & pistetulo==0)) { //raju käännös
							if(kohteen_vaihdoksen_kaantyminen_kayttamatta==false) {
								if(raju_kaannos_kaytetty==false)
									raju_kaannos_kaytetty=true;
								else
									paluu=true;
							}
						} else if(kohteen_vaihdoksen_kaantyminen_kayttamatta) {
							if(pistetulo>0) kohteen_vaihdoksen_kaantyminen_kayttamatta=false; //suunta on saatu muutettua
						} else if(pistetulo==0) { //jumiutunut ilman rajua käännöstä
							if(Time.time>(kohteen_vaihtumisaika+0.5)) { //on niin jumissa, että saadaan todeta jumiutuneeksi
								raju_kaannos_kaytetty=true;
								kohteen_vaihdoksen_kaantyminen_kayttamatta=false;
							}
						}
					} else {
						raju_kaannos_kaytetty=false;
						kohteen_vaihtumisaika=Time.time;
						if((kohde.x!=edel_kohde.x) | (kohde.y!=edel_kohde.y))
							kohteen_vaihdoksen_kaantyminen_kayttamatta=true;
						if(etaisyys_jumiutumis_testaus_alueelle>=jumiutuneeksi_rajatun_alueen_sade)
							edel_jumiutuneeksi_testattava_alue=position;
					}
				}
				if(paluu) {
					Debug.LogWarning("jumittaa: " + modified_gameobject.gameobject.name + ", position: " + Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position) + ", edel_position: " + edel_position + ", kohde: " + modified_gameobject.target_position + ", tavoite_suunta: " + tavoite_suunta + ", pistetulo: " + pistetulo);
					todettu_jumiutuneeksi=true;
				}
				edel_kohde=kohde;
				edel_position=position;
				return paluu;
			}
		}
		
		public void AsetaSeuraavaKohde(Vector2 p_seuraava_kohde) {
			modified_gameobject.target_position=p_seuraava_kohde;
			//Debug.Log("seuraava_kohde: " + p_seuraava_kohde + ", position: " + Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position));
		}
		
		public Vector2 MuunnaSuhteellinenKoordinaattiAbsoluuttiseksi(Vector2 suhteellinenKoord) {
			return suhteellinenKoord+Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position);
		}
		
		//ratkaisee ympyrän ja suoran (tai janan) leikkauspisteet
		public class Ympyran_ja_Suoran_Leikkauspisteet {
			public Vector2 leikkauspiste1;
			public Vector2 leikkauspiste2;
			public bool ratkaisu1_loytyi=false;
			public bool ratkaisu2_loytyi=false;
			
			public void Ratkaise(Vector2 A, Vector2 B, Vector2 C, float R, bool suora_on_jana=false) {
				// Let say we have the points A, B, C. Ax and Ay are the x and y components of the A points. Same for B and C. The scalar R is the circle radius.
				
				// compute the euclidean distance between A and B
				float LAB=Vector2.Distance(A, B);
				// compute the direction vector D from A to B
				Vector2 D=(B-A).normalized;
				
				// Now the line equation is x = Dx*t + Ax, y = Dy*t + Ay with 0 <= t <= 1.
				
				// compute the value t of the closest point to the circle center (Cx, Cy)
				float t=D.x*(C.x-A.x)+D.y*(C.y-A.y);
				
				// This is the projection of C on the line from A to B.
				
				// compute the coordinates of the point E on line and closest to C
				Vector2 E=t*D+A;
				// compute the euclidean distance from E to C
				float LEC=Vector2.Distance(E, C);
				
				// test if the line intersects the circle
				if( LEC < R )
				{
					// compute distance from t to circle intersection point
					float dt=Mathf.Sqrt(Mathf.Pow(R,2) - Mathf.Pow(LEC,2));
					// compute first intersection point
					leikkauspiste1=(t-dt)*D+A;
					if(suora_on_jana) { //testataan että leikkauspiste on janan päätepisteiden välissä
						if((D.x*(B.x-leikkauspiste1.x)+D.y*(B.y-leikkauspiste1.y)>0) & (D.x*(A.x-leikkauspiste1.x)+D.y*(A.y-leikkauspiste1.y)<0))
							ratkaisu1_loytyi=true;
					} else ratkaisu1_loytyi=true;
					// compute second intersection point
					leikkauspiste2=(t+dt)*D+A;
					if(suora_on_jana) { //testataan että leikkauspiste on janan päätepisteiden välissä
						if((D.x*(B.x-leikkauspiste2.x)+D.y*(B.y-leikkauspiste2.y)>0) & (D.x*(A.x-leikkauspiste2.x)+D.y*(A.y-leikkauspiste2.y)<0))
							ratkaisu2_loytyi=true;
					} else ratkaisu2_loytyi=true;
				}
				// else test if the line is tangent to circle
				else if( LEC == R ) {
					// tangent point to circle is E
					leikkauspiste1=E;
					ratkaisu1_loytyi=true;
				} // else line doesn't touch circle
			}
		}
    }
    //määrittää ukkelille staattisen liikeradan
    public class Staattinen_LiikeKohteidenMaarittaja : LiikeKohteidenMaarittaja {
		public LinkedList<Vector2> kohteet;
		public float odotusviive_kaannyttaessa; //odotusviive kun käädytään ja palataan
		private LinkedListNode<Vector2> seuraava_kohde_Node;
		private bool ajastettu_kaantyminen_asetettu=false;
		private float seuraavan_kohteen_asettamisaika;
		private bool palataan=false;
		
		public Staattinen_LiikeKohteidenMaarittaja(List<Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet> p_kohteet, bool kohteet_annettu_suhteelisena_alkupositioon, float p_odotusviive_kaannyttaessa, Prefab_and_instances.Modified_GameObject p_modified_gameobject, float p_liipaisuetaisyys_kohdepositiossa, float p_jumiutuneeksi_rajatun_alueen_sade):base(p_modified_gameobject, p_liipaisuetaisyys_kohdepositiossa, p_jumiutuneeksi_rajatun_alueen_sade, p_kohteet[0], kohteet_annettu_suhteelisena_alkupositioon) {
			kohteet=new LinkedList<Vector2>();
			if(kohteet_annettu_suhteelisena_alkupositioon) {
				p_kohteet.ForEach(delegate(Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet obj) {
					kohteet.AddLast(MuunnaSuhteellinenKoordinaattiAbsoluuttiseksi(obj.koordinaatti)); //voidaan määrittää vain koordinaattina
				});
			} else p_kohteet.ForEach(delegate(Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet obj) {
				if(obj.sijainti!=null) //voidaan määrittää koordinaattina tai gameobjectin sijaintina
					kohteet.AddLast(Ohjattava_2D.Convert_to_Vector2(obj.sijainti.transform.position));
				else
					kohteet.AddLast(obj.koordinaatti);
			});
			odotusviive_kaannyttaessa=p_odotusviive_kaannyttaessa;
			seuraava_kohde_Node=kohteet.First;
		}
		
		//tarkistaa sijainnin ja asettaa seuraavan kohteen, mikäli ollaan saavuttu sitä edelliseen
		//mikäli saavutaan päätepisteeseen, käännytään takaisinpäin viiveellä
		//mikäli annetaan vaihtoehtoinen kohde, ei seuraavaa kohdetta aseteta
		//palauttaa true, mikäli ukkelia pitää liikuttaa (ei ole odottamassa)
		public override bool TarkistaSijainti(Liikuttaja liikuttaja, bool annetaan_vaihtoehtoinen_kohde=false, Vector2 vaihtoehtoinen_kohde=new Vector2()) {
			VarmistaSeuraavaKohdeOikeaksi(annetaan_vaihtoehtoinen_kohde);
			if(ajastettu_kaantyminen_asetettu==false) {
				if(OnkoKohteessa(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde)) {
					if(!annetaan_vaihtoehtoinen_kohde) {
						//asetetaan seuraava kohde
						if(ajastettu_kaantyminen_asetettu==false) {
							bool kaannytaan=false;
							if(palataan) {
								if(seuraava_kohde_Node.Equals(kohteet.First)) {
									kaannytaan=true;
									palataan=false;
								}
							} else if(seuraava_kohde_Node.Equals(kohteet.Last)) {
								kaannytaan=true;
								palataan=true;
							}
							if(kaannytaan) { //asetetaan seuraava kohde viiveellä
								ajastettu_kaantyminen_asetettu=true;
								seuraavan_kohteen_asettamisaika=Time.time+odotusviive_kaannyttaessa;
							} else //välittömästi
								AsetaSeuraavaKohde();
						}
					}
					return false;
				}
				//if(JaikoPyorimaanEesTaas(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde)) //testataan, ollaanko jumissa
				//	return false;
				return true;
			} else if(Time.time>seuraavan_kohteen_asettamisaika) //ajastettu kääntyminen
				AsetaSeuraavaKohde();
			return false;
		}
		
		public void AsetaSeuraavaKohde() {
			if(kohteet.Count>1) {
				if(palataan)
					seuraava_kohde_Node=seuraava_kohde_Node.Previous;
				else
					seuraava_kohde_Node=seuraava_kohde_Node.Next;
				AsetaSeuraavaKohde(AnnaSeuraavaKohde());
			}
			ajastettu_kaantyminen_asetettu=false;
		}
		
		public override Vector2 AnnaSeuraavaKohde() {
			return seuraava_kohde_Node.Value;
		}
		
		public void SiirraSeuraavaKohde(Vector2 koordinaatit) {
			seuraava_kohde_Node.Value.Set(koordinaatit.x, koordinaatit.y);
		}
    }
	//määrittää ukkelille seuraavan kohteen suhteessa targettiin
	public class Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja : LiikeKohteidenMaarittaja {
		public Target_ohjaus.Koordinaattiasetukset_Hallintapaneeliin target_position_parameters;
		public Koordinaattiasetukset_Hallintapaneelista_Rakentaja koordinaatti_rakentaja;

		public Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja(Target_ohjaus.Koordinaattiasetukset_Hallintapaneeliin p_target_position_parameters, Prefab_and_instances.Modified_GameObject p_modified_gameobject, float p_liipaisuetaisyys_kohdepositiossa, float p_jumiutuneeksi_rajatun_alueen_sade):base(p_modified_gameobject, p_liipaisuetaisyys_kohdepositiossa, p_jumiutuneeksi_rajatun_alueen_sade, new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(Vector2.zero, null), true) {
			target_position_parameters=p_target_position_parameters;
			koordinaatti_rakentaja=new Koordinaattiasetukset_Hallintapaneelista_Rakentaja();
		}

		//asettaa seuraavan kohteen suhteessa targettiin
		//mikäli annetaan vaihtoehtoinen kohde, asetetaan se seuraavaksi kohteeksi
		//palauttaa true, mikäli ukkelia pitää liikuttaa (ei ole odottamassa)
		public override bool TarkistaSijainti(Liikuttaja liikuttaja, bool annetaan_vaihtoehtoinen_kohde=false, Vector2 vaihtoehtoinen_kohde=new Vector2()) {
			VarmistaSeuraavaKohdeOikeaksi(annetaan_vaihtoehtoinen_kohde);
			if(annetaan_vaihtoehtoinen_kohde)
				AsetaSeuraavaKohde(vaihtoehtoinen_kohde);
			else
				AsetaSeuraavaKohde();
			if(OnkoKohteessa(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde)) {
				//varmistetaan, että ukko on kohti targettia
				liikuttaja.KaannaOhjattavaKohtiPositiota(Ohjattava_2D.Convert_to_Vector2(modified_gameobject.target_gameobject.transform.position));
				return false;
			}
			//if(JaikoPyorimaanEesTaas(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde)) //ollaanko jumissa
			//	return false;
			return true;
		}

		public void AsetaSeuraavaKohde() {
			AsetaSeuraavaKohde(AnnaSeuraavaKohde());
		}

		public override Vector2 AnnaSeuraavaKohde() {
			return koordinaatti_rakentaja.Rakenna(target_position_parameters, modified_gameobject.target_gameobject.transform);
		}
	}
	
    //liikuttaa ukkelia (hallitsee peliukkelinohjauksen ohjauskomponentteja (virtuaalinappuloita))
    public class Liikuttaja {
		public Prefab_and_instances.Modified_GameObject modified_gameobject;
		public Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija;
		public List<OhjauskomponenttiRajapinta> ohjauskomponenttirajapinnat;
		public OhjauskomponenttiRajapinta hyppykomponenttirajapinta=null;
		public Vector2 hallittavat_suunnat; //(1,0): pystyy liikkumaan x-suunnassa, (1,1): ei rajoituksia
		public bool liikkeen_keskeytys=false; //tästä voidaan asettaa liikkuminen hetkeksi pois
		
		public Liikuttaja(Prefab_and_instances.Modified_GameObject p_modified_gameobject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija p_peliukkelin_liikkeen_hallitsija, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> p_hallittavat_ohjauskomponentit, Vector2 p_hallittavat_suunnat, bool lisaa_hyppy) {
			modified_gameobject=p_modified_gameobject;
			peliukkelin_liikkeen_hallitsija=p_peliukkelin_liikkeen_hallitsija;
			ohjauskomponenttirajapinnat=new List<OhjauskomponenttiRajapinta>();
			hallittavat_suunnat=p_hallittavat_suunnat;
			//lisätään vain rajatuissa suunnissa liikuttavat ohjauskomponentit
			p_hallittavat_ohjauskomponentit.ForEach(delegate(Hallittava_OhjausKomponentti obj) {
				LisaaHallittava_OhjausKomponentti(obj, lisaa_hyppy);
			});
		}
		
		//selvittää, onko ohjaussuunta rajattu pois
		public bool OnkoSopivaOhjauskomponetti(Peliukkeli_ohjaus.Hallittava_OhjausKomponentti komponentti) {
			bool sopiva=true;
			if(hallittavat_suunnat.x==0 && komponentti.suunta_parametrit.suunta_komponentti==Peliukkeli_ohjaus.VoimaVektori.SuuntaParametrit.suunta_x)
				sopiva=false;
			if(hallittavat_suunnat.y==0 && komponentti.suunta_parametrit.suunta_komponentti==Peliukkeli_ohjaus.VoimaVektori.SuuntaParametrit.suunta_y)
				sopiva=false;
			return sopiva;
		}
		
		//lisää ohjauskomponentin, mikäli ohjaussuuntaa ei ole rajattu pois
		public void LisaaHallittava_OhjausKomponentti(Peliukkeli_ohjaus.Hallittava_OhjausKomponentti komponentti, bool lisaa_hyppy) {
			if(OnkoSopivaOhjauskomponetti(komponentti))
				ohjauskomponenttirajapinnat.Add(new OhjauskomponenttiRajapinta(komponentti));
			else if(lisaa_hyppy && komponentti.suunta_parametrit.suunta_komponentti==Peliukkeli_ohjaus.VoimaVektori.SuuntaParametrit.suunta_y)
				hyppykomponenttirajapinta=new OhjauskomponenttiRajapinta(komponentti);
		}
		
		//painaa ohjauksien nappeja siten, että peliukkeliohjaus ohjaa ukkelia kohti seuraavaa positiota
		//pyrkii kohteeseen suoraa väylää riippuen kuitenkin käytössä olevista ohjauskomponenteista
		//mikäli ei liikuteta, kytketään kaikki ohjaukset pois
		//voidaan antaa myös vaihtoehtoinen kohde, joka riippuu muista tekijöistä, kuten edessä olevasta esteestä. tällöin valitaan kohteeksi lähempänä oleva
		public void PaivitaOhjauksienTilat(bool liikutetaan, bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde, bool saa_hypata) {
			Vector2 ohjailusuunta;
			if(liikutetaan && !liikkeen_keskeytys) {
				Vector2 this_position=Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position);
				//Debug.Log("this_position: " + this_position);
				Vector2 kohde=modified_gameobject.target_position;
				if(annetaan_vaihtoehtoinen_kohde && (Vector2.Distance(this_position, kohde)>Vector2.Distance(this_position, vaihtoehtoinen_kohde)))
					kohde=vaihtoehtoinen_kohde; //vaihtoehtoinen kohde on lähempänä
				//Debug.Log("kohde: " + kohde);
				Vector2 tavoite_suunta=(kohde-this_position).normalized;
				Vector2 suunnan_muutos_tarve=tavoite_suunta-peliukkelin_liikkeen_hallitsija.GetVelocity().normalized;
				ohjailusuunta=tavoite_suunta+suunnan_muutos_tarve;
			} else ohjailusuunta=Vector2.zero;
			//asetetaan suunnanmuutostarpeen mukaan ohjauksien tilat
			ohjauskomponenttirajapinnat.ForEach(delegate(OhjauskomponenttiRajapinta obj) {
				obj.PaivitaTila(ohjailusuunta);
			});
			if(saa_hypata & hyppykomponenttirajapinta!=null) hyppykomponenttirajapinta.PaivitaTila(ohjailusuunta); //hypätään tarvittaessa
		}
		
		public void KaannaOhjattavaKohtiPositiota(Vector2 position) {
			float pistetulo=Vector2.Dot(Vector2.right, position-Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position));
			int suuntaparametri;
			if(pistetulo>0)
				suuntaparametri=1;
			else if(pistetulo<0)
				suuntaparametri=-1;
			else
				return; //ei käännetä
			if(suuntaparametri!=peliukkelin_liikkeen_hallitsija.ukkelin_suunta)
				peliukkelin_liikkeen_hallitsija.KaannaUkkeli(suuntaparametri*Vector2.right);
		}
		
		public class OhjauskomponenttiRajapinta {
			public Peliukkeli_ohjaus.Hallittava_OhjausKomponentti hallittava_ohjauskomponentti;
			public Vector2 suuntavektori;
			public Ohjaus_laite.VirtuaalisenNappulan_PainajaApuri positiivinen_ohjausrajapinta;
			public Ohjaus_laite.VirtuaalisenNappulan_PainajaApuri negatiivinen_ohjausrajapinta;
			
			public OhjauskomponenttiRajapinta(Peliukkeli_ohjaus.Hallittava_OhjausKomponentti p_hallittava_ohjauskomponentti) {
				hallittava_ohjauskomponentti=p_hallittava_ohjauskomponentti;
				if(hallittava_ohjauskomponentti.suunta_parametrit.suunta_komponentti==Peliukkeli_ohjaus.VoimaVektori.SuuntaParametrit.suunta_x)
					suuntavektori=new Vector2(1,0);
				else if(hallittava_ohjauskomponentti.suunta_parametrit.suunta_komponentti==Peliukkeli_ohjaus.VoimaVektori.SuuntaParametrit.suunta_y)
					suuntavektori=new Vector2(0,1);
				positiivinen_ohjausrajapinta=new Ohjaus_laite.VirtuaalisenNappulan_PainajaApuri(hallittava_ohjauskomponentti.ohjaus_elementti.positiivinen_ohjaus);
				negatiivinen_ohjausrajapinta=new Ohjaus_laite.VirtuaalisenNappulan_PainajaApuri(hallittava_ohjauskomponentti.ohjaus_elementti.negatiivinen_ohjaus);
			}
			
			public void PaivitaTila(Vector2 ohjailusuunta) {
				float pistetulo;
				pistetulo=Vector2.Dot(suuntavektori, ohjailusuunta);
				if(pistetulo>0) { //ohjataan positiiviseen suuntaan
					positiivinen_ohjausrajapinta.AsetaUusiTila(true);
					negatiivinen_ohjausrajapinta.AsetaUusiTila(false);
				} else if(pistetulo<0) { //ohjataan negatiiviseen suuntaan
					positiivinen_ohjausrajapinta.AsetaUusiTila(false);
					negatiivinen_ohjausrajapinta.AsetaUusiTila(true);
				} else { //kohtisuorassa -> ei ohjata ollenkaan tällä komponentilla
					positiivinen_ohjausrajapinta.AsetaUusiTila(false);
					negatiivinen_ohjausrajapinta.AsetaUusiTila(false);
				}
			}
		}
    }
	
    public class AseenKayttaja : Target_ohjaus.Target_ohjaus.GameObject_Spawner.Spawner_Timerin_Kayttaja {
		public Liikuttaja ukkelin_liikuttaja;
		public AseRajapinta ase_rajapinta;
		public float liikkeen_keskeytysaika; //kuinka pitkäksi ajaksi ukkeli keskeyttää liikkeensä
		private float liikkeen_palautusaika; //missä ajassa liike palautetaan
		private int ampumisia_jaljella; //lkm kertoo kuinka monen seuraavien framen aikana ammutaan
		
		public AseenKayttaja(Liikuttaja p_ukkelin_liikuttaja, Menu.Button_elem ampumis_nappula, Target_ohjaus.Target_ohjaus.GameObject_Spawner.Spawner_Timer p_spawner_timer) :base(p_spawner_timer) {
			ukkelin_liikuttaja=p_ukkelin_liikuttaja;
			ase_rajapinta=new AseRajapinta(ampumis_nappula);
			Removing(); //poistaa spawnerin käyttäjän target_ohjauksen listasta, joten spawneria ei kutsuta sieltä käsin
		}
		
		//painaa ohjauksien nappeja siten, että peliukkeliohjaus ohjaa ukkelia kohti seuraavaa positiota
		//pyrkii kohteeseen suoraa väylää riippuen kuitenkin käytössä olevista ohjauskomponenteista
		//mikäli ei liikuteta, kytketään kaikki ohjaukset pois
		public void PaivitaTila() {
			Spawn(); //päivittää laukausten lkm:n (kuinka monta kertaa ammutaan)
			
			if(ampumisia_jaljella>0) { //yksi laukaus per frame
				if(ukkelin_liikuttaja.modified_gameobject.target_gameobject!=null) // jos on targetti
					ukkelin_liikuttaja.KaannaOhjattavaKohtiPositiota(Ohjattava_2D.Convert_to_Vector2(ukkelin_liikuttaja.modified_gameobject.target_gameobject.transform.position));
				if(liikkeen_keskeytysaika>0) {
					ukkelin_liikuttaja.liikkeen_keskeytys=true; //pysäytetään liike määrätyksi ajaksi
					liikkeen_palautusaika=Time.time+liikkeen_keskeytysaika;
				}
				ase_rajapinta.PaivitaTila(true);
				ampumisia_jaljella--;
			} else {
				if(liikkeen_palautusaika<Time.time) ukkelin_liikuttaja.liikkeen_keskeytys=false; //palautetaan liike
				ase_rajapinta.PaivitaTila(false);
			}
		}
		
		//palauttaa lkm:n, kuinka monta kertaa ammutaan
		public override void Spawn() {
			base.Spawn(); //päivittää, kuinka monta kertaa suoritetaan
			ampumisia_jaljella+=nmb_of_to_spawn;
		}
		
		public class AseRajapinta {
			public Ohjaus_laite.VirtuaalisenNappulan_PainajaApuri ampuminen_ohjausrajapinta;
			
			public AseRajapinta(Menu.Button_elem ampumis_nappula) {
				ampuminen_ohjausrajapinta=new Ohjaus_laite.VirtuaalisenNappulan_PainajaApuri(ampumis_nappula);
			}
			
			public void PaivitaTila(bool ammutaan) {
				if(ammutaan)
					ampuminen_ohjausrajapinta.AsetaUusiTila(true);
				else
					ampuminen_ohjausrajapinta.AsetaUusiTila(false);
			}
		}
	}
	
    //inspector
    //parametriluokkia inspectoriin ja rakentajia
    //parametriluokkaan asettuu parametrit inspectorista ja rakentajalla niistä rakennetaan elementtejä
    
    //parametritluokat
    [System.Serializable]
    public class Artificial_Intelligence_Handling_Parametrit_Hallintapaneeliin {
		public float liipaisuetaisyys_kohdepositiossa=2;
		public float jumiutuneeksi_rajatun_alueen_sade=1;
		public bool lisaa_targetin_havaitsija;
		public TargetinHavaitsija_Parametrit_Hallintapaneeliin targetin_havaitsija;
		public bool lisaa_esteen_havaitsija;
		public EsteenHavaitsija_Parametrit_Hallintapaneeliin esteen_havaitsija;
		
		[System.Serializable]
		public class ToimintaMoodiValinnat {
			public ToimintaMoodi_Parametrit_Hallintapaneeliin asetukset;
			public bool ei_havaintoa;
			public bool havainto_tehty;
			public bool nakee;
			public bool nakee_takana;
		}
		public List<ToimintaMoodiValinnat> toimintamoodi_valinnat;
    }
    [System.Serializable]
    public class TargetinHavaitsija_Parametrit_Hallintapaneeliin {
		public float max_distance_to_target=10;
		public float nakokentta_asteina=135;
		public LayerMask layerMask=-1;
		public float hakuvali_kun_ei_targettia=1;
		public float hakuvali_kun_on_target=1;
		public float nakoyhteyden_testausvali=0.2f;
    }
    [System.Serializable]
    public class ToimintaMoodi_Parametrit_Hallintapaneeliin {
		public LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin liikekohteiden_maarittaja;
		public Liikuttaja_Parametrit_Hallintapaneeliin liikuttaja;
		public bool lisaa_ampuja=false;
		public AseenKayttaja_Parametrit_Hallintapaneeliin ampuja;
    }
    [System.Serializable]
    public class EsteenHavaitsija_Parametrit_Hallintapaneeliin {
		public float havaitsijan_etaisyys_ukkelista;
		public float havaitsija_viiksen_pituus;
		public float aukon_minimipituus;
		public int reunan_maksimi_kulmaheitto;
		public int reunojen_hylkaysetaisyys;
		public int kynnys_lkm_hylkaystarkistukseen;
		public float esteeseen_reagointietaisyys;
		public Vector2 esteeseen_reagointiaika;
		public int seinan_kaltevuusraja;
		public float ukkelin_pituus;
		public int etsinta_alueen_sade;
		public LayerMask layerMask;
		public List<string> ei_saa_osua_tagit;
    }
    [System.Serializable]
    public class LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Tyyppi {
			Staattinen,
			Targetin_sijainnista_riippuva
		}
		public Tyyppi tyyppi;
		public Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin asetukset_staattinen;
		public Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin asetukset_Targetin_Sijainnista_Riippuva;
    }
    [System.Serializable]
    public class Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum KohteidenMaaritys {
			Suhteellisena_alkupositioon,
			Absoluuttisena
		}
		public KohteidenMaaritys kohteiden_maaritystapa;
		[System.Serializable]
		public class Kohteet {
			public Vector2 koordinaatti;
			public GameObject sijainti;
			
			public Kohteet(Vector2 p_koordinaatti, GameObject p_sijainti) {
				koordinaatti=p_koordinaatti;
				sijainti=p_sijainti;
			}
		}
		public List<Kohteet> kohteet;
		public float odotusviive_kaannyttaessa;
    }
    [System.Serializable]
    public class Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin {
		public Target_ohjaus.Koordinaattiasetukset_Hallintapaneeliin target_position_parameters;
    }
    [System.Serializable]
    public class Liikuttaja_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Tyyppi {
			Kavelija,
			Kavelija_ja_hyppy,
			Lentaja
		}
		public Tyyppi liikkumistapa;
    }
    [System.Serializable]
    public class AseenKayttaja_Parametrit_Hallintapaneeliin {
		public Target_ohjaus.GameObject_SpawnerTimer_Parametrit_Hallintapaneeliin spawnerin_asetukset;
    }
	                                                                                                
    //rakentajat
    public class Artificial_Intelligence_Handling_Hallintapaneelista_Rakentaja {
		public Artificial_Intelligence_Handling Rakenna(Artificial_Intelligence_Handling_Parametrit_Hallintapaneeliin parametrit, GameObject gameobject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> hallittavat_ohjauskomponentit, Button_elem ampumis_nappula) {
			Artificial_Intelligence_Handling tekoaly=new Artificial_Intelligence_Handling(gameobject);
			if(parametrit.lisaa_targetin_havaitsija)
				tekoaly.LisaaTargetinHavaitsija(new TargetinHavaitsija_Hallintapaneelista_Rakentaja().Rakenna(parametrit.targetin_havaitsija, tekoaly));
			if(parametrit.lisaa_esteen_havaitsija)
				tekoaly.LisaaEsteenHavaitsija(new EsteenHavaitsija_Hallintapaneelista_Rakentaja().Rakenna(parametrit.esteen_havaitsija, tekoaly.this_Modified_GameObject, peliukkelin_liikkeen_hallitsija, parametrit.liipaisuetaisyys_kohdepositiossa, parametrit.jumiutuneeksi_rajatun_alueen_sade));
			ToimintaMoodi moodi;
			ToimintaMoodi_Hallintapaneelista_Rakentaja moodi_rakentaja=new ToimintaMoodi_Hallintapaneelista_Rakentaja();
			parametrit.toimintamoodi_valinnat.ForEach(delegate(Artificial_Intelligence_Handling_Parametrit_Hallintapaneeliin.ToimintaMoodiValinnat obj) {
				moodi=moodi_rakentaja.Rakenna(obj.asetukset, tekoaly.this_Modified_GameObject, peliukkelin_liikkeen_hallitsija, hallittavat_ohjauskomponentit, ampumis_nappula, parametrit.liipaisuetaisyys_kohdepositiossa, parametrit.jumiutuneeksi_rajatun_alueen_sade);
				tekoaly.LisaaToimintaMoodi(moodi, obj.ei_havaintoa, obj.havainto_tehty, obj.nakee, obj.nakee_takana);
			});
			return tekoaly;
		}
    }
    public class TargetinHavaitsija_Hallintapaneelista_Rakentaja {
		public Artificial_Intelligence_Handling.TargetinHavaitsija Rakenna(TargetinHavaitsija_Parametrit_Hallintapaneeliin parametrit, Artificial_Intelligence_Handling p_this_Artificial_Intelligence_Handling) {
			return new Artificial_Intelligence_Handling.TargetinHavaitsija(p_this_Artificial_Intelligence_Handling, parametrit.max_distance_to_target, parametrit.nakokentta_asteina, parametrit.layerMask.value, parametrit.hakuvali_kun_ei_targettia, parametrit.hakuvali_kun_on_target, parametrit.nakoyhteyden_testausvali);
		}
    }
    public class EsteenHavaitsija_Hallintapaneelista_Rakentaja {
		public Artificial_Intelligence_Handling.EsteenHavaitsija Rakenna(EsteenHavaitsija_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject this_Modified_GameObject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			return new Artificial_Intelligence_Handling.EsteenHavaitsija(liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade, parametrit.havaitsijan_etaisyys_ukkelista, parametrit.havaitsija_viiksen_pituus, parametrit.aukon_minimipituus, (uint)parametrit.reunan_maksimi_kulmaheitto, (uint)parametrit.reunojen_hylkaysetaisyys, (uint)parametrit.kynnys_lkm_hylkaystarkistukseen, parametrit.esteeseen_reagointietaisyys, parametrit.esteeseen_reagointiaika, (uint)parametrit.seinan_kaltevuusraja, parametrit.ukkelin_pituus, (uint)parametrit.etsinta_alueen_sade, parametrit.layerMask.value, parametrit.ei_saa_osua_tagit, this_Modified_GameObject, peliukkelin_liikkeen_hallitsija);
		}
    }
    public class ToimintaMoodi_Hallintapaneelista_Rakentaja {
		public ToimintaMoodi Rakenna(ToimintaMoodi_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject modified_gameobject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> hallittavat_ohjauskomponentit, Menu.Button_elem ampumis_nappula, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			Liikuttaja liikuttelija=new Liikuttaja_Hallintapaneelista_Rakentaja().Rakenna(parametrit.liikuttaja, modified_gameobject, peliukkelin_liikkeen_hallitsija, hallittavat_ohjauskomponentit);
			AseenKayttaja aseen_kayttaja=null;
			if(parametrit.lisaa_ampuja)
				aseen_kayttaja=new AseenKayttaja_Hallintapaneelista_Rakentaja().Rakenna(parametrit.ampuja, liikuttelija, ampumis_nappula);
			return new ToimintaMoodi(new LiikeKohteidenMaarittaja_Hallintapaneelista_Rakentaja().Rakenna(parametrit.liikekohteiden_maarittaja, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade), liikuttelija, aseen_kayttaja);
		}
    }
    public class LiikeKohteidenMaarittaja_Hallintapaneelista_Rakentaja {
		public LiikeKohteidenMaarittaja Rakenna(LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject modified_gameobject, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			if(parametrit.tyyppi==LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Tyyppi.Staattinen)
				return Rakenna_Staattinen_LiikeKohteidenMaarittaja(parametrit, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade);
			else if(parametrit.tyyppi==LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Tyyppi.Targetin_sijainnista_riippuva)
				return Rakenna_Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja(parametrit, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade);
			return null;
		}
		private Staattinen_LiikeKohteidenMaarittaja Rakenna_Staattinen_LiikeKohteidenMaarittaja(LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject modified_gameobject, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			bool kohteet_annettu_suhteessa_alkupositioon;
			if(parametrit.asetukset_staattinen.kohteiden_maaritystapa==Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.KohteidenMaaritys.Suhteellisena_alkupositioon)
				kohteet_annettu_suhteessa_alkupositioon=true;
			else
				kohteet_annettu_suhteessa_alkupositioon=false;
			return new Staattinen_LiikeKohteidenMaarittaja(parametrit.asetukset_staattinen.kohteet, kohteet_annettu_suhteessa_alkupositioon, parametrit.asetukset_staattinen.odotusviive_kaannyttaessa, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade);
		}
		private Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja Rakenna_Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja(LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject modified_gameobject, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			return new Targetin_Sijainnista_Riippuva_LiikeKohteidenMaarittaja(parametrit.asetukset_Targetin_Sijainnista_Riippuva.target_position_parameters, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade);
		}
    }
    public class Liikuttaja_Hallintapaneelista_Rakentaja {
		public Liikuttaja Rakenna(Liikuttaja_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject modified_gameobject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> hallittavat_ohjauskomponentit) {
			Vector2 hallittavat_suunnat=Vector2.zero;
			bool lisaa_hyppy=false;
			if(parametrit.liikkumistapa==Liikuttaja_Parametrit_Hallintapaneeliin.Tyyppi.Kavelija)
				hallittavat_suunnat=new Vector2(1,0); //x-tasossa
			else if(parametrit.liikkumistapa==Liikuttaja_Parametrit_Hallintapaneeliin.Tyyppi.Kavelija_ja_hyppy) {
				hallittavat_suunnat=new Vector2(1,0); //x-tasossa (normaali liikkuminen)
				lisaa_hyppy=true;
			} else if(parametrit.liikkumistapa==Liikuttaja_Parametrit_Hallintapaneeliin.Tyyppi.Lentaja)
				hallittavat_suunnat=new Vector2(1,1); //ei rajoituksia
			return new Liikuttaja(modified_gameobject, peliukkelin_liikkeen_hallitsija, hallittavat_ohjauskomponentit, hallittavat_suunnat, lisaa_hyppy);
		}
    }
    public class AseenKayttaja_Hallintapaneelista_Rakentaja {
		public AseenKayttaja Rakenna(AseenKayttaja_Parametrit_Hallintapaneeliin parametrit, Liikuttaja p_ukkelin_liikuttaja, Menu.Button_elem ampumis_nappula) {
			return new AseenKayttaja(p_ukkelin_liikuttaja, ampumis_nappula, new Target_ohjaus.GameObject_SpawnerTimer_Hallintapaneelista_Rakentaja().Rakenna(parametrit.spawnerin_asetukset));
		}
    }
}