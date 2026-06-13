def fix_manager():
    path = r'e:\PanicAtThePond\Assets\Scripts\ShopManager.cs'
    with open(path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    # We want to remove lines 115 to 228 (0-indexed 114 to 227)
    # Let's verify line 114 is `using UnityEngine;\n`
    if 'using UnityEngine;' in lines[114]:
        del lines[114:228]
        with open(path, 'w', encoding='utf-8', newline='\n') as f:
            f.writelines(lines)
            print("Fixed!")
    else:
        print("Line 115 is not using UnityEngine; it is:")
        print(lines[114])

fix_manager()
