import os
import re

controllers = {
    "boat": r"Assets/Resources/Fisherman created/Boat/BotAnimator.controller",
    "chest": r"Assets/Resources/Fisherman created/Cheast anim/Cheast Animator.controller",
    "head": r"Assets/Resources/Fisherman created/Face/Face.controller",
    "hand": r"Assets/Resources/Fisherman created/hand anim/Hand Aniamator.controller",
    "oar": r"Assets/Resources/Fisherman created/oar/Oar.controller",
    "road": r"Assets/Resources/Fisherman created/Road/Road.controller"
}

base_dir = r"E:\PanicAtThePond"

print("GUIDs of the manual controllers:")
for name, rel_path in controllers.items():
    meta_path = os.path.join(base_dir, rel_path + ".meta")
    if os.path.exists(meta_path):
        with open(meta_path, 'r', encoding='utf-8') as f:
            content = f.read()
            m = re.search(r"guid:\s*([a-f0-9]{32})", content)
            if m:
                print(f"  {name} -> {m.group(1)}")
            else:
                print(f"  {name} -> GUID NOT FOUND in meta")
    else:
        print(f"  {name} -> META FILE NOT FOUND at {meta_path}")
