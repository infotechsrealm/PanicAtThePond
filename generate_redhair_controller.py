import os
import glob
import uuid

old_anim_dir = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\UsedAnimations"
new_anim_dir = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\DefaultAnimations (Red Hair)"

anim_files = glob.glob(os.path.join(old_anim_dir, "*.anim"))

meta_template = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

anim_guid_map = {}

print("Generating meta files for animations and building map...")
for old_path in anim_files:
    basename = os.path.basename(old_path)
    new_path = os.path.join(new_anim_dir, basename)
    
    # get old guid
    old_meta_path = old_path + ".meta"
    old_guid = None
    with open(old_meta_path, 'r') as f:
        for line in f:
            if line.startswith("guid:"):
                old_guid = line.split("guid:")[1].strip()
                break
                
    new_guid = uuid.uuid4().hex
    
    if old_guid:
        anim_guid_map[old_guid] = new_guid
        
    # write new meta
    new_meta_path = new_path + ".meta"
    with open(new_meta_path, 'w') as f:
        f.write(meta_template.format(guid=new_guid))

print(f"Built map for {len(anim_guid_map)} animations.")

# Now duplicate and patch the Animator Controller
old_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan.controller"
new_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan (Red Hair).controller"

with open(old_controller, 'r') as f:
    content = f.read()

for old_g, new_g in anim_guid_map.items():
    content = content.replace(old_g, new_g)
    
with open(new_controller, 'w') as f:
    f.write(content)

print("Duplicated and updated Animator Controller!")
