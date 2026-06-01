import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    content = f.read()

docs = content.split("--- !u!")

game_objects = {} # fileID -> name
components = {} # fileID -> (type, goID, info)

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
        
    elif utype == "95":
        go_id = ""
        controller = ""
        for line in lines:
            if "m_GameObject:" in line:
                m_go = re.search(r"fileID: (\d+)", line)
                if m_go: go_id = m_go.group(1)
            elif "m_Controller:" in line:
                controller = line.split("m_Controller:")[1].strip()
        components[fid] = ("Animator", go_id, controller)

print("Current Animators on Prefab:")
for fid, (ctype, go_id, info) in components.items():
    go_name = game_objects.get(go_id, "Unknown")
    print(f"  {ctype} on {go_name} (GO ID: {go_id}) -> {info}")
