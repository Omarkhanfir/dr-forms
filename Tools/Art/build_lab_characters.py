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
 specs={'Root':((0,0,0),(0,0,.8),None),'Body':((0,0,.8),(0,0,1.6),'Root'),'Head':((0,0,1.6),(0,0,2.3),'Body'),'Arm.L':((-.43,0,1.5),(-.43,0,1.23),'Body'),'Arm.R':((.43,0,1.5),(.43,0,1.23),'Body'),'Forearm.L':((-.43,0,1.23),(-.43,0,.95),'Arm.L'),'Forearm.R':((.43,0,1.23),(.43,0,.95),'Arm.R'),'Leg.L':((-.22,0,.8),(-.22,0,.44),'Root'),'Leg.R':((.22,0,.8),(.22,0,.44),'Root'),'Shin.L':((-.22,0,.44),(-.22,0,.12),'Leg.L'),'Shin.R':((.22,0,.44),(.22,0,.12),'Leg.R'),'Coat.L':((-.22,.08,1.0),(-.22,.08,.65),'Body'),'Coat.R':((.22,.08,1.0),(.22,.08,.65),'Body'),'Satchel':((-.47,.05,1.12),(-.47,.05,.83),'Body'),'handslot.r':((.43,0,.95),(.43,0,.8),'Forearm.R')}
 for side,x in [('L',-.22),('R',.22)]:specs['Foot.'+side]=((x,0,.12),(x,-.25,.12),'Shin.'+side)
 if name!='Acolyte':
  for key,(a,b,parent) in list(specs.items()):
   if key.startswith(('Leg.','Shin.','Foot.')) or key=='Root':continue
   specs[key]=((a[0],a[1],a[2]+.18),(b[0],b[1],b[2]+.18),parent)
  for side,x in [('L',-.22),('R',.22)]:
   specs['Leg.'+side]=((x,0,1.0),(x,0,.55),'Root')
   specs['Shin.'+side]=((x,0,.55),(x,0,.12),'Leg.'+side)
 for k,(a,b,parent) in specs.items():
  bone=rig.data.edit_bones.new(k);bone.head=a;bone.tail=b
  if parent:bone.parent=rig.data.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT')
 parts=[]
 def shape(k,p,s,mat,bone='Body',box=False):
  if name!='Acolyte' and not bone.startswith(('Leg.','Shin.','Foot.')) and bone!='Root':p=(p[0],p[1],p[2]+.18)
  if box:bpy.ops.mesh.primitive_cube_add(size=2,location=p)
  else:bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=p)
  o=bpy.context.object;o.name=k;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  if box:
   mod=o.modifiers.new('Rounded','BEVEL');mod.width=.07;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
  o.data.materials.append(mats[mat]);g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE');parts.append(o)
 if name=='Hero':
  shape('Lab coat',(0,0,1.30),(.36,.23,.38),'Ivory',box=True)
  for x in [-.22,.22]:shape('Coat tails',(x,.10,.91),(.17,.20,.18),'Ivory','Coat.L' if x<0 else 'Coat.R',True)
  shape('Teal shirt',(0,-.255,1.3),(.12,.035,.27),'Teal',box=True)
  for z in [1.1,1.3,1.5]:shape('Coat buttons',(.19,-.28,z),(.035,.025,.035),'Black')
  shape('Head',(0,0,1.97),(.30,.26,.31),'Skin','Head')
  for x in [-.20,.20]:
   shape('Oversized goggle rim',(x,-.22,2.25),(.22,.16,.16),'Teal','Head')
   shape('Goggle lens',(x,-.32,2.31),(.165,.085,.09),'Glass','Head')
   shape('Lens shine',(x-.045,-.37,2.35),(.05,.025,.025),'Ivory','Head')
  shape('Nose',(0,-.35,1.88),(.085,.13,.10),'Skin','Head')
  for i in range(7):
   x=(i-3)*.10;shape('Wild swept hair',(x,.15,2.30+(.12 if i%2 else 0)),(.11,.19,.17),'Hair','Head')
  shape('Flask satchel',(-.47,.05,1.0),(.18,.27,.23),'Violet','Satchel')
  for y,c in [(-.15,'Green'),(.12,'Glass')]:shape('Satchel flask',(-.57,y,1.20),(.10,.10,.14),c,'Satchel')
 else:
  c='Green' if name=='Guardian' else 'Violet'
  shape('Specimen body',(0,0,1.13),(.46,.31,.43 if name=='Guardian' else .56),c,box=name=='Guardian')
  shape('Specimen head',(0,0,1.8),(.43,.34,.34),c,'Head')
  for x in [-.19,.19]:
   shape('Eye socket',(x,-.30,1.87),(.16,.07,.16),'Teal','Head');shape('Ivory eye',(x,-.365,1.87),(.10,.03,.10),'Ivory','Head');shape('Pupil',(x,-.398,1.87),(.045,.012,.065),'Black','Head')
  shape('Experiment collar',(0,0,1.52),(.40,.35,.085),'Ivory')
  for x in [-.3,0,.3]:shape('Specimen sprouts',(x,.04,2.17),(.09,.12,.22),'Green' if name=='Acolyte' else 'Violet','Head')
  shape('Chemical core',(0,-.34,1.15),(.18,.055,.24),'Glass')
 for side,x in [('L',-.22),('R',.22)]:
  shape('Trouser '+side,(x,0,.78 if name!='Acolyte' else .64),(.15,.17,.24 if name!='Acolyte' else .20),'Teal','Leg.'+side)
  shape('Lower trouser '+side,(x,0,.34),(.13,.15,.22 if name!='Acolyte' else .18),'Teal','Shin.'+side)
  shape('Boot '+side,(x,-.13,.15),(.19,.31,.16),'Teal','Foot.'+side,True)
  shape('Boot sole '+side,(x,-.13,.065),(.19,.31,.05),'Black','Foot.'+side,True)
  if name!='Acolyte':
   shape('Ivory toe cap '+side,(x,-.32,.13),(.16,.11,.07),'Ivory','Foot.'+side,True)
   shape('Knee plate '+side,(x,-.17,.54),(.085,.04,.07),'Ivory' if name=='Hero' else 'Glass','Shin.'+side)
 for side,x in [('L',-.48),('R',.48)]:
  shape('Sleeve '+side,(x,0,1.36),(.16,.18,.17),'Ivory' if name=='Hero' else ('Green' if name=='Guardian' else 'Violet'),'Arm.'+side)
  shape('Cuff '+side,(x,0,1.10),(.14,.16,.17),'Ivory' if name=='Hero' else 'Teal','Forearm.'+side)
  shape('Glove '+side,(x,-.03,.94),(.17,.19,.17),'Teal','Forearm.'+side)
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='DrForms_'+name
 mod=body.modifiers.new('Skin','ARMATURE');mod.object=rig;body.parent=rig
 rig.animation_data_create()
 for action_name,frames in [('Idle',120),('Running_A',32),('Walking_A',48),('Stopping',18),('Turning',24),('Spellcast_Shoot',15),('Spellcasting',78),('Hit_A',12),('Death_A',30),('1H_Melee_Attack_Slice_Horizontal',24)]:
  action=bpy.data.actions.new(action_name);rig.animation_data.action=action
  for f in sorted(set(range(0,frames+1,2)) | {frames,6}):
   t=f/frames;wave=math.sin(t*math.tau)
   for bone in rig.pose.bones:
    bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
   if action_name in ['Running_A','Walking_A']:
    running=action_name=='Running_A'
    stride_width=(.48 if running else .32) if name!='Acolyte' else (.34 if running else .23)
    if name=='Acolyte':stride_width*=.7
    bob=(1-math.cos(t*math.tau*2))*(.032 if running else .014)
    root_height=(-.16 if running else -.075)+bob
    # Root bone's local Y is world up; local Z is world backward.
    rig.pose.bones['Root'].location.y=root_height
    for side,sign in [('L',1),('R',-1)]:
     phase=(t+(0 if side=='L' else .5))%1
     # Linear stance travel, eased airborne return and bent swing knee.
     stride=-1+4*phase if phase<.5 else math.cos((phase-.5)*math.tau)
     knee=0 if phase<.5 else math.sin((phase-.5)*math.tau)
     asym=1.2 if name=='Guardian' and side=='L' else .88 if name=='Guardian' else 1
     # Authored two-segment solve: level boot, constant stance height,
     # linear backward stance travel and a lifted swing arc. No runtime IK.
     foot_y=stride*stride_width*(1.05 if name=='Guardian' and side=='L' else 1)
     upper,lower=(.45,.43) if name!='Acolyte' else (.36,.32)
     foot_y=stride*stride_width
     down=upper+lower+root_height-knee*(.24 if running else .16)
     bend=-math.acos(max(-1,min(1,(foot_y*foot_y+down*down-upper**2-lower**2)/(2*upper*lower))))
     hip=math.atan2(foot_y,down)-math.atan2(lower*math.sin(bend),upper+lower*math.cos(bend))
     rig.pose.bones['Leg.'+side].rotation_euler.x=hip
     rig.pose.bones['Shin.'+side].rotation_euler.x=bend
     rig.pose.bones['Foot.'+side].rotation_euler.x=-(hip+bend)
     rig.pose.bones['Arm.'+side].rotation_euler.x=-stride*(.68 if running else .38)+(-.28 if name=='Guardian' else 0)
     rig.pose.bones['Forearm.'+side].rotation_euler.x=-.55 if name=='Guardian' else (-.42 if running else -.18)
     rig.pose.bones['Coat.'+side].rotation_euler.x=math.sin(t*math.tau-.65)*.26*sign
    rig.pose.bones['Body'].location.x=wave*.025
    rig.pose.bones['Body'].rotation_euler.x=(.23 if name=='Guardian' else .12) if running else (.16 if name=='Guardian' else .04)
    rig.pose.bones['Body'].rotation_euler.y=wave*(.12 if name=='Guardian' else .065)
    rig.pose.bones['Body'].rotation_euler.z=wave*.09
    rig.pose.bones['Satchel'].rotation_euler.x=math.sin(t*math.tau-.8)*.18
    rig.pose.bones['Head'].rotation_euler.z=-wave*.04
   elif action_name=='Stopping':
    pulse=math.sin(t*math.pi)*(1-t)
    rig.pose.bones['Root'].location.y=-pulse*.10
    rig.pose.bones['Body'].rotation_euler.x=-pulse*.32
    rig.pose.bones['Leg.L'].rotation_euler.x=pulse*.40
    rig.pose.bones['Shin.L'].rotation_euler.x=-pulse*.55
    rig.pose.bones['Arm.L'].rotation_euler.x=-pulse*.38
    rig.pose.bones['Arm.R'].rotation_euler.x=-pulse*.30
    rig.pose.bones['Satchel'].rotation_euler.x=math.sin(t*math.pi*2)*.18
   elif action_name=='Turning':
    pulse=math.sin(t*math.tau)
    rig.pose.bones['Root'].location.y=-pulse*.04
    rig.pose.bones['Body'].rotation_euler.z=pulse*.18
    rig.pose.bones['Head'].rotation_euler.z=pulse*.24
    rig.pose.bones['Leg.L'].rotation_euler.x=pulse*.28
    rig.pose.bones['Shin.L'].rotation_euler.x=-pulse*.5
    rig.pose.bones['Satchel'].rotation_euler.z=-pulse*.20
   elif action_name=='Spellcast_Shoot':
    # Exact frame 6 / .10s release. Back cock -> extension -> recoil -> settle.
    keys=[(0,.3),(.16,1.45),(.32,1.45),(.40,-1.45),(.64,-1.65),(1,0)]
    angle=0
    for (a,av),(b,bv) in zip(keys,keys[1:]):
     if a<=t<=b:angle=av+(bv-av)*(t-a)/(b-a);break
    rig.pose.bones['Arm.R'].rotation_euler.x=angle
    rig.pose.bones['Arm.R'].rotation_euler.z=-math.sin(t*math.pi)*.55
    rig.pose.bones['Forearm.R'].rotation_euler.x=-math.sin(min(t/.4,1)*math.pi)*1.15
    rig.pose.bones['Coat.R'].rotation_euler.x=math.sin(t*math.pi)*.18
   elif action_name=='Spellcasting':
    prepare=min(1,t/.68);release=max(0,min(1,(t-.68)/.16));recover=max(0,(t-.84)/.16)
    rig.pose.bones['Body'].rotation_euler.x=-prepare*.14+release*.26-recover*.12
    for side in ['L','R']:
     rig.pose.bones['Arm.'+side].rotation_euler.x=-prepare*.8-release*.45+recover*1.25
     rig.pose.bones['Forearm.'+side].rotation_euler.x=-prepare*.7+release*.6+recover*.1
    rig.pose.bones['Head'].rotation_euler.x=prepare*.12-release*.17+recover*.05
   elif action_name=='1H_Melee_Attack_Slice_Horizontal':
    pulse=math.sin(t*math.pi)
    rig.pose.bones['Body'].rotation_euler.x=(-.25 if t<.55 else .38)*pulse
    rig.pose.bones['Arm.R'].rotation_euler.x=(-.7 if t>.55 else .6)*pulse
    rig.pose.bones['Arm.L'].rotation_euler.x=-pulse*.45
    rig.pose.bones['Forearm.R'].rotation_euler.x=(-.12 if t>.55 else -.8)*pulse
    rig.pose.bones['Head'].rotation_euler.x=pulse*.12
   elif action_name=='Hit_A':
    rig.pose.bones['Body'].rotation_euler.x=-math.sin(t*math.pi)*.42
    rig.pose.bones['Head'].rotation_euler.x=math.sin(t*math.pi)*.26
   elif action_name=='Death_A':
    collapse=max(0,(t-.18)/.82)
    rig.pose.bones['Body'].rotation_euler.x=math.sin(t*math.pi)*.34
    rig.pose.bones['Root'].rotation_euler.x=collapse*1.45
    rig.pose.bones['Root'].rotation_euler.y=math.sin(t*math.pi)*(.22 if name=='Guardian' else -.22)
    rig.pose.bones['Arm.L'].rotation_euler.z=math.sin(t*math.pi)*.48
    rig.pose.bones['Arm.R'].rotation_euler.z=-math.sin(t*math.pi)*.48
    rig.pose.bones['Shin.L'].rotation_euler.x=-math.sin(t*math.pi)*.5
   else:
    rig.pose.bones['Body'].rotation_euler.x=wave*.025
    rig.pose.bones['Head'].rotation_euler.z=wave*(.09 if name=='Guardian' else .04)
    rig.pose.bones['Arm.L'].rotation_euler.x=wave*.055
    rig.pose.bones['Arm.R'].rotation_euler.x=-wave*.055
    rig.pose.bones['Satchel'].rotation_euler.z=math.sin(t*math.tau-.5)*.035
   for bone in rig.pose.bones:
    bone.keyframe_insert('rotation_euler',frame=f);bone.keyframe_insert('location',frame=f)
 bpy.context.scene.render.fps=60;rig.animation_data.action=None
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
requested=sys.argv[sys.argv.index('--')+2:]
for name in requested or ['Hero','Guardian','Acolyte']:build(name)
