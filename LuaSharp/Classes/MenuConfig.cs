using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using LuaSharp.Core.API.Menu;

namespace LuaSharp.Classes
{
    public class MenuConfig
    {
        public Menu Menu;
        public MenuConfig(string name, string intName, Menu menu = null)
        {
            Menu = menu ?? new Menu(name, intName, true);

            if (menu == null)
                Menu.AddToMainMenu();
        }

        public void addParam(string intName, string name, MenuSetting settings, params object[] para)
        {
            switch (settings)
            {
                case MenuSetting.KeyDown:
                    Menu.AddItem(new MenuItem(intName, name).SetValue(new KeyBind(uint.Parse(para[1].ToString()), KeyBindType.Press, bool.Parse(para[0].ToString()))));
                    break;
                case MenuSetting.KeyToggle:
                    Menu.AddItem(new MenuItem(intName, name).SetValue(new KeyBind(uint.Parse(para[1].ToString()), KeyBindType.Toggle, bool.Parse(para[0].ToString()))));
                    break;
                case MenuSetting.Color:
                    var colors = (int[])para[0];
                    Menu.AddItem(new MenuItem(intName, name).SetValue(Color.FromArgb(colors[0], colors[1], colors[2], colors[3])));
                    break;
                case MenuSetting.OnOff:
                    Menu.AddItem(new MenuItem(intName, name).SetValue(bool.Parse(para[0].ToString())));
                    break;
                case MenuSetting.Slider:
                    //Wtf does para[3] do
                    Menu.AddItem(new MenuItem(intName, name).SetValue(new Slider((int)para[0], (int)para[1], (int)para[2])));
                    break;
                case MenuSetting.Info:
                    Menu.AddItem(new MenuItem(intName, name).SetValue(new[] {""}));
                    break;
                    
            }
        }

        public void permaShow(string intName)
        {
            foreach (var item in Menu.Items.Where(item => item.Name == intName))
            {
                item.Permashow();
                break;
            }
        }

        public void addSubMenu(string name, string intName)
        {
            
        }
    }
}
