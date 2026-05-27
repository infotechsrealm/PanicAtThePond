import os
import glob
import uuid
import sys
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
            if y < 32: continue # ignore top half (hats/heads)
            if p1[3] > 10 and p2[3] > 10:
                diff += abs(p1[0] - p2[0]) + abs(p1[1] - p2[1]) + abs(p1[2] - p2[2])
                count += 1
    return diff / max(1, count)

def get_bounding_box(img):
    data = img.getdata()
    width, height = img.size
    min_x, min_y, max_x, max_y = width, height, -1, -1
    for y in range(height):
        for x in range(width):
            if data[y * width + x][3] > 10:
                if x < min_x: min_x = x
                if x > max_x: max_x = x
                if y < min_y: min_y = y
                if y > max_y: max_y = y
    if max_x >= 0:
        return min_x, min_y, max_x, max_y
    return None

meta_template = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 9
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 25
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

anim_meta_template = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def generate_hat(hat_path, hat_name):
    print(f"\n--- Processing Hat: {hat_name} ---")
    
    # Custom offsets for specific hats
    overlap_y = 6
    if "headphones" in hat_name.lower():
        overlap_y = -4 # Move down to sit on ears
    elif "frog" in hat_name.lower():
        overlap_y = 12
    elif "bucket" in hat_name.lower():
        overlap_y = 10
        
    try:
        hat_img = Image.open(hat_path).convert("RGBA")
        # Resize hat to a standard width (e.g. 26 pixels) to match 21x21 head
        target_width = 26
        wpercent = (target_width / float(hat_img.size[0]))
        hsize = int((float(hat_img.size[1]) * float(wpercent)))
        hat_img = hat_img.resize((target_width, hsize), Image.Resampling.LANCZOS)
    except Exception as e:
        print(f"Skipping {hat_name}: {e}")
        return
        
    hat_bbox = get_bounding_box(hat_img)
    if not hat_bbox:
        print(f"Skipping {hat_name}: No visible pixels in hat.")
        return
        
    hat_min_x, hat_min_y, hat_max_x, hat_max_y = hat_bbox
    hat_cx = (hat_min_x + hat_max_x) // 2
    hat_bottom = hat_max_y
    
    print(f"Hat {hat_name} bounding box: {hat_bbox}, center x: {hat_cx}, bottom y: {hat_bottom}")

    # Build the base sheet without hair/hats
    base_parts = [
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png"
    ]

    base_frames = []
    base_sheet = Image.new("RGBA", (256, 1984), (0,0,0,0))
    for part in base_parts:
        try:
            img = Image.open(part).convert("RGBA")
            base_sheet.paste(img, (0,0), img)
        except Exception as e:
            print(f"Warning: {e}")

    # Also load the head sheet to extract head positions and paste it
    head_sheet_path = r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png"
    head_sheet = Image.open(head_sheet_path).convert("RGBA")
    base_sheet.paste(head_sheet, (0,0), head_sheet)

    for y in range(24):
        for x in range(4):
            box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
            base_frame = base_sheet.crop(box)
            head_frame = head_sheet.crop(box)
            
            head_bbox = get_bounding_box(head_frame)
            if head_bbox:
                head_min_x, head_min_y, head_max_x, head_max_y = head_bbox
                head_cx = (head_min_x + head_max_x) // 2
                head_top = head_min_y
                
                paste_x = head_cx - hat_cx
                paste_y = head_top - hat_bottom + overlap_y
                
                # Create a blank 64x64 frame for the hat
                hat_layer = Image.new("RGBA", (64, 64), (0,0,0,0))
                # Paste the entire hat image (which is likely 64x64) offset by paste_x, paste_y
                hat_layer.paste(hat_img, (paste_x, paste_y), hat_img)
                
                # Composite onto base_frame
                base_frame.paste(hat_layer, (0,0), hat_layer)
                
            base_frames.append(base_frame)

    # We need match_frames from the existing Used animation ui to match file names and GUIDs
    old_parts = [
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
        r"E:\PanicAtThePond\Assets\Resources\ShopUI\FishermansAnimations-Head_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
        r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png"
    ]
    match_sheet = Image.new("RGBA", (256, 1984), (0,0,0,0))
    for part in old_parts:
        try:
            img = Image.open(part).convert("RGBA")
            match_sheet.paste(img, (0,0), img)
        except: pass
        
    match_frames = []
    for y in range(24):
        for x in range(4):
            box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
            match_frames.append(match_sheet.crop(box))


    # 1. Main 96 frames
    ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Used animation ui"
    new_ui_dir = rf"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Default animation ui ({hat_name})"

    png_files = glob.glob(os.path.join(ui_dir, "**/*.png"), recursive=True)
    guid_map = {}

    print("Generating Main 96 Sprites...")
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
        
        base_frames[best_match].save(new_path)
        
        old_meta_path = path + ".meta"
        old_guid = None
        if os.path.exists(old_meta_path):
            with open(old_meta_path, 'r') as f:
                for line in f:
                    if line.startswith("guid:"):
                        old_guid = line.split("guid:")[1].strip()
                        break
        
        new_guid = uuid.uuid4().hex
        if old_guid:
            guid_map[old_guid] = new_guid
            
        with open(new_path + ".meta", 'w') as f:
            f.write(meta_template.format(guid=new_guid))

    # 2. 16 BoatFacingLeft frames
    boat_ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft"
    new_boat_ui_dir = rf"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft ({hat_name})"

    boat_png_files = glob.glob(os.path.join(boat_ui_dir, "**/*.png"), recursive=True)

    print("Generating 16 BoatFacingLeft Sprites...")
    for path in boat_png_files:
        ui_img = Image.open(path).convert("RGBA")
        best_match = -1
        best_diff = float('inf')
        
        for i, m_frame in enumerate(match_frames):
            d = mse_bottom_half(m_frame, ui_img)
            if d < best_diff:
                best_diff = d
                best_match = i
                
        rel_path = os.path.relpath(path, boat_ui_dir)
        new_path = os.path.join(new_boat_ui_dir, rel_path)
        os.makedirs(os.path.dirname(new_path), exist_ok=True)
        
        base_frames[best_match].save(new_path)
        
        old_meta_path = path + ".meta"
        old_guid = None
        if os.path.exists(old_meta_path):
            with open(old_meta_path, 'r') as f:
                for line in f:
                    if line.startswith("guid:"):
                        old_guid = line.split("guid:")[1].strip()
                        break
        
        new_guid = uuid.uuid4().hex
        if old_guid:
            guid_map[old_guid] = new_guid
            
        with open(new_path + ".meta", 'w') as f:
            f.write(meta_template.format(guid=new_guid))

    # 3. Duplicate and Patch .anim files
    old_anim_dir = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\UsedAnimations"
    new_anim_dir = rf"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\DefaultAnimations ({hat_name})"

    anim_files = glob.glob(os.path.join(old_anim_dir, "*.anim"))
    os.makedirs(new_anim_dir, exist_ok=True)

    anim_guid_map = {}

    print("Duplicating animations...")
    for old_path in anim_files:
        with open(old_path, 'r') as f:
            content = f.read()
        
        for old_g, new_g in guid_map.items():
            content = content.replace(old_g, new_g)
            
        basename = os.path.basename(old_path)
        new_path = os.path.join(new_anim_dir, basename)
        with open(new_path, 'w') as f:
            f.write(content)
            
        old_meta_path = old_path + ".meta"
        old_anim_guid = None
        with open(old_meta_path, 'r') as f:
            for line in f:
                if line.startswith("guid:"):
                    old_anim_guid = line.split("guid:")[1].strip()
                    break
                    
        new_anim_guid = uuid.uuid4().hex
        if old_anim_guid:
            anim_guid_map[old_anim_guid] = new_anim_guid
            
        with open(new_path + ".meta", 'w') as f:
            f.write(anim_meta_template.format(guid=new_anim_guid))

    # 4. Duplicate and Patch Animator Controller
    old_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan Yellow hat.controller"
    new_controller = rf"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan ({hat_name}).controller"

    print(f"Generating {hat_name} Animator Controller...")
    with open(old_controller, 'r') as f:
        content = f.read()

    for old_g, new_g in anim_guid_map.items():
        content = content.replace(old_g, new_g)
        
    with open(new_controller, 'w') as f:
        f.write(content)

if __name__ == "__main__":
    hats_dir = r"E:\PanicAtThePond\Assets\UI\ShopUI\ExtractedPDFSprites"
    
    # Specific list of hats
    hat_files = glob.glob(os.path.join(hats_dir, "fisherman_*.png"))
    for h in hat_files:
        if "red_hair" in h: continue # Already have red hair
        
        # Determine a nice display name
        basename = os.path.basename(h).replace(".png", "").replace("fisherman_", "")
        display_name = basename.replace("_", " ").title()
        
        generate_hat(h, display_name)
    
    print("ALL HATS PROCESSED SUCCESSFULLY!")
