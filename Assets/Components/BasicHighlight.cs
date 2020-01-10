﻿using OsgiViz;
using OsgiViz.Unity.Island;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicHighlight : AdditionalIslandVizComponent
{
    public Material HighlightMaterial;

    public HighlightMode Island;
    public HighlightMode Region;
    public HighlightMode Building;
    public HighlightMode Dock;

    private Dictionary<Transform, GameObject> Highlights;


    private void Start() { } // When this has no Start method, you will not be able to disable this in the editor.
    

    // ################
    // Initiation
    // ################

    #region Initiation

    /// <summary>
    /// Initialize this visualization component. 
    /// This method is called by the IslandVizVisualization class.
    /// </summary>
    public override IEnumerator Init()
    {
        if (Island != HighlightMode.None)
            IslandVizInteraction.Instance.OnIslandSelect += HighlightIsland;
        if (Region != HighlightMode.None)
            IslandVizInteraction.Instance.OnRegionSelect += HighlightRegion;
        if (Building != HighlightMode.None)
            IslandVizInteraction.Instance.OnBuildingSelect += HighlightBuilding;
        if (Dock != HighlightMode.None)
            IslandVizInteraction.Instance.OnDockSelect += HighlightDock;

        Highlights = new Dictionary<Transform, GameObject>();

        yield return null;
    }

    #endregion


    private void HighlightIsland(IslandGO island, IslandVizInteraction.SelectionType selectionType, bool selected)
    {
        if (selectionType == IslandVizInteraction.SelectionType.Highlight && island != null)
        {
            if (Island == HighlightMode.Normal)
            {
                NormalHighlight(island.transform, selected);
            }
        }
    }

    private void HighlightRegion(Region region, IslandVizInteraction.SelectionType selectionType, bool selected)
    {
        if (selectionType == IslandVizInteraction.SelectionType.Highlight)
        {
            if (Region == HighlightMode.Normal)
            {
                NormalHighlight(region.transform, selected);
            }
        }
    }

    private void HighlightBuilding(Building building, IslandVizInteraction.SelectionType selectionType, bool selected)
    {
        if (selectionType == IslandVizInteraction.SelectionType.Highlight)
        {
            if (Building == HighlightMode.Normal)
            {
                NormalHighlight(building.transform, selected);
            }
        }
    }

    private void HighlightDock (DependencyDock dock, IslandVizInteraction.SelectionType selectionType, bool selected)
    {
        if (selectionType == IslandVizInteraction.SelectionType.Highlight && dock != null)
        {
            if (Dock == HighlightMode.Normal)
            {
                NormalHighlight(dock.transform, selected);
            }
        }

        //if (selectionType == IslandVizInteraction.SelectionType.Select && !dock.Selected && dock != null)
        //{
        //    throw new System.NotImplementedException(); // TODO
        //}
    }


    private void NormalHighlight (Transform target, bool selected)
    {
        if (selected && !Highlights.ContainsKey(target))
        {
            GameObject highlight = new GameObject("Highlight");
            highlight.AddComponent<MeshRenderer>().material = HighlightMaterial;
            highlight.AddComponent<MeshFilter>().mesh = target.GetComponent<MeshFilter>().mesh;
            highlight.transform.parent = target;
            highlight.transform.localScale = Vector3.one * 1.01f;
            highlight.transform.position = target.position;
            highlight.transform.rotation = target.rotation;
            Highlights.Add(target, highlight);
        }
        else if (!selected && Highlights.ContainsKey(target))
        {
            Destroy(Highlights[target]);
            Highlights.Remove(target);
        }
    }


    public enum HighlightMode
    {
        None,
        Normal,
        Test
    }



}
