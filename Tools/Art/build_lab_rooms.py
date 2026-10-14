"""Original, quiet laboratory architecture. Reuses Room/Gate asset GUIDs.

Scenery remains outside the playable boundary; meshes contain no colliders.
Material groups are joined before export to bound draw calls across three rooms.
"""
import bpy, math, random, sys
from pathlib import Path
random.seed(81)
bpy.ops.wm.read_factory_settings(use_empty=True)
out=Path(sys.argv[sys.argv.index('--')+1])
colors={'Stone':(.46,.52,.48,1),'StoneLight':(.64,.66,.55,1),
 'StoneDark':(.30,.38,.36,1),'Tile':(.42,.53,.53,1),
 'TileLight':(.45,.56,.55,1),'TileDark':(.39,.50,.50,1),
 'Wood':(.06,.26,.27,1),'Gold':(.63,.45,.19,1),'Iron':(.10,.17,.19,1),
 'Cloth':(.13,.34,.37,1),'Glow':(.30,.85,.74,1)}
mats={}
for name,color in colors.items():
 m=bpy.data.materials.new(name);m.diffuse_color=color;mats[name]=m
groups={'Room':[],'Gate':[]}
def box(p,s,mat,bevel=.1,group='Room'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=s
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Soft moulded corners','BEVEL');mod.width=bevel;mod.segments=3
  bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(mats[mat]);groups[group].append(o);return o
def cylinder(p,r,depth,mat,group='Room'):
 bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=depth,location=p)
 o=bpy.context.object;o.data.materials.append(mats[mat]);groups[group].append(o)
 mod=o.modifiers.new('Rounded tank lip','BEVEL');mod.width=.05;mod.segments=2
 bpy.ops.object.modifier_apply(modifier=mod.name)
 for poly in o.data.polygons:poly.use_smooth=True
 return o
def mesh(v,f,mat):
 m=bpy.data.meshes.new('Laboratory inlay');m.from_pydata(v,[],f);m.update()
 o=bpy.data.objects.new('Laboratory inlay',m);bpy.context.collection.objects.link(o)
 o.data.materials.append(mats[mat]);groups['Room'].append(o)
# Larger tiles with a tight, low-contrast palette: walking paths stay quiet.
box((0,0,-.13),(20,20,.24),'StoneDark',0)
for y in range(-5,5):
 for x in range(-5,5):
  box((x*2+1,y*2+1,-.07),(1.987,1.987,.14),random.choice(['Tile']*6+['TileLight','TileDark']),.012)
# Low, rounded wall courses and exterior skirting keep actors visible at the edge.
for side in [-1,1]:
 box((side*10,0,.43),(1,20,.86),'Stone',.19)
 box((side*10,0,.92),(1.15,20,.18),'StoneLight',.08)
 box((side*10,0,.18),(1.18,20,.25),'StoneDark',.09)
 for edge in [-10,10]:
  box((side*6,edge,.43),(8,1,.86),'Stone',.19)
  box((side*6,edge,.92),(8,1.15,.18),'StoneLight',.08)
 # Lab fixtures sit outside collision walls, never on enemy walking paths.
 for y in [-6,2,7]:
  box((side*11.35,y,.48),(1.7,2,.96),'Iron',.20)
  cylinder((side*11.35,y,1.25),.62,1.3,'Cloth')
  cylinder((side*11.35,y,1.27),.64,.64,'Glow')
  cylinder((side*11.35,y,1.94),.68,.18,'StoneLight')
  cylinder((side*11.35,y,2.13),.22,.26,'Gold')
 for y in [-9,0,9]:
  box((side*10,y,1.26),(1.3,1.3,2.52),'Stone',.22)
  box((side*10,y,2.5),(1.5,1.5,.30),'StoneLight',.12)
  box((side*9.3,y,1.62),(.07,.40,.36),'Glow',.04)
# Rounded doorway shoulders, arch and shutters shared by all three rooms.
for x in [-2.55,2.55]:
 box((x,10,1.35),(1,1.2,2.7),'Stone',.18)
 box((x,10,.20),(1.25,1.4,.4),'StoneDark',.12)
 box((x,9.36,1.4),(.17,.08,.68),'Glow',.07)
for x in [-2.5,2.5]:
 box((x,12,.42),(1,4,.84),'Stone',.16)
 box((x,12,.90),(1.12,4,.16),'StoneLight',.07)
box((0,12,-.07),(4,4,.14),'Tile',.02)
for i in range(15):
 a=i*math.pi/15;b=(i+1)*math.pi/15-.012
 v=[(math.cos(t)*r,y,2+math.sin(t)*r*.48) for y in [9.5,10.5] for r in [2,2.65] for t in [a,b]]
 mesh(v,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],'StoneLight')
box((0,9.55,3.1),(.95,.15,.7),'Iron',.12)
cylinder((0,9.42,3.16),.18,.30,'Glow').rotation_euler.x=math.pi/2
box((0,9.38,3.39),(.13,.07,.19),'Glow',.03)
for x in [-1,1]:box((x,9.49,3.1),(.27,.09,.14),'Gold',.04)
for i in range(8):box((-1.72+i*.49,0,0),(.47,.28,2.72),'Wood',.08,'Gate')
for z in [-.95,.92]:box((0,-.19,z),(3.86,.10,.14),'Iron',.04,'Gate')
box((0,-.22,.1),(.68,.12,.8),'Iron',.12,'Gate')
box((0,-.3,.1),(.33,.08,.4),'Glow',.09,'Gate')
# Flat navigation chevrons differ from warm enemy warnings; no emission.
for y in [5,6.3,7.6]:
 mesh([(-.36,y,.014),(0,y+.38,.014),(.36,y,.014),(.36,y-.10,.014),(0,y+.25,.014),(-.36,y-.10,.014)],[(0,1,4,5),(1,2,3,4)],'StoneLight')
for group,objects in groups.items():
 joined=[]
 batches={mat:[o for o in objects if o.data.materials[0].name==mat] for mat in mats}
 for mat,batch in batches.items():
  if not batch:continue
  bpy.ops.object.select_all(action='DESELECT')
  for o in batch:o.select_set(True)
  bpy.context.view_layer.objects.active=batch[0];bpy.ops.object.join()
  o=bpy.context.object;o.name=mat;joined.append(o)
  bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 bpy.ops.object.select_all(action='DESELECT')
 for o in joined:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(group+'.fbx')),use_selection=True,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
 print(group,'material groups:',len(joined))
