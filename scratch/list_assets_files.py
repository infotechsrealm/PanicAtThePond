import os

base_dir = r"E:\PanicAtThePond\Assets\Resources\Fisherman created"
subdirs = ["Boat", "Cheast anim", "Road", "hand anim", "oar"]

for s in subdirs:
    path = os.path.join(base_dir, s)
    if os.path.exists(path):
        print(f"=== {s} ===")
        files = os.listdir(path)
        controllers = [f for f in files if f.endswith(".controller")]
        anims = [f for f in files if f.endswith(".anim")]
        print(f"  Controllers: {controllers}")
        print(f"  Anims count: {len(anims)}")
    else:
        print(f"=== {s} (DOES NOT EXIST) ===")
