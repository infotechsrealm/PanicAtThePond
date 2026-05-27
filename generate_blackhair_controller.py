import os
import glob
import uuid

old_anim_dir = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\UsedAnimations"
new_anim_dir = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\DefaultAnimations (Black Hair)"
anim_files = glob.glob(os.path.join(new_anim_dir, "*.anim"))

anim_guid_map = {}

for new_path in anim_files:
    basename = os.path.basename(new_path)
    old_path = os.path.join(old_anim_dir, basename)
    
    old_meta_path = old_path + ".meta"
    old_anim_guid = None
    if os.path.exists(old_meta_path):
        with open(old_meta_path, 'r') as f:
            for line in f:
                if line.startswith("guid:"):
                    old_anim_guid = line.split("guid:")[1].strip()
                    break
                    
    new_meta_path = new_path + ".meta"
    new_anim_guid = None
    if os.path.exists(new_meta_path):
        with open(new_meta_path, 'r') as f:
            for line in f:
                if line.startswith("guid:"):
                    new_anim_guid = line.split("guid:")[1].strip()
                    break
                    
    if old_anim_guid and new_anim_guid:
        anim_guid_map[old_anim_guid] = new_anim_guid

# 4. Duplicate and Patch Animator Controller
old_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan Yellow Hat.controller"
new_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan (Black Hair).controller"

print("Generating Black Hair Animator Controller...")
with open(old_controller, 'r') as f:
    content = f.read()

for old_g, new_g in anim_guid_map.items():
    content = content.replace(old_g, new_g)
    
with open(new_controller, 'w') as f:
    f.write(content)

print(f"Mapped {len(anim_guid_map)} animation GUIDs.")
print("ALL BLACK HAIR ASSETS GENERATED SUCCESSFULLY!")
