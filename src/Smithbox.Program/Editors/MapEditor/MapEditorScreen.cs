﻿using Hexa.NET.ImGui;
using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Configuration;
using StudioCore.Core;
using StudioCore.Editor;
using StudioCore.Editors.MapEditor.Actions.Viewport;
using StudioCore.Editors.MapEditor.Core;
using StudioCore.Editors.MapEditor.Framework;
using StudioCore.Editors.MapEditor.Framework.MassEdit;
using StudioCore.Editors.MapEditor.PropertyEditor;
using StudioCore.Editors.MapEditor.Tools;
using StudioCore.Editors.MapEditor.Tools.DisplayGroups;
using StudioCore.Editors.MapEditor.Tools.EntityIdentifierOverview;
using StudioCore.Editors.MapEditor.Tools.MapQuery;
using StudioCore.Editors.MapEditor.Tools.NavmeshEdit;
using StudioCore.Editors.MapEditor.Tools.SelectionGroups;
using StudioCore.Editors.MapEditor.Tools.WorldMap;
using StudioCore.Interface;
using StudioCore.MsbEditor;
using StudioCore.Platform;
using StudioCore.Program.Editors.MapEditor.Tools;
using StudioCore.Resource;
using StudioCore.Settings;
using StudioCore.Tasks;
using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using static StudioCore.Editors.MapEditor.Framework.MapActionHandler;

namespace StudioCore.Editors.MapEditor;

/// <summary>
/// Main interface for the MSB Editor.
/// </summary>
public class MapEditorScreen : EditorScreen
{
    public Smithbox BaseEditor;
    public ProjectEntry Project;

    /// <summary>
    /// Lock variable used to handle pauses to the Update() function.
    /// </summary>
    private static readonly object _lock_PauseUpdate = new();
    private bool GCNeedsCollection;
    private bool _PauseUpdate;

    public ViewportActionManager EditorActionManager = new();
    public MapActionHandler ActionHandler;
    public ViewportSelection ViewportSelection = new();
    public MapSelection Selection;
    public Universe Universe;
    public MapEntityTypeCache EntityTypeCache;
    public EditorFocusManager FocusManager;
    public MapPropertyCache MapPropertyCache = new();
    public MapCommandQueue CommandQueue;
    public MapShortcuts Shortcuts;

    public HavokCollisionManager CollisionManager;

    // Core
    public MapViewportView MapViewportView;
    public MapListView MapListView;
    public MapPropertyView MapPropertyView;

    // Menubar
    public BasicFilters BasicFilters;
    public RegionFilters RegionFilters;
    public MapContentFilters MapContentFilter;

    // Tools
    public ToolWindow ToolWindow;
    public ToolSubMenu ToolSubMenu;

    // Actions
    public CreateAction CreateAction;
    public DuplicateAction DuplicateAction;
    public DeleteAction DeleteAction;
    public DuplicateToMapAction DuplicateToMapAction;
    public MoveToMapAction MoveToMapAction;
    public ReorderAction ReorderAction;
    public GotoAction GotoAction;
    public FrameAction FrameAction;
    public PullToCameraAction PullToCameraAction;
    public RotateAction RotateAction;
    public ScrambleAction ScrambleAction;
    public ReplicateAction ReplicateAction;
    public RenderTypeAction RenderTypeAction;
    public SelectAllAction SelectAllAction;
    public EditorVisibilityAction EditorVisibilityAction;
    public GameVisibilityAction GameVisibilityAction;
    public SelectionOutlineAction SelectionOutlineAction;
    public AdjustToGridAction AdjustToGridAction;
    public EntityInfoAction EntityInfoAction;
    public EntityIdCheckAction EntityIdCheckAction;
    public EntityRenameAction EntityRenameAction;

    // Tools
    public MassEditTool MassEditTool;
    public RotationCycleConfigTool RotationCycleConfigTool;
    public MovementCycleConfigTool MovementCycleConfigTool;
    public TreasureMakerTool TreasureMakerTool;
    public ModelSelectorTool ModelSelectorTool;
    public DisplayGroupTool DisplayGroupTool;
    public SelectionGroupTool SelectionGroupTool;
    public PrefabTool PrefabTool;
    public NavmeshBuilderTool NavmeshBuilderTool;
    public LocalSearchTool LocalSearchView;
    public GlobalSearchTool GlobalSearchTool;
    public WorldMapTool WorldMapTool;
    public EntityIdentifierTool EntityIdentifierTool;
    public MapGridTool MapGridTool;
    public WorldMapLayoutTool WorldMapLayoutTool;

    // Special Tools
    public AutomaticPreviewTool AutomaticPreviewTool;

    public MapEditorScreen(Smithbox baseEditor, ProjectEntry project)
    {
        BaseEditor = baseEditor;
        Project = project;

        MapViewportView = new MapViewportView(this, project, baseEditor);
        MapViewportView.Setup();

        Universe = new Universe(this, project);
        FocusManager = new EditorFocusManager(this);
        EntityTypeCache = new(this);

        Selection = new(this, project);

        // Core Views
        MapListView = new MapListView(this, project);
        MapPropertyView = new MapPropertyView(this);

        // Optional Views
        BasicFilters = new BasicFilters(this);
        RegionFilters = new RegionFilters(this);
        MapContentFilter = new MapContentFilters(this);

        // Framework
        ActionHandler = new MapActionHandler(this, project);
        CommandQueue = new MapCommandQueue(this);
        Shortcuts = new MapShortcuts(this);

        CollisionManager = new HavokCollisionManager(this, project);

        // Tools
        ToolWindow = new ToolWindow(this, ActionHandler);
        ToolSubMenu = new ToolSubMenu(this, ActionHandler);

        // Actions
        CreateAction = new CreateAction(this, project);
        DuplicateAction = new DuplicateAction(this, project);
        DeleteAction = new DeleteAction(this, project);
        DuplicateToMapAction = new DuplicateToMapAction(this, project);
        MoveToMapAction = new MoveToMapAction(this, project);
        ReorderAction = new ReorderAction(this, project);
        GotoAction = new GotoAction(this, project);
        FrameAction = new FrameAction(this, project);
        PullToCameraAction = new PullToCameraAction(this, project);
        RotateAction = new RotateAction(this, project);
        ScrambleAction = new ScrambleAction(this, project);
        ReplicateAction = new ReplicateAction(this, project);
        RenderTypeAction = new RenderTypeAction(this, project);
        SelectAllAction = new SelectAllAction(this, project);
        EditorVisibilityAction = new EditorVisibilityAction(this, project);
        GameVisibilityAction = new GameVisibilityAction(this, project);
        SelectionOutlineAction = new SelectionOutlineAction(this, project);
        AdjustToGridAction = new AdjustToGridAction(this, project);
        EntityInfoAction = new EntityInfoAction(this, project);
        EntityIdCheckAction = new EntityIdCheckAction(this, project);
        EntityRenameAction = new EntityRenameAction(this, project);

        // Tools
        MassEditTool = new MassEditTool(this, project);
        RotationCycleConfigTool = new RotationCycleConfigTool(this, project);
        MovementCycleConfigTool = new MovementCycleConfigTool(this, project);
        TreasureMakerTool = new TreasureMakerTool(this, project);
        AutomaticPreviewTool = new AutomaticPreviewTool(this, project);
        DisplayGroupTool = new DisplayGroupTool(this, project);
        GlobalSearchTool = new GlobalSearchTool(this, project);
        LocalSearchView = new LocalSearchTool(this, project);
        ModelSelectorTool = new ModelSelectorTool(this, project);
        PrefabTool = new PrefabTool(this, project);
        SelectionGroupTool = new SelectionGroupTool(this, project);
        NavmeshBuilderTool = new NavmeshBuilderTool(this, project);
        EntityIdentifierTool = new EntityIdentifierTool(this, project);
        MapGridTool = new MapGridTool(this, project);
        WorldMapTool = new WorldMapTool(this, project);
        WorldMapLayoutTool = new WorldMapLayoutTool(this, project);

        // Focus
        FocusManager.SetDefaultFocusElement("Properties##mapeditprop");
        EditorActionManager.AddEventHandler(MapListView);
    }

    private bool PauseUpdate
    {
        get
        {
            lock (_lock_PauseUpdate)
            {
                return _PauseUpdate;
            }
        }
        set
        {
            lock (_lock_PauseUpdate)
            {
                _PauseUpdate = value;
            }
        }
    }

    public string EditorName => "Map Editor";
    public string CommandEndpoint => "map";
    public string SaveType => "Maps";
    public string WindowName => "";
    public bool HasDocked { get; set; }

    public void OnGUI(string[] initcmd)
    {
        if (Project.IsInitializing)
            return;

        var scale = DPI.UIScale();

        // Docking setup
        //var vp = ImGui.GetMainViewport();
        Vector2 wins = ImGui.GetWindowSize();
        Vector2 winp = ImGui.GetWindowPos();
        winp.Y += 20.0f * scale;
        wins.Y -= 20.0f * scale;
        ImGui.SetNextWindowPos(winp);
        ImGui.SetNextWindowSize(wins);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0.0f);
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
                                 ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        flags |= ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
        flags |= ImGuiWindowFlags.NoBackground;
        //ImGui.Begin("DockSpace_MapEdit", flags);
        ImGui.PopStyleVar(4);
        var dsid = ImGui.GetID("DockSpace_MapEdit");
        ImGui.DockSpace(dsid, new Vector2(0, 0));

        Shortcuts.Monitor();
        ToolSubMenu.Shortcuts();
        CommandQueue.Parse(initcmd);

        DuplicateToMapAction.OnGui();
        MoveToMapAction.OnGui();
        SelectAllAction.OnGui();
        AdjustToGridAction.OnGui();

        ImGui.PushStyleColor(ImGuiCol.Text, UI.Current.ImGui_Default_Text_Color);
        ImGui.SetNextWindowSize(new Vector2(300, 500) * scale, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(20, 20) * scale, ImGuiCond.FirstUseEver);

        Vector3 clear_color = new(114f / 255f, 144f / 255f, 154f / 255f);
        //ImGui.Text($@"Viewport size: {Viewport.Width}x{Viewport.Height}");
        //ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGui.GetIO().Framerate, ImGui.GetIO().Framerate));

        if (ImGui.BeginMenuBar())
        {
            FileMenu();
            EditMenu();
            ViewMenu();
            ToolMenu();

            ImGui.EndMenuBar();
        }

        MapViewportView.OnGui();
        MapListView.OnGui();

        if (Smithbox.FirstFrame)
        {
            ImGui.SetNextWindowFocus();
        }

        if (MapPropertyView.Focus)
        {
            MapPropertyView.Focus = false;
            ImGui.SetNextWindowFocus();
        }

        MapPropertyView.OnGui(ViewportSelection, "mapeditprop", MapViewportView.Viewport.Width, MapViewportView.Viewport.Height);

        SelectionGroupTool.OnGui();
        LocalSearchView.OnGui();
        WorldMapTool.DisplayWorldMap();

        ResourceLoadWindow.DisplayWindow(MapViewportView.Viewport.Width, MapViewportView.Viewport.Height);
        if (CFG.Current.Interface_MapEditor_ResourceList)
        {
            ResourceListWindow.DisplayWindow("mapResourceList");
        }

        if (CFG.Current.Interface_MapEditor_ToolWindow)
        {
            ToolWindow.OnGui();
        }

        ImGui.PopStyleColor(1);

        FocusManager.OnFocus();
    }

    public void OnDefocus()
    {
        FocusManager.ResetFocus();
    }

    public void Update(float dt)
    {
        if (Project.IsInitializing)
            return;

        if (GCNeedsCollection)
        {
            GC.Collect();
            GCNeedsCollection = false;
        }

        if (PauseUpdate)
        {
            return;
        }

        MapViewportView.Update(dt);

        // Throw any exceptions that ocurred during async map loading.
        if (Universe.LoadMapExceptions != null)
        {
            Universe.LoadMapExceptions.Throw();
        }
    }

    public void EditorResized(Sdl2Window window, GraphicsDevice device)
    {
        MapViewportView.EditorResized(window, device);
    }

    public void FileMenu()
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem($"Save", $"{KeyBindings.Current.CORE_Save.HintText}"))
            {
                Save();
            }

            if (ImGui.MenuItem($"Save All", $"{KeyBindings.Current.CORE_SaveAll.HintText}"))
            {
                SaveAll();
            }

            ImGui.EndMenu();
        }
    }

    public void EditMenu()
    {
        if (ImGui.BeginMenu("Edit"))
        {
            // Undo
            if (ImGui.MenuItem($"Undo", $"{KeyBindings.Current.CORE_UndoAction.HintText} / {KeyBindings.Current.CORE_UndoContinuousAction.HintText}"))
            {
                if (EditorActionManager.CanUndo())
                {
                    EditorActionManager.UndoAction();
                }
            }

            // Undo All
            if (ImGui.MenuItem($"Undo All"))
            {
                if (EditorActionManager.CanUndo())
                {
                    EditorActionManager.UndoAllAction();
                }
            }

            // Redo
            if (ImGui.MenuItem($"Redo", $"{KeyBindings.Current.CORE_RedoAction.HintText} / {KeyBindings.Current.CORE_RedoContinuousAction.HintText}"))
            {
                if (EditorActionManager.CanRedo())
                {
                    EditorActionManager.RedoAction();
                }
            }

            ImGui.Separator();

            DuplicateAction.OnMenu();
            DeleteAction.OnMenu();
            RotateAction.OnMenu();
            ScrambleAction.OnMenu();
            ReplicateAction.OnMenu();
            RenderTypeAction.OnMenu();

            ImGui.Separator();

            CreateAction.OnMenu();
            DuplicateToMapAction.OnMenu();
            MoveToMapAction.OnMenu();

            ImGui.Separator();

            GotoAction.OnMenu();
            FrameAction.OnMenu();   
            PullToCameraAction.OnMenu();

            ImGui.Separator();

            ReorderAction.OnMenu();

            ImGui.Separator();

            EditorVisibilityAction.OnMenu();
            GameVisibilityAction.OnMenu();

            ImGui.EndMenu();
        }
    }

    public void ViewMenu()
    {
        // Dropdown: View
        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Viewport"))
            {
                CFG.Current.Interface_Editor_Viewport = !CFG.Current.Interface_Editor_Viewport;
            }
            UIHelper.ShowActiveStatus(CFG.Current.Interface_Editor_Viewport);

            if (ImGui.MenuItem("Map List"))
            {
                CFG.Current.Interface_MapEditor_MapList = !CFG.Current.Interface_MapEditor_MapList;
            }
            UIHelper.ShowActiveStatus(CFG.Current.Interface_MapEditor_MapList);

            if (ImGui.MenuItem("Map Contents"))
            {
                CFG.Current.Interface_MapEditor_MapContents = !CFG.Current.Interface_MapEditor_MapContents;
            }
            UIHelper.ShowActiveStatus(CFG.Current.Interface_MapEditor_MapContents);

            if (ImGui.MenuItem("Properties"))
            {
                CFG.Current.Interface_MapEditor_Properties = !CFG.Current.Interface_MapEditor_Properties;
            }
            UIHelper.ShowActiveStatus(CFG.Current.Interface_MapEditor_Properties);

            if (ImGui.MenuItem("Tool Window"))
            {
                CFG.Current.Interface_MapEditor_ToolWindow = !CFG.Current.Interface_MapEditor_ToolWindow;
            }
            UIHelper.ShowActiveStatus(CFG.Current.Interface_MapEditor_ToolWindow);

            if (ImGui.MenuItem("Resource List"))
            {
                CFG.Current.Interface_MapEditor_ResourceList = !CFG.Current.Interface_MapEditor_ResourceList;
            }
            UIHelper.ShowActiveStatus(CFG.Current.Interface_MapEditor_ResourceList);

            ImGui.Separator();

            if (ImGui.MenuItem("Map List: Categories"))
            {
                CFG.Current.MapEditor_DisplayMapCategories = !CFG.Current.MapEditor_DisplayMapCategories;
            }
            UIHelper.ShowActiveStatus(CFG.Current.MapEditor_DisplayMapCategories);

            ImGui.Separator();

            // Quick toggles for some of the Field Editor field visibility options

            if (ImGui.MenuItem("Field: Community Names"))
            {
                CFG.Current.MapEditor_Enable_Commmunity_Names = !CFG.Current.MapEditor_Enable_Commmunity_Names;
            }
            UIHelper.ShowActiveStatus(CFG.Current.MapEditor_Enable_Commmunity_Names);

            if (ImGui.MenuItem("Field: Unknowns"))
            {
                CFG.Current.MapEditor_DisplayUnknownFields = !CFG.Current.MapEditor_DisplayUnknownFields;
            }
            UIHelper.ShowActiveStatus(CFG.Current.MapEditor_DisplayUnknownFields);

            ImGui.EndMenu();
        }
    }

    public void ToolMenu()
    {
        var validViewportState = MapViewportView.RenderScene != null &&
            MapViewportView.Viewport != null;

        // Tools
        ToolSubMenu.DisplayMenu();
    }

    public void FilterMenu()
    {
        var validViewportState = MapViewportView.RenderScene != null &&
            MapViewportView.Viewport != null;

        // Filters
        if (ImGui.BeginMenu("Filters", validViewportState))
        {
            BasicFilters.Display();

            RegionFilters.DisplayOptions();

            if (ImGui.BeginMenu("Filter Presets"))
            {
                if (ImGui.MenuItem(CFG.Current.SceneFilter_Preset_01.Name))
                {
                    MapViewportView.RenderScene.DrawFilter = CFG.Current.SceneFilter_Preset_01.Filters;
                }

                if (ImGui.MenuItem(CFG.Current.SceneFilter_Preset_02.Name))
                {
                    MapViewportView.RenderScene.DrawFilter = CFG.Current.SceneFilter_Preset_02.Filters;
                }

                if (ImGui.MenuItem(CFG.Current.SceneFilter_Preset_03.Name))
                {
                    MapViewportView.RenderScene.DrawFilter = CFG.Current.SceneFilter_Preset_03.Filters;
                }

                if (ImGui.MenuItem(CFG.Current.SceneFilter_Preset_04.Name))
                {
                    MapViewportView.RenderScene.DrawFilter = CFG.Current.SceneFilter_Preset_04.Filters;
                }

                if (ImGui.MenuItem(CFG.Current.SceneFilter_Preset_05.Name))
                {
                    MapViewportView.RenderScene.DrawFilter = CFG.Current.SceneFilter_Preset_05.Filters;
                }

                if (ImGui.MenuItem(CFG.Current.SceneFilter_Preset_06.Name))
                {
                    MapViewportView.RenderScene.DrawFilter = CFG.Current.SceneFilter_Preset_06.Filters;
                }

                ImGui.EndMenu();
            }

            if (Project.ProjectType is ProjectType.ER)
            {
                if (ImGui.BeginMenu("Collision Type"))
                {
                    if (ImGui.MenuItem("Low"))
                    {
                        CollisionManager.VisibleCollisionType = HavokCollisionType.Low;
                    }
                    UIHelper.Tooltip("Visible collision will use the low-detail mesh.\nUsed for standard collision.\nMap must be reloaded after change to see difference.");
                    UIHelper.ShowActiveStatus(CollisionManager.VisibleCollisionType == HavokCollisionType.Low);

                    if (ImGui.MenuItem("High"))
                    {
                        CollisionManager.VisibleCollisionType = HavokCollisionType.High;
                    }
                    UIHelper.Tooltip("Visible collision will use the high-detail mesh.\nUsed for IK.\nMap must be reloaded after change to see difference.");
                    UIHelper.ShowActiveStatus(CollisionManager.VisibleCollisionType == HavokCollisionType.High);

                    ImGui.EndMenu();
                }
            }

            CFG.Current.LastSceneFilter = MapViewportView.RenderScene.DrawFilter;

            ImGui.EndMenu();
        }
    }

    public void Draw(GraphicsDevice device, CommandList cl)
    {
        if (Project.IsInitializing)
            return;

        if (MapViewportView.Viewport != null)
        {
            MapViewportView.Draw(device, cl);
        }
    }

    public bool InputCaptured()
    {
        return MapViewportView.InputCaptured();
    }

    public void Save()
    {
        if (Project.ProjectType == ProjectType.Undefined)
            return;

        try
        {
            Universe.SaveAllMaps();
        }
        catch (SavingFailedException e)
        {
            HandleSaveException(e);
        }

        // Save the configuration JSONs
        BaseEditor.SaveConfiguration();
    }

    public void SaveAll()
    {
        if (Project.ProjectType == ProjectType.Undefined)
            return;

        try
        {
            Universe.SaveAllMaps();
        }
        catch (SavingFailedException e)
        {
            HandleSaveException(e);
        }

        // Save the configuration JSONs
        BaseEditor.SaveConfiguration();
    }

    public void OnEntityContextMenu(Entity ent)
    {
        /*
        if (ImGui.Selectable("Create prefab"))
        {
            _activeModal = new CreatePrefabModal(Universe, ent);
        }
        */
    }

    public void ReloadUniverse()
    {
        Universe.UnloadAllMaps();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        CreateAction.PopulateClassNames();
    }

    public void HandleSaveException(SavingFailedException e)
    {
        if (e.Wrapped is MSB.MissingReferenceException eRef)
        {
            TaskLogs.AddLog(e.Message,
                LogLevel.Error, LogPriority.Normal, e.Wrapped);

            DialogResult result = PlatformUtils.Instance.MessageBox($"{eRef.Message}\nSelect referring map entity?",
                "Failed to save map",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);
            if (result == DialogResult.Yes)
            {
                foreach (var entry in Project.MapData.MapFiles.Entries)
                {
                    var currentContainer = GetMapContainerFromMapID(entry.Filename);

                    if (currentContainer != null)
                    {
                        foreach (Entity obj in currentContainer.Objects)
                        {
                            if (obj.WrappedObject == eRef.Referrer)
                            {
                                ViewportSelection.ClearSelection(this);
                                ViewportSelection.AddSelection(this, obj);
                                FrameAction.ApplyViewportFrame();
                                return;
                            }
                        }
                    }
                }

                TaskLogs.AddLog($"Unable to find map entity \"{eRef.Referrer.Name}\"",
                    LogLevel.Error, LogPriority.High);
            }
        }
        else
        {
            TaskLogs.AddLog(e.Message,
                LogLevel.Error, LogPriority.High, e.Wrapped);
        }
    }

    public MapContainer GetMapContainerFromMapID(string mapID)
    {
        var targetMap = Project.MapData.PrimaryBank.Maps.FirstOrDefault(e => e.Key.Filename == mapID);

        if (targetMap.Value != null && targetMap.Value.MapContainer != null)
        {
            return targetMap.Value.MapContainer;
        }

        return null;
    }

    public bool IsAnyMapLoaded()
    {
        var check = false;

        foreach(var entry in Project.MapData.PrimaryBank.Maps)
        {
            if(entry.Value.MapContainer != null)
            {
                check = true;
            }
        }

        return check;
    }
}
