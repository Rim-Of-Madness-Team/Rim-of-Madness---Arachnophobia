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
            var label = "";
            if (this.settings.romSpiderFactor < 0.25f)
            {
                label = "ROM_SettingsSpiderMultiplier_None".Translate();
            }
            else
            {
                label = "ROM_SettingsSpiderMultiplier_Num".Translate(this.settings.romSpiderFactor);
            }
            this.settings.romSpiderFactor = Widgets.HorizontalSlider(inRect.TopHalf().TopHalf().TopHalf(), this.settings.romSpiderFactor, 0.0f, 10f, false, label, null, null, 0.25f);

            this.WriteSettings();

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
        public float romSpiderFactor = 1;
        public string romSpiderFactorBuffer;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.romSpiderFactor, "romSpiderFactor", 0);
        }
    }
}
