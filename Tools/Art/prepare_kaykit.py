# Preserve the creator's FBX bind poses; trim imported clips/accessories in CourtyardArtImport.
import shutil,sys
from pathlib import Path
args=sys.argv[1:]
if '--' in args:args=args[args.index('--')+1:]
source=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
for a,b in [('adventurers-Knight','Hero'),('skeletons-Skeleton_Warrior','Guardian'),('skeletons-Skeleton_Mage','Acolyte'),('adventurers-wand','Wand'),('skeletons-Skeleton_Staff','EnemyStaff'),('skeletons-Skeleton_Blade','EnemyBlade')]:
 shutil.copyfile(source/(a+'.fbx'),out/(b+'.fbx'));print('Staged',b)
