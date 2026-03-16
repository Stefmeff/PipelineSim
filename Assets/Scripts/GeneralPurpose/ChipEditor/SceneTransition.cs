using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static utility for transitioning between Sandbox and ChipEditor scenes.
/// Auto-saves sandbox state to a temp file before switching; restores on return.
/// </summary>
public static class SceneTransition
{
    /// <summary>
    /// The chipId to edit when the ChipEditor scene loads. Null = new chip.
    /// </summary>
    public static string chipIdToEdit;

    /// <summary>
    /// True only when actively returning from chip editor to sandbox.
    /// Prevents stale temp files from restoring on fresh game start.
    /// </summary>
    public static bool returningFromEditor = false;

    private static string TempSavePath => Path.Combine(Application.persistentDataPath, "_sandbox_temp.pip");

    /// <summary>
    /// Saves the current sandbox state and loads the ChipEditor scene.
    /// </summary>
    /// <param name="chipId">Optional chipId to edit an existing chip. Null for new chip.</param>
    public static void OpenChipEditor(string chipId = null)
    {
        chipIdToEdit = chipId;

        // Auto-save sandbox state
        GameObject pmObj = GameObject.FindGameObjectWithTag("ProjectManager");
        if (pmObj != null)
        {
            ProjectManager pm = pmObj.GetComponent<ProjectManager>();
            string json = pm.SaveWorld();
            File.WriteAllText(TempSavePath, json);
            Debug.Log("Sandbox auto-saved to temp file");
        }

        SceneManager.LoadScene("ChipEditor");
    }

    /// <summary>
    /// Returns to the Sandbox scene and restores auto-saved state.
    /// </summary>
    public static void ReturnToSandbox()
    {
        returningFromEditor = true;
        SceneManager.LoadScene("Sandbox");
        // Restoration happens in ProjectManager.Awake() via CheckTempRestore()
    }

    /// <summary>
    /// Checks if a temp save file exists and restores it. Called by ProjectManager on Awake.
    /// </summary>
    public static bool TryRestoreTempSave(ProjectManager pm)
    {
        // Only restore in Sandbox scene
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Sandbox") return false;

        // Only restore when actively returning from chip editor
        if (!returningFromEditor)
        {
            // Clean up any stale temp files from crashes
            if (File.Exists(TempSavePath)) File.Delete(TempSavePath);
            return false;
        }

        returningFromEditor = false;

        if (File.Exists(TempSavePath))
        {
            string json = File.ReadAllText(TempSavePath);
            File.Delete(TempSavePath);
            pm.LoadWorld(json);
            pm.ResetComponents();
            Debug.Log("Sandbox restored from temp file");
            return true;
        }
        return false;
    }
}
