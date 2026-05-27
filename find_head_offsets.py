import sys
from PIL import Image

sheet_path = r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png"
try:
    img = Image.open(sheet_path).convert("RGBA")
except Exception as e:
    print("Error:", e)
    sys.exit(1)

frames = []
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        frame = img.crop(box)
        
        # find bounding box of non-transparent pixels
        data = frame.getdata()
        min_x, min_y, max_x, max_y = 64, 64, -1, -1
        for fy in range(64):
            for fx in range(64):
                if data[fy * 64 + fx][3] > 10:
                    if fx < min_x: min_x = fx
                    if fx > max_x: max_x = fx
                    if fy < min_y: min_y = fy
                    if fy > max_y: max_y = fy
        
        if max_x >= 0:
            cx = (min_x + max_x) // 2
            cy = (min_y + max_y) // 2
            frames.append((cx, cy))
        else:
            frames.append(None)

print(f"Found {len([f for f in frames if f is not None])} frames with head out of 96")
print("First 10 offsets:", frames[:10])
