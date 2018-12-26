using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ohjaus_2D;
using Ohjaus_laite;
using Menu;
using Target_ohjaus;

//peliukkelin ohjaus gravitaatioon perustuen
//ohjaus rigidbody:lla tai characterController:lla
//ohjaus tapahtuu ohjauslaitteelta. myös automaattisia (itsenäisiä) ohjauskomponentteja
namespace Peliukkeli_ohjaus {
	public class Peliukkeli_ohjaus : Ohjauslaitteen_Kayttaja {
		public Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija;
		public Hallittava_OhjausKomponentti x_suuntainen_ohjauskomponentti;
		public Animator_ParametersHandling animator_parametershandling;
		private float start_time_not_grounded=0; //mittaa aikaa siitä hetkestä, kun ukkeli irrottautuu maasta

		//parent tekee tarkistukset ohjauslaitteelle
		public Peliukkeli_ohjaus(Ohjaus_laite.Ohjaus_laite p_ohjaus_laite, Peliukkelin_Ohjausvoiman_Hallitsija p_ohjausvoiman_hallitsija, Animator p_animator, Target_ohjaus.ForDestroyingInstantiated p_hyppyaani) :base(p_ohjaus_laite) {
			ohjausvoiman_hallitsija=p_ohjausvoiman_hallitsija;
			x_suuntainen_ohjauskomponentti=ohjausvoiman_hallitsija.hallittavat_ohjaus_komponentit.Find(delegate(Hallittava_OhjausKomponentti obj) {
				return obj.suunta_parametrit.suunta_komponentti==VoimaVektori.SuuntaParametrit.suunta_x;
			});
			animator_parametershandling=new Animator_ParametersHandling(p_animator, p_hyppyaani);
		}

		public override string ToString ()
		{
			return string.Format ("[Peliukkeli_ohjaus]");
		}

		//laskee ohjausvoimat
		//ohjausnappuloiden tilat on tunnistettu aiemmin
		public override void SuoritaToiminnot() {
			ohjausvoiman_hallitsija.LaskeHallittavatOhjausVoimat(); //laskee (summaa) ohjauslaitteella hallittavat ohjausvoimat
			MaaritaAnimatorParametrit_ja_AsetaNe();
		}

		//suoritetaan FixedUpdate:ssa
		public void Laske_ja_AsetaOhjausvoima(float delta_time) {
			ohjausvoiman_hallitsija.delta_time=delta_time;
			ohjausvoiman_hallitsija.Laske_ja_AsetaOhjausvoima();
		}

		//suoritetaan ukkelin ohjauksen (hallittavat ohjauskomponentit) jälkeen
		public void MaaritaAnimatorParametrit_ja_AsetaNe() {
			VoimaVektori.SuuntaParametrit suunta_parametrit=new VoimaVektori.SuuntaParametrit(VoimaVektori.SuuntaParametrit.suunta_x, true);
			if(!ohjausvoiman_hallitsija.peliukkelin_liikkeen_hallitsija.IsGrounded2()) {
				if((Time.time-start_time_not_grounded)>0.1f) //asetetaan vasta, kun ukkeli on ollut riittävän kauan irti maasta
					animator_parametershandling.Direction=3; //hyppyanimaatio
			} else {
				start_time_not_grounded=Time.time;
				if(!(ohjausvoiman_hallitsija.OnkoNopeuttaJompaankumpaanOhjausSuuntaan(suunta_parametrit) && x_suuntainen_ohjauskomponentti.ohjaus_elementti.ohjataanko))
					animator_parametershandling.Direction=0; //paikallaan animaatio
				else
					animator_parametershandling.Direction=1; //liikkuu animaatio
			}
			animator_parametershandling.AsetaParametrit();
		}
	}

	public class Peliukkelin_Ohjausvoiman_Hallitsija {
		public Peliukkelin_Liikkeen_Hallitsija peliukkelin_liikkeen_hallitsija; //liikuttaa ukkelia ohjausvoimaan ja fysiikan lakeihin perustuen
		public List<Hallittava_OhjausKomponentti> hallittavat_ohjaus_komponentit; //nappuloista hallittavat
		public List<Automaattinen_OhjausKomponentti> automaattiset_ohjaus_komponentit; //automaattisesti (itsenäisesti) toimivat ohjauskomponentit
		public VoimaVektori hallittava_ohjausvoima=new VoimaVektori(); //hallittavien voimien summa
		public VoimaVektori kokonais_ohjausvoima=new VoimaVektori(); //kaikkien voimien summa
		public VakioVoimaKomponentti putoamiskiihtyvyys=null;
		public float delta_time;
		private string edel_alustan_tag; //edellisen kosketetun alustan tag (vaikuttaa ohjauksen parametreihin)
		public List<OhjausKomponentti.AlustanMukaanMukautuvaParametri> ohjaus_komponenttien_parametrit; //muuttuvat sen perusteella, mitä alustaa on viimeeksi koskettu
		Ray ray;
		RaycastHit hit;
		Vector3 ray_suunta=new Vector3(0, -1, 0);


		public Peliukkelin_Ohjausvoiman_Hallitsija(Peliukkelin_Liikkeen_Hallitsija p_peliukkelin_liikkeen_hallitsija) {
			peliukkelin_liikkeen_hallitsija=p_peliukkelin_liikkeen_hallitsija;
			hallittavat_ohjaus_komponentit=new List<Hallittava_OhjausKomponentti>();
			automaattiset_ohjaus_komponentit=new List<Automaattinen_OhjausKomponentti>();
			ohjaus_komponenttien_parametrit=new List<OhjausKomponentti.AlustanMukaanMukautuvaParametri>();
			putoamiskiihtyvyys=new VakioVoimaKomponentti(new VoimaVektori.SuuntaParametrit(VoimaVektori.SuuntaParametrit.suunta_y, true), Physics.gravity.y);
		}

		//parametrien hallinta alustan mukaan
		public OhjausKomponentti.AlustanMukaanMukautuvaParametri Lisaa_AlustanMukaanMukautuvaParametri(float oletus_arvo, List<OhjausKomponentti.AlustanMukainenParametri> alustan_mukaiset_arvot) {
			OhjausKomponentti.AlustanMukaanMukautuvaParametri item=new OhjausKomponentti.AlustanMukaanMukautuvaParametri(oletus_arvo, alustan_mukaiset_arvot);
			ohjaus_komponenttien_parametrit.Add(item);
			return item;
		}
		public void Paivita_AlustanMukaanMukautuvatParametrit() {
			ray=new Ray(peliukkelin_liikkeen_hallitsija.transform.position, ray_suunta);
			if(Physics.Raycast(ray, out hit, 2)) { //jos osuu johonkin -> tutkitaan osuma
				if(hit.collider.tag.CompareTo(edel_alustan_tag)!=0) { //jos alusta vaihtuu, päivitetään parametrit
					//Debug.Log("alustan paivitys: " + hit.collider.tag);
					ohjaus_komponenttien_parametrit.ForEach(delegate(OhjausKomponentti.AlustanMukaanMukautuvaParametri obj) {
						obj.PaivitaNykyinenArvo(hit.collider.tag);
					});
					edel_alustan_tag=hit.collider.tag;
					AsetaKaikkiOhjauskomponentit_Aktiiviseksi(); //asetetaan ohjauskomponentit aktiiviseksi, jotta uusien parametrien vaikutukset tarkistetaan
				}
			}
		}

		//asettaa kaikki hallittavat ohjauskomponentit aktiiviseksi eli ohjausvoima kytkeytyy päälle, jos nappi on painettuna
		public void AsetaKaikkiOhjauskomponentit_Aktiiviseksi() {
			hallittavat_ohjaus_komponentit.ForEach(delegate(Hallittava_OhjausKomponentti obj) {
				obj.ohjaus_elementti.ohjaus_kytketty=true;
			});
		}

		//summaa hallittavat ohjausvoimat ja updatessa suoritettavat automaattiset ohjauskomponentit
		//suoritettava OnGUI:ssa tai updatessa riippuen ohjausnappulan toteutustavasta
		public void LaskeHallittavatOhjausVoimat() {
			if(peliukkelin_liikkeen_hallitsija.IsGrounded2()) {
				Paivita_AlustanMukaanMukautuvatParametrit();
			}

			hallittava_ohjausvoima.Nollaa();
			hallittavat_ohjaus_komponentit.ForEach(delegate(Hallittava_OhjausKomponentti obj) {
				//tarkistetaan ensin, onko törmäyskontakti loppunut -> aktivoidaan ohjaus
				if(peliukkelin_liikkeen_hallitsija.LoppuikoTormays(obj.suunta_parametrit))
					obj.ohjaus_elementti.ohjaus_kytketty=true;
				obj.Saada_ohjausvoimaa();
			});
			hallittava_ohjausvoima.Selvita_OnkoVoimaa(); //ei huomioida mahdollista putoamiskiihtyvyyttä
			//lisätään putoamiskiihtyvyys
			//if(peliukkelin_liikkeen_hallitsija.lisaa_putoamiskiihtyvyys_manuaalisesti_ohjaukseen) {
				if(hallittava_ohjausvoima.OnkoVoimaaJompaankumpaanSuuntaan(putoamiskiihtyvyys.suunta_parametrit))
					LisaaPutoamiskiihtyvyys();
				else if(!peliukkelin_liikkeen_hallitsija.IsGrounded2())	
					LisaaPutoamiskiihtyvyys();
			//}
			//asetetaan ukkeli menosuuntaan
			//Debug.Log("hallittava_ohjausvoima: " + hallittava_ohjausvoima.Convert_to_Vector2());
			AsetaUkkeliMenosuuntaan();
		}

		public void LisaaPutoamiskiihtyvyys() {
			hallittava_ohjausvoima.LisaaVektoriinArvo(putoamiskiihtyvyys.suunta_parametrit, putoamiskiihtyvyys.voima);
		}

		//summaa hallittavien ohjausvoimien lisäksi automaattiset ohjausvoimat
		//ohjaa ohjausvoimien summalla liikkeen hallitsijaa
		//suoritettava FixedUpdate:ssa
		public void Laske_ja_AsetaOhjausvoima() {
			//päivitetään nopeus
			peliukkelin_liikkeen_hallitsija.current_velocity.Nollaa();
			peliukkelin_liikkeen_hallitsija.current_velocity.Aseta(peliukkelin_liikkeen_hallitsija.GetVelocity());
			peliukkelin_liikkeen_hallitsija.current_velocity.Selvita_OnkoVoimaa(); //onko nopeutta
			kokonais_ohjausvoima.Nollaa();
			kokonais_ohjausvoima.Aseta(hallittava_ohjausvoima);  //lähdetään liikkeelle hallittavista ohjausvoimista
			//lisätään automaattiset ohjausvoimat
			automaattiset_ohjaus_komponentit.ForEach(delegate(Automaattinen_OhjausKomponentti obj) {
				obj.Saada_ohjausvoimaa();
			});
			//selvitetään, onko ohjausvoimia
			kokonais_ohjausvoima.Selvita_OnkoVoimaa();
			//ohjataan liikkeen hallitsijaa
			peliukkelin_liikkeen_hallitsija.AsetaOhjausvoima(this);
		}

		public void AsetaUkkeliMenosuuntaan() {
			Vector2 direction=Vector2.up; //alustus
			VoimaVektori.SuuntaParametrit suunta_parametrit=new VoimaVektori.SuuntaParametrit(VoimaVektori.SuuntaParametrit.suunta_x, false); //ensin vasemmalle
			for(int parametri=-1; parametri<2; parametri+=2) { //vasemmalle ja oikealle
				if(peliukkelin_liikkeen_hallitsija.ukkelin_suunta!=parametri) { //selvitetaan kääntämistarve tarvittaessa
					if(AnnaOhjausvoimaOhjausSuuntaan(suunta_parametrit)>0) { //ohjataan suuntaan
						direction=parametri*Vector2.right;
						peliukkelin_liikkeen_hallitsija.KaannaUkkeli(direction);
						peliukkelin_liikkeen_hallitsija.ukkelin_suunta=parametri;
						break;
					}
				}
				suunta_parametrit.positiivinen=true; //oikealle
			}
		}

		//selvittää ohjausvoiman suuntavektorin vaikutussuuntaan
		public float AnnaOhjausvoimaOhjausSuuntaan(VoimaVektori.SuuntaParametrit suunta_parametrit) {
			return hallittava_ohjausvoima.AnnaVektorinKomponentinArvo(suunta_parametrit);
		}
		//onko ollenkaan voimaa ohjaussuuntaan suunta huomioiden
		public bool OnkoOhjausvoimaaOhjausSuuntaan(VoimaVektori.SuuntaParametrit suunta_parametrit) {
			return hallittava_ohjausvoima.OnkoVoimaaSuuntaan(suunta_parametrit);
		}
		//onko ollenkaan voimaa ohjaussuuntaan
		public bool OnkoOhjausvoimaaJompaankumpaanOhjausSuuntaan(VoimaVektori.SuuntaParametrit suunta_parametrit) {
			return hallittava_ohjausvoima.OnkoVoimaaJompaankumpaanSuuntaan(suunta_parametrit);
		}
		
		//selvittää nopeuden suuntavektorin vaikutussuuntaan
		public float AnnaNopeusOhjausSuuntaan(VoimaVektori.SuuntaParametrit suunta_parametrit) {
			return peliukkelin_liikkeen_hallitsija.current_velocity.AnnaVektorinKomponentinArvo(suunta_parametrit);
		}
		//onko ollenkaan nopeutta vaikutussuuntaan suunta huomioiden
		public bool OnkoNopeuttaOhjausSuuntaan(VoimaVektori.SuuntaParametrit suunta_parametrit) {
			return peliukkelin_liikkeen_hallitsija.current_velocity.OnkoVoimaaSuuntaan(suunta_parametrit);
		}
		//onko ollenkaan nopeutta vaikutussuuntaan
		public bool OnkoNopeuttaJompaankumpaanOhjausSuuntaan(VoimaVektori.SuuntaParametrit suunta_parametrit) {
			return peliukkelin_liikkeen_hallitsija.current_velocity.OnkoVoimaaJompaankumpaanSuuntaan(suunta_parametrit);
		}
	}

	//voimavektori: esittää 2-uloitteisia vektoreita 2-alkioisena tauluna
	public class VoimaVektori {
		public float[] voima=new float[2];
		public bool[] on_voimaa=new bool[2];
		public bool[] voima_on_positiivinen=new bool[2];

		public VoimaVektori() {
			Nollaa();
		}
		public VoimaVektori(float x, float y) {
			voima[0]=x;
			voima[1]=y;
		}
		public VoimaVektori(Vector2 voimavektori) {
			voima[0]=voimavektori.x;
			voima[1]=voimavektori.y;
		}

		public void Nollaa() {
			voima[0]=0;
			voima[1]=0;
			on_voimaa[SuuntaParametrit.suunta_x]=false;
			on_voimaa[SuuntaParametrit.suunta_y]=false;
		}

		//ensin pitää kutsua Nollaa()
		public void Selvita_OnkoVoimaa() {
			if(voima[SuuntaParametrit.suunta_x]!=0) {
				on_voimaa[SuuntaParametrit.suunta_x]=true;
				if(voima[SuuntaParametrit.suunta_x]>0)
					voima_on_positiivinen[SuuntaParametrit.suunta_x]=true;
				else
					voima_on_positiivinen[SuuntaParametrit.suunta_x]=false;
			}
			if(voima[SuuntaParametrit.suunta_y]!=0) {
				on_voimaa[SuuntaParametrit.suunta_y]=true;
				if(voima[SuuntaParametrit.suunta_y]>0)
					voima_on_positiivinen[SuuntaParametrit.suunta_y]=true;
				else
					voima_on_positiivinen[SuuntaParametrit.suunta_y]=false;
			}
		}

		public Vector2 Convert_to_Vector2() {
			return new Vector2(voima[SuuntaParametrit.suunta_x], voima[SuuntaParametrit.suunta_y]);
		}
		public Vector2 Convert_to_Vector3() {
			return new Vector3(voima[SuuntaParametrit.suunta_x], voima[SuuntaParametrit.suunta_y], 0);
		}
		public void Aseta(Vector2 vektori) {
			voima[SuuntaParametrit.suunta_x]=vektori.x;
			voima[SuuntaParametrit.suunta_y]=vektori.y;
		}
		public void Aseta(Vector3 vektori) {
			voima[SuuntaParametrit.suunta_x]=vektori.x;
			voima[SuuntaParametrit.suunta_y]=vektori.y;
		}
		public void Aseta(VoimaVektori vektori) {
			voima[SuuntaParametrit.suunta_x]=vektori.voima[SuuntaParametrit.suunta_x];
			voima[SuuntaParametrit.suunta_y]=vektori.voima[SuuntaParametrit.suunta_y];
		}
		public void Aseta(float arvo, uint suunta) {
			voima[suunta]=arvo;
		}

		//voimavektorin suuntaparametrit
		public class SuuntaParametrit {
			public uint suunta_komponentti; //x vai y
			public bool positiivinen; //voiman suunta
			public const uint suunta_x=0;
			public const uint suunta_y=1;

			public SuuntaParametrit(uint p_suunta_komponentti, bool p_positiivinen) {
				suunta_komponentti=p_suunta_komponentti;
				positiivinen=p_positiivinen;
			}
		}

		public void AsetaVektoriinArvo(SuuntaParametrit suunta_parametrit, float arvo) {
			this.voima[suunta_parametrit.suunta_komponentti]=arvo;
		}
		public void LisaaVektoriinArvo(SuuntaParametrit suunta_parametrit, float arvo) {
			if(suunta_parametrit.positiivinen) //positiivinen voimakomponentti
				this.voima[suunta_parametrit.suunta_komponentti]+=arvo;
			else //negatiivinen voimakomponentti
				this.voima[suunta_parametrit.suunta_komponentti]-=arvo;
		}
		public float AnnaVektorinKomponentinArvo(SuuntaParametrit suunta_parametrit) {
			if(suunta_parametrit.positiivinen) //positiivinen voimakomponentti
				return this.voima[suunta_parametrit.suunta_komponentti];
			else
				return -this.voima[suunta_parametrit.suunta_komponentti];
		}
		public bool OnkoVoimaaSuuntaan(SuuntaParametrit suunta_parametrit) {
			if(on_voimaa[suunta_parametrit.suunta_komponentti])
				if(voima_on_positiivinen[suunta_parametrit.suunta_komponentti]==suunta_parametrit.positiivinen)
					return true;
			return false;
		}
		public bool OnkoVoimaaJompaankumpaanSuuntaan(SuuntaParametrit suunta_parametrit) {
			if(on_voimaa[suunta_parametrit.suunta_komponentti])
				return true;
			return false;
		}
	}

	//hallitsee ukkelin liikettä
	//liikettä voidaan hallita joko rigidbody:llä tai characterControllerilla
	public abstract class Peliukkelin_Liikkeen_Hallitsija {
		public Transform transform;
		public int ukkelin_suunta=0; //asetettu suunta: 1: oikealle, -1: vasemmalle, 0:ei tiedossa (alkuarvo)
		public VoimaVektori current_velocity=new VoimaVektori();
		public bool lisaa_putoamiskiihtyvyys_manuaalisesti_ohjaukseen=false; //ohjausvoiman hallitsijan on itse lisättävä putoamiskiihtyvyys lausekkeisiin manuaalisesti
		public bool[] tormays_loppui=new bool[2]; //kun törmäyskontakti loppuu, ohjausvoimanhallitsija aktivoi ohjauksen

		public Peliukkelin_Liikkeen_Hallitsija(Transform p_transform) {
			transform=p_transform;
			tormays_loppui[VoimaVektori.SuuntaParametrit.suunta_x]=false;
			tormays_loppui[VoimaVektori.SuuntaParametrit.suunta_y]=false;
		}

		public abstract void AsetaOhjausvoima(Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija);
		public abstract Vector2 GetVelocity();
		public abstract float GetMass();
		public abstract bool IsGrounded();
		public abstract bool IsGrounded2();

		//kääntää ukkelin haluttuun suuntaan
		public void KaannaUkkeli(Vector2 direction) {
			transform.rotation=Quaternion.FromToRotation(Vector3.right, direction);
		}

		//ohjausvoimien hallitsija tarkistaa, onko törmäyskontakti loppunut -> aktivoi ohjauksen
		public bool LoppuikoTormays(VoimaVektori.SuuntaParametrit suuntaparametrit) {
			if(tormays_loppui[suuntaparametrit.suunta_komponentti]) {
				tormays_loppui[suuntaparametrit.suunta_komponentti]=false;
				return true;
			}
			return false;
		}
	}
	//ohjaus rigidbody:lla
	//pitää toteuttaa jokin menetelmä, millä saa tiedon törmäyskontaktin päättymisestä -> tai siis milloin nopeus voi taas alkaa kasvaa
	public class Peliukkelin_Liikkeen_Hallitsija_Rigidbodylla : Peliukkelin_Liikkeen_Hallitsija {
		public Rigidbody rigidbody; //ohjattava rigidbody

		public Peliukkelin_Liikkeen_Hallitsija_Rigidbodylla(Rigidbody p_rigidbody) :base(p_rigidbody.transform) {
			rigidbody=p_rigidbody;
		}

		//suoritettava FixedUpdate:ssa
		public override void AsetaOhjausvoima(Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) {
			rigidbody.AddForce(ohjausvoiman_hallitsija.kokonais_ohjausvoima.Convert_to_Vector3()); //voiman asetus peliobjektiin
		}
		
		public override float GetMass() {
			return rigidbody.mass;
		}

		public override Vector2 GetVelocity() {
			return Ohjattava_2D.Convert_to_Vector2(rigidbody.velocity);
		}

		public override bool IsGrounded() {
			//rigidbody ei anna tietoa suoraan, joten pitää toteuttaa erikseen
			//annetaan tila aina grounded
			return true;
		}
		public override bool IsGrounded2() {
			return true;
		}
	}
	//ohjaus characterController:lla (massa=1)
	public class Peliukkelin_Liikkeen_Hallitsija_CharacterControllerilla : Peliukkelin_Liikkeen_Hallitsija {
		public CharacterController character_controller; //hallittava character_controller
		public bool tormays_x=false;
		public bool tormays_y=false;
		public VoimaVektori motion=new VoimaVektori();
		public VoimaVektori.SuuntaParametrit[] suunta_parametrit; //suuntaparametrit x- ja y-suunnassa


		public Peliukkelin_Liikkeen_Hallitsija_CharacterControllerilla(CharacterController p_character_controller) :base(p_character_controller.transform) {
			character_controller=p_character_controller;
			lisaa_putoamiskiihtyvyys_manuaalisesti_ohjaukseen=true; //ohjausvoiman hallitsijan on itse lisättävä putoamiskiihtyvyys lausekkeisiin manuaalisesti
			suunta_parametrit=new VoimaVektori.SuuntaParametrit[2];
			suunta_parametrit[VoimaVektori.SuuntaParametrit.suunta_x]=new VoimaVektori.SuuntaParametrit(VoimaVektori.SuuntaParametrit.suunta_x, true);
			suunta_parametrit[VoimaVektori.SuuntaParametrit.suunta_y]=new VoimaVektori.SuuntaParametrit(VoimaVektori.SuuntaParametrit.suunta_y, true);
		}

		//voidaan suorittaa updatessa, mutta suositellaan fixedUpdate:a
		public override void AsetaOhjausvoima(Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) {
			//lasketaan siirtymä tasaisesti muuttuvan kiihtyvyyden kaavalla. jätetään kiihtyvyyskomponentista pois vakiolla 0,5 kertominen, niin vastaa todellisuutta (unity ilmeisesti haluaa sen siinä muodossa)
			//nopeuskomponentti
			if(current_velocity.on_voimaa[VoimaVektori.SuuntaParametrit.suunta_x]) //jos on nopeutta
				motion.Aseta(character_controller.velocity.x*ohjausvoiman_hallitsija.delta_time, VoimaVektori.SuuntaParametrit.suunta_x);
			else
				motion.Aseta(0, VoimaVektori.SuuntaParametrit.suunta_x);
			if(current_velocity.on_voimaa[VoimaVektori.SuuntaParametrit.suunta_y]) //jos on nopeutta
				motion.Aseta(character_controller.velocity.y*ohjausvoiman_hallitsija.delta_time, VoimaVektori.SuuntaParametrit.suunta_y);
			else
				motion.Aseta(0, VoimaVektori.SuuntaParametrit.suunta_y);
			//lisätään muut tekijät ohjaussuunnittain
			//kiihdytyskomponentti
			//Debug.Log("ohjausvoima: " + ohjausvoiman_hallitsija.kokonais_ohjausvoima.voima[VoimaVektori.SuuntaParametrit.suunta_x]);
			LisaaKiihdytysTekija(ohjausvoiman_hallitsija, suunta_parametrit[VoimaVektori.SuuntaParametrit.suunta_x]);
			LisaaKiihdytysTekija(ohjausvoiman_hallitsija, suunta_parametrit[VoimaVektori.SuuntaParametrit.suunta_y]);
			//jos ollaan törmätty johonkin, jätetään puskematta
			LisaaTormayslippujenVaikutus_Suunnassa_x(suunta_parametrit[VoimaVektori.SuuntaParametrit.suunta_x]);
			LisaaTormayslippujenVaikutus_Suunnassa_y(suunta_parametrit[VoimaVektori.SuuntaParametrit.suunta_y]);
			character_controller.Move(motion.Convert_to_Vector3()); //liikutetaan ukkelia
		}

		public void LisaaKiihdytysTekija(Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija, VoimaVektori.SuuntaParametrit suuntaparametrit) {
			if(ohjausvoiman_hallitsija.kokonais_ohjausvoima.on_voimaa[suuntaparametrit.suunta_komponentti]) //jos on ohjausvoimaa
				motion.LisaaVektoriinArvo(suuntaparametrit, ohjausvoiman_hallitsija.kokonais_ohjausvoima.voima[suuntaparametrit.suunta_komponentti]*Mathf.Pow(ohjausvoiman_hallitsija.delta_time,2));
		}

		//pysäyttää liikkeen x-suunnassa, jos törmää johonkin
		public void LisaaTormayslippujenVaikutus_Suunnassa_x(VoimaVektori.SuuntaParametrit suuntaparametrit) {
			if ((character_controller.collisionFlags & CollisionFlags.Sides) == CollisionFlags.Sides) {
				if(character_controller.velocity.x>0) { //tultiin vasemmalta
					if(motion.voima[VoimaVektori.SuuntaParametrit.suunta_x]>0) {
						motion.AsetaVektoriinArvo(suuntaparametrit, 0);
						tormays_x=true;
						return;
					}
				} else if(character_controller.velocity.x<0) { //tultiin oikealta
					if(motion.voima[VoimaVektori.SuuntaParametrit.suunta_x]<0) {
						motion.AsetaVektoriinArvo(suuntaparametrit, 0);
						tormays_x=true;
						return;
					}
				}
			} else if(tormays_x) { //törmäys loppuu -> aktivoidaan ohjaus (ohjausvoiman_hallitsija tarkistaa)
				tormays_loppui[VoimaVektori.SuuntaParametrit.suunta_x]=true;
				tormays_x=false;
			}
		}
		//pysäyttää liikkeen y-suunnassa, jos törmää johonkin
		public void LisaaTormayslippujenVaikutus_Suunnassa_y(VoimaVektori.SuuntaParametrit suuntaparametrit) {
			if ((character_controller.collisionFlags & CollisionFlags.Above) == CollisionFlags.Above) {
				if(character_controller.velocity.y>0) { //ylös päin
					if(motion.voima[VoimaVektori.SuuntaParametrit.suunta_y]>0) {
						motion.AsetaVektoriinArvo(suuntaparametrit, 0);
						tormays_y=true;
						return;
					}
				}
			} else if ((character_controller.collisionFlags & CollisionFlags.Below) == CollisionFlags.Below) {
				if(character_controller.velocity.y<0) { //alas päin
					if(motion.voima[VoimaVektori.SuuntaParametrit.suunta_y]<0) {
						motion.AsetaVektoriinArvo(suuntaparametrit, 0);
						tormays_y=true;
						return;
					}
				}
			} else if(tormays_y) { //törmäys loppuu -> aktivoidaan ohjaus (ohjausvoiman_hallitsija tarkistaa)
				tormays_loppui[VoimaVektori.SuuntaParametrit.suunta_y]=true;
				tormays_y=false;
			}
		}
		
		public override float GetMass() {
			return 1f;
		}

		public override Vector2 GetVelocity() {
			return Ohjattava_2D.Convert_to_Vector2(character_controller.velocity);
		}

		public override bool IsGrounded() {
			return character_controller.isGrounded;
		}
		//tätä pitää käyttää fixedUpdaten ulkopuolelta
		public override bool IsGrounded2() {
			if(character_controller!=null)
				return character_controller.isGrounded;
			return false;
		}
	}

	//ohjauskomponentit, joilla ohjausvoima muodostetaan summaamalla eri ohjauskomponentit yhteen
	//ohjauskomponenteilla on suuntavektori, jonka suuntaan ohjauskomponetti vaikuttaa (esitetään suuntaparametreilla
	public abstract class OhjausKomponentti_Vanhin {
		public VoimaVektori.SuuntaParametrit suunta_parametrit;

		public OhjausKomponentti_Vanhin(VoimaVektori.SuuntaParametrit p_suunta_parametrit) {
			suunta_parametrit=p_suunta_parametrit;
		}
		public OhjausKomponentti_Vanhin(Vector2 p_suuntavektori) {
			//suunta_parametrit=new VoimaVektori.SuuntaParametrit
			uint suunta_komponentti=0;
			bool positiivinen=true;
			if(p_suuntavektori.x!=0 & p_suuntavektori.y!=0)
				Debug.LogError("OhjausKomponentti: suuntavektorin on oltava x- tai y-suuntainen!");
			else if(p_suuntavektori.x!=0) {
				suunta_komponentti=VoimaVektori.SuuntaParametrit.suunta_x;
				if(p_suuntavektori.x>0)
					positiivinen=true;
				else
					positiivinen=false;
			} else {
				suunta_komponentti=VoimaVektori.SuuntaParametrit.suunta_y;
				if(p_suuntavektori.y>0)
					positiivinen=true;
				else
					positiivinen=false;
			}
			suunta_parametrit=new VoimaVektori.SuuntaParametrit(suunta_komponentti, positiivinen);
		}
	}
	//vakio voimakomponentti
	public class VakioVoimaKomponentti : OhjausKomponentti_Vanhin {
		public float voima;

		public VakioVoimaKomponentti(VoimaVektori.SuuntaParametrit p_suunta_parametrit, float p_voima) : base(p_suunta_parametrit) {
			voima=p_voima;
		}
	}
	public abstract class OhjausKomponentti : OhjausKomponentti_Vanhin {
		public Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija; //ohjausvoimakomponentti summataan tänne

		public OhjausKomponentti(Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija p_ohjausvoiman_hallitsija) : base(p_suuntavektori) {
			ohjausvoiman_hallitsija=p_ohjausvoiman_hallitsija;
		}

		public bool OnkoSamanSuuntainen_OhjausKomponentti(OhjausKomponentti ohjaus_komponentti, bool huomioi_suunta) {
			bool paluu=true;
			if(this.suunta_parametrit.suunta_komponentti!=ohjaus_komponentti.suunta_parametrit.suunta_komponentti)
				paluu=false;
			if(huomioi_suunta) //huomioidaan + ja -
				if(this.suunta_parametrit.positiivinen!=ohjaus_komponentti.suunta_parametrit.positiivinen)
					paluu=false;
			return paluu;
		}

		public abstract void Saada_ohjausvoimaa(); //muodostaa komponentin voimavektorin

		//voidaan asettaa parametrit alustan mukaan, jota on viimeeksi kosketettu.
		//esim. korkeampi hyppy tietyltä alustalta
		public class AlustanMukainenParametri {
			public string alustan_tag;
			public float parametrin_arvo;

			public AlustanMukainenParametri(string p_alustan_tag, float p_parametrin_arvo) {
				alustan_tag=p_alustan_tag;
				parametrin_arvo=p_parametrin_arvo;
			}
		}
		public class AlustanMukaanMukautuvaParametri {
			public float oletus_arvo; //parametri, kun viimeeksi kosketettu alustaa, jota ei löydy listasta
			public float nykyinen_arvo; //tämän hetkinen arvo
			public List<AlustanMukainenParametri> alustan_mukaiset_arvot;

			public AlustanMukaanMukautuvaParametri(float p_oletus_arvo, List<AlustanMukainenParametri> p_alustan_mukaiset_arvot) {
				oletus_arvo=p_oletus_arvo;
				nykyinen_arvo=oletus_arvo;
				alustan_mukaiset_arvot=p_alustan_mukaiset_arvot;
			}

			public void PaivitaNykyinenArvo(string p_alustan_tag) {
				AlustanMukainenParametri alustan_mukainen_parametri = alustan_mukaiset_arvot.Find(delegate(AlustanMukainenParametri obj) {
					return p_alustan_tag.CompareTo(obj.alustan_tag)==0;
				});
				if(alustan_mukainen_parametri==null)
					nykyinen_arvo=oletus_arvo;
				else
					nykyinen_arvo=alustan_mukainen_parametri.parametrin_arvo;
			}
		}
	}
	//ohjauslaitteella hallittavat voimakomponentit
	public class Hallittava_OhjausKomponentti : OhjausKomponentti {
		public Ohjaus_Elementti ohjaus_elementti;
		public AlustanMukaanMukautuvaParametri positiivinen_ohjausvoima;
		public AlustanMukaanMukautuvaParametri negatiivinen_ohjausvoima;

		public Hallittava_OhjausKomponentti(Button_elem p_positiivinen_ohjaus, Button_elem p_negatiivinen_ohjaus, AlustanMukaanMukautuvaParametri p_positiivinen_ohjausvoima, AlustanMukaanMukautuvaParametri p_negatiivinen_ohjausvoima, Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) :base(p_suuntavektori, ohjausvoiman_hallitsija) {
			ohjaus_elementti=new Ohjaus_Elementti(p_positiivinen_ohjaus, p_negatiivinen_ohjaus);
			positiivinen_ohjausvoima=p_positiivinen_ohjausvoima;
			negatiivinen_ohjausvoima=p_negatiivinen_ohjausvoima;
		}

		public class Ohjaus_Elementti {
			public Button_elem positiivinen_ohjaus;
			public Button_elem negatiivinen_ohjaus;
			public bool ohjataanko=false; //ohjataanko juuri jompaan kumpaan suuntaan
			public bool ohjaus_suunta_positiivinen=false; //jos ohjtaan, ohjataanko positiiviseen
			public bool ohjaus_kytketty=false; //ohjaus voidaan kytkeä pois (rajoitetaan ohjausvoimaa)

			public Ohjaus_Elementti(Button_elem p_positiivinen_ohjaus, Button_elem p_negatiivinen_ohjaus) {
				positiivinen_ohjaus=p_positiivinen_ohjaus;
				negatiivinen_ohjaus=p_negatiivinen_ohjaus;
			}

			//päivittää ohjauksen tilatiedot
			public void PaivitaOhjauksenTila(bool ohjataan_nyt, bool suunta_positiivinen_nyt) {
				if(ohjataan_nyt) {
					if((!ohjataanko) | suunta_positiivinen_nyt!=ohjaus_suunta_positiivinen) { //jos äsken ei ohjattu tai jos suunta vaihtuu
						ohjaus_suunta_positiivinen=suunta_positiivinen_nyt;
						ohjaus_kytketty=true;
					}
					ohjataanko=true;
				} else if(ohjataanko) { //jos äsken ohjattiin, mutta nyt ei ohjata
					ohjaus_kytketty=true;
					ohjataanko=false;
				}
			}
		}

		//summataan ohjausvoimat (positiivinen ja/tai negatiivinen)
		//ohjausnappuloiden kautta ohjattava
		public override void Saada_ohjausvoimaa() {
			if(Ohjataanko(ohjaus_elementti.positiivinen_ohjaus)) {
				Aseta_Positiivinen_Ohjausvoima();
			} else if(Ohjataanko(ohjaus_elementti.negatiivinen_ohjaus)) {
				Aseta_Negatiivinen_Ohjausvoima();
			} else {
				ohjaus_elementti.PaivitaOhjauksenTila(false, false);
			}
		}
		//ulkoinen laite esim. tekoäly voi kytkeä ohjausvoiman näistä
		public void Aseta_Positiivinen_Ohjausvoima() {
			ohjaus_elementti.PaivitaOhjauksenTila(true, true);
			if(ohjaus_elementti.ohjaus_kytketty) //jos ohjausvoima on kytkettynä
				ohjausvoiman_hallitsija.hallittava_ohjausvoima.AsetaVektoriinArvo(suunta_parametrit, positiivinen_ohjausvoima.nykyinen_arvo);
		}
		public void Aseta_Negatiivinen_Ohjausvoima() {
			ohjaus_elementti.PaivitaOhjauksenTila(true, false);
			if(ohjaus_elementti.ohjaus_kytketty) //jos ohjausvoima on kytkettynä
				ohjausvoiman_hallitsija.hallittava_ohjausvoima.AsetaVektoriinArvo(suunta_parametrit, -negatiivinen_ohjausvoima.nykyinen_arvo);
		}

		public bool Ohjataanko(Button_elem ohjaus) {
			if(ohjaus!=null) {
				if(ohjaus.tila==true)
					return true;
			}
			return false;
		}

		//automaattiset ohjauselementit kytkevät tästä ohjausvoiman pois, kun sitä pitää rajoittaa
		public void KytkeOhjausPois() {
			//Debug.Log("kytke ohjaus pois");
			ohjaus_elementti.ohjaus_kytketty=false;
			ohjausvoiman_hallitsija.hallittava_ohjausvoima.Aseta(0, suunta_parametrit.suunta_komponentti);
		}
	}
	//automaattiset (itsenäiset) ohjauskomponentit
	//eri komponentit on toteutettu siinä järjestyksessä, kuin niitä pitää käyttää
	public abstract class Automaattinen_OhjausKomponentti : OhjausKomponentti {
		public Hallittava_OhjausKomponentti hallittava_ohjauskomponentti=null; //ohjauskomponetti ja ohjauselementti, jonka tilaan voidaan vaikuttaa ja joka määrää, onko automaattinen ohjauskomponentti päällä vai ei

		public Automaattinen_OhjausKomponentti(Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) :base(p_suuntavektori, ohjausvoiman_hallitsija) {
			//haetaan hallittava ohjauselementti
			ohjausvoiman_hallitsija.hallittavat_ohjaus_komponentit.ForEach(delegate(Hallittava_OhjausKomponentti obj) {
				if(obj.OnkoSamanSuuntainen_OhjausKomponentti(this, false))
					hallittava_ohjauskomponentti=obj;
			});
		}
	}
	public abstract class StaattinenOhjausVoimanRajoitin : Automaattinen_OhjausKomponentti {
		public StaattinenOhjausVoimanRajoitin(Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) :base(p_suuntavektori, ohjausvoiman_hallitsija) {}

		public override void Saada_ohjausvoimaa() {
			hallittava_ohjauskomponentti.KytkeOhjausPois(); //kytketään ohjausvoima pois
		}
	}
	//jetback hyppy
	//ominaisuudet:
	//hyppyvoimaa annetaan vain x-sekuntia hypyn alkamisesta tai siihen asti kunnes ohjaaminen (hyppy) lopetetaan
	//aika palautuu, kun koskettaa maata (isGrounded)
	//vaatii charactercontrollerin käytön ohjauksessa (tukee isGrounded)
	public class JetBack : StaattinenOhjausVoimanRajoitin {
		public AlustanMukaanMukautuvaParametri jetback_time;
		private float time_used=0;
		private bool maassa=false;
		private bool aika_taynna=false;

		public JetBack(AlustanMukaanMukautuvaParametri p_jetback_time, Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) :base(p_suuntavektori, ohjausvoiman_hallitsija) {
			jetback_time=p_jetback_time;
		}

		public override void Saada_ohjausvoimaa() {
			if(!maassa) { //ollaan ilmassa
				if(!aika_taynna) { //hyppyaikaa on jäljellä
					if(hallittava_ohjauskomponentti.ohjaus_elementti.ohjataanko) { //hyppy jatkuu
						time_used+=ohjausvoiman_hallitsija.delta_time;
						if(time_used>jetback_time.nykyinen_arvo) { //aika tuli täyteen
							//Debug.Log("aika taynna");
							aika_taynna=true;
							base.Saada_ohjausvoimaa();
						}
					} else { //hyppy keskeytettiin
						//Debug.Log("hyppy keskeytettiin");
						aika_taynna=true;
						base.Saada_ohjausvoimaa();
					}
				} else if(ohjausvoiman_hallitsija.peliukkelin_liikkeen_hallitsija.IsGrounded()) { //tultiin maahan
					//Debug.Log("maassa");
					maassa=true;
					hallittava_ohjauskomponentti.ohjaus_elementti.ohjaus_kytketty=true;
					time_used=0;
					aika_taynna=false;
				} else if(hallittava_ohjauskomponentti.ohjaus_elementti.ohjaus_kytketty) { //aika tullut täyteen jo aiemmin (ohjaus on kytketty takaisin päälle -> kytketään pois)
					//Debug.Log("ohjaus uudestaan pois");
					base.Saada_ohjausvoimaa();
				}
			} else if(hallittava_ohjauskomponentti.ohjaus_elementti.ohjataanko) { //hyppy aloitettiin
				//Debug.Log("hyppy");
				maassa=false;
			}
		}
	}
	//maksiminopeus
	public class Maksimi_nopeusrajoitus : StaattinenOhjausVoimanRajoitin {
		public AlustanMukaanMukautuvaParametri maksiminopeus;

		public Maksimi_nopeusrajoitus(AlustanMukaanMukautuvaParametri p_maksiminopeus, Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) :base(p_suuntavektori, ohjausvoiman_hallitsija) {
			maksiminopeus=p_maksiminopeus;
		}

		public override void Saada_ohjausvoimaa() {
			if(hallittava_ohjauskomponentti.ohjaus_elementti.ohjaus_kytketty)
				if(ohjausvoiman_hallitsija.OnkoOhjausvoimaaOhjausSuuntaan(suunta_parametrit))
					if(ohjausvoiman_hallitsija.AnnaNopeusOhjausSuuntaan(suunta_parametrit)>maksiminopeus.nykyinen_arvo)
						base.Saada_ohjausvoimaa();
		}
	}
	//jarruttaa liikkeen nollaan, kun ei ohjata komponentin suuntaan
	//toimii molempiin suuntiin (positiivinen ja negatiivinen)
	public class LiikkeenJarrutus : Automaattinen_OhjausKomponentti {
		public AlustanMukaanMukautuvaParametri liikkeen_pysaytysaika; //aika jonka kuluessa liike pysäytetään
		//apuparametreja
		private float liikkeen_jarrutukseen_kaytetty_aika;
		private bool jarrutetaan=false;

		public LiikkeenJarrutus(AlustanMukaanMukautuvaParametri p_liikkeen_pysaytysaika, Vector2 p_suuntavektori, Peliukkelin_Ohjausvoiman_Hallitsija ohjausvoiman_hallitsija) :base(p_suuntavektori, ohjausvoiman_hallitsija) {
			liikkeen_pysaytysaika=p_liikkeen_pysaytysaika;
		}

		public override void Saada_ohjausvoimaa() {
			if(hallittava_ohjauskomponentti.ohjaus_elementti.ohjaus_kytketty & (!hallittava_ohjauskomponentti.ohjaus_elementti.ohjataanko)) { //jos ei ohjata kyseisellä ohjauskomponentilla ja ohjaus on aktiivinen
				if(jarrutetaan) {
					liikkeen_jarrutukseen_kaytetty_aika += ohjausvoiman_hallitsija.delta_time;
				} else { //jarrutus aloitetaan
					jarrutetaan=true;
					liikkeen_jarrutukseen_kaytetty_aika=0;
				}
				float liikkeen_jarrutukseen_aikaa_jaljella=liikkeen_pysaytysaika.nykyinen_arvo-liikkeen_jarrutukseen_kaytetty_aika;
				if(liikkeen_jarrutukseen_aikaa_jaljella<=0) { //viimeinen steppi
					liikkeen_jarrutukseen_aikaa_jaljella=ohjausvoiman_hallitsija.delta_time; //kasvatetaan time-stepin pituiseksi
					jarrutetaan=false;
				}
				//lasketaan hidastuvuus (kiihtyvyys), jolla ukkelia on jarrutettava
				float hidastuvuus=-ohjausvoiman_hallitsija.AnnaNopeusOhjausSuuntaan(suunta_parametrit) / liikkeen_jarrutukseen_aikaa_jaljella;
				//if(liikkeen_jarrutukseen_aikaa_jaljella<0) hidastuvuus*=-1;
				ohjausvoiman_hallitsija.kokonais_ohjausvoima.LisaaVektoriinArvo(suunta_parametrit, hidastuvuus);
			} else
				jarrutetaan=false;
		}
	}

	public class Animator_ParametersHandling {
		public Animator animator;
		public int Direction=0; // animator.SetInteger("Direction", 0);
		private int edelDirection=0;
		public Target_ohjaus.ForDestroyingInstantiated hyppyaani;

		public Animator_ParametersHandling(Animator p_animator, Target_ohjaus.ForDestroyingInstantiated p_hyppyaani) {
			animator=p_animator;
			hyppyaani=p_hyppyaani;
		}

		public void AsetaParametrit() {
			if((animator!=null) & (edelDirection!=Direction))
				animator.SetInteger("Direction", Direction);
			if(hyppyaani!=null && Direction==3 && edelDirection!=Direction) //hyppy alkaa -> luodaan hyppyääni
				hyppyaani.Instantiate(Ohjattava_2D.Convert_to_Vector2(animator.transform.position));
			edelDirection=Direction;
		}
	}

	//inspector
	//parametriluokkia inspectoriin ja rakentajia
	//parametriluokkaan asettuu parametrit inspectorista ja rakentajalla niistä rakennetaan elementtejä

	//parametritluokat
	[System.Serializable]
	public abstract class Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin {
		public Vector2 ohjaussuunta; //vaikutussuunta (ylös: (0,1), oikealle: (1,0))

		//parametrin arvo riippuu siitä, mitä alustaa on viimeeksi kosketettu
		[System.Serializable]
		public class AlustanMukainenParametri_Parametrit_Hallintapaneeliin {
			public string alustan_tag;
			public float parametrin_arvo;
		}
	}
	//Peliukkelin ohjauksen rakentajat laittavat tästä olion inspectoriin
	[System.Serializable]
	public class Hallittava_Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin : Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin { //ohjausnappuloilla tai tekoälyllä ohjattavan ohjauskomponentin määritykset
		[System.Serializable]
		public class Toimintasuunta { //positiivinen ja negatiivinen
			[System.Serializable]
			public enum Kaytossa {
				ei,
				kylla
			}
			public Kaytossa kaytossa;

			public float ohjausvoima=50;
			public List<AlustanMukainenParametri_Parametrit_Hallintapaneeliin> alustakohtainen_ohjausvoima;
			public Ohjaus_laite.Parametrit_Hallintapaneeliin nappulan_asetukset; //nappulan asetukset
		}
		//anna asetukset, mikäli toimintasuunta toteutetaan
		public Toimintasuunta positiivinen;
		public Toimintasuunta negatiivinen;
	}
	//automaattiset ohjauskomponentit
	[System.Serializable]
	public abstract class Automaattinen_Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin : Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin {
	}
	[System.Serializable]
	public abstract class StaattinenOhjausVoimanRajoitin_Asetukset_Parametrit_Hallintapaneeliin : Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin {

	}
	[System.Serializable]
	public class JetBack_Asetukset_Parametrit_Hallintapaneeliin : StaattinenOhjausVoimanRajoitin_Asetukset_Parametrit_Hallintapaneeliin {
		public float jetback_time=0.5f;
		public List<AlustanMukainenParametri_Parametrit_Hallintapaneeliin> alustakohtainen_jetback_time;
	}
	[System.Serializable]
	public class Maksimi_nopeusrajoitus_Asetukset_Parametrit_Hallintapaneeliin : StaattinenOhjausVoimanRajoitin_Asetukset_Parametrit_Hallintapaneeliin {
		public float maksiminopeus=10;
		public List<AlustanMukainenParametri_Parametrit_Hallintapaneeliin> alustakohtainen_maksiminopeus;
	}
	[System.Serializable]
	public class LiikkeenJarrutus_Asetukset_Parametrit_Hallintapaneeliin : Automaattinen_Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin {
		public float liikkeen_pysaytysaika=1; //aika jonka kuluessa liike pysäytetään
		public List<AlustanMukainenParametri_Parametrit_Hallintapaneeliin> alustakohtainen_liikkeen_pysaytysaika;
	}

	//rakentajat
	public class AlustanMukainenParametri_Lista_Hallintapaneelista_Rakentaja {
		public List<OhjausKomponentti.AlustanMukainenParametri> Rakenna(List<Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin.AlustanMukainenParametri_Parametrit_Hallintapaneeliin> parametri_lista) {
			List<OhjausKomponentti.AlustanMukainenParametri> items=new List<OhjausKomponentti.AlustanMukainenParametri>();
			parametri_lista.ForEach(delegate(Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin.AlustanMukainenParametri_Parametrit_Hallintapaneeliin obj) {
				items.Add(new OhjausKomponentti.AlustanMukainenParametri(obj.alustan_tag, obj.parametrin_arvo));
			});
			return items;
		}
	}
	public abstract class AlustanMukainenParametri_Listan_Rakentava {
		AlustanMukainenParametri_Lista_Hallintapaneelista_Rakentaja alustan_mukainen_parametri_lista_rakentaja=new AlustanMukainenParametri_Lista_Hallintapaneelista_Rakentaja();

		public OhjausKomponentti.AlustanMukaanMukautuvaParametri Rakenna_AlustanMukaanMukautuvaParametri(float oletus_arvo, List<Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin.AlustanMukainenParametri_Parametrit_Hallintapaneeliin> alustanmukaiset_parametrit, Peliukkelin_Ohjausvoiman_Hallitsija peliukkelin_ohjausvoiman_hallitsija) {
			return peliukkelin_ohjausvoiman_hallitsija.Lisaa_AlustanMukaanMukautuvaParametri(oletus_arvo, alustan_mukainen_parametri_lista_rakentaja.Rakenna(alustanmukaiset_parametrit));
		}
	}
	public class Hallittava_Ohjauskomponetti_Hallintapaneelista_Rakentaja : AlustanMukainenParametri_Listan_Rakentava {
		public void Rakenna(Hallittava_Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin parametrit, Peliukkelin_Ohjausvoiman_Hallitsija peliukkelin_ohjausvoiman_hallitsija, Ohjaus_laite.Ohjaus_laite peliukkelin_ohjauslaite) {
			Menu.Button_elem positiivinen_button=null;
			Menu.Button_elem negatiivinen_button=null;
			Menu.Button_elem rakennettava_button=null;
			Hallittava_Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin.Toimintasuunta toimintasuunta=null;
			Ohjaus_laite.Hallintapaneelista_Rakentaja nappulan_rakentaja=new Ohjaus_laite.Hallintapaneelista_Rakentaja();
			//käsitellään positiivinen ja negatiivinen ohjaussuunta
			toimintasuunta=parametrit.positiivinen;
			while(toimintasuunta!=null) {
				if(toimintasuunta.kaytossa==Hallittava_Ohjauskomponetti_Asetukset_Parametrit_Hallintapaneeliin.Toimintasuunta.Kaytossa.kylla) //jos toteutetaan
					rakennettava_button=nappulan_rakentaja.RakennaNappula_Hallintapaneelista(toimintasuunta.nappulan_asetukset, peliukkelin_ohjauslaite);
				else
					rakennettava_button=null;
				//vaihdetaan ohjaussuuntaa
				//jos molemmat on käsitelty, lisätään ohjauskomponentti ja sinne nappulat
				if(toimintasuunta==parametrit.positiivinen) {
					positiivinen_button=rakennettava_button;
					toimintasuunta=parametrit.negatiivinen;
				} else if(toimintasuunta==parametrit.negatiivinen) {
					negatiivinen_button=rakennettava_button;
					toimintasuunta=null;
					OhjausKomponentti.AlustanMukaanMukautuvaParametri positiivinen_ohjausvoima=Rakenna_AlustanMukaanMukautuvaParametri(parametrit.positiivinen.ohjausvoima, parametrit.positiivinen.alustakohtainen_ohjausvoima, peliukkelin_ohjausvoiman_hallitsija);
					OhjausKomponentti.AlustanMukaanMukautuvaParametri negatiivinen_ohjausvoima=Rakenna_AlustanMukaanMukautuvaParametri(parametrit.negatiivinen.ohjausvoima, parametrit.negatiivinen.alustakohtainen_ohjausvoima, peliukkelin_ohjausvoiman_hallitsija);
					peliukkelin_ohjausvoiman_hallitsija.hallittavat_ohjaus_komponentit.Add(new Hallittava_OhjausKomponentti(positiivinen_button, negatiivinen_button, positiivinen_ohjausvoima, negatiivinen_ohjausvoima, parametrit.ohjaussuunta, peliukkelin_ohjausvoiman_hallitsija));
				}
			}
		}
	}
	//automaattisten ohjauskomponenttien rakentajat
	public class JetBack_Hallintapaneelista_Rakentaja : AlustanMukainenParametri_Listan_Rakentava {
		public void Rakenna(JetBack_Asetukset_Parametrit_Hallintapaneeliin parametrit, Peliukkelin_Ohjausvoiman_Hallitsija peliukkelin_ohjausvoiman_hallitsija) {
			peliukkelin_ohjausvoiman_hallitsija.automaattiset_ohjaus_komponentit.Add(new JetBack(Rakenna_AlustanMukaanMukautuvaParametri(parametrit.jetback_time, parametrit.alustakohtainen_jetback_time, peliukkelin_ohjausvoiman_hallitsija), parametrit.ohjaussuunta, peliukkelin_ohjausvoiman_hallitsija));
		}
	}
	public class Maksimi_nopeusrajoitus_Hallintapaneelista_Rakentaja : AlustanMukainenParametri_Listan_Rakentava {
		public void Rakenna(Maksimi_nopeusrajoitus_Asetukset_Parametrit_Hallintapaneeliin parametrit, Peliukkelin_Ohjausvoiman_Hallitsija peliukkelin_ohjausvoiman_hallitsija) {
			peliukkelin_ohjausvoiman_hallitsija.automaattiset_ohjaus_komponentit.Add(new Maksimi_nopeusrajoitus(Rakenna_AlustanMukaanMukautuvaParametri(parametrit.maksiminopeus, parametrit.alustakohtainen_maksiminopeus, peliukkelin_ohjausvoiman_hallitsija), parametrit.ohjaussuunta, peliukkelin_ohjausvoiman_hallitsija));
		}
	}
	public class LiikkeenJarrutus_Hallintapaneelista_Rakentaja : AlustanMukainenParametri_Listan_Rakentava {
		public void Rakenna(LiikkeenJarrutus_Asetukset_Parametrit_Hallintapaneeliin parametrit, Peliukkelin_Ohjausvoiman_Hallitsija peliukkelin_ohjausvoiman_hallitsija) {
			peliukkelin_ohjausvoiman_hallitsija.automaattiset_ohjaus_komponentit.Add(new LiikkeenJarrutus(Rakenna_AlustanMukaanMukautuvaParametri(parametrit.liikkeen_pysaytysaika, parametrit.alustakohtainen_liikkeen_pysaytysaika, peliukkelin_ohjausvoiman_hallitsija), parametrit.ohjaussuunta, peliukkelin_ohjausvoiman_hallitsija));
		}
	}
}