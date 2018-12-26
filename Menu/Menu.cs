using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Ohjaus_2D;

//Sisältää kaikki työkalut menun rakentamiseen: sivut, rakenne, elementit, tapahtumien suorittajat (delegointi)
namespace Menu {
	//sisältöä voidaan asettaa vaihtoon yhden OnGui-kutsun yli, koska vaaditaan suorittamaan OnGui kahdesti samalla sisällöllä
	public abstract class GUI_VaihdettavaSisalto_YhdenKutsukerranViiveella<T> {
		public T sisalto;
		protected T sisalto_to_change=default(T); //sisältö joka vaihdetaan asetetaan tähän
		protected bool sisalto_changing_not_yet=false; //kun sisältö asetetaan vaihtoon esim GUI-nappulasta piirtämisen jälkeen, on piirrettävä samat elementit vielä seuraavalla OnGui-kutsulla, jonka jälkeen vasta saa vaihtaa uuden sivun
		protected bool sisalto_change_to_default=false; //vaihdetaan default:ksi (esim. olioilla null)

		public GUI_VaihdettavaSisalto_YhdenKutsukerranViiveella(T p_sisalto) {
			sisalto=p_sisalto;
		}

		//asettaa sisällön vaihdettavaksi
		public void AsetaSisaltoVaihtoon(T p_sisalto_to_change) {
			if(p_sisalto_to_change!=null)
				sisalto_to_change=p_sisalto_to_change;
			else
				sisalto_change_to_default=true;
			sisalto_changing_not_yet=true;
		}

		//testaa, vaihdetaanko sisältö tällä kutsukerralla ja (jos) vaihtaa sisällön
		public void TestaaVaihdetaanko() {
			if (sisalto_changing_not_yet == true)
				sisalto_changing_not_yet = false;
			else if(sisalto_to_change!=null) {
				sisalto=sisalto_to_change;
				sisalto_to_change=default(T);
			} else if(sisalto_change_to_default==true) {
				sisalto=default(T);
				sisalto_change_to_default=false;
			}
		}
	}
	//Page:lle
	public class GUI_VaihdettavaPage_YhdenKutsukerranViiveella : GUI_VaihdettavaSisalto_YhdenKutsukerranViiveella<Page> {
		public GUI_VaihdettavaPage_YhdenKutsukerranViiveella(Page p_sisalto) :base(p_sisalto) {}

		public bool Piirra() {
			bool paluu;
			if(sisalto!=null) {
				sisalto.Piirra();
				paluu=true;
			} else
				paluu=false;
			TestaaVaihdetaanko();
			return paluu;
		}
	}
	//Layout:lle
	public class GUI_VaihdettavaLayout_elem_YhdenKutsukerranViiveella : GUI_VaihdettavaSisalto_YhdenKutsukerranViiveella<Layout_elem> {
		public GUI_VaihdettavaLayout_elem_YhdenKutsukerranViiveella(Layout_elem p_sisalto) :base(p_sisalto) {}

		public void Piirra() {
			sisalto.Piirra();
			TestaaVaihdetaanko();
		}

		public void Lisaa_Menu_elem(Menu_elem elem) {
			sisalto.Lisaa_Menu_elem(elem);
		}

		public void Insert_Menu_elem(int ind, Menu_elem elem) {
			sisalto.Insert_Menu_elem(ind, elem);
		}
		
		public void Replace_Menu_elem(Menu_elem old_elem, Menu_elem new_elem) {
			sisalto.Replace_Menu_elem(old_elem, new_elem);
		}
	}

	//luokka tyyliparametrien säilyttämiseen
	//voidaan antaa omat parametrit tai ottaa vastaan tarjoajan parametrit
	public abstract class MenunAsetukset {
		public GUISkin gui_skin;
		public GUILayout_Options guiLayout_Options; //yleisasetukset: tarjotaan automatic-layout elementeille
		public GUILayout_Options GUILayout_Options_for_navigationcontrols; //nappuloille, joille siirrytään sivulta toiselle

		public MenunAsetukset(GUISkin p_gui_skin, GUILayout_Options p_guiLayout_Options, GUILayout_Options p_GUILayout_Options_for_navigationcontrols, MenunAsetukset tarjoaja) {
			if(tarjoaja!=null) {
				gui_skin = p_gui_skin!=null ? p_gui_skin : tarjoaja.gui_skin;
				guiLayout_Options = p_guiLayout_Options!=null ? p_guiLayout_Options : tarjoaja.guiLayout_Options;
				GUILayout_Options_for_navigationcontrols = p_GUILayout_Options_for_navigationcontrols!=null ? p_GUILayout_Options_for_navigationcontrols : tarjoaja.GUILayout_Options_for_navigationcontrols;
			} else {
				gui_skin = p_gui_skin;
				guiLayout_Options = p_guiLayout_Options;
				GUILayout_Options_for_navigationcontrols = p_GUILayout_Options_for_navigationcontrols!=null ? p_GUILayout_Options_for_navigationcontrols : guiLayout_Options;
			}
		}
	}
	public class MenunAsetukset_for_ChildPage {
		public GUILayout_Options GUILayout_Options_for_navigation_area;

		public MenunAsetukset_for_ChildPage(GUILayout_Options p_GUILayout_Options_for_navigation_area, MenunAsetukset_for_ChildPage tarjoaja) {
			if(tarjoaja!=null) {
				GUILayout_Options_for_navigation_area = p_GUILayout_Options_for_navigation_area!=null ? p_GUILayout_Options_for_navigation_area : tarjoaja.GUILayout_Options_for_navigation_area;
			} else {
				GUILayout_Options_for_navigation_area = p_GUILayout_Options_for_navigation_area;
			}
		}
	}

	//menu-luokka, jonka kautta menuun viitataan
	//sisältää main pagen ja current pagen
	public class Menu : MenunAsetukset {
		private  static Menu instance; // Singleton
		//  Instance  
		public static Menu Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new Menu();     
				return instance;   
			}
		}

		private Menu() :base(null, null, null, null) {
			current_page=new GUI_VaihdettavaPage_YhdenKutsukerranViiveella(null); //null eli menu ei ole näkyvillä
		}

		public GUI_VaihdettavaPage_YhdenKutsukerranViiveella current_page=null; 
		public MainPage main_page=null;
		public delegate void UnPauseGame();
		public UnPauseGame unPauseGame_handling=null; //viivästetty unpauseviimeistely sen takia, kun android:n kosketusnäytön resume-kosketus aiheuttaa toiminnon pelitilaan
		public MenunAsetukset_for_ChildPage menun_asetukset_for_childpage; //childpage:lle jaettavat asetukset

		public void SetMainPage(MainPage p_main_page) {
			main_page=p_main_page;
			current_page.sisalto=main_page;
		}

		public void AsetaParametrit(GUISkin p_gui_skin, GUILayout_Options p_guiLayout_Options, GUILayout_Options p_GUILayout_Options_for_navigationcontrols, GUILayout_Options GUILayout_Options_for_navigation_area) {
			gui_skin=p_gui_skin;
			guiLayout_Options=p_guiLayout_Options;
			GUILayout_Options_for_navigationcontrols=p_GUILayout_Options_for_navigationcontrols;
			menun_asetukset_for_childpage=new MenunAsetukset_for_ChildPage(GUILayout_Options_for_navigation_area, null);
		}

		public void SetUnpause_handling(UnPauseGame p_unPauseGame_handling) {
			//asettaa unpausen viimeistelyn suorotettavaksi sitten, kun menu häviää pois
			unPauseGame_handling=p_unPauseGame_handling;
		}

		//current-pagen piirtäminen
		public void Piirra() {
			if(!current_page.Piirra())
				if(unPauseGame_handling!=null) {
					unPauseGame_handling(); //mikäli ei ole menua (enää) näkyvillä (unpause-toiminnon johdosta), suoritetaan unpausen viimeistely
					unPauseGame_handling=null;
				}
		}
	}

	//Menu page ylin luokka (periyttää menun asetukset)
	public abstract class Page : MenunAsetukset {
		public GUI_VaihdettavaLayout_elem_YhdenKutsukerranViiveella layout;
		public List<ChildPage> child_pages;
		public Automatic_layout page_layout; //sivun layout, johon ei voi vaikuttaa. sis. siirtymisen sivulta toiselle ja layout-elementin, johon voi vapaasti lisätä muita elementtejä
		public Menu_elem current_Menu_elem=null; //juuri piirretty menu_elem -> saadaan tunnistettua, kuka kutsui toiminnon suorittajaa
		public GUIStyle gui_style_for_navigationcontrols;

		//voidaan asettaa omat parametrit tai ottaa ne tarjoajalta
		public Page(MenunAsetukset tarjoaja, GUIContent p_gui_content, Rect p_position, Layout_elem p_layout, GUIStyle p_gui_style, GUISkin p_gui_skin, GUILayout_Options p_guiLayout_Options, GUIStyle p_gui_style_for_navigationcontrols, GUILayout_Options p_GUILayout_Options_for_navigationcontrols) :base(p_gui_skin, p_guiLayout_Options, p_GUILayout_Options_for_navigationcontrols, tarjoaja) {
			layout=new GUI_VaihdettavaLayout_elem_YhdenKutsukerranViiveella(p_layout);
			gui_style_for_navigationcontrols = p_gui_style_for_navigationcontrols!=null ? p_gui_style_for_navigationcontrols : gui_skin.GetStyle("navigationcontrols");
			child_pages=new List<ChildPage>();
			if(p_gui_style==null) p_gui_style=gui_skin.GetStyle("page_layout_style");
			page_layout=new Automatic_layout(p_gui_content, p_position, this, p_gui_style);
			page_layout.Lisaa_Menu_elem(layout.sisalto); //layout:n eteen lisätään mahdolliset child-paget
		}

		public virtual void AddChild(ChildPage child_page) {
			child_page.parent=this;
			child_pages.Add(child_page);
		}

		//sivun dimensiot
		//sivun position
		public Rect AnnaPosition() {
			return ((GUI_Piirtaja_with_Position_Labelintyyppisille) page_layout.controllerin_piirtaja).position;
		}
		//sijaintikoordinaatit vector2:na
		public Vector2 AnnaSijaintiKoordinaatit() {
			Rect position=AnnaPosition();
			return new Vector2(position.x, position.y);
		}
		//koko
		public Vector2 AnnaKoko() {
			Rect position=AnnaPosition();
			return new Vector2(position.width, position.height);
		}

		//piirretään sivu eli layout:n sisältö
		public virtual void Piirra() {
			page_layout.Piirra();
		}

		//palauttaa sivun childin paikan (ind)
		public int GetIndex_of_Child(ChildPage page) {
			return child_pages.IndexOf(page);
		}

		//kun chilpagegroupin edustaja (childpage) vaihdetaan, täytyy vaihtaa sen nappulan sisältö
		public virtual void UpdateContent_for_Button_to_Childpage(ChildPageGroup page, int child_ind) {}
	}
	//pääsivu (tai sivu, joka ei ole jonkun child)
	public class MainPage : Page {
		public MainPage(GUIContent p_gui_content, Rect p_position, Layout_elem p_layout, GUIStyle p_gui_style=null, GUISkin p_gui_skin=null, GUILayout_Options p_guiLayout_Options=null, GUIStyle p_gui_style_for_navigationcontrols=null, GUILayout_Options p_GUILayout_Options_for_navigationcontrols=null) : base(Menu.Instance, p_gui_content, p_position, p_layout, p_gui_style, p_gui_skin, p_guiLayout_Options, p_gui_style_for_navigationcontrols, p_GUILayout_Options_for_navigationcontrols) {}

		public override void AddChild(ChildPage child_page) {this.AddChild(child_page, null);}
		public void AddChild(ChildPage child_page, GUIStyle button_style) {
			base.AddChild(child_page); //lisää child-pagen childien listaan
			//lisätään nappula, jolla sivulle pääsee
			//lisätään se loppuun ennen layout:a
			page_layout.Insert_Menu_elem(page_layout.menu_elem_container.menu_elems.Count-1, new Button_elem(GUILayout.Button, child_page.page_layout.gui_content, child_page.page_layout.gui_content.text, SivunVaihtaja, this, GUILayout_Options_for_navigationcontrols, button_style!=null ? button_style : gui_style_for_navigationcontrols));
		}

		public override void UpdateContent_for_Button_to_Childpage(ChildPageGroup page, int child_ind) {
			//päivitetään nappulalle, jonka takana on child, child:n gui_content
			int ind=GetIndex_of_Child(page);
			if(ind>(-1)) {
				Menu_elem elem=page_layout.menu_elem_container.menu_elems[ind];
				((Label_elem) elem).VaihdaContent(page.pages[child_ind].page_layout.gui_content);
				elem.Vaihda_GUIStyle(page.current_gui_style_for_navigationbutton_for_parent);
			}
		}

		//toiminnon suorittaja sivun vaihdolle. kutsutaan kun painetaan jotain sivunvaihtonappia
		public void SivunVaihtaja(Ohjaus_2D.Ohjaustapahtuma kosketus) {
			int ind=page_layout.menu_elem_container.menu_elems.IndexOf(current_Menu_elem);
			if(!child_pages[ind].SuoritaSivunvaihtoOperaatio(kosketus)) //vaihdetaan sivu vain jos ei ole asetettu erillistä sivunvaihto-operaatiota
				Menu.Instance.current_page.AsetaSisaltoVaihtoon(child_pages[ind]);
		}
	}
	public class ChildPage : Page {
		public Page parent;
		public Horizontal_layout navigation_area;
		public ToolBar_elem child_pages_toolbar;
		public GUIStyle gui_style_for_back_button;
		public MenunAsetukset_for_ChildPage menun_asetukset_for_childpage;
		public bool child_vaihdettu=false; // viesti mahdolliselle ChildPageGroup:lle
		public Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja sivunvaihto_operaation_suorittaja=null; //sivunvaihdon sijasta (tämä sivu) voidaan suorittaa joku operaatio, kuten levelin käynnistäminen

		protected ChildPage(Page p_parent, GUIContent p_gui_content, Rect position, Layout_elem p_layout, GUIStyle p_gui_style, GUISkin p_gui_skin, GUILayout_Options p_guiLayout_Options, GUIStyle p_gui_style_for_back_button, GUIStyle p_gui_style_for_navigationcontrols, GUILayout_Options p_GUILayout_Options_for_navigationcontrols, GUIContent p_gui_content_for_navigation_area, GUIStyle p_gui_style_for_navigation_area, GUILayout_Options p_GUILayout_Options_for_navigation_area) :base(p_parent, p_gui_content, position, p_layout, p_gui_style, p_gui_skin, p_guiLayout_Options, p_gui_style_for_navigationcontrols, p_GUILayout_Options_for_navigationcontrols) {
			parent=p_parent;
			//layout-elem:n eteen lisätän horizontal-layout navigointia (muille sivuille) varten
			menun_asetukset_for_childpage=new MenunAsetukset_for_ChildPage(p_GUILayout_Options_for_navigation_area, Menu.Instance.menun_asetukset_for_childpage);
			if(p_gui_content_for_navigation_area==null) p_gui_content_for_navigation_area=new GUIContent("");
			navigation_area=new Horizontal_layout(p_gui_content_for_navigation_area, this, menun_asetukset_for_childpage.GUILayout_Options_for_navigation_area, p_gui_style_for_navigation_area);
			page_layout.Insert_Menu_elem(0, navigation_area);
			//lisätään back-button
			gui_style_for_back_button = p_gui_style_for_back_button!=null ? p_gui_style_for_back_button : gui_skin.GetStyle("back_button");
			navigation_area.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, parent.page_layout.gui_content, parent.page_layout.gui_content.text, SivunVaihtaja, this, GUILayout_Options_for_navigationcontrols, gui_style_for_back_button));
		}
		//constructori hakee sivun positionin sen parent:lta
		public ChildPage(Page p_parent, GUIContent p_gui_content, Layout_elem p_layout, GUIStyle p_gui_style=null, GUISkin p_gui_skin=null, GUILayout_Options p_guiLayout_Options=null, GUIStyle p_gui_style_for_back_button=null, GUIStyle p_gui_style_for_navigationcontrols=null, GUILayout_Options p_GUILayout_Options_for_navigationcontrols=null, GUIContent p_gui_content_for_navigation_area=null, GUIStyle p_gui_style_for_navigation_area=null, GUILayout_Options p_GUILayout_Options_for_navigation_area=null) :this(p_parent, p_gui_content, p_parent.AnnaPosition(), p_layout, p_gui_style, p_gui_skin, p_guiLayout_Options, p_gui_style_for_navigationcontrols, p_gui_style_for_navigationcontrols, p_GUILayout_Options_for_navigationcontrols, p_gui_content_for_navigation_area, p_gui_style_for_navigation_area, p_GUILayout_Options_for_navigation_area) {}
		//constructori pyytää sivun positionin
		public ChildPage(Page p_parent, GUIContent p_gui_content, Vector2 location_koordinates, Vector2 size, Layout_elem p_layout, GUIStyle p_gui_style=null, GUISkin p_gui_skin=null, GUILayout_Options p_guiLayout_Options=null, GUIStyle p_gui_style_for_back_button=null, GUIStyle p_gui_style_for_navigationcontrols=null, GUILayout_Options p_GUILayout_Options_for_navigationcontrols=null, GUIContent p_gui_content_for_navigation_area=null, GUIStyle p_gui_style_for_navigation_area=null, GUILayout_Options p_GUILayout_Options_for_navigation_area=null) :this(p_parent, p_gui_content, new Rect(location_koordinates.x, location_koordinates.y, size.x, size.y), p_layout, p_gui_style, p_gui_skin, p_guiLayout_Options, p_gui_style_for_navigationcontrols, p_gui_style_for_navigationcontrols, p_GUILayout_Options_for_navigationcontrols, p_gui_content_for_navigation_area, p_gui_style_for_navigation_area, p_GUILayout_Options_for_navigation_area) {}

		//toiminnon suorittaja sivun vaihdolle. kutsutaan kun painetaan jotain sivunvaihtonappia
		public void SivunVaihtaja(Ohjaus_2D.Ohjaustapahtuma kosketus) {
			int ind=navigation_area.menu_elem_container.menu_elems.IndexOf(Menu.Instance.current_page.sisalto.current_Menu_elem); //täytyy viitata current_pagen kautta, jotta toimii myös ChildPageGroup:lle (this viittaa ChildPage:een eikä ChildPageGroup:iin)
			if(ind==0) //back-button -> vaihdetaan sivua
				Menu.Instance.current_page.AsetaSisaltoVaihtoon(parent);
			else if(!child_pages[child_pages_toolbar.selected].SuoritaSivunvaihtoOperaatio(kosketus)) { //vaihdetaan sivu vain jos ei ole asetettu erillistä sivunvaihto-operaatiota
				//child_pages_toolbar -> ei vaihdeta sivua, vaan vaihdetaan layout:n sisällöksi valitun child-pagen layout
				//child_pages[child_pages_toolbar.selected].page_layout.menu_elem_container.TulostaElementit();
				page_layout.Replace_Menu_elem(layout.sisalto, child_pages[child_pages_toolbar.selected].layout.sisalto);
				//layout.AsetaSisaltoVaihtoon(child_pages[child_pages_toolbar.selected].layout.sisalto);
				layout=child_pages[child_pages_toolbar.selected].layout;
			}
			child_vaihdettu=true; // viesti mahdolliselle ChildPageGroup:lle
		}

		//suorittaa sivulle mahdollisen asetun operaation sivunvaihdossa
		//palauttaa true, mikäli operaatio on asetettu
		public bool SuoritaSivunvaihtoOperaatio(Ohjaus_2D.Ohjaustapahtuma kosketus) {
			if(sivunvaihto_operaation_suorittaja==null) {
				return false;
			} else {
				sivunvaihto_operaation_suorittaja(kosketus);
				return true;
			}
		}

		public void AsetaSivunvaihtoOperaatio(Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_sivunvaihto_operaation_suorittaja) {
			sivunvaihto_operaation_suorittaja=p_sivunvaihto_operaation_suorittaja;
		}

		public override void AddChild(ChildPage child_page) {
			base.AddChild(child_page); //lisää child-pagen childien listaan
			//lisätään tool_bar:iin nappula, jolla sivulle pääsee
			//mikäli tämä oli ensimmäinen child, lisätään tool_bar ja lisätään se navigation_area:lle back-buttonin jälkeen
			if(child_pages.Count==1) {
				child_pages_toolbar=new ToolBar_elem(GUILayout_Options_for_navigationcontrols, this, gui_style_for_navigationcontrols);
				navigation_area.Lisaa_Menu_elem(child_pages_toolbar);
				//valitaan myös näytettäväksi tämän child-sivun layout sen parentin (eli tämä sivu) layout:ssa
				page_layout.Replace_Menu_elem(layout.sisalto, child_page.layout.sisalto);
				//layout.AsetaSisaltoVaihtoon(child_page.layout.sisalto);
				layout=child_page.layout;
			}
			//lisätään se nappula
			child_pages_toolbar.AddToolbarButton(child_page.page_layout.gui_content, child_page.page_layout.gui_content.text, SivunVaihtaja);
		}

		public override void UpdateContent_for_Button_to_Childpage(ChildPageGroup page, int child_ind) {
			//päivitetään nappulalle, jonka takana on child, child:n gui_content
			int ind=GetIndex_of_Child(page);
			if(ind>(-1)) {
				Button_elem button=child_pages_toolbar.selection_elems[ind];
				((Label_elem) button).VaihdaContent(page.pages[child_ind].page_layout.gui_content);
			}
		}
	}
	//child page:ja voidaan ryhmittää parentissa saman nappulan taakse. valittuna on niistä yksi sivu ja nappula on sen mukainen
	public class ChildPageGroup : ChildPage {
		public List<ChildPage> pages;
		public ChildPage selected; //valittuna oleva sivu
		public List<GUIStyle> gui_styles_for_navigationbutton_for_parent;
		public GUIStyle current_gui_style_for_navigationbutton_for_parent;
		public new GUI_VaihdettavaLayout_elem_YhdenKutsukerranViiveella page_layout;

		//annetaan parentille eli ChildPage:lle välttämättömät parametrit ekasta listaan lisättävästä ChildPagesta
		//laitetaan eka ChildPage edustajaksi ja osa parametreista asettuu siis siinä vaiheessa (ne saa olla tässä periaatteessa mitä vain)
		public ChildPageGroup(ChildPage first_page, GUIStyle gui_style_for_navigationbutton_for_parent=null) :base(first_page.parent, first_page.page_layout.gui_content, new Rect(0,0,0,0), first_page.layout.sisalto, first_page.page_layout.gui_style, first_page.gui_skin, first_page.guiLayout_Options, first_page.gui_style_for_back_button, first_page.gui_style_for_navigationcontrols, first_page.GUILayout_Options_for_navigationcontrols, first_page.navigation_area.gui_content, first_page.navigation_area.gui_style, null) {
			pages=new List<ChildPage>();
			gui_styles_for_navigationbutton_for_parent=new List<GUIStyle>();
			parent=first_page.parent;
			current_gui_style_for_navigationbutton_for_parent=gui_style_for_navigationbutton_for_parent!=null ? gui_style_for_navigationbutton_for_parent : parent.gui_style_for_navigationcontrols;
			AddPage(first_page, current_gui_style_for_navigationbutton_for_parent);
			gui_styles_for_navigationbutton_for_parent.Add(current_gui_style_for_navigationbutton_for_parent);
			page_layout=new GUI_VaihdettavaLayout_elem_YhdenKutsukerranViiveella(first_page.page_layout);
			SetDelegate(0); //asetetaan edustamaan
		}

		//asettaa listasta jonkin ChildPagen edustamaan ChildGroupPagea ChildPagena
		public void SetDelegate(ChildPage page) {
			SetDelegate(pages.IndexOf(page));
		}
		public void SetDelegate(int index) {
			selected=GetPage_in_Group(index); //asetetaan valituksi eli piirrettäväksi
			//asetetaan selected edustamaan ChildPageGroup:a myös ChildPagena (välittömästi)
			//ChildPageGroup: rojut:
			current_gui_style_for_navigationbutton_for_parent=gui_styles_for_navigationbutton_for_parent[index];
			//ChildPage:n rojut:
			//parent=selected.parent; //tarkoitettu samaksi
			navigation_area=selected.navigation_area;
			child_pages_toolbar=selected.child_pages_toolbar;
			menun_asetukset_for_childpage=selected.menun_asetukset_for_childpage;
			sivunvaihto_operaation_suorittaja=selected.sivunvaihto_operaation_suorittaja;
			//Pagen rojut:
			layout=selected.layout;
			child_pages=selected.child_pages;
			page_layout.AsetaSisaltoVaihtoon(selected.page_layout);
			gui_style_for_back_button=selected.gui_style_for_back_button;
			gui_style_for_navigationcontrols=selected.gui_style_for_navigationcontrols;
			GUILayout_Options_for_navigationcontrols=selected.GUILayout_Options_for_navigationcontrols;
			//MenunAsetukset rojut
			gui_skin=selected.gui_skin;
			guiLayout_Options=selected.guiLayout_Options;
			//päivitetään nappula parentissa
			parent.UpdateContent_for_Button_to_Childpage(this, index);
		}

		//palauttaa ryhmästä halutun sivun
		public ChildPage GetPage_in_Group(int ind_of_page) {
			return pages[ind_of_page];
		}

		public override void AddChild(ChildPage child_page) {
			AddChild(0, child_page); //toiminto ekalle sivulle
		}
		public void AddChild(int index_in_page_list, ChildPage child_page) {
			base.AddChild(pages[index_in_page_list]);
		}

		//lisää sivun ryhmään
		public void AddPage(ChildPage page, GUIStyle gui_style_for_navigationbutton_for_parent=null) {
			pages.Add(page);
			gui_styles_for_navigationbutton_for_parent.Add(gui_style_for_navigationbutton_for_parent!=null ? gui_style_for_navigationbutton_for_parent : parent.gui_style_for_navigationcontrols);
		}

		public override void Piirra() {
			if(selected.child_vaihdettu) { //jos valittuna oleva sivu vaihtoi näytettävän child:nsa, niin pitää päivittää edustaja
				SetDelegate(selected);
				selected.child_vaihdettu=false;
			}
			page_layout.Piirra();
		}
	}

	//joka ikinen menu-elementti periytetään tästä
	public abstract class Menu_elem {
		public GUIStyle gui_style; //elementin tyyli
		public GUI_Piirtaja controllerin_piirtaja; //piirtää elementin
		protected string styleName; //tyylin guiskinissä (tyylit haetaan sieltä)

		public Menu_elem(string p_styleName, Page target_page, GUIStyle p_gui_style) {
			styleName=p_styleName;
			gui_style=p_gui_style;
			//jos ei erikseen anneta tyyliä, otetaan se mahdolliselta target-sivulta
			if((gui_style==null) && (target_page!=null) && (target_page.gui_skin!=null)) {
				gui_style=target_page.gui_skin.FindStyle(GetStylename());
			}
		}
		
		public virtual void Piirra() {
			Menu menu=Menu.Instance;
			if(menu!=null) {
				Page current_page=menu.current_page.sisalto;
				if(current_page!=null)
					menu.current_page.sisalto.current_Menu_elem=this; //asetetaan viittaus elementtiin mahdollista sivunvaihtoklikkausta varten -> tiedetään, mitä klikattiin
			}
			controllerin_piirtaja.Piirra();
		}

		public virtual string GetStylename() {
			return this.styleName;
		}

		//palauttaa tyylin nimen, jota haetaan skinistä
		public void Vaihda_GUIStyle(GUIStyle new_style) {
			gui_style=new_style;
		}
	}

	//container, joka pitää sisällään menu-elementit
	//esim layout-lementit säilyttävät sisältämänsä menu-elementit containerissa
	//voidaan käyttää myös ilman omistajaa
	public class Menu_elem_container {
		public List<Menu_elem> menu_elems;

		public Menu_elem_container() {
			menu_elems=new List<Menu_elem>();
		}

		public void Lisaa_Menu_elem(Menu_elem elem) {
			menu_elems.Add(elem);
		}

		public void Insert_Menu_elem(int ind, Menu_elem elem) {
			menu_elems.Insert(ind, elem);
		}

		public void Replace_Menu_elem(Menu_elem old_elem, Menu_elem new_elem) {
			int ind=menu_elems.IndexOf(old_elem);
			menu_elems.RemoveAt(ind);
			Insert_Menu_elem (ind, new_elem);
		}

		public void Piirra_elementit() {
			menu_elems.ForEach(delegate(Menu_elem obj) {
				if(obj!=null) obj.Piirra();
			});
		}

		public void TulostaElementit() {
			Debug.Log("elementit:");
			menu_elems.ForEach(delegate(Menu_elem obj) {
				Debug.Log(obj.ToString());
			});
		}
	}

	//Layout-elementit
	public abstract class Layout_elem : Label_elem {
		public Menu_elem_container menu_elem_container;
		public delegate void End_func(); //asetetaan läyout-haaran lopetusfunktio riippuen layout:n tyypistä
		public End_func end_func;

		public Layout_elem(GUIContent p_gui_content, End_func p_end_func, Page target_page, GUIStyle p_gui_style=null) :base(p_gui_content, "layout", target_page, p_gui_style) {
			menu_elem_container=new Menu_elem_container();
			end_func=p_end_func;
		}

		public void Begin_func() {
			base.Piirra(); //label elem piirtää käyttäen layout-elemin piirtäjää
		}

		public override void Piirra() {
			Begin_func();
			menu_elem_container.Piirra_elementit(); //elementit layout:n sisältä
			end_func(); //end
		}

		public void Lisaa_Menu_elem(Menu_elem elem) {
			menu_elem_container.Lisaa_Menu_elem(elem);
		}

		public void Insert_Menu_elem(int ind, Menu_elem elem) {
			menu_elem_container.Insert_Menu_elem(ind, elem);
		}

		public void Replace_Menu_elem(Menu_elem old_elem, Menu_elem new_elem) {
			menu_elem_container.Replace_Menu_elem(old_elem, new_elem);
		}
	}

	//GUI.BeginGroup (Fixed_layout)
	public class Fixed_layout : Layout_elem {
		private Fixed_layout(GUIContent p_gui_content, Page target_page, GUIStyle p_gui_style) :base(p_gui_content, GUI.EndGroup, target_page, p_gui_style) {}
		public Fixed_layout(GUIContent p_gui_content, Rect p_position, Page target_page, GUIStyle p_gui_style=null) :this(p_gui_content, target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_Position_Labelintyyppisille(this, GUI.BeginGroup, p_position);
		}
	}

	//GUI.BeginScrollView
	public class ScrollView : Layout_elem {
		private ScrollView(GUIContent p_gui_content, Page target_page, GUIStyle p_gui_style) :base(null, GUI.EndScrollView, target_page, p_gui_style) {}
		public ScrollView(Rect p_position, Vector2 p_scrollPosition, Rect p_viewRect, bool p_alwaysShowHorizontal, bool p_alwaysShowVertical, Page target_page, GUIStyle p_gui_style=null) :this(null, target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_Position_for_ScrollView(this, GUI.BeginScrollView, p_position, p_scrollPosition, p_viewRect, p_alwaysShowHorizontal, p_alwaysShowVertical);
		}
	}

	//GUILayout.BeginScrollView
	public class ScrollView_AutomaticLayout : Layout_elem {
		private ScrollView_AutomaticLayout(GUIContent p_gui_content, Page target_page, GUIStyle p_gui_style) :base(null, GUILayout.EndScrollView, target_page, p_gui_style) {}
		public ScrollView_AutomaticLayout(Vector2 p_scrollPosition, bool p_alwaysShowHorizontal, bool p_alwaysShowVertical, Page target_page, GUILayout_Options p_guilayoutOptions=null, GUIStyle p_gui_style=null) :this(null, target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_for_ScrollView(this, GUILayout.BeginScrollView, p_scrollPosition, p_alwaysShowHorizontal, p_alwaysShowVertical, target_page, p_guilayoutOptions);
		}
	}

	//GUILayout.BeginArea (Automatic_layout)
	public class Automatic_layout : Layout_elem {
		private Automatic_layout(GUIContent p_gui_content, Page target_page, GUIStyle p_gui_style) :base(p_gui_content, GUILayout.EndArea, target_page, p_gui_style) {}
		public Automatic_layout(GUIContent p_gui_content, Rect p_position, Page target_page, GUIStyle p_gui_style=null) :this(p_gui_content, target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_Position_Labelintyyppisille(this, GUILayout.BeginArea, p_position);
		}
	}

	//GUILayout.BeginVertical (Vertical_layout)
	public class Vertical_layout : Layout_elem {
		private Vertical_layout(GUIContent p_gui_content, Page target_page, GUIStyle p_gui_style) :base(p_gui_content, GUILayout.EndVertical, target_page,  p_gui_style) {}
		public Vertical_layout(GUIContent p_gui_content, Page target_page, GUILayout_Options p_guilayoutOptions=null, GUIStyle p_gui_style=null) :this(p_gui_content, target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_Labelintyyppisille(this, GUILayout.BeginVertical, target_page, p_guilayoutOptions);
		}
	}

	//GUILayout.BeginHorizontal (Horizontal_layout)
	public class Horizontal_layout : Layout_elem {
		private Horizontal_layout(GUIContent p_gui_content, Page target_page, GUIStyle p_gui_style) :base(p_gui_content, GUILayout.EndHorizontal, target_page, p_gui_style) {}
		public Horizontal_layout(GUIContent p_gui_content, Page target_page, GUILayout_Options p_guilayoutOptions=null, GUIStyle p_gui_style=null) :this(p_gui_content, target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_Labelintyyppisille(this, GUILayout.BeginHorizontal, target_page, p_guilayoutOptions);
		}
	}

	//Label
	public class Label_elem : Menu_elem {
		public GUIContent gui_content;

		protected Label_elem(GUIContent p_gui_content, string styleName, Page target_page, GUIStyle p_gui_style) :base(styleName, target_page, p_gui_style) {
			gui_content=p_gui_content;
		}
		//fixed-layout:lle
		public Label_elem(GUI_Piirtaja_with_Position_Labelintyyppisille.GUI_piirtaja GUI_funktio, GUIContent p_gui_content, Rect p_position, Page target_page, GUIStyle p_gui_style=null) :this(p_gui_content, "label", target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_Position_Labelintyyppisille(this, GUI_funktio, p_position);
		}
		//automatic layout:lle
		public Label_elem(GUI_Piirtaja_with_GUILayoutOptions_Labelintyyppisille.GUI_piirtaja GUILayout_funktio, GUIContent p_gui_content, GUILayout_Options p_guilayoutOptions, Page target_page, GUIStyle p_gui_style=null) :this(p_gui_content, "label", target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_Labelintyyppisille(this, GUILayout_funktio, target_page, p_guilayoutOptions);
		}

		//vaihtaa näytettävän sisällön kuten tekstin
		public void VaihdaContent(GUIContent content) {
			gui_content=content;
		}
	}
	//Button, ReapeatButton ja Toggle (katso constructorin ohje)
	//myös button tai repeatButton label-, GUITexture- tai virtualbutton-toteutuksena, jolloin toimii multitouch-ohjaus
	//GUITexturessa skaalautuminen resoluution mukaan toimii automaattisesti, eikä sitä siis piirretä piirtofunktiolla, vaan se on GameObjectina scenellä..
	//GUITexturessa pixel_inset-arvot pitää olla kaikki nollia, jotta skaalautuminen resoluution mukaan toimii täysin automaattisesti!
	//virtualbuttoniakaan ei tietenkään oikeasti piirretä
	//virtualbuttonia käytetään ohjaus_elem_tilan_tallettaja:n kautta, jota hallitaan Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D-tyyppisen tila_inputin avulla
	public class Button_elem : Label_elem {
		public Ohjaus_elem_2D ohjaus_elem; //sisältää ohjauksen suorittajan ja määrittää ohjauksen tilat, joilla toiminto suoritetaan. ei lisätä ohjaus_handlinkiin, vaan kosketus kytketään suoraan tähän
		public Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D tila_input=null; //antaa ohjauksen tilan kosketuksena
		public bool tila=false;
		public bool edellinen_tila=false; //tila edellisen framen aikana
		public bool tukee_multitouch=false;
		public bool valittu_button=false;
		public bool valittu_repeat_button=false;
		public bool valittu_toggle=false;
		public Ohjaus_elem_2D ohjaus_elem_tilan_tallettaja=null; //voidaan käyttää nappulan tilan tallettamiseen
		
		protected Button_elem(GUIContent p_gui_content, Rect p_dimensio, string p_nimi, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja, Page target_page, GUIStyle p_gui_style, List<TouchPhase> p_hyvaksytyt_kosketukset, GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja GUI_funktio, GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille.GUI_piirtaja GUILayout_funktio, bool p_tukee_multitouch) :base(p_gui_content, "button", target_page, p_gui_style) {
			ohjaus_elem=new Ohjaus_elem_2D(p_dimensio, p_nimi, p_ohjaustapahtumien_suorittaja);
			//tunnistetaan elementin tyyppi
			if((GUI_funktio==null) && (GUILayout_funktio==null)) // toggle
				valittu_toggle=true;
			else {
				// testataan, mikä on valittu: button, repeat_button
				GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja GUI_funktio_button;
				GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja GUI_funktio_repeatbutton;
				GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille.GUI_piirtaja GUILayout_funktio_button;
				GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille.GUI_piirtaja GUILayout_funktio_repeatbutton;
				GUI_funktio_button=GUI.Button;
				GUI_funktio_repeatbutton=GUI.RepeatButton;
				GUILayout_funktio_button=GUILayout.Button;
				GUILayout_funktio_repeatbutton=GUILayout.RepeatButton;
				if(GUI_funktio!=null) {
					if(GUI_funktio.Equals(GUI_funktio_button))
						valittu_button=true;
					else if(GUI_funktio.Equals(GUI_funktio_repeatbutton))
						valittu_repeat_button=true;
				} else if(GUILayout_funktio!=null) {
					if(GUILayout_funktio.Equals(GUILayout_funktio_button))
						valittu_button=true;
					else if(GUILayout_funktio.Equals(GUILayout_funktio_repeatbutton))
						valittu_repeat_button=true;
				}
			}

			//hyväksytyt kosketukset
			if(p_hyvaksytyt_kosketukset!=null) {
				ohjaus_elem.hyvaksytyt_kosketukset=p_hyvaksytyt_kosketukset;
			}
			//määritetään hyväksytyt kosketukset (oletuksena)
			else if(valittu_button) {
				ohjaus_elem.LisaaHyvaksyttyKosketus(TouchPhase.Ended); //oletuksena buttonille
			}
			else if(valittu_repeat_button) {
				//oletuksena repeatbuttonille
				ohjaus_elem.LisaaHyvaksyttyKosketus(TouchPhase.Began);
				ohjaus_elem.LisaaHyvaksyttyKosketus(TouchPhase.Stationary);
				ohjaus_elem.LisaaHyvaksyttyKosketus(TouchPhase.Moved);
			} else if(valittu_toggle)
				ohjaus_elem.LisaaHyvaksyttyKosketus(TouchPhase.Began); //oletuksena togglelle

			//tukee multitouch?
			if(valittu_button || valittu_repeat_button) tukee_multitouch=p_tukee_multitouch; //voi tukea
		}
		//Fixed layout:lle
		//jos annetaan toiseksi viimeinen parametri eli ohjaus, ohjauksen kautta tulevat kosketukset, jotka osuvat nappula-elementtiin, suodatetaan pois (button, repeat button ja toggle), eikä se aiheuta ohjaustoimintoa kyseisen ohjauksen kautta
		//ohjaus pitää antaa aina multitouch buttonin tapauksessa (poislukien virtualbutton, jolloin ohjausta ei pidä antaa), jolloin ohjauksen tila tunnistetaan ohjauksen kautta. tällöin elementtiä ohjataan kosketusnäytöltä (tukee multitouch:a) tai hiirellä (virtualbuttonia ohjaa jokin ulkoinen järjestelmä) ja nämä ohjaustoiminnot on suoritettava ennen nappuloiden piirtämistä
		//ohjaustoiminnot kytketään suoraan nappulan omaan ohjauselementtiin (luodaan tässä).
		//*******Tapaus, jossa 1.) on avoinna menu-ikkuna ja sen ulkopuolelle osuvat kosketukset hylätään (kytke ohjaus-handling pois päältä)
		//2.)Käytetään fixed-layout:a -> silloin ohjaus-handling pitää rajata kyseiseen ikkunaan ja origo ikkunan kulmaan
		//3.)Suodata nappuloihin menevät kosketukset ohjaus-handlingin kautta (eli anna parametrina ohjaus) aina tilanteissa, joissa on esim. nappuloita pelialueen reunoilla ja haluat ohjata muun pelitilan kosketuksia koordinaatteihin perustuen
		//***********Anna eka parametri null, jos halutaan piirtää Togle. Buttonin ja RepeatButtonin tapauksessa anna delegaatti näihin.
		public Button_elem(GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja GUI_funktio, GUIContent p_gui_content, Rect p_position, string p_nimi, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja, Page target_page, GUIStyle p_gui_style=null, List<TouchPhase> p_hyvaksytyt_kosketukset=null, Ohjattava_2D ohjaus=null, bool p_tukee_multitouch=false, GUITexture gui_texture=null, bool virtual_button=false) :this(p_gui_content, p_position, p_nimi, p_ohjaustapahtumien_suorittaja, target_page, p_gui_style, p_hyvaksytyt_kosketukset, GUI_funktio, null, p_tukee_multitouch) {
			if(valittu_button || valittu_repeat_button) {
				if(tukee_multitouch) // Multitouch Button
					if(gui_texture!=null || virtual_button==true) //toteutus GUITexturena tai virtuaalisena buttonina
						controllerin_piirtaja=new GUI_Piirtaja_with_Position_for_Multitouch_Button_ei_piirrettava(this);
					else //GUI-elementtinä
						controllerin_piirtaja=new GUI_Piirtaja_with_Position_for_Multitouch_Button(this, p_position);
				else
					controllerin_piirtaja=new GUI_Piirtaja_with_Position_Buttontyyppisille(this, GUI_funktio, p_position);
			} else if(valittu_toggle) //Togle
				controllerin_piirtaja=new GUI_Piirtaja_with_Position_for_Toggle(this, p_position);

			//jos annetaan ohjaus, laitetaan sinne ohjaus-elementti, joka kytkee ohjaus-handling:n kautta tulevat kosketukset koordinaattien perusteella ohjaus-elementtiin. tämä ohjaus-elementti ei suorita toimintoa,
			if(tukee_multitouch) {
				// vaan ottaa ohjauksen tilan talteen
				if(gui_texture!=null)
					ohjaus_elem_tilan_tallettaja=new Ohjaus_elem_2D(gui_texture, ohjaus_elem.alue.nimi, ((GUI_Piirtaja_with_Position_for_Multitouch_Button)controllerin_piirtaja).AktiivisenTilanTallettaja);
				else if(virtual_button==true) //ei lisätä ohjaus_handlinkiin, vaan nappulan käyttäjän on käytettävä nappulaa tilantallettajan kautta, jota hallitaan Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D-tyyppisen tila-inputin avulla
					ohjaus_elem_tilan_tallettaja=new Ohjaus_elem_2D(ohjaus_elem.alue.dimensio, ohjaus_elem.alue.nimi + "_tilan_tallettaja", ((GUI_Piirtaja_with_Position_for_Multitouch_Button)controllerin_piirtaja).AktiivisenTilanTallettaja, false);
				else
					ohjaus_elem_tilan_tallettaja=new Ohjaus_elem_2D(ohjaus_elem.alue.dimensio, ohjaus_elem.alue.nimi + "_tilan_tallettaja", ((GUI_Piirtaja_with_Position_for_Multitouch_Button)controllerin_piirtaja).AktiivisenTilanTallettaja, true);
				ohjaus_elem_tilan_tallettaja.hyvaksytyt_kosketukset=ohjaus_elem.hyvaksytyt_kosketukset;
				if(ohjaus!=null && virtual_button==false)
					ohjaus.LisaaOhjaus_elem(ohjaus_elem_tilan_tallettaja); //ohjauksen tila talteen
			} else if(ohjaus!=null) { //muille
				//vaan vain suodattaa kosketuksen pois
				ohjaus.LisaaOhjaus_elem(new Ohjaus_elem_2D(ohjaus_elem.alue.dimensio, ohjaus_elem.alue.nimi + "_filter", null, true));
			}
			if((!tukee_multitouch) || p_ohjaustapahtumien_suorittaja!=null) //multitouchin tapauksessa käyttöä löytyy vain, jos on asetettu ohjaustapahtuman suorittaja
				//kytketään varsinainen ohjaus suoraan ohjaus-elementtiin
				tila_input=new Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D();
		}
		//fixed layout GUITexturella
		//annetaan nappulalle virtuaalipositionin Rect(0,0,2,2), jolla ei ole niin väliä, kun ohjauselementti on tyyppiä Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D
		public Button_elem(GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja GUI_funktio, GUITexture gui_texture, string p_nimi, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja, Ohjattava_2D ohjaus, List<TouchPhase> p_hyvaksytyt_kosketukset=null) :this(GUI_funktio, null, new Rect(0,0,2,2), p_nimi, p_ohjaustapahtumien_suorittaja, null, null, p_hyvaksytyt_kosketukset, ohjaus, true, gui_texture, false) {
			//tarkistetaan, että GUITexturen pixel_inset-arvot on nollia
			if(!(gui_texture.pixelInset.x==0 && gui_texture.pixelInset.y==0 && gui_texture.pixelInset.width==0 && gui_texture.pixelInset.height==0))
				Debug.LogError("GUITexturen pixelInset on nollasta eriava elementissa: " + p_nimi);
		}
		//fixed layout virtuaalisena buttonina
		//annetaan nappulalle virtuaalipositionin Rect(0,0,2,2), jolla ei ole niin väliä, kun ohjauselementti on tyyppiä Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D
		public Button_elem(GUI_Piirtaja_with_Position_Buttontyyppisille.GUI_piirtaja GUI_funktio, string p_nimi, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja, Ohjattava_2D ohjaus, List<TouchPhase> p_hyvaksytyt_kosketukset=null) :this(GUI_funktio, null, new Rect(0,0,2,2), p_nimi, p_ohjaustapahtumien_suorittaja, null, null, p_hyvaksytyt_kosketukset, ohjaus, true, null, true) {}
		//automatic layout:lle
		public Button_elem(GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille.GUI_piirtaja GUILayout_funktio, GUIContent p_gui_content, string p_nimi, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja, Page target_page, GUILayout_Options p_guilayoutOptions=null, GUIStyle p_gui_style=null, List<TouchPhase> p_hyvaksytyt_kosketukset=null) :this(p_gui_content, new Rect(0, 0, 10, 10), p_nimi, p_ohjaustapahtumien_suorittaja, target_page, p_gui_style, p_hyvaksytyt_kosketukset, null, GUILayout_funktio, false) {
			if(valittu_button || valittu_repeat_button) { //Button tai RepeatButton
				controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille(this, GUILayout_funktio, target_page, p_guilayoutOptions);
			} else if(valittu_toggle) { //Togle
				controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_for_Toggle(this, target_page, p_guilayoutOptions);
			}
			tila_input=new Kosketuksen_suoraan_Ohjaus_elemiin_kytkeva_Tila_input_2D();
		}

		//piirtää ja suorittaa (rekisteröi kosketuksen) mahdollisen toiminnon
		public override void Piirra() {
			edellinen_tila=tila;
			base.Piirra(); //piirtää ja asettaa tilan talteen
			if(tila_input!=null) {
				tila_input.RekisteroiKosketus(ohjaus_elem, tila_input.AnnaTilasiirtyma(tila, edellinen_tila, true), tila_input.AnnaTilasiirtyma(tila, edellinen_tila, false));
			}
		}

		//olio annetaan Togglen piirtäjälle ja se estää asettamisen klikkaamalla falseksi
		public class Blocker_for_Toggle_to_Set_False {
			public Button_elem elem;

			public Blocker_for_Toggle_to_Set_False(Button_elem p_elem) {
				elem=p_elem;
			}

			public void Block_to_Set_False() {
				if(elem.tila==false && elem.edellinen_tila==true)
					elem.tila=true;
			}
		}

		//nappulan voi asettaa tästä näkyviin tai piiloon
		//tukee ainoastaan GUITexture-toteutusta!
		public void AsetaNakyvyys(bool tila) {
			((GUITexture_Alue)ohjaus_elem_tilan_tallettaja.alue).gui_texture.gameObject.SetActive (tila);
		}
	}

	//ToolBar_elem
	public class ToolBar_elem : Menu_elem {
		public List<Button_elem> selection_elems=new List<Button_elem>();
		public int edel_selected=-1;
		public int selected=-1;

		protected ToolBar_elem(string styleName, Page target_page, GUIStyle p_gui_style) :base(styleName, target_page, p_gui_style) {}
		//fixed layout:lle
		public ToolBar_elem(Rect p_position, string styleName, Page target_page, GUIStyle p_gui_style=null) :this("toolbar", target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_Position_for_ToolBar(this, p_position);
		}
		//automatic layout:lle
		public ToolBar_elem(GUILayout_Options p_guilayoutOptions, Page target_page, GUIStyle p_gui_style=null) :this("toolbar", target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_for_ToolBar(this, target_page, p_guilayoutOptions);
		}

		//lisää nappulan, jota edustaa Button_elem
		public void AddToolbarButton(GUIContent p_gui_content, string p_nimi, Ohjaus_elem_2D.OhjausTapahtumien_Suorittaja p_ohjaustapahtumien_suorittaja) {
			selection_elems.Add(new Button_elem(GUILayout.Button, p_gui_content, p_nimi, p_ohjaustapahtumien_suorittaja, null));
		}

		//piirtää ja suorittaa (rekisteröi kosketuksen) mahdollisen toiminnon
		public override void Piirra() {
			edel_selected=selected;
			base.Piirra(); //piirtää ja asettaa tilan talteen
			if(selected>-1) { //jos on valinta
				selection_elems[selected].tila_input.RekisteroiKosketus(selection_elems[selected].ohjaus_elem, false, selection_elems[selected].tila_input.AnnaTilasiirtyma(true, edel_selected==selected, true));
				if(edel_selected>-1) //jos oli myös aiemmin jo valinta
					selection_elems[edel_selected].tila_input.RekisteroiKosketus(selection_elems[edel_selected].ohjaus_elem, false, selection_elems[selected].tila_input.AnnaTilasiirtyma(edel_selected==selected, true, false));
			}
		}

		//tekee listan nappuloiden GUIContenteista
		public List<GUIContent> GetList_of_GUIContents() {
			return selection_elems.ConvertAll(new Converter<Button_elem, GUIContent>(delegate(Button_elem elem) {
				return elem.gui_content;
			}));
		}
		//ja tämä palauttaa sen arrayna, joka voidaan antaa tool_bar:n piirtäjä-funktiolle
		public GUIContent[] GetArray_of_GUIContents() {
			List<GUIContent> list_of_guicontents=this.GetList_of_GUIContents();
			GUIContent[] array_of_guicontents=new GUIContent[list_of_guicontents.Count];
			list_of_guicontents.CopyTo(array_of_guicontents);
			return array_of_guicontents;
		}
	}
	//SelectionGrid_elem (eroaa vain vähän tool_bar:sta)
	public class SelectionGrid_elem : ToolBar_elem {
		public int xCount;

		//fixed-layout:lle
		public SelectionGrid_elem(Rect p_position, int p_xCount, Page target_page, GUIStyle p_gui_style=null) :base("selectiongrid", target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_Position_for_SelectionGrid(this, p_position);
			xCount=p_xCount;
		}
		//automatic layout:lle
		public SelectionGrid_elem(int p_xCount, Page target_page, GUILayout_Options p_guilayoutOptions=null, GUIStyle p_gui_style=null) :base("selectiongrid", target_page, p_gui_style) {
			controllerin_piirtaja=new GUI_Piirtaja_with_GUILayoutOptions_for_SelectionGrid(this, target_page, p_guilayoutOptions);
			xCount=p_xCount;
		}
	}


	//Toteuttamattomat menu-elementit
/*
	//myös TextField
	public class TextArea_elem : Menu_elem {
		public string text;
		public int maxLength;
	}

	//Horizontal tai Vertical
	public class Slider_elem : Menu_elem {
		public Ohjaus_elem_2D ohjaus_elem; //sisältää dimensionin, ohjauksen tilan ja suorittajan
		public float value;
		public float edel_value;
		public GUIStyle thumb; //ylemmän luokan gui_style toimii sliderin tyylinä
		public float minValue;
		public float maxValue;
	}
	//Horizontal tai Vertical
	public class ScrollBar_elem : Slider_elem {
		public float size;
	}
*/

	//elementtien piirtäjät
	public abstract class GUI_Piirtaja {
		public Menu_elem elem; //piirtäjän omistaja
		public delegate void GUI_piirtaja(); //GUI-funktion
		public GUI_piirtaja piirtaja;

		public GUI_Piirtaja(Menu_elem p_elem, GUI_piirtaja p_piirtaja) {
			elem=p_elem;
			piirtaja=p_piirtaja;
		}

		public virtual void Piirra() {}
	}

	//**************** Piirtäjät fixed-layout:lle ******************
	public abstract class GUI_Piirtaja_with_Position : GUI_Piirtaja {
		public Rect position;

		public GUI_Piirtaja_with_Position(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position) :base(p_elem, p_piirtaja) {
			position=p_position;
		}
	}
	//Labelintyyppisille
	//GUIStyle voidaan jättää pois, niin silloin elementtiä ei piirretä. Voidaan käyttää esim. silloin kun elementti toteutetaan GUITexturella (ei piirretä)
	public class GUI_Piirtaja_with_Position_Labelintyyppisille : GUI_Piirtaja_with_Position {
		public new delegate void GUI_piirtaja(Rect position, GUIContent content, GUIStyle style);
		public new GUI_piirtaja piirtaja;

		public GUI_Piirtaja_with_Position_Labelintyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position) :base(p_elem, null, p_position) {
			piirtaja=p_piirtaja;
		}

		public override void Piirra () {
			if(elem.gui_style!=null)
				piirtaja(position, ((Label_elem) elem).gui_content, elem.gui_style);
		}
	}
	//ScrollViewtyyppisille
	public class GUI_Piirtaja_with_Position_for_ScrollView : GUI_Piirtaja_with_Position {
		public new delegate Vector2 GUI_piirtaja(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar);
		public new GUI_piirtaja piirtaja;
		public Vector2 scrollPosition;
		public Rect viewRect;
		public bool alwaysShowHorizontal;
		public bool alwaysShowVertical;

		public GUI_Piirtaja_with_Position_for_ScrollView(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position, Vector2 p_scrollPosition, Rect p_viewRect, bool p_alwaysShowHorizontal, bool p_alwaysShowVertical) :base(p_elem, null, p_position) {
			piirtaja=p_piirtaja;
			scrollPosition=p_scrollPosition;
			viewRect=p_viewRect;
			alwaysShowHorizontal=p_alwaysShowHorizontal;
			alwaysShowVertical=p_alwaysShowVertical;
		}

		public override void Piirra () {
			scrollPosition=piirtaja(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, elem.gui_style, elem.gui_style);
		}
	}
	//Buttontyyppisille
	public class GUI_Piirtaja_with_Position_Buttontyyppisille : GUI_Piirtaja_with_Position {
		public new delegate bool GUI_piirtaja(Rect position, GUIContent content, GUIStyle style);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_Position_Buttontyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position) :base(p_elem, null, p_position) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			((Button_elem) elem).tila=piirtaja(position, ((Label_elem) elem).gui_content, elem.gui_style);
		}
	}
	//Toggle:lle
	public class GUI_Piirtaja_with_Position_for_Toggle : GUI_Piirtaja_with_Position_Buttontyyppisille {
		public Button_elem.Blocker_for_Toggle_to_Set_False setting_to_false_blocker=null;

		public GUI_Piirtaja_with_Position_for_Toggle(Menu_elem p_elem, Rect p_position) :base(p_elem, null, p_position) {}

		public void SetBlocking_to_SetFalse() {
			setting_to_false_blocker = new Button_elem.Blocker_for_Toggle_to_Set_False((Button_elem) elem);
		}

		public override void Piirra () {
			((Button_elem) elem).tila=GUI.Toggle(position, ((Button_elem) elem).edellinen_tila, ((Label_elem) elem).gui_content, elem.gui_style);
			if(setting_to_false_blocker!=null) setting_to_false_blocker.Block_to_Set_False(); //estetään mahdollinen falseksi asettaminen, mikäli asetettu
		}
	}
	//Multitouch_Buttonille (piirretään labelina)
	public class GUI_Piirtaja_with_Position_for_Multitouch_Button : GUI_Piirtaja_with_Position_Labelintyyppisille {
		public bool aktiivinen_tila_asetettu=false;

		public GUI_Piirtaja_with_Position_for_Multitouch_Button(Menu_elem p_elem, Rect p_position) :base(p_elem, GUI.Label, p_position) {}

		public override void Piirra () {
			base.Piirra(); //piirtää elementin labelina
			AsetaTilaTalteen();
		}

		//asettaa talteen otetun tilan
		public void AsetaTilaTalteen() {
			((Button_elem) elem).tila=aktiivinen_tila_asetettu; //asetetaan talteen otettu tila
			aktiivinen_tila_asetettu=false; //talteen otettu tila nollataan
		}

		//ohjaus merkitsee (asettaa) nappulan tilan aktiiviseksi tällä. mikäli kutsua ei tule, tila ei ole aktiivinen
		public void AktiivisenTilanTallettaja(Ohjaustapahtuma kosketus) {
			aktiivinen_tila_asetettu=true;
		}
	}
	//Multitouch_Button GUITexturena tai virtuaalisena nappulana (ei piirretä, vaan se on scenellä GameObjectina)
	public class GUI_Piirtaja_with_Position_for_Multitouch_Button_ei_piirrettava : GUI_Piirtaja_with_Position_for_Multitouch_Button {
		public GUI_Piirtaja_with_Position_for_Multitouch_Button_ei_piirrettava(Menu_elem p_elem) :base(p_elem, new Rect()) {}

		//ei oikeasti piirrä, koska GUITexturea ei tarvitse piirtää. Ottaa vain tilan talteen
		public override void Piirra () {
			AsetaTilaTalteen();
		}
	}
	//ToolBar:lle
	public class GUI_Piirtaja_with_Position_for_ToolBar : GUI_Piirtaja_with_Position {
		public GUI_Piirtaja_with_Position_for_ToolBar(Menu_elem p_elem, Rect p_position) :base(p_elem, null, p_position) {}

		public override void Piirra () {
			((ToolBar_elem) elem).selected=GUI.Toolbar(position, ((ToolBar_elem) elem).edel_selected, ((ToolBar_elem) elem).GetArray_of_GUIContents(), elem.gui_style);
		}
	}
	//SelectionGrid
	public class GUI_Piirtaja_with_Position_for_SelectionGrid : GUI_Piirtaja_with_Position {
		public GUI_Piirtaja_with_Position_for_SelectionGrid(Menu_elem p_elem, Rect p_position) :base(p_elem, null, p_position) {}
		
		public override void Piirra () {
			((SelectionGrid_elem) elem).selected=GUI.SelectionGrid(position, ((SelectionGrid_elem) elem).edel_selected, ((SelectionGrid_elem) elem).GetArray_of_GUIContents(), ((SelectionGrid_elem) elem).xCount, elem.gui_style);
		}
	}

	//toteuttamattomat piirtäjät
/*	public class GUI_Piirtaja_with_Position_TextAreatyyppisille : GUI_Piirtaja_with_Position {
		public new delegate void GUI_piirtaja(Rect position, string text, int maxLength, GUIStyle style);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_Position_TextAreatyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position) :base(p_elem, null, p_position) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			piirtaja(position, ((TextArea_elem) elem).text, ((TextArea_elem) elem).maxLength, elem.gui_style);
		}
	}
	public class GUI_Piirtaja_with_Position_Slidertyyppisille : GUI_Piirtaja_with_Position {
		public new delegate float GUI_piirtaja(Rect position, float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb);
		public new GUI_piirtaja piirtaja;

		public GUI_Piirtaja_with_Position_Slidertyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position) :base(p_elem, null, p_position) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			((Slider_elem) elem).value=piirtaja(position, ((Slider_elem) elem).edel_value, ((Slider_elem) elem).minValue, ((Slider_elem) elem).maxValue, elem.gui_style, ((Slider_elem) elem).thumb);
		}
	}
	public class GUI_Piirtaja_with_Position_Scrollbartyyppisille : GUI_Piirtaja_with_Position {
		public new delegate float GUI_piirtaja(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_Position_Scrollbartyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Rect p_position) :base(p_elem, null, p_position) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			((ScrollBar_elem) elem).value=piirtaja(position, ((ScrollBar_elem) elem).edel_value, ((ScrollBar_elem) elem).size, ((ScrollBar_elem) elem).minValue, ((ScrollBar_elem) elem).maxValue, elem.gui_style);
		}
	}
*/

	// ****************** Piirtäjät automatic-layout tyyppisille ********************
	public abstract class GUI_Piirtaja_with_GUILayoutOptions : GUI_Piirtaja {
		public GUILayout_Options guilayoutOptions;

		public GUI_Piirtaja_with_GUILayoutOptions(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Page target_page, GUILayout_Options p_guilayoutOptions) :base(p_elem, p_piirtaja) {
			guilayoutOptions=p_guilayoutOptions;
			//tarjotaan kohdesivulta layout-asetuksia, mikäli ei ole erikseen annettu
			if(guilayoutOptions==null && target_page!=null)
				guilayoutOptions=target_page.guiLayout_Options;
			//jos ei ollu target-pagellakaan, luodaan tyhjä
			if(guilayoutOptions==null)
				guilayoutOptions=new GUILayout_Options();
		}
	}
	//Labelintyyppisille
	public class GUI_Piirtaja_with_GUILayoutOptions_Labelintyyppisille : GUI_Piirtaja_with_GUILayoutOptions {
		public new delegate void GUI_piirtaja(GUIContent content, GUIStyle style, GUILayoutOption[] options);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_GUILayoutOptions_Labelintyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Page target_page, GUILayout_Options p_guilayoutOptions=null) :base(p_elem, null, target_page, p_guilayoutOptions) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			piirtaja(((Label_elem) elem).gui_content, elem.gui_style, guilayoutOptions.Get_Array());
		}
	}
	//ScrollScrollView
	public class GUI_Piirtaja_with_GUILayoutOptions_for_ScrollView : GUI_Piirtaja_with_GUILayoutOptions {
		public new delegate Vector2 GUI_piirtaja(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUILayoutOption[] options);
		public new GUI_piirtaja piirtaja;
		public Vector2 scrollPosition;
		public bool alwaysShowHorizontal;
		public bool alwaysShowVertical;

		public GUI_Piirtaja_with_GUILayoutOptions_for_ScrollView(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Vector2 p_scrollPosition, bool p_alwaysShowHorizontal, bool p_alwaysShowVertical, Page target_page, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, target_page, p_guilayoutOptions) {
			piirtaja=p_piirtaja;
			scrollPosition=p_scrollPosition;
			alwaysShowHorizontal=p_alwaysShowHorizontal;
			alwaysShowVertical=p_alwaysShowVertical;
		}

		public override void Piirra () {
			scrollPosition=piirtaja(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, elem.gui_style, elem.gui_style, guilayoutOptions.Get_Array());
		}
	}
	//Buttontyyppisille
	public class GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille : GUI_Piirtaja_with_GUILayoutOptions {
		public new delegate bool GUI_piirtaja(GUIContent content, GUIStyle style, GUILayoutOption[] options);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, Page target_page, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, target_page, p_guilayoutOptions) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			((Button_elem) elem).tila=piirtaja(((Label_elem) elem).gui_content, elem.gui_style, guilayoutOptions.Get_Array());
		}
	}
	//Toggle
	public class GUI_Piirtaja_with_GUILayoutOptions_for_Toggle : GUI_Piirtaja_with_GUILayoutOptions_Buttontyyppisille {
		public Button_elem.Blocker_for_Toggle_to_Set_False setting_to_false_blocker=null;

		public GUI_Piirtaja_with_GUILayoutOptions_for_Toggle(Menu_elem p_elem, Page target_page, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, target_page, p_guilayoutOptions) {}

		public void SetBlocking_to_SetFalse() {
			setting_to_false_blocker = new Button_elem.Blocker_for_Toggle_to_Set_False((Button_elem) elem);
		}

		public override void Piirra () {
			((Button_elem) elem).tila=GUILayout.Toggle(((Button_elem) elem).edellinen_tila, ((Label_elem) elem).gui_content, elem.gui_style, guilayoutOptions.Get_Array());
			if(setting_to_false_blocker!=null) setting_to_false_blocker.Block_to_Set_False(); //estetään mahdollinen falseksi asettaminen, mikäli asetettu
		}
	}
	//ToolBar
	public class GUI_Piirtaja_with_GUILayoutOptions_for_ToolBar : GUI_Piirtaja_with_GUILayoutOptions {
		public GUI_Piirtaja_with_GUILayoutOptions_for_ToolBar(Menu_elem p_elem, Page target_page, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, target_page, p_guilayoutOptions) {}
		
		public override void Piirra () {
			((ToolBar_elem) elem).selected=GUILayout.Toolbar(((ToolBar_elem) elem).edel_selected, ((ToolBar_elem) elem).GetArray_of_GUIContents(), elem.gui_style, guilayoutOptions.Get_Array());
		}
	}
	//SelectionGrid
	public class GUI_Piirtaja_with_GUILayoutOptions_for_SelectionGrid : GUI_Piirtaja_with_GUILayoutOptions {
		public GUI_Piirtaja_with_GUILayoutOptions_for_SelectionGrid(Menu_elem p_elem, Page target_page, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, target_page, p_guilayoutOptions) {}
		
		public override void Piirra () {
			((SelectionGrid_elem) elem).selected=GUILayout.SelectionGrid(((SelectionGrid_elem) elem).edel_selected, ((SelectionGrid_elem) elem).GetArray_of_GUIContents(), ((SelectionGrid_elem) elem).xCount, elem.gui_style, guilayoutOptions.Get_Array());
		}
	}

	//toteuttamattomat piirtäjät
/*	public class GUI_Piirtaja_with_GUILayoutOptions_TextAreatyyppisille : GUI_Piirtaja_with_GUILayoutOptions {
		public new delegate void GUI_piirtaja(string text, int maxLength, GUIStyle style, GUILayoutOption[] options);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_GUILayoutOptions_TextAreatyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, p_guilayoutOptions) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			piirtaja(((TextArea_elem) elem).text, ((TextArea_elem) elem).maxLength, elem.gui_style, guilayoutOptions.Get_Array());
		}
	}
	public class GUI_Piirtaja_with_GUILayoutOptions_Slidertyyppisille : GUI_Piirtaja_with_GUILayoutOptions {
		public new delegate float GUI_piirtaja(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUILayoutOption[] options);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_GUILayoutOptions_Slidertyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, p_guilayoutOptions) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			((Slider_elem) elem).value=piirtaja(((Slider_elem) elem).edel_value, ((Slider_elem) elem).minValue, ((Slider_elem) elem).maxValue, elem.gui_style, ((Slider_elem) elem).thumb, guilayoutOptions.Get_Array());
		}
	}
	public class GUI_Piirtaja_with_GUILayoutOptions_Scrollbartyyppisille : GUI_Piirtaja_with_GUILayoutOptions {
		public new delegate float GUI_piirtaja(float value, float size, float leftValue, float rightValue, GUIStyle style, GUILayoutOption[] options);
		public new GUI_piirtaja piirtaja;
		
		public GUI_Piirtaja_with_GUILayoutOptions_Scrollbartyyppisille(Menu_elem p_elem, GUI_piirtaja p_piirtaja, GUILayout_Options p_guilayoutOptions) :base(p_elem, null, p_guilayoutOptions) {
			piirtaja=p_piirtaja;
		}
		
		public override void Piirra () {
			((ScrollBar_elem) elem).value=piirtaja(((ScrollBar_elem) elem).edel_value, ((ScrollBar_elem) elem).size, ((ScrollBar_elem) elem).minValue, ((ScrollBar_elem) elem).maxValue, elem.gui_style, guilayoutOptions.Get_Array());
		}
	}
*/
	//luokkaan rakennetaan lista GUILayoutOption:sta (Automatic layout)
	public class GUILayout_Options {
		public List<GUILayoutOption> guilayoutOptions;

		public GUILayout_Options() {
			guilayoutOptions=new List<GUILayoutOption>();
		}

		public void Lisaa_GUILayoutOption(GUILayoutOption option) {
			guilayoutOptions.Add(option);
		}

		//palauttaa muodossa, joka voidaan antaa piirtäjä-funktiolle
		public GUILayoutOption[] Get_Array() {
			GUILayoutOption[] options_array=new GUILayoutOption[guilayoutOptions.Count];
			guilayoutOptions.CopyTo(options_array);
			return options_array;
		}
	}

	//inspector
	[System.Serializable]
	public class HyvaksyttyjenKosketuksienMaarittaminen_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Maaritetaanko {
			oletusasetukset,
			maaritan_itse
		}
		public Maaritetaanko maaritetaanko;
		public bool Began;
		public bool Stationary;
		public bool Moved;
		public bool Ended;
	}
	[System.Serializable]
	public class GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Option {
			Width,
			Height,
			MinWidth,
			MaxWidth,
			MinHeight,
			MaxHeight,
			ExpandWidth,
			ExpandHeight
		}
		public Option option;
		public float lukuarvo;
		public bool tila;
	}

	//************* rakentajat *********************
	public class HyvaksyttyjenKosketuksienMaarittaminen_Hallintapaneelista_Rakentaja {
		public List<TouchPhase> Rakenna(HyvaksyttyjenKosketuksienMaarittaminen_Parametrit_Hallintapaneeliin parametrit) {
			List<TouchPhase> hyvaksytyt_kosketukset=new List<TouchPhase>();
			if(parametrit.maaritetaanko==HyvaksyttyjenKosketuksienMaarittaminen_Parametrit_Hallintapaneeliin.Maaritetaanko.maaritan_itse) {
				if(parametrit.Began) hyvaksytyt_kosketukset.Add(TouchPhase.Began);
				if(parametrit.Stationary) hyvaksytyt_kosketukset.Add(TouchPhase.Stationary);
				if(parametrit.Moved) hyvaksytyt_kosketukset.Add(TouchPhase.Moved);
				if(parametrit.Ended) hyvaksytyt_kosketukset.Add(TouchPhase.Ended);
			} else
				hyvaksytyt_kosketukset=null;
			return hyvaksytyt_kosketukset;
		}
	}
	public class GUILayout_Options_Hallintapaneelista_Rakentaja {
		public GUILayout_Options Rakenna(List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> parametrit) {
			GUILayout_Options options=new GUILayout_Options();
			parametrit.ForEach(delegate(GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin obj) {
				if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.ExpandHeight)
					options.Lisaa_GUILayoutOption(GUILayout.ExpandHeight(obj.tila));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.ExpandWidth)
					options.Lisaa_GUILayoutOption(GUILayout.ExpandWidth(obj.tila));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.Height)
					options.Lisaa_GUILayoutOption(GUILayout.Height(obj.lukuarvo));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.MaxHeight)
					options.Lisaa_GUILayoutOption(GUILayout.MaxHeight(obj.lukuarvo));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.MaxWidth)
					options.Lisaa_GUILayoutOption(GUILayout.MaxWidth(obj.lukuarvo));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.MinHeight)
					options.Lisaa_GUILayoutOption(GUILayout.MinHeight(obj.lukuarvo));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.MinWidth)
					options.Lisaa_GUILayoutOption(GUILayout.MinWidth(obj.lukuarvo));
				else if(obj.option==GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin.Option.Width)
					options.Lisaa_GUILayoutOption(GUILayout.Width(obj.lukuarvo));
			});
			if(options.guilayoutOptions.Count>0)
				return options;
			return null;
		}
	}
}
