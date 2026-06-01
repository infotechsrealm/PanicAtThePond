import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

i = 0
found_controller = False
while i < len(lines):
    if "--- !u!114 &" in lines[i]:
        # Read the next lines to see if it's FishermanController (guid: a8ba3c4804b59d9469892d7b3a2208f6)
        j = i + 1
        block = []
        is_fisherman = False
        while j < len(lines) and not lines[j].startswith("---"):
            block.append(lines[j])
            if "a8ba3c4804b59d9469892d7b3a2208f6" in lines[j]:
                is_fisherman = True
            j += 1
        if is_fisherman:
            print("Found FishermanController component:")
            for line in block:
                if "animator:" in line or "m_Name" in line:
                    print(line.strip())
                # Also look at any lines around animator
                if any(x in line for x in ["animator", "chest", "hand", "oar", "road"]):
                    print("  " + line.strip())
            found_controller = True
            break
    i += 1

if not found_controller:
    print("FishermanController component not found in prefab.")
