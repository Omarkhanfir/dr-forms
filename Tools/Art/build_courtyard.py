# Original courtyard geometry, authored deterministically in Blender. No third-party geometry.
import bpy,math,random,sys
from pathlib import Path
from mathutils import Vector
random.seed(81);bpy.ops.wm.read_factory_settings(use_empty=True)
out=Path(sys.argv[sys.argv.index('--')+1]);out.mkdir(parents=True,exist_ok=True)
colors={'Stone':(.46,.52,.48,1),'StoneLight':(.64,.66,.55,1),'StoneDark':(.30,.38,.36,1),'Tile':(.48,.55,.45,1),'TileLight':(.53,.58,.48,1),'TileDark':(.44,.51,.42,1),'Wood':(.06,.26,.27,1),'Gold':(.63,.45,.19,1),'Iron':(.10,.17,.19,1),'Leaf':(.15,.32,.19,1),'LeafLight':(.31,.44,.20,1),'Trunk':(.20,.17,.12,1),'Cloth':(.13,.34,.37,1),'Glow':(.30,.85,.74,1),'Flame':(1,.60,.12,1)}
mats={}
for name,color in colors.items():
 m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True;m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=color;mats[name]=m
objects=[];gate=[]
def box(name,p,s,mat,bevel=.07,group=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Hand softened edges','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(mats[mat]);(objects if group is None else group).append(o);return o
# Blender Z up; scene coordinates are X, forward Y, height Z.
for y in range(-6,6):
 for x in range(-6,6):
  mat=random.choice(['Tile','Tile','TileLight','TileDark']);box('Courtyard paving',((x+.5)*1.65,(y+.5)*1.65,-.09),(1.63,1.63,.20),mat,.012)
box('Dark grout underlay',(0,0,-.12),(20,20,.247),'StoneDark',0)
# Thin engraved rosette kept flat for uninterrupted combat movement.
def mesh(name,verts,faces,mat,group=None):
 m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.data.materials.append(mats[mat]);(objects if group is None else group).append(o);return o
def ring(name,r1,r2,z,mat,cx=0,cy=0):
 v=[];f=[]
 for i in range(65):
  a=i*math.pi/32;v.extend([(cx+math.cos(a)*r1,cy+math.sin(a)*r1,z),(cx+math.cos(a)*r2,cy+math.sin(a)*r2,z)])
 for i in range(64):f.append((i*2,i*2+1,i*2+3,i*2+2))
 return mesh(name,v,f,mat)
ring('Weathered garden medallion',2.3,2.4,.018,'StoneDark');ring('Inner brass inlay',1.85,1.88,.024,'Gold')
for a in range(8):
 angle=a*math.pi/4
 o=box('Inlaid compass spoke',(math.cos(angle)*2.12,math.sin(angle)*2.12,.022),(.12,.35,.025),'StoneLight',.025);o.rotation_euler.z=angle
for side in [-1,1]:
 for y in range(-5,5):
  for course in range(3):box('Coursed side wall',(side*10,y*2+1,course*.42+.21),(1.1,1.95,.42),'Stone' if course!=2 else 'StoneLight')
 for edge in [-10,10]:
  for x in [3,5,7,9]:
   for course in range(3):box('Coursed door shoulder',(side*x,edge,course*.42+.21),(1.95,1.1,.42),'Stone' if course!=2 else 'StoneLight')
# Octagonal buttress pillars with a recessed seam and gold-set trim.
for x,y in [(x,y) for x in [-9.8,9.8] for y in [-9.8,0,9.8]]+[(x,10) for x in [-2.45,2.45]]:
 box('Column plinth',(x,y,.25),(1.5,1.5,.5),'StoneDark',.12)
 box('Column base',(x,y,.55),(1.26,1.26,.18),'StoneLight',.06)
 box('Column shaft',(x,y,1.58),(.95,.95,1.95),'Stone',.12)
 box('Column capital',(x,y,2.6),(1.36,1.36,.3),'StoneLight',.08)
 box('Column cap',(x,y,2.83),(1.45,1.45,.18),'StoneDark',.07)
 if abs(x)<3:
  box('Gate rune inset',(x,y-.55,1.6),(.28,.045,.6),'Iron',.02)
  box('Gate glowing rune',(x,y-.58,1.6),(.055,.025,.40),'Glow',.01)
# True wedge arch, not a box header.
for i in range(13):
 a=i*math.pi/13;b=(i+1)*math.pi/13-.025
 v=[(math.cos(t)*r,yy,1.7+math.sin(t)*r*.55) for yy in [9.5,10.5] for r in [2,2.65] for t in [a,b]]
 mesh('Arch voussoir',v,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],'StoneLight' if i%2 else 'Stone')
# Door model centred at collider origin, moved by existing door animation.
for i in range(8):box('Laboratory shutter',(-1.72+i*.49,0,0),(.46,.28,2.72),'Wood',.055,gate)
for z in [-.95,.92]:box('Iron gate binding',(0,-.19,z),(3.86,.10,.14),'Iron',.035,gate)
for x in [-1.7,1.7]:
 for z in [-.95,.92]:box('Brass rivet',(x,-.27,z),(.12,.08,.12),'Gold',.035,gate)
box('Gate sigil',(0,-.22,.1),(.68,.12,.8),'Iron',.09,gate)
box('Chemical lock',(0,-.3,.1),(.33,.08,.4),'Glow',.07,gate)
# Peripheral foliage: branching trunks and custom layered leaf lobes outside collision routes.
def branch(start,end,r,mat='Trunk'):
 d=Vector(end)-Vector(start);bpy.ops.mesh.primitive_cone_add(vertices=7,radius1=r,radius2=r*.62,depth=d.length,location=(Vector(start)+Vector(end))/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(mats[mat]);objects.append(o)
def canopy(p,size,mat):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=p);o=bpy.context.object;o.scale=size;o.data.materials.append(mats[mat]);objects.append(o)
for x,y in [(-12,-7),(12,-5),(-12,5),(12,7),(-7,13),(7,13)]:
 branch((x,y,0),(x+.3,y,3.8),.35)
 for k in range(4):
  a=k*math.pi/2;dx=math.cos(a)*1.1;dy=math.sin(a)*1.1
  branch((x,y,2.2),(x+dx,y+dy,4),.15);canopy((x+dx,y+dy,4.5),(1.8,1.5,1.35),'Leaf' if k%2 else 'LeafLight')
 canopy((x,y,5.4),(1.7,1.5,1.2),'Leaf')
# Art-directed low ferns, cracks and ivy at the room perimeter, never tall within combat space.
for x,y in [(-8,-8),(8,-8),(-8,7),(8,7),(-8,0),(8,2)]:
 for k in range(9):
  a=k*math.pi*2/9;length=random.uniform(.55,1.05)
  mesh('Sculpted fern frond',[(x,y,.04),(x+math.cos(a)*length*.6-.1,y+math.sin(a)*length*.6,.30),(x+math.cos(a)*length,y+math.sin(a)*length,.15),(x+math.cos(a)*length*.6+.1,y+math.sin(a)*length*.6,.25)],[(0,1,2),(0,2,3)],'LeafLight' if k%2 else 'Leaf')
 for k in range(3):canopy((x+random.uniform(-.6,.6),y+random.uniform(-.6,.6),.12),(.25,.3,.18),'StoneDark')
# Original laboratory perimeter equipment: bounded, collision-free scenery.
for x in [-8.5,8.5]:
 box('Teal laboratory bench',(x,6.8,.75),(1.4,3,.2),'Iron',.06)
 for y in [5.8,7.6]:
  branch((x,y,0),(x,y,1.8),.45,'Cloth')
  ring('Tank brass rim',.43,.49,1.7,'Gold',x,y)
  branch((x,y,1.8),(x,y,2.05),.17,'Glow')
 for y in [-5,-1,3]:
  branch((x,y,0),(x,y,1.0),.30,'Iron')
  canopy((x,y,1.35),(.42,.42,.50),'Glow')
  branch((x,y,1.7),(x,y,2.0),.15,'Cloth')
  for k in range(3):
   a=k*math.tau/3;canopy((x+math.cos(a)*.23,y+math.sin(a)*.23,1.4),(.14,.14,.3),'LeafLight')
 branch((x,9.3,.3),(x,-8,.3),.12,'Gold')
 branch((x,-8,.3),(x,-8,1.8),.12,'Gold')
 box('Laboratory exit lintel',(0,10,3.15),(4.8,.7,.45),'Iron')
 for x in [-.5,.5]:box('Laboratory exit lamps',(x,9.60,3.15),(.30,.05,.12),'Glow',.02)
# Flask insignia over door, with clear chemical-green centre.
box('Flask sign',(0,9.58,3.15),(.25,.05,.30),'Glow',.04)
# Join static meshes by material for a small, predictable draw-call budget.
def export(group,name):
 bpy.ops.object.select_all(action='DESELECT')
 for mat in mats:
  objs=[o for o in bpy.data.objects if o.type=='MESH' and o.get('export_group')==name and o.data.materials[0].name==mat]
  if not objs:continue
  bpy.ops.object.select_all(action='DESELECT')
  for o in objs:o.select_set(True)
  bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=mat
  bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 # Joined groups retain object identities; use spatial group collection tags rather than position.
 bpy.ops.object.select_all(action='DESELECT')
 for o in bpy.data.objects:
  if o.get('export_group')==name:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
# Assign tags before joins (active object's custom properties survive).
for o in objects:o['export_group']='Room'
for o in gate:o['export_group']='Gate'
export(objects,'Room');export(gate,'Gate')
print('COURTYARD_EXPORTED',len(bpy.data.objects),'mesh groups')
