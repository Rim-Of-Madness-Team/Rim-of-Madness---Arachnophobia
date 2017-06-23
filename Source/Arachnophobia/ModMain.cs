using UnityEngine;
using Verse;

namespace Arachnophobia
{

    public class ModMain : Mod
    {
        Settings settings;
        
        public ModMain(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<Settings>();
            ModInfo.romSpiderFactor = this.settings.romSpiderFactor;
        }

        public override string SettingsCategory() => "Rim of Madness - Arachnophobia";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Widgets.TextFieldNumericLabeled<int>(inRect.TopHalf().TopHalf().TopHalf(), "ROM_SettingsSpiderMultiplier".Translate(), ref this.settings.romSpiderFactor, ref this.settings.romSpiderFactorBuffer, 0, 999999);

            this.WriteSettings();

            //this.settings.Write();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            if (Find.World?.GetComponent<WorldComponent_ModSettings>() is WorldComponent_ModSettings modSettings)
            {
                ModInfo.romSpiderFactor = this.settings.romSpiderFactor;
                modSettings.SpiderDefsModified = false;
            }
        }

    }

    public class Settings : ModSettings
    {
        public int romSpiderFactor = 1;
        public string romSpiderFactorBuffer;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.romSpiderFactor, "romSpiderFactor", 0);
        }
    }
}
