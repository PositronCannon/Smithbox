﻿using Microsoft.Extensions.Logging;
using StudioCore.Core;
using StudioCore.Editors.ParamEditor;
using StudioCore.Editors.ParamEditor.Tools;
using StudioCore.Resource.Locators;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StudioCore.Formats.JSON;

// -------------- Param Memory Offsets --------------
public class GameOffsetResource
{
    public string exeName { get; set; }
    public List<GameOffsetReference> list { get; set; }
}

public class GameOffsetReference
{
    public string exeVersion { get; set; }

    public List<GameOffsetBase> bases { get; set; }
}

public class GameOffsetBase
{
    public string paramBaseAob { get; set; }
    public string paramBaseAobRelativeOffset { get; set; }
    public string paramBase { get; set; }
    public string paramInnerPath { get; set; }
    public string paramCountOffset { get; set; }
    public string paramDataOffset { get; set; }
    public string rowPointerOffset { get; set; }
    public string rowHeaderSize { get; set; }

    public string ERItemGiveFuncOffset { get; set; }
    public string ERMapItemManOffset { get; set; }

    public List<string> paramOffsets { get; set; }
    public List<string> itemIDCategories { get; set; }
}

// -------------- Param Categories --------------
public class ParamCategoryResource
{
    public List<ParamCategoryEntry> Categories { get; set; }
}

public class ParamCategoryEntry
{
    public bool ForceBottom { get; set; } = false;
    public bool ForceTop { get; set; } = false;

    public string DisplayName { get; set; }
    public List<string> Params { get; set; }
}

// -------------- Commutative Param Groups --------------
public class ParamCommutativeResource
{
    public List<ParamCommutativeEntry> Groups { get; set; }
}

public class ParamCommutativeEntry
{
    public string Name { get; set; }
    public List<string> Params { get; set; }
}

// -------------- Param Type Info --------------
public class ParamTypeInfo
{
    /// <summary>
    /// Filename : Param Type string
    /// </summary>
    public Dictionary<string, string> Mapping { get; set; }

    /// <summary>
    /// This is for params that need skip the !defs.ContainsKey(curParam.ParamType) check (e.g. EquipParamWeapon_Npc)
    /// </summary>
    public List<string> Exceptions { get; set; }
}

// -------------- Row Name Store --------------
public class RowNameStore
{
    public List<RowNameParam> Params { get; set; }
}

public class RowNameStoreLegacy
{
    public List<RowNameParamLegacy> Params { get; set; }
}

public class RowNameParam
{
    public string Name { get; set; }

    public List<RowNameEntry> Entries { get; set; }
}
public class RowNameParamLegacy
{
    public string Name { get; set; }

    public List<RowNameEntryLegacy> Entries { get; set; }
}

public class RowNameEntry
{
    public int ID { get; set; }

    public List<string> Entries { get; set; }
}

public class RowNameEntryLegacy
{
    public int ID { get; set; }
    public int Index { get; set; }
    public string Name { get; set; }
}


// -------------- Param Upgrader Instructions --------------
public class ParamUpgraderInfo
{
    public string MaxVersion { get; set; }

    public List<OldRegulationEntry> RegulationEntries { get; set; }

    public List<UpgraderMassEditEntry> UpgradeCommands { get; set; }
}

public class OldRegulationEntry
{
    public string Version { get; set; }
    public string Folder { get; set; }
}

public class UpgraderMassEditEntry
{
    public string Version { get; set; }
    public string Message { get; set; }
    public string Command { get; set; }
}

// -------------- Graph Legends --------------
public class GraphLegends
{
    public List<GraphLegendEntry> Entries { get; set; }
}

public class GraphLegendEntry
{
    public string Param { get; set; }
    public string RowID { get; set; }
    public string X { get; set; }
    public string Y { get; set; }
}

// -------------- Icon Configurations --------------
public class IconConfigurations
{
    public List<IconConfigurationEntry> Configurations { get; set; }
}

public class IconConfigurationEntry
{
    public string Name { get; set; }
    public string File { get; set; }
    public string SubTexturePrefix { get; set; }
    public List<string> InternalFiles { get; set; }
}