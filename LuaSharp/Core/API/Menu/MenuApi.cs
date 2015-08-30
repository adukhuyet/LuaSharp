using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaSharp.Classes;
using MoonSharp.Interpreter;

namespace LuaSharp.Core.API.Menu
{
    internal class MenuApi
    {
        public static void AddApi(Script script)
        {
            UserData.RegisterType<MenuConfig>();
            script.Globals["SCRIPT_PARAM_SLICE"] = MenuSetting.Slider;
            script.Globals["SCRIPT_PARAM_ONOFF"] = MenuSetting.OnOff;
            script.Globals["SCRIPT_PARAM_ONKEYDOWN"] = MenuSetting.KeyDown;
            script.Globals["SCRIPT_PARAM_ONKEYTOGGLE"] = MenuSetting.KeyToggle;
            script.Globals["SCRIPT_PARAM_COLOR"] = MenuSetting.Color;
            script.Globals["SCRIPT_PARAM_INFO"] = MenuSetting.Info;

            script.Globals["scriptConfig"] = (Func<string, string, MenuConfig>) scriptConfig;
        }

        private static MenuConfig scriptConfig(string name, string intName)
        {
            return new MenuConfig(name, intName);
        }

    }
}
