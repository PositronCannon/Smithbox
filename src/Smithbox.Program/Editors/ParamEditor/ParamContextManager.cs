﻿using Hexa.NET.ImGui;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudioCore.Core;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Editors.ParamEditor;

public class ParamContextManager
{
    public ParamEditorScreen Editor;
    public ProjectEntry Project;

    public ParamEditorContext CurrentContext = ParamEditorContext.None;

    public ParamContextManager(ParamEditorScreen editor, ProjectEntry project)
    {
        Editor = editor;
        Project = project;
    }

    public void SetWindowContext(ParamEditorContext context)
    {
        if(ImGui.IsWindowHovered())
        {
            CurrentContext = context;
            //TaskLogs.AddLog($"Context: {context.GetDisplayName()}");
        }
    }

    public void SetColumnContext(ParamEditorContext context)
    {
        var result = ImGui.TableGetColumnFlags() & ImGuiTableColumnFlags.IsHovered;
        if (result is ImGuiTableColumnFlags.IsHovered)
        {
            CurrentContext = context;
            //TaskLogs.AddLog($"Context: {context.GetDisplayName()}");
        }
    }
}

public enum ParamEditorContext
{
    [Display(Name = "None")] None,
    [Display(Name = "Param List")] ParamList,
    [Display(Name = "Table Group List")] TableGroupList,
    [Display(Name = "Row List")] RowList,
    [Display(Name = "Field List")] FieldList,
}
