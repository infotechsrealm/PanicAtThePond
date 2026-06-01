import re

prefab_path = r"E:\PanicAtThePond\Assets\Resources\FisherMan (2).prefab"

with open(prefab_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

i = 0
while i < len(lines):
    if "--- !u!114 &" in lines[i]:
        j = i + 1
        block = []
        is_target = False
        target_name = ""
        while j < len(lines) and not lines[j].startswith("---"):
            block.append(lines[j])
            if "9b8c4a61274f60b4ea5fb4299cfdbf14" in lines[j]: # PhotonAnimatorView
                is_target = True
                target_name = "PhotonAnimatorView"
            elif "7f6f3bf89aa97405989c802ba270f815" in lines[j]: # NetworkAnimator
                is_target = True
                target_name = "NetworkAnimator"
            j += 1
        if is_target:
            print(f"=== {target_name} ===")
            for line in block:
                if any(x in line for x in ["animator", "m_Animator", "m_GameObject"]):
                    print("  " + line.strip())
    i += 1
