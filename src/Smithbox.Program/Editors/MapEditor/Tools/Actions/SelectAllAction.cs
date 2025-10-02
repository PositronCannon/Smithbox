﻿using Hexa.NET.ImGui;
using SoulsFormats;
using StudioCore;
using StudioCore.Configuration;
using StudioCore.Core;
using StudioCore.Editor;
using StudioCore.Editors.MapEditor;
using StudioCore.Editors.MapEditor.Core;
using StudioCore.Editors.MapEditor.Framework;
using StudioCore.Editors.MapEditor.Framework.MassEdit;
using StudioCore.Interface;
using StudioCore.Platform;
using StudioCore.Scene.Helpers;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Editors.MapEditor.Tools;

public class SelectAllAction
{
    public MapEditorScreen Editor;
    public ProjectEntry Project;

    public bool ConfigurableSelectAllPopupOpen = false;

    public List<string> SelectionInputs = new List<string>() { "" };

    public SelectionConditionLogic MapObjectSelectionLogic;

    private bool DisplayHelp = false;

    private bool OpenPopup = false;

    public SelectAllAction(MapEditorScreen editor, ProjectEntry project)
    {
        Editor = editor;
        Project = project;
    }

    /// <summary>
    /// Update Loop
    /// </summary>
    public void OnGui()
    {
        if (OpenPopup)
        {
            ImGui.OpenPopup("ConfigurableSelectAllPopup");
            OpenPopup = false;
        }

        if (ImGui.BeginPopup("ConfigurableSelectAllPopup"))
        {
            ConfigurableSelectAllPopupOpen = true;

            ConfigureSelection();

            if (ImGui.Button("Submit", DPI.WholeWidthButton(550f * DPI.UIScale(), 24)))
            {
                SelectAll();

                ConfigurableSelectAllPopupOpen = false;
            }

            if (DisplayHelp)
            {
                HelpSection();
            }

            ImGui.EndPopup();
        }
        else
        {
            ConfigurableSelectAllPopupOpen = false;
        }
    }

    /// <summary>
    /// Shortcut
    /// </summary>
    public void OnShortcut()
    {
        // Select All by Configuration
        if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_ConfigurableSelectAll) && Editor.ViewportSelection.IsSelection())
        {
            OpenPopup = true;
        }

        // Select All by Map Object Type
        if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectAllByMapObjectType) && Editor.ViewportSelection.IsSelection())
        {
            SelectAllByMapObjectType();
        }

        // Select All by Model Name
        if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectAllByModelName) && Editor.ViewportSelection.IsSelection())
        {
            SelectAllByModelName();
        }
    }

    /// <summary>
    /// Context Menu
    /// </summary>
    public void OnContext(Entity ent)
    {
        if (ImGui.BeginMenu("Select All"))
        {
            if (ImGui.Selectable("By Configuration"))
            {
                OpenPopup = true;
            }
            UIHelper.Tooltip($"Select all map objects via a user-defined search filter configuration.\n\nShortcut: {KeyBindings.Current.MAP_ConfigurableSelectAll.HintText}");

            if (ImGui.Selectable("Select All by Type"))
            {
                SelectAllByMapObjectType();
            }
            UIHelper.Tooltip($"Select all map objects that share the same internal type as the current selection.\n\nShortcut: {KeyBindings.Current.MAP_SelectAllByMapObjectType.HintText}");

            if (ent.WrappedObject is IMsbPart)
            {
                if (ImGui.Selectable("Select All by Model Name"))
                {
                    SelectAllByModelName();
                }
                UIHelper.Tooltip($"Select all part map objects that share the same model name as the current selection.\n\nShortcut: {KeyBindings.Current.MAP_SelectAllByModelName.HintText}");
            }

            ImGui.EndMenu();
        }
    }

    /// <summary>
    /// Edit Menu
    /// </summary>
    public void OnMenu(Entity ent)
    {

    }

    /// <summary>
    /// Tool Window
    /// </summary>
    public void OnToolWindow()
    {
        var windowWidth = ImGui.GetWindowWidth();

        // Not shown here
    }

    /// <summary>
    /// Effect
    /// </summary>
    public void SelectAll()
    {
        Editor.ViewportSelection.ClearSelection(Editor);

        var curView = Editor.Selection.SelectedMapView;
        var container = Editor.GetMapContainerFromMapID(Editor.Selection.SelectedMapID);

        if (container != null)
        {
            foreach (var entry in container.Objects)
            {
                if (entry is MsbEntity mEnt)
                {
                    if (IsValidMapObject(curView, mEnt))
                    {
                        Editor.ViewportSelection.AddSelection(Editor, mEnt);
                    }
                }
            }
        }
    }

    public void SelectAllByModelName()
    {
        if (Editor.ViewportSelection.IsSelection())
        {
            var sel = Editor.ViewportSelection.GetFilteredSelection<MsbEntity>().ToList();

            List<string> targetNames = new List<string>();

            foreach (var entry in sel)
            {
                var curName = "";
                var result = entry.GetPropertyValue("ModelName");

                if (result != null)
                {
                    curName = (string)result;
                }

                if (curName != "" && !targetNames.Contains(curName))
                {
                    targetNames.Add(curName);
                }
            }

            var curMap = Editor.Selection.SelectedMapView.MapID;
            MapContainer curMapContainer = null;

            foreach (var entry in Project.MapData.PrimaryBank.Maps)
            {
                var mapID = entry.Key.Filename;
                var map = entry.Value.MapContainer;

                if (map != null)
                {
                    if (curMap == mapID)
                    {
                        curMapContainer = (MapContainer)map;
                    }
                }
            }

            if (curMapContainer == null)
            {
                PlatformUtils.Instance.MessageBox("Failed to select map container.", "Smithbox", MessageBoxButtons.OK);
                return;
            }

            Editor.ViewportSelection.ClearSelection(Editor);

            foreach (var ent in curMapContainer.Objects)
            {
                var curName = "";
                var result = ent.GetPropertyValue("ModelName");

                if (result != null)
                {
                    curName = (string)result;
                }

                if (curName != "" && targetNames.Contains(curName))
                {
                    Editor.ViewportSelection.AddSelection(Editor, ent);
                }
            }
        }
        else
        {
            PlatformUtils.Instance.MessageBox("No object selected.", "Smithbox", MessageBoxButtons.OK);
        }
    }

    public void SelectAllByMapObjectType()
    {
        if (Editor.ViewportSelection.IsSelection())
        {
            var sel = Editor.ViewportSelection.GetFilteredSelection<MsbEntity>().ToList();

            List<Type> targetTypes = new List<Type>();

            foreach (var entry in sel)
            {
                var curType = entry.WrappedObject.GetType();
                if (!targetTypes.Contains(curType))
                {
                    targetTypes.Add(curType);
                }
            }

            var curMap = Editor.Selection.SelectedMapView.MapID;
            MapContainer curMapContainer = null;

            foreach (var entry in Project.MapData.PrimaryBank.Maps)
            {
                var mapID = entry.Key.Filename;
                var map = entry.Value.MapContainer;

                if (map != null)
                {
                    if (curMap == mapID)
                    {
                        curMapContainer = (MapContainer)map;
                    }
                }
            }

            if (curMapContainer == null)
            {
                PlatformUtils.Instance.MessageBox("Failed to select map container.", "Smithbox", MessageBoxButtons.OK);
                return;
            }

            Editor.ViewportSelection.ClearSelection(Editor);

            foreach (var ent in curMapContainer.Objects)
            {
                var curType = ent.WrappedObject.GetType();

                if (targetTypes.Contains(curType))
                {
                    Editor.ViewportSelection.AddSelection(Editor, ent);
                }
            }
        }
        else
        {
            PlatformUtils.Instance.MessageBox("No object selected.", "Smithbox", MessageBoxButtons.OK);
        }
    }
    private void HelpSection()
    {
        var tableFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders;

        ImGui.Text("Precede the command with ! to select the invert.");

        if (ImGui.CollapsingHeader("Name", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.BeginTable($"nameSelectionTable", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Contents", ImGuiTableColumnFlags.WidthFixed);

                // Name
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Command");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("<string>");

                // Description
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Description");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Select map objects whose name matches, or partially matches the specified string.");

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Property Value", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.BeginTable($"propValueSelectionTable", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Contents", ImGuiTableColumnFlags.WidthFixed);

                // Name
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Command");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("prop: <property name> [<index>] <comparator> <value>");

                // Description
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Description");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Select map objects who possess the specified property, and where the " +
                    "\nproperty's value is equal, less than or greater than the specified value.");

                // Parameter 1
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Parameters");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("<property name>: the name of the property to target." +
                    "\nTarget a slot in an array property with the [] syntax.");

                // Parameter 2
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("<comparator>: the comparator to use." +
                    "\nAccepted symbols: =, <, >");

                // Parameter 3
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("<value>: the value to check for.");

                // Example 1
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Examples");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("prop:EntityID = 1" +
                    "\nSelect all map objects with an Entity ID equal to 1.");

                // Example 2
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("prop:EntityID > 1000" +
                    "\nSelect all map objects with an Entity ID equal or greater than 1000.");

                // Example 3
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("");

                ImGui.TableSetColumnIndex(1);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("prop:EntityGroupIDs[1] < 999" +
                    "\nSelect all map objects with an EntityGroupID (at index 1) equal or less than 999");

                ImGui.EndTable();
            }
        }
    }
    private void ConfigureSelection()
    {
        var windowWidth = ImGui.GetWindowWidth();

        //--------------
        // Actions
        //--------------
        // Documentation
        if (ImGui.Button($"{Icons.QuestionCircle}##selectionHintButton", DPI.IconButtonSize))
        {
            DisplayHelp = !DisplayHelp;
        }
        UIHelper.Tooltip("View documentation on selection commands.");

        ImGui.SameLine();

        // Add
        if (ImGui.Button($"{Icons.Plus}##selectionAdd", DPI.IconButtonSize))
        {
            SelectionInputs.Add("");
        }
        UIHelper.Tooltip("Add new selection input row.");

        ImGui.SameLine();

        // Remove
        if (SelectionInputs.Count < 2)
        {
            ImGui.BeginDisabled();

            if (ImGui.Button($"{Icons.Minus}##selectionRemoveDisabled", DPI.IconButtonSize))
            {
                SelectionInputs.RemoveAt(SelectionInputs.Count - 1);
            }
            UIHelper.Tooltip("Remove last added selection input row.");

            ImGui.EndDisabled();
        }
        else
        {
            if (ImGui.Button($"{Icons.Minus}##selectionRemove", DPI.IconButtonSize))
            {
                SelectionInputs.RemoveAt(SelectionInputs.Count - 1);
                UIHelper.Tooltip("Remove last added selection input row.");
            }
        }

        ImGui.SameLine();

        // Reset
        if (ImGui.Button("Reset##resetSelectionInput", DPI.StandardButtonSize))
        {
            SelectionInputs = new List<string>() { "" };
        }
        UIHelper.Tooltip("Reset selection input rows.");

        ImGui.SameLine();

        // Conditional Logic
        if (ImGui.BeginCombo($"##selectionCommandLogic", MapObjectSelectionLogic.GetDisplayName()))
        {
            foreach (var entry in Enum.GetValues(typeof(SelectionConditionLogic)))
            {
                var curEnum = (SelectionConditionLogic)entry;

                if (ImGui.Selectable($"{curEnum.GetDisplayName()}", MapObjectSelectionLogic == curEnum))
                {
                    MapObjectSelectionLogic = curEnum;
                }
            }

            ImGui.EndCombo();
        }
        UIHelper.Tooltip("The logic with which to handle the selection inputs." +
            "\n\nAll must match means all the selection criteria must be true for the map object to be included." +
            "\n\nOne must match means only one of the selection criteria must be true for the map object to be included.");

        //--------------
        // Selection Inputs
        //--------------
        for (int i = 0; i < SelectionInputs.Count; i++)
        {
            var curCommand = SelectionInputs[i];
            var curText = curCommand;

            ImGui.SetNextItemWidth(520f * DPI.UIScale());
            if (ImGui.InputText($"##selectionInput{i}", ref curText, 255))
            {
                SelectionInputs[i] = curText;
            }
            UIHelper.Tooltip("The selection command to process.");
        }
    }
    private bool IsValidMapObject(MapContentView curView, MsbEntity mEnt)
    {
        var invertTruth = false;
        var isValid = true;

        if (MapObjectSelectionLogic is SelectionConditionLogic.OR)
        {
            isValid = false;
        }

        bool[] partTruth = new bool[SelectionInputs.Count];

        for (int i = 0; i < SelectionInputs.Count; i++)
        {
            var cmd = SelectionInputs[i];

            if (cmd.StartsWith("!"))
            {
                cmd = cmd.Replace("!", "");
                invertTruth = true;
            }

            if (cmd.Contains("prop:"))
            {
                partTruth[i] = Editor.MassEditTool.PropertyValueFilter(curView, mEnt, cmd);
                if (invertTruth)
                    partTruth[i] = !partTruth[i];
            }
            // Default to name filter if no explicit command is used
            else
            {
                partTruth[i] = Editor.MassEditTool.PropertyNameFilter(curView, mEnt, cmd);
                if (invertTruth)
                    partTruth[i] = !partTruth[i];
            }
        }

        foreach (bool entry in partTruth)
        {
            if (MapObjectSelectionLogic is SelectionConditionLogic.AND)
            {
                if (!entry)
                    isValid = false;
            }
            else if (MapObjectSelectionLogic is SelectionConditionLogic.OR)
            {
                if (entry)
                    isValid = true;
            }
        }

        return isValid;
    }
}