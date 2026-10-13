"""Original Dr.forms meshes, rigid skin weights and authored skeletal actions."""
import bpy, math, sys
from pathlib import Path
out=Path(sys.argv[sys.argv.index('--')+1])
palette={'Ivory':(.94,.88,.70,1),'Teal':(.035,.24,.25,1),'Skin':(.72,.43,.24,1),'Hair':(.9,.92,.83,1),'Green':(.62,.88,.16,1),'Violet':(.50,.23,.70,1),'Glass':(.30,.75,.76,1),'Black':(.025,.07,.09,1)}
def build(name):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 mats={}
 for k,c in palette.items():
  m=bpy.data.materials.new(k);m.diffuse_color=c;mats[k]=m
 bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='LaboratorySkeleton'
 bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
 specs={'Root':((0,0,0),(0,0,.8),None),'Body':((0,0,.8),(0,0,1.6),'Root'),'Head':((0,0,1.6),(0,0,2.3),'Body'),'Arm.L':((-.43,0,1.5),(-.43,0,1.23),'Body'),'Arm.R':((.43,0,1.5),(.43,0,1.23),'Body'),'Forearm.L':((-.43,0,1.23),(-.43,0,.95),'Arm.L'),'Forearm.R':((.43,0,1.23),(.43,0,.95),'Arm.R'),'Leg.L':((-.22,0,.8),(-.22,0,.44),'Root'),'Leg.R':((.22,0,.8),(.22,0,.44),'Root'),'Shin.L':((-.22,0,.44),(-.22,0,.12),'Leg.L'),'Shin.R':((.22,0,.44),(.22,0,.12),'Leg.R'),'Coat.L':((-.22,.08,1.0),(-.22,.08,.65),'Body'),'Coat.R':((.22,.08,1.0),(.22,.08,.65),'Body'),'handslot.r':((.43,0,.95),(.43,0,.8),'Forearm.R')}
 for k,(a,b,parent) in specs.items():
  bone=rig.data.edit_bones.new(k);bone.head=a;bone.tail=b
  if parent:bone.parent=rig.data.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT')
 parts=[]
 def shape(k,p,s,mat,bone='Body',box=False):
  if box:bpy.ops.mesh.primitive_cube_add(size=2,location=p)
  else:bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=p)
  o=bpy.context.object;o.name=k;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  if box:
   mod=o.modifiers.new('Rounded','BEVEL');mod.width=.07;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
  o.data.materials.append(mats[mat]);g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE');parts.append(o)
 if name=='Hero':
  shape('Lab coat',(0,0,1.22),(.42,.25,.46),'Ivory',box=True)
  for x in [-.22,.22]:shape('Coat tails',(x,.08,.82),(.20,.24,.20),'Ivory','Coat.L' if x<0 else 'Coat.R',True)
  shape('Teal shirt',(0,-.255,1.3),(.12,.035,.27),'Teal',box=True)
  for z in [1.1,1.3,1.5]:shape('Coat buttons',(.19,-.28,z),(.035,.025,.035),'Black')
  shape('Head',(0,0,1.97),(.35,.29,.36),'Skin','Head')
  for x in [-.20,.20]:
   shape('Oversized goggle rim',(x,-.22,2.25),(.22,.16,.16),'Teal','Head')
   shape('Goggle lens',(x,-.32,2.31),(.165,.085,.09),'Glass','Head')
   shape('Lens shine',(x-.045,-.37,2.35),(.05,.025,.025),'Ivory','Head')
  shape('Nose',(0,-.35,1.88),(.085,.13,.10),'Skin','Head')
  for i in range(7):
   x=(i-3)*.12;shape('Wild swept hair',(x,.15,2.30+(.12 if i%2 else 0)),(.12,.23,.20),'Hair','Head')
  shape('Flask satchel',(-.47,.05,1.0),(.18,.27,.23),'Violet')
  for y,c in [(-.15,'Green'),(.12,'Glass')]:shape('Satchel flask',(-.57,y,1.20),(.10,.10,.14),c)
 else:
  c='Green' if name=='Guardian' else 'Violet'
  shape('Specimen body',(0,0,1.13),(.50,.33,.56),c,box=name=='Guardian')
  shape('Specimen head',(0,0,1.8),(.43,.34,.34),c,'Head')
  for x in [-.19,.19]:
   shape('Eye socket',(x,-.30,1.87),(.16,.07,.16),'Teal','Head');shape('Ivory eye',(x,-.365,1.87),(.10,.03,.10),'Ivory','Head');shape('Pupil',(x,-.398,1.87),(.045,.012,.065),'Black','Head')
  shape('Experiment collar',(0,0,1.52),(.40,.35,.085),'Ivory')
  for x in [-.3,0,.3]:shape('Specimen sprouts',(x,.04,2.17),(.09,.12,.22),'Green' if name=='Acolyte' else 'Violet','Head')
  shape('Chemical core',(0,-.34,1.15),(.18,.055,.24),'Glass')
 for side,x in [('L',-.22),('R',.22)]:
  shape('Trouser '+side,(x,0,.64),(.16,.18,.20),'Teal','Leg.'+side)
  shape('Lower trouser '+side,(x,0,.34),(.14,.16,.18),'Teal','Shin.'+side)
  shape('Boot '+side,(x,-.09,.15),(.19,.27,.16),'Black','Shin.'+side,True)
 for side,x in [('L',-.48),('R',.48)]:
  shape('Sleeve '+side,(x,0,1.36),(.16,.18,.17),'Ivory' if name=='Hero' else ('Green' if name=='Guardian' else 'Violet'),'Arm.'+side)
  shape('Cuff '+side,(x,0,1.10),(.14,.16,.17),'Ivory' if name=='Hero' else 'Teal','Forearm.'+side)
  shape('Glove '+side,(x,-.03,.94),(.17,.19,.17),'Teal','Forearm.'+side)
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='DrForms_'+name
 mod=body.modifiers.new('Skin','ARMATURE');mod.object=rig;body.parent=rig
 rig.animation_data_create()
 for action_name,frames in [('Idle',60),('Running_A',24),('Walking_A',36),('Spellcast_Shoot',15),('Spellcasting',60),('Hit_A',12),('Death_A',30),('1H_Melee_Attack_Slice_Horizontal',15)]:
  action=bpy.data.actions.new(action_name);rig.animation_data.action=action
  for f in range(0,frames+1,3):
   t=f/frames;wave=math.sin(t*math.tau)
   for bone in rig.pose.bones:
    bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
   if action_name in ['Running_A','Walking_A']:
    for side,sign in [('L',1),('R',-1)]:
     rig.pose.bones['Leg.'+side].rotation_euler.x=wave*.65*sign;rig.pose.bones['Shin.'+side].rotation_euler.x=-max(0,-wave*sign)*.75;rig.pose.bones['Arm.'+side].rotation_euler.x=-wave*.4*sign;rig.pose.bones['Coat.'+side].rotation_euler.x=wave*.10*sign
    rig.pose.bones['Root'].location.z=abs(wave)*.055;rig.pose.bones['Body'].rotation_euler.z=wave*.045
   elif action_name in ['Spellcast_Shoot','1H_Melee_Attack_Slice_Horizontal']:
    # .10 second release in a .25 second clip: arm moves from back to forward.
    angle= .9*t/.25 if t<.25 else .9-(t-.25)/.15*1.8 if t<.4 else -.9-(t-.4)/.25*.4 if t<.65 else -1.3+(t-.65)/.35*1.3
    rig.pose.bones['Arm.R'].rotation_euler.x=angle;rig.pose.bones['Forearm.R'].rotation_euler.x=max(0,math.sin(t*math.pi*2))*.75;rig.pose.bones['Body'].rotation_euler.z=math.sin(t*math.pi)*.10
   elif action_name=='Hit_A':rig.pose.bones['Body'].rotation_euler.x=-math.sin(t*math.pi)*.22
   elif action_name=='Death_A':rig.pose.bones['Root'].rotation_euler.x=t*1.45
   else:
    rig.pose.bones['Body'].rotation_euler.x=wave*.025;rig.pose.bones['Head'].rotation_euler.z=wave*.055
   for bone in rig.pose.bones:
    bone.keyframe_insert('rotation_euler',frame=f);bone.keyframe_insert('location',frame=f)
 bpy.context.scene.render.fps=60;rig.animation_data.action=None
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
for name in ['Hero','Guardian','Acolyte']:build(name)
