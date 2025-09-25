using System.IO;
using NgContent;
using NgEvents;
using NgModding;
using NgUi.MenuUi;
using ModOptions = NgUi.Options.ModOptions;

namespace MyMod
{
    public class ExampleSettings : CodeMod
    {
        // The path to our mods ini file for saving settings.
        private static string _configPath;

        /*---Selector Settings---*/
        public static bool MyBooleanSelectorSetting;
        public static EShips MyEnumerationSelectorSetting;
        public static int MyIntSelectorSetting;
        
        /*---Slider Settings---*/
        public static int MyIntSliderSetting;
        public static float MyFloatSliderSetting;
        
        // Called when the mod has been registered into the game.
        public override void OnRegistered(string modPath)
        {
            _configPath = Path.Combine(modPath, "config.ini");
            
            RegisterSettings();
            
            NgSystemEvents.OnConfigRead += OnConfigRead;
            NgSystemEvents.OnConfigWrite += OnConfigWrite;
        }

        // Register our settings into the options menu.
        private void RegisterSettings()
        {
            // If we're adding multiple settings then we can store the categories into strings to reuse and
            //  easily adjust later if we want.
            //
            // You don't need a category per element type. This example is just structuring them like this
            //  for organizational purposes.
            string selectorCategory = "example selectors";
            string sliderCategory = "example sliders";

            string modId = "my mod"; // the mod ID is used to create the menu category
            
            // ModOptions.RegisterOption is how we register our setting into the menu. They will show up in the mods
            //  tab under the category of the name you provide.
            //
            // Currently the only two element types supported are NgBoxSelector and NgBoxSlider. You can create seperators
            //  by setting the first argument of the method to true. The seperator is created before the element.
            //
            // The two delegates are for configuring and reading the UI elements that you're generating.
            
            /*---Selectors---*/
            ModOptions.RegisterOption<NgBoxSelector>(false, modId, selectorCategory, "my boolean setting",
                selector =>
                {
                    selector.Configure("my boolean setting", "An example of using the boolean override for selectors.",
                        MyBooleanSelectorSetting, EBooleanDisplayType.EnabledDisabled);
                }, selector =>
                {
                    MyBooleanSelectorSetting = selector.ToBool();
                });
            
            ModOptions.RegisterOption<NgBoxSelector>(false, modId, selectorCategory, "my enumeration setting",
                selector =>
                {
                    selector.Configure("my enumeration setting", "An example of using the enumeration override for selectors.",
                        MyEnumerationSelectorSetting);
                }, selector =>
                {
                    MyEnumerationSelectorSetting = (EShips)selector.Value;
                });
            
            ModOptions.RegisterOption<NgBoxSelector>(true, modId, selectorCategory, "my int selector setting",
                selector =>
                {
                    selector.Configure("my int selector setting", "An example of setting up a custom list of data for selectors.",
                        MyIntSelectorSetting, null, "setting a", "setting b", "setting c");
                }, selector =>
                {
                    MyIntSelectorSetting = selector.Value;
                });
            
            /*---Sliders---*/
            // These examples show you how to setup a slider that allows a selection between 0% - 100%.
            // The float version is assuming the float represents a range of 0.0f - 1.0f, so we're multiplying by 100
            //  for presentation to the player then dividing by 100 to bring it back down to the internal range.
            ModOptions.RegisterOption<NgBoxSlider>(false, modId, sliderCategory, "my int slider setting",
                slider =>
                {
                    slider.Configure("my int slider setting", "An example of setting up a slider and reading data from it as an integer.",
                        "%", MyIntSliderSetting, 0, 100, 1);
                }, slider =>
                {
                    MyIntSliderSetting = (int) slider.Value;
                });
            
            ModOptions.RegisterOption<NgBoxSlider>(false, modId, sliderCategory, "my float slider setting",
                slider =>
                {
                    slider.Configure("my float slider setting", "An example of setting up a slider and reading data from it as a float.",
                        "%", MyFloatSliderSetting * 100.0f, 0.0f, 100.0f, 1.0f);
                }, slider =>
                {
                    MyFloatSliderSetting = slider.Value / 100.0f;
                });
        }

        // This is called every time the game's config fifle is read and is where we can load our config file.
        private void OnConfigRead()
        {
            INIParser ini = new INIParser();
            ini.Open(_configPath);
            
            MyBooleanSelectorSetting = ini.ReadValue("selectors", "my boolean setting", MyBooleanSelectorSetting);
            MyEnumerationSelectorSetting = (EShips) ini.ReadValue("selectors", "my enumeration setting", (int)MyEnumerationSelectorSetting);
            MyIntSelectorSetting = ini.ReadValue("selectors", "my int setting", MyIntSelectorSetting);
            
            MyIntSliderSetting = ini.ReadValue("sliders", "my int slider setting", MyIntSliderSetting);
            MyFloatSliderSetting = (float)ini.ReadValue("sliders", "my float slider setting", MyFloatSliderSetting);
            
            ini.Close();
        }
    
        // This is called every time the game's config file is written and is where we can save our config file.
        private void OnConfigWrite()
        {
            INIParser ini = new INIParser();
            ini.Open(_configPath);
            
            ini.WriteValue("selectors", "my boolean setting", MyBooleanSelectorSetting);
            ini.WriteValue("selectors", "my enumeration setting", (int)MyEnumerationSelectorSetting);
            ini.WriteValue("selectors", "my int setting", MyIntSelectorSetting);
            
            ini.WriteValue("sliders", "my int slider setting", MyIntSliderSetting);
            ini.WriteValue("sliders", "my float slider setting", MyFloatSliderSetting);
            
            ini.Close();
        }
       
    }
}
