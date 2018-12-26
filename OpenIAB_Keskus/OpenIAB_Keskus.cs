using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Menu;
using MenuFactory;
using Level_hallinta;

//rajapinta OpenIAB:n ja muiden osien kanssa
//käynnistää tapahtumat ja suorittaa niihin liittyvät toiminnot
namespace OpenIAB_Keskus {
	public class OpenIAB_Keskus {
		
		private  static OpenIAB_Keskus instance; // Singleton
		//  Instance  
		public static OpenIAB_Keskus Instance  
		{    
			get    
			{      
				if (instance ==  null)
					instance = new OpenIAB_Keskus();     
				return instance;   
			}
		}
		
		private OpenIAB_Keskus() {}

		public string ostonappulan_alkuperainen_teksti;

		//ostotapahtumissa välilettävät tiedot
		public bool ostotapahtuma_kaynnistetty=false; //viesti OpenIAB:lle
		public bool ostotapahtuma_kesken=false; //estää muut ostotapahtumat
		public string SKU_to_purchace; //viesti OpenIAB:lle
		public Level_Set dest_level_set; //kohde
		public Menu_elem_container dest_menu_elem_container;
		public Button_elem ostonappi;

		public void Purchace_SKU(string p_SKU_to_purchace, Button_elem p_ostonappi=null) {
			if(!ostotapahtuma_kesken) { //jos ei ole jo ostotapahtumaa käynnissä
				ostotapahtuma_kaynnistetty=true;
				ostotapahtuma_kesken=true;
				SKU_to_purchace=p_SKU_to_purchace;
				if(p_ostonappi==null) //otetaan ostonappi juuri piirretystä menu_elemistä
					ostonappi=(Button_elem)Menu.Menu.Instance.current_page.sisalto.current_Menu_elem;
			}
		}

		public void Ostettu_NoAds() {
			MenuFactory.MenuFactory.Instance.PoistaMainokset();
		}

		public void Ostettu_Maailma() {
			MenuFactory.MenuFactory.Instance.UnlockLevel(dest_level_set, dest_level_set.Anna_eka_level_id(), dest_level_set.GetKey(0));
			dest_menu_elem_container.menu_elems.Remove(ostonappi);
		}

		public void Ostettu_LukkojenAukaisija() {
			int level_id=dest_level_set.Anna_eka_level_id();
			for(int i=0; i<dest_level_set.level_lkm; i++) {
				MenuFactory.MenuFactory.Instance.UnlockLevel(dest_level_set, level_id++, dest_level_set.GetKey(i));
			}
			dest_menu_elem_container.menu_elems.Remove(ostonappi);
		}

		public void Ostettu_SkipLevel() {
			if(Level_hallinta.Level_hallinta.Instance.levelin_kello!=null)
				if(Level_hallinta.Level_hallinta.Instance.levelin_kello.aika<999)
					Level_hallinta.Level_hallinta.Instance.levelin_kello.aika=999;
			Level_hallinta.Level_hallinta.Instance.LevelComplete(true);
			ostonappi.AsetaNakyvyys(false);
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
	}
}

