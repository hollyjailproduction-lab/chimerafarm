# AGENTS.md

Unity 6 (6000.0.23f1) farming game built on the **Farming Engine** asset (indiemarc). URP 17.0.3, Input System only (`activeInputHandler: 2`), .NET Standard 2.1.

## Asset vs. project code

- `Assets/FarmingEngine/` is a **licensed third-party asset** (see `Assets/FarmingEngine/Licence.txt` — no redistribution of its source). The full scripting API is documented in `Assets/FarmingEngine/FarmingEngine.pdf` and `ReadMe.txt`. Treat it as a library: prefer extending over modifying.
- The project has essentially **no first-party code yet** (only Unity template boilerplate in `Assets/TutorialInfo/`). New game code should live in new top-level folders outside `Assets/FarmingEngine/`.
- The only asmdef is `FarmingEngine/Scripts/NavMesh` (NavMeshComponents, MIT). Everything else compiles into `Assembly-CSharp`.

## Scenes & input

- Real playable scenes are the build settings: `Assets/FarmingEngine/Scenes/{Farm,House,Test,Mine}.unity`. `Assets/Scenes/SampleScene.unity` is a leftover URP template scene, not in build settings.
- `Assets/InputSystem_Actions.inputactions` is the project's Input System actions file.
- Farming Engine's own input (`PlayerControls` etc.) still uses **legacy `KeyCode`**; the Input System integration is an opt-in upgrade package: `Assets/FarmingEngine/Upgrades/NewInputSystem.unitypackage` (see ReadMe.txt integration steps, e.g. EventSystem must use `InputSystemUIInputModule`, `Single Unified Pointer`).
- URP renderers/pipeline assets are in `Assets/Settings/` (PC_RPAsset / Mobile_RPAsset).

## Unity-specific gotchas

- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and the generated `.csproj`/`.sln` are Unity-managed — never hand-edit; regenerate via the editor. For a quick typecheck, `dotnet build ChimeraFarm.sln` works once the editor has generated project files (rerun editor reimport after adding new files).
- No CLI build/test — the editor compile is the feedback loop.
- Save files are JSON written to `Application.persistentDataPath` via `FarmingEngine.SaveTool`; `Assets/FarmingEngine/Scripts/Editor/DeleteSave.cs` clears them (menu: Tools > Delete Save File).
- Scenes/prefabs rely on `UniqueID` (GUID-like) references — `Assets/FarmingEngine/Scripts/Editor/GenerateUIDs.cs` / `ClearUIDs.cs` / `UniqueIDEditor.cs` exist to manage them; duplicate/paste objects and IDs break save and linking logic.
- `.vscode/settings.json` excludes Unity-generated files from the file tree and points dotnet at `ChimeraFarm.sln`.
- Not a git repository (no `.gitignore`); if initializing one, exclude the folders above.
