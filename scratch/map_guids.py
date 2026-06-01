import os
import re

guids = [
    "a8ba3c4804b59d9469892d7b3a2208f6",
    "aa584fbee541324448dd18d8409c7a41",
    "627855c7f81362d41938ffe0b1475957",
    "9b8c4a61274f60b4ea5fb4299cfdbf14",
    "62899f850307741f2a39c98a8b639597",
    "9b91ecbcc199f4492b9a91e820070131",
    "a553cb17010b2403e8523b558bffbc14",
    "cc0bb1a7437ee3a458d010895f26d7cd",
    "7f6f3bf89aa97405989c802ba270f815",
    "6bd1afb3aa35b5c4b9d8259e2203bae7"
]

search_dirs = [
    r"E:\PanicAtThePond\Assets\Scripts",
    r"E:\PanicAtThePond\Assets\Mirror\Components",
    r"E:\PanicAtThePond\Assets\Photon\PhotonUnityNetworking\Code"
]
guid_to_path = {}

for sdir in search_dirs:
    if not os.path.exists(sdir):
        continue
    for root, dirs, files in os.walk(sdir):
        for file in files:
            if file.endswith(".meta"):
                meta_path = os.path.join(root, file)
                with open(meta_path, "r", encoding="utf-8", errors="ignore") as f:
                    content = f.read()
                    m = re.search(r"guid:\s*([a-f0-9]{32})", content)
                    if m:
                        g = m.group(1)
                        if g in guids:
                            asset_path = meta_path[:-5]
                            guid_to_path[g] = asset_path

print("GUID MAPPINGS:")
for g in guids:
    print(f"  {g} -> {guid_to_path.get(g, 'Not Found')}")
