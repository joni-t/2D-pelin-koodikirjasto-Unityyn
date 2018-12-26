using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Menu;
using Ohjaus_2D;
using Level_hallinta;
using Ohjaus_laite;
//using Cryptography;

//Tehtaalla rakennetaan menua: sen sisältöä, toiminnallisuutta ja rakennetta
namespace MenuFactory {
	public class MenuFactory {

		private  static MenuFactory instance; // Singleton
		//  Instance  
		public static MenuFactory Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new MenuFactory();     
				return instance;   
			}
		}
		
		private MenuFactory() {
			menu=Menu.Menu.Instance;
			levelin_hallinta=Level_hallinta.Level_hallinta.Instance;
		}
		//custom tyylit menu_skin:ssä:

		//vakiotyylit:
		//page_layout_style: menu-ikkunan tyyli
		//layout: muiden layout:ien oletustyyli
		//back_button: back-buttonin tyyli
		//navigationcontrols: muiden navigointinappuloiden tyyli

		//lisätyylit
		//kampanja_alue: facebook ja twitter-alue
		//level_ostonappula_alue: level-valitsijasivun ostonappua-alue
		//remove_adds: nappulan tyyli
		//restore_purchases: nappulan tyyli
		//start_button
		//resume_game_button: main-menussa
		//resume_button: pause-menussa
		//options_button
		//quit_button
		//facebook: facebook-nappula
		//twitter: twitter-nappula
		//level_selection
		//sound_selector
		//music_selector
		//play_again_button
		//play_another_level_button
		//play_next_level_button
		//return_to_mainmenu_button
		//tulos_alue_levelcomplete
		//tulos_alue_levelfailed
		//uusi_tulos
		//paras_tulos
		//uusi_tulos_label
		//paras_tulos_label

		public GUISkin menu_skin; //tyylit kulkee tässä
		public GUILayout_Options_Maaritykset guilayout_options_maaritykset;
		public Rect menu_position; //menun position (kaikilla sivuilla sama)
		public int levelselection_grid_xCount; //kuinka monta on listassa vaakarivillä
		public int first_level_index; //ensimmäisen peli-levelin indeksi
		public Level_hallinta.Level_hallinta levelin_hallinta; //rajapinta levelin hallintaan
		public bool game_paused=false;
		private bool set_level_kaynnissa_true_when_unpause; //asetetaanko levelin hallinta peli käynnissä tilaan un-pause toiminnossa
		public Button_elem sound_button;
		public Button_elem play_next_level_button;
		public Label_elem congratulations_label;
		public string game_completed_message="Game Completed!";
		public Label_elem current_showed_changeable_elem_in_levelcompletepage; //joko play next level nappi tai congratulations_label
		public Texture locked_level_image; //lukkokuva
		public LevelinTulosMittaaja.PistemittariResurssit pistemittariresurssit;
		public List<Level_Set> level_sets=null;
		public bool avaa_lukitutkin_levelit;
		//public RSA_Cryptaus completed_levels_crypting; //tietojen salaus
		public string game_key; //pelin avain
		public string noads_key; //avain mainoksien poistamiseen
		public string first_time_key; //ilmaisee ensimmäisen käynnistyskerran
		public string level_message; //tekstin pätkä, josta koostuu teksti, minkä taakse levelin avain laitetaan
		public string level_best_time_message="_best_time";
		public string level_best_points_message="_best_points";
		private string noads_message="_noads";
		private string first_time_started_message="_first_time_started";
		//mainoskanavat
		public bool nayta_mainokset;
		public bool nayta_mainos_kaynnistyessa;
		public bool ala_nayta_mainos_uusille_alussa;
		public int mainos_lkm_levelin_jalkeen;

		public Menu.Menu menu;
		public MainPage mainpage;
		public Menu.ChildPage levelselectorpage;
		public Menu.ChildPage pausepage;
		public ChildPageGroup levelselectorpage_and_pausepage;
		public MainPage levelcompletepage;
		public MainPage levelfailedpage;
		public ChildPage optionspage;
		public PausenappulaRajapinta pause_rajapinta;
		private delegate void SetMainPage(Menu.MainPage Page); //Menun set-metodi
		private SelectionGrid_elem level_selection_grid;
		public Menu.GUILayout_Options_Hallintapaneelista_Rakentaja gUILayout_Options_rakentaja=new Menu.GUILayout_Options_Hallintapaneelista_Rakentaja();
		public Skip_Level skip_level_hallinta=null;

		public void AsetaParametrit(GUISkin p_menu_skin, GUILayout_Options_Maaritykset p_guilayout_options_maaritykset, Rect p_menu_position, int p_levelselection_grid_xCount, int p_first_level_index, float originalScreenWidth, float originalScreenHeight, LevelinTulosMittaaja.PistemittariResurssit p_pistemittariresurssit, List<Level_Set> p_level_sets, string p_game_key, string p_noads_key, string p_first_time_key, bool p_avaa_lukitutkin_levelit, bool p_nayta_mainokset, bool p_nayta_mainos_kaynnistyessa, bool p_ala_nayta_mainos_uusille_alussa, int p_mainos_lkm_levelin_jalkeen) {
			menu_skin=p_menu_skin;
			guilayout_options_maaritykset=p_guilayout_options_maaritykset;
			menu_position=p_menu_position;
			levelselection_grid_xCount=p_levelselection_grid_xCount;
			first_level_index=p_first_level_index;
			menu.AsetaParametrit(menu_skin, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.menu_yleisasetukset), gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.menu_yleisasetukset_for_navigationcontrols), gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.menu_yleisasetukset_for_navigation_area_of_childpages));
			pistemittariresurssit=p_pistemittariresurssit;
			level_sets=p_level_sets;
			game_key=p_game_key;
			noads_key=p_noads_key;
			first_time_key=p_first_time_key;
			level_message=game_key + "_level_key_";
			avaa_lukitutkin_levelit=p_avaa_lukitutkin_levelit;
			nayta_mainokset=p_nayta_mainokset;
			nayta_mainos_kaynnistyessa=p_nayta_mainos_kaynnistyessa;
			ala_nayta_mainos_uusille_alussa=p_ala_nayta_mainos_uusille_alussa;
			mainos_lkm_levelin_jalkeen=p_mainos_lkm_levelin_jalkeen;
		}
		public void AsetaPausenappi(PausenappulaRajapinta p_pause_rajapinta) {
			pause_rajapinta=p_pause_rajapinta;
		}

		public void ToiminnotPiirrettavanSivunMukaan() {
			//sallitaan mainosbanneri level selection sivulla
			Mainosbanneri_Voidaan_Nayttaa=false;
			if(levelselectorpage_and_pausepage.Equals(menu.current_page.sisalto)) { //pause tai level selection
				if(!levelselectorpage_and_pausepage.selected.Equals(pausepage)) { //ei pausesivu -> joku level selection sivu
					Mainosbanneri_Voidaan_Nayttaa=true;
				}
			}
		}

		//määrittää pystysuoran layout:n
		private Layout_elem Maarita_pystysuora_layout(GUILayout_Options gUILayout_options=null) {
			return new Menu.Vertical_layout(new GUIContent(""), null, gUILayout_options, menu_skin.FindStyle("layout"));
		}
		//ja vaakasuoran
		private Layout_elem Maarita_vaakasuora_layout(GUILayout_Options gUILayout_options=null) {
			return new Menu.Horizontal_layout(new GUIContent(""), null, gUILayout_options, menu_skin.FindStyle("layout"));
		}

		//jonkinlainen mainpage (asettaa sivun talteen)
		private Menu.MainPage MainPageFactoryMethod(SetMainPage SetMethod, GUIContent p_gui_content, Menu.Layout_elem layout) {
			Menu.MainPage page=new Menu.MainPage(p_gui_content, menu_position, layout); 
			if(SetMethod!=null)
				SetMethod(page);
			return page;
		}
		//jonkinlainen childpage (parent:na childpage)
		private Menu.ChildPage ChildPageFactoryMethod(Menu.ChildPage p_parent, GUIContent p_gui_content, Menu.Layout_elem layout) {
			Menu.ChildPage child=ChildPageFactoryMethod_Support(p_parent, p_gui_content, layout);
			p_parent.AddChild(child);
			return child;
		}
		//jonkinlainen childpage (parent:na mainpage)
		private Menu.ChildPage ChildPageFactoryMethod_where_Parent_is_Mainpage(GUIContent p_gui_content, Menu.Layout_elem layout, GUIStyle button_style=null) {
			Menu.ChildPage child=ChildPageFactoryMethod_Support(mainpage, p_gui_content, layout);
			mainpage.AddChild(child, button_style);
			return child;
		}
		//jonkinlainen childpagegroup (sisältää ensimmäisen childpagen, joka annetaan parametrina)
		private Menu.ChildPageGroup ChildPageGroupFactoryMethod(Menu.ChildPage p_parent, Menu.ChildPage first_page) {
			Menu.ChildPageGroup child=new Menu.ChildPageGroup(first_page);
			p_parent.parent.AddChild(child);
			return child;
		}
		//jonkinlainen childpagegroup (parent:na mainpage)
		private Menu.ChildPageGroup ChildPageGroupFactoryMethod_where_Parent_is_Mainpage(Menu.ChildPage first_page, GUIStyle button_style=null) {
			Menu.ChildPageGroup child=new Menu.ChildPageGroup(first_page, button_style);
			mainpage.AddChild(child, button_style);
			return child;
		}
		//ainoastaan luo jonkinlaisen childpagen
		private Menu.ChildPage ChildPageFactoryMethod_Support(Menu.Page p_parent, GUIContent p_gui_content, Menu.Layout_elem layout) {
			return new Menu.ChildPage(p_parent, p_gui_content, layout);
		}

		//Play_another_level_button
		private Button_elem Get_new_Play_another_level_button(Page target_page, GUILayout_Options guilayout_options=null) {
			return new Button_elem(GUILayout.Button, new GUIContent("Level Menu"), "Play_another_level", Play_another_level, target_page, guilayout_options, menu_skin.FindStyle("play_another_level_button"));
		}
		//Play_again_button
		private Button_elem Get_new_Play_again_button(Page target_page, GUILayout_Options guilayout_options=null) {
			return new Button_elem(GUILayout.Button, new GUIContent("Play again"), "Play_again", Play_again, target_page, guilayout_options, menu_skin.FindStyle("play_again_button"));
		}

		public void Lisaa_MainPage() {
			mainpage=MainPageFactoryMethod(menu.SetMainPage, new GUIContent("Main"), Maarita_pystysuora_layout(gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.main_page.layout)));
			GUILayout_Options guilayout_options_for_buttons=gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.main_page.buttons);
			if (!KeyHandling.Detect_Is_Lock_Open (game_key + noads_message, noads_key)) { //jos ei ole ostettu mainoksia pois
				if(nayta_mainos_kaynnistyessa) {
					bool nayta_mainos=true;
					if(ala_nayta_mainos_uusille_alussa) { //testataan, nko eka käynnistyskerta -> ei anneta mainosta alussa
						if(!KeyHandling.Detect_Is_Lock_Open (game_key + first_time_started_message, first_time_key)) {
							nayta_mainos=false;
							PlayerPrefs.SetString(game_key + first_time_started_message, first_time_key);
						}
					}
					if(nayta_mainos) {
						Kokoruudun_Mainos_Voidaan_Nayttaa=true; //mainos heti alkuun)
						Kokoruudun_Mainos_Voidaan_Nayttaa_Viivastetysti=true;
					}
				}
				mainpage.layout.Lisaa_Menu_elem (new Button_elem (GUILayout.Button, new GUIContent ("Remove Ads"), "Remove_adds", RemoveAdds, mainpage, guilayout_options_for_buttons, menu_skin.FindStyle ("remove_adds")));
			} else
				nayta_mainokset = false;
			//mainpage.layout.Lisaa_Menu_elem (new Button_elem (GUILayout.Button, new GUIContent ("Restore Purchases"), "restore_purchases", RestorePurchases, mainpage, guilayout_options_for_buttons, menu_skin.FindStyle ("restore_purchases")));
			mainpage.layout.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Quit game"), "Quit_game", Quit_game, mainpage, guilayout_options_for_buttons, menu_skin.FindStyle("quit_button")));
			Horizontal_layout kampanjointi_alue=new Horizontal_layout(new GUIContent(""), mainpage, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.main_page.kampanja_alue), menu_skin.FindStyle("kampanja_alue"));
			mainpage.layout.Lisaa_Menu_elem(kampanjointi_alue);
			kampanjointi_alue.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Facebook"), "Facebook", Facebook, mainpage, guilayout_options_for_buttons, menu_skin.FindStyle("facebook")));
			kampanjointi_alue.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Twitter"), "Twitter", Twitter, mainpage, guilayout_options_for_buttons, menu_skin.FindStyle("twitter")));
		}

		public void KasaaLevelSelectorPage() {
			GUILayout_Options guilayout_options_for_layout=gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_selector_page.layout);
			levelselectorpage=ChildPageFactoryMethod_Support(mainpage, new GUIContent("Start game"), Maarita_pystysuora_layout(guilayout_options_for_layout));
			//completed_levels_crypting=new RSA_Cryptaus("completed_levels_crypting"); //suoritettujen levelien salaaja
			if(level_sets.Count>0) { //jos on useita leveleitä (ja settejä)
				int first_level_id=1;
				level_sets.ForEach(delegate(Level_Set obj) {
					//levelselectorpage.layout.Lisaa_Menu_elem(new Label_elem(GUILayout.Label, new GUIContent("Select level:"), null, levelselectorpage, menu_skin.FindStyle("Label")));
					ChildPage child=ChildPageFactoryMethod(levelselectorpage, new GUIContent(obj.nimi), Maarita_pystysuora_layout(guilayout_options_for_layout));
					bool on_lukittuja_leveleita=KasaaLevelSelectionGrid(first_level_id, obj, child); //kasaa gridin: level_selection_grid
					Horizontal_layout ostonappula_alue=new Horizontal_layout(new GUIContent(""), child, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_selector_page.ostonappula_alue), menu_skin.FindStyle("level_ostonappula_alue"));
					child.layout.Lisaa_Menu_elem(ostonappula_alue);
					if(obj.maksullinen) {
						if(!Detect_is_level_completed(level_selection_grid, obj.Anna_eka_level_id())) //jos ei eka level ole auki, lisätään ostonappi
							ostonappula_alue.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Unlock World"), "unlock_" + obj.nimi, UnlockWorld, child, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_selector_page.ostonappulat), menu_skin.FindStyle("remove_adds")));
					}
					if(on_lukittuja_leveleita) { //lisätään nappula, jolla voi poistaa kaikki lukitukset
						ostonappula_alue.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Unlock Levels"), "unlock_levels" + obj.nimi, Unlock_LockedLevels, child, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_selector_page.ostonappulat), menu_skin.FindStyle("remove_adds")));
					}
					//ScrollView_AutomaticLayout scrollview=new ScrollView_AutomaticLayout(Vector2.zero, true, true, child, null, menu_skin.FindStyle("scroll_bar"));
					//scrollview.Lisaa_Menu_elem(level_selection_grid);
					child.layout.Lisaa_Menu_elem(level_selection_grid);
					first_level_id+=obj.level_lkm;
				});
			} else { //vain yksi level -> tehdään tästä sivusta sellainen, joka sivunvaihdon yhteydessä käynnistää sen (ekan) levelin
				levelselectorpage.AsetaSivunvaihtoOperaatio(StartFirstLevel);
			}
		}

		//palauttaa true, mikäli on lukittuja leveleitä
		public bool KasaaLevelSelectionGrid(int first_level_id, Level_Set level_set, ChildPage page=null, bool ala_kasaa=false) {
			if(!ala_kasaa) {
				level_selection_grid=new SelectionGrid_elem(levelselection_grid_xCount, levelselectorpage, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_selector_page.level_selection_grid), menu_skin.FindStyle("level_selection"));
				((ToolBar_elem)level_selection_grid).selection_elems.Clear();
//				level_selection_grid=new SelectionGrid_elem(levelselection_grid_xCount, levelselectorpage, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_selector_page.level_selection_grid), menu_skin.FindStyle("level_selection"));
				level_set.AsetaTaydennettavatTiedot(level_selection_grid, page, first_level_id);
			} else {
				//palautetaan vain se tallessa oleva grid, jotta saadaan kaikki tiedot tässä funktiossa
				level_selection_grid=level_set.Anna_SelectionGrid_elem();
			}
			//lisätään levelit
			bool on_lukittuja=false;
			for(int level_id=first_level_id; level_id<(first_level_id+level_set.level_lkm); level_id++) {
				if(level_id>(Application.levelCount-first_level_index))
					Debug.LogError("Liian suuri levelin id annettu: " + level_id);
				else {
					if(Detect_is_level_completed(level_selection_grid, level_id))
						level_selection_grid.AddToolbarButton(new GUIContent(level_id.ToString()), "level_" + level_id.ToString(), StartSelectedLevel);
					else {
						level_selection_grid.AddToolbarButton(new GUIContent(level_id.ToString(), locked_level_image), "level_" + level_id.ToString(), StartSelectedLevel);
						on_lukittuja=true;
					}
				}
			}
			return on_lukittuja;
		}

		public void PaivitaLevelSelectionGrid(int level_id) {
			//haetaan levelin sivun indeksi
			int levels_lkm=0;
			int ind=0;
			int first_level_id=1;
			level_sets.ForEach(delegate(Level_Set obj) {
				levels_lkm+=obj.level_lkm;
				if(level_id>levels_lkm) { //ei vielä tämä
					ind++;
					first_level_id+=obj.level_lkm;
				}
			});
			SelectionGrid_elem old_elem = level_sets[ind].Anna_SelectionGrid_elem();
			KasaaLevelSelectionGrid(first_level_id, level_sets[ind]); //kasaa gridin: level_selection_grid
			level_sets[ind].Anna_level_selection_page().layout.Replace_Menu_elem(old_elem, level_selection_grid);
		}

		public void KasaaPausePage() {
			pausepage=ChildPageFactoryMethod_Support(mainpage, new GUIContent("Resume game"), Maarita_vaakasuora_layout(gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.pause_page.layout)));
			GUILayout_Options guilayout_options_for_buttons=gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.pause_page.buttons);
			pausepage.layout.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Resume"), "Resume", Resume, pausepage, guilayout_options_for_buttons, menu_skin.FindStyle("resume_button")));
			pausepage.layout.Lisaa_Menu_elem(Get_new_Play_another_level_button(pausepage, guilayout_options_for_buttons));
		}
		public void LisaaLisaaLevelSelector_and_PausePageGroup(Texture p_locked_level_image) {
			locked_level_image=p_locked_level_image;
			KasaaLevelSelectorPage();
			levelselectorpage_and_pausepage=ChildPageGroupFactoryMethod_where_Parent_is_Mainpage(levelselectorpage, menu_skin.FindStyle("start_button"));
			KasaaPausePage();
			levelselectorpage_and_pausepage.AddPage(pausepage, menu_skin.FindStyle("resume_game_button"));
		}

		public LevelinTulosMittaaja levelin_tulosmittaaja_LevelCompletePage;
		public void LisaaLevelCompletePage() {
			levelcompletepage=MainPageFactoryMethod(null, new GUIContent("Level complete"), Maarita_pystysuora_layout(gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_complete_page.layout)));
			GUILayout_Options guilayout_options_for_buttons=gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_complete_page.buttons);
			LevelinTulosMittaaja_Rakentaja tulosmittaaja_rakentaja=new LevelinTulosMittaaja_Rakentaja();
			levelin_tulosmittaaja_LevelCompletePage=tulosmittaaja_rakentaja.Rakenna(levelcompletepage, guilayout_options_maaritykset.level_complete_page, menu_skin.FindStyle("tulos_alue_levelcomplete"), level_completed_valinnat.tulosmittaaja_asetukset, pistemittariresurssit);
			levelcompletepage.layout.Lisaa_Menu_elem(Get_new_Play_another_level_button(levelcompletepage, guilayout_options_for_buttons));
			levelcompletepage.layout.Lisaa_Menu_elem(Get_new_Play_again_button(levelcompletepage, guilayout_options_for_buttons));
			play_next_level_button=new Button_elem(GUILayout.Button, new GUIContent("Next level"), "Play_next_level", Play_next_level, levelcompletepage, guilayout_options_for_buttons, menu_skin.FindStyle("play_next_level_button"));
			current_showed_changeable_elem_in_levelcompletepage=play_next_level_button;
			levelcompletepage.layout.Lisaa_Menu_elem(play_next_level_button);
			congratulations_label=new Label_elem(GUILayout.Label, new GUIContent(game_completed_message), gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_complete_page.congratulations_label), levelcompletepage, menu_skin.FindStyle("Label"));
		}

		public LevelinTulosMittaaja levelin_tulosmittaaja_LevelFailedPage;
		public void LisaaLevelFailedPage() {
			levelfailedpage=MainPageFactoryMethod(null, new GUIContent("Level failed"), Maarita_vaakasuora_layout(gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_failed_page.layout)));
			GUILayout_Options guilayout_options_for_buttons=gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.level_failed_page.buttons);
			LevelinTulosMittaaja_Rakentaja tulosmittaaja_rakentaja=new LevelinTulosMittaaja_Rakentaja();
			levelin_tulosmittaaja_LevelFailedPage=tulosmittaaja_rakentaja.Rakenna(levelfailedpage, guilayout_options_maaritykset.level_failed_page, menu_skin.FindStyle("tulos_alue_levelfailed"), level_failed_valinnat.tulosmittaaja_asetukset, pistemittariresurssit);
			if(level_sets.Count>0) { //jos on useita leveleitä (ja settejä)
				levelfailedpage.layout.Lisaa_Menu_elem(Get_new_Play_another_level_button(levelfailedpage, guilayout_options_for_buttons));
			}
			levelfailedpage.layout.Lisaa_Menu_elem(Get_new_Play_again_button(levelfailedpage, guilayout_options_for_buttons));
			if(level_sets.Count==0) { //jos on vain 1 level
				levelfailedpage.layout.Lisaa_Menu_elem(new Button_elem(GUILayout.Button, new GUIContent("Main Menu"), "Return to Main Menu", Return_to_MainMenu, levelfailedpage, guilayout_options_for_buttons, menu_skin.FindStyle("return_to_mainmenu_button")));
			}
		}

		public void LisaaOptionsPage() {
			optionspage=ChildPageFactoryMethod_where_Parent_is_Mainpage(new GUIContent("Options"), Maarita_pystysuora_layout(gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.options_page.layout)), menu_skin.FindStyle("options_button"));
			sound_button=new Button_elem(null, new GUIContent("Sound"), "Sound", Sound, optionspage, gUILayout_Options_rakentaja.Rakenna(guilayout_options_maaritykset.options_page.buttons), menu_skin.FindStyle("sound_selector"));
			sound_button.ohjaus_elem.LisaaHyvaksyttyKosketus(TouchPhase.Ended); //reagoi molempiin muutoksiin
			optionspage.layout.Lisaa_Menu_elem(sound_button);
			//luetaan tallennettu tila ja asetetaan se lomakkeelle
			if(PlayerPrefs.HasKey("sound")) {
				if(PlayerPrefs.GetInt("sound")==1)
					sound_button.tila=true;
				else
					sound_button.tila=false;
			} else
				sound_button.tila=true;
			sound_button.edellinen_tila=sound_button.tila;
			Sound(null);
			//optionspage.layout.Lisaa_Menu_elem(new Button_elem(null, new GUIContent("Music"), "Music", Music, optionspage, null, menu_skin.FindStyle("music_selector")));
		}

		//************* toimintoja **************
		//start level
		public void StartLevel(int level_id) {
			level_laskuri++;
			Application.LoadLevel(level_id-1+first_level_index);
		}
		//detect level is completed
		public bool Detect_is_level_completed(SelectionGrid_elem level_selection_grid, int level_id) {
			Level_Set level_set=null;
			int level_set_ind=AnnaLevelSet_Ind(level_selection_grid, ref level_set);
			if(level_id==1)
				return true; //eka level on auki
			else {
				//luetaan levelin message, jos löytyy
				string this_level_message=level_message + level_id.ToString();
				return KeyHandling.Detect_Is_Lock_Open(this_level_message, level_set.GetKey(level_id-level_set.Anna_eka_level_id()));
			}
		}
		//Get_level_best_time
		//palauttaa true, mikäli levelillä on aika
		public bool Get_level_best_time(int level_id, ref float best_time) {
			//luetaan levelin message, jos löytyy
			string this_level_message=level_message + level_id.ToString() + level_best_time_message;
			if(PlayerPrefs.HasKey(this_level_message)) {
				best_time=PlayerPrefs.GetFloat(this_level_message);
				return true;
			}
			return false;
		}
		//Get_level_best_points
		//palauttaa true, mikäli levelillä on pisteet
		public bool Get_level_best_points(int level_id, ref int best_points) {
			//luetaan levelin message, jos löytyy
			string this_level_message=level_message + level_id.ToString() + level_best_points_message;
			if(PlayerPrefs.HasKey(this_level_message)) {
				best_points=PlayerPrefs.GetInt(this_level_message);
				return true;
			}
			return false;
		}
		//valmistelee levelin valmiiksi käynnistettäväksi
		public void Preparing_Level() {
			menu.current_page.AsetaSisaltoVaihtoon(null);
			Kokoruudun_Mainos_Voidaan_Nayttaa=false;
			if(game_paused) UnPauseGame_first_step(false);
		}
		//set to in-game state
		public void Set_to_inGame_State() {
			menu.current_page.AsetaSisaltoVaihtoon(null);
			Kokoruudun_Mainos_Voidaan_Nayttaa=false;
			if(game_paused) UnPauseGame_first_step(true);
			levelin_hallinta.Start_level();
		}
		//pausetoiminto
		private float savedTimeScale;
		public void PauseGame() {
			if(game_paused==false) {
				savedTimeScale = Time.timeScale;
				Time.timeScale = 0;
			}
			levelin_hallinta.level_kaynnissa=false;
			game_paused=true;
			AudioListener.pause=true;
			menu.current_page.AsetaSisaltoVaihtoon(levelselectorpage_and_pausepage);
			levelselectorpage_and_pausepage.SetDelegate(pausepage);
		}
		//unpause: ensimmäisessä vaiheessa suoritetaan toiminnot, jotka voidaan suorittaa välittömästi
		public void UnPauseGame_first_step(bool set_level_kaynnissa_true) {
			menu.current_page.AsetaSisaltoVaihtoon(null);
			set_level_kaynnissa_true_when_unpause=set_level_kaynnissa_true;
			menu.SetUnpause_handling(UnPauseGame_last_step); //viimeistellään viiveellä, että ehditään suodattaa pois resumeen osunut kosketus (android)
		}
		//suoritetaan viiveellä, että (esim.) ehditään suodattaa pois resumeen osunut kosketus (android)
		public void UnPauseGame_last_step() {
			Time.timeScale = savedTimeScale;
			AudioListener.pause=false;
			if(set_level_kaynnissa_true_when_unpause) //jos on asetettu laittamaan leveli käyntiin heti
				levelin_hallinta.level_kaynnissa=true;
			game_paused=false;
		}
		//level complete
		public bool LevelComplete(bool testaa_vain_voidaanko_seuraavaa_avata=false) {
			if(!testaa_vain_voidaanko_seuraavaa_avata) { //testaamalla ei tehdä mitään toimenpiteitä, ei tässä eikä myöhemmin
				menu.current_page.AsetaSisaltoVaihtoon(levelcompletepage);
				NaytankoLevelOhiMainos(); //koko ruudun mainos mahdolliseksi, jos laskuri tuli täyteen
			}
			//päivitetään levelien lukitus ja levelcompletesivu
			Level_Set level_set_of_this_level=null;
			Level_Set level_set_of_next_level=null;
			if(testaa_vain_voidaanko_seuraavaa_avata & level_selection_grid==null) //ei pystytä selvittämään (menua ei ehkä rakennettu)
				return false;
			int level_set_ind_of_this_level=AnnaLevelSet_Ind(level_selection_grid, ref level_set_of_this_level);
			//Debug.Log("level_set_of_this_level: " + level_set_of_this_level.nimi);
			int level_id_of_next_level=Application.loadedLevel+2-first_level_index;
			string key_of_next_level="";
			bool seuraava_level_aukaistaan=false;
			if((level_id_of_next_level-level_set_of_this_level.Anna_eka_level_id())<level_set_of_this_level.level_lkm) { //jos level_set ei vaihdu
				seuraava_level_aukaistaan=true;
				level_set_of_next_level=level_set_of_this_level;
				key_of_next_level=level_set_of_this_level.GetKey(level_id_of_next_level-level_set_of_this_level.Anna_eka_level_id());
				if(!testaa_vain_voidaanko_seuraavaa_avata) {
					levelcompletepage.layout.Replace_Menu_elem(current_showed_changeable_elem_in_levelcompletepage, play_next_level_button);
					current_showed_changeable_elem_in_levelcompletepage=play_next_level_button;
				}
			} else if((level_set_ind_of_this_level+1)==level_sets.Count) { //viimeinenkin maailma suoritettu
				if(!testaa_vain_voidaanko_seuraavaa_avata) {
					//congratulations_label.gui_content.text=game_completed_message;
					congratulations_label.gui_content.text=level_set_of_this_level.nimi + " Completed!";
					levelcompletepage.layout.Replace_Menu_elem(current_showed_changeable_elem_in_levelcompletepage, congratulations_label);
					current_showed_changeable_elem_in_levelcompletepage=congratulations_label;
				}
			} else { //maailma suoritettu (ei ollut viimeinen)
				level_set_of_next_level=level_sets[level_set_ind_of_this_level+1];
				if(!level_sets[level_set_ind_of_this_level+1].maksullinen) { //jos ei ole maksullinen
					seuraava_level_aukaistaan=true;
					key_of_next_level=level_sets[level_set_ind_of_this_level+1].GetKey(level_id_of_next_level-level_sets[level_set_ind_of_this_level+1].Anna_eka_level_id());
				}
				if(!testaa_vain_voidaanko_seuraavaa_avata) {
					congratulations_label.gui_content.text=level_set_of_this_level.nimi + " Completed!";
					levelcompletepage.layout.Replace_Menu_elem(current_showed_changeable_elem_in_levelcompletepage, congratulations_label);
					current_showed_changeable_elem_in_levelcompletepage=congratulations_label;
				}
			}
			if(!testaa_vain_voidaanko_seuraavaa_avata) {
				//Debug.Log("level_set_of_next_level: " + level_set_of_next_level.nimi);
				Debug.Log("seuraava_level_aukaistaan: " + seuraava_level_aukaistaan.ToString());
				if(seuraava_level_aukaistaan) {
					UnlockLevel(level_set_of_next_level, level_id_of_next_level, key_of_next_level);
				}
				//päivitetään tiedot uudesta tuloksesta
				if(levelin_tulosmittaaja_LevelCompletePage!=null) {
					int level_id_of_this_level=level_id_of_next_level-1;
					levelin_tulosmittaaja_LevelCompletePage.AsetaUusiTulos(level_id_of_this_level);
				}
			}
			return seuraava_level_aukaistaan;
		}
		//level_completed-valinnat
		public Level_completed_Valinnat_Parametrit_Hallintapaneeliin level_completed_valinnat=null;
		public void Aseta_LevelCompleted_Valinnat(Level_completed_Valinnat_Parametrit_Hallintapaneeliin p_level_completed_valinnat)  {
			level_completed_valinnat=p_level_completed_valinnat;
		}
		public void UnlockLevel(Level_Set level_set, int level_id, string key) {
			if(!Detect_is_level_completed(level_set.Anna_SelectionGrid_elem(), level_id)) { //jos ei jo ole aukaistu
				string this_level_message=level_message + level_id.ToString();
				
				Debug.Log("Aukaistaan lukitus: " + this_level_message);
				
				/*ByteArray cryptattu=completed_levels_crypting.Encrypt(this_level_message);
						PlayerPrefs.SetString(this_level_message, cryptattu.ToString());
						*/
				PlayerPrefs.SetString(this_level_message, key);
				//PlayerPrefs.SetString(this_level_message, this_level_message);
				
				PaivitaLevelSelectionGrid(level_id); //päivitetään level_selection_grid -> kasaa gridin: level_selection_grid
			} else
				Debug.Log("Lukitus on jo avattu");
		}
		//level failed
		//palauttaa true, mikäli viivästetty resume on asetettu
		public bool LevelFailed() {
			if(skip_level_hallinta!=null)
				skip_level_hallinta.TestaaYritykset(); //testataan, laitetaanko skip level nappi näkyville
			if(level_failed_valinnat.toiminto==Level_failed_Valinnat_Parametrit_Hallintapaneeliin.Toiminto.nayta_level_failed_sivu) {
				NaytankoLevelOhiMainos(); //koko ruudun mainos mahdolliseksi, jos laskuri tuli täyteen
				menu.current_page.AsetaSisaltoVaihtoon(levelfailedpage);
				//päivitetään tiedot uudesta tuloksesta
				if(levelin_tulosmittaaja_LevelFailedPage!=null) {
					levelin_tulosmittaaja_LevelFailedPage.AsetaUusiTulos(Application.loadedLevel+1-first_level_index);
				}
				return false;
			} else if(level_failed_valinnat.toiminto==Level_failed_Valinnat_Parametrit_Hallintapaneeliin.Toiminto.resume) {
				NaytankoLevelOhiMainos();
				return true; //kutsuja vastaa viivästetyn resumen suorittamisesta
			}
			return false;
		}
		//level_failed-valinnat
		public Level_failed_Valinnat_Parametrit_Hallintapaneeliin level_failed_valinnat=null;
		public void Aseta_LevelFailed_Valinnat(Level_failed_Valinnat_Parametrit_Hallintapaneeliin p_level_failed_valinnat)  {
			level_failed_valinnat=p_level_failed_valinnat;
		}
		public void PoistaMainokset() { //poistaa mainokset käytöstä ja lataa nykyisen levelin (menu) uudestaan
			PlayerPrefs.SetString(game_key + noads_message, noads_key);
			nayta_mainokset = false;
		}
		// Ostotoiminnot **************************
		public string ostonappulan_alkuperainen_teksti;		
		//ostotapahtumissa välilettävät tiedot
		public bool ostotapahtuma_kaynnistetty=false; //viesti OpenIAB:lle
		public bool ostotapahtuma_kesken=false; //estää muut ostotapahtumat
		public bool restore_purchases_kaynnistetty=false; //viesti OpenIAB:lle
		public string SKU_to_purchace; //viesti OpenIAB:lle
		public Level_Set dest_level_set; //kohde
		public Menu_elem_container dest_menu_elem_container;
		public Button_elem ostonappi;
		//käynnistää ostotapahtuman
		public void Purchace_SKU(string p_SKU_to_purchace, Button_elem p_ostonappi=null) {
			if(!ostotapahtuma_kesken) { //jos ei ole jo ostotapahtumaa käynnissä
				ostotapahtuma_kaynnistetty=true;
				ostotapahtuma_kesken=true;
				SKU_to_purchace=p_SKU_to_purchace;
				if(p_ostonappi==null) //otetaan ostonappi juuri piirretystä menu_elemistä
					ostonappi=(Button_elem)menu.current_page.sisalto.current_Menu_elem;
				else
					ostonappi=p_ostonappi;
			}
		}
		//palauttaa ostonapin tekstin alkuperäiseksi
		public void PalautaOstonappula() {
			ostonappi.gui_content.text=ostonappulan_alkuperainen_teksti;
			ostotapahtuma_kesken=false;
		}
		//vaihtaa ostonappulan tekstin hetkeksi "try again"
		public void Vaihda_TryAgain() {
			ostonappulan_alkuperainen_teksti=ostonappi.gui_content.text;
			ostonappi.gui_content.text="Try again!";
			ostotapahtuma_kesken=true;
		}
		public void Ostettu_NoAds() {
			PoistaMainokset();
			dest_menu_elem_container.menu_elems.Remove (ostonappi);
		}

		public void Ostettu_Maailma() {
			UnlockLevel(dest_level_set, dest_level_set.Anna_eka_level_id(), dest_level_set.GetKey(0));
			if(KasaaLevelSelectionGrid(dest_level_set.Anna_eka_level_id(), dest_level_set, null, true)) //jos jää lukittuja leveleitä
				dest_menu_elem_container.menu_elems.Remove(ostonappi); //poistetaan vain ekan levelin aukaisija
			else
				dest_menu_elem_container.menu_elems.Clear(); //poistetaan myös kaikkien leveleiden lukituksen aukaisija
		}

		public void Ostettu_LukkojenAukaisija() {
			int level_id=dest_level_set.Anna_eka_level_id();
			for(int i=0; i<dest_level_set.level_lkm; i++) {
				UnlockLevel(dest_level_set, level_id++, dest_level_set.GetKey(i));
			}
			dest_menu_elem_container.menu_elems.Clear();
		}
		
		public void Ostettu_SkipLevel() {
			if(Level_hallinta.Level_hallinta.Instance.levelin_kello!=null)
				if(Level_hallinta.Level_hallinta.Instance.levelin_kello.aika<999)
					Level_hallinta.Level_hallinta.Instance.levelin_kello.aika=999;
			Level_hallinta.Level_hallinta.Instance.LevelComplete(true);
			//ostonappi.AsetaNakyvyys(false);
		}
		//mainosrajapinta **********************
		public bool Kokoruudun_Mainos_Voidaan_Nayttaa=false; //mainostaja saa tästä viestin, milloin voidaan näyttää mainos
		public bool Kokoruudun_Mainos_Voidaan_Nayttaa_Viivastetysti=false; //mainostaja saa tästä viestin, että mainos voidaan näyttää viivästetysti
		public bool Mainosbanneri_Voidaan_Nayttaa=false;
		private int level_laskuri=0;

		public bool NaytankoLevelOhiMainos() {
			if(nayta_mainokset & level_laskuri>=mainos_lkm_levelin_jalkeen) {
				Kokoruudun_Mainos_Voidaan_Nayttaa=true;
				level_laskuri=0;
				return true;
			}
			return false;
		}
		// ************ toimintojen suorittajat ************************
		private void RemoveAdds(Ohjaustapahtuma kosketus) {
			dest_menu_elem_container = menu.current_page.sisalto.layout.sisalto.menu_elem_container;
			Purchace_SKU("noads");
		}
		private void RestorePurchases(Ohjaustapahtuma kosketus) {
			restore_purchases_kaynnistetty = true;
		}
		private void UnlockWorld(Ohjaustapahtuma kosketus) {
			Purchace_SKU("world" + (AnnaOstetunMaailman_Level_set_ind()+1).ToString());
		}
		private void Unlock_LockedLevels(Ohjaustapahtuma kosketus) {
			AnnaOstetunMaailman_Level_set_ind(); //tallentaa tarvittavat tiedot
			Purchace_SKU("skip_world");
		}
		private int AnnaOstetunMaailman_Level_set_ind() {
			//palauttaa ostetun (esillä olevan) maailman level_set_ind:n ja tallentaa tarvittavat tiedot ostotapahtuman suorittamista varten
			dest_menu_elem_container=((Horizontal_layout)menu.current_page.sisalto.layout.sisalto.menu_elem_container.menu_elems[0]).menu_elem_container;
			level_selection_grid=(SelectionGrid_elem)menu.current_page.sisalto.layout.sisalto.menu_elem_container.menu_elems[menu.current_page.sisalto.layout.sisalto.menu_elem_container.menu_elems.Count-1];
			return AnnaLevelSet_Ind(level_selection_grid, ref dest_level_set);
		}
		private void Quit_game(Ohjaustapahtuma kosketus) {
			Application.Quit();
		}
		private void Facebook(Ohjaustapahtuma kosketus) {
			Application.OpenURL("https://www.facebook.com/pages/Wildman-Games/239995892852363");
		}
		private void Twitter(Ohjaustapahtuma kosketus) {
			Application.OpenURL("https://twitter.com/WildmanGames");
		}
		private void Sound(Ohjaustapahtuma kosketus) {
			if(sound_button.tila==false)
				AudioListener.volume=0;
			else if(sound_button.tila==true)
				AudioListener.volume=1;
			PlayerPrefs.SetInt("sound", (int) AudioListener.volume); //tallennetaan asetus
		}
		private void Music(Ohjaustapahtuma kosketus) {
			
		}
		//palauttaa level setin indeksin, jossa levelin valintalista on
		private int AnnaLevelSet_Ind(SelectionGrid_elem level_selection_grid, ref Level_Set level_set) {
			int ind=level_sets.FindIndex(delegate(Level_Set obj) {
				return level_selection_grid.Equals(obj.Anna_SelectionGrid_elem());
			});
			level_set=level_sets[ind];
			return ind;
		}
		//käytetään, jos pelissä on vain yksi level
		private void StartFirstLevel(Ohjaustapahtuma kosketus) {
			StartLevel(1);
		}
		private void StartSelectedLevel(Ohjaustapahtuma kosketus) {
			//haetaan level_id
			level_selection_grid=(SelectionGrid_elem) menu.current_page.sisalto.current_Menu_elem;
			Level_Set level_set=null;
			int ind=AnnaLevelSet_Ind(level_selection_grid, ref level_set);
			int level_id=level_selection_grid.selected+level_sets[ind].Anna_eka_level_id();

			Debug.Log("level_id: " + level_id);

			if(Detect_is_level_completed(level_selection_grid, level_id) | avaa_lukitutkin_levelit) {
				Debug.Log("Start level " + level_id);
				level_selection_grid.selected=-1;
				level_selection_grid.edel_selected=-1;
				StartLevel(level_id);
			} else {
				//Kokoruudun_Mainos_Voidaan_Nayttaa=true; //paskiaiset!!!!!!
				Debug.Log("Level is locked!");
			}
		}
		private void Resume(Ohjaustapahtuma kosketus) {
			UnPauseGame_first_step(true);
		}
		private void Play_another_level(Ohjaustapahtuma kosketus) {
			menu.current_page.AsetaSisaltoVaihtoon(levelselectorpage_and_pausepage);
			levelselectorpage_and_pausepage.SetDelegate(levelselectorpage);
		}
		private void Play_next_level(Ohjaustapahtuma kosketus) {
			if(Application.loadedLevel<(Application.levelCount-1)) {
				Debug.Log("Play_next_level");
				StartLevel(Application.loadedLevel+1);
			} else
				Debug.Log("No more levels!");
		}
		public void Play_again(Ohjaustapahtuma kosketus) {
			Debug.Log("Play again");
			StartLevel(Application.loadedLevel);
		}
		public void Return_to_MainMenu(Ohjaustapahtuma kosketus) {
			Debug.Log("Return to MainMenu");
			menu.current_page.AsetaSisaltoVaihtoon(mainpage);
		}
	}

	public class KeyHandling {
		public static bool Detect_Is_Lock_Open(string _lock, string key) {
			if(PlayerPrefs.HasKey(_lock)) {
				/*ByteArray crypted_key=new ByteArray();
					crypted_key.Set_from_strBytes(PlayerPrefs.GetString(_lock));
					string decrypted_key=completed_levels_crypting.Decrypt(crypted_key);
					*/
				
				string decrypted_key=PlayerPrefs.GetString(_lock);
				
				if(decrypted_key.CompareTo(key)==0)
					return true;
			}
			return false;
		}
	}

	//pause-nappula-rajapinta
	public class PausenappulaRajapinta : Ohjauslaitteen_Kayttaja {
		public Button_elem pause_nappula;
		public MenuFactory menufactory=null;

		public PausenappulaRajapinta(Ohjaus_laite.Ohjaus_laite p_ohjaus_laite, Button_elem p_pause_nappula) :base(p_ohjaus_laite) {
			pause_nappula=p_pause_nappula;
			menufactory=MenuFactory.Instance;
		}

		public override string ToString ()
		{
			return string.Format ("[PausenappulaRajapinta]");
		}
		
		//asettaa pausen, mikäli nappia on painettu
		//ohjausnappuloiden tilat on tunnistettu aiemmin
		public override void SuoritaToiminnot() {
			if(pause_nappula.tila)
				menufactory.PauseGame();
		}
	}

	//Skip Level
	public class Skip_Level {
		public uint vaaditut_yritykset;
		public SkipLevel_Nappula_Rajapinta nappula_rajapinta;
		public Level_hallinta.Level_hallinta levelin_hallinta;
		public MenuFactory menufactory;
		
		public Skip_Level(uint p_vaaditut_yritykset, SkipLevel_Nappula_Rajapinta p_nappula_rajapinta) {
			vaaditut_yritykset=p_vaaditut_yritykset;
			nappula_rajapinta=p_nappula_rajapinta;
			levelin_hallinta=Level_hallinta.Level_hallinta.Instance;
			menufactory=MenuFactory.Instance;
			//piilotetaanko nappula vai näkyville?
			TestaaYritykset();
		}

		public void TestaaYritykset() {
			if(menufactory.LevelComplete(true) & levelin_hallinta.levelia_yritetty_lkm>=vaaditut_yritykset)
				nappula_rajapinta.skip_level_nappula.AsetaNakyvyys(true);
			else
				nappula_rajapinta.skip_level_nappula.AsetaNakyvyys(false);
		}
		
		public class SkipLevel_Nappula_Rajapinta : Ohjauslaitteen_Kayttaja {
			public Button_elem skip_level_nappula;
			public MenuFactory menufactory;
			
			public SkipLevel_Nappula_Rajapinta(Ohjaus_laite.Ohjaus_laite p_ohjaus_laite, Button_elem p_skip_level_nappula) :base(p_ohjaus_laite) {
				skip_level_nappula=p_skip_level_nappula;
				menufactory=MenuFactory.Instance;
			}
			
			public override string ToString ()
			{
				return string.Format ("[SkipLevel_Nappula_Rajapinta]");
			}			
			
			//skippaa levelin, mikäli nappia on painettu
			//ohjausnappuloiden tilat on tunnistettu aiemmin
			public override void SuoritaToiminnot() {
				if(skip_level_nappula.tila) {
					Debug.Log("skip_level: " + Application.loadedLevelName);
					menufactory.Purchace_SKU("skiplevel", skip_level_nappula);
				}
			}
		}
	}

	//hallitsee levelin suorituksen hyvyyden mittausta
	//saa levelin suorituksen suoritusajan tai pisteet
	//näyttää ne
	//päivittää parhaan ajan tai pisteet
	public class LevelinTulosMittaaja {
		public Horizontal_layout tulos_alue;
		public Label_elem uusi_tulos;
		public Label_elem paras_tulos;
		public MittausSuure mittaussuure;

		public LevelinTulosMittaaja(MainPage kohdesivu, GUILayout_Options_Maaritykset.LevelCompleteOrFailedPage GUILayout_options_maaritykset_tulosalue, GUIStyle tulos_alue_GUIStyle, string uusi_tulos_label, string paras_tulos_label, MittausSuure p_mittaussuure) {
			tulos_alue=new Horizontal_layout(new GUIContent(""), kohdesivu, MenuFactory.Instance.gUILayout_Options_rakentaja.Rakenna(GUILayout_options_maaritykset_tulosalue.tulos_alue.area_container), tulos_alue_GUIStyle);
			kohdesivu.layout.Lisaa_Menu_elem(tulos_alue);
			tulos_alue.Lisaa_Menu_elem(new Label_elem(GUILayout.Label, new GUIContent(uusi_tulos_label), MenuFactory.Instance.gUILayout_Options_rakentaja.Rakenna(GUILayout_options_maaritykset_tulosalue.tulos_alue.uusi_tulos_label), kohdesivu, MenuFactory.Instance.menu_skin.FindStyle("uusi_tulos_label")));
			uusi_tulos=new Label_elem(GUILayout.Label, new GUIContent(""), MenuFactory.Instance.gUILayout_Options_rakentaja.Rakenna(GUILayout_options_maaritykset_tulosalue.tulos_alue.uusi_tulos), kohdesivu, MenuFactory.Instance.menu_skin.FindStyle("uusi_tulos"));
			tulos_alue.Lisaa_Menu_elem(uusi_tulos);
			tulos_alue.Lisaa_Menu_elem(new Label_elem(GUILayout.Label, new GUIContent(paras_tulos_label), MenuFactory.Instance.gUILayout_Options_rakentaja.Rakenna(GUILayout_options_maaritykset_tulosalue.tulos_alue.paras_tulos_label), kohdesivu, MenuFactory.Instance.menu_skin.FindStyle("paras_tulos_label")));
			paras_tulos=new Label_elem(GUILayout.Label, new GUIContent(""), MenuFactory.Instance.gUILayout_Options_rakentaja.Rakenna(GUILayout_options_maaritykset_tulosalue.tulos_alue.paras_tulos), kohdesivu, MenuFactory.Instance.menu_skin.FindStyle("paras_tulos"));
			tulos_alue.Lisaa_Menu_elem(paras_tulos);
			mittaussuure=p_mittaussuure;
		}

		public void AsetaUusiTulos(int level_id_of_this_level) {
			uusi_tulos.gui_content.text=mittaussuure.PalautaTulos();
			mittaussuure.HaeVanhaParasTulos(level_id_of_this_level);
			paras_tulos.gui_content.text=mittaussuure.PalautaJaPaivitaParasTulos(level_id_of_this_level);
		}

		public abstract class MittausSuure {
			public abstract string PalautaTulos();
			public abstract void HaeVanhaParasTulos(int level_id_of_this_level);
			public abstract string PalautaJaPaivitaParasTulos(int level_id_of_this_level);
		}
		public class AikaMittari : MittausSuure {
			public float new_time;
			public float best_time;

			public override string PalautaTulos() {
				new_time=Level_hallinta.Level_hallinta.Instance.levelin_kello.aika;
				return Level_hallinta.Level_hallinta.Instance.levelin_kello.MuotoileAika(new_time);
			}

			public override void HaeVanhaParasTulos(int level_id_of_this_level) {
				if(!MenuFactory.Instance.Get_level_best_time(level_id_of_this_level, ref best_time)) {
					best_time=new_time+0.01f; //päivittyy samaksi myöhemmin
				}
			}

			public override string PalautaJaPaivitaParasTulos(int level_id_of_this_level) {
				if(new_time<best_time) {
					best_time=new_time;
					string this_level_message=MenuFactory.Instance.level_message + level_id_of_this_level.ToString() + MenuFactory.Instance.level_best_time_message; //päivitetään paraempi aika
					PlayerPrefs.SetFloat(this_level_message, best_time);
				}
				return Level_hallinta.Level_hallinta.Instance.levelin_kello.MuotoileAika(best_time);
			}
		}
		public class PisteMittari : MittausSuure {
			public PistemittariResurssit resurssit=null;
			public int new_points;
			public int best_points;

			public void AsetaResurssit(PistemittariResurssit p_resurssit) {
				resurssit=p_resurssit;
			}

			public override string PalautaTulos() {
				new_points=resurssit.pisteiden_palauttaja();
				Debug.Log("PalautaTulos: " + new_points);
				return resurssit.pisteiden_muotoilija((float)new_points);
			}

			public override void HaeVanhaParasTulos(int level_id_of_this_level) {
				if(!MenuFactory.Instance.Get_level_best_points(level_id_of_this_level, ref best_points)) {
					best_points=new_points-1; //päivittyy samaksi myöhemmin
				}
				Debug.Log("HaeVanhaParasTulos: " + best_points);
			}
			
			public override string PalautaJaPaivitaParasTulos(int level_id_of_this_level) {
				Debug.Log("PalautaJaPaivitaParasTulos, new_points: " + new_points + ", best_points: " + best_points);
				if(new_points>best_points) {
					best_points=new_points;
					string this_level_message=MenuFactory.Instance.level_message + level_id_of_this_level.ToString() + MenuFactory.Instance.level_best_points_message; //päivitetään paremmat pisteet
					PlayerPrefs.SetInt(this_level_message, best_points);
					Debug.Log("Paras paivitetaan: " + this_level_message);
				}
				return resurssit.pisteiden_muotoilija((float)best_points);
			}
		}

		public class PistemittariResurssit {
			public delegate int Pisteiden_palauttaja();
			public Pisteiden_palauttaja pisteiden_palauttaja;
			public delegate string Pisteiden_muotoilija(float pisteet);
			public Pisteiden_muotoilija pisteiden_muotoilija;

			public PistemittariResurssit(Pisteiden_palauttaja p_pisteiden_palauttaja, Pisteiden_muotoilija p_pisteiden_muotoilija) {
				pisteiden_palauttaja=p_pisteiden_palauttaja;
				pisteiden_muotoilija=p_pisteiden_muotoilija;
			}
		}
	}

	//asetukset hallintapaneeliin
	//levelin tulosmittaaja
	[System.Serializable]
	public class LevelinTulosMittaaja_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Kaytossa {
			ei,
			kylla
		}
		public Kaytossa kaytossa;
		[System.Serializable]
		public class Asetukset {
			public string uusi_tulos_label="uusi tulos:";
			public string paras_tulos_label="paras tulos:";
			[System.Serializable]
			public enum Mittaussuure {
				aika,
				pisteet
			}
			public Mittaussuure mittaussuure;
		}
		public Asetukset asetukset;
	}

	//Level failed valinnat
	[System.Serializable]
	public class Level_failed_Valinnat_Parametrit_Hallintapaneeliin {
		[System.Serializable]
		public enum Toiminto {
			nayta_level_failed_sivu,
			resume
		}
		public Toiminto toiminto;
		public float resume_viive;
		public LevelinTulosMittaaja_Parametrit_Hallintapaneeliin tulosmittaaja_asetukset;
	}

	//Level completed valinnat
	[System.Serializable]
	public class Level_completed_Valinnat_Parametrit_Hallintapaneeliin {
		public LevelinTulosMittaaja_Parametrit_Hallintapaneeliin tulosmittaaja_asetukset;
	}

	//rajaa levelit yhdelle sivulle ja määrittää, onko maksullinen
	[System.Serializable]
	public class Level_Set {
		public int level_lkm;
		public bool maksullinen;
		public string nimi;
		//täydennettävät tiedot
		private SelectionGrid_elem level_selection_grid;
		private Page level_selection_page;
		private int eka_level_id;
		//avaimet
		private string[] level_keys;

		public void AsetaTaydennettavatTiedot(SelectionGrid_elem p_level_selection_grid, Page p_level_selection_page, int p_eka_level_id) {
			level_selection_grid=p_level_selection_grid;
			if(p_level_selection_page!=null) //sivua ei tarvitse antaa kuin ekalla kerralla
				level_selection_page=p_level_selection_page;
			eka_level_id=p_eka_level_id;
		}
		public void AsetaLevelienAvaimet(string[] p_level_keys) {
			level_keys=p_level_keys;
			if(level_keys.Length<level_lkm)
				Debug.LogError("Liian vahan levelin avaimia level_set: " + nimi);
		}
		public SelectionGrid_elem Anna_SelectionGrid_elem() {
			return level_selection_grid;
		}
		public Page Anna_level_selection_page() {
			return level_selection_page;
		}
		public int Anna_eka_level_id() {
			return eka_level_id;
		}
		public string GetKey(int level_ind) {
			return level_keys[level_ind];
		}
		public bool OnkoOikeaAvain(string decrypted_level_message, int level_id) {
			//Debug.Log(nimi + ": OnkoOikeaAvain: " + decrypted_level_message + ", level_id: " + level_id + ", level_keys==null: " + (level_keys==null).ToString());
			//Debug.Log("ind: " + (level_id-eka_level_id) + ", koko: " + level_keys.Length);
			return decrypted_level_message.CompareTo(level_keys[level_id-eka_level_id])==0;
		}
	}
	[System.Serializable]
	public class GUILayout_Options_Maaritykset {
		public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> menu_yleisasetukset;
		public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> menu_yleisasetukset_for_navigationcontrols;
		public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> menu_yleisasetukset_for_navigation_area_of_childpages;
		[System.Serializable]
		public class Page {
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> yleisasetukset;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> layout;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> buttons;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> navigation_controls;
		}
		[System.Serializable]
		public class ChildPage : Page {
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> navigation_area;
		}
		[System.Serializable]
		public class MainPage : Page {
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> kampanja_alue;
		}
		public MainPage main_page;
		[System.Serializable]
		public class LevelSelectorPage : ChildPage {
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> ostonappula_alue;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> ostonappulat;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> level_selection_grid;
		}
		public LevelSelectorPage level_selector_page;
		public ChildPage pause_page;
		[System.Serializable]
		public class TulosAlue {
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> area_container;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> uusi_tulos;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> uusi_tulos_label;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> paras_tulos;
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> paras_tulos_label;
		}
		[System.Serializable]
		public class LevelCompleteOrFailedPage : ChildPage {
			public TulosAlue tulos_alue;
		}
		[System.Serializable]
		public class LevelCompletePage : LevelCompleteOrFailedPage {
			public List<GUILayout_Options_Valinnat_Parametrit_Hallintapaneeliin> congratulations_label;
		}
		public LevelCompletePage level_complete_page;
		public LevelCompleteOrFailedPage level_failed_page;
		public ChildPage options_page;
	}

	[System.Serializable]
	public class Skip_Level_asetukset {
		public Kylla_Ei kaytossa;
		public int vaaditut_yritykset;
		public Ohjaus_laite.Parametrit_Hallintapaneeliin nappulan_asetukset;
	}

	// Rakentajat
	//levelin tulosmittaaja
	public class LevelinTulosMittaaja_Rakentaja {
		public LevelinTulosMittaaja Rakenna(MainPage kohdesivu, GUILayout_Options_Maaritykset.LevelCompleteOrFailedPage GUILayout_options_maaritykset_tulosalue, GUIStyle tulos_alue_GUIStyle, LevelinTulosMittaaja_Parametrit_Hallintapaneeliin asetukset, LevelinTulosMittaaja.PistemittariResurssit pistemittariresurssit=null) {
			if(asetukset.kaytossa==LevelinTulosMittaaja_Parametrit_Hallintapaneeliin.Kaytossa.kylla) {
				LevelinTulosMittaaja.MittausSuure mittaussuure=null;
				if(asetukset.asetukset.mittaussuure==LevelinTulosMittaaja_Parametrit_Hallintapaneeliin.Asetukset.Mittaussuure.aika) {
					mittaussuure=new LevelinTulosMittaaja.AikaMittari();
				} else if(asetukset.asetukset.mittaussuure==LevelinTulosMittaaja_Parametrit_Hallintapaneeliin.Asetukset.Mittaussuure.pisteet) {
					mittaussuure=new LevelinTulosMittaaja.PisteMittari();
					((LevelinTulosMittaaja.PisteMittari)mittaussuure).AsetaResurssit(pistemittariresurssit);
				}
				return new LevelinTulosMittaaja(kohdesivu, GUILayout_options_maaritykset_tulosalue, tulos_alue_GUIStyle, asetukset.asetukset.uusi_tulos_label, asetukset.asetukset.paras_tulos_label, mittaussuure);
			} else {
				return null;
			}
		}
	}

	//skip level
	public class Skip_Level_Rakentaja {
		public Skip_Level Rakenna(Skip_Level_asetukset skip_level_asetukset) {
			if(skip_level_asetukset.kaytossa==Kylla_Ei.kylla) {
				Ohjaus_laite.Hallintapaneelista_Rakentaja nappulan_rakentaja=new Ohjaus_laite.Hallintapaneelista_Rakentaja();
				Ohjaus_laite.Ohjaus_laite ohjauslaite=new Ohjaus_laite.Ohjaus_laite("Skip_Level");
				Menu.Button_elem skip_level_button=nappulan_rakentaja.RakennaNappula_Hallintapaneelista(skip_level_asetukset.nappulan_asetukset, ohjauslaite);
				return new Skip_Level((uint)skip_level_asetukset.vaaditut_yritykset, new Skip_Level.SkipLevel_Nappula_Rajapinta(ohjauslaite, skip_level_button));
			} else
				return null;
		}
	}
}
