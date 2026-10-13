# CC0 Knight atlas derivative: red cloth/plume to azure, keeping skin/metal/UVs intact.
from PIL import Image
import colorsys,sys
im=Image.open(sys.argv[1]).convert('RGBA');pixels=im.load()
for y in range(im.height):
 for x in range(im.width):
  r,g,b,a=pixels[x,y];h,s,v=colorsys.rgb_to_hsv(r/255,g/255,b/255)
  if (h>.95 or h<.035) and s>.45 and v>.15:
   rgb=colorsys.hsv_to_rgb(.55,.65,min(.82,v*.7+.12));pixels[x,y]=tuple(round(c*255) for c in rgb)+(a,)
im.save(sys.argv[2])
