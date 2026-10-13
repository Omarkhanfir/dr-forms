using UnityEditor;
using UnityEngine;
using System.Linq;
// Scoped to the new art folder; existing models, GUIDs and project settings stay unchanged.
public sealed class CourtyardArtImport : AssetPostprocessor {
 public override uint GetVersion()=>2;
 void OnPreprocessModel(){if(!assetPath.Contains("/Resources/Courtyard/"))return;var model=(ModelImporter)assetImporter;
 model.globalScale=1;model.addCollider=false;
 bool character=assetPath.EndsWith("Hero.fbx")||assetPath.EndsWith("Guardian.fbx")||assetPath.EndsWith("Acolyte.fbx");
 model.importAnimation=character;if(character){model.animationType=ModelImporterAnimationType.Legacy;}
 }
 void OnPreprocessAnimation(){
  if(!assetPath.Contains("/Resources/Courtyard/"))return;
  var model=(ModelImporter)assetImporter;
  var names=new[]{"Idle","Running_A","Walking_A","Spellcast_Shoot","Spellcasting","Hit_A","Death_A","1H_Melee_Attack_Slice_Horizontal"};
  var clips=model.defaultClipAnimations.Where(c=>names.Any(n=>c.name.EndsWith(n))).ToArray();
  if(clips.Length>0)model.clipAnimations=clips;
 }
 void OnPostprocessModel(GameObject root){
  if(!assetPath.Contains("/Resources/Courtyard/"))return;
  bool hero=assetPath.EndsWith("Hero.fbx");bool enemy=assetPath.EndsWith("Guardian.fbx")||assetPath.EndsWith("Acolyte.fbx");
  if(!hero&&!enemy)return;
  // Original Dr.forms skins retain authored material slots and bone weights.
 }
 void OnPreprocessTexture(){if(!assetPath.Contains("/Resources/Courtyard/"))return;var texture=(TextureImporter)assetImporter;texture.maxTextureSize=1024;texture.mipmapEnabled=!assetPath.EndsWith("Panel.png")&&!assetPath.EndsWith("TouchRing.png");texture.alphaIsTransparency=true;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.wrapMode=TextureWrapMode.Clamp;}
}
