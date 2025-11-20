using System;
using System.Xml;
using Monocle;

namespace Celeste.Mod.MovementLinter;

/// <summary>
/// Annoyingly, the vanilla <see cref="MiniTextbox"/> can only take in a dialog ID, not an arbitrary string,
/// so you can't display text formatted with values set at runtime. This version adds a constructor to do that.
/// Also at least one unrelated rendering improvement.
/// </summary>
class CustomMiniTextbox : MiniTextbox {
    public CustomMiniTextbox(string dialog) : base("") {
        // We give the parent constructor a blank ID so it doesn't find any portraits.
        // Then we overwrite the text field and look for portraits again.
        text = FancyText.Parse(dialog, (int) (1688f - portraitSize - 32f), 2, 1f, null);
        foreach (FancyText.Node node in text.Nodes) {
            if (node is FancyText.Portrait) {
                portraitData   = node as FancyText.Portrait;
                portrait       = GFX.PortraitsSpriteBank.Create("portrait_" + portraitData.Sprite);
                XmlElement xml = GFX.PortraitsSpriteBank.SpriteData["portrait_" + portraitData.Sprite].Sources[0].XML;
                portraitScale  = portraitSize / xml.AttrFloat("size", 160f);
                string id      = "textbox/" + xml.Attr("textbox", "default") + "_mini";
                if (GFX.Portraits.Has(id)) {
                    box = GFX.Portraits[id];
                }
                Add(portrait);
            }
        }
        // Display over the timer
        Depth = -101;
    }
}
