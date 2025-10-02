﻿using Hexa.NET.ImGui;
using StudioCore.Configuration;
using StudioCore.Core;
using StudioCore.Editor;
using StudioCore.Editors.MapEditor.Enums;
using StudioCore.Editors.MapEditor.Framework;
using StudioCore.Formats.JSON;
using StudioCore.Interface;
using StudioCore.Platform;
using StudioCore.Scene.Interfaces;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;
using static MsbUtils;

namespace StudioCore.Editors.MapEditor.Core;

public class MapContentView
{
    public MapEditorScreen Editor;
    public ProjectEntry Project;

    public string MapID;

    public string ImguiID = "";
    private int treeImGuiId = 0;

    public MapContentViewType ContentViewType = MapContentViewType.ObjectType;
    public MapContentLoadState ContentLoadState = MapContentLoadState.Unloaded;

    private bool _setNextFocus;
    private ISelectable _pendingClick;
    private HashSet<Entity> _treeOpenEntities = new();

    public MapContentView(MapEditorScreen editor, ProjectEntry project, FileDictionaryEntry fileEntry)
    {
        Editor = editor;
        Project = project;

        ImguiID = fileEntry.Filename;
        MapID = fileEntry.Filename;
    }

    public void Load(bool selected)
    {
        ContentLoadState = MapContentLoadState.Loaded;

        Editor.ViewportSelection.ClearSelection(Editor);

        Editor.Universe.LoadMap(MapID, selected);
    }

    public void Unload()
    {
        ContentLoadState = MapContentLoadState.Unloaded;

        Editor.EntityTypeCache.RemoveMapFromCache(this);

        Editor.ViewportSelection.ClearSelection(Editor);
        Editor.EditorActionManager.Clear();

        Editor.Universe.UnloadMapContainer(MapID, true);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Handles the update for each frame
    /// </summary>
    public void OnGui()
    {
        if (ContentLoadState is MapContentLoadState.Unloaded)
            return;

        Editor.MapContentFilter.DisplaySearch(this);

        DisplayQuickActionButtons();

        // Reset this every frame, otherwise the map object selectables won't work correctly
        treeImGuiId = 0;

        DisplayContentTree();
    }

    /// <summary>
    /// Handles the show all button
    /// </summary>
    private void DisplayQuickActionButtons()
    {
        ImGui.SameLine();

        var targetContainer = Editor.GetMapContainerFromMapID(MapID);

        // Cycle Name Display Type
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Bars}", DPI.IconButtonSize))
        {
            var curType = (int)CFG.Current.MapEditor_MapContentList_NameDisplayType;
            curType++;

            if (curType > 4)
                curType = 0;

            CFG.Current.MapEditor_MapContentList_NameDisplayType = (NameDisplayType)curType;
        }
        UIHelper.Tooltip($"Cycle through the name display types.\nCurrent Type: {CFG.Current.MapEditor_MapContentList_NameDisplayType.GetDisplayName()}");

        // Show All
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Eye}", DPI.IconButtonSize))
        {
            foreach (var entry in targetContainer.Objects)
            {
                entry.EditorVisible = true;
            }
        }
        UIHelper.Tooltip("Force all map objects within this map to be shown.");

        // Hide All
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.EyeSlash}", DPI.IconButtonSize))
        {
            foreach (var entry in targetContainer.Objects)
            {
                entry.EditorVisible = false;
            }
        }
        UIHelper.Tooltip("Force all map objects within this map to be hidden.");

        // Switch View Type
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Sort}", DPI.IconButtonSize))
        {
            if (ContentViewType is MapContentViewType.ObjectType)
            {
                ContentViewType = MapContentViewType.Flat;
            }
            else if (ContentViewType is MapContentViewType.Flat)
            {
                ContentViewType = MapContentViewType.ObjectType;
            }
        }
        UIHelper.Tooltip("Switch the map content list style.");
    }

    /// <summary>
    /// Handles the display of the MSB contents
    /// </summary>
    public void DisplayContentTree()
    {
        ImGui.BeginChild($"mapContentsTree_{ImguiID}");

        var targetContainer = Editor.GetMapContainerFromMapID(MapID);

        Entity mapRoot = targetContainer?.RootObject;
        ObjectContainerReference mapRef = new(MapID);
        ISelectable selectTarget = (ISelectable)mapRoot ?? mapRef;

        ImGuiTreeNodeFlags treeflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;

        var selected = Editor.ViewportSelection.GetSelection().Contains(mapRoot) || Editor.ViewportSelection.GetSelection().Contains(mapRef);
        if (selected)
        {
            treeflags |= ImGuiTreeNodeFlags.Selected;
        }

        var nodeopen = false;
        var unsaved = targetContainer != null && targetContainer.HasUnsavedChanges ? "*" : "";

        ImGui.BeginGroup();

        string treeNodeName = $@"{Icons.Cube} {MapID}";
        string treeNodeNameFormat = $@"{Icons.Cube} {MapID}{unsaved}";

        if (targetContainer != null && ContentLoadState is MapContentLoadState.Loaded)
        {
            nodeopen = ImGui.TreeNodeEx(treeNodeName, treeflags, treeNodeNameFormat);

            var mapName = AliasUtils.GetMapNameAlias(Editor.Project, MapID);
            UIHelper.DisplayAlias(mapName);
        }

        ImGui.EndGroup();

        if (Editor.ViewportSelection.ShouldGoto(mapRoot) || Editor.ViewportSelection.ShouldGoto(mapRef))
        {
            ImGui.SetScrollHereY();
            Editor.ViewportSelection.ClearGotoTarget();
        }

        if (nodeopen)
        {
            ImGui.Indent(); //TreeNodeEx fails to indent as it is inside a group / indentation is reset
        }

        DisplayRootContextMenu(selected);
        HandleSelectionClick(selectTarget, mapRoot, mapRef, nodeopen);

        if (nodeopen)
        {
            var scale = DPI.UIScale();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, 3.0f) * scale);

            if (ContentViewType is MapContentViewType.ObjectType)
            {
                TypeView(targetContainer);
            }
            else if (ContentViewType is MapContentViewType.Flat)
            {
                FlatView(targetContainer);
            }

            ImGui.PopStyleVar();
            ImGui.TreePop();
        }

        ImGui.EndChild();
    }

    /// <summary>
    /// Handle the pending click stuff
    /// </summary>
    private void HandleSelectionClick(ISelectable selectTarget, Entity mapRoot, ObjectContainerReference mapRef, bool nodeopen)
    {
        if (ImGui.IsItemClicked())
        {
            _pendingClick = selectTarget;
        }

        if (ImGui.IsMouseDoubleClicked(0) && _pendingClick != null && mapRoot == _pendingClick)
        {
            Editor.MapViewportView.Viewport.FramePosition(mapRoot.GetLocalTransform().Position, 10f);
        }

        if ((_pendingClick == mapRoot || mapRef.Equals(_pendingClick)) && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (ImGui.IsItemHovered())
            {
                // Only select if a node is not currently being opened/closed
                if (mapRoot == null || nodeopen && _treeOpenEntities.Contains(mapRoot) || !nodeopen && !_treeOpenEntities.Contains(mapRoot))
                {
                    if (InputTracker.GetKey(Key.ControlLeft) || InputTracker.GetKey(Key.ControlRight))
                    {
                        // Toggle Selection
                        if (Editor.ViewportSelection.GetSelection().Contains(selectTarget))
                        {
                            Editor.ViewportSelection.RemoveSelection(Editor, selectTarget);
                        }
                        else
                        {
                            Editor.ViewportSelection.AddSelection(Editor, selectTarget);
                        }
                    }
                    else
                    {
                        Editor.ViewportSelection.ClearSelection(Editor);
                        Editor.ViewportSelection.AddSelection(Editor, selectTarget);
                    }
                }

                // Update the open/closed state
                if (mapRoot != null)
                {
                    if (nodeopen && !_treeOpenEntities.Contains(mapRoot))
                    {
                        _treeOpenEntities.Add(mapRoot);
                    }
                    else if (!nodeopen && _treeOpenEntities.Contains(mapRoot))
                    {
                        _treeOpenEntities.Remove(mapRoot);
                    }
                }
            }

            _pendingClick = null;
        }
    }

    /// <summary>
    /// Handles the right-click context menu for map root
    /// </summary>
    private void DisplayRootContextMenu(bool selected)
    {
        if (ImGui.BeginPopupContextItem($@"mapcontext_{MapID}"))
        {
            if (ImGui.Selectable("Copy Map ID"))
            {
                PlatformUtils.Instance.SetClipboardText(MapID);
            }
            if (ImGui.Selectable("Copy Map Name"))
            {
                var mapName = AliasUtils.GetMapNameAlias(Editor.Project, MapID);
                PlatformUtils.Instance.SetClipboardText(mapName);
            }
            if (Editor.GlobalSearchTool.IsOpen)
            {
                if (ImGui.Selectable("Add to Map Filter"))
                {
                    Editor.GlobalSearchTool.AddMapFilterInput(MapID);
                }
            }

            ImGui.EndPopup();
        }
    }


    /// <summary>
    /// Handles the right-click context menu for map object
    /// </summary>
    private void DisplayMapObjectContextMenu(Entity ent, int imguiID)
    {
        if (ImGui.BeginPopupContextItem($@"mapobjectcontext_{MapID}_{imguiID}"))
        {
            Editor.ReorderAction.OnContext(ent);

            Editor.DuplicateAction.OnContext();
            Editor.DeleteAction.OnContext();
            Editor.DuplicateToMapAction.OnContext();
            Editor.RotateAction.OnContext();
            Editor.ScrambleAction.OnContext(ent);
            Editor.ReplicateAction.OnContext(ent);
            Editor.RenderTypeAction.OnContext(ent);

            ImGui.Separator();

            Editor.FrameAction.OnContext(ent);
            Editor.PullToCameraAction.OnContext(ent);

            ImGui.Separator();

            Editor.EditorVisibilityAction.OnContext();
            Editor.GameVisibilityAction.OnContext();

            ImGui.Separator();

            Editor.SelectionGroupTool.OnContext();

            ImGui.Separator();

            Editor.SelectAllAction.OnContext(ent);

            ImGui.Separator();

            Editor.AdjustToGridAction.OnContext();

            ImGui.Separator();

            Editor.EntityInfoAction.OnContext(ent);

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Handles the setup for the object type content selectables
    /// </summary>
    private void TypeView(MapContainer map)
    {
        Editor.EntityTypeCache.AddMapToCache(map);

        foreach (KeyValuePair<MsbEntityType, Dictionary<Type, List<MsbEntity>>> cats in
                 Editor.EntityTypeCache._cachedTypeView[map.Name].OrderBy(q => q.Key.ToString()))
        {
            if (cats.Value.Count > 0)
            {
                ImGuiTreeNodeFlags treeflags = ImGuiTreeNodeFlags.OpenOnArrow;

                if (ImGui.TreeNodeEx(cats.Key.ToString(), treeflags))
                {
                    foreach (KeyValuePair<Type, List<MsbEntity>> typ in cats.Value.OrderBy(q => q.Key.Name))
                    {
                        if (typ.Value.Count > 0)
                        {
                            // Regions don't have multiple types in certain games
                            if (cats.Key == MsbEntityType.Region &&
                                Editor.Project.ProjectType is ProjectType.DES
                                    or ProjectType.DS1
                                    or ProjectType.DS1R
                                    or ProjectType.BB)
                            {
                                foreach (MsbEntity obj in typ.Value)
                                {
                                    AliasUtils.UpdateEntityAliasName(Editor.Project, obj);

                                    if (Editor.MapContentFilter.ContentFilter(this, obj))
                                    {
                                        MapObjectSelectable(obj, true);
                                    }
                                }
                            }
                            else if (cats.Key == MsbEntityType.Light)
                            {
                                foreach (Entity parent in map.BTLParents)
                                {
                                    var parentName = parent.WrappedObject;

                                    if (ImGui.TreeNodeEx($"{typ.Key.Name} {parentName}",
                                            treeflags))
                                    {
                                        ImGui.SetNextItemAllowOverlap();
                                        var visible = parent.EditorVisible;
                                        ImGui.SameLine();
                                        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X -
                                                            18.0f * DPI.UIScale());
                                        ImGui.PushStyleColor(ImGuiCol.Text, visible
                                            ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                                            : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                                        ImGui.TextWrapped(visible ? Icons.Eye : Icons.EyeSlash);
                                        ImGui.PopStyleColor();
                                        if (ImGui.IsItemClicked(0))
                                        {
                                            // Hide/Unhide all lights within this BTL.
                                            parent.EditorVisible = !parent.EditorVisible;
                                        }

                                        for (int i = 0; i < parent.Children.Count; i++)
                                        {
                                            var curObj = parent.Children[i];

                                            AliasUtils.UpdateEntityAliasName(Editor.Project, curObj);

                                            if (Editor.MapContentFilter.ContentFilter(this, curObj))
                                            {
                                                MapObjectSelectable(curObj, true);
                                            }
                                        }

                                        ImGui.TreePop();
                                    }
                                    else
                                    {
                                        ImGui.SetNextItemAllowOverlap();
                                        var visible = parent.EditorVisible;
                                        ImGui.SameLine();
                                        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X -
                                                            18.0f * DPI.UIScale());
                                        ImGui.PushStyleColor(ImGuiCol.Text, visible
                                            ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                                            : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                                        ImGui.TextWrapped(visible ? Icons.Eye : Icons.EyeSlash);
                                        ImGui.PopStyleColor();
                                        if (ImGui.IsItemClicked(0))
                                        {
                                            // Hide/Unhide all lights within this BTL.
                                            parent.EditorVisible = !parent.EditorVisible;
                                        }
                                    }
                                }
                            }
                            else if (ImGui.TreeNodeEx(typ.Key.Name, treeflags))
                            {
                                foreach (MsbEntity obj in typ.Value)
                                {
                                    AliasUtils.UpdateEntityAliasName(Editor.Project, obj);

                                    if (Editor.MapContentFilter.ContentFilter(this, obj))
                                    {
                                        MapObjectSelectable(obj, true);
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            ImGui.Text($@"   {typ.Key}");
                        }
                    }

                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.Text($@"   {cats.Key.ToString()}");
            }
        }
    }

    /// <summary>
    /// Handles the basic selectable entry
    /// </summary>
    private unsafe void MapObjectSelectable(Entity e, bool visicon, bool hierarchial = false)
    {
        var scale = DPI.UIScale();

        // Main selectable
        if (e is MsbEntity me)
        {
            ImGui.PushID(me.Type + e.Name);
        }
        else
        {
            ImGui.PushID(e.Name);
        }

        var doSelect = false;
        if (_setNextFocus)
        {
            ImGui.SetItemDefaultFocus();
            _setNextFocus = false;
            doSelect = true;
        }

        var nodeopen = false;
        var padding = hierarchial ? "   " : "    ";

        var arrowKeySelect = false;

        // Visibility icon
        if (visicon)
        {
            var icon = e.EditorVisible ? Icons.Eye : Icons.EyeSlash;

            ImGui.PushItemFlag(ImGuiItemFlags.NoNav, true);
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
            if (ImGui.Button($"{icon}##mapObject{e.Name}", DPI.InlineIconButtonSize))
            {
                if (InputTracker.GetKey(KeyBindings.Current.MAP_ToggleMapObjectGroupVisibility))
                {
                    var targetContainer = Editor.GetMapContainerFromMapID(MapID);

                    var mapRoot = targetContainer.RootObject;
                    foreach(var entry in mapRoot.Children)
                    {
                        if (entry.WrappedObject.GetType() == e.WrappedObject.GetType())
                        {
                            entry.EditorVisible = !entry.EditorVisible;
                            doSelect = false;
                        }
                    }
                }
                else
                {
                    e.EditorVisible = !e.EditorVisible;
                    doSelect = false;
                }
            }
            ImGui.PopStyleColor(4);
            ImGui.PopItemFlag();
            ImGui.SameLine();

            UIHelper.Tooltip("Toggle visibility state of this map object.");
        }

        if (hierarchial && e.Children.Count > 0)
        {
            ImGuiTreeNodeFlags treeflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (Editor.ViewportSelection.GetSelection().Contains(e))
            {
                treeflags |= ImGuiTreeNodeFlags.Selected;
            }

            nodeopen = ImGui.TreeNodeEx(e.PrettyName, treeflags);
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                if (e.RenderSceneMesh != null)
                {
                    Editor.MapViewportView.Viewport.FrameBox(e.RenderSceneMesh.GetBounds());
                }
            }
            if (ImGui.IsItemFocused() && (InputTracker.GetKey(Key.Up) || InputTracker.GetKey(Key.Down)))
            {
                doSelect = true;
                arrowKeySelect = true;
            }
        }
        else
        {
            treeImGuiId++;
            var selectableFlags = ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowOverlap;

            var displayName = "";

            if (CFG.Current.MapEditor_MapContentList_NameDisplayType is NameDisplayType.Internal or NameDisplayType.Internal_FMG or NameDisplayType.Internal_Community)
            {
                displayName = e.PrettyName;
            }
            else if (CFG.Current.MapEditor_MapContentList_NameDisplayType is NameDisplayType.Community or NameDisplayType.Community_FMG)
            {
                displayName = e.PrettyName;

                var nameListEntry = Project.MapData.MapObjectNameLists.FirstOrDefault(entry => entry.Key == Editor.Selection.SelectedMapID);

                if (nameListEntry.Value != null)
                {
                    var match = nameListEntry.Value.Entries.FirstOrDefault(entry => entry.ID == e.Name);

                    if (match != null)
                    {
                        displayName = match.Name;
                    }
                }
            }

            if (ImGui.Selectable($"{displayName}##{treeImGuiId}", Editor.ViewportSelection.GetSelection().Contains(e), selectableFlags))
            {
                doSelect = true;

                // If double clicked frame the selection in the viewport
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (e.RenderSceneMesh != null)
                    {
                        Editor.MapViewportView.Viewport.FrameBox(e.RenderSceneMesh.GetBounds());
                    }
                }
            }
            if (ImGui.IsItemFocused() && (InputTracker.GetKey(Key.Up) || InputTracker.GetKey(Key.Down)))
            {
                doSelect = true;
                arrowKeySelect = true;
            }

            if (CFG.Current.MapEditor_MapContentList_NameDisplayType is NameDisplayType.Internal_FMG or NameDisplayType.Community_FMG)
            {
                var alias = AliasUtils.GetEntityAliasName(Editor.Project, e);
                if (ImGui.IsItemVisible())
                {
                    UIHelper.DisplayAlias(alias);
                }
            }
            else if (CFG.Current.MapEditor_MapContentList_NameDisplayType is NameDisplayType.Internal_Community)
            {
                var nameListEntry = Project.MapData.MapObjectNameLists.FirstOrDefault(entry => entry.Key == Editor.Selection.SelectedMapID);

                if (nameListEntry.Value != null)
                {
                    var match = nameListEntry.Value.Entries.FirstOrDefault(entry => entry.ID == e.Name);

                    if (match != null)
                    {
                        if (ImGui.IsItemVisible())
                        {
                            UIHelper.DisplayAlias(match.Name);
                        }
                    }
                }

            }

            DisplayMapObjectContextMenu(e, treeImGuiId);

        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            _pendingClick = e;
        }

        if (_pendingClick == e && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (ImGui.IsItemHovered())
            {
                doSelect = true;
            }

            _pendingClick = null;
        }

        if (hierarchial && doSelect)
        {
            if (nodeopen && !_treeOpenEntities.Contains(e) ||
                !nodeopen && _treeOpenEntities.Contains(e))
            {
                doSelect = false;
            }

            if (nodeopen && !_treeOpenEntities.Contains(e))
            {
                _treeOpenEntities.Add(e);

            }
            else if (!nodeopen && _treeOpenEntities.Contains(e))
            {
                _treeOpenEntities.Remove(e);
            }
        }

        if (Editor.ViewportSelection.ShouldGoto(e))
        {
            // By default, this places the item at 50% in the frame. Use 0 to place it on top.
            ImGui.SetScrollHereY();
            Editor.ViewportSelection.ClearGotoTarget();
        }

        // If the visibility icon wasn't clicked, perform the selection
        Utils.EntitySelectionHandler(Editor, Editor.ViewportSelection, e, doSelect, arrowKeySelect);

        // If there's children then draw them
        if (nodeopen)
        {
            HierarchyView(e);
            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    /// <summary>
    /// Handles the setup for the heiarchical content selectables
    /// </summary>
    private void HierarchyView(Entity entity)
    {
        foreach (Entity obj in entity.Children)
        {
            if (obj is Entity e)
            {
                MapObjectSelectable(e, true, true);
            }
        }
    }

    private void FlatView(MapContainer map)
    {
        foreach (Entity obj in map.Objects)
        {
            if (obj is MsbEntity e)
            {
                if (Editor.MapContentFilter.ContentFilter(this, obj))
                {
                    MapObjectSelectable(e, true);
                }
            }
        }
    }
}
