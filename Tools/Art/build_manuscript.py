"""Original laboratory paper, drawn independently of the paid reference kit."""
from PIL import Image, ImageDraw, ImageFilter
from pathlib import Path
import math, random, sys

out = Path(sys.argv[1])
rng = random.Random(1847)
size = 512
paper = Image.new('RGBA', (size, size))
pixels = paper.load()
for y in range(size):
    for x in range(size):
        edge = min(x, y, size-1-x, size-1-y)
        wear = max(0, 30-edge)/30
        grain = rng.uniform(-1.7, 1.7) + math.sin(x*.035)*math.sin(y*.024)*1.2
        pixels[x,y] = tuple(int(c + grain - wear*d) for c,d in [(244,36),(235,43),(215,52)]) + (255,)
# Uneven silhouette stays within the 36-pixel nine-slice border.
mask = Image.new('L', paper.size, 0)
d = ImageDraw.Draw(mask)
points = []
for side in range(4):
    for t in range(0, 501, 5):
        wobble = 5 + rng.randrange(0,4) + int(1.8*math.sin(t*.11))
        points.append([(t+6,wobble),(511-wobble,t+6),(505-t,511-wobble),(wobble,505-t)][side])
d.polygon(points, fill=255)
paper.putalpha(mask.filter(ImageFilter.GaussianBlur(.45)))
d = ImageDraw.Draw(paper)
# Fine laboratory ink ticks at the corners, leaving the reading area quiet.
for x,y,dx,dy in [(26,26,1,1),(486,26,-1,1),(26,486,1,-1),(486,486,-1,-1)]:
    d.line([(x,y+dy*28),(x,y),(x+dx*28,y)], fill=(115,91,70,95), width=1)
    d.ellipse((x-2,y-2,x+2,y+2), fill=(115,91,70,110))
paper.save(out/'Manuscript.png')
