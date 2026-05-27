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

# We use the old order for matching frames
match_parts = [
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
    r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png"
]

match_sheet = Image.new("RGBA", (256, 1536), (0,0,0,0))
for part in match_parts:
    try:
        img = Image.open(part).convert("RGBA")
        match_sheet.paste(img, (0,0), img)
    except:
        pass

match_frames = []
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        match_frames.append(match_sheet.crop(box))


def fix_sprites(head_sheet_path, target_dirs):
    # NEW CORRECT ORDER: Body -> Boat -> Head -> Oars -> Rods -> Arms
    parts = [
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
        head_sheet_path,
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png"
    ]

    sheet_img = Image.new("RGBA", (256, 1984), (0,0,0,0))
    for part in parts:
        try:
            img = Image.open(part).convert("RGBA")
            sheet_img.paste(img, (0,0), img)
        except Exception as e:
            print(f"Error loading {part}: {e}")

    frames = []
    for y in range(24):
        for x in range(4):
            box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
            frames.append(sheet_img.crop(box))
            
    for (ui_dir, new_ui_dir) in target_dirs:
        png_files = glob.glob(os.path.join(ui_dir, "**/*.png"), recursive=True)
        print(f"Processing {len(png_files)} files in {new_ui_dir}...")
        
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
            os.makedirs(os.path.dirname(new_path), exist_ok=True)
            
            frames[best_match].save(new_path)


print("Fixing Red Hair Sprites...")
red_head = r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png"
red_targets = [
    (r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Used animation ui", r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Default animation ui (Red Hair)"),
    (r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft", r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft (Red Hair)")
]
fix_sprites(red_head, red_targets)

print("Fixing Black Hair Sprites...")
black_head = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\FishermansAnimations-Head-BlackHair-Sheet-frame0.png"
black_targets = [
    (r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Used animation ui", r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Default animation ui (Black Hair)"),
    (r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft", r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft (Black Hair)")
]
fix_sprites(black_head, black_targets)

print("All sprites re-rendered with Oars/Rods behind Arms!")
