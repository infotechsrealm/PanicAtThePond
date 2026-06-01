import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    content = f.read()

docs = content.split("--- !u!")

game_objects = {} # fileID -> name
animator_id = None
controller_id = None
sync_id = None
network_animator_id = None

for doc in docs:
    lines = doc.strip().split("\n")
    if not lines or not lines[0]:
        continue
    header = lines[0]
    hm = re.match(r"^(\d+)\s+&(\d+)", header)
    if not hm:
        continue
    utype, fid = hm.group(1), hm.group(2)
    
    if utype == "1":
        name = ""
        for line in lines:
            if "m_Name:" in line:
                name = line.split("m_Name:")[1].strip().strip('"').strip("'")
                break
        game_objects[fid] = name

# Find root Animator ID (Animator on GO "FisherMan (2)")
for doc in docs:
    lines = doc.strip().split("\n")
    if not lines or not lines[0]:
        continue
    header = lines[0]
    hm = re.match(r"^95\s+&(\d+)", header) # Animator
    if hm:
        fid = hm.group(1)
        # Check if it belongs to FisherMan (2)
        go_id = ""
        for line in lines:
            if "m_GameObject:" in line:
                m = re.search(r"fileID: (\d+)", line)
                if m: go_id = m.group(1)
        if go_id and game_objects.get(go_id) == "FisherMan (2)":
            animator_id = fid
            print(f"Found root Animator component (ID: {animator_id}) on FisherMan (2)")

# Check FishermanController, FishermanChildAnimatorSync, and NetworkAnimator references
for doc in docs:
    lines = doc.strip().split("\n")
    if not lines or not lines[0]:
        continue
    header = lines[0]
    hm = re.match(r"^114\s+&(\d+)", header) # MonoBehaviour
    if hm:
        fid = hm.group(1)
        is_fisherman_controller = False
        is_sync = False
        is_net_animator = False
        
        for line in lines:
            if "guid: a8ba3c4804b59d9469892d7b3a2208f6" in line:
                is_fisherman_controller = True
            elif "guid: 6bd1afb3aa35b5c4b9d8259e2203bae7" in line:
                is_sync = True
            elif "guid: 7f6f3bf89aa97405989c802ba270f815" in line:
                is_net_animator = True
        
        if is_fisherman_controller:
            controller_id = fid
            anim_ref = None
            for line in lines:
                if "animator:" in line:
                    anim_ref = line.strip()
            print(f"FishermanController (ID: {fid}) animator ref: {anim_ref}")
        elif is_sync:
            sync_id = fid
            root_anim_ref = None
            for line in lines:
                if "rootAnimator:" in line:
                    root_anim_ref = line.strip()
            print(f"FishermanChildAnimatorSync (ID: {fid}) rootAnimator ref: {root_anim_ref}")
        elif is_net_animator:
            network_animator_id = fid
            anim_ref = None
            for line in lines:
                if "animator:" in line:
                    anim_ref = line.strip()
            print(f"NetworkAnimator (ID: {fid}) animator ref: {anim_ref}")
