import os
import glob
from PIL import Image

def mse_bottom_half(img1, img2):
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
            if y < 24: continue
            if p1[3] > 10 and p2[3] > 10:
                diff += abs(p1[0] - p2[0]) + abs(p1[1] - p2[1]) + abs(p1[2] - p2[2])
                count += 1
    return diff / max(1, count)

# CHANGED ORDER: Body is now before Boat
parts = [
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
    r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png",
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
new_ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Default animation ui (Red Hair)"

png_files = glob.glob(os.path.join(ui_dir, "**/*.png"), recursive=True)

print("Regenerating sprites with correct layering (Body behind Boat)...")
for path in png_files:
    ui_img = Image.open(path).convert("RGBA")
    best_match = -1
    best_diff = float('inf')
    
    # We can optimize this by finding the matching frame using the Boat layer alone 
    # but since it's just 96 files and takes 5 seconds, it's fine to just match again.
    
    # Wait! the matching was done against the old frame which had Boat before Body!
    # Will mse_bottom_half still work? Yes, because mse_bottom_half ignores the top 24 pixels (head),
    # and the boat hull itself (which is at the bottom half) is mostly the same. 
    # Actually, the matching is matching `frame` (new composite) with `ui_img` (yellow hat original frame).
    # Since we swapped Body and Boat in `frame`, the match might be slightly different.
    # To be absolutely 100% safe, we can match against the `sheet_img` built with the *old* layering!
    pass

# SAFER APPROACH: Use old layering for matching, but save with new layering
old_parts = [
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
    r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png"
]

match_sheet = Image.new("RGBA", (256, 1536), (0,0,0,0))
for part in old_parts:
    try:
        match_sheet.alpha_composite(Image.open(part).convert("RGBA"))
    except:
        pass

match_frames = []
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        match_frames.append(match_sheet.crop(box))


for path in png_files:
    ui_img = Image.open(path).convert("RGBA")
    best_match = -1
    best_diff = float('inf')
    
    for i, m_frame in enumerate(match_frames):
        d = mse_bottom_half(m_frame, ui_img)
        if d < best_diff:
            best_diff = d
            best_match = i
            
    rel_path = os.path.relpath(path, ui_dir)
    new_path = os.path.join(new_ui_dir, rel_path)
    
    # Save corresponding frame from the NEW layered frames
    red_hair_frame = frames[best_match]
    red_hair_frame.save(new_path)

print("Successfully replaced all 96 sprites with correct layering!")
