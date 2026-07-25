<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/f64b9366-9cd0-4840-bb5b-b2ab4198cfd1" />
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/65371fdf-2a81-4001-910c-38c314539aa9">
  <img alt="Dying Light Map Dumping Tools" src="" width="auto">
</picture>

Tools for dumping original Dying Light maps into a form that can be opened in the official Dying Light Developer Tools.

The game ships most map geometry in compiled `.sobj` files and keeps the closest editable map data in compiled `.exp` files. The official editor cannot open those game maps directly as normal workshop projects. This project automates the extraction and patching steps needed to inspect and edit them.

This project continues work originally started by [Brendon](https://github.com/12brendon34), with permission.

## What It Does

- Scans a Dying Light installation for PAK files that contain maps.
- Dumps `.sobj` map geometry into readable entity data.
- Generates `.eds` files for manual workflows.
- Creates editor-ready workshop map folders in automatic mode.
- Copies the original map folder files from the game PAK.
- Uses the original `.exp` as the closest available `.map` baseline.
- Injects dumped `ModelObject` geometry back into the `.map`.
- Recovers compiled static lights from embedded `SLIs` records as editable `LightObject` records.
- Copies loose retail `.light` files that contain environment-probe lighting data.
- Stages built-terrain resource packs when a map references `_buildterrain_` meshes.

## Requirements

- Windows.
- Dying Light installed through Steam or another local install.
- Dying Light Developer Tools installed.
- .NET 8 Desktop Runtime if you use a published release.
- .NET 8 SDK if you want to build from source.

Default paths used by the GUI:

```text
C:\Program Files (x86)\Steam\steamapps\common\Dying Light
C:\Program Files (x86)\Steam\steamapps\common\Dying Light\DevTools\workshop
```

Custom install paths are supported through the Browse buttons.

## Projects In This Repository

- `AutoMapDumperGUI` - the main Windows Forms application, also handles light recovery.
- `SO_Dumper` - reads Dying Light `.sobj` files and prints entity data.
- `Map2EDS` - converts dumped SOBJ text into `.eds` for manual use.

## Recommended Usage

Use automatic mode for normal map dumping.

1. Open `AutoMapDumperGUI.exe`.
2. Click `Browse Game` if the game path was not detected automatically.
3. Go to the automatic tab.
4. Set the output folder to your workshop project's `data\maps` folder.

Example:

```text
C:\Program Files (x86)\Steam\steamapps\common\Dying Light\DevTools\workshop\MyProject\data\maps
```

5. Select one map, one PAK group, or the root node for all maps.
6. Click `Dump Selected Map`, `Dump Selected Group`, or `Dump ALL Maps`.
7. Wait until the log says `ALL OPERATIONS COMPLETE`.
8. Open the project in Dying Light Developer Tools.

Automatic output for a map named `slums_int_school` will look like:

```text
MyProject
└── data
    └── maps
        └── slums_int_school
            ├── slums_int_school.map
            ├── slums_int_school.exp
            ├── slums_int_school.sobj
            ├── slums_int_school0.light
            ├── slums_int_school1.light
            └── other original map files...
```

## Manual Mode (OLD)

Manual mode is mostly for old workflows and debugging.

It extracts the selected `.sobj`, runs `SO18_Dumper`, and generates a `.eds` file using `Map2EDS`. It does not create the full patched `.map` workflow and does not perform the exact static-light recovery used by automatic mode.

Use manual mode only if you specifically need the `.eds` workflow.

## Build From Source

From the repository root:

```powershell
dotnet build MapTools.sln -c Release
```

The main executable will be under:

```text
AutoMapDumperGUI\bin\Release\net8.0-windows\AutoMapDumperGUI.exe
```

To create a release ZIP (RECOMMENDED):

```powershell
.\release.ps1
```

That script publishes the GUI and helper tools into `DLMDT_Release`, then creates a `DLMDT_vX_Y_Z.zip` archive based on the GUI project version.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
