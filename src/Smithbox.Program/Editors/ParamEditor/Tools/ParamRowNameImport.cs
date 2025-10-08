﻿using Hexa.NET.ImGui;
using StudioCore.Core;
using StudioCore.Interface;
using StudioCore.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StudioCore.Editors.ParamEditor.Data.ParamBank;

namespace StudioCore.Editors.ParamEditor.Tools;

public partial class ParamTools
{
    public void DisplayRowNameImportMenu()
    {
        if (ImGui.BeginMenu("Import"))
        {
            if (ImGui.BeginMenu("Community Names"))
        {
                if (ImGui.MenuItem($"Selected Param"))
                {
                    Project.ParamData.PrimaryBank.ImportRowNamesForParam(ImportRowNameSourceType.Community, Editor._activeView.Selection.GetActiveParam());
                }
                if (ImGui.MenuItem($"All"))
                {
                    Project.ParamData.PrimaryBank.ImportRowNames( ImportRowNameSourceType.Community);
                }

                ImGui.EndMenu();
            }

            if (ParamUtils.HasDeveloperRowNames(Project))
            {
                if (ImGui.BeginMenu("Developer Names"))
                {
                    if (ImGui.MenuItem($"Selected Param"))
                    {
                        Project.ParamData.PrimaryBank.ImportRowNamesForParam(ImportRowNameSourceType.Developer, Editor._activeView.Selection.GetActiveParam());
                    }
                    if (ImGui.MenuItem($"All"))
                    {
                        Project.ParamData.PrimaryBank.ImportRowNames(ImportRowNameSourceType.Developer);
                    }
                    ImGui.EndMenu();
                }
            }

            if (ImGui.BeginMenu("From JSON File"))
            {
                if (ImGui.MenuItem($"Selected Param"))
                {
                    var filePath = "";
                    var result = PlatformUtils.Instance.OpenFolderDialog("Select row name folder", out filePath);

                    if (result)
                    {
                        Project.ParamData.PrimaryBank.ImportRowNamesForParam(ImportRowNameSourceType.External, Editor._activeView.Selection.GetActiveParam(), filePath);
                    }
                }

                if (ImGui.MenuItem($"All"))
                {
                    var filePath = "";
                    var result = PlatformUtils.Instance.OpenFolderDialog("Select row name folder", out filePath);

                    if (result)
                    {
                        Project.ParamData.PrimaryBank.ImportRowNames(ImportRowNameSourceType.External, filePath);
                    }
                }
                ImGui.EndMenu();
            }


            if (ImGui.BeginMenu("From CSV File"))
            {
                if (ImGui.MenuItem($"Selected Param"))
                {
                    var filePath = "";
                    var result = PlatformUtils.Instance.OpenFileDialog("Select row name CSV text file", ["csv"], out filePath);

                    if (result)
                    {
                        Project.ParamData.PrimaryBank.ImportRowNamesForParam_CSV(filePath, Editor._activeView.Selection.GetActiveParam());
                    }
                }
                UIHelper.Tooltip("This will import the external names from a CSV file, matching via row ID.");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("From Legacy Name Folder"))
            {
                if (ImGui.MenuItem($"All"))
                {
                    var folderPath = "";
                    var result = PlatformUtils.Instance.OpenFolderDialog("Select legacy row name folder", out folderPath);

                    if (result)
                    {
                        Project.ParamData.PrimaryBank.ImportRowNamesForParam_Legacy(folderPath);
                    }
                }
                UIHelper.Tooltip("This will import the external names from a legacy row name file (older Stripped Row Name folder), matching via row index.");

                if (ImGui.MenuItem($"Selected Param"))
                {
                    var folderPath = "";
                    var result = PlatformUtils.Instance.OpenFolderDialog("Select legacy row name folder", out folderPath);

                    if (result)
                    {
                        Project.ParamData.PrimaryBank.ImportRowNamesForParam_Legacy(folderPath, Editor._activeView.Selection.GetActiveParam());
                    }
                }
                UIHelper.Tooltip("This will import the external names from a legacy row name file (Stripped Row Name folder), matching via row index.");

                ImGui.EndMenu();
            }

            ImGui.Separator();

            ImGui.Checkbox("Replace Empty Names Only", ref CFG.Current.Param_RowNameImport_ReplaceEmptyNamesOnly);
            UIHelper.Tooltip("If enabled, only rows with empty names will have their row names replaced with the import name.");

            ImGui.EndMenu();
        }

    }
}
