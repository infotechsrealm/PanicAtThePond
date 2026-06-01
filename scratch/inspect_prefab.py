import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

current_id = None
current_type = None
game_objects = {} # fileID -> name
components = {} # fileID -> (type, goID, additional_info)

i = 0
while i < len(lines):
    line = lines[i]
    m = re.match(r"^--- !u!(\d+) &(\d+)", line)
    if m:
        utype = m.group(1)
        fid = m.group(2)
        current_id = fid
        if utype == "1":
            current_type = "GameObject"
            name = ""
            # read until next header or end
            j = i + 1
            while j < len(lines) and not lines[j].startswith("---"):
                if "m_Name:" in lines[j]:
                    name = lines[j].split("m_Name:")[1].strip()
                    break
                j += 1
            game_objects[fid] = name
        elif utype == "95":
            current_type = "Animator"
            go_id = ""
            controller = ""
            j = i + 1
            while j < len(lines) and not lines[j].startswith("---"):
                if "m_GameObject:" in lines[j]:
                    m_go = re.search(r"fileID: (\d+)", lines[j])
                    if m_go:
                        go_id = m_go.group(1)
                if "m_Controller:" in lines[j]:
                    controller = lines[j].split("m_Controller:")[1].strip()
                j += 1
            components[fid] = ("Animator", go_id, f"Controller: {controller}")
        elif utype == "114":
            current_type = "MonoBehaviour"
            go_id = ""
            script_name = ""
            j = i + 1
            while j < len(lines) and not lines[j].startswith("---"):
                if "m_GameObject:" in lines[j]:
                    m_go = re.search(r"fileID: (\d+)", lines[j])
                    if m_go:
                        go_id = m_go.group(1)
                if "m_Script:" in lines[j]:
                    script_name = lines[j].split("m_Script:")[1].strip()
                j += 1
            components[fid] = ("MonoBehaviour", go_id, f"Script: {script_name}")
    i += 1

print("=== GAMEOBJECTS ===")
for fid, name in game_objects.items():
    print(f"GameObject ID {fid}: {name}")

print("\n=== ANIMATORS & MONOBEHAVIOURS ===")
for fid, (ctype, go_id, info) in components.items():
    go_name = game_objects.get(go_id, "Unknown")
    print(f"Component {ctype} (ID {fid}) on {go_name} (GO ID {go_id}): {info}")
