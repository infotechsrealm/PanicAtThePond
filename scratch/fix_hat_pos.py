import re

def fix_dash():
    path = r'e:\PanicAtThePond\Assets\Scenes\Dash.unity'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Find DefaultHatIconAnchoredPosition
    content = re.sub(r'DefaultHatIconAnchoredPosition: \{x: -110, y: 120\}', r'DefaultHatIconAnchoredPosition: {x: -110, y: 70}', content)
    
    content = re.sub(r'\s{4}AnchoredPosition: \{x: -110, y: 120\}', r'    AnchoredPosition: {x: -110, y: 70}', content)
    
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(content)

fix_dash()
