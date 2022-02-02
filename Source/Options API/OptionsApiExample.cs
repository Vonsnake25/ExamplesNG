using System.IO;
using NgData;
using NgEvents;
using NgModding;
using NgUi.MenuUi;
using UnityEngine;

namespace BallisticNG.ExampleMods
{
    public class OptionsApiExample : CodeMod
    {
        // The file path to our settings ini. See below in OnRegistered.
        private string _settingsIni;

        // Our setting. If enabled then we'll print hello world to the notification buffer when the race countdown starts.
        public static bool DoHelloWorld;
        
        // The HUE of our message color.
        public static float HelloWorldHue = 0.5f;

        public override void OnRegistered(string modPath)
        {
            // Cache the file path to our settings ini. We'll place it in the mods folder.
            _settingsIni = Path.Combine(modPath, "settings.ini");
            
            // Hook into the load/save delegates for settings.
            //
            // If you have data that you'd like to automatically remember between play sessions, you can also hook into
            // ModOptions.OnLoadPreferences and ModOptions.OnSavePreferences.
            ModOptions.OnLoadSettings += OnLoadSettings;
            ModOptions.OnSaveSettings += OnSaveSettings;

            // also hook into OnCountdownStart so we can trigger our message.
            NgRaceEvents.OnCountdownStart += OnCountdownStart;
            
            // Finally register our options menu
            ModOptions.RegisterMod("Api Example", GenerateModUi, ModUiToCode);
        }
        
        // Called when the game wants us to generate our mods option interface.
        private void GenerateModUi(ModOptionsUiContext ctx)
        {
            // Generates a header with the provided title
            ctx.GenerateHeader("Example API Header");
            
            // Generates a selector. The options for this are parameters so you can manually enter each option or provide an array
            // In this case we're manually entering each option.
            ctx.GenerateSelector("Hello World", "Hello World Message", "If enabled will print \"Hello World\" to notifications.",
                DoHelloWorld ? 1 : 0, "off", "on");
            
            // Generates a space. Good for creating a visual separation between option sub-groups.
            // You don't need to call this between each element. This is only for generating additional space.
            ctx.GenerateSpace();
            
            // Generates a slider. We'll use this to set the hue of the hello world message.
            ctx.GenerateSlider("Hello World HUE", "Hello World HUE" , "The HUE of the hello world notification.",
                0.0f, 1.0f, HelloWorldHue, 0.1f, 100, NgSlider.RoundMode.Round, 100, NgSlider.RoundMode.Round);
        }
        
        // Called when the games wants us to interpret our mod options back into our code.
        private void ModUiToCode(ModOptionsUiContext ctx)
        {
            // Get the value from the Hello World selector. Since this is an off/on switch we can compare it to 1
            // to detrmine if it's on.
            DoHelloWorld = ctx.GetSelectorValue("Hello World") == 1;

            // Get the HUE value from the Hello World HUE slider.
            HelloWorldHue = ctx.GetSliderValue("Hello World HUE");
        }
        
        // Called when the race countdown has just started.
        private void OnCountdownStart()
        {
            if (DoHelloWorld)
            {
                Color msgColor = Color.HSVToRGB(HelloWorldHue, 1.0f, 1.0f);
                NgUiEvents.CallOnTriggerMessage("Hello World", Ships.PlayerOneShip, msgColor);
            }
        }
        
        // Called when the game wants us to load our settings from disk.
        private void OnLoadSettings()
        {
            // Open the ini file and read our values from it.
            // If the ini file doesn't exist then it'll be created and the defaults we set in code will be used.
            INIParser ini = new INIParser();
            ini.Open(_settingsIni);

            DoHelloWorld = ini.ReadValue("Settings", "Do Hello World", DoHelloWorld);
            HelloWorldHue = (float)ini.ReadValue("Settings", "Message HUE", HelloWorldHue);
            
            ini.Close();
        }
        
        // Called when the game wants us to save our settings to disk.
        private void OnSaveSettings()
        {
            // Open the settings ini file and write our values into it.
            INIParser ini = new INIParser();
            ini.Open(_settingsIni);

            ini.WriteValue("Settings", "Do Hello World", DoHelloWorld);
            ini.WriteValue("Settings", "Message HUE", HelloWorldHue);
            
            ini.Close();
        }
    }
}