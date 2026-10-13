# Original hand-directed procedural stone grain and moss; deterministic, no external textures.
from PIL import Image,ImageDraw,ImageFilter
from pathlib import Path
import random,sys,math
random.seed(9);out=Path(sys.argv[1]);im=Image.new('RGB',(512,512));px=im.load()
for y in range(512):
 for x in range(512):
  grain=random.uniform(-7,7)+3*math.sin(x*.025)*math.sin(y*.05)
  edge=min(x,y,511-x,511-y);v=1-max(0,8-edge)*.016
  px[x,y]=tuple(int(max(0,min(255,(c+grain)*v))) for c in [219,222,202])
d=ImageDraw.Draw(im)
for i in range(34):
 x=random.randrange(512);y=random.randrange(512);d.line([(x,y),(x+random.randrange(3,23),y+random.randrange(-3,3))],fill=(180,191,173),width=1)
im.save(out/'StoneGrain.png')
# Softly rimmed HUD panels and touch surfaces, authored from vector-like paths.
ui=Image.new('RGBA',(96,96),(0,0,0,0));d=ImageDraw.Draw(ui);d.rounded_rectangle((2,2,93,93),radius=18,fill=(17,36,40,238),outline=(235,222,180,220),width=2);d.rounded_rectangle((5,5,90,90),radius=15,outline=(65,101,92,150),width=1);ui.save(out/'Panel.png')
t=Image.new('RGBA',(128,128),(0,0,0,0));d=ImageDraw.Draw(t);d.ellipse((3,3,124,124),fill=(14,31,37,132),outline=(235,222,180,195),width=3);d.ellipse((9,9,118,118),outline=(118,146,125,85),width=1);t.save(out/'TouchRing.png')
# Luminous star sprite for bounded pooled VFX.
s=Image.new('RGBA',(64,64),(0,0,0,0));d=ImageDraw.Draw(s);d.polygon([(32,0),(38,25),(64,32),(38,39),(32,64),(25,39),(0,32),(25,25)],fill=(255,244,188,255));s=s.filter(ImageFilter.GaussianBlur(1));s.save(out/'Spark.png')
