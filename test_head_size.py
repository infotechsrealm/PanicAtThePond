import sys
from PIL import Image
sheet_path = r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png"
img = Image.open(sheet_path).convert("RGBA")
for x in range(4):
    frame = img.crop((x * 64, 0, (x + 1) * 64, 64))
    data = frame.getdata()
    min_x, min_y, max_x, max_y = 64, 64, -1, -1
    for fy in range(64):
        for fx in range(64):
            if data[fy * 64 + fx][3] > 10:
                if fx < min_x: min_x = fx
                if fx > max_x: max_x = fx
                if fy < min_y: min_y = fy
                if fy > max_y: max_y = fy
    print(f"Frame {x} head size: {max_x - min_x}x{max_y - min_y}")
