﻿using StudioCore.Configuration;
using StudioCore.Editors.MapEditor;
using StudioCore.Scene;
using StudioCore.Scene.DebugPrimitives;
using StudioCore.Scene.Framework;
using System.Drawing;
using System.Numerics;
using Veldrid.Utilities;

namespace StudioCore.ViewportNS;

public class MapGrid
{
    private MapEditorScreen Editor;

    private DbgPrimWireGrid WireGridPrimitive;
    private DebugPrimitiveRenderableProxy Grid;
    private MeshRenderables RenderList;

    public MapGrid(MapEditorScreen editor, MeshRenderables renderlist, int size, float sectionSize, Vector3 color)
    {
        Editor = editor;
        RenderList = renderlist;

        WireGridPrimitive = new DbgPrimWireGrid(Color.Red, Color.Red, size, sectionSize);

        Grid = new DebugPrimitiveRenderableProxy(RenderList, WireGridPrimitive);
        Grid.BaseColor = GetViewGridColor(color);
    }

    private Color GetViewGridColor(Vector3 color)
    {
        return Color.FromArgb((int)(color.X * 255), (int)(color.Y * 255), (int)(color.Z * 255));
    }

    public void Regenerate(int size, float sectionSize, Vector3 color)
    {
        WireGridPrimitive.Dispose();
        Grid.Dispose();

        WireGridPrimitive = new DbgPrimWireGrid(Color.Red, Color.Red,
            size,
            sectionSize);

        Grid = new DebugPrimitiveRenderableProxy(RenderList, WireGridPrimitive);
        Grid.BaseColor = GetViewGridColor(color);
    }

    public void Update(bool displayGrid, Ray ray, int size, float sectionSize, Vector3 color, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, ref bool regenerateGrid)
    {
        if (regenerateGrid)
        {
            regenerateGrid = false;

            Regenerate(size, sectionSize, color);
        }

        if (displayGrid && Editor.Project.FocusedEditor is MapEditorScreen)
        {
            Grid.BaseColor = GetViewGridColor(color);
            Grid.Visible = true;
            Grid.World = new Transform(
                posX,
                posY,
                posZ,
                Utils.DegToRadians(rotX),
                Utils.DegToRadians(rotY),
                Utils.DegToRadians(rotZ)).WorldMatrix;
        }
        else
        {
            Grid.Visible = false;
        }
    }
}
