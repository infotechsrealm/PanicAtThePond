import yaml

with open('Assets/Animations/Fisher Man Animations/Sprite Sheets/FishermansAnimations-GreenBody_Sheet.png.meta') as f:
    data = yaml.safe_load(f)

sprites = data['TextureImporter']['spriteSheet']['sprites']
print(f"Total sprites: {len(sprites)}")
for i, s in enumerate(sprites):
    name = s['name']
    r = s['rect']
    print(f"{name}: x={r['x']}, y={r['y']}, w={r['width']}, h={r['height']}")
