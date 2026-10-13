# Mosslight Courtyard art provenance

This milestone builds one representative first room, retaining the connected three-room game. No assets were purchased and no Unity/package versions were changed.

## Third-party assets

| Shipped asset | Creator/source | Licence and use | Changes |
| --- | --- | --- | --- |
| Hero.fbx, HeroAtlas.png, Wand.fbx | Kay Lousberg, [KayKit Adventurers 1.0](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0/tree/672074b73ba276876a19e8816ecdc5241817ab47) | [Creator's CC0 commercial-use grant](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0/blob/672074b73ba276876a19e8816ecdc5241817ab47/LICENSE.txt); retained copy: [licence](licenses/KayKit-adventurers-CC0.txt) | Knight rig/body/cape/helmet; unused weapon variants removed; eight skeletal clips retained by the Unity importer. Red cloth/plume atlas colours remapped to azure by hero_palette.py, preserving skin/metal/UVs. Wand attached to hand slot at runtime. |
| Guardian.fbx, Acolyte.fbx, EnemyAtlas.png, EnemyBlade.fbx, EnemyStaff.fbx | Kay Lousberg, [KayKit Skeletons 1.0](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0/tree/15b62b9bad122f72926c10fb14d622c73819fa54) | [Creator's CC0 commercial-use grant](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0/blob/15b62b9bad122f72926c10fb14d622c73819fa54/LICENSE.txt); retained copy: [licence](licenses/KayKit-skeletons-CC0.txt) | Warrior and Mage rigs retained, unused variants removed, eight clips retained by the Unity importer. Original atlas unchanged. Separate blade/staff hand accessories. |

The creator explicitly permits personal, educational and commercial projects in both distributed licence files. Credit is optional; it is recorded here anyway. CC0 details: https://creativecommons.org/publicdomain/zero/1.0/ . These assets are authored by Kay Lousberg, not original character models authored for Dr.forms.

[Download manifest](licenses/asset-downloads.json) records every original file's pinned source URL, repository revision and SHA-256. The creator's original approximately 20 MB character FBXs are retained as source assets to preserve their bind poses. The Unity importer includes only the eight selected clips and removes unused weapon renderers from the playable models. These authoring-file sizes are distinct from Web player size.

Retained clips: Idle, Running_A, Walking_A, Spellcast_Shoot, Spellcasting, Hit_A, Death_A, 1H_Melee_Attack_Slice_Horizontal. Animation is visual only; the existing CharacterController/combat rules move and damage actors. The first room retains two easy chasers, shown with distinct Guardian/Acolyte models; the actual ranged enemy still appears only in later rooms.

## Original project work

- Room.fbx and Gate.fbx: original Blender-authored courtyard paving/inlays, coursed softened stone walls, columns, wedge arch, carved gate, ivy/ferns, branching trees, banners and torch housings. No borrowed environment geometry.
- StoneGrain.png, Panel.png, TouchRing.png and Spark.png: original deterministic procedural texture/UI/VFX drawings made using Pillow. No external images or AI image generation.
- Runtime lighting, contact shadows, shared material palette, skeletal-clip selection, bounded spell sprites/trails and HUD treatment: original project implementation.

No third-party asset licence applies to these original project files. This task does not select a distribution licence for the application as a whole.

## Reproduce

Use the existing Blender 5.0.1 and Python/Pillow installations. No tool or Unity package installation is required. Preserve existing .meta files when regenerating asset contents.

```powershell
python Tools/Art/fetch_kaykit.py C:/Users/21654/DrFormsGame-WebVerification/ArtResearch
python Tools/Art/prepare_kaykit.py C:/Users/21654/DrFormsGame-WebVerification/ArtResearch C:/Users/21654/DrFormsGame/Assets/SourceFiles/Prototype/Resources/Courtyard
& 'C:/Program Files/Blender Foundation/Blender 5.0/blender.exe' -b -t 2 -P Tools/Art/build_courtyard.py -- C:/Users/21654/DrFormsGame/Assets/SourceFiles/Prototype/Resources/Courtyard
python Tools/Art/build_surface.py Assets/SourceFiles/Prototype/Resources/Courtyard
python Tools/Art/hero_palette.py C:/Users/21654/DrFormsGame-WebVerification/ArtResearch/adventurers-knight_texture.png Assets/SourceFiles/Prototype/Resources/Courtyard/HeroAtlas.png
```

The scoped CourtyardArtImport postprocessor configures the new models as Legacy skeletal clips and the new textures for Web compatibility; it does not process existing assets. Shared imported materials are replaced with URP materials at runtime. Models contain no gameplay colliders.

## Art direction and review limits

Inspected actual prior-build phone screenshots first. Biggest weaknesses: primitive character silhouettes, empty floor, flat unshaded block walls, little material depth and overbearing controls. The earlier handoff describes the reference as overhead framing, quiet ground, softened borders and translucent controls. Its image was not available in the current context; a path/link was requested. Until supplied, exact reference comparison cannot be claimed. The first room is the environment showcase; later rooms retain their simpler route geometry. Physical-phone/Safari visual quality and performance require device review.

Runtime shadow quality uses a cloned URP asset with one 1024 map and a 28-unit distance; the original quality asset is not mutated by gameplay. Mobile soft-shadow support/bias are the only intentional pipeline asset edits. [Shipped asset hashes](licenses/shipped-assets.json) distinguish original CC0 source files and the recoloured atlas from original project geometry/UI assets.

Retained licence copies have whitespace normalized only; grants and credits are unchanged. Original downloaded byte hashes remain in the download manifest.
