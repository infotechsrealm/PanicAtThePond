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

# First, build a map of animation GUID to clip name by scanning all meta files under Fisherman created
guid_to_name = {}
for root, dirs, files in os.walk(os.path.join(base_dir, "Assets/Resources/Fisherman created")):
    for file in files:
        if file.endswith(".anim"):
            anim_path = os.path.join(root, file)
            meta_path = anim_path + ".meta"
            if os.path.exists(meta_path):
                with open(meta_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    m = re.search(r"guid:\s*([a-f0-9]{32})", content)
                    if m:
                        guid_to_name[m.group(1)] = file

print(f"Loaded {len(guid_to_name)} animation clip mappings.")

# Now parse each controller and extract states and their assigned motions (fileID/guid)
for name, rel_path in controllers.items():
    full_path = os.path.join(base_dir, rel_path)
    if not os.path.exists(full_path):
        print(f"=== {name} (NOT FOUND) ===")
        continue
    
    with open(full_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Unity AnimatorController YAML format contains AnimatorState blocks:
    # --- !u!1102 &...
    # AnimatorState:
    #   m_ObjectHideFlags: 1
    #   ...
    #   m_Name: Idel Left
    #   m_Motion: {fileID: 7400000, guid: ..., type: 2}
    
    states = re.findall(r"AnimatorState:\s*\n(.*?)\n(?=---|m_AnimatorStates:|$)", content, re.DOTALL)
    print(f"\n=== Controller: {name} ({os.path.basename(rel_path)}) ===")
    
    found_any = False
    for state_block in states:
        state_name = None
        motion_guid = None
        for line in state_block.split("\n"):
            if "m_Name:" in line:
                state_name = line.split("m_Name:")[1].strip()
            elif "m_Motion:" in line:
                m = re.search(r"guid:\s*([a-f0-9]{32})", line)
                if m:
                    motion_guid = m.group(1)
        
        if state_name and motion_guid:
            clip_name = guid_to_name.get(motion_guid, f"Unknown ({motion_guid})")
            print(f"  State: {state_name:<30} -> Clip: {clip_name}")
            found_any = True
            
    if not found_any:
        print("  No state-to-motion mappings found in this controller YAML.")
