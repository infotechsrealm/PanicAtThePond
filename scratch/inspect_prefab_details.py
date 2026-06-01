import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    content = f.read()

# Split into documents
docs = content.split("--- !u!")

game_objects = {} # fileID -> name
transforms = {} # fileID -> (goID, localPosition, localScale)
sprite_renderers = {} # fileID -> (goID, sortingOrder)

def parse_vector3(line):
    # Try inline format: {x: 1, y: 2, z: 3}
    m = re.search(r"\{\s*x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^\}]+)\}", line)
    if m:
        return f"{m.group(1).strip()}, {m.group(2).strip()}, {m.group(3).strip()}"
    return None

for doc in docs:
    lines = doc.strip().split("\n")
    if not lines or not lines[0]:
        continue
    
    header = lines[0]
    # format: "1 &333858920731257930" or "4 &12345" etc.
    hm = re.match(r"^(\d+)\s+&(\d+)", header)
    if not hm:
        continue
    
    utype, fid = hm.group(1), hm.group(2)
    
    if utype == "1": # GameObject
        name = ""
        for line in lines:
            if "m_Name:" in line:
                name = line.split("m_Name:")[1].strip()
                # remove quotes if any
                name = name.strip('"').strip("'")
                break
        game_objects[fid] = name
        
    elif utype == "4": # Transform
        go_id = ""
        pos = "0, 0, 0"
        scale = "1, 1, 1"
        
        idx = 0
        while idx < len(lines):
            line = lines[idx]
            if "m_GameObject:" in line:
                m_go = re.search(r"fileID: (\d+)", line)
                if m_go:
                    go_id = m_go.group(1)
            elif "m_LocalPosition:" in line:
                inline = parse_vector3(line)
                if inline:
                    pos = inline
                else:
                    # multi line
                    try:
                        x = lines[idx+1].split("x:")[1].strip()
                        y = lines[idx+2].split("y:")[1].strip()
                        z = lines[idx+3].split("z:")[1].strip()
                        pos = f"{x}, {y}, {z}"
                    except Exception:
                        pass
            elif "m_LocalScale:" in line:
                inline = parse_vector3(line)
                if inline:
                    scale = inline
                else:
                    try:
                        x = lines[idx+1].split("x:")[1].strip()
                        y = lines[idx+2].split("y:")[1].strip()
                        z = lines[idx+3].split("z:")[1].strip()
                        scale = f"{x}, {y}, {z}"
                    except Exception:
                        pass
            idx += 1
        if go_id:
            transforms[go_id] = (pos, scale)
            
    elif utype == "212": # SpriteRenderer
        go_id = ""
        sorting_order = "0"
        for line in lines:
            if "m_GameObject:" in line:
                m_go = re.search(r"fileID: (\d+)", line)
                if m_go:
                    go_id = m_go.group(1)
            elif "m_SortingOrder:" in line:
                sorting_order = line.split("m_SortingOrder:")[1].strip()
        if go_id:
            sprite_renderers[go_id] = sorting_order

print("=== DETAILED CHILD OBJECTS ===")
for go_id in sorted(game_objects.keys()):
    name = game_objects[go_id]
    pos, scale = transforms.get(go_id, ("None", "None"))
    sort = sprite_renderers.get(go_id, "N/A")
    print(f"Name: {name} (GO ID: {go_id})")
    print(f"  Local Position: {pos}")
    print(f"  Local Scale:    {scale}")
    print(f"  Sorting Order:  {sort}")
