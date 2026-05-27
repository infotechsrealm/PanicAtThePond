import os
import glob
from PIL import Image

def mse(img1, img2):
    # Calculate MSE on RGB and Alpha channels, but only where img1 (boat) has alpha > 0
    # Actually, let's just do a simple difference of RGBA pixels where img1 is not transparent
    data1 = img1.getdata()
    data2 = img2.getdata()
    diff = 0
    count = 0
    for p1, p2 in zip(data1, data2):
        if p1[3] > 10: # boat has pixel here
            diff += abs(p1[0] - p2[0]) + abs(p1[1] - p2[1]) + abs(p1[2] - p2[2])
            count += 1
    return diff / max(1, count)

sheet_path = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png"
sheet_img = Image.open(sheet_path).convert("RGBA")

# Extract the 96 frames
frames = []
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        frames.append(sheet_img.crop(box))

ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Used animation ui"
mapping = {} # frame_idx -> file_path

png_files = glob.glob(os.path.join(ui_dir, "**/*.png"), recursive=True)

for path in png_files:
    ui_img = Image.open(path).convert("RGBA")
    best_match = -1
    best_diff = float('inf')
    
    for i, frame in enumerate(frames):
        d = mse(frame, ui_img)
        if d < best_diff:
            best_diff = d
            best_match = i
            
    print(f"Matched {os.path.basename(path)} -> Frame {best_match} (Diff: {best_diff})")
