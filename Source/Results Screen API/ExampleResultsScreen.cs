using NgData;
using NgLib;
using NgSp;
using NgUi;
using NgUi.RaceUi.Menus;
using NgUi.Results;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example result screen generator for a race like gamemode. This is just for a single player.
///
/// The results screen API is still a work in progress. More features will be added and bugs will be fixed before 1.3
///  is fully released. The general API structure is not planned to be changed, so no breaking changes going forward
///  should happen.
/// 
/// To open after your gamemode has concluded, do the following:
///     GeneratedResultsScreen result = GeneratedResultsScreen.Create(new RaceResultsGenerator());
///     result.OpenInterface(NgAward.Platinum);
/// </summary>
public class ExampleResultsScreen : ResultsScreenGenerator
{
    public override GameObject GenerateInterface(RectTransform windowRect, Image windowImage, NgAward award, MedalRenderer medalRenderer)
    {
        float tableWidth = Build.MaxSize.x - 256;
        
        // Create the header
        Build.BigHeader("title", "race ended", Color.white);
        Build.Seperator("");
        
        // Show the player their place.
        Build.BigHeader("", $"{NgStr.RacePositionToString(1)} place", Color.white);
        Build.Space("", new Vector2(Build.MaxSize.x, 32.0f));
        
        // Build the table that shows the player their lap records data
        ResultsTable table = Build.Table("laps table", new Vector2(tableWidth, 300.0f));
        FillTableLapData(table, tableWidth, Ships.PlayerOneShip);
        table.BuildUi();
        
        // Render the awarded medal.
        // First draw the header, draw a background for the medal and then draw the output of the medal renderer on
        //  top of it.
        Build.SameLine();
        Build.GraphicText("medal header", 256, "medal", Color.white, Color.black);
        Build.SameColumn();
        
        Build.Image("medal background", Vector2.one * 268, null, new Color32(0, 0, 0, 100));
        Build.SamePosition();
        Build.RawImage("medal", Vector2.one * 256, medalRenderer.OutTexture, Color.white);

        // Create a default options strip and anchor it to the bottom of the results screen.
        // You can create your own options strip using the constructor (new OptionsStrip) and using the
        //      OptionsStrip.AddOption method.
        Build.AlignVerticalBottom();
        OptionsStrip strip = BuildDefaultOptions(out string selection);

        // return the option that should be selected by default (this is handled automatically by BuildDefaultOptions)
        return strip.GetOption(selection);
    }
}