import os

paths = [
    r"Assets/Resources/Fisherman created/Boat/BotAnimator.controller",
    r"Assets/Resources/Fisherman created/Cheast anim/Cheast Animator.controller",
    r"Assets/Resources/Fisherman created/Face/Face.controller",
    r"Assets/Resources/Fisherman created/hand anim/Hand Aniamator.controller",
    r"Assets/Resources/Fisherman created/oar/Oar.controller",
    r"Assets/Resources/Fisherman created/Road/Road.controller"
]

base_dir = r"E:\PanicAtThePond"

print("Verifying animator controller paths:")
for path in paths:
    full_path = os.path.join(base_dir, path)
    exists = os.path.exists(full_path)
    print(f"  {path} -> {'EXISTS' if exists else 'MISSING'}")
