import os
import glob
import uuid
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

parts = [
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-GreenBody_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Boat_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\FishermansAnimations-Head-BlackHair-Sheet-frame0.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Arms_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Oars_Sheet.png",
    r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\New Fisherman\FishermansAnimations-Rods_Sheet.png"
]

sheet_img = Image.new("RGBA", (256, 1984), (0,0,0,0))
for part in parts:
    try:
        img = Image.open(part).convert("RGBA")
        sheet_img.paste(img, (0,0), img)
    except Exception as e:
        print(f"Failed to load {part}: {e}")

frames = []
# We only care about the first 24 rows, as those map to the original 96 yellow hat sprites
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        frames.append(sheet_img.crop(box))

# MATCHING LAYERING
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
    except:
        pass

match_frames = []
for y in range(24):
    for x in range(4):
        box = (x * 64, y * 64, (x + 1) * 64, (y + 1) * 64)
        match_frames.append(match_sheet.crop(box))


# 1. Main 96 frames
ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Used animation ui"
new_ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\Default animation ui (Black Hair)"

png_files = glob.glob(os.path.join(ui_dir, "**/*.png"), recursive=True)
guid_map = {}

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
    
    frames[best_match].save(new_path)
    
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
new_boat_ui_dir = r"E:\PanicAtThePond\Assets\UI\Game UI\Fisherman\BoatFacingLeft (Black Hair)"

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
    
    frames[best_match].save(new_path)
    
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
new_anim_dir = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\DefaultAnimations (Black Hair)"

anim_files = glob.glob(os.path.join(old_anim_dir, "*.anim"))

os.makedirs(new_anim_dir, exist_ok=True)

anim_guid_map = {}

anim_meta_template = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

print("Duplicating animations...")
for old_path in anim_files:
    with open(old_path, 'r') as f:
        content = f.read()
    
    # Replace sprite GUIDs
    for old_g, new_g in guid_map.items():
        content = content.replace(old_g, new_g)
        
    basename = os.path.basename(old_path)
    new_path = os.path.join(new_anim_dir, basename)
    with open(new_path, 'w') as f:
        f.write(content)
        
    # generate new anim guid for the controller
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
old_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan.controller"
new_controller = r"E:\PanicAtThePond\Assets\Animations\Fisher Man Animations\Fisher Man Animator Controller\FisherMan (Black Hair).controller"

print("Generating Black Hair Animator Controller...")
with open(old_controller, 'r') as f:
    content = f.read()

for old_g, new_g in anim_guid_map.items():
    content = content.replace(old_g, new_g)
    
with open(new_controller, 'w') as f:
    f.write(content)

print("ALL BLACK HAIR ASSETS GENERATED SUCCESSFULLY!")
