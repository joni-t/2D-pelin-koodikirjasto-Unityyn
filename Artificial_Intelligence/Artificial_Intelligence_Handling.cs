using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Target_ohjaus;
using Peliukkeli_ohjaus;
using Ohjaus_2D;
using Ohjaus_laite;
using Menu;

namespace Artificial_Intelligence {
	//perii ohjausinput2D:n eli on palautettava ohjaustapahtumat ja tässä tapauksessa virtuaalinappuloiden painallukset
	//virtuaalinapeissa on toiminnonsuorittaja, joka ohjaa napin varsinaista tilaa, joka ohjaa varsinaisia toimintoja
	public class Artificial_Intelligence_Handling : VirtuaalinappuloidenOhjaaja {
		public PerusResurssit perusresurssit; //tekoälyn perusresurssit
		public EsteenHavaitsija esteen_havaitsija=null;
		
		public Artificial_Intelligence_Handling(GameObject this_gameobject) {
			perusresurssit=new PerusResurssit(this_gameobject);
		}
		
		public void LisaaEsteenHavaitsija(EsteenHavaitsija p_esteen_havaitsija) {
			esteen_havaitsija=p_esteen_havaitsija;
		}
		
		//ohjaus_handling kutsuu tätä: tästä käynnistyy toimintojen suorittaminen, jonka tavoitteena on painaa virtuaalinappuloita
		//ei siis varsinaisesti palauta ohjaustapahtumia, vaan ratkoo ne (generoi virtuaalinappuloiden tilat) ja asettaa ne suoraan kohteeseen (virtuaalinappuloihin)
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			if(perusresurssit.this_Modified_GameObject.gameobject!=null) {
				//bool on_jumissa=false;
				//if(current_toimintamoodi!=null)
				//	on_jumissa=current_toimintamoodi.liikekohteiden_maarittaja.OnkoTodettuJumiutuneeksi();
				if(perusresurssit.this_Modified_GameObject.gameobject.activeSelf /*& !on_jumissa*/) {
					//havaitaanko target -> päivitetään toimintamoodi
					if(perusresurssit.targetin_havaitsija!=null) {
						perusresurssit.targetin_havaitsija.EtsiTarget();
						perusresurssit.targetin_havaitsija.Havaitse();
					}
					//havaitaan esteet edessä ja kierretään ne
					if(esteen_havaitsija!=null) {
						esteen_havaitsija.SuoritaToiminnot();
						//liikutaan
						if(perusresurssit.current_toimintamoodi!=null)
							perusresurssit.current_toimintamoodi.SuoritaToiminnot(esteen_havaitsija.liikutaan_kohti_esteen_jalkeista_positionia, esteen_havaitsija.hypyn_tahtayspiste, esteen_havaitsija.hyppyvoima_paalla, esteen_havaitsija.hyppyvoima_paalla, esteen_havaitsija.liikutaan_kohti_esteen_jalkeista_positionia);
					} else if(perusresurssit.current_toimintamoodi!=null) //liikutaan
						perusresurssit.current_toimintamoodi.SuoritaToiminnot(false, Vector2.zero, false, false, false);
				}
			}
		}

		public class PerusResurssit {
			public Prefab_and_instances this_Prefab_and_instances;
			public Prefab_and_instances.Modified_GameObject this_Modified_GameObject; //ohjattava ukkeli
			public TargetinHavaitsija targetin_havaitsija=null;
			//toimintamoodit
			public ToimintaMoodi ei_havaintoa=null;
			public ToimintaMoodi havainto_tehty=null;
			public ToimintaMoodi nakee=null;
			public ToimintaMoodi nakee_takana=null;
			public ToimintaMoodi current_toimintamoodi;
			
			public PerusResurssit(GameObject this_gameobject) {
				this_Prefab_and_instances=Target_ohjaus.Target_ohjaus.Instance.Get_Prefab_with_Name(this_gameobject.name);
				this_Modified_GameObject=this_Prefab_and_instances.Add_new_GameObject(this_gameobject, null, Vector2.zero, null);
			}
			
			public void LisaaTargetinHavaitsija(TargetinHavaitsija p_targetin_havaitsija) {
				targetin_havaitsija=p_targetin_havaitsija;
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
		}

		//tarkkailee targettia ja asettaa toimintamoodin sen mukaan
		public class TargetinHavaitsija {
			public PerusResurssit this_PerusResurssit;
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
			
			public TargetinHavaitsija(PerusResurssit p_this_PerusResurssit, float p_max_distance_to_target, float p_nakokentta_asteina, int p_layerMask, float p_hakuvali_kun_ei_targettia, float p_hakuvali_kun_on_target, float p_nakoyhteyden_testausvali) {
				this_PerusResurssit=p_this_PerusResurssit;
				max_distance_to_target=p_max_distance_to_target;
				nakokentta_asteina=p_nakokentta_asteina;
				layerMask=p_layerMask;
				hakuvali_kun_ei_targettia=p_hakuvali_kun_ei_targettia;
				hakuvali_kun_on_target=p_hakuvali_kun_on_target;
				nakoyhteyden_testausvali=p_nakoyhteyden_testausvali;
			}

			//etsii riittävän lähellä olevan lähimmän targetin
			public void EtsiTarget() {
				bool hae=false;
				//haetaan target määrä välein
				if(this_PerusResurssit.this_Modified_GameObject.target_gameobject==null) { //jos ei ole targettia
					if((Time.time-edel_targetin_hakuaika)>hakuvali_kun_ei_targettia) hae=true;
				} else if((Time.time-edel_targetin_hakuaika)>hakuvali_kun_on_target) hae=true;
				if(hae) {
					edel_targetin_hakuaika=Time.time;
					finded_target=this_PerusResurssit.this_Prefab_and_instances.Find_Nearest_Target_for_GameObject_from_given_position(Ohjattava_2D.Convert_to_Vector2(this_PerusResurssit.this_Modified_GameObject.gameobject.transform.position), max_distance_to_target);
					if(finded_target!=null) //kun on kerran nähty, ei hukata sitä ennen kuin vastaan tulee uusi target
						this_PerusResurssit.this_Modified_GameObject.target_gameobject=finded_target;
				}
			}

			//havaitsee näkökentässä olevan targetin ja valitsee sen mukaan toimintamoodin
			public void Havaitse() {
				nakyyJuuri=false;
				nakyyJuuriTakana=false;
				if(this_PerusResurssit.this_Modified_GameObject.target_gameobject!=null) { //testataan näköyhteys
					if((Time.time-edel_nakoyhteyden_testausaika)<nakoyhteyden_testausvali) return; //testataan näköyhteys määrävälein
					edel_nakoyhteyden_testausaika=Time.time;
					suunta_targettiin=Ohjattava_2D.Convert_to_Vector2(this_PerusResurssit.this_Modified_GameObject.target_gameobject.transform.position-this_PerusResurssit.this_Modified_GameObject.gameobject.transform.position);
					kulma=Vector2.Angle(Ohjattava_2D.Convert_to_Vector2(this_PerusResurssit.this_Modified_GameObject.gameobject.transform.right), suunta_targettiin);
					voi_nakya_takana=false;
					voi_nakya_edessa=false;
					if(kulma<(nakokentta_asteina/2))
						voi_nakya_edessa=true;
					if((180-kulma)<(nakokentta_asteina/2))
					   voi_nakya_takana=true;
					//Debug.Log("kulma: " + kulma + ", 180-kulma: " + (180-kulma) + ", nakokentta_asteina: " + nakokentta_asteina + ", voi_nakya_edessa: " + voi_nakya_edessa.ToString() + ", voi_nakya_takana: " + voi_nakya_takana.ToString());
					if(voi_nakya_edessa || voi_nakya_takana) {
						ray=new Ray(this_PerusResurssit.this_Modified_GameObject.gameobject.transform.position, suunta_targettiin);
						if(Physics.Raycast(ray, out hit, max_distance_to_target, layerMask)) { //jos osuu johonkin -> tutkitaan osuma
							if(hit.collider.gameObject.Equals(this_PerusResurssit.this_Modified_GameObject.target_gameobject)) {
								if(Vector2.Dot(Ohjattava_2D.Convert_to_Vector2(this_PerusResurssit.this_Modified_GameObject.gameobject.transform.right), suunta_targettiin)>0) {
									//Debug.Log("testataan edesta");
									if(voi_nakya_edessa) {
										//näkee edessä päin
										onHavaittu=true;
										nakyyJuuri=true;
										if(this_PerusResurssit.VaihdaToimintaMoodi(this_PerusResurssit.nakee))
											Debug.Log("Vaihdetaan toimintamoodi: nakee");
										return;
									}
								} else if(voi_nakya_takana) {
									//Debug.Log("testataan takaa");
									onHavaittu=true;
									nakyyJuuriTakana=true;
									if(this_PerusResurssit.VaihdaToimintaMoodi(this_PerusResurssit.nakee_takana))
										Debug.Log("Vaihdetaan toimintamoodi: nakee takana");
									return;
								}
							}
						}
					}
				}
				//Debug.Log("Testataan onHavaittu: " + onHavaittu.ToString());
				if(onHavaittu) { //on havaittu joskus
					if(this_PerusResurssit.VaihdaToimintaMoodi(this_PerusResurssit.havainto_tehty))
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
			public Peliukkeli_ohjaus.Hallittava_OhjausKomponentti hyppy_ohjauskomponentti; //saadaan ratkaistua hypyn kiihtyvyys
			public float hyppy_kiihtyvyys; //kiihtyvyys, jolla ukkeli hyppää (päivitetään alustan mukaan hyppyhetkellä)
			public Havaitsija havaitsija; //havaitsija, joka muodostuu raycast-hit:sta
			public Prefab_and_instances.Modified_GameObject this_Modified_GameObject;
			public Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija;

			List<Reuna> reunat; //havaitut reunat
			Vector2 this_position; //ukkelin paikka
			Vector2 this_position_alussa; //ukkelin paikka hypyn alussa
			Vector2 havaitsijan_paikka;
			Vector2 edel_havaitsijan_paikka=Vector2.zero;
			Vector2 edel_kauimmainen_havaitsijan_paikka; //reunojen havannoinnissa edelliselä kerralla kauimmainen havaitsijan paikka
			float havaitsijaa_siirretty; //kuinka paljon havaitsijaa on siirretty edel_kauimmainen_havaitsijan_paikka:sta
			Vector2 current_velocity; //ukkelin nopeus
			Vector2 current_velocity_alussa; //ukkelin nopeus hypyn alussa
			Vector2 current_direction; //ukkelin suunta
			bool ei_ruuhkaa_reunojen_havainnoinnissa=true;
			float etaisyys_havaitsijan_edessa_olevaan_esteeseen;
			Vector2 uusi_kauimmainen_havaitsijan_paikka;
			float etaisyys_uusi_ja_edel_havaitijan_paikka;
			bool havaittu_este_juuri_nyt_edessa;
			int havaittu_este=-1; //mahdollinen havaittu este (esteen havaitsijan viiksen indeksi)
			int havaitsijan_edessa_oleva_este=-1; //este, joka ei ole vielä liian lähellä (otetaan talteen, koska havaitsija ei välttämättä havaitse estettä enää liian lähellä)
			int havaittu_este_tarpeeksi_kaukana=-1; //mahdollinen havaitsijan edessä oleva este (reuna) eli havaitsijaa ei pidä siirtää reunan läpi
			int havaitsijan_edessa_oleva_este_tarpeeksi_kaukana=-1;
			int nykyinen_este=-1;
			int uusi_este=-1;
			int edellinen_este=-1;
			int este_juuri_nyt_tarpeeksi_kaukana=-1;
			Reuna kasiteltava_tarkkailtava_reuna;
			Reuna kasiteltava_tarkkailtava_reuna_2;
			bool este_on_seina=false; //kertoo, minkälainen este on kyseessä
			uint umpikujalaskuri; //kun etsitään esteen jälkeistä positionia
			Reuna tahan_voi_hypata=null; //paikka, johon voidaan laskeutua, kun hypätään esteen yli
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
			public Vector2 hypyn_tahtayspiste; //lentoradan lakipiste, johon hyppyä tähdätään
			public float hypyn_tahtayspiste_x_siirto; //x-pistettä siirretään aikaisemmaksi, jotta ei tule liian lähelle ukkelia
			float y_suuntainen_matka; //hypyn laskennallinen kokonaismatka y-suunnassa (nousu ja lasku)
			float suhdeluku; //kuinka suuri osuus matkasta on nousua
			public bool hyppyvoima_paalla; //onko hyppy (ohjausvoima) päällä
			bool hyppaaminen_aloitettu; //hypyn alkuvaihe
			bool hyppaaminen_aloitettu_eka_kutsukerta;
			bool ehto=false;
			bool x_ehto=false;
			float putoamiskiihtyvyys_kertaa_2=-2*Physics.gravity.y;
			float putoamiskiihtyvyys_kertaa_puoli=-0.5f*Physics.gravity.y;
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
			float t_vaadittu;
			
			public EsteenHavaitsija(float p_liipaisuetaisyys_kohdepositiossa, float p_jumiutuneeksi_rajatun_alueen_sade, float p_havaitsijan_etaisyys_ukkelista, float p_havaitsija_viiksen_pituus, float p_aukon_minimipituus, uint p_reunan_maksimi_kulmaheitto, float p_esteeseen_reagointietaisyys, Vector2 p_esteeseen_reagointiaika, uint p_seinan_kaltevuusraja, float p_ukkelin_pituus, uint p_etsinta_alueen_sade, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> hallittavat_ohjauskomponentit, int p_layerMask, List<string> p_ei_saa_osua_tagit, Prefab_and_instances.Modified_GameObject p_this_Modified_GameObject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija p_peliukkelin_liikkeen_hallitsija) {
				reunat=new List<Reuna>();
				havaitsija=new Havaitsija(p_havaitsija_viiksen_pituus, p_aukon_minimipituus, p_reunan_maksimi_kulmaheitto, p_layerMask, p_ei_saa_osua_tagit);

				liipaisuetaisyys_kohdepositiossa=p_liipaisuetaisyys_kohdepositiossa;
				jumiutuneeksi_rajatun_alueen_sade=p_jumiutuneeksi_rajatun_alueen_sade;
				havaitsijan_etaisyys_ukkelista=p_havaitsijan_etaisyys_ukkelista;
				esteeseen_reagointietaisyys=p_esteeseen_reagointietaisyys;
				esteeseen_reagointiaika=p_esteeseen_reagointiaika;
				seinan_kaltevuusraja=p_seinan_kaltevuusraja;
				ukkelin_pituus=p_ukkelin_pituus;
				ukkelin_pituus_per_2=ukkelin_pituus/2;
				etsinta_alueen_sade=p_etsinta_alueen_sade;
				hallittavat_ohjauskomponentit.ForEach(delegate(Hallittava_OhjausKomponentti obj) {
					if(obj.suunta_parametrit.suunta_komponentti==Peliukkeli_ohjaus.VoimaVektori.SuuntaParametrit.suunta_y)
						hyppy_ohjauskomponentti=obj;
				});
				this_Modified_GameObject=p_this_Modified_GameObject;
				peliukkelin_liikkeen_hallitsija=p_peliukkelin_liikkeen_hallitsija;
				edel_kauimmainen_havaitsijan_paikka=Ohjattava_2D.Convert_to_Vector2(this_Modified_GameObject.gameobject.transform.position);
				siirtoaskel=havaitsija.havaitsija_viiksen_pituus;
				ukkelin_puolikas_mitta_y=new Vector2(0, ukkelin_pituus_per_2);
			}

			//esteen havaitsemiseen ja kiertämiseen liittyvät toiminnot
			public void SuoritaToiminnot() {
				this_position=Ohjattava_2D.Convert_to_Vector2(this_Modified_GameObject.gameobject.transform.position);
				current_velocity=peliukkelin_liikkeen_hallitsija.GetVelocity();
				current_direction=current_velocity.normalized;
				tavoite_suunta=(this_Modified_GameObject.target_position-this_position).normalized;
				tavoite_suunta_x.x=tavoite_suunta.x;
				HavaitseReunat();
				if(liikutaan_kohti_esteen_jalkeista_positionia) { //on havaittu este ja kierretään sitä
					//testataan, ollaanko esteen jälkeisessä positionissa -> valmis
					if(tahan_voi_hypata.LyhinEtaisyysPisteeseen(this_position)<ukkelin_pituus & tahan_voi_hypata.MinX() < this_position.x & this_position.x < tahan_voi_hypata.MaxX()) {
						liikutaan_kohti_esteen_jalkeista_positionia=false;
                        hyppyvoima_paalla = false;
						ei_ruuhkaa_reunojen_havainnoinnissa=true;
						Debug.Log("******************* este ylitetty! position: " + this_position);
					} else {
						MaaritaSeuraavaValipositioPolulla(); //ohjataan ukkelia esteen ohi (yli)
					}
				} else {
					//HavaitseReunat();
					if(havaitsija.PalautaTarkkailtavaReuna(havaittu_este)!=null) { //este havaittu -> etsitään paikka, minne voi hypätä
						ei_ruuhkaa_reunojen_havainnoinnissa=true;
						Debug.Log("***************** havaittu_este: " + havaitsija.PalautaTarkkailtavaReuna(havaittu_este).ToString() + ", this_position: " + this_position);
						if(EtsiEsteenJalkeinenPositio()) {
							Debug.Log("*************** tahan voi hypata: " + tahan_voi_hypata.ToString() + ", this_position: "  + this_position);
							//Debug.DrawLine(tahan_voi_hypata.alkupiste, tahan_voi_hypata.loppupiste, Color.white, 10);
							liikutaan_kohti_esteen_jalkeista_positionia=true;
							this_Modified_GameObject.target_position=esteen_kierto_etappien_maarittaja.AnnaSeuraavaKohde(); //vaihdetaan esteen havaitsijan tarjoama
							hypyn_tahtayspiste=this_position; //asetetaan hypyn tähtäyspiste päivitettäväksi
							hyppyvoima_paalla=true;
							hyppaaminen_aloitettu=true;
							hyppaaminen_aloitettu_eka_kutsukerta=true;
							esteen_kierto_etappien_maarittaja.TarkistaSijainti(null);
							seuraava_kohde=esteen_kierto_etappien_maarittaja.AnnaSeuraavaKohde(); //seuraava kohde polulla
							hyppy_kiihtyvyys=hyppy_ohjauskomponentti.positiivinen_ohjausvoima.nykyinen_arvo+Physics.gravity.y;
							MaaritaSeuraavaValipositioPolulla(); //ohjataan ukkelia esteen ohi (yli)
						}
					}
				}
			}
			
			//tutkii ympäristöä tietyn matkan päässä edessä (tutkii menosuunnassa tietyn kulman sisällä eteenpäin ja havaitsijan paikka on edessä menosuunnassa)
			//muodostaa yhtenäisistä havaintopisteistä reunoja (pintoja)
			//mikäli havaitsijaan on kertynyt liikaa havaittuja eri reunoja, joudutaan odottamaan, kunnes ruuhka purkautuu (reunoja jää ukkelin taakse)
			public void HavaitseReunat() {
				if(ei_ruuhkaa_reunojen_havainnoinnissa) {
					//määritetän havaitsijan liikuttelun alku- ja loppupiste
					havaitsijan_paikka=this_position+current_velocity*havaitsijan_etaisyys_ukkelista;
					kasiteltava_tarkkailtava_reuna=havaitsija.PalautaTarkkailtavaReuna(havaitsijan_edessa_oleva_este);
					if(kasiteltava_tarkkailtava_reuna!=null) { //joudutaan ehkä rajoittamaan havaitsijan paikkaa siten, ettei se mene esteen läpi 
						etaisyys_havaitsijan_edessa_olevaan_esteeseen=kasiteltava_tarkkailtava_reuna.SuunnattuEtaisyysPisteesta(this_position, current_direction); //otetaan etäisyydestä kymmenys pois (jätetään pelivaraa)
						if(etaisyys_havaitsijan_edessa_olevaan_esteeseen<Vector2.Distance(this_position, havaitsijan_paikka)) //siirretään havaitsijaa
							havaitsijan_paikka=this_position+(havaitsijan_paikka-this_position).normalized*(etaisyys_havaitsijan_edessa_olevaan_esteeseen-esteeseen_reagointietaisyys);
					}
					//liikutetaan havaitsijaa edellisestä kauimmaisesta havaitsijan pisteestä uuteen kauimmaiseen
					edel_kauimmainen_havaitsijan_paikka=uusi_kauimmainen_havaitsijan_paikka;
					uusi_kauimmainen_havaitsijan_paikka=havaitsijan_paikka;
					if(Vector2.Distance(edel_kauimmainen_havaitsijan_paikka, havaitsijan_paikka)>Vector2.Distance(this_position, havaitsijan_paikka)) { //jos ukkeli meni edellisen havaintolaajuuden yli
						edel_kauimmainen_havaitsijan_paikka=this_position;
					}
					etaisyys_uusi_ja_edel_havaitijan_paikka=Vector2.Distance(uusi_kauimmainen_havaitsijan_paikka, edel_kauimmainen_havaitsijan_paikka);
					havaitsijan_siirtosuunta=(uusi_kauimmainen_havaitsijan_paikka-edel_kauimmainen_havaitsijan_paikka).normalized;
					havaitsijan_paikka=edel_kauimmainen_havaitsijan_paikka;
					havaitsijaa_siirretty=0;
				}
				//Debug.Log("HavaitseReunat, this_position: " + this_position + ", havaitsijan_paikka (kauimmainen): " + havaitsijan_paikka + ", havaitsijan_siirtosuunta: " + havaitsijan_siirtosuunta);
				do {
					//Debug.Log("havaitaan, havaitsijan_paikka: " + havaitsijan_paikka);
					//tehdään havainnot uusista reunoista (pinnoista)
					ei_ruuhkaa_reunojen_havainnoinnissa=havaitsija.Havaitse(havaitsijan_paikka, reunat, this_position, current_direction, ei_ruuhkaa_reunojen_havainnoinnissa);
					//havaitaan mahdollinen este
					//havaitaan myös havaitsijan edessä oleva este -> ei siirretä havaitsijaa esteen läpi
					havaittu_este_juuri_nyt_edessa=false;
					havaitsija.AsetaTarkkailtavaReuna(ref havaittu_este, havaittu_este, false);
					havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este, havaitsijan_edessa_oleva_este, false);
					//Debug.Log("havaitaan reunat, this_position: " + this_position + ", havaitsijan_paikka: " + havaitsijan_paikka);
					//testataan ensiksi aiemmin talteen otettu liian kaukana oleva este
					kasiteltava_tarkkailtava_reuna=havaitsija.PalautaTarkkailtavaReuna(havaittu_este_tarpeeksi_kaukana);
					if(kasiteltava_tarkkailtava_reuna!=null) {
						if(OnkoEdessaEste(kasiteltava_tarkkailtava_reuna, this_position, current_velocity, current_direction, false, ref havaittu_este_juuri_nyt_edessa)) {
							if(havaittu_este_juuri_nyt_edessa) {
								havaitsija.AsetaTarkkailtavaReuna(ref havaittu_este, havaittu_este_tarpeeksi_kaukana, true);
								havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este, havaittu_este_tarpeeksi_kaukana, true);
							}
						}
					}
					if(ei_ruuhkaa_reunojen_havainnoinnissa) {
						kasiteltava_tarkkailtava_reuna=havaitsija.PalautaTarkkailtavaReuna(havaitsijan_edessa_oleva_este_tarpeeksi_kaukana);
						if(kasiteltava_tarkkailtava_reuna!=null) {
							if(OnkoEdessaEste(kasiteltava_tarkkailtava_reuna, havaitsijan_paikka, current_direction, current_direction, true, ref havaittu_este_juuri_nyt_edessa)) {
								if(havaittu_este_juuri_nyt_edessa) {
									havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este, havaitsijan_edessa_oleva_este_tarpeeksi_kaukana, true);
								}
							}
						}
					}
					//testataan nyt tielle osuneet
					int viiksi_ind=0;
					foreach(Havaitsija.Havaitsijan_Viiksi viiksi in havaitsija.esteen_havaitsijan_viikset) {
						if(viiksi.havaittu_reuna!=null) {
							if(OnkoEdessaEste(viiksi.havaittu_reuna, this_position, current_velocity, current_direction, false, ref havaittu_este_juuri_nyt_edessa)) {
								if(havaittu_este_juuri_nyt_edessa) {
									//Debug.Log("havaittu_este_juuri_nyt_edessa");
									havaitsija.AsetaTarkkailtavaReuna(ref havaittu_este, viiksi_ind, true);
									havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este, viiksi_ind, true);
								} // else Debug.Log("havaittu_este_ei_juuri_nyt_edessa");
								havaitsija.AsetaTarkkailtavaReuna(ref havaittu_este_tarpeeksi_kaukana, viiksi_ind, true);
								havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este_tarpeeksi_kaukana, viiksi_ind, true);
							}
							if(ei_ruuhkaa_reunojen_havainnoinnissa) {
								if(OnkoEdessaEste(viiksi.havaittu_reuna, havaitsijan_paikka, current_direction, current_direction, true, ref havaittu_este_juuri_nyt_edessa)) {
									if(havaittu_este_juuri_nyt_edessa) {
										havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este, viiksi_ind, true);
									}
									havaitsija.AsetaTarkkailtavaReuna(ref havaitsijan_edessa_oleva_este_tarpeeksi_kaukana, viiksi_ind, true);
								}
							}
						}
						viiksi_ind++;
					}
					//siirretään havaitsijaa
					if(ei_ruuhkaa_reunojen_havainnoinnissa) {
						if(etaisyys_uusi_ja_edel_havaitijan_paikka>havaitsijaa_siirretty) { //jos havaitsija ei ole päätepisteessään
							havaitsijaa_siirretty+=siirtoaskel*2;
							if(etaisyys_uusi_ja_edel_havaitijan_paikka<havaitsijaa_siirretty) //jos meni yli päätepisteen
								havaitsijaa_siirretty=etaisyys_uusi_ja_edel_havaitijan_paikka;
							havaitsijan_paikka=edel_kauimmainen_havaitsijan_paikka+havaitsijan_siirtosuunta*havaitsijaa_siirretty; //siirretään havaitsijaa
						} else
							break;
					}

					/*Debug.Log("ei_ruuhkaa_reunojen_havainnoinnissa: " + ei_ruuhkaa_reunojen_havainnoinnissa.ToString() + ", reunat:");
					foreach(Havaitsija.Havaitsijan_Viiksi viiksi in havaitsija.esteen_havaitsijan_viikset)
						if(viiksi.havaittu_reuna!=null)
							Debug.Log("viiksen_kulma: " + viiksi.viiksen_kulma + ", : " + viiksi.havaittu_reuna.ToString());*/

					
				} while(havaitsija.PalautaTarkkailtavaReuna(havaittu_este)==null & ei_ruuhkaa_reunojen_havainnoinnissa);
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
				//Debug.Log("Etsitaan esteen jalkeista positionia");
				tahan_voi_hypata=null;
				umpikujalaskuri=0; //kasvatetaan laskuria, kun on epäilyt juuttumisesta umpikujaan -> kun laskuri kasva riittävästi, todetaan olevan umpikujassa
				polku.Clear();
				havaitsijan_paikka=this_position;
				umpikujalaskuri=0;
				//tehdään ensimmäinen siirto, ettei havaitsija ole this_positionissa
				siirto_suunta=havaitsija.PalautaTarkkailtavaReuna(havaittu_este).Direction(tavoite_suunta);
				havaitsijan_paikka+=siirtoaskel*siirto_suunta;
				//Debug.Log("this_position: " + this_position + ", tavoite_suunta: " + tavoite_suunta + ", siirto_suunta: " + siirto_suunta + ", havaitsijan_paikka: " + havaitsijan_paikka);
				polku.Add(new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(new Vector2(havaitsijan_paikka.x, havaitsijan_paikka.y), null));
				edel_havaitsijan_paikka.x = havaitsijan_paikka.x;
				edel_havaitsijan_paikka.y = havaitsijan_paikka.y;
				havaitsija.AsetaTarkkailtavaReuna(ref nykyinen_este, havaittu_este, true);
				havaitsija.AsetaTarkkailtavaReuna(ref uusi_este, nykyinen_este, true);
				havaitsija.AsetaTarkkailtavaReuna(ref edellinen_este, edellinen_este, false);
				havaitsija.AsetaTarkkailtavaReuna(ref este_juuri_nyt_tarpeeksi_kaukana, este_juuri_nyt_tarpeeksi_kaukana, false);
				havaitsijan_laskennallinen_nopeus=esteeseen_reagointietaisyys/Vector2.Dot(esteeseen_reagointiaika, siirto_suunta); //lasketaan havaitsijalle laskennallinen nopeus siten, että ajasta riippuva esteeseen reagointietäisyys on esteeseen (paikallaan) reagointietäisyyden verran
				//tutkitaan kunnes löytyy laskeutumispaikka tai mennään etsintäalueen ulkopuolelle
				while(tahan_voi_hypata==null && Vector2.Distance(this_position, havaitsijan_paikka)<=etsinta_alueen_sade) {
					//siirretään havaitsijaa
					kasiteltava_tarkkailtava_reuna_2=havaitsija.PalautaTarkkailtavaReuna(uusi_este);
					if(kasiteltava_tarkkailtava_reuna_2!=null) {
						//Debug.Log("edessa on este: " + kasiteltava_tarkkailtava_reuna_2.ToString());
						kasiteltava_tarkkailtava_reuna=havaitsija.PalautaTarkkailtavaReuna(nykyinen_este);
						if(!kasiteltava_tarkkailtava_reuna_2.Equals(kasiteltava_tarkkailtava_reuna)) { //este vaihtuu -> joudutaan ratkaisemaan siirtosuunta suhteessa esteeseen
							//Debug.Log("este vaihtui");
							if(kasiteltava_tarkkailtava_reuna!=null) {
								if(kasiteltava_tarkkailtava_reuna.Equals(havaitsija.PalautaTarkkailtavaReuna(edellinen_este))) //kasvatetaan umpikujalaskuria
									if(++umpikujalaskuri>30) { //umpikuja -> lopetetaan
										Debug.Log("umpikuja");
										return false;
									}
							}
							tavoite_suunta=(this_Modified_GameObject.target_position-havaitsijan_paikka).normalized;
							pistetulo=Vector2.Dot(kasiteltava_tarkkailtava_reuna_2.ProjectPointLine(havaitsijan_paikka).normalized, tavoite_suunta);
							if(pistetulo>0) { //tavoitesuunnassa on este edessä -> kierretään pois päin this_positionista
								siirto_suunta=kasiteltava_tarkkailtava_reuna_2.Direction((havaitsijan_paikka-this_position).normalized);
								//siirretään havaitsija lähelle esteeseen reagointietäisyyttä
								normaali=kasiteltava_tarkkailtava_reuna_2.ProjectPointLine(havaitsijan_paikka);
								havaitsijan_paikka=havaitsijan_paikka+normaali-normaali.normalized*esteeseen_reagointietaisyys;
							} else siirto_suunta=tavoite_suunta; //este ei ole kulkusuunnassa edessä
							havaitsija.AsetaTarkkailtavaReuna(ref edellinen_este, nykyinen_este, true);
							havaitsija.AsetaTarkkailtavaReuna(ref nykyinen_este, uusi_este, true);
						}
					} else {
						if(kasiteltava_tarkkailtava_reuna!=null) { //muuttuu esteettömäksi
							siirto_suunta=tavoite_suunta; //ei ole estettä kulkusuunnassa edessä
							havaitsija.AsetaTarkkailtavaReuna(ref edellinen_este, nykyinen_este, true);
							havaitsija.AsetaTarkkailtavaReuna(ref nykyinen_este, nykyinen_este, false);
						}
						//Debug.Log("ei ole estetta");
					}
					if(havaitsija.PalautaTarkkailtavaReuna(nykyinen_este)!=null || havaitsija.PalautaTarkkailtavaReuna(edellinen_este)!=null) //siirtosuunta muuttuu -> päivitetään havaitsijan_laskennallinen_nopeus
						havaitsijan_laskennallinen_nopeus=esteeseen_reagointietaisyys/Vector2.Dot(esteeseen_reagointiaika, siirto_suunta);
					havaitsijan_paikka+=siirtoaskel*2*siirto_suunta;
					//Debug.Log("siirto_suunta: " + siirto_suunta + ", havaitsijan_paikka: " + havaitsijan_paikka);
					//testataan, että polun edellisestä kohdepisteestä on näköyhteys uuteen paikkaan
					vali=havaitsijan_paikka-polku[polku.Count-1].koordinaatti;
					ray=new Ray(polku[polku.Count-1].koordinaatti, vali);
					if(Physics.Raycast(ray, out hit, vali.magnitude)) { //jos välissä on este -> lisätään uusi kohde polulle
						polku.Add(new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(new Vector2(edel_havaitsijan_paikka.x, edel_havaitsijan_paikka.y), null));
						//Debug.DrawLine(polku[polku.Count-2].koordinaatti, polku[polku.Count-1].koordinaatti, Color.white, 10);
						//Debug.Log("lisataan valietappi: " + polku[polku.Count-1].koordinaatti);
					}
					//havaitaan uudet reunapisteet
					havaitsija.Havaitse(havaitsijan_paikka, reunat, havaitsijan_paikka, siirto_suunta, ei_ruuhkaa_reunojen_havainnoinnissa);
					ei_ruuhkaa_reunojen_havainnoinnissa=true; //ruuhkat puretaan välittömästi
					//havaitaan mahdollinen (uusi) este (viimeisenä havaittu)
					havaitsija.AsetaTarkkailtavaReuna(ref uusi_este, uusi_este, false);
					havaittu_este_juuri_nyt_edessa=false;
					//testataan ensiksi aiemmin talteen otettu liian kaukana oleva este
					kasiteltava_tarkkailtava_reuna=havaitsija.PalautaTarkkailtavaReuna(este_juuri_nyt_tarpeeksi_kaukana);
					if(kasiteltava_tarkkailtava_reuna!=null)
						if(OnkoEdessaEste(kasiteltava_tarkkailtava_reuna, havaitsijan_paikka, tavoite_suunta*havaitsijan_laskennallinen_nopeus, tavoite_suunta, true, ref havaittu_este_juuri_nyt_edessa))
							if(havaittu_este_juuri_nyt_edessa) {
								//Debug.Log("aiemmin kaukana ollut este on nyt lahella");
								havaitsija.AsetaTarkkailtavaReuna(ref uusi_este, este_juuri_nyt_tarpeeksi_kaukana, true);
							}
					//testataan juuri eteen tulleet
					int viiksi_ind=0;
					foreach(Havaitsija.Havaitsijan_Viiksi viiksi in havaitsija.esteen_havaitsijan_viikset) {
						if(viiksi.havaittu_reuna!=null) {
							if(OnkoEdessaEste(viiksi.havaittu_reuna, havaitsijan_paikka, tavoite_suunta*havaitsijan_laskennallinen_nopeus, tavoite_suunta, true, ref havaittu_este_juuri_nyt_edessa)) {
								if(havaittu_este_juuri_nyt_edessa) {
									//Debug.Log("havaittu_este_juuri_nyt_edessa");
									havaitsija.AsetaTarkkailtavaReuna(ref uusi_este, viiksi_ind, true);
								} //else Debug.Log("este_juuri_nyt_tarpeeksi_kaukana");
								havaitsija.AsetaTarkkailtavaReuna(ref este_juuri_nyt_tarpeeksi_kaukana, viiksi_ind, true);
							}
						}
						viiksi_ind++;
					}
					//jos edessä ei ole nyt (enää) estettä, selvitetään voisiko johonkin hypätä
					int mitka_pisteet=0;
					int mitka_pisteet2=0;
					if(havaitsija.PalautaTarkkailtavaReuna(uusi_este)==null) { //jos ei ole (enää) estettä
						foreach(Havaitsija.Havaitsijan_Viiksi viiksi in havaitsija.esteen_havaitsijan_viikset) {
							if(viiksi.havaittu_reuna!=null) {
								if(VoikoTahanHypata(viiksi.havaittu_reuna, havaitsijan_paikka)) {
									//hyppypaikan pitää olla lähempänä kohdepositionia kuin esteen (tai yhtä kaukana -> se ei olekaan este)
									if(havaitsija.PalautaTarkkailtavaReuna(havaittu_este).Etaisyys(this_Modified_GameObject.target_position, ref mitka_pisteet)>=viiksi.havaittu_reuna.Etaisyys(this_Modified_GameObject.target_position, ref mitka_pisteet2)) {
										tahan_voi_hypata=viiksi.havaittu_reuna;
										//lasketaan loppupositio reunan tasolle
										havaitsijan_paikka.y=(viiksi.havaittu_reuna.loppupiste.y-viiksi.havaittu_reuna.alkupiste.y)/(viiksi.havaittu_reuna.loppupiste.x-viiksi.havaittu_reuna.alkupiste.x)*(havaitsijan_paikka.x-viiksi.havaittu_reuna.alkupiste.x)+viiksi.havaittu_reuna.alkupiste.y+ukkelin_pituus_per_2;
										polku.Add(new Staattinen_LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin.Kohteet(new Vector2(havaitsijan_paikka.x, havaitsijan_paikka.y), null));
										//Debug.DrawLine(polku[polku.Count-2].koordinaatti, polku[polku.Count-1].koordinaatti, Color.white, 10);
										//Debug.Log("lisataan viimeinen etappi: " + polku[polku.Count-1].koordinaatti);
										//poistetaan ensimmäinen etapii, koska se on tämän hetkinen positio
										polku.RemoveAt(0);
										esteen_kierto_etappien_maarittaja=new Staattinen_LiikeKohteidenMaarittaja(polku, false, 0, this_Modified_GameObject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade);
										return true;
									}
								}
							}
						}
					}
					edel_havaitsijan_paikka=new Vector2(havaitsijan_paikka.x, havaitsijan_paikka.y);
				}
				Debug.Log("Ei loytynyt esteen jalkeista positionia, havaitsijan_paikka: " + havaitsijan_paikka + ", target_position: " + this_Modified_GameObject.target_position + ", this_position: " + this_position);
				Debug.Log("etaisyys: " + Vector2.Distance(this_position, havaitsijan_paikka) + ", etsinta_alueen_sade: " + etsinta_alueen_sade);
				return false;
			}

			//asettaa seuraavan koordinaatin esteen ylityspolulla riippuen ukkelin sijainnista ja seuraavasta kohteesta (ja edessä olevasta esteestä)
			public void MaaritaSeuraavaValipositioPolulla() {
				//selvitetään, että liike kantaa seuraavaan kohteeseen -> ohjataan tarvittaessa hyppäämään korkeammalle
				if(EnnakoiHyppaamalla()) { //jos hypyn tähtäyspiste päivitetään
					if(havaitsija.PalautaTarkkailtavaReuna(havaittu_este)!=null) { //este havaittu -> joudutaan muuttamaan suuntaa suhteessa esteeseen, jotta ei mennä liian lähelle sitä
						hypyn_tahtayspiste=SiirraKohdePositioPoisReunaanPain(hypyn_tahtayspiste, havaitsija.PalautaTarkkailtavaReuna(havaittu_este));
					}
				}
			}

			//parametrit reunan tyypin toteamiseen
			public void MuodostaReunanTutkimisParametrit(Reuna reuna, ref float reunan_kulma) {
				reunan_kulma=Reuna.Angle_0_90(reuna.Angle(tavoite_suunta_x));
			}
			
			//ilmoittaa esteestä, kun se on riittävän lähellä
			//jos OnkoEdessaReuna on asetettu, reunan ei tarvitse olla este vaan kaikki edessä olevat reunat huomioidaan, mutta ei esteenä
			//este_on_juuri_nyt_edessa on false, mikäli este ei ole vielä liian lähellä
			public bool OnkoEdessaEste(Reuna reuna, Vector2 position, Vector2 nopeus, Vector2 suunta, bool OnkoEdessaReuna, ref bool este_on_juuri_nyt_edessa) {
				este_on_juuri_nyt_edessa=false;
				MuodostaReunanTutkimisParametrit(reuna, ref reunan_kulma);
				if(reunan_kulma>seinan_kaltevuusraja) //seinä
					este_on_seina=true;
				else
					este_on_seina=false;
				//Debug.Log ("OnkoEdessaEste: " + reuna.ToString () + ", position: " + position + ", nopeus: " + nopeus + ", OnkoEdessaReuna: " + OnkoEdessaReuna.ToString() + ", este_on_seina: " + este_on_seina.ToString());
				if(nopeus.magnitude>0) { //on liikkeessä -> otetaan huomioon liikesuunta ja suuruus
					este_on_edessa=false;
					etaisyys_reunaan=reuna.SuunnattuEtaisyysPisteesta(position, suunta);
					ajasta_riippuva_esteeseen_reagointietaisyys=Vector2.Dot(nopeus, esteeseen_reagointiaika);
					if(ajasta_riippuva_esteeseen_reagointietaisyys<esteeseen_reagointietaisyys)
						ajasta_riippuva_esteeseen_reagointietaisyys=esteeseen_reagointietaisyys; //jos menee alle nolla-nopeuden rajan
					//Debug.Log("etaisyys_reunaan: " + etaisyys_reunaan + ", ajasta_riippuva_esteeseen_reagointietaisyys: " + ajasta_riippuva_esteeseen_reagointietaisyys);
					if(etaisyys_reunaan<Mathf.Infinity) {
						//Debug.Log("este on ainakin kaukana edessa");
						este_on_edessa=true; //este on ainakin kaukana edessä
						if(etaisyys_reunaan<=ajasta_riippuva_esteeseen_reagointietaisyys)
							este_on_juuri_nyt_edessa=true; //este on reagointietäisyyden sisällä
					} else { //testataan myös ylhäältä
						etaisyys_reunaan=reuna.SuunnattuEtaisyysPisteesta(position+ukkelin_puolikas_mitta_y, suunta);
						if(etaisyys_reunaan<=ajasta_riippuva_esteeseen_reagointietaisyys) {
							//Debug.Log("este on ylhaalla");
							este_on_edessa=true;
							este_on_juuri_nyt_edessa=true;
						} else {
							etaisyys_reunaan=reuna.SuunnattuEtaisyysPisteesta(position-ukkelin_puolikas_mitta_y, suunta);
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
					} else if((!este_on_seina) && (!OnkoEdessaReuna)) { //testataan lattian loppuminen
						//Debug.Log("Testataan lattian loppuminen, reuna_tutkittu_loppuun: " + reuna.reuna_tutkittu_loppuun.ToString());
						if(reuna.reuna_tutkittu_loppuun) { //reunan päätepiste on selvillä -> "este" on ainakin kaukana edessä
							//Debug.Log("etaisyys lattian reunaan: " + (reuna.Etaisyys(position-new Vector2(0, ukkelin_pituus_per_2), suunta)-nopeus.magnitude*Time.deltaTime).ToString());
							if((reuna.Etaisyys(position-new Vector2(0, ukkelin_pituus_per_2), suunta)-nopeus.magnitude*Time.deltaTime)<=esteeseen_reagointietaisyys) {
								//Debug.Log("lattia loppuu vauhdissa");
								//Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
								este_on_juuri_nyt_edessa=true;
							}
							return true;
						}
					}
				} else if(!OnkoEdessaReuna) {
					if(este_on_seina) { //seinä
						if(reuna.LyhinEtaisyysPisteeseen(position)<=esteeseen_reagointietaisyys) {
							//Debug.Log("edessa on seina");
							//Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
							este_on_juuri_nyt_edessa=true;
							return true;
						}
					} else { //lattia (loppuu)
						if(reuna.Etaisyys(position-new Vector2(0, ukkelin_pituus_per_2), tavoite_suunta)<=esteeseen_reagointietaisyys) {
							//Debug.Log("lattia loppuu");
							//Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.red, 2);
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
						//Debug.DrawLine(reuna.alkupiste, reuna.loppupiste, Color.yellow, 2);
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

			//hypyn tähtäyspisteen x-koordinaatin tarkistuspistettä pitää siirtää lähemmäksi, jotta ei synny tilanne, että ukkelin x-koordinaatti on liian lähellä kohdekoordinaattia
			public void Ratkaise_hypyn_tahtayspiste_x_siirto() {
				if(this_position.x<hypyn_tahtayspiste.x)
					hypyn_tahtayspiste_x_siirto=hypyn_tahtayspiste.x-1;
				else
					hypyn_tahtayspiste_x_siirto=hypyn_tahtayspiste.x+1;
			}

			//asettaa ukkelin hyppäämään ennakkoon, jotta lento kantaa varmasti kohdepositioniin asti
			//aluksi ukkelille lasketaan tasaisen kiihtyvyyden kaavalla hypyn lakipiste, jolla lento kantaa esteen yli
			//kantamaa vahditaan koko lennon ajan (eli kutsutaan jatkuvasti) ja kun se riittää vapaa-pudotuksella, hyppyyn pakottaminen keskeytetään
			//hypyn tähtäyspiste asetetaan sen hetkisen vaaditun lentoradan lakipisteeseen ja päivitetään, kun x- tai y-koordinaatti saavuttaa tähtäyspisteen
			//mikäli sen hetkinen liike riittää seuraavaan kohteeseen, tähtäyspiste asetetaan seuraavaan kohteeseen
			//palauttaa true, mikäli tähtäyspiste päivitetään
			public bool EnnakoiHyppaamalla() {
				if(hyppaaminen_aloitettu_eka_kutsukerta) {
					//ensimmäisellä kutsukerralla ukkeli laitetaan vain hyppäämään ja talletetaan tarvittavat parametrit, joita käytetään toisella kutsukerralla
					hyppaaminen_aloitettu_eka_kutsukerta=false;
					//Debug.Log("Hypyn kiihdyttaminen alkoi, kiihtyvyys: " + hyppy_kiihtyvyys + ", nopeus: " + current_velocity.x);
					//lasketaan vaadittu aika kantamalle
					t_vaadittu=(seuraava_kohde.x-this_position.x)/current_velocity.x;
					//otetaan tiedot talteen -> hypyn kiihdytysvaiheen koordinaatit ratkaistaan seuraavan framen aikana
					this_position_alussa=this_position;
					current_velocity_alussa=current_velocity;
					//laitetaan ukkeli hyppäämään
					hypyn_tahtayspiste.y=this_position.y+1;
					hypyn_tahtayspiste.x=this_position.x+current_direction.x;
					return false;
				} else if(hyppaaminen_aloitettu) {
					//ratkaistaan hypyn tähtäyspiste alkutilanteen perusteella eli tasaisen kiihtyvyyden lakikorkeus siten, että se kantaa esteen yli
					hyppaaminen_aloitettu=false;
					Hypyn_Koordinaattien_Ratkaisija koordinaattien_ratkaisija=new Hypyn_Koordinaattien_Ratkaisija(t_vaadittu, hyppy_kiihtyvyys, -Physics.gravity.y, this_position_alussa.y-seuraava_kohde.y);
					hypyn_tahtayspiste.y=this_position_alussa.y+koordinaattien_ratkaisija.Ratkaise(); //palauttaa lakikorkeuden
					hypyn_tahtayspiste.x=this_position_alussa.x+current_velocity.x*(koordinaattien_ratkaisija.t1+koordinaattien_ratkaisija.aikamuutos1);
				}

				if(current_velocity.y>0) { //jos nousee ylös päin
					h=Mathf.Pow(current_velocity.y,2)/putoamiskiihtyvyys_kertaa_2; //lakikorkeus
				} else {
					h=0;
				}
				//Debug.Log("EnnakoiHyppaamalla, this_position: " + this_position + ", h: " + h + ", hypyn_tahtayspiste: " + hypyn_tahtayspiste + ", seuraava_kohde: " + seuraava_kohde + ", current_velocity: " + current_velocity);
				//selvitetään, onko seuraava tähtäyspiste saavutettu
				ehto=false;
				x_ehto=false;
				if(hyppyvoima_paalla) {
					if(!(hypyn_tahtayspiste.y>(this_position.y+h)))
						ehto=true;
				} else if(!(hypyn_tahtayspiste.y<(this_position.y+h)))
					ehto=true;
				if(!ehto) {
					if(current_direction.x>0) {
						if(!(hypyn_tahtayspiste_x_siirto>this_position.x)) {
							ehto=true;
							x_ehto=true;
						}
					} else if(!(hypyn_tahtayspiste_x_siirto<this_position.x)) {
						ehto=true;
						x_ehto=true;
					}
				}
				//Debug.Log("Seuraava tahtayspiste saavutettu: " + ehto.ToString());
				if(!ehto) //jos seuraavaa tähtäyspistettä ei ole saavutettu
					return false; //ei päivitetä
				//päivitetään tähtäyspiste
				if(x_ehto & (!hyppyvoima_paalla)) {
					if(this_position.x < tahan_voi_hypata.MaxX() & this_position.x > tahan_voi_hypata.MinX()) {
						if(current_direction.x>0)
							hypyn_tahtayspiste.x=tahan_voi_hypata.MaxX();
						else
							hypyn_tahtayspiste.x=tahan_voi_hypata.MinX();
						Ratkaise_hypyn_tahtayspiste_x_siirto();
						hypyn_tahtayspiste.y=seuraava_kohde.y;
						esteen_kierto_etappien_maarittaja.SiirraSeuraavaKohde(hypyn_tahtayspiste);
						//Debug.Log("Siirretaan laskeutumispaikkaa, hypyn_tahtayspiste: " + hypyn_tahtayspiste);
						return true;
					}
				}
				if(!hyppyvoima_paalla) {
					esteen_kierto_etappien_maarittaja.TarkistaSijainti(null);
					seuraava_kohde=esteen_kierto_etappien_maarittaja.AnnaSeuraavaKohde(); //seuraava kohde polulla
				}
				//lasketaan, mihin päädytään tämän hetkisellä tilalla (nopeudella ja positionilla)
				if(current_velocity.y>0) { //jos nousee ylös päin
					t_h=current_velocity.y/(-Physics.gravity.y); //nousuaika
				} else {
					t_h=0;
				}
				//Debug.Log("seuraava_kohde: " + seuraava_kohde + ", h: " + h + ", t_h: " + t_h);
				if((this_position.y+h)>seuraava_kohde.y) // jos ja/tai menee korkeammalle kuin kohde
					t_total=t_h+Mathf.Sqrt((this_position.y-seuraava_kohde.y+h)/putoamiskiihtyvyys_kertaa_puoli); //kokonaislentoaika siihen asti kunnes y-koordinaatti on kohde-positionin tasolla
				else
					t_total=0;
				//lasketaan vaadittu aika kantamalle
				t_vaadittu=(seuraava_kohde.x-this_position.x)/current_velocity.x;
				//Debug.Log("t_total: " + t_total + ", t_vaadittu: " + t_vaadittu);
				if(t_vaadittu>t_total) { //jos kantama ei riitä, joudutaan nostamaan korkeammalle (eli hyppäämään tai jatkamaan hyppyä)
					y_suuntainen_matka=putoamiskiihtyvyys_kertaa_puoli*Mathf.Pow(t_vaadittu,2);
					suhdeluku=0.5f+(seuraava_kohde.y-this_position.y)/2/y_suuntainen_matka;
					//Debug.Log("y_suuntainen_matka: " + y_suuntainen_matka + ", suhdeluku: " + suhdeluku + ", nousumatka: " + (y_suuntainen_matka*suhdeluku));
					hypyn_tahtayspiste.x=this_position.x+current_velocity.x*t_vaadittu*suhdeluku;
					Ratkaise_hypyn_tahtayspiste_x_siirto();
					hypyn_tahtayspiste.y=this_position.y+y_suuntainen_matka*suhdeluku;
					hyppyvoima_paalla=true;
					//Debug.Log("tahdataan uuteen hyppyyn, hypyn_tahtayspiste: " + hypyn_tahtayspiste);
				} else {
					hypyn_tahtayspiste=seuraava_kohde;
					hyppyvoima_paalla=false;
					//Debug.Log("tahdataan seuraavaan kohteeseen, hypyn_tahtayspiste: " + hypyn_tahtayspiste);
				}
				return true;
			}

			//tasaisen kiihtyvyyden hypyn arvojen ja koordinaattien ratkaisija
			public class Hypyn_Koordinaattien_Ratkaisija {
				// ratkaisee hypyn koordinaatit ottaen huomioon täydelliset tiedot eli myös kiihtyvyystekijät
				/* ratkaisun vaiheet Matlab-koodina:
				  	t=3; %kokonaisaika hypylle
					a=30; %nousukiihtyvyys
					g=10; %laskukiihtyvyys
					korkeusero=-0.7 %hyppy- ja laskeutumispaikan korkeusero

					% ratkaistaan aikaparametrit siten kuin hyppy- ja laskeutumispaikka
					% olisivat samalla korkeudella
					suhdeluku=[sqrt(a+a^2/g); sqrt(g)]/(sqrt(a+a^2/g)+sqrt(g))
					v0=a*t*suhdeluku(2) % alkunopeus ylöspäin suuntautuvan nopeuden jarrutusvaiheen alussa
					t2=v0/g
					t1=(t-t2)*suhdeluku(2)
					t3=(t-t2)*suhdeluku(1)
					v0=a*t1
					t2=v0/g
					t_summa=t1+t2+t3

					%matkat laskettu eri tavoilla
					% 1)
					x1=0.5*a*t1^2 %nousukiihdytys
					x2=v0*t2-0.5*g*t2^2 %nousujarrutus
					x3=0.5*g*t3^2 % laskeutuminen (kiihdytys)
					x3_tark=x1+x2

					% 2)
					x4=0.5*a*t1^2+v0*t2 %ylöspäin suuntautuvat komponentit

)^2 %nousukiihdytys + nousujarrutus t1-funktiona (t2 johdettu t1:nä)
					x7_2=0.5*g*(t3-aikamuutos2)^2 % laskeutuminen (kiihdytys)
					ratkaistu_korkeusero_2=x6_2_2-x7_2
					*/
				public float t; //kokonaisaika hypylle
				public float a; //nousukiihtyvyys
				public float g; //laskukiihtyvyys
				public float korkeusero; //hyppy- ja laskeutumispaikan korkeusero
				public float t1; //hypyn kiihdytysaika
				public float t2; //hypyn ylös nousevan liikkeen jarrutusaika
				public float t3; //laskeutumisaika
				public float aikamuutos1; //ajan muutos, jolla saadaan säädettyä hyppy- ja laskeutumispaikan korkeusero
				public float s1; //hypyn lakikorkeus laskettuna hyppypaikasta

				public Hypyn_Koordinaattien_Ratkaisija(float p_t, float p_a, float p_g, float p_korkeusero) {
					t=p_t;
					a=p_a;
					g=p_g;
					korkeusero=p_korkeusero;
					//Debug.Log("input: t: " + t + ", a: " + a + ", g: " + g + ", korkeusero: " + korkeusero);
				}

				//palauttaa hypyn lakikorkeuden
				public float Ratkaise() {
					// ratkaistaan aikaparametrit siten kuin hyppy- ja laskeutumispaikka olisivat samalla korkeudella
					float[] suhdeluku=new float[2];
					float tekija1=a+Mathf.Pow(a,2)/g;
					suhdeluku[0]=Mathf.Sqrt(tekija1);
					suhdeluku[1]=Mathf.Sqrt(g);
					float suhdelukusumma=suhdeluku[0]+suhdeluku[1];
					suhdeluku[0]/=suhdelukusumma;
					suhdeluku[1]/=suhdelukusumma;
					float v0=a*t*suhdeluku[1]; // alkunopeus ylöspäin suuntautuvan nopeuden jarrutusvaiheen alussa
					t2=v0/g;
					t1=(t-t2)*suhdeluku[1];
					t3=(t-t2)*suhdeluku[0];
					v0=a*t1;
					t2=v0/g;
					// otetaan huomioon myös hyppy- ja laskeutumispaikan korkeusero
					float k1=0.5f*tekija1;
					float k2=0.5f*g;
					float a2=k1+k2;
					float b=2*(k1*t1+k2*t3);
					float c=k1*Mathf.Pow(t1,2)-k2*Mathf.Pow(t3,2)-korkeusero;
					float neliojuuri=Mathf.Sqrt(Mathf.Pow(b,2)-4*a2*c);
					aikamuutos1=(-b+neliojuuri)/(2*a2); // antaa oikean ratkaisun
					//float aikamuutos2=(-b-neliojuuri)/(2*a2); // ei anna oikeaa ratkaisua
					//ratkaisu:
					s1=0.5f*tekija1*Mathf.Pow(t1+aikamuutos1,2); //nousukiihdytys + nousujarrutus t1-funktiona (t2 johdettu t1:nä)
					//float s2=0.5*g*Mathf.Pow(t3-aikamuutos1,2); // laskeutuminen (kiihdytys)
					//float ratkaistu_korkeusero=s1-s2;

					//Debug.Log("s1: " + s1 + " t1: " + t1 + " t3: " + t3 + " aikamuutos1: " + aikamuutos1 + " t: " + t);

					return s1;
				}
			}
		}

		//raycast-hit ympäristön havaitsija
		//havaitsijassa on viiksiä 45-asteen välein
		public class Havaitsija {
			public float havaitsija_viiksen_pituus; //viiksen pituus nopeuden ollessa nolla viiksen suuntaan päin
			public float aukon_minimipituus; //vain tätä suuremmat aukot tunnistetaan aukoksi
			public uint reunan_maksimi_kulmaheitto; //kun pisteistä muodostetaan reuna ja laajennetaan pidemmäksi, saa jatkeen kulmaero heittää korkeintaan tämän
			public int layerMask;
			public float havaitsijan_viiksien_kulmaero;
			public List<string> ei_saa_osua_tagit; //lista prefabeista, joihin ei saa osua tai kuolee
			public bool reunoja_saa_poistaa_seurannasta=false; //mikäli havaitsijan viikset täyttyvät reunoista, joudutaan joko lopettamaan pidemmälle havainnointi tai luopumaan reunoista
			
			public Havaitsijan_Viiksi[] esteen_havaitsijan_viikset=new Havaitsijan_Viiksi[8];
			public int kulkusuunnassa_jaljimmainen_viiksi=0; //mikä viiksi on kulkusuunnassa kaikkein taaimpainen

			public Ray ray;
			public RaycastHit hit;
			Vector2 havaitsijan_paikka;
			Vector2 uusi_piste=Vector2.zero;
			Vector2 uusi_piste2=Vector2.zero;
			Vector2 viiksen_suunta=Vector2.zero;
			float aukon_minimipituus_per_2;
			Vector2 hieman_siirtosuunta=Vector2.zero;
			float kulmaero;
			int ind_ero;
			bool paluu=false;
			Havaitsijan_Viiksi kasiteltava_viiksi;
			
			public Havaitsija(float p_havaitsija_viiksen_pituus, float p_aukon_minimipituus, uint p_reunan_maksimi_kulmaheitto, int p_layerMask, List<string> p_ei_saa_osua_tagit) {
				havaitsija_viiksen_pituus=p_havaitsija_viiksen_pituus;
				aukon_minimipituus=p_aukon_minimipituus;
				reunan_maksimi_kulmaheitto=p_reunan_maksimi_kulmaheitto;
				layerMask=p_layerMask;
				ei_saa_osua_tagit=p_ei_saa_osua_tagit;
				aukon_minimipituus_per_2=aukon_minimipituus/2;

				//havaitsijat 45-asteen kulman välein
				havaitsijan_viiksien_kulmaero=Mathf.PI/(esteen_havaitsijan_viikset.Length/2);
				for(int i=0; i<esteen_havaitsijan_viikset.Length; i++) {
					esteen_havaitsijan_viikset[i]=new Havaitsijan_Viiksi(this, havaitsijan_viiksien_kulmaero*i, i);
					if(i>0) {
						esteen_havaitsijan_viikset[i].edellinen=esteen_havaitsijan_viikset[i-1];
						esteen_havaitsijan_viikset[i-1].seuraava=esteen_havaitsijan_viikset[i];
					}
					if(i==(esteen_havaitsijan_viikset.Length-1)) {
						esteen_havaitsijan_viikset[0].edellinen=esteen_havaitsijan_viikset[i];
						esteen_havaitsijan_viikset[i].seuraava=esteen_havaitsijan_viikset[0];
					}
				}
			}
			
			//tarkkailtava reuna on erityinen ja jos se vaihtuu, muutos välittyy kutsujalle
			//palauttaa true, mikäli kaikki saadaan käsiteltyä eli reunat eivät kasaudu havaitsijan viiksiin
			//palauttaa false, mikäli reunoja tulee liikaa eli tulee ruuhkaa -> pitää odotella tai poistaa turhia reunoja
			public bool Havaitse(Vector2 p_havaitsijan_paikka, List<Reuna> reunat, Vector2 this_position, Vector2 current_direction, bool ei_ruuhkaa_reunojen_havainnoinnissa) {
				havaitsijan_paikka=p_havaitsijan_paikka;
				//alkuvalmistelut
				if(!ei_ruuhkaa_reunojen_havainnoinnissa) {
					if(VapautaViiksetTurhistaReunoista(this_position, current_direction)) {
						ei_ruuhkaa_reunojen_havainnoinnissa=true;
					} else if(PaivitaKulkusuunnassaJaljimmainenViiksi(current_direction)) {
						ei_ruuhkaa_reunojen_havainnoinnissa=true;
					}
				}
				if(ei_ruuhkaa_reunojen_havainnoinnissa) {
					//havaitaan
					foreach(Havaitsijan_Viiksi viiksi in esteen_havaitsijan_viikset) {
						//Debug.Log("Havaitaan viiksella: " + viiksi.viiksen_suunta);
						if(Reuna.Angle(current_direction, viiksi.viiksen_suunta)<68.5f) { //jos viiksi on kulkusuuntaan nähden riittävän edessä
							if(!viiksi.TeeJaKasitteleHavainto()) { //mikäli havaintoa ei saada käsiteltyä -> ruuhkaa -> päivitetään kulkusuunnassa jäljimmäinen viiksi ruuhkan purkamista varten
								PaivitaKulkusuunnassaJaljimmainenViiksi(current_direction);
								return false;
							}
						} else { //todennetaan, tuliko reunan päätepiste vastaan
							//Debug.Log("Viiksi kulkusuuntaan nähden liian takana");
							viiksi.TodennaOnkoReunaTutkittuLoppuun(true);
						}
					}
					return true;
				} else
					return false;
			}

			//jäljimmäinen viiksi on kulkusuunnassaan kaikkein viimeisimpänä tuleva viiksi
			//palauttaa true, mikäli viiksi vaihtuu
			public bool PaivitaKulkusuunnassaJaljimmainenViiksi(Vector2 current_direction) {
				kulmaero=Reuna.DirectedAngle(esteen_havaitsijan_viikset[kulkusuunnassa_jaljimmainen_viiksi].viiksen_suunta, -current_direction);
				ind_ero=Mathf.RoundToInt(Mathf.Deg2Rad*kulmaero/havaitsijan_viiksien_kulmaero);
				kulkusuunnassa_jaljimmainen_viiksi+=ind_ero;
				if(kulkusuunnassa_jaljimmainen_viiksi>=esteen_havaitsijan_viikset.Length)
					kulkusuunnassa_jaljimmainen_viiksi-=esteen_havaitsijan_viikset.Length;
				else if(kulkusuunnassa_jaljimmainen_viiksi<0)
					kulkusuunnassa_jaljimmainen_viiksi+=esteen_havaitsijan_viikset.Length;
				if(ind_ero==0)
					return false;
				else
					return true;
			}

			//vapauttaa viiksiltä reunat, jotka eivät ole enää tarpeen säilyttää -> viiksiä vapautuu uusille tärkeemmille reunoille
			//palauttaa true, mikäli yksikin viiksi vapautuu
			public bool VapautaViiksetTurhistaReunoista(Vector2 this_position, Vector2 current_direction) {
				paluu=false;
				//ensin seuraavan suuntaan
				kasiteltava_viiksi=esteen_havaitsijan_viikset[kulkusuunnassa_jaljimmainen_viiksi];
				if(kasiteltava_viiksi.havaittu_reuna==null)
					kasiteltava_viiksi=kasiteltava_viiksi.seuraava;
				while(kasiteltava_viiksi.VapautaViiksiTurhastaReunasta(this_position, current_direction)) {
					kasiteltava_viiksi=kasiteltava_viiksi.seuraava;
					paluu=true;
				}
				//edellisen suuntaan
				kasiteltava_viiksi=esteen_havaitsijan_viikset[kulkusuunnassa_jaljimmainen_viiksi].edellinen;
				while(kasiteltava_viiksi.VapautaViiksiTurhastaReunasta(this_position, current_direction)) {
					kasiteltava_viiksi=kasiteltava_viiksi.edellinen;
					paluu=true;
				}
				return paluu;
			}

			//asettaa havaitsijan viiksen hallitseman reunan tarkkailtavaksi tai poistaa tarkkailtavuuden
			public void AsetaTarkkailtavaReuna(ref int tarkkailtava_reuna_tyyppi, int viiksen_ind, bool tarkkailtavaksi) {
				if(tarkkailtavaksi) { //asetetaan tarkkailtavaksi
					if(tarkkailtava_reuna_tyyppi!=viiksen_ind) { //vaihtuu
						if(tarkkailtava_reuna_tyyppi>(-1)) {
							if(esteen_havaitsijan_viikset[tarkkailtava_reuna_tyyppi].havaittu_reuna!=null) {
								esteen_havaitsijan_viikset[tarkkailtava_reuna_tyyppi].havaittu_reuna.tarkkailtavat.Remove(tarkkailtava_reuna_tyyppi);
							}
						}
						if(viiksen_ind>(-1)) {
							if(esteen_havaitsijan_viikset[viiksen_ind].havaittu_reuna!=null) {
								esteen_havaitsijan_viikset[viiksen_ind].havaittu_reuna.tarkkailtavat.Add(tarkkailtava_reuna_tyyppi);
							}
						}
					}
					tarkkailtava_reuna_tyyppi=viiksen_ind;
				} else { //poistetaan tarkkailtavuus
					tarkkailtava_reuna_tyyppi=-1;
					if(viiksen_ind>(-1)) {
						if(esteen_havaitsijan_viikset[viiksen_ind].havaittu_reuna!=null) {
							esteen_havaitsijan_viikset[viiksen_ind].havaittu_reuna.tarkkailtavat.Remove(tarkkailtava_reuna_tyyppi);
						}
					}
				}
			}

			//palauttaa tarkkailtavan reunan, mikäli se on asetettu
			//muussa tapauksessa palauttaa null
			public Reuna PalautaTarkkailtavaReuna(int viiksen_ind) {
				if(viiksen_ind>(-1)) {
					return esteen_havaitsijan_viikset[viiksen_ind].havaittu_reuna;
				}
				return null;
			}

			//viiksi tekee havaintoja ympäristöstä ja muodostaa niistä reunan
			//viiksi ylläpitää yhtä reunaa
			//mikäli havaintopiste ei kuulu viiksen ylläpitämään reunaan, sitä tarjotaan naapurille
			//mikäli havaintoa ei saada tarjottua naapurillekaan, syntyy ruuhka, jolloin havaintoja ei voida tehdä ennen kuin ruuhka on purettu
			public class Havaitsijan_Viiksi {
				public Havaitsija havaitsija;
				public float viiksen_kulma;
				public int viiksen_indeksi;
				public Vector2 viiksen_suunta=Vector2.zero;
				public Vector2 viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2=Vector2.zero;
				public Vector2 vapaa_viiksen_suunta;
				public Havaitsijan_Viiksi seuraava;
				public Havaitsijan_Viiksi edellinen;

				public Reuna havaittu_reuna=null;
				public Collider havaittu_collider=null;

				bool laajennus_ok;
				bool saa_osua;
				Vector2 uusi_piste;
				Vector2 uusi_piste2;
				Vector2 edellinen_laajennettu; //päivitetään aina vain tarvittaessa
				int suunta_parametri_edellisen_laajennetun_suuntaan=0; //mikäli edellinen laajennettu on loppupiste, arvo on 1 (alkupiste, niin -1). käytetään reuna.Direction() kertoimena
				Ray ray;
				RaycastHit hit;
				int kumpaan_pisteeseen=-1;
				int mitka_pisteet=-1;
				Havaitsijan_Viiksi naapuri;
				bool x_ehto;
				bool y_ehto;

				public Havaitsijan_Viiksi(Havaitsija p_havaitsija, float p_viiksen_kulma, int p_viiksen_indeksi) {
					viiksen_kulma=p_viiksen_kulma;
					viiksen_indeksi=p_viiksen_indeksi;
					havaitsija=p_havaitsija;
					viiksen_suunta.x=Mathf.Cos(viiksen_kulma); viiksen_suunta.y=Mathf.Sin(viiksen_kulma);
					viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2.x=-viiksen_suunta.y; viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2.y=viiksen_suunta.x;
					viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2*=havaitsija.aukon_minimipituus_per_2;
					//x1y1-x2y2=0
				}

				//palauttaa true, mikäli havainto saadaan käsiteltyä
				//palauttaa false, mikäli edes naapuri ei voi käsitellä havaintoa -> ruuhkaa, eli kaikkia reunoja eisaada säilytettyä havaitsijan viiksissä -> pitää lopettaa tutkiminen toistaiseksi
				public bool TeeJaKasitteleHavainto() {
					//Debug.Log("TeeJaKasitteleHavainto, viiksen kulmalla: " + viiksen_kulma);
					if(TeeHavainto(viiksen_suunta, ref hit, ref uusi_piste, ref saa_osua)) {
						//Debug.Log("******* Tehdaan havainnot viiksen kulmalla: " + viiksen_kulma + ", havainto: " + uusi_piste);
						if(!KasitteleHavainto(hit, uusi_piste, saa_osua)) {
							naapuri=AnnaNaapuriOmastaReunastaPisteenSuuntaan(uusi_piste);
							//Debug.Log("******* Viiksen kulma: " + viiksen_kulma + " tarjoaa havainnon naapurille: " + naapuri.viiksen_kulma);
							if(!naapuri.Ota_Vastaan_Havainto_Toiselta(hit, uusi_piste, saa_osua, AnnaSuunta(naapuri))) {
								//Debug.Log("Naapuri: " + naapuri.viiksen_kulma + " ei ottanut vastaan eli on ruuhkaa");
								return false;
							}
						}
					} else { //todennetaan, tuliko reunan päätepiste vastaan
						//Debug.Log("Ei havaintoa");
						TodennaOnkoReunaTutkittuLoppuun(true);
					}
					return true;
				}

				//vapauttaa viiksen turhasta reunasta. reuna on turha, jos se jää kulkusuunnassaan jälkeen
				//palauttaa true, mikäli viiksi vapautuu
				public bool VapautaViiksiTurhastaReunasta(Vector2 this_position, Vector2 current_direction) {
					if (havaittu_reuna != null) {
						if (current_direction.x < 0) {
							x_ehto = (this_position.x < havaittu_reuna.MinX ());
						} else {
							x_ehto = (this_position.x > havaittu_reuna.MaxX ());
						}
						if (x_ehto) {
							if (current_direction.y < 0) {
								y_ehto = (this_position.y < havaittu_reuna.MinY ());
							} else {
								y_ehto = (this_position.y > havaittu_reuna.MaxY ());
							}
							if(x_ehto & y_ehto) {
								PoistaHavaittuReuna();
								return true;
							}
						}
					}
					return false;
				}

				public void PoistaHavaittuReuna() {
					havaittu_reuna.tarkkailtavat.Clear();
					havaittu_reuna=null;
					havaittu_collider=null;
				}

				//kertoo käsiteltiinkö aiemmin samaa collideria (ehkä samaa reunaa)
				public bool KasiteltiinkoCollideria(Collider collider) {
					return collider.Equals(havaittu_collider);
				}

				public bool TeeHavainto(Vector2 p_viiksen_suunta, ref RaycastHit hit, ref Vector2 uusi_piste, ref bool saa_osua) {
					ray=new Ray(havaitsija.havaitsijan_paikka, p_viiksen_suunta);
					Debug.DrawLine(havaitsija.havaitsijan_paikka, Ohjattava_2D.Convert_to_Vector3(havaitsija.havaitsijan_paikka+havaitsija.havaitsija_viiksen_pituus*viiksen_suunta), Color.yellow, 0.1f);
					if(Physics.Raycast(ray, out hit, havaitsija.havaitsija_viiksen_pituus, havaitsija.layerMask)) { //jos osuu johonkin -> rekisteröidään piste
						saa_osua=!havaitsija.ei_saa_osua_tagit.Contains(hit.collider.tag);
						uusi_piste=Ohjattava_2D.Convert_to_Vector2(hit.point);
						return true;
					}
					return false;
				}

				//Käsittelee havainnon eli yrittää laajentaa reunaa (mikäli piste kuuluu reunaan)
				//Uuden havaintopisteen ja edellisen väli voi olla pitkäkin. Väli tutkitaan tarkasti, mikäli collider ei ole sama (reuna voi silti olla sama)
				//Palauttaa true, mikäli havainto saatiin liitettyä reunaan (tai saatiin selvyys, että havaintopisteen kohdalla ei ole riittävän pitkää reunaa)
				//palauttaa true myös, jos naapuri saa liitettyä sen omaan reunaansa (testataan, käsitteleekö naapuri jo kyseistä reunaa)
				//Palauttaa false, mikäli havaintopiste ei kuulu reunaan -> havaintopiste pitää tarjota vierekkäiselle havaitsijan viikselle
				public bool KasitteleHavainto(RaycastHit hit, Vector2 uusi_piste, bool saa_osua, bool edellinen_laajennettu_paivitetty=false) {
					//Debug.Log("KasitteleHavainto, gameobject: " + hit.collider.name + ", viiksen kulmalla: " + viiksen_kulma);
					if(KasiteltiinkoCollideria(hit.collider)) {
						//Debug.Log("Sama collider, reuna: " + havaittu_reuna.ToString());
						if(LisaaUusiPisteJoLaajennettuun(uusi_piste, saa_osua)) {
							return true;
						}
					} else if(havaittu_collider==null) {
						//tarkistetaan ensin, ettei kumpikaan naapuri käsittele jo reunaa, johon piste kuuluu
						//Debug.Log("ei aiempaa reunaa");
						if(!KuuluukoHavaintopisteNaapurinReunaan(seuraava, hit, uusi_piste, saa_osua)) {
							if(!KuuluukoHavaintopisteNaapurinReunaan(seuraava.seuraava, hit, uusi_piste, saa_osua)) {
								if(!KuuluukoHavaintopisteNaapurinReunaan(edellinen, hit, uusi_piste, saa_osua)) {
									if(!KuuluukoHavaintopisteNaapurinReunaan(edellinen.edellinen, hit, uusi_piste, saa_osua)) {
										//yritetään laajentaa
										if(!LisaaUusiPiste_JaMuodostaUusiReuna(uusi_piste, saa_osua, hit.collider.tag)) { //jos ei saatu laajennettua
											PoistaHavaittuReuna(); //hylätään nolla-pituuksinen reuna (collideria ei asetettu)
										}
									}
								}
							}
						}						
						return true;
					} else { //eri collider (gameobject) -> selvitetään, onko niiden välissä aukkoa tai muita collidereja
						//Debug.Log("Eri colliderit, reuna: " + havaittu_reuna);
						if(havaittu_reuna.Etaisyys(uusi_piste, ref kumpaan_pisteeseen)<=havaitsija.aukon_minimipituus) {
							//Debug.Log("etaisyys reunaan on riittavan pieni");
							naapuri=AnnaNaapuriOmastaReunastaPisteenSuuntaan(uusi_piste);
							if(!KuuluukoHavaintopisteNaapurinReunaan(naapuri, hit, uusi_piste, saa_osua)) {
								if(LisaaUusiPisteJoLaajennettuun(uusi_piste, saa_osua)) {
									edellinen_laajennettu=uusi_piste; //riittää olion päivitys
									havaittu_collider=hit.collider;
									return true;
								} else {
									AsetaEdellinenLaajennettu(edellinen_laajennettu_paivitetty, kumpaan_pisteeseen);
								}
							} else {
								//yhdistetään reunat, jos on sama reuna (eri osaset) -> naapuri jatkaa reunan hallinnointia
								//Debug.Log("Kuuluu naapurille -> yritetaan yhdistaa reunat, naapurin: " + naapuri.havaittu_reuna);
								if(naapuri.havaittu_reuna.YhdistaReunatJosEhdotTayttyy(havaittu_reuna, havaitsija.aukon_minimipituus, havaitsija.reunan_maksimi_kulmaheitto)) {
									//Debug.Log("Reunat yhdistettiin");
									havaittu_reuna.tarkkailtavat.ForEach(delegate(int obj) {
										obj=naapuri.viiksen_indeksi;
										naapuri.havaittu_reuna.tarkkailtavat.Add(obj);
									});
									PoistaHavaittuReuna();
								} else {
									AsetaEdellinenLaajennettu(edellinen_laajennettu_paivitetty, kumpaan_pisteeseen);
									//Debug.Log("Reunoja ei saatu yhdistettya");
									TodennaOnkoReunaTutkittuLoppuun();
								}
								return true;
							}
						} else if(havaittu_reuna.Angle(uusi_piste, true)<havaitsija.reunan_maksimi_kulmaheitto) { //havaintopisteen etäisyys reunaan on liian suuri -> joudutaan tutkimaan tarkemmin muodostamalla lisää havaintopisteitä
							//Debug.Log("Havaintopisteen etaisyys reunaan on liian suuri -> muodostetaan väliin uusi(a) pisteita");
							AsetaEdellinenLaajennettu(edellinen_laajennettu_paivitetty, kumpaan_pisteeseen);
							uusi_piste2=Vector2.Lerp(uusi_piste, edellinen_laajennettu, 0.5f); //puolitetaan pisteiden väli
							//Debug.Log("uusi havaintopiste-ehdokas: " + uusi_piste2);
							while(!TeeHavainto((uusi_piste2-havaitsija.havaitsijan_paikka).normalized, ref hit, ref uusi_piste2, ref saa_osua)) {
								if(Vector2.Distance(edellinen_laajennettu, uusi_piste2)<havaitsija.aukon_minimipituus_per_2) return false; //on riittävän lähellä -> toisin sanoen havaintopisteeseen kuuluva reunan osa ei kuulu reunaan
								uusi_piste2=Vector2.Lerp(uusi_piste2, edellinen_laajennettu, 0.5f); //puolitetaan pisteiden väli, kunnes osuu johonkin
								//Debug.Log("uusi havaintopiste-ehdokas: " + uusi_piste2);
							}
							//Debug.Log("kelpaa: havaintopiste-ehdokas: " + uusi_piste2);
							RaycastHit hit_kopio=hit;
							Vector2 uusi_piste_kopio=uusi_piste2;
							bool saa_osua_kopio=saa_osua;
							if(KasitteleHavainto(hit, uusi_piste2, saa_osua, true)) { //jos eri colliderit muodostavat yhtenäisen reunan -> koitetaan laajentaa lisää samalla kun palataan
								//Debug.Log("Kasitellaan paluu-suunnassa: " + uusi_piste_kopio);
								if(KasitteleHavainto(hit_kopio, uusi_piste_kopio, saa_osua_kopio, true)) {
									return true;
								}
							}
						} else {
							AsetaEdellinenLaajennettu(edellinen_laajennettu_paivitetty, kumpaan_pisteeseen);
						}
					}
					//Debug.Log("KasitteleHavainto: palautetaan false");
					TodennaOnkoReunaTutkittuLoppuun();
					return false;
				}

				//päivittää tutkittavan reunan edellisen laajennetun pisteen
				public void AsetaEdellinenLaajennettu(bool edellinen_laajennettu_paivitetty, int kumpaan_pisteeseen) {
					if(!edellinen_laajennettu_paivitetty) {
						if(kumpaan_pisteeseen==0) {
							edellinen_laajennettu=havaittu_reuna.alkupiste;
							suunta_parametri_edellisen_laajennetun_suuntaan=-1;
						} else {
							edellinen_laajennettu=havaittu_reuna.loppupiste;
							suunta_parametri_edellisen_laajennetun_suuntaan=1;
						}
					}
				}

				//todentaa, onko tutkittavan reunan päätepiste löytynyt -> reuna tutkittu loppuun
				//asettaa tiedon reunaan
				//edellinen laajennettu pitää olla selvillä, mikäli viiksellä on havaittu reuna (joka siis todennetaan)
				public void TodennaOnkoReunaTutkittuLoppuun(bool paivita_edellinen_laajennettu=false) {
					//Debug.Log("TodennaOnkoReunaTutkittuLoppuun, havaittu_reuna==null: " + (havaittu_reuna==null).ToString());
					if(havaittu_reuna!=null) {
						//Debug.Log("TodennaOnkoReunaTutkittuLoppuun: " + havaittu_reuna.ToString() + ", on todennettu jo: " + havaittu_reuna.reuna_tutkittu_loppuun.ToString());
						if(!havaittu_reuna.reuna_tutkittu_loppuun) {
							//todennetaan, onko reuna tutkittu loppuun asti
							Reuna havaitsijan_viiksi_reunana=new Reuna(havaitsija.havaitsijan_paikka, havaitsija.havaitsijan_paikka+viiksen_suunta*havaitsija.havaitsija_viiksen_pituus, false, "");
							//Debug.Log("havaitsijan_viiksi_reunana: " + havaitsijan_viiksi_reunana.ToString() + ", suunnattu etaisyys: " + havaitsijan_viiksi_reunana.SuunnattuEtaisyysPisteesta(edellinen_laajennettu, havaittu_reuna.Direction(Vector2.zero)*suunta_parametri_edellisen_laajennetun_suuntaan));
							if(paivita_edellinen_laajennettu) {
								havaittu_reuna.Etaisyys(havaitsijan_viiksi_reunana, ref mitka_pisteet);
								if(mitka_pisteet<2)
									kumpaan_pisteeseen=0; //alkupiste
								else
									kumpaan_pisteeseen=1; //loppupiste
								AsetaEdellinenLaajennettu(false, kumpaan_pisteeseen);
							}
							//Debug.Log("edellinen_laajennettu: " + edellinen_laajennettu + ", suuntaan: " + (havaittu_reuna.Direction(Vector2.zero)*suunta_parametri_edellisen_laajennetun_suuntaan));
							if(havaitsijan_viiksi_reunana.SuunnattuEtaisyysPisteesta(edellinen_laajennettu, havaittu_reuna.Direction(Vector2.zero)*suunta_parametri_edellisen_laajennetun_suuntaan)<Mathf.Infinity) {
								// reunan jatke leikkaa havaitsijan viiksen -> on todennettu
								havaittu_reuna.reuna_tutkittu_loppuun=true;
							}
						}
					}
				}

				public bool Ota_Vastaan_Havainto_Toiselta(RaycastHit hit, Vector2 uusi_piste, bool saa_osua, bool tarjoamis_suunta_eteenpain) {
					//Debug.Log("viiksi: " + viiksen_kulma + ", koittaa ottaa havainnon toiselta, havainto: " + uusi_piste);
					if(!KasitteleHavainto(hit, uusi_piste, saa_osua)) {
						//Debug.Log("Koitetaan tarjota oma reuna naapurille");
						if(AnnaSeuraavaTaiEdellinen(tarjoamis_suunta_eteenpain).Ota_Vastaan_Reuna_Toiselta(havaittu_reuna, havaittu_collider, tarjoamis_suunta_eteenpain, 1)) { //koitetaan tarjota oma reuna seuraavalle
							//Debug.Log("reuna saatiin naapurille");
							if(!LisaaUusiPiste_JaMuodostaUusiReuna(uusi_piste, saa_osua, hit.collider.tag)) { //jos ei saatu laajennettua
								PoistaHavaittuReuna(); //hylätään nolla-pituuksinen reuna
							}
							return true;
						}
						return false;
					}
					return true;
				}
				
				public bool Ota_Vastaan_Reuna_Toiselta(Reuna reuna, Collider collider, bool tarjoamis_suunta_eteenpain, uint rekursio_kutsulaskuri) {
					//Debug.Log("viiksi: " + viiksen_kulma + " koittaa ottaa reunan: " + reuna.ToString() + " vastaan toiselta, laskurin arvo: " + rekursio_kutsulaskuri);
					if((++rekursio_kutsulaskuri)>=havaitsija.esteen_havaitsijan_viikset.Length) return false; //on kiertänyt kierroksen -> ei tarjota aloittajalle
					bool otetaan_vastaan=false;
					if(reuna.Angle(viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2, false)<46) { //kulmaero riittävän pieni
						//Debug.Log("kulmaero on riittavan pieni");
						if(havaittu_reuna!=null) { //oma pitää saada siirrettyä seuraavalle
							//Debug.Log("Oma reuna joudutaan siirtamaan");
							if(AnnaSeuraavaTaiEdellinen(tarjoamis_suunta_eteenpain).Ota_Vastaan_Reuna_Toiselta(havaittu_reuna, havaittu_collider, tarjoamis_suunta_eteenpain, rekursio_kutsulaskuri)) {
								otetaan_vastaan=true;
							}
						} else {
							otetaan_vastaan=true;
						}
						//Debug.Log("otetaan_vastaan: " + otetaan_vastaan.ToString());
						if(otetaan_vastaan) {
							havaittu_reuna=reuna;
							havaittu_collider=collider;
							return true;
						}
					}
					return false;
				}

				//kun collider vaihtuu tai havaitsijan viiksi on aloittamassa uutta reunaa, pitää ensin testata ettei naapuri jo käsittele reunaa, johon havaintopiste kuuluu
				//tällä voi testata minkä tahansa naapurin
				public bool KuuluukoHavaintopisteNaapurinReunaan(Havaitsijan_Viiksi naapuri, RaycastHit hit, Vector2 uusi_piste, bool saa_osua) {
					//Debug.Log("KuuluukoHavaintopisteNaapurinReunaan, viiksi, piste: " + uusi_piste);
					if(naapuri!=null) {
						//Debug.Log("naapuri: " + naapuri.viiksen_kulma);
						if(naapuri.havaittu_collider!=null) {
							if(naapuri.KasitteleHavainto(hit, uusi_piste, saa_osua)) {
								//Debug.Log("kuuluu");
								return true;
							}
						}
					}
					//Debug.Log("ei kuulu");
					return false;
				}
				
				public Havaitsijan_Viiksi AnnaSeuraavaTaiEdellinen(bool suunta_eteenpain) {
					if(suunta_eteenpain)
						return seuraava;
					else
						return edellinen;
				}

				//ratkaisee suunnan reunasta pisteen suuntaan (kumpi naapuriviiksi on pisteen suunnassa omasta reunasta katsottuna)
				public Havaitsijan_Viiksi AnnaNaapuriOmastaReunastaPisteenSuuntaan(Vector2 havaintopiste) {
					//seuraavassa voidaan käyttää joko alku- tai loppupistettä
					if(Reuna.DirectedAngle(havaittu_reuna.alkupiste-havaitsija.havaitsijan_paikka, havaintopiste-havaitsija.havaitsijan_paikka)<180) {
						return seuraava; //suunta eteenpäin
					}
					return edellinen;
				}

				public bool AnnaSuunta(Havaitsijan_Viiksi naapuri) {
					if(naapuri.Equals(seuraava))
						return true; //suunta eteen päin
					else
						return false; //suunta taakse päin
				}

				//lisää pisteen aiemmin jo laajennettuun reunaan (pitää olla tiedossa)
				public bool LisaaUusiPisteJoLaajennettuun(Vector2 uusi_piste, bool saa_osua) {
					//Debug.Log("LisaaUusiPisteJoLaajennettuun: " + uusi_piste);
					if(havaittu_reuna.LaajennaReunaJosEhdotTayttyy(uusi_piste, saa_osua, havaitsija.reunan_maksimi_kulmaheitto, havaitsija.aukon_minimipituus)) {
						//Debug.Log("lisattiin");
						return true;
					}
					return false;
				}

				//lisää pisteen aiemmin laajentamattoon reunaan (pitää olla tiedossa, mutta ei haittaa, vaikka pitäisi käyttää kuitenkin muussa tapauksessa tuota yksinkertaisempaa)
				public bool LisaaUusiPisteLaajentamattomaan(Vector2 uusi_piste, bool saa_osua) {
					//Debug.Log("LisaaUusiPisteLaajentamattomaan: " + uusi_piste + ", reuna: " + havaittu_reuna.ToString());
					if(havaittu_reuna.LaajennaReunaJosEhdotTayttyy(uusi_piste, saa_osua, havaitsija.reunan_maksimi_kulmaheitto, havaitsija.aukon_minimipituus)) {
						//Debug.Log("Laajentaa");
						ray=new Ray(havaitsija.havaitsijan_paikka, (Vector2.Lerp(havaittu_reuna.alkupiste, havaittu_reuna.loppupiste, 0.5f)-havaitsija.havaitsijan_paikka).normalized);
						if(Physics.Raycast(ray, out hit, havaitsija.havaitsija_viiksen_pituus, havaitsija.layerMask)) { //jos osuu johonkin
							saa_osua=!havaitsija.ei_saa_osua_tagit.Contains(hit.collider.tag);
							uusi_piste=Ohjattava_2D.Convert_to_Vector2(hit.point);
							if(havaittu_reuna.LaajennaReunaJosEhdotTayttyy(uusi_piste, saa_osua, havaitsija.reunan_maksimi_kulmaheitto, havaitsija.aukon_minimipituus)) {
								//Debug.Log("Laajennus hyvaksytty, pituus: " + Vector2.Distance(havaittu_reuna.alkupiste, havaittu_reuna.loppupiste));
								if(Vector2.Distance(havaittu_reuna.alkupiste, havaittu_reuna.loppupiste)>0) { //jos ei laajenna olemassa olevaan pisteeseen
									//Debug.Log("Laajennus riittavan levea");
									return true;
								}
							}
						}
						havaittu_reuna.PalautaNollaPituiseksi(uusi_piste); //kumotaan laajennus
					}
					//Debug.Log("Ei lisatty");
					return false;
				}

				public bool LisaaUusiPiste_JaMuodostaUusiReuna(Vector2 p_uusi_piste, bool saa_osua, string tag) {
					//Debug.Log("LisaaUusiPiste_JaMuodostaUusiReuna: " + p_uusi_piste);
					havaittu_reuna=new Reuna(new Vector2(p_uusi_piste.x, p_uusi_piste.y), new Vector2(p_uusi_piste.x, p_uusi_piste.y), saa_osua, tag);
					//laajennetaan reunaa, jotta saadaan suunta selville
					laajennus_ok=false;
					//Debug.Log("Viiksen suunta: " + viiksen_suunta + ", viiksen_kohtisuora_suunta: " + viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2 + ", havaitsijan_paikka: " + havaitsija.havaitsijan_paikka);
					//Debug.Log("1. havainnon suunta normalisoitu: " + ((p_uusi_piste+viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2)-havaitsija.havaitsijan_paikka).normalized + ", 1. havainnon suunta: " + ((p_uusi_piste+viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2)-havaitsija.havaitsijan_paikka));
					if(TeeHavainto(((p_uusi_piste+viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2)-havaitsija.havaitsijan_paikka).normalized, ref hit, ref uusi_piste, ref saa_osua)) {
						//Debug.Log("Saatiin suunta, piste: " + uusi_piste);
						//saatiin suunta -> muodostetaan riittävän leveä reuna
						//Debug.Log("Levennetaan suuntaan normalisoitu: " + ((p_uusi_piste+(uusi_piste-p_uusi_piste).normalized*havaitsija.aukon_minimipituus)-havaitsija.havaitsijan_paikka).normalized + ", Levennetaan suuntaan: " + ((p_uusi_piste+(uusi_piste-p_uusi_piste).normalized*havaitsija.aukon_minimipituus)-havaitsija.havaitsijan_paikka));
						if(TeeHavainto(((p_uusi_piste+(uusi_piste-p_uusi_piste).normalized*havaitsija.aukon_minimipituus)-havaitsija.havaitsijan_paikka).normalized, ref hit, ref uusi_piste, ref saa_osua)) {
							//Debug.Log("Levennetty pisteeseen: " + uusi_piste);
							if(LisaaUusiPisteLaajentamattomaan(uusi_piste, saa_osua)) {
								laajennus_ok=true;
							}
						}
					}
					if(!laajennus_ok) { //koitetaan laajentaa toiseen suuntaan
						//Debug.Log("Laajennus ei onnistunut. Koitetaan toiseen suuntaan, suunta normalisoitu: " + ((p_uusi_piste-viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2)-havaitsija.havaitsijan_paikka).normalized + ", Laajennus ei onnistunut. Koitetaan toiseen suuntaan, suunta: " + ((p_uusi_piste-viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2)-havaitsija.havaitsijan_paikka));
						if(TeeHavainto(((p_uusi_piste-viiksen_kohtisuora_suunta_kertaa_aukon_minimipituus_per_2)-havaitsija.havaitsijan_paikka).normalized, ref hit, ref uusi_piste, ref saa_osua)) {
							//Debug.Log("Saatiin suunta, piste: " + uusi_piste);
							//saatiin suunta -> muodostetaan riittävän leveä reuna
							//Debug.Log("Levennetaan suuntaan normalisoitu: " + ((p_uusi_piste+(uusi_piste-p_uusi_piste).normalized*havaitsija.aukon_minimipituus)-havaitsija.havaitsijan_paikka).normalized + ", Levennetaan suuntaan: " + ((p_uusi_piste+(uusi_piste-p_uusi_piste).normalized*havaitsija.aukon_minimipituus)-havaitsija.havaitsijan_paikka));
							if(TeeHavainto(((p_uusi_piste+(uusi_piste-p_uusi_piste).normalized*havaitsija.aukon_minimipituus)-havaitsija.havaitsijan_paikka).normalized, ref hit, ref uusi_piste, ref saa_osua)) {
								//Debug.Log("Levennetty pisteeseen: " + uusi_piste);
								if(LisaaUusiPisteLaajentamattomaan(uusi_piste, saa_osua)) {
									laajennus_ok=true;
								}
							}
						}
					}
					//Debug.Log("laajennus_ok: " + laajennus_ok.ToString());
					if(laajennus_ok) {
						havaittu_collider=hit.collider;
						return true;
					}
					return false;
				}
			}
		}

        //luokka esittää suoraa reunaa (pintaa)
        public class Reuna {
			public Vector2 alkupiste;
			public Vector2 loppupiste;
			public bool saa_osua;
			public string tag;
			public List<int> tarkkailtavat; //reuna on tarkkailtava, jos se on todettu esteeksi juurin nyt tai kauempana edessä. listassa on tiedot, millä tavalla on tarkkailtava
			public bool reuna_tutkittu_loppuun=false; // asetetaan, kun reunan päätepiste (kulkusuunnassa) on löytynyt
			public bool suunta_kiinnitetty=false; //suunta saattaa aluksi olla 180 astetta haluttua väärään suuntaan, joten se tarkistetaan ensin
			
			public Reuna(Vector2 p_alkupiste, Vector2 p_loppupiste, bool p_saa_osua, string p_tag) {
				alkupiste=p_alkupiste;
				loppupiste=p_loppupiste;
				saa_osua=p_saa_osua;
				tag=p_tag;
				tarkkailtavat=new List<int>();
			}
			
			public override string ToString ()
			{
				return alkupiste + "->" + loppupiste + " (" + tag + ")";
			}
			
			public Vector2 Direction(Vector2 vertailusuunta) {
				Vector2 suunta=(loppupiste-alkupiste).normalized;
				if((!suunta_kiinnitetty) & (vertailusuunta.sqrMagnitude>0)) {
					//jos suunta on käänteinen vertailusuunnalle, vaihdetaan suunta
					if(Vector2.Dot(vertailusuunta, suunta)<0) {
						suunta=alkupiste;
						alkupiste=loppupiste;
						loppupiste=suunta;
						suunta=(loppupiste-alkupiste).normalized;
					}
					suunta_kiinnitetty=true;
				}
				return suunta;
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
				//Debug.DrawLine(alkupiste, loppupiste, Color.black, 10);
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
				return Angle(toinen_reuna.Direction(Vector2.zero), Direction(Vector2.zero));
			}
			//antaa kulman 0-180 astetta
			public float Angle(Vector2 suunta, bool annettu_piste=false) {
				if(annettu_piste) { //suunta onkin piste -> ratkaistaan suunta
					Vector2 lahempi_piste=alkupiste;
					if(Vector2.Distance(suunta,alkupiste)>Vector2.Distance(suunta,loppupiste)) lahempi_piste=loppupiste;
					return Angle((suunta-lahempi_piste).normalized, Direction(Vector2.zero));
				} else
					return Angle(Direction(Vector2.zero), suunta);
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
								//Debug.DrawLine(alkupiste, uusi_piste, vari, 10);
								alkupiste=uusi_piste;
							}
						} else if(Vector2.Distance(uusi_piste, alkupiste)>Vector2.Distance(alkupiste, loppupiste)) { //jos uusi piste todella laajentaa
							//Debug.DrawLine(loppupiste, uusi_piste, vari, 10);
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
							//Debug.DrawLine(alkupiste, toinen_reuna.loppupiste, vari, 10);
							alkupiste=toinen_reuna.loppupiste;
						} else if(mitka_pisteet_lahimpina==1) {
							//Debug.DrawLine(loppupiste, toinen_reuna.loppupiste, vari, 10);
							loppupiste=toinen_reuna.loppupiste;
						}
					} else if(mitka_pisteet_sisalla==1) { //loppupiste
						Etaisyys(toinen_reuna.alkupiste, ref mitka_pisteet_lahimpina);
						if(mitka_pisteet_lahimpina==0) {
							//Debug.DrawLine(alkupiste, toinen_reuna.alkupiste, vari, 10);
							alkupiste=toinen_reuna.alkupiste;
						} else if(mitka_pisteet_lahimpina==1) {	
							//Debug.DrawLine(loppupiste, toinen_reuna.alkupiste, vari, 10);
							loppupiste=toinen_reuna.alkupiste;
						}
					} else if(mitka_pisteet_sisalla==2) { //molemmat -> ei muutoksia tähän reunaan
						//Debug.DrawLine(alkupiste, loppupiste, vari, 10);
					} else if(mitka_pisteet_sisalla==3) { //tämä reuna on kokonaan toisen sisällä -> vaihdetaan
						//Debug.DrawLine(toinen_reuna.alkupiste, toinen_reuna.loppupiste, vari, 10);
						alkupiste=toinen_reuna.alkupiste;
						loppupiste=toinen_reuna.loppupiste;
					}
					return true;
					//tutkitaan vain reunojen välinen etäisyys
				} else if(Etaisyys(toinen_reuna, ref mitka_pisteet_lahimpina)<=aukon_minimipituus) { //yhdistetään
					//Debug.Log("yhdistetaan vierekkaiset reunat: " + ToString() + "_ja_" + toinen_reuna.ToString());
					if(mitka_pisteet_lahimpina==0) {
						//Debug.DrawLine(alkupiste, toinen_reuna.loppupiste, vari, 10);
						alkupiste=toinen_reuna.loppupiste;
					}
					else if(mitka_pisteet_lahimpina==1) {
						//Debug.DrawLine(alkupiste, toinen_reuna.alkupiste, vari, 10);
						alkupiste=toinen_reuna.alkupiste;
					}
					else if(mitka_pisteet_lahimpina==2) {
						//Debug.DrawLine(loppupiste, toinen_reuna.loppupiste, vari, 10);
						loppupiste=toinen_reuna.loppupiste;
					}
					else if(mitka_pisteet_lahimpina==3) {
						//Debug.DrawLine(loppupiste, toinen_reuna.alkupiste, vari, 10);
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

	//suppeampi "tekoäly" esim. staattisesti liikkuvien ohjaamiseen
	//antaa hissille koordinaatit ja target ohjauksen prefabin moving-olio huolehtii liikuttamisesta eli ei varsinaisesti ohjaa mitään nappuloita
	public class LiikkuvaHissiHallinta : VirtuaalinappuloidenOhjaaja {
		public Artificial_Intelligence_Handling.PerusResurssit perusresurssit; //tekoälyn perusresurssit
		
		public LiikkuvaHissiHallinta(GameObject this_gameobject) {
			perusresurssit=new Artificial_Intelligence_Handling.PerusResurssit(this_gameobject);
		}
		
		//ohjaus_handling kutsuu tätä: tästä käynnistyy toimintojen suorittaminen, jonka tavoitteena on antaa hissille koordinaatti
		//ei siis palauta ohjaustapahtumia, vaan ainoastaan ratkaisee hissin kohdekoordinaatin
		public override void PalautaOhjaustapahtumat(Ohjaus_handling_2D ohjaus_handling) {
			if(perusresurssit.this_Modified_GameObject.gameobject!=null) {
				//bool on_jumissa=false;
				//if(current_toimintamoodi!=null)
				//	on_jumissa=current_toimintamoodi.liikekohteiden_maarittaja.OnkoTodettuJumiutuneeksi();
				if(perusresurssit.this_Modified_GameObject.gameobject.activeSelf /*& !on_jumissa*/) {
					//havaitaanko target -> päivitetään toimintamoodi
					if(perusresurssit.targetin_havaitsija!=null) {
						perusresurssit.targetin_havaitsija.EtsiTarget();
						perusresurssit.targetin_havaitsija.Havaitse();
					}
					//tarkistetaan kohdekoordinaatti
					perusresurssit.current_toimintamoodi.TarkistaSijainti();
				}
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
		public void SuoritaToiminnot(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde, bool ei_olla_kohteessa, bool ala_tarkista_ollaanko_kohteessa, bool saa_hypata) {
			if(aseen_kayttaja!=null) aseen_kayttaja.PaivitaTila(); //ammutaan ensin, koska ampuminen mahdollisesti käskee ukkelin olla paikallaan
			liikuttaja.PaivitaOhjauksienTilat(liikekohteiden_maarittaja.TarkistaSijainti(liikuttaja, annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, ei_olla_kohteessa, ala_tarkista_ollaanko_kohteessa), annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, saa_hypata);
		}

		//ainoastaan sijainnin tarkistus (kohdekoordinaatin asetus) eli ei huolehdi varsinaisesta liikuttelusta
		//liikuttelu tapahtuu esim target ohjauksen prefabin liikuttelijalla
		public void TarkistaSijainti() {
			liikekohteiden_maarittaja.TarkistaSijainti(null);
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
		
		public abstract bool TarkistaSijainti(Liikuttaja liikuttaja, bool annetaan_vaihtoehtoinen_kohde=false, Vector2 vaihtoehtoinen_kohde=new Vector2(), bool ei_olla_kohteessa=false, bool ala_tarkista_ollaanko_kohteessa=false); //palauttaa true, mikäli ukkelia pitää liikuttaa (ei ole odottamassa)
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
		public bool OnkoKohteessa(bool annetaan_vaihtoehtoinen_kohde, Vector2 vaihtoehtoinen_kohde, bool ei_olla_kohteessa, bool ala_tarkista_ollaanko_kohteessa) {
			//Debug.Log("OnkoKohteessa, ei_olla_kohteessa: " + ei_olla_kohteessa.ToString() + ", ala_tarkista_ollaanko_kohteessa: " + ala_tarkista_ollaanko_kohteessa);
			if(ei_olla_kohteessa | ala_tarkista_ollaanko_kohteessa) return false; //jos on todettu toisaalla
			Vector2 kohde=Vector2.zero;
			float etaisyys=0;
			position=Ohjattava_2D.Convert_to_Vector2(modified_gameobject.gameobject.transform.position);
			//Debug.Log("position: " + position + ", edel_position: " + edel_position);
			Ympyran_ja_Suoran_Leikkauspisteet kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija=new Ympyran_ja_Suoran_Leikkauspisteet();
			if(OnkoAlueella(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, ref kohde, ref etaisyys)) { //jos ollaan alueella
				//Debug.Log("on_saapunut_seuraava_kohteen_alueelle: " + on_saapunut_seuraava_kohteen_alueelle.ToString());
				if(on_saapunut_seuraava_kohteen_alueelle==false) { //jos saavuttiin juuri
					//testataan, mentiinkö saman tien keskustasta yli (eli ollaan menossa kaueuemmaksi keskustasta)
					kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.Ratkaise(edel_position, position, kohde, Vector2.Distance(position, kohde), true);
					//Debug.Log("ratkaisu1: " + kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu1_loytyi + ", ratkaisu2: " + kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu2_loytyi);
					if(kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu1_loytyi & kohteen_saapumis_ja_poistumis_pisteiden_ratkaisija.ratkaisu2_loytyi) //ollaan menty jo tarpeeksi pitkälle -> ollaan kohteessa
						return true;
					else {
						edel_etaisyys_seuraavan_kohteen_keskustaan=etaisyys;
						on_saapunut_seuraava_kohteen_alueelle=true;
						return false;
					}
				} else { //myöhemmin testataan, alkaako etäisyys keskustaan kasvamaan
					//Debug.Log("etaisyys: " + etaisyys + ", edel_etaisyys_seuraavan_kohteen_keskustaan: " + edel_etaisyys_seuraavan_kohteen_keskustaan);
					if(etaisyys>=edel_etaisyys_seuraavan_kohteen_keskustaan) { //ollaan käyty alueella ja etäisyys alkaa kasvaa
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
			//Debug.Log("OnkoAlueella, kohde: " + kohde + ", etaisyys: " + etaisyys + ", liipaisuetaisyys_kohdepositiossa: " + liipaisuetaisyys_kohdepositiossa);
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
		public override bool TarkistaSijainti(Liikuttaja liikuttaja, bool annetaan_vaihtoehtoinen_kohde=false, Vector2 vaihtoehtoinen_kohde=new Vector2(), bool ei_olla_kohteessa=false, bool ala_tarkista_ollaanko_kohteessa=false) {
			//Debug.Log("TarkistaSijainti, annetaan_vaihtoehtoinen_kohde: " + annetaan_vaihtoehtoinen_kohde + ", " + vaihtoehtoinen_kohde);
			VarmistaSeuraavaKohdeOikeaksi(annetaan_vaihtoehtoinen_kohde);
            if (ajastettu_kaantyminen_asetettu == false)
            {
                //Debug.Log("Ajastettu kaantyminen: ei asetettu");
                if (OnkoKohteessa(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, ei_olla_kohteessa, ala_tarkista_ollaanko_kohteessa))
                {
                    Debug.Log("On kohteessa");
                    if (!annetaan_vaihtoehtoinen_kohde)
                    {
                        //asetetaan seuraava kohde
                        if (ajastettu_kaantyminen_asetettu == false)
                        {
                            bool kaannytaan = false;
                            if (palataan)
                            {
                                if (seuraava_kohde_Node.Equals(kohteet.First))
                                {
                                    kaannytaan = true;
                                    palataan = false;
                                }
                            }
                            else if (seuraava_kohde_Node.Equals(kohteet.Last))
                            {
                                kaannytaan = true;
                                palataan = true;
                            }
                            Debug.Log("kaannytaan: " + kaannytaan.ToString());
                            if (kaannytaan)
                            { //asetetaan seuraava kohde viiveellä
                                ajastettu_kaantyminen_asetettu = true;
                                seuraavan_kohteen_asettamisaika = Time.time + odotusviive_kaannyttaessa;
                            }
                            else //välittömästi
                                AsetaSeuraavaKohde();
                        }
                    }
                    return false;
                }
                //if(JaikoPyorimaanEesTaas(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde)) //testataan, ollaanko jumissa
                //	return false;
                //Debug.Log("Jai jumimaan!!!!!!!");
                return true;
            }
            else if (Time.time > seuraavan_kohteen_asettamisaika)//ajastettu kääntyminen
                AsetaSeuraavaKohde();
            else
                Debug.Log("Ajastettu kääntyminen peruttu, Time.time: " + Time.time + ", seuraavan_kohteen_asettamisaika: " + seuraavan_kohteen_asettamisaika);
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
			Debug.Log("AsetaSeuraavaKohde: " + seuraava_kohde_Node.Value);
		}
		
		public override Vector2 AnnaSeuraavaKohde() {
			return seuraava_kohde_Node.Value;
		}
		
		public void SiirraSeuraavaKohde(Vector2 koordinaatit) {
			//seuraava_kohde_Node.Value.Set(koordinaatit.x, koordinaatit.y);
			seuraava_kohde_Node.Value=new Vector2(koordinaatit.x, koordinaatit.y);
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
		public override bool TarkistaSijainti(Liikuttaja liikuttaja, bool annetaan_vaihtoehtoinen_kohde=false, Vector2 vaihtoehtoinen_kohde=new Vector2(), bool ei_olla_kohteessa=false, bool ala_tarkista_ollaanko_kohteessa=false) {
			VarmistaSeuraavaKohdeOikeaksi(annetaan_vaihtoehtoinen_kohde);
			if(annetaan_vaihtoehtoinen_kohde)
				AsetaSeuraavaKohde(vaihtoehtoinen_kohde);
			else
				AsetaSeuraavaKohde();
			if(OnkoKohteessa(annetaan_vaihtoehtoinen_kohde, vaihtoehtoinen_kohde, ei_olla_kohteessa, ala_tarkista_ollaanko_kohteessa)) {
				//varmistetaan, että ukko on kohti targettia
				if(liikuttaja!=null)
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
			//Debug.Log("PaivitaOhjauksienTilat");
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
				//Debug.Log("tavoite_suunta: " + tavoite_suunta + ", suunnan_muutos_tarve: " + suunnan_muutos_tarve);
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
				//Debug.Log("PaivitaTila, suuntavektori: " + suuntavektori + ", ohjailusuunta: " + ohjailusuunta + ", pistetulo: " + pistetulo);
				//Debug.Log("ohjailusuunta_x: " + ohjailusuunta.x);
				if(pistetulo>0) { //ohjataan positiiviseen suuntaan
					//Debug.Log("Positiivinen");
					positiivinen_ohjausrajapinta.AsetaUusiTila(true);
					negatiivinen_ohjausrajapinta.AsetaUusiTila(false);
				} else if(pistetulo<0) { //ohjataan negatiiviseen suuntaan
					//Debug.Log("Negatiivinen");
					positiivinen_ohjausrajapinta.AsetaUusiTila(false);
					negatiivinen_ohjausrajapinta.AsetaUusiTila(true);
				} else { //kohtisuorassa -> ei ohjata ollenkaan tällä komponentilla
					//Debug.Log("ei ohjata");
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
	public class LiikkuvaHissiHallinta_Parametrit_Hallintapaneeliin {
		public float liipaisuetaisyys_kohdepositiossa=2;
		public float jumiutuneeksi_rajatun_alueen_sade=1;
		public bool lisaa_targetin_havaitsija;
		public TargetinHavaitsija_Parametrit_Hallintapaneeliin targetin_havaitsija;

		[System.Serializable]
		public class ToimintaMoodiValinnat {
			public LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin liikekohteiden_maarittaja;
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
				tekoaly.perusresurssit.LisaaTargetinHavaitsija(new TargetinHavaitsija_Hallintapaneelista_Rakentaja().Rakenna(parametrit.targetin_havaitsija, tekoaly.perusresurssit));
			if(parametrit.lisaa_esteen_havaitsija)
				tekoaly.LisaaEsteenHavaitsija(new EsteenHavaitsija_Hallintapaneelista_Rakentaja().Rakenna(parametrit.esteen_havaitsija, tekoaly.perusresurssit.this_Modified_GameObject, peliukkelin_liikkeen_hallitsija, hallittavat_ohjauskomponentit, parametrit.liipaisuetaisyys_kohdepositiossa, parametrit.jumiutuneeksi_rajatun_alueen_sade));
			ToimintaMoodi moodi;
			ToimintaMoodi_for_Artificial_Intelligence_Handling_Hallintapaneelista_Rakentaja moodi_rakentaja=new ToimintaMoodi_for_Artificial_Intelligence_Handling_Hallintapaneelista_Rakentaja();
			parametrit.toimintamoodi_valinnat.ForEach(delegate(Artificial_Intelligence_Handling_Parametrit_Hallintapaneeliin.ToimintaMoodiValinnat obj) {
				moodi=moodi_rakentaja.Rakenna(obj.asetukset, tekoaly.perusresurssit.this_Modified_GameObject, peliukkelin_liikkeen_hallitsija, hallittavat_ohjauskomponentit, ampumis_nappula, parametrit.liipaisuetaisyys_kohdepositiossa, parametrit.jumiutuneeksi_rajatun_alueen_sade);
				tekoaly.perusresurssit.LisaaToimintaMoodi(moodi, obj.ei_havaintoa, obj.havainto_tehty, obj.nakee, obj.nakee_takana);
			});
			return tekoaly;
		}
    }
	public class LiikkuvaHissiHallinta_Hallintapaneelista_Rakentaja {
		public LiikkuvaHissiHallinta Rakenna(LiikkuvaHissiHallinta_Parametrit_Hallintapaneeliin parametrit, GameObject gameobject) {
			LiikkuvaHissiHallinta hissi=new LiikkuvaHissiHallinta(gameobject);
			if(parametrit.lisaa_targetin_havaitsija)
				hissi.perusresurssit.LisaaTargetinHavaitsija(new TargetinHavaitsija_Hallintapaneelista_Rakentaja().Rakenna(parametrit.targetin_havaitsija, hissi.perusresurssit));
			ToimintaMoodi moodi;
			ToimintaMoodi_for_LiikkuvaHissiHallinta_Hallintapaneelista_Rakentaja moodi_rakentaja=new ToimintaMoodi_for_LiikkuvaHissiHallinta_Hallintapaneelista_Rakentaja();
			parametrit.toimintamoodi_valinnat.ForEach(delegate(LiikkuvaHissiHallinta_Parametrit_Hallintapaneeliin.ToimintaMoodiValinnat obj) {
				moodi=moodi_rakentaja.Rakenna(obj.liikekohteiden_maarittaja, hissi.perusresurssit.this_Modified_GameObject, parametrit.liipaisuetaisyys_kohdepositiossa, parametrit.jumiutuneeksi_rajatun_alueen_sade);
				hissi.perusresurssit.LisaaToimintaMoodi(moodi, obj.ei_havaintoa, obj.havainto_tehty, obj.nakee, obj.nakee_takana);
			});
			return hissi;
		}
	}
    public class TargetinHavaitsija_Hallintapaneelista_Rakentaja {
		public Artificial_Intelligence_Handling.TargetinHavaitsija Rakenna(TargetinHavaitsija_Parametrit_Hallintapaneeliin parametrit, Artificial_Intelligence_Handling.PerusResurssit p_this_PerusResurssit) {
			return new Artificial_Intelligence_Handling.TargetinHavaitsija(p_this_PerusResurssit, parametrit.max_distance_to_target, parametrit.nakokentta_asteina, parametrit.layerMask.value, parametrit.hakuvali_kun_ei_targettia, parametrit.hakuvali_kun_on_target, parametrit.nakoyhteyden_testausvali);
		}
    }
    public class EsteenHavaitsija_Hallintapaneelista_Rakentaja {
		public Artificial_Intelligence_Handling.EsteenHavaitsija Rakenna(EsteenHavaitsija_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject this_Modified_GameObject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> hallittavat_ohjauskomponentit, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			return new Artificial_Intelligence_Handling.EsteenHavaitsija(liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade, parametrit.havaitsijan_etaisyys_ukkelista, parametrit.havaitsija_viiksen_pituus, parametrit.aukon_minimipituus, (uint)parametrit.reunan_maksimi_kulmaheitto, parametrit.esteeseen_reagointietaisyys, parametrit.esteeseen_reagointiaika, (uint)parametrit.seinan_kaltevuusraja, parametrit.ukkelin_pituus, (uint)parametrit.etsinta_alueen_sade, hallittavat_ohjauskomponentit, parametrit.layerMask.value, parametrit.ei_saa_osua_tagit, this_Modified_GameObject, peliukkelin_liikkeen_hallitsija);
		}
    }
	public class ToimintaMoodi_for_Artificial_Intelligence_Handling_Hallintapaneelista_Rakentaja {
		public ToimintaMoodi Rakenna(ToimintaMoodi_Parametrit_Hallintapaneeliin parametrit, Prefab_and_instances.Modified_GameObject modified_gameobject, Peliukkeli_ohjaus.Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija, List<Peliukkeli_ohjaus.Hallittava_OhjausKomponentti> hallittavat_ohjauskomponentit, Menu.Button_elem ampumis_nappula, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			Liikuttaja liikuttelija=new Liikuttaja_Hallintapaneelista_Rakentaja().Rakenna(parametrit.liikuttaja, modified_gameobject, peliukkelin_liikkeen_hallitsija, hallittavat_ohjauskomponentit);
			AseenKayttaja aseen_kayttaja=null;
			if(parametrit.lisaa_ampuja)
				aseen_kayttaja=new AseenKayttaja_Hallintapaneelista_Rakentaja().Rakenna(parametrit.ampuja, liikuttelija, ampumis_nappula);
			return new ToimintaMoodi(new LiikeKohteidenMaarittaja_Hallintapaneelista_Rakentaja().Rakenna(parametrit.liikekohteiden_maarittaja, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade), liikuttelija, aseen_kayttaja);
		}
    }
	public class ToimintaMoodi_for_LiikkuvaHissiHallinta_Hallintapaneelista_Rakentaja {
		public ToimintaMoodi Rakenna(LiikeKohteidenMaarittaja_Parametrit_Hallintapaneeliin liikekohteet, Prefab_and_instances.Modified_GameObject modified_gameobject, float liipaisuetaisyys_kohdepositiossa, float jumiutuneeksi_rajatun_alueen_sade) {
			return new ToimintaMoodi(new LiikeKohteidenMaarittaja_Hallintapaneelista_Rakentaja().Rakenna(liikekohteet, modified_gameobject, liipaisuetaisyys_kohdepositiossa, jumiutuneeksi_rajatun_alueen_sade), null, null);
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