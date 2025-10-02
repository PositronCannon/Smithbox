﻿using Hexa.NET.ImGui;
using Microsoft.Extensions.Logging;
using StudioCore.Configuration;
using StudioCore.Core;
using StudioCore.Editors.MapEditor.Framework;
using StudioCore.Formats.JSON;
using StudioCore.Interface;
using StudioCore.Platform;
using StudioCore.Utilities;
using StudioCore.ViewportNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace StudioCore.Editors.MapEditor.Tools.SelectionGroups;

public class SelectionGroupTool
{
    public MapEditorScreen Editor;
    public ProjectEntry Project;

    public SelectionGroupTool(MapEditorScreen editor, ProjectEntry project)
    {
        Editor = editor;
        Project = project;
    }

    private string selectedResourceName = "";
    private List<string> selectedResourceTags = new List<string>();
    private List<string> selectedResourceContents = new List<string>();
    private int selectedResourceKeybind = -1;

    private string createPromptGroupName = "";
    private string createPromptTags = "";

    private string editPromptGroupName = "";
    private string editPromptTags = "";
    private int editPromptKeybind = -1;

    private string editPromptOldGroupName = "";

    private string _searchInput = "";

    private int currentKeyBindOption = -1;
    private List<int> keyBindOptions = new List<int>() { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    public bool OpenPopup = false;

    /// <summary>
    /// Update Loop
    /// </summary>
    public void OnGui()
    {
        if (Editor.Project.ProjectType == ProjectType.Undefined)
            return;

        if (Editor.Project.MapData.MapObjectSelections.Resources == null)
            return;

        if (OpenPopup)
        {
            CreateSelectionGroup("External");
            OpenPopup = false;
        }

        // This exposes the pop-up to the map editor
        if (ImGui.BeginPopup("##selectionGroupModalExternal"))
        {
            DisplayCreationModal();

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Shortcuts
    /// </summary>
    public void OnShortcut()
    {
        if (CFG.Current.Shortcuts_MapEditor_EnableSelectionGroupShortcuts)
        {
            // Selection Groups
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_CreateSelectionGroup))
            {
                CreateSelectionGroup("External");
            }

            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup_0))
            {
                ShortcutSelectGroup(0);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup_1))
            {
                ShortcutSelectGroup(1);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup_2))
            {
                ShortcutSelectGroup(2);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup_3))
            {
                ShortcutSelectGroup(3);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup4))
            {
                ShortcutSelectGroup(4);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup5))
            {
                ShortcutSelectGroup(5);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup6))
            {
                ShortcutSelectGroup(6);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup7))
            {
                ShortcutSelectGroup(7);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup8))
            {
                ShortcutSelectGroup(8);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup9))
            {
                ShortcutSelectGroup(9);
            }
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_SelectionGroup10))
            {
                ShortcutSelectGroup(10);
            }
        }
    }

    /// <summary>
    /// Context Menu
    /// </summary>
    public void OnContext()
    {
        if (ImGui.Selectable("Create Selection Group"))
        {
            OpenPopup = true;
        }
        UIHelper.Tooltip($"Create a selection group from the current selection.\n\nShortcut: {KeyBindings.Current.MAP_CreateSelectionGroup.HintText}");
    }

    /// <summary>
    /// Tool Window
    /// </summary>
    public void OnToolWindow()
    {
        if (Editor.Project.ProjectType == ProjectType.Undefined)
            return;

        if (Editor.Project.MapData.MapObjectSelections.Resources == null)
            return;

        if (ImGui.CollapsingHeader("Selection Groups"))
        {
            var windowSize = DPI.GetWindowSize(Editor.BaseEditor._context);
            var sectionWidth = ImGui.GetWindowWidth() * 0.95f;
            var topSectionHeight = windowSize.Y * 0.1f;
            var bottomSectionHeight = windowSize.Y * 0.3f;
            var topSectionSize = new Vector2(sectionWidth * DPI.UIScale(), topSectionHeight * DPI.UIScale());
            var bottomSectionSize = new Vector2(sectionWidth * DPI.UIScale(), bottomSectionHeight * DPI.UIScale());

            var windowWidth = ImGui.GetWindowWidth();

            if (ImGui.BeginPopup("##selectionGroupModalInternal"))
            {
                DisplayCreationModal();

                ImGui.EndPopup();
            }

            UIHelper.SimpleHeader("Selection Groups", "Selection Groups", "", UI.Current.ImGui_Default_Text_Color);

            ImGui.BeginChild("##selectionGroupList", topSectionSize, ImGuiChildFlags.Borders);

            ImGui.InputText($"##selectionGroupFilter", ref _searchInput, 255);
            UIHelper.Tooltip("Filter the selection group list. Separate terms are split via the + character.");

            ImGui.Separator();

            foreach (var entry in Editor.Project.MapData.MapObjectSelections.Resources)
            {
                var displayName = $"{entry.Name}";

                if (CFG.Current.MapEditor_SelectionGroup_ShowKeybind)
                {
                    if (entry.SelectionGroupKeybind != -1)
                    {
                        var keyBind = GetSelectionGroupKeyBind(entry.SelectionGroupKeybind);
                        if (keyBind != null)
                        {
                            displayName = $"{displayName} [{keyBind.HintText}]";
                        }
                    }
                }

                if (CFG.Current.MapEditor_SelectionGroup_ShowTags)
                {
                    if (entry.Tags.Count > 0)
                    {
                        var tagString = string.Join(" ", entry.Tags);
                        displayName = $"{displayName} {{ {tagString} }}";
                    }
                }

                if (SearchFilters.IsSelectionSearchMatch(_searchInput, entry.Name, entry.Tags))
                {
                    if (ImGui.Selectable(displayName, selectedResourceName == entry.Name))
                    {
                        selectedResourceName = entry.Name;
                        selectedResourceTags = entry.Tags;
                        selectedResourceContents = entry.Selection;
                        selectedResourceKeybind = entry.SelectionGroupKeybind;

                        editPromptOldGroupName = entry.Name;
                        editPromptGroupName = entry.Name;
                        editPromptTags = AliasUtils.GetTagListString(entry.Tags);
                        editPromptKeybind = entry.SelectionGroupKeybind;

                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            SelectSelectionGroup();
                        }
                    }
                }
            }
            ImGui.EndChild();

            if (ImGui.Button("Create New Selection Group", DPI.WholeWidthButton(windowWidth, 24)))
            {
                CreateSelectionGroup("Internal");
            }
            UIHelper.Tooltip($"Shortcut: {KeyBindings.Current.MAP_CreateSelectionGroup.HintText}\nBring up the selection group creation menu to assign your current selection to a selection group.");


            UIHelper.SimpleHeader("Current Selection Group", "Current Selection Group", "", UI.Current.ImGui_Default_Text_Color);

            ImGui.BeginChild("##selectionGroupActions", bottomSectionSize, ImGuiChildFlags.Borders);

            if (selectedResourceName != "")
            {
                if (ImGui.Button("Select", DPI.ThirdWidthButton(bottomSectionSize.X, 24)))
                {
                    SelectSelectionGroup();
                }
                UIHelper.Tooltip("Select the map objects listed by your currently selected group.");

                ImGui.SameLine();

                if (ImGui.Button("Edit", DPI.ThirdWidthButton(bottomSectionSize.X, 24)))
                {
                    ImGui.OpenPopup($"##selectionGroupModalEdit");
                }
                UIHelper.Tooltip("Edit the name, tags and keybind for the selected group.");

                ImGui.SameLine();

                if (ImGui.Button("Delete", DPI.ThirdWidthButton(bottomSectionSize.X, 24)))
                {
                    DeleteSelectionGroup();
                }
                UIHelper.Tooltip("Delete this selected group.");

                if (ImGui.BeginPopup("##selectionGroupModalEdit"))
                {
                    DisplayEditModal();

                    ImGui.EndPopup();
                }

                if (selectedResourceTags.Count > 0)
                {
                    var tagString = string.Join(" ", selectedResourceTags);
                    if (tagString != "")
                    {
                        UIHelper.WrappedText("");
                        UIHelper.WrappedText("Tags:");
                        UIHelper.WrappedTextColored(UI.Current.ImGui_Default_Text_Color, tagString);
                        UIHelper.WrappedText("");
                    }
                }

                UIHelper.WrappedText("Contents:");
                foreach (var entry in selectedResourceContents)
                {
                    UIHelper.WrappedTextColored(UI.Current.ImGui_Benefit_Text_Color, entry);
                }
            }

            ImGui.EndChild();
        }
    }

    private void DisplayCreationModal()
    {
        var windowWidth = ImGui.GetWindowWidth();

        ImGui.InputText("Group Name##selectionGroup_GroupName", ref createPromptGroupName, 255);
        UIHelper.Tooltip("The name of the selection group.");
        ImGui.InputText("Tags##selectionGroup_Tags", ref createPromptTags, 255);
        UIHelper.Tooltip("Separate each tag with the , character as a delimiter.");

        var keyBind = GetSelectionGroupKeyBind(currentKeyBindOption);
        var previewString = "None";
        if (keyBind != null)
        {
            previewString = keyBind.HintText;
        }

        if (ImGui.BeginCombo("Keybind##keybindCombo", previewString))
        {
            foreach (var entry in keyBindOptions)
            {
                keyBind = GetSelectionGroupKeyBind(entry);
                var nameString = "None";
                if (keyBind != null)
                {
                    nameString = keyBind.HintText;
                }

                bool isSelected = currentKeyBindOption == entry;

                if (ImGui.Selectable($"{nameString}##{entry}", isSelected))
                {
                    currentKeyBindOption = entry;
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
        UIHelper.Tooltip("The keybind to quickly select the contents of this selection group.");

        if (ImGui.Button("Create Group", DPI.WholeWidthButton(windowWidth, 24)))
        {
            AmendSelectionGroupBank();
            ImGui.CloseCurrentPopup();
        }
    }

    private void DisplayEditModal()
    {
        var windowWidth = ImGui.GetWindowWidth();

        ImGui.InputText("Group Name##selectionGroup_GroupName", ref editPromptGroupName, 255);
        UIHelper.Tooltip("The name of the selection group.");
        ImGui.InputText("Tags##selectionGroup_Tags", ref editPromptTags, 255);
        UIHelper.Tooltip("Separate each tag with the , character as a delimiter.");

        var keyBind = GetSelectionGroupKeyBind(editPromptKeybind);
        var previewString = "None";
        if (keyBind != null)
        {
            previewString = keyBind.HintText;
        }

        if (ImGui.BeginCombo("Keybind##keybindCombo", previewString))
        {
            foreach (var entry in keyBindOptions)
            {
                keyBind = GetSelectionGroupKeyBind(entry);
                var nameString = "None";
                if (keyBind != null)
                {
                    nameString = keyBind.HintText;
                }

                bool isSelected = editPromptKeybind == entry;

                if (ImGui.Selectable($"{nameString}##{entry}", isSelected))
                {
                    editPromptKeybind = entry;
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
        UIHelper.Tooltip("The keybind to quickly select the contents of this selection group.");

        if (ImGui.Button("Edit Group", DPI.WholeWidthButton(windowWidth, 24)))
        {
            createPromptGroupName = editPromptGroupName;
            createPromptTags = editPromptTags;
            currentKeyBindOption = editPromptKeybind;

            AmendSelectionGroupBank(true);
            ImGui.CloseCurrentPopup();
        }
    }

    /// <summary>
    /// Effect
    /// </summary>

    public bool DeleteSelectionGroup(string currentResourceName)
    {
        var resource = Editor.Project.MapData.MapObjectSelections.Resources.Where(x => x.Name == currentResourceName).FirstOrDefault();

        Editor.Project.MapData.MapObjectSelections.Resources.Remove(resource);
        Editor.Project.MapData.SaveMapObjectSelections();

        return true;
    }

    public bool AddSelectionGroup(string name, List<string> tags, List<string> selection, int keybindIndex, bool isEdit = false, string oldName = "")
    {
        if (name == "")
        {
            PlatformUtils.Instance.MessageBox("Group name is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        else if (!isEdit && Editor.Project.MapData.MapObjectSelections.Resources.Any(x => x.Name == name))
        {
            PlatformUtils.Instance.MessageBox("Group name already exists.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        else if (!isEdit && selection == null)
        {
            PlatformUtils.Instance.MessageBox("Selection is invalid.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        else if (!isEdit && selection.Count == 0)
        {
            PlatformUtils.Instance.MessageBox("Selection is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        else if (keybindIndex != -1 && Editor.Project.MapData.MapObjectSelections.Resources.Any(x => x.SelectionGroupKeybind == keybindIndex))
        {
            var group = Editor.Project.MapData.MapObjectSelections.Resources.Where(x => x.SelectionGroupKeybind == keybindIndex).First();
            if (isEdit)
            {
                group = Editor.Project.MapData.MapObjectSelections.Resources.Where(x => x.SelectionGroupKeybind == keybindIndex && x.Name != name).First();
            }
            PlatformUtils.Instance.MessageBox($"Keybind already assigned to another selection group: {group.Name}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        else
        {
            // Delete old entry, since we will create it a-new with the edits immediately
            if (isEdit)
            {
                DeleteSelectionGroup(oldName);
            }

            var res = new EntitySelectionGroupResource();
            res.Name = name;
            res.Tags = tags;
            res.Selection = selection;
            res.SelectionGroupKeybind = keybindIndex;

            Editor.Project.MapData.MapObjectSelections.Resources.Add(res);
            Editor.Project.MapData.SaveMapObjectSelections();
        }

        return false;
    }
    public void CreateSelectionGroup(string type)
    {
        if (CFG.Current.MapEditor_SelectionGroup_AutoCreation)
        {
            if (Editor.ViewportSelection.GetSelection().Count != 0)
            {
                var ent = (Entity)Editor.ViewportSelection.GetSelection().First();
                createPromptGroupName = ent.Name;
                createPromptTags = "";
                AmendSelectionGroupBank();
            }
        }
        else
        {
            ImGui.OpenPopup($"##selectionGroupModal{type}");
        }
    }

    private void DeleteSelectionGroup()
    {
        var result = DialogResult.Yes;

        if (CFG.Current.MapEditor_SelectionGroup_ConfirmDelete)
        {
            result = PlatformUtils.Instance.MessageBox($"You are about to delete this selection group. Are you sure?", $"Smithbox", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        }

        if (result == DialogResult.Yes)
        {
            DeleteSelectionGroup(selectedResourceName);

            selectedResourceName = "";
            selectedResourceTags = new List<string>();
            selectedResourceContents = new List<string>();

            Editor.Project.MapData.SaveMapObjectSelections();
        }
    }

    private void SelectSelectionGroup()
    {
        Editor.ViewportSelection.ClearSelection(Editor);

        List<Entity> entities = new List<Entity>();

        // TODO: add something to prevent confusion if multiple maps are loaded with the same names within
        foreach (var entry in Editor.Project.MapData.PrimaryBank.Maps)
        {
            if (entry.Value.MapContainer != null)
            {
                foreach (var mapObj in entry.Value.MapContainer.Objects)
                {
                    if (selectedResourceContents.Contains(mapObj.Name))
                    {
                        //TaskLogs.AddLog(mapObj.Name);
                        entities.Add(mapObj);
                    }
                }
            }
        }

        foreach (var entry in entities)
        {
            Editor.ViewportSelection.AddSelection(Editor, entry);
        }

        if (CFG.Current.MapEditor_SelectionGroup_FrameSelection)
        {
            Editor.FrameAction.ApplyViewportFrame();
            Editor.GotoAction.GotoMapObjectEntry();
        }
    }

    private void AmendSelectionGroupBank(bool isEdit = false)
    {
        List<string> tagList = AliasUtils.GetTagList(createPromptTags);
        List<string> selectionList = new List<string>();

        if (isEdit)
        {
            selectionList = selectedResourceContents;
        }
        else
        {
            foreach (var entry in Editor.ViewportSelection.GetSelection())
            {
                var ent = (Entity)entry;
                selectionList.Add(ent.Name);
            }
        }

        if (AddSelectionGroup(createPromptGroupName, tagList, selectionList, currentKeyBindOption, isEdit, editPromptOldGroupName))
        {
            Editor.Project.MapData.SaveMapObjectSelections();
        }
    }

    public void ShortcutSelectGroup(int index)
    {
        if (Editor.Project.MapData.MapObjectSelections.Resources == null)
            return;

        foreach (var entry in Editor.Project.MapData.MapObjectSelections.Resources)
        {
            if (entry.SelectionGroupKeybind == index)
            {
                selectedResourceName = entry.Name;
                selectedResourceTags = entry.Tags;
                selectedResourceContents = entry.Selection;

                SelectSelectionGroup();
            }
        }
    }

    private KeyBind GetSelectionGroupKeyBind(int index)
    {
        if (index == -1)
        {
            return null;
        }

        switch (index)
        {
            case 0: return KeyBindings.Current.MAP_SelectionGroup_0;
            case 1: return KeyBindings.Current.MAP_SelectionGroup_1;
            case 2: return KeyBindings.Current.MAP_SelectionGroup_2;
            case 3: return KeyBindings.Current.MAP_SelectionGroup_3;
            case 4: return KeyBindings.Current.MAP_SelectionGroup4;
            case 5: return KeyBindings.Current.MAP_SelectionGroup5;
            case 6: return KeyBindings.Current.MAP_SelectionGroup6;
            case 7: return KeyBindings.Current.MAP_SelectionGroup7;
            case 8: return KeyBindings.Current.MAP_SelectionGroup8;
            case 9: return KeyBindings.Current.MAP_SelectionGroup9;
            case 10: return KeyBindings.Current.MAP_SelectionGroup10;
            default: return null;
        }
    }
}
