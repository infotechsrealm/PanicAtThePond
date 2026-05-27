import os
import glob
from PIL import Image

def mse_bottom_half(img1, img2):
    # Calculate MSE on RGB channels, but only for the bottom half (Y >= 30)
    # where the boat, arms, oars, and body are. This ignores the head/hat!
    data1 = img1.getdata()
    data2 = img2.getdata()
    diff = 0
    count = 0
    
    width, height = img1.size
    
    for y in range(height):
        for x in range(width):
            idx = y * width + x
            p1 = data1[idx]
            p2 = data2[idx]
            
            # Ignore the top part (head area)
            if y < 24:
                continue
                
            # Ignore transparent pixels
            if p1[3] > 10 and p2[3] > 10:
                diff += abs(p1[0] - p2[0]) + abs(p1[1] - p2[1]) + abs(p1[2] - p2[2])
                count += 1
                
    return diff / max(1, count)

# Load the 5 part sheets and composite them into a "base" sheet (without head)
parts = [
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png"
]

sheet_img = Image.new("RGBA", (256, 1536), (0,0,0,0))
for part in parts:
    try:
        img = Image.open(part).convert("RGBA")
        sheet_img.alpha_composite(img)
    except:
        pass

# Extract the 96 frames
frames = []
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        frames.append(sheet_img.crop(box))

ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Used animation ui"

png_files = glob.glob(os.path.join(ui_dir, "**/*.png"), recursive=True)

mapping = {}

for path in png_files:
    ui_img = Image.open(path).convert("RGBA")
    best_match = -1
    best_diff = float('inf')
    
    for i, frame in enumerate(frames):
        d = mse_bottom_half(frame, ui_img)
        if d < best_diff:
            best_diff = d
            best_match = i
            
    print(f"{os.path.basename(path)} -> Frame {best_match} (Diff: {best_diff})")
    if best_match not in mapping:
        mapping[best_match] = []
    mapping[best_match].append(os.path.basename(path))

print("Unique frames matched:", len(mapping))
