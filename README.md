# GSN Diagram Editor

A lightweight Windows WPF MVVM app for constructing GSN diagrams using a large editable canvas.

## Current version

This version removes the SVG preview/export and Graphviz dependency. The app focuses on fast manual editing.

## Features

- Add GSN shapes
- Add simple free text labels without any shape
- Double-click empty canvas to place free text at that location
- Drag nodes and text labels anywhere
- Connect GSN shapes
- Delete selected nodes/connections/text
- Zoom using slider or Ctrl + mouse wheel
- Large editing canvas
- Grid background
- Snap to grid
- Basic undo/redo
- Simple auto-layout for GSN shapes
- Save/open editable JSON project files
- Export DOT file if needed

## Run

```bash
dotnet restore
dotnet run
```

## Notes

- Free text labels are saved as project items in JSON.
- Free text labels are intentionally not used as connection endpoints.
- SVG and Graphviz files have been removed to keep the tool simple and lighter.
