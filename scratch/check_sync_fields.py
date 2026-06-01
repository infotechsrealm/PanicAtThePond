import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

i = 0
found = False
while i < len(lines):
    if "--- !u!114 &" in lines[i]:
        j = i + 1
        block = []
        is_sync = False
        while j < len(lines) and not lines[j].startswith("---"):
            block.append(lines[j])
            if "6bd1afb3aa35b5c4b9d8259e2203bae7" in lines[j]:
                is_sync = True
            j += 1
        if is_sync:
            print("Found FishermanChildAnimatorSync component:")
            for line in block:
                print("  " + line.strip())
            found = True
            break
    i += 1

if not found:
    print("FishermanChildAnimatorSync component not found in prefab.")
