import re

controller_path = r"E:\PanicAtThePond\Assets\Resources\Fisherman created\Cheast anim\CheastanimAnimator.controller"

with open(controller_path, "r", encoding="utf-8") as f:
    content = f.read()

# Let's find the AnimatorController block and search within it
# An AnimatorController starts with:
# --- !u!91 &...
# AnimatorController:
#   m_ObjectHideFlags: ...
#   ...
#   m_AnimatorParameters:
#   - m_Name: idel_l
#     m_Type: 4
#     ...
#   m_AnimatorLayers:

match = re.search(r"AnimatorController:\s*\n(.*?)\n\s*m_AnimatorLayers:", content, re.DOTALL)
if match:
    parameters_block = match.group(1)
    # Find all m_Name inside the parameters block
    param_names = re.findall(r"m_Name:\s*([a-zA-Z0-9_-]+)", parameters_block)
    print("Parameters inside AnimatorController:")
    for name in param_names:
        print(f"  - {name}")
else:
    print("Could not find AnimatorController parameters block.")
