"""Original first-room laboratory. Blender geometry, no external assets."""
import bpy, sys
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
out=Path(sys.argv[sys.argv.index('--')+1])
palette={'LabFloor':(.16,.24,.31,1),'LabFloorLight':(.17,.25,.32,1),'LabFloorDark':(.12,.19,.25,1),'LabWall':(.72,.79,.83,1),'LabTrim':(.035,.14,.20,1),'LabMetal':(.11,.21,.27,1),'LabBrass':(.53,.36,.16,1),'LabAccent':(.23,.73,.66,1),'LabGlass':(.21,.49,.55,1)}
mats={}
for k,c in palette.items():
 m=bpy.data.materials.new(k);m.diffuse_color=c;mats[k]=m
def box(p,s,mat,bevel=.08):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=s
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Soft machined corners','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(mats[mat])
def tank(p):
 x,y=p
 for z,r,h,mat in [(.20,.7,.4,'LabMetal'),(1.25,.54,1.7,'LabGlass'),(2.2,.7,.24,'LabWall')]:
  bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=h,location=(x,y,z));bpy.context.object.data.materials.append(mats[mat])
 box((x,y-.55,1.3),(.10,.05,1.25),'LabAccent',.025)
box((0,0,-.15),(20,20,.27),'LabFloorDark',0)
for x in range(8):
 for y in range(8):box(((x-3.5)*2.5,(y-3.5)*2.5,-.04),(2.48,2.48,.16),'LabFloorLight' if (x+y)%5==0 else 'LabFloor',.015)
for side in [-1,1]:
 box((side*9.8,0,.30),(.8,20,.60),'LabTrim')
 for y in [-8,-4,0,4,8]:
  box((side*10,y,1.03),(1,3.90,1.30),'LabWall',.18)
  box((side*9.47,y,.65),(.05,3.4,.11),'LabMetal',.02)
  box((side*10,y-1.95,1.18),(1.18,.18,1.95),'LabTrim',.07)
  box((side*9.38,y-1.95,1.5),(.04,.09,.62),'LabAccent',.025)
 for edge in [-10,10]:
  for x in [4.25,7.8]:
   box((side*x,edge,.35),(3.45,1,.7),'LabTrim')
   box((side*x,edge,1.1),(3.45,1,1.0),'LabWall',.18)
 box((side*2.5,10,1.45),(.65,1,2.9),'LabWall',.24)
 box((side*2.5,9.46,1.5),(.15,.06,1.5),'LabAccent',.05)
 # Exterior workstations form groups, beyond the collision boundary.
 for y in [-6,3,7]:
  x=side*11.2
  box((x,y,.55),(1.5,2.5,1.1),'LabTrim',.17)
  box((x,y,1.17),(1.8,2.7,.22),'LabWall',.09)
  box((x,y+.6,1.63),(.24,.7,.65),'LabMetal',.08)
  box((x-side*.14,y+.6,1.65),(.04,.5,.35),'LabAccent',.03)
  for dy in [-.7,-.2]:
   box((x,y+dy,1.48),(.28,.28,.45),'LabGlass',.07)
   box((x,y+dy,1.75),(.14,.14,.12),'LabBrass',.03)
 tank((side*11.3,-1.6))
box((0,10,3.0),(5.7,1,.65),'LabTrim',.28)
box((0,9.45,3.02),(1.1,.10,.43),'LabWall',.12)
box((0,9.37,3.01),(.3,.06,.25),'LabAccent',.07)
box((0,-10,1.0),(4.2,.6,2),'LabMetal',.22)
for x in [-1,0,1]:box((x,-9.66,1.1),(.7,.06,.06),'LabAccent',.025)
for mat in mats:
 objs=[o for o in bpy.data.objects if o.type=='MESH' and o.data.materials[0]==mats[mat]]
 if not objs:continue
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();bpy.context.object.name=mat
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'LaboratoryRoom.fbx'),use_selection=True,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
