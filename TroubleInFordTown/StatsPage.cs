using BoneLib.BoneMenu;
using UnityEngine;

namespace TroubleInFordTown;

public class StatsPage
{
    private Page _statsPage;
    private FunctionElement _statsContent;

    public void Register()
    {
        _statsPage = Page.Root.CreatePage("Stats", new Color(0.2f, 0.8f, 0.2f));

        // Initial placeholder content
        _statsContent = _statsPage.CreateFunction("No stats yet \u2014 play a round!", Color.gray, delegate { });
    }

    public void Unregister()
    {
        if (_statsPage != null)
        {
            Page.Root.RemovePage(_statsPage);
            _statsPage = null;
        }
    }

    public void Refresh()
    {
        if (_statsPage == null) return;

        _statsPage.RemoveAll();

        // Placeholder -- actual stats data will come from StatsManager (Feature 14)
        _statsContent = _statsPage.CreateFunction("Stats tracking coming soon...", Color.yellow, delegate { });

        // Also add a note about what will be shown
        _statsPage.CreateFunction("Lifetime loot collection", new Color(0.5f, 0.5f, 0.5f), delegate { });
        _statsPage.CreateFunction("Per-round earnings", new Color(0.5f, 0.5f, 0.5f), delegate { });
    }
}
