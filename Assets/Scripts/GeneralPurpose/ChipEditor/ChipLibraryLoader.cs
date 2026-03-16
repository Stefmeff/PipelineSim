using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Scans for saved .chip files and populates the Custom folder in the library.
/// Clones Template/TemplateLast, places them as siblings beneath FolderCustom in Content.
/// Attach to the FolderCustom GameObject.
/// </summary>
public class ChipLibraryLoader : MonoBehaviour
{
    [SerializeField] private GameObject template;
    [SerializeField] private GameObject templateLast;
    [SerializeField] private Folder folder;

    private List<GameObject> createdEntries = new List<GameObject>();

    private void Start()
    {
        LoadChipEntries();
    }

    public void LoadChipEntries()
    {
        // Clear previous dynamic entries
        foreach (GameObject entry in createdEntries)
        {
            if (entry != null) Destroy(entry);
        }
        createdEntries.Clear();

        // Load all saved chips
        List<ChipDefinition> chips = ChipDefinition.LoadAll();
        if (chips.Count == 0) return;

        // Parent is Content (same parent as FolderCustom and templates)
        Transform contentParent = template.transform.parent;

        // Get FolderCustom's sibling index so we place entries right after it
        int folderIndex = transform.GetSiblingIndex();

        List<GameObject> folderItems = new List<GameObject>();

        for (int i = 0; i < chips.Count; i++)
        {
            bool isLast = (i == chips.Count - 1);
            GameObject source = isLast ? templateLast : template;

            GameObject entry = Instantiate(source, contentParent);
            entry.SetActive(true);
            entry.name = "Chip_" + chips[i].name;

            // Place as sibling right after FolderCustom (and after previous entries)
            entry.transform.SetSiblingIndex(folderIndex + 1 + i);

            // Set chip name in text (max 7 characters + "." if truncated)
            TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                string displayName = chips[i].name;
                if (displayName.Length > 7) displayName = displayName.Substring(0, 7) + ".";
                label.text = displayName;
            }

            // Replace DragDropUI with ChipDragDropUI
            DragDropUI oldDrag = entry.GetComponent<DragDropUI>();
            if (oldDrag != null) Destroy(oldDrag);

            ChipDragDropUI chipDrag = entry.AddComponent<ChipDragDropUI>();
            chipDrag.chipName = chips[i].name;
            chipDrag.dropPrefab = Resources.Load("Prefabs/CustomChip") as GameObject;

            createdEntries.Add(entry);
            folderItems.Add(entry);
        }

        // Update folder's item list so open/close works
        folder.SetItems(folderItems);
    }
}
